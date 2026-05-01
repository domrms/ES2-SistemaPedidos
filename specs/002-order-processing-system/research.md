# Research & Technical Decisions: Order Processing System

**Date**: 2026-05-01  
**Status**: Complete

## Overview

This document consolidates research on technical decisions for implementing the ES2-SistemaPedidos asynchronous order processing system. All clarification questions from the feature specification have been resolved.

---

## Decision: .NET 10.0 as Primary Framework

**Decision**: Use C# with .NET 10.0 (LTS) as the implementation language and runtime.

**Rationale**:
- Project is already a .NET monorepo (ES2-SistemaPedidos.sln exists with Api, Worker, Shared projects)
- .NET 10 provides excellent async/await support needed for event-driven architecture
- Strong typing prevents runtime errors in complex domain logic
- Entity Framework Core is mature and provides efficient ORM for PostgreSQL
- Built-in dependency injection and logging make configuration simple
- Large ecosystem with proven libraries for messaging (AWS SDK) and resilience patterns (Polly)

**Alternatives Considered**:
- **Node.js/TypeScript**: Would require project migration; less optimal for long-running worker processes
- **Go**: Great for workers but lacks maturity in ORM ecosystem for complex domain models
- **Java**: Would require JVM alongside .NET infrastructure; complexity not justified

**Implementation**: Continue using existing .NET project structure

---

## Decision: PostgreSQL for Data Persistence

**Decision**: Use PostgreSQL 15+ as the primary data store with Docker containerization.

**Rationale**:
- Fully-featured relational database; supports complex querying for order history and analytics
- ACID compliance ensures data consistency across order state transitions
- JSON column support allows flexible event storage if needed
- Connection pooling via Entity Framework Core handles scaling
- Docker image available and well-maintained
- Clear migration path from development (Docker) to production (managed PostgreSQL)

**Alternatives Considered**:
- **MongoDB**: Would add operational complexity for relational data; audit trail and transactions are critical
- **DynamoDB**: Overkill for current scale; adds AWS-only dependency; eventual consistency problematic for order state
- **SQLite**: Insufficient for multi-process architecture (API + Worker)

**Implementation**: 
- Docker Compose service: postgres:15-alpine
- Connection string: `postgresql://user:password@localhost:5432/es2_orders`
- Migrations: Entity Framework Core Code-First
- Initial schema in `docker/postgres/init-db.sql`

---

## Decision: LocalStack for SNS/SQS (Development/Test Only)

**Decision**: Use LocalStack Docker image to simulate AWS SNS and SQS services in development and test environments.

**Rationale**:
- Feature specification explicitly requires LocalStack for dev/test
- Zero cost vs AWS service fees during development
- Identical API to real AWS services; production switch is just configuration
- Docker-based makes integration with dev environment seamless
- No AWS account required for local development
- Community support and documentation widely available

**Alternatives Considered**:
- **Kafka**: More complex; would require schema registry, Zookeeper; overkill for current requirements
- **RabbitMQ**: Solid alternative but less compatible with AWS migration path; feature spec specifies SNS/SQS
- **Direct database polling**: Unreliable; doesn't match event-driven architecture pattern

**Implementation**:
- Docker Compose service: localstack/localstack:latest
- Create SNS topic: `OrderEvents`
- Create SQS queue: `OrderProcessingQueue`
- Subscribe SQS queue to SNS topic
- Environment variables for LocalStack endpoint configuration
- Production: Switch to real AWS SNS/SQS via configuration only (no code changes)

---

## Decision: Polly for Resilience & Retry Logic

**Decision**: Use Polly library for implementing exponential backoff retry strategy in Worker service.

**Rationale**:
- Specification explicitly requires: "Initial delay 1s, maximum 5 retries, maximum delay 10s"
- Polly provides battle-tested implementations of retry, circuit breaker, and timeout patterns
- Composable policies allow combining multiple resilience strategies
- Widely adopted in .NET ecosystem; well-documented
- NuGet package integration is seamless

**Alternatives Considered**:
- **Custom retry logic**: Error-prone; reinvents the wheel; lacks sophistication
- **SQS native visibility timeout**: Insufficient; doesn't handle retry logic elegantly
- **Hangfire**: Overkill for simple retry needs; adds complexity

**Implementation**:
- NuGet: `Polly` and `Polly.CircuitBreaker` packages
- Policy configuration:
  ```
  var policy = Policy
    .Handle<Exception>()
    .OrResult<ProcessResult>(r => !r.Success)
    .WaitAndRetryAsync(
      retryCount: 5,
      sleepDurationProvider: retryAttempt => 
        TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryAttempt - 1), 10)),
      onRetry: (outcome, timespan, retryCount, context) => 
        logger.LogWarning($"Retry {retryCount} after {timespan.TotalSeconds}s")
    );
  ```
- Failed messages after 5 retries move to dead-letter queue

---

## Decision: Entity Framework Core with Code-First Migrations

**Decision**: Use EF Core Code-First approach with migrations for database schema management.

**Rationale**:
- Provides type-safe LINQ queries for complex order retrieval scenarios
- Automatic change tracking simplifies entity updates
- Migrations version-control schema changes; audit trail for database evolution
- Lazy loading and eager loading strategies allow performance optimization
- Integrated validation via Data Annotations and Fluent API

**Alternatives Considered**:
- **Dapper**: Lower-level control but requires manual SQL; more error-prone
- **Database-First**: Would require reverse-engineering; less maintainable as schema evolves
- **Raw SQL**: Security risks; harder to maintain; no type safety

**Implementation**:
- DbContext: `ApplicationDbContext` in Shared project
- Entities: Order, OrderItem with relationships defined fluently
- Owned types for embedded value objects if needed
- Migrations folder for version control
- Initial migration creates schema with constraints

---

## Decision: MediatR for CQRS Pattern (Optional Enhancement)

**Decision**: Recommend (not require) MediatR for API request handling to decouple controllers from business logic.

**Rationale**:
- Cleanly separates commands (CreateOrder, ApproveOrder) from queries (GetOrder)
- Enables cross-cutting concerns (logging, validation, error handling) via pipeline behaviors
- Testability improves with isolated command/query handlers
- Scales well as endpoint count grows (10+ endpoints planned)

**Alternatives Considered**:
- **Service layer directly**: Simpler initially but leads to controller bloat as more endpoints added
- **Repository pattern alone**: Still requires business logic layer; MediatR complements this

**Implementation** (Phase 1, post-MVP if needed):
- NuGet: `MediatR` and `MediatR.Extensions.Microsoft.DependencyInjection`
- Create command and query classes
- Implement handlers
- Register in DependencyInjection configuration

---

## Decision: Bearer Token Authentication (Simple Implementation)

**Decision**: Implement bearer token validation in middleware for MVP; extensible design for OAuth2 migration.

**Rationale**:
- Feature specification requires authentication but doesn't mandate OAuth2
- Simpler implementation for MVP; prevents exposure of sensitive endpoints
- Token validation logic easily swappable with future OAuth2 provider
- Middleware approach applies consistently across all endpoints
- Specification clarification decision: "Simple token validation; extensible design for future OAuth2 migration"

**Alternatives Considered**:
- **API Keys**: Less secure; harder to rotate
- **Full OAuth2**: Over-engineered for MVP; can be added later
- **No authentication**: Violates feature requirements

**Implementation**:
- Middleware extracts Bearer token from Authorization header
- Configuration-based list of valid tokens (or database lookup)
- Returns 401 Unauthorized if token missing or invalid
- Can extend to JWT validation later

---

## Decision: Docker Compose for Local Development

**Decision**: Use Docker Compose to orchestrate PostgreSQL, LocalStack, and application services for local development.

**Rationale**:
- Eliminates "works on my machine" issues
- Developers can spin up entire infrastructure in one command
- Matches production deployment model
- Easy to add services (Redis cache, monitoring, etc.) later
- Team consistency on infrastructure versions

**Alternatives Considered**:
- **Manual Docker commands**: Error-prone; hard to document
- **Kubernetes locally (Minikube)**: Overkill for development; adds complexity
- **Direct system-level services**: Platform-specific; unmaintainable

**Implementation**:
- File: `docker/docker-compose.yml`
- Services: postgres, localstack, (optional: api, worker)
- Volumes for data persistence
- Networks for service communication
- Environment files for configuration

---

## Decision: xUnit + FluentAssertions for Testing

**Decision**: Use xUnit as testing framework with FluentAssertions for readable assertions.

**Rationale**:
- xUnit is standard in .NET ecosystem; excellent parallel test execution
- FluentAssertions provide readable, self-documenting test assertions
- Both have strong IDE support and community adoption
- xUnit isolates test state; each test runs independently

**Alternatives Considered**:
- **NUnit**: Functional but older; more verbose
- **MSTest**: Microsoft-provided but less ergonomic
- **SpecFlow**: BDD-style; adds complexity for current scope

**Implementation**:
- NuGet: `xunit`, `xunit.runner.visualstudio`, `FluentAssertions`
- Test projects follow naming: `*.UnitTests`, `*.IntegrationTests`, `*.E2ETests`
- Assertions use fluent syntax: `result.Should().NotBeNull().And.HaveCount(5)`

---

## Decision: TestContainers for Integration Testing

**Decision**: Use TestContainers library to spawn PostgreSQL and LocalStack containers during integration tests.

**Rationale**:
- Tests run against real database/message queue instances; not mocked
- Containers spun up/down per test run; no test state pollution
- Ensures schema migrations work correctly
- Identifies real integration issues vs mock artifacts
- Self-documenting infrastructure requirements

**Alternatives Considered**:
- **Mocks/fakes**: Faster but doesn't catch real integration issues
- **Shared test database**: State pollution between tests; flaky failures
- **Production database**: Dangerous; risk of data loss

**Implementation**:
- NuGet: `Testcontainers` and `Testcontainers.PostgreSql`
- Integration test base class creates containers in setup
- Containers cleaned up after tests
- Example: `Integration/OrderServiceTests.cs`

---

## Decision: Serilog for Structured Logging

**Decision**: Use Serilog for structured logging with console and file sinks.

**Rationale**:
- Structured logging enables rich querying of logs in production
- Serilog integrates seamlessly with .NET dependency injection
- Supports multiple sinks (console, file, external services)
- Preserves context across async operations via enrichers
- Industry standard for .NET applications

**Alternatives Considered**:
- **NLog**: Functional but less streamlined with .NET Core DI
- **Built-in ILogger**: Limited features; not suitable for production

**Implementation**:
- NuGet: `Serilog`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`
- Configuration: `appsettings.json` with log level and output formats
- Structured properties logged via `LogContext`

---

## Decision: Event Contract Versioning Strategy

**Decision**: Version events implicitly; include event schema in published events for forward compatibility.

**Rationale**:
- Consumers must handle events from multiple publishers (different deployments)
- Schema versioning prevents breaking changes
- Graceful degradation if event data missing

**Alternatives Considered**:
- **Explicit version numbers in event class**: Adds maintenance burden
- **No versioning**: Causes breaking changes as system evolves

**Implementation**:
- Events include metadata: EventId, EventType, PublishedAt, Schema version
- Worker handles missing optional fields gracefully
- API validates required fields only during parsing

---

## Decision: Order State Machine Implementation

**Decision**: Implement order state machine with explicit validation of allowed transitions.

**Rationale**:
- Prevents invalid states (e.g., Approved → Pending)
- Single source of truth for state transition rules
- Enables clear error messages when invalid transitions attempted
- Testable state logic

**Alternatives Considered**:
- **No validation**: Leads to corrupted states; hard to debug
- **Database constraints only**: Application-level validation faster and provides better UX

**Implementation**:
- OrderStatus enum: Pending, Processing, Approved, Rejected, Failed
- Transition validation in Order domain entity:
  - Pending → Processing (Worker starts evaluation)
  - Processing → Approved (threshold met)
  - Processing → Rejected (threshold exceeded)
  - Processing → Failed (error during processing)
  - Any state → Failed (system error)

---

## Decision: Approval Threshold Configuration

**Decision**: Implement configurable approval threshold with default of €1000.

**Rationale**:
- Business rule may change; externalize configuration
- Environment-specific thresholds (test vs production)
- No code deployment needed for threshold adjustment

**Alternatives Considered**:
- **Hard-coded threshold**: Inflexible; requires code change and redeployment
- **Database configuration table**: Adds complexity; not needed for static thresholds

**Implementation**:
- Configuration source: appsettings.json or environment variable
- Key: `OrderProcessing:ApprovalThresholdAmount` (default: 1000)
- Loaded into dependency injection container
- Worker injected with IOrderApprovalConfiguration interface

---

## Decision: Idempotent Message Processing

**Decision**: Implement message deduplication using message ID and processed message tracking.

**Rationale**:
- SQS may deliver messages multiple times (exactly-once semantics not guaranteed)
- Feature specification requires idempotent processing
- Prevents duplicate order status updates if message reprocessed

**Alternatives Considered**:
- **No deduplication**: Risk of duplicate approvals/rejections
- **Distributed lock**: More complex; unnecessary for this scale

**Implementation**:
- Store processed message IDs in database table: `ProcessedMessages(MessageId, ProcessedAt)`
- Before processing, check if MessageId already processed
- If yes, acknowledge message and skip processing
- If no, process and record in ProcessedMessages table
- Add database constraint (unique) on MessageId

---

## Decision: Dead-Letter Queue Handling

**Decision**: Move messages to DLQ after 5 retries; include error details for investigation.

**Rationale**:
- Prevents infinite retry loops
- Preserves failed messages for investigation
- Manual intervention possible if systematic issue identified
- Operations can replay DLQ after fixes

**Implementation**:
- SQS DLQ associated with OrderProcessingQueue
- Policy: 5 max attempts, then move to DLQ
- DLQ consumer logs and stores error details
- Dashboard shows DLQ message count and oldest message timestamp

---

## Decision: API Pagination and Filtering

**Decision**: Implement pagination for list endpoints; support filtering by status and date range.

**Rationale**:
- Prevents slow queries and large response payloads
- Supports list endpoints for operations team (US-4)
- Industry standard for REST APIs

**Alternatives Considered**:
- **Return all records**: Unscalable; poor UX

**Implementation**:
- Query parameters: `skip`, `take`, `status`, `dateFrom`, `dateTo`
- Default: skip=0, take=20
- Response includes: data array, total count, page info

---

## Dependencies Summary

### NuGet Packages (Shared)
- `Microsoft.EntityFrameworkCore` (8.0+)
- `Npgsql.EntityFrameworkCore.PostgreSQL` (8.0+)
- `Amazon.SimpleNotificationService` (latest)
- `Amazon.SQS` (latest)

### NuGet Packages (API)
- `Microsoft.AspNetCore.App` (net10.0 built-in)
- `Serilog` and sinks

### NuGet Packages (Worker)
- `Microsoft.Extensions.Hosting` (net10.0 built-in)
- `Polly`
- `Serilog` and sinks

### NuGet Packages (Tests)
- `xunit`
- `FluentAssertions`
- `Moq` (for unit tests)
- `Testcontainers` (for integration tests)

### Docker Images
- `postgres:15-alpine`
- `localstack/localstack:latest`

---

## Environment Configuration

### Development (docker-compose)
```env
DATABASE_URL=postgresql://dev:dev@postgres:5432/es2_orders
AWS_ENDPOINT_URL=http://localstack:4566
AWS_REGION=us-east-1
SNS_TOPIC_ARN=arn:aws:sns:us-east-1:000000000000:OrderEvents
SQS_QUEUE_URL=http://localstack:4566/000000000000/OrderProcessingQueue
APPROVAL_THRESHOLD=1000
LOG_LEVEL=Debug
```

### Production
```env
DATABASE_URL=postgresql://produser:***@prod-rds-instance:5432/es2_orders
AWS_REGION=eu-west-1
SNS_TOPIC_ARN=arn:aws:sns:eu-west-1:XXXXX:OrderEvents
SQS_QUEUE_URL=https://sqs.eu-west-1.amazonaws.com/XXXXX/OrderProcessingQueue
APPROVAL_THRESHOLD=1000
LOG_LEVEL=Information
```

---

## Open Questions & Decisions

| Question | Decision | Status |
|----------|----------|--------|
| Should Worker auto-scale? | Deployment concern; use Kubernetes HPA | Out of scope |
| Should API use GraphQL? | REST is sufficient for requirements | Decided: REST |
| Should events be encrypted? | HTTPS/TLS sufficient for MVP | Future: Consider for sensitive data |
| Should order history be immutable? | Yes, via event sourcing | Future: Consider for full audit |

---

## Next Steps

1. **Phase 1**: Generate `data-model.md` with detailed entity definitions
2. **Phase 1**: Create API and event contracts in `/contracts/`
3. **Phase 1**: Generate `quickstart.md` with setup instructions
4. **Phase 2**: Implement infrastructure (Docker, database)
5. **Phase 3**: Develop API service
6. **Phase 4**: Develop Worker service
7. **Phase 5**: Implement comprehensive testing

