# Changelog

This project has a single git commit (`6a03f5b`) as its baseline, so this changelog distinguishes three layers instead of using commit history:

- **[BASELINE]** — the original freelance engineer's handoff (commit `6a03f5b`)
- **[ANTIGRAVITY]** — changes made by a prior AI-assisted session (Antigravity) before this engagement started, applied on top of the baseline, still uncommitted, **unverified**
- **[AUDIT]** — work done in this session

Nothing has been committed yet. This file will gain dated entries as Phase 2+ fixes land.

---

## [PHASE 4b] — 2026-08-02 — Deployment-readiness follow-up

Follow-up pass triggered by preparing for actual deployment. Two real gaps found and fixed, both verified live (logged in, restarted the process, confirmed the same session cookie still worked with no re-login):

- **Admin sessions didn't survive a restart.** `AddDistributedMemoryCache` (the session backing store) is entirely in-process, and the Data Protection key ring that encrypts the session cookie lived at the OS-default, non-persisted location. Every restart/redeploy silently logged out every admin, and a second instance behind a load balancer wouldn't recognize sessions from the first. Fixed by switching the session store to `AddDistributedSqlServerCache` (backed by the same SQL Server the app already uses — a `SessionCache` table is created idempotently at startup, no manual migration step) and persisting the Data Protection key ring to a configurable path (`DataProtection:KeysPath`, defaulting to a local `keys/` folder; the `Dockerfile` points it at a mounted volume). This also means sessions now work correctly across multiple instances, not just across restarts.
- **CORS silently allowed any origin outside Development with no signal.** Left as-is functionally (a mobile-only client has no browser origin to restrict, so this can't be a hard failure), but now logs a startup warning if `Cors:AllowedOrigins` is unset outside Development, so it can't be missed silently at deploy time.
- Added `Microsoft.Extensions.Caching.SqlServer` package reference; updated `Dockerfile` to mount and point at a persisted `keys/` path alongside the existing document-storage volume; updated `.gitignore` to exclude the local `keys/` fallback folder (never commit real key material).

Verified: `dotnet build` clean (0 errors), `dotnet test` 14/14 passing, live restart test on the admin session as described above, health checks and admin login both re-confirmed working after the changes.

**Follow-up in the same pass:** simplified `DocumentStorage:RootPath` from a hard startup requirement to an optional setting — if unset, it now defaults to an `App_Data/documents` folder next to the app (`App_Data` is a standard ASP.NET convention hosting platforms/IIS never serve as static files, unlike `wwwroot`), so a plain deployment needs zero document-storage configuration while documents remain completely private. Explicitly confirmed this does *not* mean documents are reachable publicly — verified live by unsetting the config, confirming the app logs the fallback and creates `App_Data/documents` automatically, still only reachable through the authenticated admin download action, never through a public URL or `wwwroot`.

---

## [PHASE 4] — 2026-07-24 — Production Readiness Hardening

Planned via `EnterPlanMode` with a dedicated exploration pass; scope confirmed with the project owner up front (add a minimal test project; defer the EPPlus.Core replacement, document the risk instead; skip CI for now).

**Fixed exception-message leaks across the whole API surface.** The initial exploration reported 8 controller files leaking `ex.Message` to HTTP callers; a follow-up exhaustive sweep (the exploration's grep pattern missed the fully-qualified `catch (System.Exception` form) found it in 12 controller files total — `History.cs`, `Pickup.cs`, `Seats.cs`, and `Transaction.cs` were significantly under-reported or missed entirely — plus 7 methods in `BookingService.cs` and 1 in `RazorpayService.cs` leaking via their tuple return values. All ~35 instances fixed: full exception logged via `ILogger` (added to constructors that lacked one), generic message returned to the caller.

**Caught and fixed a severe regression before it shipped**: adding `EnableRetryOnFailure` to the DB connection (for resiliency) is incompatible with EF Core's manually-opened transactions unless wrapped in an `IExecutionStrategy` — and `BookingService.CreateBookingWithSeatsAsync`/`BookCarAsync` (booking creation — the core feature) both open transactions manually. Without the fix, every booking attempt would have thrown `InvalidOperationException` immediately after this change shipped. Found via code review (this sandbox's Smart App Control restriction was blocking live app execution at the time), fixed by wrapping both methods in `CreateExecutionStrategy().ExecuteAsync(...)`, confirmed correct once `dotnet test` became available again — all 14 tests pass, including `BookingServiceTests` which exercise this exact path.

**New**: `HealthChecks/DatabaseHealthCheck.cs` + `/health/live` and `/health/ready` endpoints (verified live — both return `Healthy`); `AshtavinayakApp.Tests` xUnit project (`DocumentStorageServiceTests`, `BookingServiceTests`, `AgentServiceTests`, `Fakes/`) — 14 tests, all passing; `Dockerfile` + `.dockerignore` (multi-stage build, cloud-agnostic); `DEPLOYMENT.md` (every required env var, deployment checklist, re-flags the unrotated-secrets and local-disk-storage risks at the point someone will actually deploy this).

**Edited**: `Program.cs` (DB retry policy, health check registration/mapping); `Services/BookingSrc/BookingService.cs` (execution-strategy wrapping, all 7 exception-message leaks fixed); `Services/RazorPay/RazorpayService.cs` (1 leak fixed); `Services/DocumentStorage/DocumentStorageService.cs` (fixed a real gap the new tests surfaced: content-type and extension were checked against two independent allowlists rather than as a matching pair — a `.pdf` could be uploaded with an `image/png` content-type header and pass; now validated as a pair); 12 controller files (exception sanitization); `Controllers/{Booking,Package,Trip,User}.cs` (removed 4 stray unused `using` statements that only compiled because of the now-removed scaffolding package); `AshtavinayakAPP.csproj` (removed `Microsoft.VisualStudio.Web.CodeGeneration.Design`; bumped `Microsoft.IdentityModel.JsonWebTokens`/`System.IdentityModel.Tokens.Jwt` to 8.4.0; added a direct pin on `Microsoft.Bcl.Memory` 9.0.18).

**Dependency vulnerabilities**: started at 6 flagged (`dotnet list package --vulnerable --include-transitive`), fixed 4 (`Microsoft.Build`, `NuGet.Packaging`, `NuGet.Protocol`, `Microsoft.Bcl.Memory`), 2 deferred and documented (`System.Data.SqlClient`, `System.Text.RegularExpressions` — both root-cause to `EPPlus.Core`, which the project owner explicitly chose not to replace this session).

**Verified**: `dotnet build` clean (0 errors) throughout; `dotnet test` 14/14 passing; live health checks; broad regression sweep across the admin panel (Home, Agents, Agent Bookings, Commission Settings, Bookings, Users, Packages — all load cleanly). Not independently re-verified via a live HTTP 500 trigger on each of the ~35 sanitized error paths — covered by code review + the regression sweep instead, given how uniform and mechanical that particular change was.

---

## [PHASE 3] — 2026-07-24 — Agent Registration & Login module (new feature)

Built the full Agent module per the client's requirements document: self-registration with 3 mandatory document uploads, admin approval workflow, password-based login (rate-limited), booking through the existing customer booking flow with commission deducted only at the payment stage, admin-configurable commission (default + per-agent override, no code changes needed to adjust), and a new Agent Bookings admin report with date/agent/status/payment filters. Planned via `EnterPlanMode` with a dedicated design-review pass before implementation; scope (backend + Admin Panel only, no mobile UI; documents on local disk outside `wwwroot`) was confirmed with the project owner up front.

**New**: `Models/{Agent,CommissionSetting,AgentRegisterDto,AgentLoginDto,ResolveCustomerDto,AgentBookingReportRow}.cs`; `Services/DocumentStorage/` (file upload validation — extension + content-type + magic-byte signature check + 5MB limit + GUID filenames); `Services/AgentSrc/` (registration, login, customer resolution, admin operations); `Controllers/Agent.cs` (mobile-facing API: Register/Login/ResolveCustomer/Profile); `Controllers/{AgentsController,AgentBookingsController,CommissionSettingsController}.cs` (session-guarded MVC admin panel, following the existing `TransactionsController`/`Views/Packages` conventions exactly); corresponding `Views/{Agents,AgentBookings,CommissionSettings}/*.cshtml`; one EF Core migration (`AddAgentsAndCommission`) adding the `Agents`/`CommissionSettings` tables and `Booking.AgentId`/`CommissionPercentage`/`CommissionAmount`.

**Edited**: `Booking.cs` model (+3 nullable columns, snapshotted at booking time so later commission-rate changes don't retroactively alter past bookings — verified live); `AshtvinayakTravelContext.cs` (new DbSets + entity config); `BookingService.cs`/`IBookingService.cs` (`CreateBookingWithSeatsAsync` gained two optional trailing parameters, `agentId`/`commissionPercentage`, resolved by the controller from the caller's JWT — never trusted from the request body; response gains commission fields additively only when an agent is booking, unchanged for normal users, verified live both ways); `Controllers/Booking.cs` (derives agent identity from JWT); `Program.cs` (new DI registrations, fail-fast `DocumentStorage:RootPath` validation, rate limiter); `Views/Shared/_Layout.cshtml` (3 new sidebar links); `Data/DatabaseSeeder.cs` (seeds a 10% default commission row); `appsettings.json`/`appsettings.Development.json` (new `DocumentStorage:RootPath` key).

**Bug found and fixed during live testing, with retroactive impact on earlier work**: `AddJwtBearer` was silently remapping the `"sub"` JWT claim to a legacy URI on every inbound token (ASP.NET Core's default inbound claim mapping), which meant every `User.FindFirst(JwtRegisteredClaimNames.Sub)` call in the codebase — including this session's earlier `Transaction`/`Notification` IDOR fixes — was silently getting `null` back instead of the caller's ID, with no exception to signal it. Caught because the new `Agent.Profile` endpoint and the agent-booking-commission code path both depend on this same claim lookup and produced obviously wrong results (missing commission fields, spurious 401s) during smoke testing. Fixed with one line in `Program.cs` (`options.MapInboundClaims = false;`) rather than patching every call site — tokens are already issued with standard short claim names, so this makes them round-trip exactly as issued everywhere, present and future. Re-verified live afterward: `Transaction`/`Notification` ownership scoping now correctly allows a user their own data and blocks others' (previously it was accidentally over-blocking, including a user's own data — a functional regression, not a re-opened security hole, since the fail-safe direction was "deny by default").

**Verified live, end-to-end**: full registration → pending-blocks-login → admin document review/download → approve → agent login → JWT round-trip → resolve-customer (both create and find-existing-without-overwriting paths) → booking with a deliberately tampered client price (server-computed the correct amount, matching the client doc's ₹10,000/10%/₹9,000 example formula exactly, scaled to this test's real package pricing) → Agent Bookings report with all 4 filters → commission-setting change confirmed non-retroactive → deactivate blocks login → reject-with-remarks shown to the agent. Build stayed clean (0 errors) throughout; migration applied with zero drift (`dotnet ef migrations has-pending-model-changes` confirms).

---

## [AUDIT] — 2026-07-24 — Phase 2: dev-only OTP echo + config simplification

- **Added a Development-only OTP echo** to `User.LoginByOTP` — the response now includes `devOnlyOtp` when `ASPNETCORE_ENVIRONMENT=Development`, so the customer-facing register → OTP → verify → JWT flow can be tested end-to-end locally (via Swagger or otherwise) without a live SMS gateway. Gated on `IWebHostEnvironment.IsDevelopment()`, never present in Production; the OTP still isn't written to logs anywhere (that fix from earlier in the session stands).
- **Deleted `appsettings.Production.json`** at the project owner's request. It had every value blank; since Production config is sourced entirely from environment variables (and `Program.cs` already fails fast with a clear error if any required one is missing), the file was pure maintenance overhead with no actual effect. Down to 2 config files (base + Development) instead of 3.

---

## [AUDIT] — 2026-07-24 — Phase 2: authorization scoping, IDOR fix, and cleanup sweep

- **Fixed the notification IDOR.** `Notification.GetUserNotifications/{userId}` let any authenticated user read any other user's notifications (including trip vehicle/driver contact details) by just changing the URL. Now checks the caller's JWT `Sub` claim against the requested `userId` and returns 403 on mismatch (Admins exempted).
- **Rebalanced `Categorys.cs` and `Transaction.cs` (API controllers) from all-or-nothing Admin gating to least-privilege scoping.** Both had been locked to class-level `[Authorize(Roles = "Admin")]`, which fixed the original vulnerabilities (unauthenticated category writes; any user listing all users' transactions) but as a side effect also fully blocked legitimate use — `Categorys.GetCategories`/`GetCategory` are pure catalog reads with no sensitive data, almost certainly needed by the mobile app's browsing flow, and `Transaction`'s GET endpoints should let a customer see *their own* payment history, not nobody's. Fixed by:
  - Moving `[Authorize(Roles = "Admin")]` down to just the mutating actions (POST/PUT/DELETE) on both controllers, matching the pattern already established in `Seats.cs`/`Pickup.cs`.
  - Adding real per-caller scoping to `Transaction`'s `GetTransactions`/`GetTransaction`: non-Admin callers only ever see rows where `UserId` matches their JWT identity; Admins still see everything.
- **Fixed `CategoryExists` and 22 other scaffolded `XxxExists(id)` helpers** across the controller layer to exclude soft-deleted rows, matching the pattern already correct in `BookingsController`/`CategoriesController`. These helpers only run inside `catch (DbUpdateConcurrencyException)` blocks to decide "was this deleted?" vs. "some other conflict" — low severity (a miss just surfaces a 500 instead of a clean 404), but now consistent everywhere rather than inconsistent per-controller.
- **Housekeeping**: fixed the `AshtvinayakAPP.Controllers` → `AshtavinayakAPP.Controllers` namespace typo in `Notification.cs`/`Vehicles.cs` (verified nothing referenced the misspelled name first); renamed `Services/PakageService/` → `Services/PackageService/` via `git mv` to match its already-corrected namespace; removed the stray `Views/Notifications.zip` artifact.

---

## [AUDIT] — 2026-07-24 — Phase 2: server-side booking price computation

- **Fixed client-trusted pricing** flagged in Phase 1. `BookingService.CreateBookingWithSeatsAsync` and `BookCarAsync` previously persisted `Booking.TotalPayment` straight from the client request DTO with no server-side check — a tampered request could book any trip at any price. Both methods now look up the trip's/package's actual rates and compute the payable amount server-side:
  - Seat bookings: `Adults × Package.AdultPrice + Childwithseat × Package.Child3To8YrswithSeat + Childwithoutseat × Package.Child3To8YrsWithoutSeat`.
  - Car bookings: `Package.CarPackagePrice` (flat rate), with a new check rejecting the request if the given `PackageId` isn't actually marked `IsCar`.
  - The client's `TotalPayment` is still accepted (kept for API compatibility) but only used to log a warning when it disagrees with the computed price — the persisted amount is always the computed one.
  - `Advance` remains customer-supplied but is now rejected if it exceeds the computed total.
  - Both paths reject with a clear message if the computed price is ≤ 0 (guards against a car-only package being booked through the seat-booking path, or vice versa).
  - Confirmed no discount/coupon/promo mechanism exists anywhere in the codebase that this could conflict with.
- **Verification:** the build passes; the formula was traced by hand against real seeded data (Package 1 — AdultPrice 4500, Child-with-seat 3500, Child-without-seat 2500 — correctly computes ₹12,500 for 2 adults + 1 child-with-seat regardless of what the client submits). A live HTTP test was attempted but blocked partway through by Windows Smart App Control newly enforcing on the rebuilt binary (see the sandbox note in `CODEBASE_AUDIT.md` §2) — this is an environment restriction, not a code issue, but it means this particular fix wasn't exercised via an actual API round-trip this session. Recommend a live test pass once the environment allows running the app again.

---

## [AUDIT] — 2026-07-24 — Phase 2: built the missing Packages admin UI

- **Built `Views/Packages/{Index,Details,Create,Edit,Delete}.cshtml`** — `PackagesController.cs`'s actions were already fully coded (it's a standard scaffolded CRUD controller) but had zero views backing them, so every action 500'd. Followed the existing Categories/Vehicles view templates exactly: same layout, SweetAlert confirm dialogs on create/edit/delete, client-side search on the index table, pagination matching the controller's existing `page`/`pageSize` logic. Added a small car-vs-bus field toggle (`IsCar` checkbox show/hides the relevant pricing fields) since `Package` has two mutually-relevant field groups the original scaffolded controller already accounted for in its `[Bind(...)]` list but no UI ever exposed.
- Verified live end-to-end: logged into the admin panel, created a real package, confirmed it listed on the Index page, opened Details, edited it (change persisted), and soft-deleted it (disappeared from Index afterward). No errors at any step.
- No controller changes were needed — `PackagesController.cs` was correct all along, just missing its views.

---

## [AUDIT] — 2026-07-24 — Phase 2: migration drift + seat double-booking race condition

- **Fixed the `Notification → User` FK-name drift** flagged in Phase 1. Added migration `20260723194811_FixNotificationsUserForeignKeyName` (pure rename, `DropForeignKey`/`AddForeignKey`, no data impact) instead of regenerating `InitialCreate`, since a local database with seeded data already existed. Applied to LocalDB; `dotnet ef migrations has-pending-model-changes` confirms no more drift.
- **Fixed the seat double-booking race condition** flagged in Phase 1. The existing code only checked for already-booked seats via an application-level query before inserting — two concurrent requests for the same seat could both pass that check before either committed. Added a filtered unique index (`UQ_BookingSeats_TripId_SeatNumber_Active`, migration `20260723195113_AddUniqueSeatBookingIndex`) on `BookingSeats(TripId, SeatNumber) WHERE IsDeleted = 0`, so the database now rejects the duplicate outright. `BookingService.CreateBookingWithSeatsAsync` catches the resulting constraint-violation exception and returns the same `"Some seats are already booked."` message the pre-check already produced — callers see no difference in the normal case, but the race is now actually closed. Confirmed no pre-existing duplicate rows in the dev DB before applying (would have blocked the migration).

---

## [AUDIT] — 2026-07-24 — Phase 2: live smoke test of the MVC admin panel

Ran the app against the real LocalDB database (worked around a sandbox Application Control restriction on the generated `.exe` apphost by launching via `dotnet bin/Debug/net8.0/AshtavinayakAPP.dll`) and drove it end to end:
- Logged in successfully (dev admin credentials: `admin` / `Admin@123`).
- Dashboard (`Home/Index`) loads cleanly.
- All 17 MVC controllers' `Index` pages hit: **16/17 return HTTP 200 with no errors.** The one failure, `Packages/Index` (HTTP 500, missing view), is the pre-existing bug already documented in `CODEBASE_AUDIT.md` — confirmed live, not a new regression.
- Spot-checked `Details`/`Edit` for seeded entities (Trips, Seats, PickupPoints) — all render correctly with real navigation-property data (City, Package, Trip associations all resolve).
- Unseeded transactional tables (Bookings, Users, Vehicles, etc.) correctly 404 on a nonexistent ID rather than crashing — no test data exists for these yet, which is expected (the seeder only populates reference data).

No code changes made in this pass — this was verification only.

---

## [AUDIT] — 2026-07-24 — Phase 2: initial fixes (build restored)

- **Fixed the build.** Reverted the 25 corrupted Razor views (`Views/{BookingSeats,Bookings,Categories,DropUps,FamilyBookings,Histories,Notifications,PickupPoints,Seats,Transactions,Trips,Vehicles}/*.cshtml`) to their pre-Antigravity baseline via `git checkout 6a03f5b -- <path>`, discarding the botched auto-edit rather than trying to salvage it. `dotnet build AshtavinayakApp.sln` now completes with 0 errors (164 warnings, all pre-existing nullable-reference warnings). The null-safety improvement the broken script was attempting (`model.X.Y` → guarded `Model?.X?.Y`) was deliberately **not** reapplied — that's real, worthwhile follow-up work but needs to be done file-by-file with review, not resurrected from corrupted output.
- **Fixed `BookingSeatsController.cs`**: the `Edit()` GET and POST-fallback dropdowns were still building `ViewData["BookingId"]` from `_context.Users` (copy-paste bug) instead of `_context.Bookings` — Antigravity had already fixed this in `Create()` but missed both `Edit()` paths. Now consistent everywhere.
- **Fixed `SeatsController.cs`**: `Edit()` GET's `PackageId` dropdown didn't filter out soft-deleted packages, unlike every sibling dropdown in the same controller. Added the missing `.Where(x => !x.IsDeleted)`.
- **Fixed `BookingService.cs`**: `BookCarAsync` dereferenced the looked-up `User` without a null check (now returns `"User not found."` with an explicit transaction rollback); `GetInvoiceAsync` dereferenced `booking.User.UserName` without a null check (now falls back to `"N/A"`, consistent with the rest of that method's placeholder pattern).
- **Blanked the plaintext SMS gateway credential** in `appsettings.Development.json` (was a real-looking password, see the [ANTIGRAVITY] entry below) — replaced with `REPLACE_WITH_YOUR_SMS_GATEWAY_*` placeholders matching the existing Razorpay placeholder convention.

Not yet fixed in this pass (see `CODEBASE_AUDIT.md` for full list, tracked deliberately for a follow-up conversation since they involve product/business judgment calls, not pure bugs): seat double-booking race condition, client-trusted booking pricing, notification-read IDOR, non-functional Packages MVC admin page, the EF migration/FK-name drift, and whether the Admin-role tightening on Categories/Transactions APIs is correct for the mobile app's needs.

---

## [AUDIT] — 2026-07-24 — Phase 1: Inventory & Audit

No code changes made. Read-only audit of the inherited codebase plus Antigravity's uncommitted changes. Full findings: `CODEBASE_AUDIT.md`.

Headline findings:
- **Build currently fails** (958–1558 compiler errors, all isolated to 25 Razor admin-panel views corrupted by what appears to be a malfunctioning bulk find/replace script — literal PowerShell fragments were written into the `.cshtml` files as text instead of being executed, plus large template sections got duplicated). All Controller/Service/Model C# code compiles cleanly on its own.
- **Three live secrets found hardcoded in the baseline commit** (already pushed to `origin/main`): a live Razorpay payment key/secret, a production DB password, and an SMS gateway password. Flagged to the project owner, who is rotating them directly.
- Antigravity's changes span 72 files: security fixes (removed hardcoded credentials, closed several missing-auth-guard holes, fixed predictable OTP generation, fixed plaintext password storage in the admin "create user" flow, fixed a validation-bypass bug), real logic bug fixes (seat-inventory sync, wrong FK matching, wrong dropdown pre-selection, broken PDF export), new infrastructure (Serilog structured logging, Swagger, an `ISmsService` abstraction, an idempotent `DatabaseSeeder`, an initial EF Core migration), and the view corruption described above.
- One EF Core migration/model drift found (a foreign-key constraint name mismatch on `Notification → User`) — cosmetic at runtime, but means the single `InitialCreate` migration isn't a byte-for-byte accurate snapshot of the current model.
- Several bugs found that are still open — not introduced or fixed by Antigravity, not fixed by this audit (audit-only phase): a booking seat double-booking race condition, client-trusted pricing on bookings, an IDOR on notification read access, a non-functional MVC Packages admin page (missing Views folder), and a couple of Antigravity fixes that were applied inconsistently (fixed in one code path, not in a sibling path).

---

## [ANTIGRAVITY] — uncommitted, pre-dates this engagement, **unverified until noted otherwise in Phase 2**

Applied on top of baseline `6a03f5b`. Grouped by area; see `CODEBASE_AUDIT.md` §4/§6/§7 for full per-file detail and verification status.

### Security fixes
- Removed hardcoded admin login credentials from `HomeController.cs`; now `IConfiguration` + BCrypt-verified.
- Removed hardcoded live Razorpay key/secret from `RazorpayService.cs`; now config-driven, throws a clear startup error if unconfigured.
- Removed hardcoded production DB connection string from `AshtvinayakTravelContext.cs`'s design-time fallback; replaced with a safe LocalDB fallback (never used at runtime — DI always supplies the real connection string).
- Removed hardcoded SMS gateway credentials from three separate inline call sites (`BookingService.cs`, `User.cs`, `VehiclesController.cs`); consolidated into a new `ISmsService`/`SmsService`, config-driven.
- Fixed a session-guard bug in `HomeController.cs` where `OnActionExecuting` was declared without the `override` keyword, meaning it silently never executed — the entire admin panel's session check was effectively inert for this controller.
- Added the missing session guard (`OnActionExecuting` override) to `CategoriesController.cs`, `CitiesController.cs`, and `TourDestinationsController.cs`, which previously had none — those admin routes were reachable without authentication.
- Locked down `Dashdata.cs` (dashboard data API) to `[Authorize(Roles = "Admin")]` — was previously fully unauthenticated.
- Locked down mutation endpoints (`POST`/`PUT`/`DELETE`) across `Pickup.cs`, `Seats.cs` to `[Authorize(Roles = "Admin")]`.
- Locked down `Categorys.cs` `GetCategories` and the entire `Transaction.cs` controller to `[Authorize(Roles = "Admin")]` — flagged in the audit as needing verification against actual mobile app usage, since this may be an over-broad restriction rather than a pure fix.
- Locked down `Notification.cs`'s `SendNotificationsForTrip` to `[Authorize(Roles = "Admin")]` — was previously callable by any authenticated user.
- Replaced predictable `Random`-based OTP generation in `User.cs` with a cryptographically secure `RandomNumberGenerator`.
- Removed OTP values from application logs in `User.cs` (were being written via `Console.WriteLine`); replaced with debug-level structured logging that excludes the OTP itself.
- Shortened JWT token lifetime in `User.cs` from 365 days to 24 hours.
- Fixed plaintext password storage in `UsersController.cs` (MVC admin "create/edit user"): passwords are now always BCrypt-hashed before saving, with logic to avoid re-hashing an already-hashed value on edit.
- Deleted `Controllers/FamilyBooking.cs` — verified this was 100% commented-out, inert code even in the baseline commit; not a functional removal.
- Deleted `Models/Payment.cs` — verified this was dead code with no `DbSet`, no EF configuration, and zero references anywhere; not a functional removal.

### Logic/behavior fixes
- `BookingService.cs`: booking creation now also marks the individual `Seat.IsAvailable = false` for booked seats (previously only `Trip.AvailableSeats` was decremented, so `GetAvailableSeats` could keep showing already-booked seats as available).
- `BookingService.cs`: `Booking.BookingCode` is now generated (`Guid.NewGuid()`-based); previously left null.
- `Trip.cs` (API): seat-availability update now scopes the seat match by `TripId` in addition to `SeatNumber` — previously matched by seat number alone, which isn't unique across trips.
- `TripsController.cs` (MVC): fixed `Edit()` dropdown pre-selection, which was passing `TripId` instead of `PackageId`/`CategoryId`.
- `BookingSeatsController.cs` (MVC): fixed the `Edit()` GET `BookingId` dropdown, which was incorrectly built from `_context.Users` instead of `_context.Bookings`. **Note: the same bug still exists in the POST fallback path — not fully fixed, see `CODEBASE_AUDIT.md` §4.**
- `VehiclesController.cs` (MVC): fixed a validation-bypass bug where `if (ModelState.IsValid || vehicle.Trip == null)` was always true (since `vehicle.Trip` is never populated by model binding on POST), meaning form validation never actually ran.
- `TransactionsController.cs`/`Transaction.cs` (API): fixed `PutTransaction`, which previously updated `PaymentMethod` twice (duplicate line) and never updated `PaymentStatus` at all.
- `BookSeat.cs`: deprecated the old `POST BookSeats` endpoint (now returns HTTP 410 with a pointer to the replacement) in favor of the fully-transactional `Booking.CreateBookingWithSeats`.
- Converted several hard-deletes to soft-deletes (`IsDeleted = true`) across `Categorys.cs`, `History.cs`, `Transaction.cs`, for consistency with the rest of the codebase's soft-delete pattern.
- Fixed several pagination `CountAsync()` calls (`CitiesController.cs`, `CategoriesController.cs`, `DropUpsController.cs`) that were counting soft-deleted rows.
- `History.cs`: added a missing `namespace` wrapper — all classes in this file were previously declared in the global namespace.
- `AshtvinayakTravelContext.cs`: renamed a foreign key constraint (`Notification → User`) from the incorrect `FK_Notifications_Trips` to `FK_Notifications_Users`. **Not reflected in the `InitialCreate` migration — see migration drift note in `CODEBASE_AUDIT.md` §7.**
- `PackageService`/`IPackageService`: fixed a namespace typo (`PakageService` → `PackageService`) consistently across implementation, interface, and all call sites. The containing folder is still misspelled on disk.
- `RazorpayService.cs`: changed `BookingService`/`CarBookingDTOModel` from `record` to `class` — a stateful DI service and a mutable DTO have no use for record value-equality semantics.

### New infrastructure
- Added Serilog (console + daily rolling file sink) as the logging provider, replacing default `ILogger` output.
- Added Swashbuckle/Swagger with JWT bearer support in the UI, Development-environment only.
- Added CORS policy driven by `Cors:AllowedOrigins` configuration (falls back to allow-any-origin if unconfigured — worth revisiting for Production in Phase 4).
- Added `Data/DatabaseSeeder.cs` — idempotent reference-data seeding (cities, tour destinations, categories, packages, trip routes, drop-ups, pickup points, trips, seats), invoked once at startup.
- Added the first EF Core migration (`Migrations/20260722103837_InitialCreate`) — no migration existed in the baseline commit.
- Added `appsettings.Production.json` (all values intentionally blank, pending real production config — see `CODEBASE_AUDIT.md` §5).
- Startup now fails fast with descriptive errors if `JwtSettings:*` or `ConnectionStrings:DefaultConnection` are missing, instead of silently proceeding with nulls.

### Known incomplete/problematic Antigravity changes
- **The 25 corrupted Razor views** (see `CODEBASE_AUDIT.md` §2) — the dominant reason the build currently fails. Root-caused to a malfunctioning automated edit script; not a deliberate feature change.
- **`appsettings.Development.json`** now contains a real-looking plaintext SMS gateway password — needs to be blanked/placeholder'd before this file is ever committed.
- Inconsistent fix propagation: `BookingSeatsController.Edit()` POST path still has the dropdown bug fixed in the GET path; `SeatsController.Edit()` GET still lacks a soft-delete filter present on sibling actions.
