# Deployment Guide

This document is intentionally cloud-agnostic — the deployment target hasn't been decided yet. The app is a standard ASP.NET Core 8 web app (MVC admin panel + REST API) backed by SQL Server, and ships with a `Dockerfile` that runs unmodified on any container host (Azure App Service, AWS ECS/Fargate, Google Cloud Run, a bare VPS with Docker, etc.).

## Before you deploy anywhere — two things that need a decision first

1. **Rotate the credentials that leaked in git history.** The original inherited codebase had a live Razorpay key, a production DB password, and an SMS gateway password hardcoded in source (see `CODEBASE_AUDIT.md` §1). Confirm these have actually been rotated before this ever goes to a real production environment — the values below must be the *new*, rotated ones, never the old ones.
2. **Document storage is local-disk only right now** (`DocumentStorage:RootPath` — where agent-uploaded Aadhaar/Shop Act/Udyam documents are saved). This works fine on a single persistent VM, but on most container platforms (ECS, Cloud Run, App Service's default tier) local disk is wiped on every redeploy/restart/scale event. Either mount a persistent volume at that path, or plan a migration to cloud blob storage (Azure Blob/S3/GCS) before relying on this in real production — see `CODEBASE_AUDIT.md`'s Agent Module section for the reasoning behind the current design.

## Required environment variables

The app fails fast at startup with a clear error message if any of these are missing — it will not silently run with defaults for anything security-sensitive.

Config keys use ASP.NET Core's standard double-underscore convention for nested sections, e.g. `JwtSettings:SecretKey` in `appsettings.json` becomes the environment variable `JwtSettings__SecretKey`.

| Variable | Required? | Notes |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | **Yes, fails startup if missing** | SQL Server connection string. The app assumes SQL Server throughout (`UseSqlServer`) — confirm this is your actual target before deploying. |
| `JwtSettings__SecretKey` | **Yes, fails startup if missing** | Minimum 32 characters, cryptographically random. **Do not reuse the Development value.** Used to sign both User and Agent JWTs. |
| `JwtSettings__Issuer` | **Yes, fails startup if missing** | Any stable string identifying this API, e.g. `AshtavinayakApp-Prod`. |
| `JwtSettings__Audience` | **Yes, fails startup if missing** | Any stable string identifying the token consumers, e.g. `AshtavinayakApp-Prod-Users`. |
| `DocumentStorage__RootPath` | **Yes, fails startup if missing** | Absolute path outside any publicly-served directory. In the provided `Dockerfile` this defaults to `/app/data/documents` — mount a volume there. |
| `Razorpay__Key` / `Razorpay__Secret` | Needed for payments to work | Real (rotated) Razorpay credentials. Order creation will fail without these, but the app still starts. |
| `SmsGateway__User` / `SmsGateway__Password` | Needed for SMS/OTP to work | Real (rotated) gateway credentials. Without these, `SmsService` logs a warning and skips sending — OTP/booking-confirmation SMS silently won't go out, so don't deploy without setting these. |
| `SmsGateway__SenderId` / `SmsGateway__PeId` / `SmsGateway__OtpTemplateId` / `SmsGateway__BookingTemplateId` / `SmsGateway__VehicleTemplateId` | Needed for SMS/OTP to work | DLT-registered template IDs from the SMS gateway provider. |
| `Admin__Username` / `Admin__PasswordHash` | Needed for admin panel login | `PasswordHash` is a BCrypt hash, not plaintext — generate one fresh for production, never reuse the Development hash. |
| `Cors__AllowedOrigins__0`, `__1`, etc. | Recommended | If unset, CORS falls back to allowing any origin (`AllowAnyOrigin`) — fine for an API with no browser-based frontend of its own, but set this explicitly if a web frontend will call this API directly from a browser. |

## Deployment checklist

1. Set every environment variable above with real, rotated values.
2. Build and push the container image: `docker build -t ashtavinayak-app .`
3. Run database migrations against the target database before (or as part of) first boot: `dotnet ef database update` (from `AshtavinayakApp/`, with `ConnectionStrings__DefaultConnection` pointing at the target DB) — or apply the same SQL via your platform's migration step if you don't run this from a dev machine with DB network access.
4. Start the container, confirm `/health/live` returns healthy immediately (process is up).
5. Confirm `/health/ready` returns healthy (this checks actual DB connectivity — if it's not healthy, don't route traffic to this instance yet).
6. Smoke-test: log into the admin panel, confirm the dashboard loads with real data.
7. If using a load balancer / orchestrator, point its readiness probe at `/health/ready` and its liveness probe at `/health/live`.

## Health check endpoints

- `GET /health/live` — process-alive check only, no dependencies. Use for liveness probes (should a crashed/hung instance be restarted?).
- `GET /health/ready` — includes a live database connectivity check. Use for readiness probes (should this instance receive traffic right now?).

## Known gaps worth knowing about before relying on this in production

These are documented in detail in `CODEBASE_AUDIT.md` — flagged here as a deployment-relevant summary, not repeated in full:

- No server-side verification that a Razorpay payment actually succeeded before a booking is marked paid — `Transaction.PaymentStatus` is currently client-supplied.
- `EPPlus.Core` (used for admin panel "Export to Excel") pulls in two old, flagged-vulnerable transitive dependencies (`System.Data.SqlClient`, `System.Text.RegularExpressions`). Deferred intentionally — see `CODEBASE_AUDIT.md` Phase 4 section for the reasoning.
- No CI pipeline configured yet (`.github/workflows/` is empty) — `dotnet build`/`dotnet test` are confirmed clean locally, but nothing runs them automatically on push.
