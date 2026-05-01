# Event Contracts: Order Processing System

**Date**: 2026-05-01  
**Version**: 1.0.0  
**Status**: Complete

## Overview

This document defines the event contracts published to AWS SNS for the ES2-SistemaPedidos order processing system. Events enable asynchronous communication between the API and Worker services.

All events are published to the SNS topic: `OrderEvents`

---

## Event Envelope

Every event follows this envelope structure:

```json
{
  "MessageId": "aws-message-id-uuid",
  "TopicArn": "arn:aws:sns:us-east-1:000000000000:OrderEvents",
  "Message": "{ /* event payload */ }",
  "Timestamp": "2026-05-01T10:30:00Z",
  "SignatureVersion": "1",
  "Signature": "...",
  "SigningCertUrl": "...",
  "UnsubscribeUrl": "..."
}
```

The `Message` field contains the actual event payload (JSON string).

---

## Event Types

### 1. OrderCreatedEvent

**Topic**: `OrderEvents`  
**Publisher**: API Service  
**Subscriber**: Worker Service  
**Reliability**: Must be delivered at least once

#### Purpose

Published immediately after an order is persisted to the database. Signals the Worker to begin processing the order.

#### Payload

```json
{
  "eventId": "evt-550e8400-e29b-41d4-a716-446655440000",
  "eventType": "OrderCreated",
  "version": "1.0.0",
  "publishedAt": "2026-05-01T10:30:00Z",
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "customerId": "CUST-001",
  "totalAmount": 750.00,
  "currency": "EUR",
  "items": [
    {
      "productId": "PROD-A",
      "quantity": 2,
      "unitPrice": 300.00,
      "lineTotal": 600.00,
      "description": "Premium Widget"
    },
    {
      "productId": "PROD-B",
      "quantity": 1,
      "unitPrice": 150.00,
      "lineTotal": 150.00,
      "description": "Standard Widget"
    }
  ],
  "correlationId": "corr-12345",
  "source": "es2-api",
  "metadata": {
    "environment": "development",
    "apiVersion": "1.0.0"
  }
}
```

#### Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `eventId` | string (UUID) | ✅ | Unique event identifier |
| `eventType` | string | ✅ | Literal: `"OrderCreated"` |
| `version` | string | ✅ | Event schema version (e.g., "1.0.0") |
| `publishedAt` | ISO 8601 datetime | ✅ | When event was published (UTC) |
| `orderId` | UUID | ✅ | The order being created |
| `customerId` | string | ✅ | Customer identifier |
| `totalAmount` | decimal | ✅ | Total order amount |
| `currency` | string | ✅ | Currency code (default: "EUR") |
| `items` | array | ✅ | Order line items (at least 1) |
| `items[].productId` | string | ✅ | Product identifier |
| `items[].quantity` | integer | ✅ | Units ordered |
| `items[].unitPrice` | decimal | ✅ | Price per unit |
| `items[].lineTotal` | decimal | ✅ | quantity × unitPrice |
| `items[].description` | string | ❌ | Optional item description |
| `correlationId` | string | ❌ | Trace ID for distributed tracing |
| `source` | string | ❌ | Publishing service identifier |
| `metadata` | object | ❌ | Additional context (environment, version, etc.) |

#### Retry Policy

- **Max Attempts**: 5
- **Initial Delay**: 1 second
- **Backoff**: Exponential (2^(attempt-1))
- **Max Delay**: 10 seconds
- **Dead-Letter Queue**: After 5 failed attempts

#### Processing Rules (Worker)

1. Extract MessageId from SNS envelope
2. Check if MessageId already processed (idempotency)
3. If yes: Log and skip (message already handled)
4. If no:
   - Update order status: Pending → Processing
   - Evaluate amount against threshold (€1000)
   - If totalAmount < €1000: Publish OrderApprovedEvent
   - If totalAmount ≥ €1000: Publish OrderRejectedEvent
   - Record MessageId in ProcessedMessages table
5. On error: Publish OrderProcessingFailedEvent; retry via SNS/SQS

---

### 2. OrderApprovedEvent

**Topic**: `OrderEvents`  
**Publisher**: Worker Service  
**Subscriber**: API Service (for audit), external systems (optional)  
**Reliability**: Must be delivered at least once

#### Purpose

Published when an order passes the approval threshold check and is automatically approved.

#### Payload

```json
{
  "eventId": "evt-550e8400-e29b-41d4-a716-446655440001",
  "eventType": "OrderApproved",
  "version": "1.0.0",
  "publishedAt": "2026-05-01T10:30:05Z",
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "customerId": "CUST-001",
  "approvalReason": "Amount below threshold (€750 < €1000)",
  "approverService": "worker-service",
  "thresholdAmount": 1000.00,
  "actualAmount": 750.00,
  "correlationId": "corr-12345",
  "source": "es2-worker",
  "metadata": {
    "processingTime": "1250ms",
    "workerId": "worker-1",
    "environment": "development"
  }
}
```

#### Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `eventId` | string (UUID) | ✅ | Unique event identifier |
| `eventType` | string | ✅ | Literal: `"OrderApproved"` |
| `version` | string | ✅ | Event schema version |
| `publishedAt` | ISO 8601 datetime | ✅ | When event was published (UTC) |
| `orderId` | UUID | ✅ | The approved order |
| `customerId` | string | ✅ | Customer identifier |
| `approvalReason` | string | ✅ | Human-readable reason for approval |
| `approverService` | string | ✅ | Service that approved (e.g., "worker-service") |
| `thresholdAmount` | decimal | ❌ | Approval threshold used |
| `actualAmount` | decimal | ❌ | Order amount |
| `correlationId` | string | ❌ | Trace ID |
| `source` | string | ❌ | Publishing service identifier |
| `metadata` | object | ❌ | Processing metrics, worker ID, etc. |

#### Processing Rules (API)

1. Update order status: Processing → Approved
2. Set completed_at timestamp
3. Store approval_reason
4. Publish confirmation event (optional future)
5. Return to client via GET endpoint

---

### 3. OrderRejectedEvent

**Topic**: `OrderEvents`  
**Publisher**: Worker Service  
**Subscriber**: API Service (for audit), external systems (optional)  
**Reliability**: Must be delivered at least once

#### Purpose

Published when an order fails the approval threshold check and is rejected.

#### Payload

```json
{
  "eventId": "evt-550e8400-e29b-41d4-a716-446655440002",
  "eventType": "OrderRejected",
  "version": "1.0.0",
  "publishedAt": "2026-05-01T10:30:05Z",
  "orderId": "550e8400-e29b-41d4-a716-446655440001",
  "customerId": "CUST-002",
  "rejectionReason": "Amount exceeds threshold (€2500 >= €1000)",
  "rejecterService": "worker-service",
  "thresholdAmount": 1000.00,
  "actualAmount": 2500.00,
  "requiresManualReview": true,
  "correlationId": "corr-12346",
  "source": "es2-worker",
  "metadata": {
    "processingTime": "892ms",
    "workerId": "worker-2",
    "environment": "development"
  }
}
```

#### Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `eventId` | string (UUID) | ✅ | Unique event identifier |
| `eventType` | string | ✅ | Literal: `"OrderRejected"` |
| `version` | string | ✅ | Event schema version |
| `publishedAt` | ISO 8601 datetime | ✅ | When event was published (UTC) |
| `orderId` | UUID | ✅ | The rejected order |
| `customerId` | string | ✅ | Customer identifier |
| `rejectionReason` | string | ✅ | Human-readable reason for rejection |
| `rejecterService` | string | ✅ | Service that rejected (e.g., "worker-service") |
| `thresholdAmount` | decimal | ❌ | Approval threshold used |
| `actualAmount` | decimal | ❌ | Order amount |
| `requiresManualReview` | boolean | ❌ | Flag for operations team (default: false) |
| `correlationId` | string | ❌ | Trace ID |
| `source` | string | ❌ | Publishing service identifier |
| `metadata` | object | ❌ | Processing metrics, worker ID, etc. |

#### Processing Rules (API)

1. Update order status: Processing → Rejected
2. Set completed_at timestamp
3. Store rejection_reason
4. If requiresManualReview=true: Flag for operations team alert
5. Return to client via GET endpoint

---

### 4. OrderProcessingFailedEvent

**Topic**: `OrderEvents`  
**Publisher**: Worker Service  
**Subscriber**: API Service (for audit), monitoring systems  
**Reliability**: Best effort (sent after exception handling)

#### Purpose

Published when the Worker encounters an error during order processing. Indicates the order status should be set to Failed.

#### Payload

```json
{
  "eventId": "evt-550e8400-e29b-41d4-a716-446655440003",
  "eventType": "OrderProcessingFailed",
  "version": "1.0.0",
  "publishedAt": "2026-05-01T10:30:15Z",
  "orderId": "550e8400-e29b-41d4-a716-446655440002",
  "customerId": "CUST-003",
  "failureReason": "Database connection timeout",
  "errorCode": "DB_TIMEOUT",
  "errorMessage": "Connection to PostgreSQL timed out after 30 seconds",
  "stackTrace": "at OrderService.GetOrder(...)",
  "retryable": true,
  "correlationId": "corr-12347",
  "source": "es2-worker",
  "metadata": {
    "workerId": "worker-3",
    "attemptNumber": 5,
    "environment": "development"
  }
}
```

#### Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `eventId` | string (UUID) | ✅ | Unique event identifier |
| `eventType` | string | ✅ | Literal: `"OrderProcessingFailed"` |
| `version` | string | ✅ | Event schema version |
| `publishedAt` | ISO 8601 datetime | ✅ | When event was published (UTC) |
| `orderId` | UUID | ✅ | The failed order |
| `customerId` | string | ✅ | Customer identifier |
| `failureReason` | string | ✅ | Human-readable description of failure |
| `errorCode` | string | ✅ | Error classification (DB_TIMEOUT, VALIDATION_ERROR, etc.) |
| `errorMessage` | string | ✅ | Detailed error message |
| `stackTrace` | string | ❌ | Stack trace (only in non-production) |
| `retryable` | boolean | ❌ | Whether Worker will/has retried (default: true) |
| `correlationId` | string | ❌ | Trace ID |
| `source` | string | ❌ | Publishing service identifier |
| `metadata` | object | ❌ | Worker ID, attempt number, etc. |

#### Processing Rules (API)

1. Update order status: Processing → Failed
2. Set completed_at timestamp
3. Store error_message from event
4. Alert operations team if retryable=false (permanent failure)
5. Make available for support investigation via API

---

## Event Publishing

### API Service (OrderCreatedEvent)

```csharp
public class OrderEventPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    
    public async Task PublishOrderCreatedAsync(Order order)
    {
        var orderCreatedEvent = new OrderCreatedEvent
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = "OrderCreated",
            Version = "1.0.0",
            PublishedAt = DateTime.UtcNow,
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Currency = "EUR",
            Items = order.Items.Select(oi => new OrderItemPayload
            {
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                LineTotal = oi.LineTotal,
                Description = oi.Description
            }).ToList(),
            CorrelationId = GetOrCreateCorrelationId(),
            Source = "es2-api"
        };

        var message = JsonSerializer.Serialize(orderCreatedEvent);
        
        var publishRequest = new PublishRequest
        {
            TopicArn = _topicArn,
            Message = message,
            Subject = "Order Created"
        };

        var response = await _snsClient.PublishAsync(publishRequest);
        logger.LogInformation($"Published OrderCreatedEvent: {orderCreatedEvent.EventId}");
    }
}
```

### Worker Service (OrderApprovedEvent, OrderRejectedEvent)

```csharp
public class OrderProcessingService
{
    public async Task ProcessOrderAsync(OrderCreatedEvent createdEvent)
    {
        // Check idempotency
        if (await _messageService.IsProcessedAsync(messageId))
        {
            logger.LogInformation($"Message {messageId} already processed");
            return;
        }

        // Update status to Processing
        var order = await _orderRepository.GetAsync(createdEvent.OrderId);
        order.Status = OrderStatus.Processing;
        order.ProcessingStartedAt = DateTime.UtcNow;
        await _orderRepository.SaveAsync(order);

        try
        {
            // Evaluate amount
            if (createdEvent.TotalAmount < _config.ApprovalThreshold)
            {
                // Approve
                order.Status = OrderStatus.Approved;
                order.CompletedAt = DateTime.UtcNow;
                order.ApprovalReason = $"Amount below threshold (€{createdEvent.TotalAmount} < €{_config.ApprovalThreshold})";
                await _orderRepository.SaveAsync(order);

                await _eventPublisher.PublishOrderApprovedAsync(order, createdEvent);
            }
            else
            {
                // Reject
                order.Status = OrderStatus.Rejected;
                order.CompletedAt = DateTime.UtcNow;
                order.RejectionReason = $"Amount exceeds threshold (€{createdEvent.TotalAmount} >= €{_config.ApprovalThreshold})";
                await _orderRepository.SaveAsync(order);

                await _eventPublisher.PublishOrderRejectedAsync(order, createdEvent);
            }

            // Record in ProcessedMessages
            await _messageService.RecordProcessedAsync(messageId, order.Id, "SUCCESS");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process order");
            
            order.Status = OrderStatus.Failed;
            order.CompletedAt = DateTime.UtcNow;
            order.ErrorMessage = ex.Message;
            await _orderRepository.SaveAsync(order);

            await _eventPublisher.PublishOrderProcessingFailedAsync(order, ex);
            
            // Message will be retried by SQS/Polly; after max retries, moved to DLQ
            throw;
        }
    }
}
```

---

## Event Subscription

### SQS Queue Configuration

The Worker subscribes to events via an SQS queue subscribed to the SNS topic:

```
SNS Topic: OrderEvents
  ↓ (publish to)
SQS Queue: OrderProcessingQueue (subscribed with filter policy)
  ↓ (consumed by)
Worker Service
```

### Queue Attributes

- **Queue Name**: `OrderProcessingQueue`
- **Message Retention**: 14 days
- **Visibility Timeout**: 300 seconds (5 minutes)
- **Dead-Letter Queue**: `OrderProcessingQueue-dlq`
- **DLQ Max Receive Count**: 5 (before moving to DLQ)

### Filter Policy (Optional)

Subscribe only to relevant events:

```json
{
  "eventType": ["OrderCreated"]
}
```

---

## Event Ordering & Idempotency

### Ordering Guarantees

- **Single Topic, Single Subscriber**: FIFO ordered per SQS queue
- **Multiple Workers**: No ordering guarantee across workers (OK for current design)

### Idempotency Implementation

Worker ensures exactly-once processing:

1. **Before Processing**:
   ```sql
   SELECT * FROM processed_messages WHERE message_id = $1
   ```
   If exists: Skip processing

2. **During Processing**:
   - Update order status to Processing
   - Evaluate and update to final status
   - Record in database within same transaction

3. **After Processing**:
   ```sql
   INSERT INTO processed_messages (message_id, order_id, message_type, status)
   VALUES ($1, $2, $3, $4)
   ```

---

## Event Format Examples

### OrderCreatedEvent (Full Example)

```json
{
  "eventId": "evt-a1b2c3d4-e5f6-47a8-b9c0-d1e2f3a4b5c6",
  "eventType": "OrderCreated",
  "version": "1.0.0",
  "publishedAt": "2026-05-01T10:30:00.000Z",
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "customerId": "CUST-001",
  "totalAmount": 750.00,
  "currency": "EUR",
  "items": [
    {
      "productId": "PROD-A",
      "quantity": 2,
      "unitPrice": 300.00,
      "lineTotal": 600.00,
      "description": "Premium Widget"
    },
    {
      "productId": "PROD-B",
      "quantity": 1,
      "unitPrice": 150.00,
      "lineTotal": 150.00,
      "description": "Standard Widget"
    }
  ],
  "correlationId": "corr-req-12345",
  "source": "es2-api",
  "metadata": {
    "environment": "development",
    "apiVersion": "1.0.0",
    "timestamp": "2026-05-01T10:30:00.000Z"
  }
}
```

---

## Event Versioning Strategy

### Current Version: 1.0.0

**Versioning Rules**:
- MAJOR.MINOR.PATCH
- MAJOR: Breaking changes (new required fields, changed field types)
- MINOR: Backward-compatible additions (new optional fields)
- PATCH: Bug fixes, no schema changes

### Forward Compatibility

New fields added as optional; old consumers ignore unknown fields:

```csharp
// Example: OrderCreatedEvent v1.0.0 adds optional "discountApplied" field in v1.1.0
[JsonPropertyName("discountApplied")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public decimal? DiscountApplied { get; set; }
```

### Breaking Changes (Future)

For breaking changes, create new event types:
- OrderCreatedEvent_v2 (instead of modifying OrderCreatedEvent)
- Both published to topic
- Consumers upgrade incrementally

---

## Error Handling

### Dead-Letter Queue Processing

Messages moved to DLQ after 5 failed processing attempts:

```sql
SELECT * FROM messages_in_dlq 
ORDER BY received_at DESC 
LIMIT 100;
```

Operations team:
1. Investigate error (check logs, metrics)
2. Fix underlying issue (database, service)
3. Replay messages from DLQ manually or via admin API

### Monitoring & Alerting

Track metrics:
- Events published per minute
- Events processed successfully
- Events moved to DLQ
- Processing latency (p50, p95, p99)
- Errors by type

Alert on:
- > 1% DLQ failure rate
- Processing latency > 10 seconds
- > 10 messages in DLQ

---

## Testing

### Unit Tests

```csharp
[Fact]
public async Task OrderCreatedEvent_PublishedCorrectly()
{
    // Arrange
    var order = CreateTestOrder();
    var eventPublisher = new OrderEventPublisher(_mockSnsClient);

    // Act
    await eventPublisher.PublishOrderCreatedAsync(order);

    // Assert
    _mockSnsClient.Verify(c => c.PublishAsync(
        It.Is<PublishRequest>(r => r.Message.Contains("OrderCreated")),
        It.IsAny<CancellationToken>()
    ));
}
```

### Integration Tests

Publish to LocalStack SNS and verify SQS delivery

---

## Migration Path

### Version 1.0.0 → 2.0.0 (Future)

1. Deploy Worker v2 (handles both v1 and v2 events)
2. Deploy API v2 (publishes v2 events)
3. Monitor for v1 event processing completion
4. Decommission v1 event handling

---

## References

- AWS SNS: https://docs.aws.amazon.com/sns/
- AWS SQS: https://docs.aws.amazon.com/sqs/
- AWS SDK for .NET: https://github.com/aws/aws-sdk-net

