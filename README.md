# Ashtavinayak Yatra — Backend API & Admin Panel

ASP.NET Core 8 web application serving both:
- **REST API** consumed by the mobile app (Flutter/React Native frontend)
- **Admin Panel** (MVC Razor Views) for managing bookings, packages, trips

## ?? Live Server

| | |
|---|---|
| **URL** | https://ashtavinayak-travel.azurewebsites.net |
| **Admin Login** | Username: `admin` |
| **Azure Account** | itastourism@gmail.com |

## ??? Tech Stack

- ASP.NET Core 8 (.NET 8 LTS)
- Entity Framework Core (SQL Server)
- Serilog (structured logging)
- BCrypt (admin password hashing)
- Razorpay (payment gateway)
- Bulk SMS (OTP via bulksmspune.mobi)
- Azure App Service B1 + Azure SQL Basic

## ?? Project Structure

```
AshtavinayakApp/          Main project (API + Admin Panel)
  Controllers/            MVC + API controllers
  Models/                 EF Core entity models
  Views/                  Razor admin panel views
  Migrations/             EF Core database migrations
  Services/               SMS, email, notification services
  HealthChecks/           /health/live and /health/ready endpoints
  Program.cs              App startup, DI, middleware
AshtavinayakApp.Tests/    Unit tests
docs/                     Additional documentation
```

## ?? Local Development

```bash
# 1. Clone & restore
git clone <repo>
cd AshtavinayakApp
dotnet restore

# 2. Set secrets (appsettings.Development.json or user secrets)
# See .env.example for required keys

# 3. Run
dotnet run
# Admin panel: https://localhost:7289
# Swagger: https://localhost:7289/swagger
```

## ?? Deploy to Azure

See `DEPLOYMENT_NOTES.md` for full Azure deployment guide.

```powershell
dotnet publish -c Release -o ./publish
Compress-Archive ./publish/* app.zip
az webapp deploy --name ashtavinayak-travel --resource-group rg-itas-tourism --src-path app.zip --type zip
```

## ?? API Reference

See `API_CHANGES_FOR_FRONTEND.md` for the complete API reference for the frontend team.

## Health Endpoints

- `GET /health/live` — Is the process alive?
- `GET /health/ready` — Is the DB connected and ready?
