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

| Service | Interface / Config | Purpose |
|---------|-------------------|---------|
| Stripe | `IPaymentService` | Payments |
| Email | `IEmailService` | SMTP via Gmail |
| Analytics | `IAnalyticsService` | Usage tracking |
| Gemini | `IGeminiService` | AI chat |
| Image | `IImageService` | Image handling |
| Tax | `TaxSettings` (config) | Configurable tax rate via `appsettings.json -> TaxSettings:TaxRate` |
| Admin Email | `EmailSettings:ContactEmail` | Configurable contact/notification recipient |

## App Settings Reference

| Section | Key | Purpose | Default |
|---------|-----|---------|---------|
| `Stripe` | `PublishableKey`, `SecretKey`, `Domain` | Stripe payment keys | Placeholder test keys |
| `EmailSettings` | `SMTPServer`, `SMTPPort`, `SenderEmail`, `SenderPassword`, `SenderName`, `ContactEmail` | SMTP config + contact recipient | Gmail SMTP, placeholder |
| `Gemini` | `ApiKey`, `Model`, `MaxTokens`, `Temperature` | Gemini AI configuration | gemini-flash-latest |
| `TaxSettings` | `TaxRate` | Order tax rate (decimal) | `0.14` (14%) |

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

### Security Hardening (June 2026)
- **XSS**: All product name/description passed to AI modal uses `data-*` attributes (Razor auto-encodes) instead of `Html.Raw`
- **Chart labels**: Chart.js labels use `@Json.Serialize()` instead of `Html.Raw(string.Join(...))` to prevent XSS via user-created category names
- **Authorization**: `CheckoutController.PaymentSuccess`/`PaymentCancelled` verify `order.UserId == currentUserId` before mutating payment status
- **CSRF**: All `[HttpPost]` endpoints now have `[ValidateAntiForgeryToken]` (WishlistController, AdminController RemoveImage, HomeController Subscribe)
- **Image Upload**: Extension whitelist (jpg/png/gif/webp/bmp), content-type validation, 5MB size limit, path traversal protection
- **Rate Limiting**: Review helpful votes tracked via cookie to prevent spam

### Transactional Integrity (June 2026)
- `AccountController.DeleteAccountConfirmed` wraps all entity deletions in a DB transaction
- `DbInitializer.SeedAsync` wraps seed operations in a transaction for atomicity
- `AdminController.DeleteUser` now cleans up cart, wishlist, reviews, orders, and payments (was missing wishlist, reviews, orders)

### Resource Management (June 2026)
- `EmailService`: `SmtpClient` and `MailMessage` are now properly disposed via `using` statements
- `StripePaymentService`: `StripeConfiguration.ApiKey` set once (thread-safe), `Session.Url` null-checked
- `AnalyticsService`: Daily/monthly sales now use server-side `GroupBy` + `ToListAsync()` instead of client-side grouping

### Performance (June 2026)
- `OrdersController.MyOrders` now paginated (10 per page) instead of loading all orders
- `AnalyticsService.GetDailySalesAsync` uses `Dictionary` for O(1) lookups instead of O(n²) `FirstOrDefault`

### Model Annotations Added (June 2026)
- `ApplicationUser.FullName`: `[Required]`, `[StringLength(100)]`
- `ProductVariant.Stock`: `[Range(0, int.MaxValue)]`
- `ProductVariant.AdditionalPrice`: `[Range(0, double.MaxValue)]`
- `PromoCode.DiscountValue`: `[Range(0, double.MaxValue)]`
- `Order.PhoneNumber`: `[StringLength(20)]`
- `ProductReview.Product`/`User`: Non-null nav properties
- `Product.ProductReviews`: Added missing collection nav property

### Configuration (June 2026)
- Tax rate moved from hardcoded `0.14m` to `appsettings.json -> TaxSettings:TaxRate`
- Contact email moved from hardcoded `"ataba.contact@example.com"` to `EmailSettings:ContactEmail`
- `Repository.GetByIdAsync` with includes now resolves PK dynamically via EF metadata instead of hardcoded `"Id"`