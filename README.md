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

