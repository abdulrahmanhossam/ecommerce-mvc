# AGENTS.md

## Project Overview

ASP.NET Core 10.0 MVC e-commerce application with SQL Server, Identity, Stripe payments, and Gemini AI integration.

## Run Commands

```bash
# Run in development (HTTP)
dotnet run --project ECommerceProject

# Run with HTTPS (requires certificate)
dotnet run --project ECommerceProject --launch-profile https
```

Default URL: `http://localhost:5112`

## Database

- **Provider**: SQL Server (localhost:1433)
- **Connection**: `appsettings.json` -> `ConnectionStrings:DefaultConnection`
- **Migrations**: Run `dotnet ef migrations add <Name>` then `dotnet ef database update`
- Database auto-seeds on startup via `DbInitializer.SeedAsync()` in Program.cs

## Key Services

| Service | Interface | Purpose |
|---------|-----------|---------|
| Stripe | `IPaymentService` | Payments |
| Email | `IEmailService` | SMTP via Gmail |
| Analytics | `IAnalyticsService` | Usage tracking |
| Gemini | `IGeminiService` | AI chat |
| Image | `IImageService` | Image handling |

## Project Structure

- `Controllers/` - MVC controllers
- `Models/` - Entity models
- `Views/` - Razor views
- `Services/` - Business logic
- `Data/` - EF Core context, repositories
- `Migrations/` - EF migrations

## Development Notes

- Razor views compile at runtime (`AddRazorRuntimeCompilation`)
- Identity configured with email/password login
- Password policy: 6+ chars, digit, lowercase, uppercase required
- No test project exists

## Environment Variables / Secrets

`appsettings.json` contains placeholder secrets (Stripe keys, Gmail app password, Gemini API key) - replace for production.

## EF Core Commands

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
dotnet ef migrations list
```

## Anchored Architecture Decisions (May 2026)

### Repository AsNoTracking
`IRepository<T>` has overloads for all read methods with `asNoTracking` parameter (default `false`). Callers performing read-only GET operations should pass `asNoTracking: true`. Existing write paths pass nothing (tracking on).

### Concurrency Tokens
- `Product`, `ProductVariant`, and `PromoCode` entities have `[Timestamp] byte[] RowVersion` for optimistic concurrency
- Prevents stock overselling when two users checkout the same product simultaneously
- Prevents promo code `UsageCount` race conditions
- Migration: `AddConcurrencyTokens` (run `dotnet ef database update` to apply)

### AdminController N+1 Fixed
`Users()` page batches role lookups using `RoleManager<IdentityRole>` + `GetUsersInRoleAsync()` per role (not per user). Injected via DI alongside existing `UserManager<ApplicationUser>`.