# Conference Room Booking API

REST API for conference room booking using ASP.NET Core Minimal API and Entity Framework Core.

## Technologies

* .NET 9
* ASP.NET Core Minimal API
* Entity Framework Core 9
* SQL Server
* Swagger / OpenAPI

## Database

The database uses **EF Core Code First**.

Main tables:

* `Rooms` — conference rooms and their base hourly rates.
* `Services` — available additional services.
* `Bookings` — room bookings with start/end time and final price.
* `BookingServices` — selected services for a booking with the price snapshot at booking time.
* `RoomServices` — automatically generated EF Core join table for the `Room` ↔ `Service` many-to-many relationship.

Relationships:

* `Room` → `Bookings`: one-to-many.
* `Room` ↔ `Service`: many-to-many via automatically generated `RoomServices`.
* `Booking` ↔ `Service`: many-to-many via `BookingService`, because the service price must be stored for historical accuracy.

## Concurrency

Booking creation uses a database transaction and a row-level lock on the selected room.

The room is locked before checking existing bookings and creating a new one. This prevents two concurrent requests from successfully booking the same room for overlapping time periods.

## Pricing

The hourly rate depends on the booking time:

* 06:00–09:00 — −10%
* 09:00–12:00 — standard rate
* 12:00–14:00 — +15%
* 14:00–18:00 — standard rate
* 18:00–23:00 — −20%

The final price is stored in `Booking.TotalPrice`, while selected service prices are stored in `BookingService.Price`.

## API

### Rooms

**GET `/api/rooms`**

Returns all rooms. Mainly provided for convenient testing and obtaining IDs.

**POST `/api/rooms`**

```json
{
  "name": "string",
  "capacity": 0,
  "hourlyRate": 0,
  "serviceIds": [
    "guid"
  ]
}
```

**PUT `/api/rooms/{id}`**

```json
{
  "name": "string",
  "capacity": 0,
  "hourlyRate": 0,
  "serviceIds": [
    "guid"
  ]
}
```

**DELETE `/api/rooms/{id}`**

Deletes a room using soft delete.

**GET `/api/rooms/available`**

Query parameters:

```text
startTime
endTime
capacity
```

Request:

```json
{
  "startTime": "2026-09-06T13:00:00",
  "endTime": "2026-09-06T17:00:00",
  "capacity": 10
}
```

Returns rooms available for the specified period and capacity.

### Services

**GET `/api/services`**

Returns all available services. Mainly provided for convenient testing and obtaining service IDs.

### Bookings

**GET `/api/bookings`**

Returns all bookings. Mainly provided for convenient testing.

**POST `/api/bookings`**

```json
{
  "roomId": "guid",
  "startTime": "2026-09-06T13:00:00",
  "endTime": "2026-09-06T17:00:00",
  "serviceIds": [
    "guid"
  ]
}
```

## Initial Data

The database is seeded with:

* Room A — capacity 50, 2000 UAH/hour
* Room B — capacity 100, 3500 UAH/hour
* Room C — capacity 30, 1500 UAH/hour
* Projector — 500 UAH
* Wi-Fi — 300 UAH
* Sound — 700 UAH


## Run locally

### Clone the repository

```bash
git clone <repository-url>
cd conference-room-booking-api/ConferenceRoomBooking
```

Run the following commands from the `ConferenceRoomBooking` directory, where the `.csproj` file is located.

### Requirements

* .NET 9 SDK
* SQL Server

The default connection string is configured in `appsettings.json` for a local SQL Server instance.

### Run

```bash
dotnet run
```

Database migrations and initial data seeding are applied automatically on startup.

Swagger UI: `http://localhost:5258/swagger/index.html`

