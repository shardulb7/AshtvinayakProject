# API Changes — Frontend Integration Guide

Covers everything new or changed on the backend that the frontend team needs to know about: the new Agent module, changes to the existing booking flow, and general behavior changes across the API.

Base URL: `http://<host>/api`
Auth: Bearer JWT in the `Authorization` header (`Authorization: Bearer <token>`) unless noted otherwise.

---

## 1. Agent Documents — where they live and how to access them

Agent registration uploads 3 documents (Aadhaar, Shop Act License, Udyam Certificate). These are **not** stored in `wwwroot` and are **not** publicly accessible by URL — they're saved to a private folder on the server, outside the web root, with randomly generated filenames. There is no direct link you can put in an `<img src>` or similar.

The only way to retrieve a document is through an authenticated admin action in the Admin Panel (`GET /Agents/DownloadDocument/{agentId}?documentType=aadhaar|shopact|udyam`), which checks the admin's session before streaming the file back. This is intentional — these are private KYC documents, so nothing about their storage or retrieval is public-facing. The frontend/customer/agent-facing apps never need to construct a URL to these files directly.

---

## 2. New: Agent API

### 2.1 Register an agent
`POST /api/Agent/Register`
Content-Type: `multipart/form-data` (because of the file uploads)

| Field | Type | Notes |
|---|---|---|
| FullName | string | required |
| BusinessName | string | required |
| MobileNumber | string | required |
| Email | string | required, valid email |
| Address | string | required |
| Password | string | required — min 8 chars, at least 1 letter + 1 digit |
| ConfirmPassword | string | required — must match Password |
| AadhaarDocument | file | required — PDF/JPG/PNG, max 5MB |
| ShopActLicense | file | required — PDF/JPG/PNG, max 5MB |
| UdyamCertificate | file | required — PDF/JPG/PNG, max 5MB |

**Success (200):**
```json
{
  "message": "Registration submitted. Your account is pending admin approval.",
  "data": { "agentId": 14 }
}
```

**Failure (400)** — validation error, duplicate mobile/email, wrong file type, oversized file, etc.:
```json
{ "message": "This mobile number is already registered." }
```

Note: a newly registered agent **cannot log in immediately** — an admin must review and approve the account first (see section 4).

### 2.2 Agent login
`POST /api/Agent/Login`
Content-Type: `application/json`

```json
{
  "mobileOrEmail": "9876543210",
  "password": "MyPassword1"
}
```

**Success (200):**
```json
{
  "message": "Login successful.",
  "data": {
    "token": "eyJhbGciOi...",
    "agent": {
      "agentId": 14,
      "fullName": "Ramesh Kumar",
      "businessName": "Kumar Travels",
      "email": "ramesh@example.com",
      "mobileNumber": "9876543210"
    }
  }
}
```

**Failure (401)** — wrong credentials, account still pending, rejected, or deactivated:
```json
{ "message": "Your registration is still pending admin approval." }
```

This endpoint is **rate-limited**: 5 attempts per 15 minutes per client. Exceeding it returns **HTTP 429** with no body — handle this distinctly from a normal 401 (e.g. "Too many attempts, please try again later" rather than "wrong password").

The returned `token` is a JWT valid for 24 hours. Use it as `Authorization: Bearer <token>` on every subsequent agent-only request.

### 2.3 Resolve or create a customer
`POST /api/Agent/ResolveCustomer` — **requires Agent token**

When an agent books on behalf of a walk-in customer, use this first to get a `userId` to pass into the booking call. It looks the customer up by phone number; if not found, it creates a new customer record automatically.

```json
{
  "customerName": "Suresh Patil",
  "customerPhone": "9123456789",
  "customerEmail": "suresh@example.com"
}
```
(`customerEmail` is optional — omit it if the customer didn't provide one.)

**Success (200):**
```json
{
  "message": "Existing customer found.",
  "userId": 231,
  "customerName": "Suresh Patil"
}
```
(`message` will be `"Customer created."` for a brand-new customer — same response shape either way.)

**Why this exists, and why it's a separate call:** every booking requires a real `userId` — `Bookings.UserId` is a required foreign key into `Users`. But agents mostly book for walk-in customers who've never used the app and have no account, so something has to get a valid `userId` for them before the booking call. This endpoint does that: idempotent lookup-or-create by phone number, so a repeat customer (of this agent, or someone who separately registered via the app) never gets duplicated.

It's a separate call rather than folded into `CreateBookingWithSeats` itself so the agent flow can show a confirmation step — e.g. "Existing customer found: Suresh Patil" vs. "New customer will be created" — before the agent commits to the booking. If your UI doesn't need that confirmation step, this could be simplified into a single call later; flag it if that's preferred and it can be revisited.

### 2.4 Agent profile
`GET /api/Agent/Profile` — **requires Agent token**

**Success (200):**
```json
{
  "agentId": 14,
  "fullName": "Ramesh Kumar",
  "businessName": "Kumar Travels",
  "email": "ramesh@example.com",
  "mobileNumber": "9876543210",
  "address": "123 MG Road, Pune",
  "approvalStatus": "Approved",
  "isActive": true
}
```

---

## 3. Changed: Booking API now supports agent-made bookings

**No new request fields were added to the booking endpoint.** Instead, whether a booking is an "agent booking" is determined entirely by **which token** is used to call it:

- Call `POST /api/Booking/CreateBookingWithSeats` with a **customer's** JWT → behaves exactly as before, response unchanged.
- Call the same endpoint with an **agent's** JWT (from section 2.2) → the server automatically detects the agent, looks up their commission rate, and the response includes extra commission fields. You still pass `userId` in the request body as normal — that's the *customer* being booked for (use the `userId` you got from ResolveCustomer), not the agent.

**Response when booked by a customer (unchanged):**
```json
{
  "message": "Booking and seats saved successfully.",
  "data": { "bookingId": 5021 }
}
```

**Response when booked by an agent (new fields):**
```json
{
  "message": "Booking and seats saved successfully.",
  "data": {
    "bookingId": 5022,
    "totalPayment": 9000,
    "commissionPercentage": 10,
    "commissionAmount": 900,
    "agentPayable": 8100
  }
}
```
`agentPayable` is `totalPayment - commissionAmount` — the amount the agent should actually collect/remit.

Commission rate is looked up per-agent (an admin-configurable override) and falls back to a global default if the agent has no override set. Whatever rate applied at the moment of booking is permanently recorded on that booking — a later change to commission rates never changes past bookings.

---

## 4. Agent approval — admin-only, no public API

There is currently no API for agents or the frontend app to check their own approval status other than attempting login and reading the message (see 2.2 failure responses) or calling `/api/Agent/Profile` once they do have a valid token from a prior approved login. Approval itself happens only through the Admin Panel (not something the frontend integrates with).

---

## 5. General behavior changes across the whole API

**Error responses no longer include technical detail.** Previously, some endpoints returned the raw exception message on failure (e.g. a database error string). All endpoints now return a clean, generic message on unexpected server errors, e.g.:
```json
{ "message": "An unexpected error occurred. Please try again." }
```
Validation errors (bad input) still return specific, useful messages as before — only unexpected server-side failures were changed. If you were ever parsing/displaying the old raw error text anywhere, that text will now be generic; don't rely on its exact wording.

**Booking price is always server-calculated.** `TotalPayment` sent in a booking request is no longer used to set the actual price — the server calculates it from the trip's package rates regardless of what's submitted. You can still send it (kept for compatibility) but it's ignored for pricing purposes; only `Advance` (how much the customer pays upfront) is still taken from the request, and it's rejected if it exceeds the real computed total.

**OTP login, Development only:** `POST /api/User/LoginByOTP` now includes a `devOnlyOtp` field in its response, but **only** when the API is running in the Development environment. It will never appear in a staging/production response — don't build any logic around it being present.

**Health check endpoints** (not authenticated, informational — useful if the frontend/ops wants a quick "is the API up" check): `GET /health/live` and `GET /health/ready`.

---

## 6. Database changes (for awareness — not directly relevant to frontend calls, but good context)

- New tables: `Agents`, `CommissionSettings`
- New columns on `Bookings`: `AgentId`, `CommissionPercentage`, `CommissionAmount` (all nullable — `null` for ordinary customer bookings)
- A uniqueness rule was added at the database level so the same seat on the same trip can never be double-booked, even under simultaneous requests
- A foreign key naming fix on `Notifications` (internal only, no API impact)

None of this requires any frontend changes by itself — it's covered here so the team has the full picture of what changed underneath the API.

---

## 7. Before deployment — what's still needed

Things the frontend team should know about or provide before this goes live:

**We need your production domain(s) for CORS.** The API restricts which domains can call it from a browser. Send us the exact production URL(s) the frontend will be served from (e.g. `https://app.example.com`) so they can be added to the allowed list — without this, browser-based calls from production will be blocked even though everything works fine from `localhost` today.

**No token refresh — plan for full re-login.** Both customer and agent JWTs expire after 24 hours, and there is currently no refresh-token endpoint. When a token expires, calls will start returning 401 — the app needs to catch that and send the user back through login (OTP for customers, password for agents), not silently retry.

**No server-side payment confirmation yet.** A booking's paid status is currently set from what the client/Razorpay callback reports, not confirmed independently by the server via a webhook. Functionally this doesn't change anything about how the frontend calls `CreateBookingWithSeats`/`UpdatePayment` today, but don't build any assumption that `PaymentStatus` is independently verified — flagging so the team has full visibility, this is a backend/business decision being tracked separately, not something the frontend needs to change now.

**Family/car bookings (`BookCar`) don't have agent support.** Only `CreateBookingWithSeats` (regular seat bookings) was extended with agent/commission handling in this phase. If agent-assisted car bookings are needed, that's follow-up work, not yet available.

**Agent password reset — not built yet.** There's a registration flow and a login flow, but no "forgot password" for agents. If the frontend needs this for launch, flag it — currently an agent who forgets their password has no self-service recovery path.

**Confirm before launch:** whether any of the above are launch-blockers for the frontend's scope, so they can be prioritized ahead of deployment rather than discovered afterward.
