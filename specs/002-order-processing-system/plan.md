# Implementation Plan: Asynchronous Order Processing System

**Branch**: `002-order-processing-system` | **Date**: 2026-05-01 | **Spec**: `/specs/002-order-processing-system/spec.md`
**Input**: Feature specification from `/specs/002-order-processing-system/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

This plan outlines the implementation of an asynchronous order processing system for ES2-SistemaPedidos using .NET (C#). The system comprises three main components:

1. **ES2-SistemaPedidos.Api**: REST API for order creation and retrieval with bearer token authentication
2. **ES2-SistemaPedidos.Worker**: Background worker service that processes orders asynchronously based on SNS/SQS messaging
3. **ES2-SistemaPedidos.Shared**: Shared contracts and domain models including OrderCreatedEvent

The system uses PostgreSQL for data persistence, LocalStack for SNS/SQS messaging simulation (development/test), and implements a robust state machine for order lifecycle management (Pending → Processing → Approved/Rejected/Failed). Orders are approved/rejected based on a configurable amount threshold (default €1000).

## Technical Context

**Language/Version**: C# with .NET 10.0

**Primary Dependencies**:
- AWS SDK (Amazon.SimpleNotificationService, Amazon.SQS)
- Entity Framework Core (PostgreSQL provider)
- Polly (resilience patterns for retry logic)
- MediatR (CQRS pattern for command/query handling, optional)
- Serilog (logging)
- xUnit + FluentAssertions (unit testing)
- TestContainers (integration testing with Docker containers)

**Storage**: PostgreSQL (database container via Docker)

**Testing**: xUnit with TestContainers for integration tests, standard unit tests

**Target Platform**: Linux/Docker container-based deployment

**Project Type**: Backend service/multi-tier system (API + Worker + Shared library)

**Performance Goals**:
- API order creation: < 500ms
- API order retrieval: < 100ms
- Worker order processing: < 5 seconds per order
- System throughput: 100+ orders per second
- Message delivery: 100% reliability

**Constraints**:
- Minimal external dependencies (prefer .NET standard libraries)
- LocalStack SNS/SQS for dev/test only
- PostgreSQL connection pooling for scalability
- Dead-letter queue for failed messages

**Scale/Scope**:
- 5 user stories (3 P1, 2 P2)
- 3 projects in monorepo
- Core entities: Order, OrderItem, OrderStatus enum
- 4 event types: OrderCreatedEvent, OrderApprovedEvent, OrderRejectedEvent, error events
- ~10-15 API endpoints (CRUD + diagnostics)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Constitution Compliance

✅ **I. Code Quality**: Plan includes static analysis integration and coding standards enforcement through .NET analyzers

✅ **II. Testing Standards**: Comprehensive testing strategy with unit, integration (TestContainers), and E2E tests; all critical paths covered

✅ **III. User Experience Consistency**: API follows REST conventions; event contracts are consistent across services

✅ **IV. Performance Requirements**: All performance goals explicitly defined (< 500ms creation, < 100ms retrieval, < 5s processing, 100+ orders/sec)

✅ **V. Simplicity and Maintainability**: Monorepo structure keeps dependencies minimal; shared contracts prevent duplication; clear separation between API, Worker, and Shared layers

## Project Structure

### Documentation (this feature)

```text
specs/002-order-processing-system/
├── plan.md                    # This file (implementation planning)
├── research.md                # Phase 0 research findings (generated)
├── data-model.md              # Phase 1 database schema and entities (generated)
├── quickstart.md              # Phase 1 quick start guide (generated)
├── contracts/                 # Phase 1 API and event contracts (generated)
│   ├── api-contracts.md
│   ├── events-contracts.md
│   └── examples/
├── spec.md                    # Original feature specification
└── checklists/
```

### Source Code (repository root)

```text
src/
├── ES2-SistemaPedidos.Api/                    # REST API service
│   ├── Controllers/
│   │   ├── OrdersController.cs
│   │   └── HealthController.cs
│   ├── Services/
│   │   ├── OrderService.cs
│   │   └── EventPublisher.cs
│   ├── Models/
│   │   ├── CreateOrderRequest.cs
│   │   └── OrderResponse.cs
│   ├── Middleware/
│   │   └── AuthenticationMiddleware.cs
│   ├── Configuration/
│   │   └── DependencyInjection.cs
│   └── Program.cs
│
├── ES2-SistemaPedidos.Worker/                 # Background worker service
│   ├── Services/
│   │   ├── OrderProcessingService.cs
│   │   ├── MessageHandler.cs
│   │   └── MessageConsumer.cs
│   ├── Models/
│   │   └── ProcessingConfiguration.cs
│   ├── Configuration/
│   │   └── DependencyInjection.cs
│   └── Program.cs
│
└── ES2-SistemaPedidos.Shared/                 # Shared contracts and models
    ├── Domain/
    │   ├── OrderStatus.cs
    │   ├── Order.cs
    │   ├── OrderItem.cs
    │   └── ValidationRules.cs
    ├── Events/
    │   ├── OrderCreatedEvent.cs
    │   ├── OrderApprovedEvent.cs
    │   ├── OrderRejectedEvent.cs
    │   └── ErrorEvent.cs
    ├── DTOs/
    │   ├── OrderDto.cs
    │   └── OrderItemDto.cs
    └── Data/
        ├── ApplicationDbContext.cs
        ├── Migrations/
        └── DbInitializer.cs

docker/
├── docker-compose.yml         # Orchestrates PostgreSQL + LocalStack
├── postgres/
│   └── init-db.sql
└── localstack/
    └── init-services.sh

tests/
├── unit/
│   └── ES2-SistemaPedidos.Api.UnitTests/
│   └── ES2-SistemaPedidos.Worker.UnitTests/
├── integration/
│   └── ES2-SistemaPedidos.Api.IntegrationTests/
└── e2e/
    └── ES2-SistemaPedidos.E2ETests/
```

**Structure Decision**: Monorepo with 3 projects (Api, Worker, Shared) following domain-driven design principles. Shared library contains domain models, event contracts, and data access layers used by both Api and Worker. Docker composition handles PostgreSQL and LocalStack infrastructure.

## Complexity Tracking

No constitution violations. All complexity is justified by functional requirements.

## Implementation Phases

### Phase 0: Foundational Infrastructure Setup
**Deliverables**: Docker Compose setup, database initialization, LocalStack configuration
- Configure docker-compose.yml with PostgreSQL and LocalStack services
- Initialize database schema and seed data if needed
- Set up AWS SDK client configuration for SNS/SQS (local endpoints)
- Create base Shared library structure with domain models

**User Stories**: None directly (supports all stories)

### Phase 1: Core Data Model & API Foundation
**Deliverables**: Order entity, OrderItem entity, OrderStatus enum, Order API endpoints (create, get, list)
- US-1: Implement Order creation endpoint with validation
- US-2: Implement Order retrieval endpoints
- Database migrations for Order and OrderItem tables
- Authentication middleware setup
- Event publishing infrastructure (SNS)

**Duration**: ~2-3 sprints
**Dependencies**: Phase 0

### Phase 2: Worker Service Implementation
**Deliverables**: Worker service, message consumer, order processing logic
- US-3: Implement Worker message consumer (SQS)
- Implement amount-based approval/rejection logic
- Implement order status transitions and event publishing
- Dead-letter queue handling
- Idempotent message processing
- Error handling and logging

**Duration**: ~2-3 sprints
**Dependencies**: Phase 1

### Phase 3: Order Lifecycle & Observability
**Deliverables**: Order history tracking, status audit trail, diagnostics endpoints
- US-4: Implement order history retrieval endpoints
- Timestamp tracking for all status transitions
- Add statistics/dashboard endpoints
- Improve logging and monitoring

**Duration**: ~1-2 sprints
**Dependencies**: Phase 2

### Phase 4: Message Reliability & Testing
**Deliverables**: Comprehensive testing, retry policies, reliability hardening
- US-5: Implement exponential backoff retry logic with Polly
- Add health check endpoints
- Integration tests for full order lifecycle
- E2E tests with Docker containers
- Load testing and performance validation

**Duration**: ~2-3 sprints
**Dependencies**: Phase 1-3

### Phase 5: Production Hardening
**Deliverables**: Security, monitoring, documentation
- Bearer token authentication hardening
- SQL injection prevention and database constraints
- Comprehensive logging and tracing
- API documentation (Swagger/OpenAPI)
- Deployment automation
- Performance optimization
