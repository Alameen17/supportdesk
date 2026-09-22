# SupportDesk

A multi-tenant helpdesk API built in C# / .NET 9 — the backend infrastructure a real SaaS support product (like Zendesk or Help Scout) would need, built from scratch to demonstrate production-grade patterns.

## What problem this solves

Small companies need a way for support agents to track, claim, and resolve customer tickets without stepping on each other. SupportDesk is a **multi-tenant** system — many separate companies share the same deployment, each with their own isolated data, users, and tickets — which is the standard shape of real B2B SaaS products.

The project is deliberately scoped to showcase the parts of backend engineering that are hard to get right:

- **Tenant isolation that can't be bypassed by accident** — the most common real-world multi-tenant SaaS bug is a forgotten `WHERE TenantId = ...` clause that leaks one customer's data to another. This is solved structurally here, not by convention.
- **Auth that's actually secure** — hashed passwords, short-lived JWTs, refresh tokens that never touch client-side JavaScript.
- **Safe concurrent writes** — when two support agents try to claim the same ticket at the same moment, only one should win. 

## Architecture

```
src/
├── Domain/          → entities, enums. Zero external dependencies.
├── Application/      → DTOs, service interfaces (IPasswordHasher, ITokenService).
├── Infrastructure/   → EF Core, Postgres, JWT generation, password hashing — 
│                       the concrete implementations of Application's interfaces.
└── Api/               → ASP.NET Core minimal API host. Endpoints, middleware, DI wiring.
```

`Domain` depends on nothing. `Application` depends only on `Domain`. `Infrastructure` implements `Application`'s interfaces. `Api` wires everything together. This means the core business rules (`Domain`) could be reused with a completely different database or a completely different web framework without changes.

## Tech stack

- **.NET 9** / ASP.NET Core Minimal APIs
- **PostgreSQL** via EF Core + Npgsql
- **JWT** access tokens (15 min lifetime) + opaque refresh tokens (30 days, httpOnly cookie)
- **BCrypt** for password hashing

## How tenant isolation actually works

Every entity that belongs to a tenant (`User`, `Ticket`, `TicketComment`) implements a marker interface, `ITenantEntity`. In `AppDbContext.OnModelCreating`, a **global query filter** is applied automatically to every entity implementing that interface:

```csharp
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
        continue;
    // ...apply a filter scoping every query to the current tenant
}
```

The current tenant is resolved once per HTTP request, from the `tenant_id` claim in the caller's JWT, by `TenantResolutionMiddleware` — and stored in a scoped `ITenantContext` service that `AppDbContext` reads from.

**The result:** a developer would have to deliberately call `.IgnoreQueryFilters()` to leak data across tenants. A normal `db.Tickets.ToListAsync()` call, anywhere in the codebase, is automatically safe. This flips the default from "unsafe unless you remember to filter" to "safe unless you deliberately opt out" — which is the property that actually matters in production multi-tenant systems.

## How auth works

1. **Register** — creates a new `Tenant` and its first `User` (role: `Owner`) in one transaction. Password is hashed with BCrypt before it ever touches the database.
2. **Login** — verifies the password hash, issues a short-lived JWT access token (with `tenant_id`, `sub`, `email`, and `role` claims) plus a long-lived refresh token, set as an `httpOnly` cookie so client-side JavaScript can never read it (mitigates XSS-based token theft).
3. **Every subsequent request** — `TenantResolutionMiddleware` reads the `tenant_id` claim off the validated JWT and populates `ITenantContext` before any endpoint code or database query runs.


## Getting it running locally

```bash
# 1. Start Postgres
docker run --name supportdesk-db -e POSTGRES_PASSWORD=devpassword -p 5432:5432 -d postgres

# 2. Apply migrations
cd src/Api
dotnet ef database update --project ../Infrastructure --startup-project .

# 3. Run the API
dotnet run
```

dotnet run prints the port it's listening on, e.g. Now listening on: http://localhost:5290 — use whatever port shows up in your own terminal for the requests below (ASP.NET Core assigns this per-machine, so it won't always be 5290).

Then, register a tenant (swap in your port):

```bash
curl -X POST http://localhost:5290/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"tenantName":"Acme Support","email":"owner@acme.com","password":"TestPass123!","displayName":"Alice Owner"}'
```

