# Ashtavinayak Travel API — Frontend Developer Reference

**Base URL:** `https://ashtavinayak-api.azurewebsites.net`  
**Auth:** All endpoints require a JWT Bearer token (except User Login/Register).  
Include header: `Authorization: Bearer <token>`

---

## 🆕 Recent Changes (October 2026)

### 1. `POST /api/Booking/BookCar` — Updated Request Body
New fields added to `CarBookingDTOModel`:

| Field | Type | Required | Notes |
|---|---|---|---|
| `adults` | `int?` | No | Number of adults |
| `childwithseat` | `int?` | No | Children with seat |
| `childwithoutseat` | `int?` | No | Children without seat |
| `bookingDate` | `datetime?` | No | Date booking was made |
| `pickUpPointName` | `string?` | No | Pickup point name (text) |

> **Note:** `pickupPointId` (int FK) was already present. `pickUpPointName` is a free-text snapshot.

**Full Request Body:**
```json
{
  "userId": 5,
  "packageId": 3,
  "carType": "Innova",
  "date": "2026-10-15T00:00:00",
  "time": "2026-10-15T14:30:00",
  "pickupPointId": null,
  "pickUpPointName": "Swargate",
  "droppoint": "Shirdi",
  "roomType": null,
  "status": "Confirmed",
  "totalPayment": 10000.00,
  "advance": 4000.00,
  "adults": 2,
  "childwithseat": 1,
  "childwithoutseat": 0,
  "bookingDate": "2026-10-03T00:00:00",
  "transaction": {
    "amount": 4000,
    "paymentMethod": "Cash",
    "transactionReference": "TXN001"
  }
}
```

**Success Response `200`:**
```json
{ "message": "Car booked successfully.", "data": { ... } }
```

**Error Response `400`:**
```json
{ "message": "UserId and CarType are required fields.", "data": null }
```

---

### 2. `POST /api/Booking/CreateBookingWithSeats` — No Change to Contract

**Request Body (`BookingRequestDto`):**
```json
{
  "userId": 5,
  "tripId": 12,
  "pickupPointId": 3,
  "droppoint": "Shirdi",
  "droppointId": 7,
  "bookingDate": "2026-10-03T00:00:00",
  "roomType": "Double",
  "status": "Confirmed",
  "totalPayment": 15000.00,
  "advance": 5000.00,
  "seatNumbers": ["A1", "A2", "B3"],
  "adults": 2,
  "childwithseat": 1,
  "childwithoutseat": 0,
  "categoryId": 1,
  "cityId": 2
}
```

---

## 📋 Full API Reference

### 🔐 Authentication

#### `POST /api/User/LoginByOTP`
Request OTP on mobile number.
```json
{ "mobileNo": "9876543210" }
```

#### `POST /api/User/VerifyOTP`
Verify OTP and receive JWT.
```json
{ "mobileNo": "9876543210", "otp": "123456" }
```
Returns: `{ "token": "<jwt>", "userId": 5, "userName": "Parth" }`

#### `POST /api/User/Register`
```json
{ "userName": "Parth", "mobileNo": "9876543210", "email": "parth@example.com" }
```

#### `POST /api/Agent/Login`
Agent login — JWT will contain `Role=Agent`.

#### `POST /api/Agent/Register`
Register a new agent.

#### `GET /api/Agent/Profile`
Get logged-in agent profile.

#### `POST /api/Agent/ResolveCustomer`
Resolve a customer by mobile number for agent-assisted bookings.

---

### 📦 Packages & Categories

#### `GET /api/Package/GetPackages/{cityId}/{categoryId}`
All packages for a city+category.

#### `GET /api/Package/GetPackageByCateGoryId?categoryId={id}`
Packages by category.

#### `GET /api/Package/GetPackagePrice/{cityId}/{categoryId}/{packageId}`
Pricing breakdown for a specific package.

#### `GET /api/Categorys/GetCategories`
All active categories.

#### `GET /api/City`
All cities.

---

### 🚌 Bus Bookings

#### `POST /api/Booking/CreateBookingWithSeats`
*(See full body above)*

#### `GET /api/Booking/HistoryByUser/{userId}`
All bus booking history for a user.

**Response `200`:**
```json
{
  "message": "Bookings found.",
  "data": [{
    "bookingId": 10,
    "bookingDate": "2026-10-03T00:00:00",
    "status": "Confirmed",
    "totalPayment": 15000.00,
    "advance": 5000.00,
    "tripName": "Pune-Shirdi",
    "tripDate": "2026-10-15T00:00:00",
    "pickupPointName": "Swargate",
    "droppoint": "Shirdi",
    "roomType": "Double",
    "seatNumbers": ["A1", "A2"],
    "adults": 2,
    "childwithseat": 1,
    "childwithoutseat": 0,
    "bookingCode": "ASHBK001"
  }]
}
```

#### `GET /api/Booking/DownLoadInvoice/{bookingId}`
Download invoice details for a bus booking.

#### `POST /api/Booking/UpdatePayment`
Add a payment transaction to an existing booking.
```json
{
  "bookingId": 10,
  "amount": 5000,
  "paymentMethod": "UPI",
  "transactionReference": "UPI123"
}
```

---

### 🚗 Car Bookings

#### `POST /api/Booking/BookCar`
*(See full body in Recent Changes section above)*

#### `GET /api/Booking/FamilyBookingHistory/{userId}`
All car booking history for a user.

**Response `200`:**
```json
{
  "message": "Bookings found.",
  "data": [{
    "bookingId": 5,
    "bookingDate": "2026-10-03T00:00:00",
    "status": "Confirmed",
    "totalPayment": 10000.00,
    "advance": 4000.00,
    "carType": "Innova",
    "packageName": "ASHPNC-S1",
    "tripDate": "2026-10-15T00:00:00",
    "pickupTime": "14:30:00",
    "pickupPointName": "Swargate",
    "adults": 2,
    "childwithseat": 1,
    "childwithoutseat": 0
  }]
}
```

---

### 🪑 Seats & Trips

#### `GET /api/Trip`
All trips.

#### `GET /api/Trip/TripsByPackage/{packageId}`
Trips filtered by package.

#### `GET /api/Trip/Available/{tripId}`
Available seat count for a trip.

#### `GET /api/Trip/BookingSeats/{tripId}`
All booked seats for a trip.

#### `PUT /api/Trip/UpdateAvailability`
Update seat availability for a trip.

#### `GET /api/TripRoute/{cityId}/{packageId}`
Get pickup + drop route for a city+package combination.

#### `GET /api/Pickup/city/{cityId}`
All pickup points for a city.

#### `GET /api/Package/GetDropPointByCityIdAsync/{cityId}`
All drop points for a city.

#### `GET /api/BookingSeat/ByTrip/{tripId}`
All booking seat records for a trip.

#### `GET /api/BookingSeat/GetAvailableSeats/{tripId}`
Available seats by trip.

#### `POST /api/BookingSeat/BookSeats`
Book specific seats (alternative endpoint).

---

### 💰 Transactions

#### `GET /api/Transaction?page=1&pageSize=20`
All transactions (paginated).

#### `POST /api/Transaction`
Create a new transaction record.

#### `GET /api/Transaction/{id}`
Get single transaction.

#### `PUT /api/Transaction/{id}`
Update a transaction.

#### `DELETE /api/Transaction/{id}`
Delete a transaction.

---

### 📊 Dashboard

#### `GET /api/Dashdata`
Summary stats (total bookings, revenue, etc.).

#### `GET /api/Dashdata/RecentBookings`
Recent bookings list.

#### `GET /api/Dashdata/RecentPackages`
Recently added packages.

#### `GET /api/Dashdata/RecentTrips`
Upcoming trips.

---

### 🗺️ Tour Destinations

#### `GET /api/TourDestinationApi/GetTourDestinationList`
All tour destinations for homepage/listing display.

---

### 📞 Notifications

#### `GET /api/Notification/GetUserNotifications/{userId}`
All notifications for a user.

#### `POST /api/Notification/SendNotificationsForTrip/{tripId}`
Push notifications to all users booked on a trip.

---

### 🚘 Vehicles

#### `GET /api/Vehicle`
All vehicles.

#### `GET /api/Vehicle/{vehicleId}`
Vehicle by ID.

---

### 💳 Razorpay

#### `POST /api/RazorPay/CreateRazorPayOrder`
Create a Razorpay payment order.
```json
{ "amount": 5000, "bookingId": 10 }
```
Returns Razorpay order object with `orderId`, `amount`, `currency`, `key`.

---

## 🔄 Data Models (Key Fields)

### Booking (Bus Booking)
| Field | Type | Notes |
|---|---|---|
| `bookingId` | int | Primary key |
| `userId` | int? | FK → Users |
| `tripId` | int? | FK → Trips |
| `pickupPointId` | int? | FK → PickupPoints |
| `pickUpPointName` | string? | Pickup name snapshot |
| `bookingDate` | datetime? | When booking was made |
| `status` | string? | `Confirmed` / `Pending` / `Cancelled` |
| `totalPayment` | decimal? | Total fare |
| `advance` | decimal? | Advance paid |
| `droppoint` | string? | Drop point name |
| `droppointId` | int? | FK → DropUps |
| `roomType` | string? | `Single` / `Double` / `Triple` |
| `bookingCode` | string? | Unique booking reference |
| `seatNumbers` | string[] | e.g. `["A1","A2","B3"]` |
| `adults` | int? | Adult count (from BookingSeat) |
| `childwithseat` | int? | Children with seat |
| `childwithoutseat` | int? | Children without seat |

### Car Booking (FamilyBooking)
| Field | Type | Notes |
|---|---|---|
| `familyId` | int | Primary key |
| `userId` | int | FK → Users |
| `packageId` | int | FK → Packages |
| `carType` | string | e.g. "Innova", "Ertiga" |
| `date` | datetime | Trip travel date |
| `time` | datetime | Pickup time |
| `bookingDate` | datetime? | When booking was made |
| `pickupPoint` | string? | 🆕 Pickup point (text) |
| `totalPayment` | decimal? | 🆕 Total fare |
| `advance` | decimal? | 🆕 Advance paid |
| `adults` | int? | Number of adults |
| `childwithseat` | int? | Children with seat |
| `childwithoutseat` | int? | Children without seat |

> Fields marked 🆕 are **new columns added in October 2026**.

---

## ⚠️ Important Notes

1. **JWT Auth**: Tokens expire — handle `401` by redirecting to OTP login.
2. **Seat Number Format**: Send as array `["A1","A2"]`. Display as `A1, A2`.
3. **Pickup/Drop by City**: Use `GET /api/TripRoute/{cityId}/{packageId}` — pickup and drop points are configured per city in the admin panel.
4. **Car Booking Price**: The server recomputes `totalPayment` from the package rate — client-submitted price is overridden server-side.
5. **Agent Role**: JWT with `Role=Agent` triggers automatic commission calculation server-side.
6. **Swagger UI**: `https://ashtavinayak-api.azurewebsites.net/swagger`
