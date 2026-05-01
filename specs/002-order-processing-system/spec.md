# Feature Specification: Asynchronous Order Processing System

**Feature Branch**: `002-order-processing-system`  
**Created**: 2026-05-01  
**Status**: Ready  
**Clarified**: 2026-05-01  
**Input**: User description: "Create a feature specification for the ES2-SistemaPedidos project based on the provided requirements. The project is a .NET monorepo that implements an asynchronous order processing flow using SNS/SQS messaging (via LocalStack), PostgreSQL, and a Worker service. Include user stories covering API order creation, order retrieval, worker order processing with approval/rejection logic based on total amount, and the complete order lifecycle from creation to final status. Focus on the domain model (OrderStatus enum with states: Pending, Processing, Approved, Rejected, Failed), the shared contracts (OrderCreatedEvent), and infrastructure requirements (LocalStack SNS/SQS, PostgreSQL)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create Order via API (Priority: P1)

As a customer or system integration, I want to submit an order through the API so that the system begins processing my request asynchronously.

**Why this priority**: This is the critical entry point for the entire order lifecycle. Without the ability to create orders, no downstream processing can occur. This is foundational functionality required by all other features.

**Independent Test**: Can be fully tested by submitting a POST request to the order creation endpoint and verifying the order is persisted in the database with Pending status and an OrderCreatedEvent is published to SNS.

**Acceptance Scenarios**:

1. **Given** the API is running and database is accessible, **When** a valid order creation request is submitted (with order items and total amount), **Then** the system returns HTTP 201 with the created order ID and the order status is Pending
2. **Given** a valid order request, **When** the order is created, **Then** an OrderCreatedEvent is published to SNS containing the order ID and order details
3. **Given** an invalid order request (missing required fields or invalid data), **When** the creation is attempted, **Then** the system returns HTTP 400 Bad Request with validation error details
4. **Given** the order is created, **When** querying the database, **Then** the order record exists with Pending status, creation timestamp, and complete order data

---

### User Story 2 - Retrieve Order Status via API (Priority: P1)

As a customer or system integration, I want to retrieve the current status and details of an order so that I can track its progress through the system.

**Why this priority**: This is equally critical as order creation - users need visibility into their orders at any time. This enables customers to monitor their orders and systems to verify order processing state.

**Independent Test**: Can be fully tested by creating an order, then retrieving it via GET endpoint and verifying all order details and current status are returned correctly.

**Acceptance Scenarios**:

1. **Given** an existing order in the system, **When** a GET request is made with the order ID, **Then** the system returns HTTP 200 with complete order details including current status
2. **Given** multiple orders exist in different states (Pending, Processing, Approved, Rejected, Failed), **When** each is retrieved, **Then** the correct current status is returned for each
3. **Given** a request for a non-existent order ID, **When** the retrieval is attempted, **Then** the system returns HTTP 404 Not Found
4. **Given** an order is retrieved, **When** the response is received, **Then** it includes order ID, total amount, items, creation timestamp, and last updated timestamp

---

### User Story 3 - Worker Processes Order with Amount-Based Approval (Priority: P1)

As the system worker, I want to receive and process orders asynchronously from the message queue so that orders can be evaluated and approved or rejected based on business logic (order total amount threshold).

**Why this priority**: This is the core business logic that defines the system's value. The worker's ability to automatically process orders based on amount thresholds represents the primary business rule and differentiates this from a simple CRUD API.

**Independent Test**: Can be fully tested by publishing an OrderCreatedEvent to SNS, verifying the worker picks up the message, evaluates the order amount, and publishes the appropriate outcome (OrderApprovedEvent or OrderRejectedEvent) based on threshold.

**Acceptance Scenarios**:

1. **Given** the Worker is running and listening to the SQS queue, **When** an OrderCreatedEvent arrives, **Then** the worker processes it and updates the order status to Processing
2. **Given** an order is being processed with total amount below the approval threshold (e.g., €1000), **When** the worker evaluates it, **Then** the order is automatically approved, status changes to Approved, and OrderApprovedEvent is published
3. **Given** an order is being processed with total amount at or above the approval threshold, **When** the worker evaluates it, **Then** the order is rejected, status changes to Rejected, and OrderRejectedEvent is published
4. **Given** a processing error occurs during worker execution (e.g., database unavailable), **When** the worker handles the exception, **Then** the order status is set to Failed and an appropriate error event is published
5. **Given** an order transitions to Approved or Rejected state, **When** the state change occurs, **Then** the last updated timestamp is modified and can be retrieved via API

---

### User Story 4 - View Complete Order Lifecycle (Priority: P2)

As an operations team member, I want to see the complete history of an order's status transitions so that I can understand what happened to any order in the system.

**Why this priority**: While critical for system observability and debugging, this is secondary to the core processing flow. It enables auditing and troubleshooting but doesn't block basic functionality.

**Independent Test**: Can be fully tested by creating an order, allowing it to process through multiple state transitions, then querying the order and verifying all state information is available and correct.

**Acceptance Scenarios**:

1. **Given** an order has been created and processed, **When** the order details are retrieved, **Then** all state information is present: creation time, initial Pending status, transition to Processing time, final status (Approved/Rejected/Failed) and timestamp
2. **Given** an order failed during processing, **When** the order is queried, **Then** the Failed status is shown along with any error context or message
3. **Given** multiple orders exist in various final states, **When** the system is queried for order statistics, **Then** counts of Approved, Rejected, and Failed orders can be determined

---

### User Story 5 - Ensure Reliable Message Delivery (Priority: P2)

As a system architect, I want the messaging infrastructure to reliably deliver order events so that no orders are lost due to infrastructure failures.

**Why this priority**: This is essential for system reliability but is primarily an infrastructure/operational concern. It doesn't represent new user-facing functionality but rather ensures existing functionality works reliably.

**Independent Test**: Can be tested by creating multiple orders rapidly, stopping and restarting the worker, and verifying all orders are eventually processed without duplicates or losses.

**Acceptance Scenarios**:

1. **Given** the Worker service is stopped, **When** new OrderCreatedEvents are published to SNS, **Then** the messages remain in the SQS queue without loss
2. **Given** messages are in the SQS queue, **When** the Worker restarts, **Then** it processes all pending messages from the queue
3. **Given** the Worker is processing a message, **When** the message is successfully processed, **Then** it is acknowledged and removed from the queue
4. **Given** a message causes a processing error, **When** the Worker encounters the error, **Then** the message is not deleted from the queue and can be retried or moved to a dead-letter queue

---

### Edge Cases

- What happens when an order creation request arrives while the database is temporarily unavailable? (System should return appropriate error and allow retry)
- How does the system handle an order with zero or negative total amount? (Validation should reject before processing)
- What happens if the Worker crashes mid-processing after changing status to Processing but before publishing the outcome event? (Order remains in Processing state; Worker restart should handle recovery)
- How does the system behave when SNS or SQS is unavailable? (Order remains in initial state; messaging should retry)
- What happens if an order with duplicate ID is submitted? (System should handle gracefully - either reject as duplicate or return existing order)
- How should the system handle orders with extremely large amounts? (Should be accepted; only threshold comparison matters)
- What happens if OrderCreatedEvent data is malformed or incomplete? (Worker should log error and move to dead-letter queue)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow authenticated users and external systems to create orders via REST API with order items and total amount
- **FR-002**: System MUST validate order requests (required fields, data types, positive amounts) and return validation errors
- **FR-003**: System MUST immediately persist created orders to PostgreSQL with Pending status upon creation
- **FR-004**: System MUST publish an OrderCreatedEvent to SNS immediately after an order is persisted
- **FR-005**: System MUST provide a REST API endpoint to retrieve order details by order ID, including all order data and current status
- **FR-006**: System MUST return HTTP 404 when requesting a non-existent order
- **FR-007**: System MUST implement a Worker service that listens to orders on the SQS queue (subscribed to the SNS topic)
- **FR-008**: System MUST transition orders to Processing status when the Worker begins evaluation
- **FR-009**: System MUST implement amount-based approval logic: if order total is below the approval threshold (configurable, default €1000), automatically approve; otherwise, reject
- **FR-010**: System MUST update order status to Approved when approval criteria are met and publish OrderApprovedEvent to SNS
- **FR-011**: System MUST update order status to Rejected when rejection criteria are met and publish OrderRejectedEvent to SNS
- **FR-012**: System MUST update order status to Failed when an exception occurs during processing and publish appropriate error event
- **FR-013**: System MUST maintain order lifecycle state (Pending → Processing → Approved/Rejected/Failed) and prevent invalid state transitions
- **FR-014**: System MUST persist all order status transitions with timestamps for audit trail
- **FR-015**: System MUST use LocalStack SNS/SQS for messaging in development and test environments
- **FR-016**: System MUST use PostgreSQL as the persistent data store for all order data
- **FR-017**: System MUST implement proper error handling and logging at all layers (API, Worker, Database)
- **FR-018**: System MUST handle message failures gracefully with automatic retry using exponential backoff (initial delay 1s, max 5 retries, max delay 10s) before moving to dead-letter queue
- **FR-019**: System MUST define and export OrderCreatedEvent contract in the Shared library for consistency across API and Worker services
- **FR-020**: System MUST define OrderStatus enum in the domain with states: Pending, Processing, Approved, Rejected, Failed
- **FR-021**: System MUST validate orders with standard HTTP status codes (400 for validation errors, 500 for server errors, 5xx retryable)
- **FR-022**: System MUST reject orders with zero or negative amounts at validation layer before persistence
- **FR-023**: Worker MUST implement idempotent message processing to handle potential duplicate messages from SQS
- **FR-024**: API MUST use bearer token authentication for order endpoints with basic validation framework

### Key Entities

- **Order**: Represents a customer order with unique ID, customer information, ordered items, total amount, status, creation timestamp, and last updated timestamp. Has relationships to OrderItems. Status must be one of the OrderStatus enum values.

- **OrderItem**: Represents a line item within an order, including product/item ID, quantity, unit price, and line total. Multiple OrderItems belong to a single Order.

- **OrderCreatedEvent**: Event contract published when an order is created. Contains OrderId, CustomerId, OrderItems, TotalAmount, and CreatedAt. Shared across API and Worker services.

- **OrderApprovedEvent**: Event published when an order is approved by the Worker. Contains OrderId, ApprovedAt, and ApprovedReason.

- **OrderRejectedEvent**: Event published when an order is rejected by the Worker. Contains OrderId, RejectedAt, and RejectionReason.

- **OrderStatus**: Enum defining valid order states: Pending (initial), Processing (being evaluated by Worker), Approved (passed approval), Rejected (failed approval), Failed (error during processing).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create an order via API and receive confirmation within 500ms
- **SC-002**: Created orders are retrievable via API within 100ms
- **SC-003**: 100% of orders created are successfully delivered to the Worker for processing (no message loss)
- **SC-004**: Worker processes orders and transitions them to final state (Approved/Rejected/Failed) within 5 seconds of receiving the message
- **SC-005**: System supports creating and processing at least 100 orders per second without degradation
- **SC-006**: All state transitions are persisted and auditable with timestamps
- **SC-007**: Failed orders can be identified and their error details retrieved for investigation
- **SC-008**: System achieves 99% success rate for orders reaching final state without manual intervention

## Assumptions

- Order creation requests will include all required fields (customer info, items, total amount) and will be pre-validated by the client before submission
- The approval threshold for order amounts will be configured at system startup (default €1000 for the MVP)
- LocalStack will be used for SNS/SQS in development and test environments; production will use AWS services
- PostgreSQL database will be provisioned and accessible with appropriate connection pooling configured
- Authentication/authorization for API endpoints will be handled by existing mechanisms or basic auth for MVP
- Orders are for a single customer per request; multi-customer orders are out of scope
- Order items are pre-defined products; no product catalog management is in scope
- Worker processes orders sequentially per queue consumer; horizontal scaling of workers is assumed to be managed by deployment infrastructure
- Event publishing failures will be retried automatically by SNS/SQS; custom retry logic is not required

## Clarification Decisions

### Retry Policy for Worker Message Processing
- **Decision**: Implement exponential backoff retry strategy with configurable parameters
- **Details**: Initial delay of 1 second, maximum 5 retries, maximum delay of 10 seconds
- **Rationale**: Balances reliability for transient failures with operational clarity and prevents infinite retry loops
- **Implementation**: Use Polly library for resilience patterns; move messages to dead-letter queue after max retries exhausted

### Error Handling & HTTP Status Codes
- **Decision**: Differentiate between client errors (4xx) and server errors (5xx)
- **Details**: 
  - `400 Bad Request`: Validation failures (missing fields, invalid data types, zero/negative amounts)
  - `404 Not Found`: Order does not exist
  - `500 Internal Server Error`: Unrecoverable server errors
  - `503 Service Unavailable`: Temporary infrastructure issues (database, messaging)
- **Rationale**: Enables client systems to handle errors appropriately (retry vs. user correction)

### Message Idempotency
- **Decision**: Implement idempotent message processing in Worker
- **Details**: Use message ID-based deduplication; track processed messages to prevent duplicate processing
- **Rationale**: SQS may deliver messages more than once; system must handle gracefully without data corruption

### Data Validation
- **Decision**: Multi-layer validation with early rejection
- **Details**: 
  - Request validation: Check required fields, data types
  - Business validation: Reject zero/negative amounts, excessive order sizes
  - Database constraints: Enforce at database level as safety net
- **Rationale**: Prevents invalid data from entering system and reduces worker processing errors

### Authentication & Authorization
- **Decision**: Implement bearer token authentication for MVP
- **Details**: Simple token validation; extensible design for future OAuth2 migration
- **Rationale**: Secure by default while maintaining implementation simplicity for MVP

