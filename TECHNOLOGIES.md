# 🧪 Technical Manuscript

This manuscript documents the alchemies and protocols used to build the **Architect's Forge**.

> **Note:** For comprehensive architectural decisions and design rationale, see [ADR-001](./docs/adr/001-backend-architecture-and-technology-stack.md).

---

## 🏛️ Architecture

### **Clean Architecture**
The template follows the sacred principles of Clean Architecture. Dependency flow is strictly inward to ensure that your business logic remains untainted by the shifting sands of external technologies.

```
┌────────────────────┐
│    API Layer       │  ← HTTP, Controllers, Middleware, Filters
└─────────┬──────────┘
          ↓ depends on
┌─────────▼──────────┐
│ Application Layer  │  ← Use Cases, Services, DTOs, Validators
└─────────┬──────────┘
          ↓ depends on
┌─────────▼──────────┐
│   Domain Layer     │  ← Business Rules, Entities, Domain Logic
└────────────────────┘
          ↑ implemented by
┌─────────┴──────────┐
│ Infrastructure     │  ← Repositories, DbContext, External Services
└────────────────────┘
```

**Layer Responsibilities:**
- **Domain:** Pure business logic, no dependencies on external frameworks
- **Application:** Orchestrates domain operations, defines interfaces
- **Infrastructure:** Implements interfaces, handles data persistence and external APIs
- **API:** HTTP concerns, authentication, validation, response formatting

### **The Result Pattern**
We avoid the "Throwing the Exception" for expected failures. Instead, we use the **Result Pattern**, returning a standard outcome object that either contains the prize or a detailed description of why the quest failed.

**Example:**
```csharp
public async Task<Result<UserDto>> GetUserAsync(Guid id)
{
    var user = await _repository.GetByIdAsync(id);
    if (user is null)
        return Result<UserDto>.Failure(ApiErrors.UserNotFound);
    
    return Result<UserDto>.Success(mappedUser);
}
```

**Benefits:**
- Explicit error handling without performance cost of exceptions
- Forces consumers to handle both success and failure cases
- Consistent error response format across entire API
- Easier to test and reason about control flow

---

## 🔧 Code Organization Patterns

### **Dependency Injection Facades**
Each architectural layer exposes a clean facade for service registration:

```csharp
// Program.cs stays clean
builder.Services.AddDomain();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication(emailConfig, redis, rabbitmq);
builder.Services.AddApiLayer(jwtKey, jwtIssuer, jwtAudience);
```

Each `Add*` method internally organizes related registrations into focused sub-functions.

### **Environment Variable Loader**
Centralized configuration loading with validation:

```csharp
var jwtKey = EnvironmentVariableLoader.GetRequired("JWT_KEY");
var emailConfig = EnvironmentVariableLoader.GetEmailConfig();
```

**Benefits:**
- Fail-fast on startup if configuration is missing
- Single consolidated error message for all missing variables
- Type-safe configuration objects
- Reduced boilerplate in Program.cs

### **Controller Extensions**
Reusable controller helper methods:

```csharp
public static class ControllerBaseExtensions
{
    // Convert Result<T> to IActionResult automatically
    public static IActionResult ToActionResult<T>(this Result<T> result);
    
    // Extract authenticated user ID from claims
    public static Result<Guid> GetAuthenticatedUserId(this ControllerBase controller);
    
    // Set secure refresh token cookie
    public static void AddRefreshTokenCookie(this ControllerBase controller, ...);
}
```

**Usage in controllers:**
```csharp
var userIdResult = this.GetAuthenticatedUserId();
if (!userIdResult.IsSuccess)
    return userIdResult.ToActionResult();

var profileResult = await _userService.GetProfileAsync(userIdResult.Value);
return profileResult.ToActionResult();
```

---

## 🌩️ Messaging

### **MassTransit & RabbitMQ**
Asynchronous communication is handled by **MassTransit**, an abstraction layer over **RabbitMQ**. It allows your services to communicate without being tightly coupled.

**Features:**
- Publish/Subscribe pattern for event broadcasting
- Request/Response for synchronous-like communication
- Dead Letter Queues for failed message handling
- Automatic message retry with exponential backoff
- Message serialization and deserialization

**Use Cases:**
- Welcome email sending after registration
- Account verification email delivery
- Password reset notifications
- Future: Inter-service communication in microservices architecture

---

## 🔒 Defense: Idempotency Protocols

### **The Idempotency-Key**
To prevent a client from accidentally casting the same spell twice (e.g., double charging a user), we implement **Idempotent Filters**. 
Critical actions require a unique `Idempotency-Key`. The system checks the **Redis Relic** to see if this key has been used recently, returning cached results for duplicates.

**Implementation:**
```csharp
[Idempotent] // Custom attribute on controller actions
public async Task<IActionResult> Register(RegisterDto request)
{
    // If Idempotency-Key header matches cached key, return cached response
    // Otherwise, process request and cache result with 24-hour TTL
}
```

**Protection Against:**
- Network failures causing retries
- Client-side bugs triggering duplicate requests
- Malicious actors attempting to trigger duplicate operations
- Race conditions from concurrent requests

---

## 🛡️ Security Measures

### **Multi-Layered DoS Protection**

**1. Kestrel Server Limits:**
```csharp
serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
serverOptions.Limits.MaxRequestHeadersTotalSize = 32 * 1024; // 32 KB
serverOptions.Limits.MaxRequestHeaderCount = 100;
serverOptions.Limits.MaxRequestLineSize = 8 * 1024; // 8 KB
```

**2. Controller Options:**
```csharp
options.MaxModelBindingCollectionSize = 1000; // Max items in collections
```

**3. Form Options:**
```csharp
options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB uploads
```

**4. Rate Limiting:**
- 60 requests per minute per IP address
- Fixed window algorithm with IP-based partitioning
- Custom 429 (Too Many Requests) error responses

### **Authentication & Authorization**
- **JWT Bearer Tokens:** Stateless authentication with access/refresh token pair
- **Google OAuth2:** External authentication provider integration
- **Guest Access:** Immediate anonymous session support via idempotency-safe guest login
- **Account Promotion:** Seamless transition from Guest to Registered user (Internal or Google)
- **BCrypt Password Hashing:** Work factor 11, salted per-password
- **Refresh Token Rotation:** Single-use refresh tokens with blacklist support

### **Input Validation**
- **FluentValidation:** Type-safe, expressive validation rules
- **Automatic DTO Validation:** Via `ValidationFilter` before controller actions
- **Custom Error Format:** Consistent error responses across all endpoints

---

## 📖 Chronicles: Structured Logging

### **Serilog & Seq**
Logs are not just text; they are **Structured Data**. 
*   **Serilog:** Enrich logs with contextual metadata (Environment, UserID, RequestPath).
*   **Seq:** A powerful analytics engine that filters and searches these logs in real-time.

**Log Enrichment:**
```csharp
Log.ForContext("UserId", userId)
   .ForContext("TraceId", traceId)
   .Information("User {UserId} updated profile");
```

**Multiple Sinks:**
- **Console:** Development debugging
- **File:** Persistent logs with daily rolling (`Logs/log-YYYYMMDD.txt`)
- **Seq:** Real-time querying and dashboards (development only)

**Query Examples in Seq:**
- Find all errors for a specific user: `UserId = 'abc-123' AND @Level = 'Error'`
- Trace a request across services: `TraceId = 'xyz-789'`
- Monitor response times: `@Message LIKE '%completed in%' | SELECT AVG(ResponseTime)`

---

## 🧪 Simulation: Testcontainers

We don't trust the "Works on my Machine" curse. 
Using **Testcontainers**, our integration tests spin up real, short-lived Docker containers for PostgreSQL and Redis. This ensures that your tests are running in an environment identical to your production realm.

**Test Infrastructure:**
```csharp
CustomWebApplicationFactory<Program>
  ├─ PostgreSQL Container (Real database for integration tests)
  ├─ Redis Container (Real cache for idempotency testing)
  ├─ RabbitMQ Container (Real message broker for event testing)
  └─ MailHog Container (SMTP server for email verification testing)
```

**Test Features:**
- **Respawn:** Database cleanup between tests for isolation
- **Test Collections:** Shared container infrastructure across test classes
- **Custom Web Application Factory:** Test-specific configuration overrides
- **Real Infrastructure:** No mocks for external dependencies

**Test Coverage:**
- 58 integration tests (100% pass rate)
- Authentication flows (internal & external)
- User profile management
- Idempotency verification
- Email sending and verification
- Token refresh and validation

---

## 🚀 Performance Optimizations

### **Async/Await Pattern**
All I/O operations (database, cache, HTTP, email) use async/await to prevent thread pool exhaustion:
```csharp
public async Task<Result<UserDto>> GetUserAsync(Guid id)
{
    var user = await _repository.GetByIdAsync(id);
    // Non-blocking I/O allows handling thousands of concurrent requests
}
```

### **Connection Pooling**
- **Entity Framework Core:** Automatic connection pooling with configurable size
- **Redis:** Connection multiplexing for efficiency
- **RabbitMQ:** Channel pooling via MassTransit

### **Caching Strategy**
- **Redis Distributed Cache:** Shared across multiple API instances
- **Cache-Aside Pattern:** Load from database on cache miss, populate cache
- **TTL Expiration:** Automatic cleanup of stale data

### **Database Optimizations**
- **EF Core Retry Logic:** Automatic retry on transient failures (network blips)
- **Snake_Case Naming:** Consistent with PostgreSQL conventions
- **Indexes:** Applied to frequently queried columns (migrations)

---

### 📜 Glossary of Artifacts

*   **.NET 9 SDK:** The core engine.
*   **Entity Framework Core 9:** The portal to the Database with Npgsql provider.
*   **StackExchange.Redis:** The bridge to the high-speed distributed cache.
*   **DotNetEnv:** The scribe that reads environmental variables from .env files.
*   **Asp.Versioning:** Manages different versions of your API routes (v1.0, v2.0, etc.).
*   **FluentValidation:** Expressive validation library with automatic integration.
*   **FluentEmail:** Elegant email composition and SMTP sending.
*   **Mapster:** High-performance object mapping library.
*   **Serilog:** Structured logging framework with multiple sinks.
*   **MassTransit:** Message broker abstraction for RabbitMQ.
*   **xUnit:** Leading .NET testing framework.
*   **Testcontainers:** Docker orchestration for integration tests.
*   **Respawn:** Database cleanup utility for test isolation.
*   **BCrypt.Net:** Password hashing library with configurable work factor.

---

## 📚 Further Reading

For comprehensive architectural decisions, design rationale, and alternatives considered, see:
- **[ADR-001: Backend Architecture and Technology Stack](./docs/adr/001-backend-architecture-and-technology-stack.md)**

For Docker and local development setup:
- **[DOCKER_SETUP.md](./DOCKER_SETUP.md)**

