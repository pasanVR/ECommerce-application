# Secure E-commerce Order Processing API

A secure, layered ASP.NET Core (.NET 9) backend for an e-commerce platform: JWT
authentication with Admin/Customer roles, product management, order processing
with stock validation, and a guarded order lifecycle.

Built with **Clean Architecture**, **EF Core (SQLite)**, **Serilog**, global
exception handling, and **Swagger** for interactive testing.

---

## Table of contents
1. [Tech stack](#tech-stack)
2. [Architecture](#architecture)
3. [Prerequisites](#prerequisites)
4. [Setup & run](#setup--run)
5. [Connection string guidance](#connection-string-guidance)
6. [Database & migration commands](#database--migration-commands)
7. [Default admin & auth flow](#default-admin--auth-flow)
8. [API reference](#api-reference)
9. [Order lifecycle](#order-lifecycle)
10. [Architecture decisions & trade-offs](#architecture-decisions--trade-offs)

---

## Tech stack

| Concern            | Choice                                              |
|--------------------|-----------------------------------------------------|
| Framework          | ASP.NET Core Web API, .NET 9                         |
| Persistence        | EF Core 9 + SQL Server (code-first, migrations)      |
| Auth               | JWT Bearer, role-based authorization (Admin/Customer)|
| Password hashing   | BCrypt (`BCrypt.Net-Next`, work factor 12)           |
| Logging            | Serilog (console + rolling file)                     |
| API docs           | Swagger / OpenAPI (with JWT "Authorize" button)      |
| Minimal UI         | Razor Pages + vanilla JS (separate Login & console)  |
| Error handling     | Global exception-handling middleware                 |

---

## Architecture

Clean Architecture with dependencies pointing **inward** (Domain has no
dependencies; the API depends on everything but is depended on by nothing):

```
src/
├── Ecommerce.Domain          # Entities, enums, lifecycle rules, domain exceptions (no external deps)
├── Ecommerce.Application      # DTOs, service interfaces + business logic, IApplicationDbContext
├── Ecommerce.Infrastructure   # EF Core DbContext, migrations, JWT, BCrypt, current-user, seeding
└── Ecommerce.Api             # Controllers, middleware, Program.cs, Swagger, DI composition root
```

**Entities & relationships**

- `User (1) ──< Order (1) ──< OrderItem >── (1) Product`
- `User`  — Id, FullName, Email *(unique)*, PasswordHash, Role, CreatedDate
- `Product` — Id, Name, Description, Price, Stock, CreatedDate
- `Order` — Id, CustomerId (FK), Status, TotalAmount, CreatedDate
- `OrderItem` — Id, OrderId (FK), ProductId (FK), ProductName + UnitPrice *(price snapshot)*, Quantity

Order-status transitions and stock guards live on the **domain entities**
(`Order.MarkAsPaid/Ship/Deliver/Cancel`, `Product.ReduceStock`), so the rules
cannot be bypassed by application code.

---

## Prerequisites

- [.NET SDK 9.0+](https://dotnet.microsoft.com/download)
- **SQL Server** — any of:
  - **SQL Server LocalDB** (ships with Visual Studio / the SQL Server Express
    installer) — the default connection string targets `(localdb)\MSSQLLocalDB`.
  - SQL Server Express / Developer / a full instance — just update the connection string.
  - Docker: `docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Your_password123" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest`
- EF Core CLI tool (only needed if you want to add/modify migrations):
  ```bash
  dotnet tool install --global dotnet-ef --version 9.0.0
  ```

The database (`EcommerceDb`) and its schema are created automatically on first run.

---

## Setup & run

```bash
# 1. Restore & build
dotnet build

# 2. Run the API (from the repo root)
dotnet run --project src/Ecommerce.Api
```

On startup the app **automatically applies migrations** and **seeds a default
admin** (see below). Two entry points are available:

- **Minimal UI (Razor Pages):**
  - `http://localhost:<port>/Login` — dedicated sign-in page (Login / Register tabs).
  - `http://localhost:<port>/` — storefront/admin console (product list + admin
    create/delete, place orders, admin order-lifecycle buttons). Redirects to
    `/Login` when not authenticated; logout returns there.
- **Swagger UI:** `http://localhost:<port>/swagger`
- The exact port is printed in the console, or force one:
  ```bash
  dotnet run --project src/Ecommerce.Api --urls http://localhost:5080
  ```

> The `EcommerceDb` database is created on the configured SQL Server instance on first run.

---

## Connection string guidance

Configured in `src/Ecommerce.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=EcommerceDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

- **LocalDB (default):** `Server=(localdb)\MSSQLLocalDB` uses Windows auth — no
  username/password needed. Ensure the instance is running: `sqllocaldb start MSSQLLocalDB`.
- **SQL Server Express / named instance:**
  `Server=.\SQLEXPRESS;Database=EcommerceDb;Trusted_Connection=True;TrustServerCertificate=True`
- **SQL auth (e.g. Docker / remote):**
  `Server=localhost,1433;Database=EcommerceDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True`
- **Override without editing the file** (env var, double underscore = section nesting):
  ```bash
  # PowerShell (persist) or use $env:... for the current session
  setx ConnectionStrings__DefaultConnection "Server=.\SQLEXPRESS;Database=EcommerceDb;Trusted_Connection=True;TrustServerCertificate=True"
  ```
- `TrustServerCertificate=True` avoids local dev TLS-cert errors; drop it (and use a
  trusted cert) in production.
- The provider is set in one place — `Ecommerce.Infrastructure/DependencyInjection.cs`
  (`UseSqlServer`) — so swapping to PostgreSQL (`UseNpgsql`) later is a one-line change
  plus the matching NuGet package and a fresh migration.

### Secrets

`Jwt:Key` and `Seed:AdminPassword` ship with development placeholders. **Change
them for any real deployment** via environment variables or user-secrets:

```bash
dotnet user-secrets set "Jwt:Key" "a-long-random-secret-at-least-32-chars" --project src/Ecommerce.Api
```

---

## Database & migration commands

Run from the repo root. The startup project (Api) holds the connection string;
the migrations live in the Infrastructure project.

```bash
# Add a new migration
dotnet ef migrations add <Name> \
  --project src/Ecommerce.Infrastructure \
  --startup-project src/Ecommerce.Api \
  --output-dir Persistence/Migrations

# Apply migrations manually (also done automatically on startup)
dotnet ef database update \
  --project src/Ecommerce.Infrastructure \
  --startup-project src/Ecommerce.Api

# Remove the last (unapplied) migration
dotnet ef migrations remove \
  --project src/Ecommerce.Infrastructure \
  --startup-project src/Ecommerce.Api
```

The initial migration `InitialCreate` is already included.

---

## Default admin & auth flow

A default administrator is seeded on first run (configurable under `Seed` in
`appsettings.json`):

| Field    | Value                     |
|----------|---------------------------|
| Email    | `admin@ecommerce.local`   |
| Password | `Admin#12345`             |

**Using the minimal UI:** open `/Login` (admin credentials are pre-filled) and
click **Login** — you're redirected to the console at `/` to manage products and
orders. Use the **Register** tab to create a customer account.

**Using Swagger (`/swagger`):**
1. `POST /api/auth/login` with the admin credentials → copy the `token`.
2. Click **Authorize** (top-right), paste the token, and authorize.
3. Call the protected endpoints.

- `POST /api/auth/register` creates a **Customer** (public).
- Creating another **Admin** requires being authenticated as an Admin and passing
  `"role": "Admin"` in the register body.

---

## API reference

| Method | Endpoint                          | Auth          | Description                              |
|--------|-----------------------------------|---------------|------------------------------------------|
| POST   | `/api/auth/register`              | Public        | Register a customer, returns JWT         |
| POST   | `/api/auth/login`                 | Public        | Login, returns JWT                       |
| GET    | `/api/products`                   | Authenticated | List products                            |
| GET    | `/api/products/{id}`              | Authenticated | Get one product                          |
| POST   | `/api/products`                   | **Admin**     | Create product                           |
| PUT    | `/api/products/{id}`              | **Admin**     | Update product                           |
| DELETE | `/api/products/{id}`              | **Admin**     | Delete product                           |
| POST   | `/api/orders`                     | **Customer**  | Create order (validates stock, totals)   |
| GET    | `/api/orders/mine`                | **Customer**  | List my orders                           |
| GET    | `/api/orders`                     | **Admin**     | List all orders                          |
| GET    | `/api/orders/{id}`               | Authenticated | Get order (own for customers, any admin) |
| PATCH  | `/api/orders/{id}/status`        | **Admin**     | Advance/cancel order lifecycle           |

**Create order** body:
```json
{ "items": [ { "productId": "GUID", "quantity": 3 } ] }
```

**Update status** body (`action`: `Pay` \| `Ship` \| `Deliver` \| `Cancel`):
```json
{ "action": "Pay" }
```

Errors return a consistent shape: `{ "status": 400, "error": "message" }`.

---

## Order lifecycle

```
Pending ──Pay──> Paid ──Ship──> Shipped ──Deliver──> Delivered
   │                │
   └──────Cancel────┘   (Cancel allowed only while Pending or Paid)
```

Enforced rules:
- **Cannot ship** unless the order is **Paid**.
- **Cannot deliver** unless the order is **Shipped**.
- **Cannot cancel** once **Shipped** (or Delivered).
- Cancelling a Pending/Paid order **restores reserved stock** to inventory.

Stock is validated and decremented atomically on order creation; ordering more
than the available stock is rejected with HTTP 400.

---

## Architecture decisions & trade-offs

- **Clean Architecture (4 projects).** Clear separation of concerns and a
  testable, framework-agnostic core. *Trade-off:* more projects/ceremony than a
  single-project API — justified by the spec's emphasis on clean architecture.

- **Rich domain model.** Lifecycle transitions and stock rules are methods on
  `Order`/`Product` (with a private setter on `Status`/`TotalAmount`), so invariants
  can't be bypassed. *Trade-off:* slightly less anemic-DTO-friendly, but safer.

- **`IApplicationDbContext` over the repository pattern.** The Application layer
  depends on an interface exposing `DbSet`s, keeping it decoupled from the concrete
  context while still leveraging EF's `IQueryable`/change-tracking. *Trade-off:* the
  Application layer references EF Core abstractions (a pragmatic, widely-used choice)
  rather than being 100% persistence-ignorant.

- **SQL Server.** Production-grade RDBMS with native `decimal(18,2)` money,
  `datetime2`, and `uniqueidentifier` types, plus real concurrency. LocalDB makes
  local dev zero-config on Windows. EF Core's `EnableRetryOnFailure()` is enabled for
  transient-fault resilience. *Trade-off:* requires a SQL Server instance available
  (LocalDB/Express/Docker) versus a self-contained file; the provider is isolated to a
  single line for portability.

- **Price snapshot on `OrderItem`.** `UnitPrice`/`ProductName` are copied at order
  time so historical orders are unaffected by later product edits.

- **Migrate + seed on startup.** Frictionless first run. *Trade-off:* for
  multi-instance production you'd typically run migrations as a separate deploy step.

- **JWT with role claims.** Stateless auth; roles drive `[Authorize(Roles=...)]`.
  *Trade-off:* no server-side token revocation (acceptable for short-lived tokens;
  add a refresh-token/blacklist for production).

### Bonus features implemented
- ✅ Serilog logging (console + daily rolling file in `logs/`)
- ✅ Global exception-handling middleware (consistent error JSON + logging)
- ✅ Request logging via `UseSerilogRequestLogging`

### Possible next steps
- Refresh tokens & token revocation
- FluentValidation for richer validation messages
- Caching (e.g. `IMemoryCache`) on product reads
- Integration tests (the `Program` class is exposed via `public partial class Program`)
- Pagination on list endpoints
```
