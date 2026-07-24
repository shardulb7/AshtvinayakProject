# Local Setup Guide

Instructions for getting the Ashtavinayak Travel App backend running on a new machine.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB is fine for local development — it ships with Visual Studio, or install the [SQL Server Express LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) package separately)
- The EF Core CLI tool (`dotnet tool install --global dotnet-ef`, or it will be restored automatically via `AshtavinayakApp/.config/dotnet-tools.json` when you run `dotnet tool restore`)

## 1. Clone and restore

```bash
git clone <repository-url>
cd AshtavinayakApp
dotnet tool restore
dotnet restore
```

## 2. Configure local settings

Copy the required keys into `AshtavinayakApp/appsettings.Development.json` (this file is already present with placeholder values for most keys — fill in real ones for anything you need to exercise):

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string. Default points at LocalDB. |
| `JwtSettings:SecretKey` / `Issuer` / `Audience` | Used to sign and validate JWTs for both the customer and agent APIs. Any values work locally as long as they're consistent. |
| `Razorpay:Key` / `Secret` | Only needed if you're testing the payment order flow — use Razorpay test-mode credentials. |
| `SmsGateway:*` | Only needed if you want real OTP/SMS delivery. Without these, OTPs are still generated and usable — see the dev-only OTP note below. |
| `Admin:Username` / `PasswordHash` | Admin panel login. `PasswordHash` is a BCrypt hash, not plaintext — generate one with `BCrypt.Net.BCrypt.HashPassword("your-password")` if you want to change it. |
| `DocumentStorage:RootPath` | Local folder path (outside `wwwroot`) where agent-uploaded documents are saved. |

The app fails to start with a clear error message if any of the required keys (`ConnectionStrings`, `JwtSettings`, `DocumentStorage:RootPath`) are missing — that's expected behavior, not a bug.

## 3. Create/update the database

```bash
cd AshtavinayakApp
dotnet ef database update
```

This applies all migrations and creates the schema. On first run against an empty database, the app also seeds a small set of reference data automatically (cities, categories, packages, trips, seats, and a default commission rate) — this happens on every startup and is safe to run repeatedly.

## 4. Run the app

```bash
dotnet run
```

By default this listens on the URLs configured in `Properties/launchSettings.json`. In the Development environment, Swagger UI is available at `/swagger` for exploring and testing the REST API directly.

## 5. Log in

- **Admin panel**: the root URL redirects to the login page. Use the username/password corresponding to whatever's configured under `Admin:Username`/`PasswordHash`.
- **Customer API**: register via `POST /api/User/Register`, then log in via `POST /api/User/LoginByOTP` followed by `POST /api/User/VerifyOTP`. In Development, the OTP response includes a `devOnlyOtp` field so you can complete the flow without a working SMS gateway — this field is never present outside Development.
- **Agent API**: register via `POST /api/Agent/Register` (multipart form with the three required documents), then have an admin approve the account from the Agents section of the admin panel before logging in via `POST /api/Agent/Login`.

## Running tests

```bash
dotnet test
```

This runs the `AshtavinayakApp.Tests` project (unit tests, no database required — they use an in-memory provider).

## Notes

- Uploaded agent documents are stored outside `wwwroot` and are only ever served back through an authenticated admin download action, never a public URL.
- Health check endpoints are available at `/health/live` (process liveness) and `/health/ready` (includes a database connectivity check) — useful for confirming the app started correctly.
