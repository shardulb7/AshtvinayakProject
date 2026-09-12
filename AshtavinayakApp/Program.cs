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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

// Document storage root — where agent-uploaded KYC documents (Aadhaar, Shop Act License,
// Udyam Certificate) are saved. Deliberately kept outside wwwroot: these are private ID/
// business documents and must never be reachable by a public URL, only through the
// authenticated admin download action. DocumentStorage:RootPath is optional — if it isn't
// set, this defaults to an App_Data folder next to the app. App_Data is a long-standing
// ASP.NET convention that hosting platforms (including IIS) never serve as static files,
// unlike wwwroot — so a plain deployment works with zero extra configuration while staying
// just as private. Set DocumentStorage__RootPath explicitly only if you want documents on a
// specific volume/mount instead of next to the app.
var documentStorageRootPath = builder.Configuration["DocumentStorage:RootPath"];
if (string.IsNullOrWhiteSpace(documentStorageRootPath))
{
    documentStorageRootPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "documents");
    Directory.CreateDirectory(documentStorageRootPath);
    builder.Configuration["DocumentStorage:RootPath"] = documentStorageRootPath;
    Log.Information("DocumentStorage:RootPath not configured — defaulting to {DefaultPath} (private, outside wwwroot).", documentStorageRootPath);
}

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

// Not fatal (a mobile-only client has no browser origin to restrict), but silently allowing
// any origin is worth calling out loudly outside Development rather than only in DEPLOYMENT.md.
if (allowedOrigins.Length == 0 && !builder.Environment.IsDevelopment())
{
    Log.Warning(
        "Cors:AllowedOrigins is not configured — CORS is falling back to allow-any-origin. " +
        "If a browser-based frontend calls this API directly, set Cors__AllowedOrigins__0 " +
        "(and __1, etc.) to its real domain(s) before relying on this in production.");
}

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


// Data Protection keys encrypt the session cookie (and anti-forgery tokens). Left at its
// default, the key ring lives under the OS user profile — on every restart/redeploy on a
// fresh container, or under a different account, the keys are lost and every logged-in
// admin session silently breaks. Persisting to a configurable, stable path fixes that for
// a single persistent instance. DataProtection:KeysPath is optional; defaults to a folder
// under the app's own content root. Scaling to more than one instance additionally requires
// this path to be shared/persistent storage reachable by every instance.
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
var keysDirectory = string.IsNullOrWhiteSpace(dataProtectionKeysPath)
    ? Path.Combine(builder.Environment.ContentRootPath, "keys")
    : dataProtectionKeysPath;
builder.Services.AddDataProtection()
    .SetApplicationName("AshtavinayakApp")
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory));

// The admin panel's login session is stored via this cache. AddDistributedMemoryCache (the
// default) lives entirely in-process — every restart/redeploy silently logs every admin out,
// and a second instance behind a load balancer wouldn't see sessions created on the first.
// Backing it with the same SQL Server the app already depends on fixes both, with no new
// infrastructure to provision. The required table is created idempotently below.
builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = connectionString;
    options.SchemaName = "dbo";
    options.TableName = "SessionCache";
});
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// Build the app
var app = builder.Build();

// AUTO-MIGRATE: Apply any pending EF Core migrations on startup.
// This is idempotent — safe to run on every restart. It creates the schema on a fresh
// Azure SQL Database and applies any new migrations after deploys automatically.
// Exceptions are logged and re-thrown so the app fails fast with a clear error if the DB
// is unreachable or the migration fails (rather than silently starting broken).
try
{
    using var migrationScope = app.Services.CreateScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<AshtvinayakTravelContext>();
    Log.Information("Applying EF Core migrations...");
    await dbContext.Database.MigrateAsync();
    Log.Information("EF Core migrations applied successfully.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to apply EF Core migrations. The application cannot start.");
    throw;
}

// Create the SQL-backed session cache table if it doesn't exist yet — idempotent, safe on
// every restart. Schema matches what AddDistributedSqlServerCache/Microsoft.Extensions.Caching.SqlServer
// expects (the same table `dotnet sql-cache create` would generate).
await using (var cacheTableConnection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
{
    await cacheTableConnection.OpenAsync();
    await using var cacheTableCommand = cacheTableConnection.CreateCommand();
    cacheTableCommand.CommandText = """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SessionCache' AND schema_id = SCHEMA_ID('dbo'))
        BEGIN
            CREATE TABLE [dbo].[SessionCache] (
                [Id] NVARCHAR(449) NOT NULL,
                [Value] VARBINARY(MAX) NOT NULL,
                [ExpiresAtTime] DATETIMEOFFSET(7) NOT NULL,
                [SlidingExpirationInSeconds] BIGINT NULL,
                [AbsoluteExpiration] DATETIMEOFFSET(7) NULL,
                CONSTRAINT [PK_SessionCache] PRIMARY KEY CLUSTERED ([Id] ASC)
            );
            CREATE NONCLUSTERED INDEX [Index_SessionCache_ExpiresAtTime] ON [dbo].[SessionCache]([ExpiresAtTime]);
        END
        """;
    await cacheTableCommand.ExecuteNonQueryAsync();
}

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

// ── Testing bypass login ──────────────────────────────────────────────────────
// Minimal API endpoint — completely outside MVC pipeline, no global auth filters.
// Active only when Testing__Key is set in Azure App Service Configuration.
// Remove that setting to disable the endpoint before go-live.
app.MapPost("/api/bypass/login", async (
    HttpRequest req,
    IConfiguration config,
    AshtvinayakTravelContext db) =>
{
    var testKey = config["Testing:Key"];
    if (string.IsNullOrWhiteSpace(testKey))
        return Results.NotFound(new { message = "Test login endpoint not available." });

    BypassLoginRequest? body;
    try { body = await req.ReadFromJsonAsync<BypassLoginRequest>(); }
    catch { return Results.BadRequest(new { message = "Invalid JSON body." }); }

    if (body is null)
        return Results.BadRequest(new { message = "Request body is required." });

    if (body.TestKey != testKey)
        return Results.Json(new { message = "Invalid testing key." }, statusCode: 401);

    if (string.IsNullOrEmpty(body.MobileNo))
        return Results.BadRequest(new { message = "mobileNo is required." });

    var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == body.MobileNo);
    if (user is null)
        return Results.NotFound(new { message = "Mobile number not registered." });

    // Build JWT — same settings as UserController.GenerateJwtToken
    var jwtCfg  = config.GetSection("JwtSettings");
    var keyBytes = Encoding.UTF8.GetBytes(jwtCfg["SecretKey"]
        ?? throw new InvalidOperationException("JwtSettings:SecretKey not configured."));
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub,   user.UserId.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.Role,               user.Role),
        new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        new Claim("PhoneNumber",                 user.PhoneNumber)
    };
    var jwt = new JwtSecurityToken(
        issuer:            jwtCfg["Issuer"],
        audience:          jwtCfg["Audience"],
        claims:            claims,
        expires:           DateTime.UtcNow.AddHours(24),
        signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256));
    var token = new JwtSecurityTokenHandler().WriteToken(jwt);

    Log.Warning("[BypassLogin] JWT issued without OTP for {Phone} — testing bypass used.", body.MobileNo);

    return Results.Ok(new
    {
        message = "Test login successful.",
        token,
        user = new { user.UserId, user.UserName, user.Email, user.PhoneNumber, user.Role }
    });
}).AllowAnonymous();

await app.RunAsync();

/// <summary>Request model for the /api/bypass/login testing endpoint.</summary>
record BypassLoginRequest(string MobileNo, string TestKey);
