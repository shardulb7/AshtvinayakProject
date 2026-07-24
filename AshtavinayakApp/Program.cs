using AshtavinayakAPP.Data;
using AshtavinayakAPP.HealthChecks;
using AshtavinayakAPP.Models;
using AshtavinayakAPP.Services.AgentSrc;
using AshtavinayakAPP.Services.BookingSrc;
using AshtavinayakAPP.Services.CategoryService;
using AshtavinayakAPP.Services.DocumentStorage;
using AshtavinayakAPP.Services.PackageService;
using AshtavinayakAPP.Services.RazorPay;
using AshtavinayakAPP.Services.SmsService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

// LOW-08: Configure Serilog before building the host so startup errors are captured
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// LOW-08: Replace default logging with Serilog
builder.Host.UseSerilog();

builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IRazorpayService, RazorpayService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IDocumentStorageService, DocumentStorageService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddHttpClient("SmsGateway");

// Validate document-storage config at startup — fail fast with a clear error if missing.
// In Development: set in appsettings.Development.json → DocumentStorage:RootPath
// In Production:  set the environment variable DocumentStorage__RootPath
var documentStorageRootPath = builder.Configuration["DocumentStorage:RootPath"];
if (string.IsNullOrWhiteSpace(documentStorageRootPath))
    throw new InvalidOperationException(
        "Document storage root path 'DocumentStorage:RootPath' is not configured. " +
        "In Development, set it in appsettings.Development.json. " +
        "In Production, set the environment variable 'DocumentStorage__RootPath'.");

// Agent login is the first password-based, self-registered, publicly-reachable login in this
// app (every other login is OTP-based or the single non-self-serve Admin account) — rate-limit it.
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("agent-login", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(15);
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// CORS — AllowedOrigins read from configuration per environment
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCorsPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        else
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
    });
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// MED-01: Swagger/OpenAPI with JWT Bearer support in Authorize dialog
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Ashtavinayak Travel API",
        Version     = "v1",
        Description = "REST API for Ashtavinayak Tour booking platform"
    });
    // Allow JWT token entry in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token (without the 'Bearer' prefix)"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// MED-05: Single session registration (30-minute timeout) — removed duplicate at top that had 24-hour timeout

// Validate JWT settings at startup — fail fast with a clear error if any are missing.
// In Development: set in appsettings.Development.json → JwtSettings section
// In Production:  set environment variables JwtSettings__SecretKey, JwtSettings__Issuer, JwtSettings__Audience
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecretKey))
    throw new InvalidOperationException(
        "JWT Secret Key 'JwtSettings:SecretKey' is not configured. " +
        "In Development, set it in appsettings.Development.json. " +
        "In Production, set the environment variable 'JwtSettings__SecretKey'.");

var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
if (string.IsNullOrWhiteSpace(jwtIssuer))
    throw new InvalidOperationException(
        "JWT Issuer 'JwtSettings:Issuer' is not configured. " +
        "In Development, set it in appsettings.Development.json. " +
        "In Production, set the environment variable 'JwtSettings__Issuer'.");

var jwtAudience = builder.Configuration["JwtSettings:Audience"];
if (string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException(
        "JWT Audience 'JwtSettings:Audience' is not configured. " +
        "In Development, set it in appsettings.Development.json. " +
        "In Production, set the environment variable 'JwtSettings__Audience'.");

// Configure Authentication with JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // MED-14: without this, the JWT bearer handler silently remaps short claim names
        // (e.g. "sub") to long legacy URIs on the way in, which breaks every
        // User.FindFirst(JwtRegisteredClaimNames.Sub) lookup in the codebase (used for
        // deriving the caller's identity from the token — Transaction/Notification
        // ownership checks, Agent identity in bookings, etc.) without throwing any error;
        // it just silently returns null. Tokens are already issued with standard short
        // claim names (see UserController/AgentService's GenerateJwtToken), so disabling
        // inbound remapping makes claims round-trip exactly as issued.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecretKey))
        };
    });






builder.Services.AddAuthorization();


// Read connection string from configuration.
// In Development: set in appsettings.Development.json → ConnectionStrings:DefaultConnection
// In Production:  set the environment variable ConnectionStrings__DefaultConnection
// If not configured, the application fails at startup with a clear, actionable error.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection string 'DefaultConnection' is not configured. " +
        "In Development, set it in appsettings.Development.json under 'ConnectionStrings:DefaultConnection'. " +
        "In Production, set the environment variable 'ConnectionStrings__DefaultConnection'.");

builder.Services.AddDbContext<AshtvinayakTravelContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// Readiness includes DB connectivity; liveness is process-alive only (see MapHealthChecks below).
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });

builder.Services.AddHttpContextAccessor();


builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Build the app
var app = builder.Build();

// Seed reference data on startup — idempotent, safe on every restart.
// Exceptions are caught inside SeedAsync; the app continues even if seeding fails.
var seederLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
await DatabaseSeeder.SeedAsync(app.Services, seederLogger);

// Middleware setup
// Middleware setup
if (app.Environment.IsDevelopment())
{
    // MED-01: Swagger UI only in Development
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ashtavinayak Travel API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();
app.UseRouting();
app.UseRateLimiter();

// Enable Authentication & Authorization
app.UseCors("AppCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.MapControllers(); // MED-01: required for API controllers to be discoverable by Swagger

// Liveness: process is up, no dependency checks — for "is it running" probes.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
// Readiness: includes the DB connectivity check — for "can it actually serve traffic" probes.
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

await app.RunAsync();
