using AshtavinayakAPP.Models;
using AshtavinayakAPP.Services.BookingSrc;
using AshtavinayakAPP.Services.CategoryService;
using AshtavinayakAPP.Services.PackageService;
using AshtavinayakAPP.Services.PakageService;
using AshtavinayakAPP.Services.RazorPay;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<IBookingService,BookingService>();
builder.Services.AddScoped<IRazorpayService, RazorpayService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache(); 
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24); 
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure Authentication with JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]))
        };
    });






builder.Services.AddAuthorization();


//builder.Services.AddDbContext<AshtvinayakTravelAppContext>(options =>
//    options.UseSqlServer("workstation id=AshtvinayakTravelApp.mssql.somee.com;packet size=4096;user id=TechMafiya_SQLLogin_1;pwd=k87vpca19n;data source=AshtvinayakTravelApp.mssql.somee.com;persist security info=False;initial catalog=AshtvinayakTravelApp;TrustServerCertificate=True"));

builder.Services.AddDbContext<AshtvinayakTravelContext>(options =>
    options.UseSqlServer("Data Source=LAPTOP-LE8NGR23\\SQLEXPRESS;Initial Catalog=AshtvinayakTravelApp;Integrated Security=True;TrustServerCertificate=True"));


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

// Middleware setup
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();
app.UseRouting();

// Enable Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();
