# 8. Conclusion & References

## 8.1 Conclusion

The development of the Ataba E-commerce platform successfully demonstrates the viability of building a custom, full-stack, self-hosted online commerce application tailored to local and regional market requirements. By using a monolithic ASP.NET Core 10.0 MVC architecture combined with Microsoft SQL Server, the project provides a highly cohesive system that balances performance, ease of deployment, and ease of maintenance for small-to-medium enterprises.

Throughout the project lifecycle, several core milestones were reached and validated:

1. **Enterprise Data Access**: The implementation of the Repository and Unit of Work patterns abstracted data access behind clean interfaces, ensuring proper separation of concerns and database independence.
2. **Concurrency Safety**: By introducing SQL Server row versioning (`[Timestamp] RowVersion`) and EF Core optimistic concurrency tokens, the platform successfully mitigated stock overselling and promo code race conditions under high concurrent demand.
3. **Responsive Frontend & Modern UX**: Integrating design tokens, cookie-based theme storage (avoiding Flash of Unstyled Content), and debounced AJAX filters created a desktop and mobile UX that rivals global proprietary platforms.
4. **Secure Payment Processing**: The dual checkout pipeline supported both Cash on Delivery (COD) and Credit Card payments via the Stripe Checkout Session API, catering to low card-penetration markets while offering secure digital payment options.
5. **AI-Driven Customer Experience**: The context-aware AI assistant, built using Google Gemini API with robust rate-limit retries and exponential backoff, successfully demonstrated how generative AI can be securely and effectively integrated into consumer-facing platforms.

### 8.1.1 Future Work

While the current platform is fully functional and production-ready, several areas are identified for future enhancements:

- **Advanced Search Indexing**: Transitioning from database-level SQL `LIKE` queries to a dedicated search cluster like Elasticsearch to support fuzzy matching, auto-suggestions, and high-performance faceted searches.
- **Distributed Cache Integration**: Migrating from built-in in-memory caching to a distributed Redis cache, enabling the system to scale horizontally across multiple web nodes.
- **Automated Testing Suite**: Implementing a testing project containing unit tests for core services (such as `GeminiService` and `StripePaymentService`) and integration tests for controller workflows.
- **Mobile Native Applications**: Developing native mobile wrappers (using Flutter or React Native) communicating with the backend's ASP.NET Core Web APIs to capture the mobile-first customer base.

---

## 8.2 References

1. **Microsoft Corporation.** (2025). *ASP.NET Core MVC Documentation: Overview of MVC architecture*. Retrieved from [https://learn.microsoft.com/aspnet/core/mvc](https://learn.microsoft.com/aspnet/core/mvc).
2. **Microsoft Corporation.** (2025). *Entity Framework Core Documentation: Handling Concurrency Conflicts*. Retrieved from [https://learn.microsoft.com/ef/core/saving/concurrency](https://learn.microsoft.com/ef/core/saving/concurrency).
3. **Stripe Inc.** (2026). *Stripe Checkout API Reference: Creating checkout sessions*. Retrieved from [https://stripe.com/docs/api/checkout/sessions](https://stripe.com/docs/api/checkout/sessions).
4. **Google Cloud.** (2026). *Gemini API Documentation: Developer Guides & SDK References*. Retrieved from [https://ai.google.dev/gemini-api/docs](https://ai.google.dev/gemini-api/docs).
5. **Fowler, M.** (2002). *Patterns of Enterprise Application Architecture*. Addison-Wesley Professional. (Details regarding the Repository and Unit of Work patterns).
6. **Martin, R. C.** (2017). *Clean Architecture: A Craftsman's Guide to Software Structure and Design*. Prentice Hall. (Guidelines on separation of concerns and interface boundaries).
7. **Fielding, R., & Reschke, J.** (2014). *Hypertext Transfer Protocol (HTTP/1.1): Semantics and Content*. RFC 7231, Internet Engineering Task Force (IETF).
8. **Barth, A.** (2011). *HTTP State Management Mechanism (Cookies)*. RFC 6265, Internet Engineering Task Force (IETF).
