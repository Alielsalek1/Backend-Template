# ADR-001: Backend Architecture and Technology Stack

**Status:** Accepted  
**Date:** February 15, 2026  
**Decision Makers:** Alielsalek1 
**Context:** Post-refactoring phase after successful TDD implementation with 58 passing integration tests

---

## Context and Problem Statement

We needed to establish a robust, scalable, and maintainable backend architecture for a modern web application that supports:
- Internal and external authentication mechanisms
- High reliability with idempotency guarantees
- Asynchronous event processing
- Comprehensive testing with real infrastructure
- Strong security posture against common attacks
- Clean separation of concerns for long-term maintainability

---

## Decision Drivers

1. **Maintainability:** Clean Architecture principles for clear dependency flow
2. **Testability:** Integration tests with real infrastructure (not mocks)
3. **Security:** Multi-layered defense against DoS, injection, and authentication attacks
4. **Performance:** Caching, rate limiting, and efficient data access patterns
5. **Reliability:** Idempotency, structured logging, and resilient messaging
6. **Developer Experience:** Type safety, validation, and clear error handling

---

## Considered Options

### Architecture Patterns
- **Option A:** Monolithic Clean Architecture (Chosen)
- **Option B:** Microservices with event-driven architecture
- **Option C:** Layered architecture without strict boundaries

### Technology Stack
- **Option A:** .NET 9 + PostgreSQL + Redis + RabbitMQ (Chosen)
- **Option B:** Node.js + MongoDB + Redis
- **Option C:** Java Spring Boot + MySQL + Kafka

---

## Decision Outcome

### Chosen: Clean Architecture with .NET 9 Stack

**Architectural Layers:**
```
┌─────────────────────────────────────┐
│         API Layer (Presentation)     │
│   Controllers, Middleware, Filters   │
└──────────────┬──────────────────────┘
               │ depends on ↓
┌──────────────▼──────────────────────┐
│      Application Layer (Use Cases)   │
│  Services, DTOs, Validators, Mappers │
└──────────────┬──────────────────────┘
               │ depends on ↓
┌──────────────▼──────────────────────┐
│      Domain Layer (Business Logic)   │
│   Entities, Enums, Exceptions, Rules │
└──────────────────────────────────────┘
               ▲ implemented by
┌──────────────┴──────────────────────┐
│    Infrastructure Layer (External)   │
│  Repositories, DbContext, Migrations │
└─────────────────────────────────────┘
```

**Dependency Flow:** API → Application → Domain ← Infrastructure

---

## Technology Decisions

### 1. **Core Framework: .NET 9 with C#**
**Why:**
- High performance with minimal allocations
- Strong type system with nullable reference types
- Excellent async/await support for I/O-bound operations
- Rich ecosystem with Entity Framework Core, Serilog, FluentValidation
- Long-term support and active development

**Alternatives Rejected:**
- Node.js: Weaker type safety, less mature ORM options
- Java: More verbose, heavier runtime footprint

---

### 2. **Database: PostgreSQL with Entity Framework Core 9**
**Why:**
- Robust ACID compliance for transactional data
- Excellent JSON support for semi-structured data
- Snake_case naming convention for consistency
- EF Core provides type-safe queries, migrations, and retry logic
- Built-in connection pooling and resilience

**Configuration:**
```csharp
UseNpgsql(connectionString, options => {
    options.EnableRetryOnFailure(maxRetryCount: 3, 
                                  maxRetryDelay: TimeSpan.FromSeconds(5), 
                                  errorCodesToAdd: null);
})
.UseSnakeCaseNamingConvention()
```

**Alternatives Rejected:**
- MongoDB: Lacks strong consistency guarantees needed for auth/user data
- MySQL: PostgreSQL has better JSON support and extensibility

---

### 3. **Caching: Redis (StackExchange.Redis)**
**Why:**
- Distributed caching for horizontal scalability
- Idempotency key storage with TTL expiration
- Session management and refresh token blacklisting
- Sub-millisecond latency for cache hits
- Persistence options for critical cached data

**Usage Patterns:**
- Idempotency keys: 24-hour TTL
- Cache instance name: `"MyBackendTemplate_"`
- Connection multiplexing for efficiency

**Alternatives Rejected:**
- In-memory cache: Doesn't scale across instances
- Memcached: Less feature-rich, no persistence options

---

### 4. **Messaging: RabbitMQ with MassTransit 8.3.6**
**Why:**
- Asynchronous event processing without tight coupling
- Reliable message delivery with acknowledgments
- MassTransit provides excellent .NET abstractions
- Dead letter queues for failed message handling
- Supports future microservices decomposition

**Configuration:**
```csharp
services.AddMassTransit(x => {
    x.UsingRabbitMq((context, cfg) => {
        cfg.Host(rabbitMqHost, ushort.Parse(rabbitMqPort), "/", h => {
            h.Username(rabbitMqUsername);
            h.Password(rabbitMqPassword);
        });
    });
});
```

**Alternatives Rejected:**
- Azure Service Bus: Vendor lock-in, higher cost
- Kafka: Overkill for current event volumes, more complex operations

---

### 5. **Authentication: JWT Bearer Tokens**
**Why:**
- Stateless authentication scales horizontally
- Industry-standard (RFC 7519)
- Works seamlessly with SPAs and mobile apps
- Claims-based identity model
- Symmetric key validation for performance

**Implementation:**
- Access tokens: Short-lived (configurable expiration)
- Refresh tokens: Stored securely, single-use pattern
- Token validation: Issuer, Audience, Lifetime, Signature

**Alternatives Rejected:**
- Session cookies: Requires server-side storage, harder to scale
- OAuth2 flows only: Need internal auth in addition to Google OAuth2

---

### 6. **External Auth: Google OAuth2**
**Why:**
- Reduces friction for user onboarding
- Google handles password security
- Access to verified email addresses
- Industry-standard OpenID Connect flow

**Implementation:**
- JWT issued after successful Google authentication
- User profile created/updated on first login
- Seamless integration with internal auth system

---

### 7. **Guest User Support & Promotion**
**Why:**
- Reduces initial barrier to entry for new users
- Allows capturing user activity before formal registration
- Enables "Try before you buy" application flows
- Seamless transition preserves user data/ID during promotion

**Implementation:**
- `/api/v1/internal-auth/login/guest`: Creates an anonymous user entity with a "Guest" role
- `/api/v1/internal-auth/promote/guest`: Converts existing guest record to a full internal account
- `/api/v1/external-auth/link/google`: Links an existing guest session to a Google account, promoting it to an external account
- Idempotency-Key required for guest creation to prevent duplicate ghost accounts

---

### 8. **Validation: FluentValidation**
**Why:**
- Expressive, strongly-typed validation rules
- Separates validation logic from DTOs
- Async validation support (e.g., database uniqueness checks)
- Rich error messages with property-level details
- Automatic validation via `ValidationFilter`

**Pattern:**
```csharp
services.AddFluentValidationAutoValidation();
options.Filters.Add<ValidationFilter>();
options.SuppressModelStateInvalidFilter = true; // Custom error format
```

**Alternatives Rejected:**
- Data Annotations: Less expressive, harder to test in isolation
- Manual validation: Error-prone, scattered across codebase

---

### 8. **Logging: Serilog with Seq Integration**
**Why:**
- Structured logging with rich context (UserId, TraceId, RequestPath)
- Multiple sinks: Console (development), File (persistent), Seq (analysis)
- Log correlation across distributed operations
- Real-time log querying and filtering in Seq
- Performance-optimized with async writing

**Configuration:**
```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq(seqUrl) // Development only
    .CreateLogger();
```

**Alternatives Rejected:**
- Plain text logging: Hard to query and analyze
- Application Insights: Azure lock-in for development

---

### 9. **Testing: xUnit + Testcontainers + Respawn**
**Why:**
- Real infrastructure testing (PostgreSQL, Redis, RabbitMQ via Docker)
- Deterministic test environments
- Database cleanup between tests (Respawn)
- MailHog for SMTP testing without external dependencies
- 58 integration tests with 100% pass rate

**Test Architecture:**
```csharp
CustomWebApplicationFactory<Program>
  ├─ PostgreSQL Container (Testcontainers)
  ├─ Redis Container (Testcontainers)
  ├─ RabbitMQ Container (Testcontainers)
  └─ MailHog Container (SMTP testing)
```

**Alternatives Rejected:**
- Unit tests only: Miss integration issues
- In-memory databases: Don't match production behavior
- Manual test environments: Non-deterministic, hard to maintain

---

### 10. **Object Mapping: Mapster**
**Why:**
- High performance (faster than AutoMapper)
- Compile-time code generation
- Explicit mapping configurations
- Minimal reflection overhead

**Alternatives Rejected:**
- AutoMapper: Slower runtime performance
- Manual mapping: Boilerplate code, error-prone

---

### 11. **Email: FluentEmail with SMTP**
**Why:**
- Fluent API for composing emails
- Template support for consistent branding
- Async sending with 20-second timeout
- Easy to swap providers (SMTP, SendGrid, etc.)

**Configuration:**
```csharp
services.AddFluentEmail(emailConfig.FromEmail, emailConfig.FromName)
    .AddSmtpSender(emailConfig.Host, emailConfig.Port);
```

**Alternatives Rejected:**
- Direct SmtpClient: More boilerplate, less readable
- Third-party APIs (SendGrid): Want to avoid vendor lock-in initially

---

### 12. **API Versioning: URL Segment-Based**
**Why:**
- Clear and explicit versioning in URLs (`/api/v1.0/users`)
- Easy to route different versions to different implementations
- Backwards compatibility for clients
- Asp.Versioning library provides standardized approach

**Configuration:**
```csharp
services.AddApiVersioning(options => {
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});
```

**Alternatives Rejected:**
- Header versioning: Less visible, harder to test in browsers
- Query string versioning: Can conflict with other query params

---

## Security Decisions

### 1. **Multi-Layered DoS Protection**

**Kestrel Server Limits:**
```csharp
serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
serverOptions.Limits.MaxRequestHeadersTotalSize = 32 * 1024; // 32 KB
serverOptions.Limits.MaxRequestHeaderCount = 100;
serverOptions.Limits.MaxRequestLineSize = 8 * 1024; // 8 KB
```

**Controller & Form Limits:**
```csharp
options.MaxModelBindingCollectionSize = 1000; // Max items in collections
options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB file uploads
```

**Rate Limiting:**
- Fixed window: 60 requests per minute per IP
- Custom 429 response with error details
- IP-based partitioning

**Why:** Defense in depth with multiple layers prevents resource exhaustion attacks.

---

### 2. **Idempotency Filter with Redis**

**Implementation:**
```csharp
[Idempotent] // Applied to critical endpoints
public async Task<IActionResult> Register(RegisterUserRequestDto request) { }
```

**Mechanism:**
- Client provides unique `Idempotency-Key` header
- Server checks Redis cache (24-hour TTL)
- Duplicate requests return cached response
- Prevents double registration, payment, etc.

**Why:** Network failures and retries are common; idempotency prevents data corruption.

---

### 3. **Password Security**

**BCrypt.Net:**
- Work factor: Configurable (default 11)
- Salted hashing with per-password random salts
- Resistant to rainbow table attacks
- Slow by design to prevent brute force

**Why:** Industry best practice for password storage.

---

### 4. **Result Pattern for Error Handling**

**Pattern:**
```csharp
public async Task<Result<UserProfileDto>> GetProfileAsync(Guid userId)
{
    var user = await _userRepository.GetByIdAsync(userId);
    if (user is null)
        return Result<UserProfileDto>.Failure(ApiErrors.UserNotFound);
    
    return Result<UserProfileDto>.Success(mappedDto);
}
```

**Benefits:**
- Expected failures don't throw exceptions (performance)
- Forces explicit error handling
- Consistent error response format across API
- Easier to test and reason about

**Alternatives Rejected:**
- Exception-based flow control: Poor performance, unexpected behavior
- Returning nulls: Doesn't convey error information

---

## Code Organization Decisions

### 1. **Dependency Injection Facade Pattern**

**Structure:**
```csharp
// Program.cs
builder.Services.AddDomain();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication(emailConfig, redis, rabbitmq);
builder.Services.AddApiLayer(jwtKey, jwtIssuer, jwtAudience);
```

**Each layer has DependencyInjection.cs with sub-functions:**
```csharp
public static IServiceCollection AddApplication(...)
{
    services.AddServices();
    services.AddRepositories();
    services.AddValidators();
    services.AddMappings();
    services.AddMessaging(...);
    return services;
}
```

**Why:**
- Program.cs stays clean and readable
- Each layer encapsulates its own dependencies
- Easy to test DI configuration in isolation
- Supports incremental loading and feature flags

---

### 2. **Environment Variable Loader Utility**

**Utility Class:**
```csharp
public static class EnvironmentVariableLoader
{
    public static string GetRequired(string key) { }
    public static string[] GetRequiredGroup(string[] keys) { }
    public static EmailConfig GetEmailConfig() { }
}
```

**Benefits:**
- Single point of failure for missing env vars
- Consolidated error reporting (all missing vars reported at once)
- Reduced Program.cs from ~80 lines to ~40 lines (50% reduction)
- Type-safe configuration objects

**Why:** Startup failures should be fast and informative; consolidated validation prevents multiple restarts.

---

### 3. **Controller Extension Methods**

**Implementation:**
```csharp
public static class ControllerBaseExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result) { }
    
    public static void AddRefreshTokenCookie(this ControllerBase controller, 
                                             string refreshToken, 
                                             DateTime expirationDate) { }
    
    public static Result<Guid> GetAuthenticatedUserId(this ControllerBase controller) { }
}
```

**Benefits:**
- DRY principle: No duplicated user ID extraction logic
- Consistent error responses across controllers
- Type-safe with Result pattern
- Easy to unit test in isolation

**Why:** Controllers should focus on routing and orchestration, not boilerplate logic.

---

## Performance Optimizations

### 1. **Database Connection Pooling**
- EF Core manages pool automatically
- Retry logic for transient failures (network blips)
- Snake_case convention reduces cognitive load

### 2. **Redis Caching Strategy**
- Cache frequently accessed data (user profiles, config)
- Idempotency keys with automatic TTL expiration
- Connection multiplexing to reduce overhead

### 3. **Async/Await Throughout**
- All I/O operations are async (database, cache, HTTP, email)
- Prevents thread pool starvation
- Scales to thousands of concurrent requests

### 4. **Structured Logging Performance**
- Async writing to sinks
- Log level filtering (Information in production, Debug in development)
- Seq only enabled in development to reduce production overhead

---

## Observability and Monitoring

### 1. **Health Checks**
- `/health`: Basic liveness check
- `/health/auth`: Requires authentication (tests auth pipeline)

### 2. **Structured Logging with Context**
- TraceId for request correlation
- UserId for user-specific debugging
- Environment, MachineName for multi-instance deployments
- RequestPath, StatusCode for access logs

### 3. **Seq Integration (Development)**
- Real-time log searching and filtering
- Query by TraceId, UserId, StatusCode
- Dashboards for error rates, response times

### 4. **RabbitMQ Management UI**
- Monitor queue depths, message rates
- Dead letter queue inspection
- Connection and channel metrics

---

## Testing Strategy

### 1. **Integration Test Philosophy**
- Test with real infrastructure (Docker containers)
- Cover happy paths and error scenarios
- Test idempotency guarantees
- Verify email sending with MailHog
- Database cleanup between tests with Respawn

### 2. **Test Organization**
```
Tests/
  ├─ Common/ (Shared test infrastructure)
  ├─ IntegrationTests/
  │   ├─ Auth/ (Login, register, OAuth, token refresh)
  │   └─ User/ (Profile updates, email verification)
  └─ Reusables/ (Test data builders, helpers)
```

### 3. **Test Coverage**
- 58 integration tests (100% pass rate)
- Critical paths: Auth, registration, profile management
- Idempotency: Duplicate request handling
- Error cases: Invalid tokens, missing users, validation failures

---

## Migration and Deployment Considerations

### 1. **Database Migrations**
- EF Core migrations in Infrastructure layer
- Applied automatically on startup (development) or via CI/CD (production)
- Idempotent migration scripts (safe to re-run)

### 2. **Environment Variables**
- .env file for local development
- Environment-specific appsettings.{Environment}.json
- Fail-fast validation on startup if missing required vars

### 3. **Docker Support**
- docker-compose.yml for local infrastructure
- Services: PostgreSQL, Redis, RabbitMQ, Seq, MailHog
- Dockerfile for API containerization

---

## Future Scalability Considerations

### 1. **Horizontal Scaling**
- Stateless API design (JWT auth, Redis cache)
- Load balancer ready (no sticky sessions needed)
- Database connection pooling per instance

### 2. **Microservices Migration Path**
- Clean Architecture makes extraction easier
- RabbitMQ already in place for inter-service communication
- Domain layer can be shared or duplicated per service

### 3. **CQRS Potential**
- Read/write separation for read-heavy workloads
- Application layer already separates commands from queries
- Could introduce read replicas for GET endpoints

---

## Risks and Mitigations

### Risk 1: Single Point of Failure (Database)
**Mitigation:**
- PostgreSQL replication (primary-replica setup)
- Regular backups with point-in-time recovery
- EF Core retry logic for transient failures

### Risk 2: Redis Cache Unavailability
**Mitigation:**
- Graceful degradation: API continues without cache
- Redis persistence (RDB/AOF) for critical data
- Cache-aside pattern: Rebuild from database on miss

### Risk 3: Message Queue Backlog
**Mitigation:**
- Dead letter queues for failed messages
- Monitoring queue depths with alerts
- Auto-scaling consumers based on queue size

### Risk 4: JWT Secret Compromise
**Mitigation:**
- Rotate JWT keys periodically
- Short access token expiration
- Refresh token single-use pattern with blacklist

---

## Lessons Learned

### 1. **TDD First, Refactor Second**
- Passing tests gave confidence during refactoring
- Integration tests caught issues mocks would miss
- Test infrastructure (Testcontainers) paid for itself immediately

### 2. **Consolidation Reduces Complexity**
- Environment variable loader eliminated ~40 lines of boilerplate
- Extension methods reduced controller duplication
- DI facades made Program.cs readable

### 3. **Multi-Layered Security is Essential**
- Rate limiting, request size limits, idempotency all contribute
- Defense in depth prevents single-point security failures

### 4. **Repository Pattern Clarity**
- Repositories return null for "not found" (not exceptions)
- Services handle validation and business logic
- Controllers orchestrate and format responses

---

## Alternatives Considered But Not Implemented

### 1. **CQRS (Command Query Responsibility Segregation)**
**Decision:** Deferred until read/write workload imbalance emerges.
**Reasoning:** Current complexity doesn't justify the additional patterns yet.

### 2. **Event Sourcing**
**Decision:** Not implemented; using traditional CRUD with audit logging.
**Reasoning:** Audit trail sufficient for current requirements; event sourcing adds significant complexity.

### 3. **gRPC Instead of REST**
**Decision:** REST/JSON for API.
**Reasoning:** Better browser/tooling support, easier for frontend teams, no performance bottleneck yet.

### 4. **GraphQL**
**Decision:** REST with versioning.
**Reasoning:** Simpler to implement, predictable performance, adequate for current use cases.

---

## Success Metrics

### Achieved:
- ✅ 58/58 integration tests passing (100% success rate)
- ✅ Clean Architecture with clear layer boundaries
- ✅ Sub-second response times for most endpoints
- ✅ Zero unhandled exceptions in production-like tests
- ✅ Idempotency guarantees verified
- ✅ Comprehensive logging with structured data

### Ongoing:
- 🔄 Code coverage measurement (unit tests to be added)
- 🔄 Production observability dashboards
- 🔄 Load testing under realistic traffic patterns
- 🔄 Security audit and penetration testing

---

## References

### Documentation:
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Result Pattern in C#](https://www.milanjovanovic.tech/blog/functional-error-handling-in-dotnet-with-the-result-pattern)
- [ASP.NET Core Performance Best Practices](https://learn.microsoft.com/en-us/aspnet/core/performance/performance-best-practices)

### Libraries:
- [.NET 9 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [MassTransit](https://masstransit.io/)
- [Serilog](https://serilog.net/)
- [FluentValidation](https://docs.fluentvalidation.net/)
- [Testcontainers](https://dotnet.testcontainers.org/)

---

## Change Log

| Date | Change | Rationale |
|------|--------|-----------|
| 2026-02-15 | Initial ADR created | Document current architectural state post-refactoring |

---

## Approval

**Approved by:** Development Team  
**Date:** February 15, 2026  
**Next Review:** Q2 2026 (or when significant architectural changes are proposed)
