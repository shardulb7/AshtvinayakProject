# Deployment Guide

This document is intentionally cloud-agnostic — the deployment target hasn't been decided yet. The app is a standard ASP.NET Core 8 web app (MVC admin panel + REST API) backed by SQL Server, and ships with a `Dockerfile` that runs unmodified on any container host (Azure App Service, AWS ECS/Fargate, Google Cloud Run, a bare VPS with Docker, etc.).

## Before you deploy anywhere — two things that need a decision first

1. **Rotate the credentials that leaked in git history.** The original inherited codebase had a live Razorpay key, a production DB password, and an SMS gateway password hardcoded in source (see `CODEBASE_AUDIT.md` §1). Confirm these have actually been rotated before this ever goes to a real production environment — the values below must be the *new*, rotated ones, never the old ones.
2. **Document storage is local-disk only right now** (`DocumentStorage:RootPath` — where agent-uploaded Aadhaar/Shop Act/Udyam documents are saved; defaults to an `App_Data/documents` folder next to the app if not set explicitly — see the environment variable table below). This works fine on a single persistent VM/server with no extra setup, but on most container platforms (ECS, Cloud Run, App Service's default tier) local disk is wiped on every redeploy/restart/scale event. On a plain VM or traditional host, no action is needed. On a container platform, either mount a persistent volume at that path, or plan a migration to cloud blob storage (Azure Blob/S3/GCS) before relying on this in real production — see `CODEBASE_AUDIT.md`'s Agent Module section for the reasoning behind the current design.

## Admin panel sessions — now backed by SQL Server, not in-memory

Admin login sessions and the Data Protection keys that encrypt them used to be entirely in-process/local-disk, which meant every restart or redeploy silently logged every admin out, and a second instance behind a load balancer wouldn't recognize sessions created on the first. This has been fixed:

- Sessions are now stored in a `SessionCache` table in the same SQL Server database (created automatically on first startup — no manual migration step needed).
- The Data Protection key ring (which encrypts the session cookie) is persisted to disk instead of the OS-default, ephemeral location — configurable via `DataProtection__KeysPath` (defaults to a `keys/` folder next to the app if unset; the provided `Dockerfile` sets it to `/app/data/keys`, alongside the document-storage volume).

Verified locally: logged in, restarted the app process, and the same session cookie still worked with no re-login required.

One caveat worth knowing: the persisted key file isn't encrypted at rest by an OS-level mechanism (no XML encryptor is configured, since that would require deciding on a Windows-DPAPI vs. certificate-based approach specific to the deployment target). It's protected the same way any other file on the server is — fine for a session-cookie-signing key, but keep the `keys/` directory in the volume as private as the rest of the app's data.

## Required environment variables

The app fails fast at startup with a clear error message if any of these are missing — it will not silently run with defaults for anything security-sensitive.

Config keys use ASP.NET Core's standard double-underscore convention for nested sections, e.g. `JwtSettings:SecretKey` in `appsettings.json` becomes the environment variable `JwtSettings__SecretKey`.

| Variable | Required? | Notes |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | **Yes, fails startup if missing** | SQL Server connection string. The app assumes SQL Server throughout (`UseSqlServer`) — confirm this is your actual target before deploying. |
| `JwtSettings__SecretKey` | **Yes, fails startup if missing** | Minimum 32 characters, cryptographically random. **Do not reuse the Development value.** Used to sign both User and Agent JWTs. |
| `JwtSettings__Issuer` | **Yes, fails startup if missing** | Any stable string identifying this API, e.g. `AshtavinayakApp-Prod`. |
| `JwtSettings__Audience` | **Yes, fails startup if missing** | Any stable string identifying the token consumers, e.g. `AshtavinayakApp-Prod-Users`. |
| `DocumentStorage__RootPath` | Optional | If unset, defaults to an `App_Data/documents` folder next to the app — `App_Data` is a standard ASP.NET convention that hosting platforms (including IIS) never serve as static files, so this is private with zero configuration. Set this explicitly only if you want documents on a specific mounted volume instead (the provided `Dockerfile` does this, pointing at `/app/data/documents`). Either way, this must never be a path under `wwwroot`. |
| `Razorpay__Key` / `Razorpay__Secret` | Needed for payments to work | Real (rotated) Razorpay credentials. Order creation will fail without these, but the app still starts. |
| `SmsGateway__User` / `SmsGateway__Password` | Needed for SMS/OTP to work | Real (rotated) gateway credentials. Without these, `SmsService` logs a warning and skips sending — OTP/booking-confirmation SMS silently won't go out, so don't deploy without setting these. |
| `SmsGateway__SenderId` / `SmsGateway__PeId` / `SmsGateway__OtpTemplateId` / `SmsGateway__BookingTemplateId` / `SmsGateway__VehicleTemplateId` | Needed for SMS/OTP to work | DLT-registered template IDs from the SMS gateway provider. |
| `Admin__Username` / `Admin__PasswordHash` | Needed for admin panel login | `PasswordHash` is a BCrypt hash, not plaintext — generate one fresh for production, never reuse the Development hash. |
| `Cors__AllowedOrigins__0`, `__1`, etc. | Recommended | If unset, CORS falls back to allowing any origin (`AllowAnyOrigin`) — fine for an API with no browser-based frontend of its own, but set this explicitly if a web frontend will call this API directly from a browser. The app now logs a startup warning (not a hard failure) if this is left unset outside Development. |
| `DataProtection__KeysPath` | Recommended | Where the session-cookie encryption key ring is persisted. Defaults to a `keys/` folder next to the app if unset. Must be on storage that survives restarts/redeploys — the `Dockerfile` already points this at a mounted path (`/app/data/keys`). |

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
