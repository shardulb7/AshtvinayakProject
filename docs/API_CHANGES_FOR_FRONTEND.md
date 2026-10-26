# API Changes — Frontend Integration Guide

Covers everything new or changed on the backend that the frontend team needs to know about.

**Base URL:** `https://ashtavinayak-api.azurewebsites.net/api`
**Auth:** `Authorization: Bearer <token>` on every authenticated request.

---

## 1. Agent Documents

Agent registration uploads 3 documents (Aadhaar, Shop Act License, Udyam Certificate). These are **not** publicly accessible by URL — stored in a private folder outside web root. Retrieve only via Admin Panel (`GET /Agents/DownloadDocument/{agentId}?documentType=aadhaar|shopact|udyam`).

---

## 2. Agent API

### 2.1 Register
`POST /api/Agent/Register` — `multipart/form-data`

| Field | Type | Required |
|---|---|---|
| FullName | string | yes |
| BusinessName | string | yes |
| MobileNumber | string | yes |
| Email | string | yes |
| Address | string | yes |
| Password | string | yes — min 8 chars, 1 letter + 1 digit |
| ConfirmPassword | string | yes — must match Password |
| AadhaarDocument | file | yes — PDF/JPG/PNG max 5MB |
| ShopActLicense | file | yes — PDF/JPG/PNG max 5MB |
| UdyamCertificate | file | yes — PDF/JPG/PNG max 5MB |

Success: `{ "message": "Registration submitted. Your account is pending admin approval.", "data": { "agentId": 14 } }`

New agent **cannot log in** until admin approves.

### 2.2 Login
`POST /api/Agent/Login`

Request:
```json
{ "mobileOrEmail": "9876543210", "password": "MyPassword1" }
```

Success:
```json
{
  "message": "Login successful.",
  "data": {
    "token": "eyJhbGciOi...",
    "agent": { "agentId": 14, "fullName": "Ramesh Kumar", "businessName": "Kumar Travels", "email": "ramesh@example.com", "mobileNumber": "9876543210" }
  }
}
```

Failure (401): `{ "message": "Your registration is still pending admin approval." }`

Rate-limited: 5 attempts per 15 minutes. HTTP 429 if exceeded.
Token valid for **24 hours**. No refresh endpoint — re-login required after expiry.

### 2.3 Resolve / Create Customer
`POST /api/Agent/ResolveCustomer` — Agent token required

Use before booking on behalf of a walk-in customer. Looks up by phone; creates if not found.

```json
{ "customerName": "Suresh Patil", "customerPhone": "9123456789", "customerEmail": "suresh@example.com" }
```
(customerEmail optional)

Success: `{ "message": "Existing customer found.", "userId": 231, "customerName": "Suresh Patil" }`

### 2.4 Profile
`GET /api/Agent/Profile` — Agent token required

```json
{ "agentId": 14, "fullName": "Ramesh Kumar", "businessName": "Kumar Travels", "email": "ramesh@example.com", "mobileNumber": "9876543210", "address": "123 MG Road, Pune", "approvalStatus": "Approved", "isActive": true }
```

### 2.5 My Bookings (NEW)
`GET /api/Agent/MyBookings` — Agent token required

Returns all bookings finalized by the logged-in agent. Auto-filtered by JWT — no userId param needed.

> IMPORTANT: Use this for the agent dashboard — NOT FamilyBookingHistory or HistoryByUser.

```json
{
  "message": "Bookings fetched.",
  "count": 2,
  "data": [
    {
      "bookingId": 18,
      "bookingDate": "2026-09-20T19:24:00Z",
      "customerName": "Parth",
      "customerContact": "9860646215",
      "tourName": "Ashtavinayak Darshan 2N/3D (Bus)",
      "tourDate": "2026-09-22T00:00:00Z",
      "totalSeats": 2,
      "amountCollected": 6000.00,
      "commissionPercentage": 5.00,
      "commissionAmount": 300.00,
      "status": "Confirmed"
    }
  ]
}
```

Field descriptions:
- customerName — Customer name
- customerContact — Customer phone number
- tourName — Package/tour name
- tourDate — Trip date
- totalSeats — Adults + children with seat + children without seat
- amountCollected — Total booking amount (INR)
- commissionPercentage — Agent commission % on this booking
- commissionAmount — Exact INR commission earned

---

## 3. Booking — Agent Support

No new request fields added. Whether a booking is agent-made is determined by the token used:
- Customer JWT — normal booking, response unchanged
- Agent JWT — server auto-detects agent, applies commission

Pass userId in the body = the customer userId (from ResolveCustomer), not the agent.

Agent booking response:
```json
{
  "message": "Booking and seats saved successfully.",
  "data": { "bookingId": 5022, "totalPayment": 9000, "commissionPercentage": 10, "commissionAmount": 900, "agentPayable": 8100 }
}
```
agentPayable = totalPayment - commissionAmount

---

## 4. Package API (UPDATED)

### 4.1 Get packages by category — new destinationId parameter

```
GET /api/Package/GetPackageByCateGoryId?id={categoryId}&isCarType={bool}&destinationId={id}
```

| Parameter | Required | Notes |
|---|---|---|
| id | yes | Category ID. Ignored when isCarType=true |
| isCarType | no | true = car packages, false = bus packages |
| destinationId | no | NEW — filter car packages by destination |

Examples:
```
GET /api/Package/GetPackageByCateGoryId?id=2&isCarType=true                    -> all car packages
GET /api/Package/GetPackageByCateGoryId?id=2&isCarType=true&destinationId=1    -> car packages for destination 1
GET /api/Package/GetPackageByCateGoryId?id=1&isCarType=false                   -> bus packages (cat 1)
```

### 4.2 Sharing charges — new fields on package (NEW)

Bus packages now include room sharing charges per person:

```json
{
  "packageId": 1,
  "packageName": "Ashtavinayak Darshan 2N/3D (Bus)",
  "adultPrice": 3500,
  "singleSharingChargePerPerson": 800,
  "doubleSharingChargePerPerson": 500,
  "tripleSharingChargePerPerson": 300,
  "isCar": false
}
```

- singleSharingChargePerPerson — Extra per person for single room
- doubleSharingChargePerPerson — Extra per person for double sharing
- tripleSharingChargePerPerson — Extra per person for triple sharing

Values are null if admin has not set them. Not applicable when isCar = true.

Booking form dropdown options:
- Default/Group sharing — no extra charge
- Single Sharing — add singleSharingChargePerPerson x persons
- Double Sharing — add doubleSharingChargePerPerson x persons
- Triple Sharing — add tripleSharingChargePerPerson x persons

### 4.3 Package price endpoint

```
GET /api/Package/GetPackagePrice/{cityId}/{categoryId}/{packageId}
```

For car packages, use categoryId 7 (not the display ID):
```
GET /api/Package/GetPackagePrice/1/7/9
```

---

## 5. Booking History — New Fields (UPDATED)

Both history endpoints now return 3 extra fields per booking.

### 5.1 Car booking history
```
GET /api/Booking/FamilyBookingHistory/{userId}
```

New fields added:
```json
{
  "bookingId": 3,
  "customerName": "Parth",
  "customerContact": "9860646215",
  "commissionAmount": null,
  "carType": "Swift",
  "totalPayment": 1.00
}
```

### 5.2 Bus booking history
```
GET /api/Booking/HistoryByUser/{userId}
```

New fields added:
```json
{
  "bookingId": 18,
  "customerName": "Parth",
  "customerContact": "9860646215",
  "commissionAmount": 300.00,
  "tripName": "Ashtavinayak Darshan",
  "totalPayment": 6000.00
}
```

commissionAmount is null when no agent was involved in the booking.

---

## 6. IMPORTANT — Agent vs Customer: Use Correct Endpoints

| Use Case | API | Token |
|---|---|---|
| Customer own car bookings | GET /api/Booking/FamilyBookingHistory/{userId} | Customer JWT |
| Customer own bus bookings | GET /api/Booking/HistoryByUser/{userId} | Customer JWT |
| Agent finalized bookings | GET /api/Agent/MyBookings | Agent JWT |

The agent AgentId and their UserId (as a customer in the Users table) are separate records.
Calling FamilyBookingHistory with an agent userId returns their personal customer bookings, NOT their agent bookings.
Always use /api/Agent/MyBookings for the agent dashboard.

---

## 7. General Behavior

- Error responses — clean generic message on server errors: `{ "message": "An unexpected error occurred. Please try again." }`
- Booking price — always server-calculated; TotalPayment in request is ignored for pricing
- Health checks — GET /health/live and GET /health/ready (no auth required)
- JWT expiry — 24 hours, no refresh endpoint; redirect to login on 401

---

## 8. Database Changes (for awareness)

- New tables: Agents, CommissionSettings
- New columns on Bookings: AgentId, CommissionPercentage, CommissionAmount (nullable)
- New columns on Packages: SingleSharingChargePerPerson, DoubleSharingChargePerPerson, TripleSharingChargePerPerson, DestinationId
- Unique constraint on seat bookings — same seat on same trip cannot be double-booked

---

## 9. Known Limitations

| Item | Status |
|---|---|
| Car bookings via agent with commission | Not yet built |
| Agent forgot password / reset | Not yet built |
| JWT refresh token | Not built — re-login after 24h |
| Server-side payment webhook verification | Not built |
