# Data Model: Order Processing System

**Date**: 2026-05-01  
**Status**: Complete  
**Spec Reference**: `/specs/002-order-processing-system/spec.md`

## Overview

This document defines the complete database schema, entity relationships, and validation rules for the ES2-SistemaPedidos order processing system. The model supports the order lifecycle from creation through processing to final state (Approved/Rejected/Failed).

---

## Entity Definitions

### 1. OrderStatus Enum

Defines valid order states. Located in Shared library: `ES2-SistemaPedidos.Shared/Domain/OrderStatus.cs`

```csharp
public enum OrderStatus
{
    /// <summary>Order created; awaiting processing</summary>
    Pending = 0,
    
    /// <summary>Order being evaluated by Worker</summary>
    Processing = 1,
    
    /// <summary>Order approved by Worker (amount below threshold)</summary>
    Approved = 2,
    
    /// <summary>Order rejected by Worker (amount at/above threshold)</summary>
    Rejected = 3,
    
    /// <summary>Error during Worker processing</summary>
    Failed = 4
}
```

**Allowed Transitions**:
- Pending → Processing (Worker starts evaluation)
- Processing → Approved (Threshold criteria met)
- Processing → Rejected (Threshold criteria not met)
- Processing → Failed (Error during evaluation)
- Any state → Failed (System error)

**Invalid Transitions** (must be prevented at application layer):
- Pending → Approved (must go through Processing)
- Pending → Rejected (must go through Processing)
- Approved → Pending (terminal state)
- Rejected → Pending (terminal state)
- Failed → * (terminal state)

---

### 2. Order Entity

Core order aggregate root. Represents a customer's order with items, status, and lifecycle.

**Table Name**: `orders`

**Columns**:

| Column | Type | Nullable | Default | Constraints | Description |
|--------|------|----------|---------|-----------|-------------|
| `id` | UUID | No | gen_random_uuid() | PK | Unique order identifier |
| `customer_id` | VARCHAR(255) | No | - | Not null | Customer identifier (from request) |
| `total_amount` | DECIMAL(19,2) | No | - | ≥ 0, Check | Total order amount in EUR |
| `status` | SMALLINT | No | 0 (Pending) | Enum | Current order status |
| `created_at` | TIMESTAMP | No | now() | Not null | Creation timestamp |
| `updated_at` | TIMESTAMP | No | now() | Not null | Last update timestamp |
| `processing_started_at` | TIMESTAMP | Yes | NULL | - | When Worker started processing |
| `completed_at` | TIMESTAMP | Yes | NULL | - | When order reached terminal state |
| `error_message` | TEXT | Yes | NULL | - | Error details if status=Failed |
| `approval_reason` | TEXT | Yes | NULL | - | Reason for approval (informational) |
| `rejection_reason` | TEXT | Yes | NULL | - | Reason for rejection (informational) |

**Indexes**:
- Primary Key: `id`
- Index: `customer_id` (for queries by customer)
- Index: `status` (for filtering by status)
- Index: `created_at` (for time-range queries)
- Index: `(status, updated_at)` (for "recent orders" queries)

**Constraints**:
- Check: `total_amount >= 0` (reject zero/negative)
- Check: `status` in (0, 1, 2, 3, 4)
- Check: `completed_at IS NULL OR status IN (2, 3, 4)` (completed_at only for terminal states)
- Unique: None (multiple orders per customer allowed)

**Example Data**:
```sql
INSERT INTO orders (id, customer_id, total_amount, status, created_at, updated_at)
VALUES ('550e8400-e29b-41d4-a716-446655440000', 'CUST-001', 750.00, 0, now(), now());
-- Pending order for €750

INSERT INTO orders (id, customer_id, total_amount, status, processing_started_at, completed_at, created_at, updated_at)
VALUES ('550e8400-e29b-41d4-a716-446655440001', 'CUST-002', 2500.00, 3, now() - '5 minutes'::interval, now(), now() - '10 minutes'::interval, now());
-- Rejected order for €2500
```

---

### 3. OrderItem Entity

Represents line items within an order. Belongs to exactly one Order.

**Table Name**: `order_items`

**Columns**:

| Column | Type | Nullable | Default | Constraints | Description |
|--------|------|----------|---------|-----------|-------------|
| `id` | UUID | No | gen_random_uuid() | PK | Unique item identifier |
| `order_id` | UUID | No | - | FK → orders(id) | Reference to parent order |
| `product_id` | VARCHAR(255) | No | - | Not null | Product/item identifier |
| `quantity` | INT | No | - | > 0, Check | Units ordered |
| `unit_price` | DECIMAL(19,2) | No | - | ≥ 0, Check | Price per unit in EUR |
| `line_total` | DECIMAL(19,2) | No | - | ≥ 0, Check | quantity × unit_price |
| `description` | TEXT | Yes | NULL | - | Optional item description |

**Indexes**:
- Primary Key: `id`
- Foreign Key: `order_id` (with ON DELETE CASCADE)
- Index: `order_id` (for order line-item queries)

**Constraints**:
- Foreign Key: `order_id` references `orders(id)` with cascade delete
- Check: `quantity > 0`
- Check: `unit_price >= 0`
- Check: `line_total >= 0`
- Check: `line_total = quantity * unit_price` (data integrity)

**Cascading Behavior**:
- When order deleted, all order_items automatically deleted (ON DELETE CASCADE)

**Example Data**:
```sql
INSERT INTO order_items (id, order_id, product_id, quantity, unit_price, line_total, description)
VALUES 
  ('550e8400-e29b-41d4-a716-446655440010', '550e8400-e29b-41d4-a716-446655440000', 'PROD-A', 2, 300.00, 600.00, 'Premium Widget'),
  ('550e8400-e29b-41d4-a716-446655440011', '550e8400-e29b-41d4-a716-446655440000', 'PROD-B', 1, 150.00, 150.00, 'Standard Widget');
-- Total: €750
```

---

### 4. ProcessedMessages Table (Idempotency)

Tracks messages already processed to ensure idempotent message handling by Worker.

**Table Name**: `processed_messages`

**Columns**:

| Column | Type | Nullable | Default | Constraints | Description |
|--------|------|----------|---------|-----------|-------------|
| `message_id` | VARCHAR(255) | No | - | PK | AWS message ID |
| `order_id` | UUID | Yes | NULL | FK → orders(id) | Related order (if applicable) |
| `processed_at` | TIMESTAMP | No | now() | Not null | When message was processed |
| `message_type` | VARCHAR(100) | No | - | Not null | Type of message (e.g., OrderCreated) |
| `status` | VARCHAR(20) | No | 'SUCCESS' | Not null | Processing result |
| `error_details` | TEXT | Yes | NULL | - | Error message if status=FAILURE |

**Indexes**:
- Primary Key: `message_id`
- Index: `(order_id, processed_at)` (for order audit)
- Index: `(message_type, processed_at)` (for analytics)

**Constraints**:
- Foreign Key: `order_id` references `orders(id)` (optional)
- Check: `status IN ('SUCCESS', 'FAILURE', 'DLQ')`

---

### 5. Event Audit Table (Optional, for P2)

Stores published events for audit trail and replay capability. (Phase 3+)

**Table Name**: `published_events` (optional, for full audit trail)

**Columns**:

| Column | Type | Nullable | Default | Constraints | Description |
|--------|------|----------|---------|-----------|-------------|
| `id` | UUID | No | gen_random_uuid() | PK | Event record ID |
| `event_id` | VARCHAR(255) | No | - | Unique | AWS message ID from SNS |
| `order_id` | UUID | No | - | FK → orders(id) | Related order |
| `event_type` | VARCHAR(100) | No | - | Not null | Type (OrderCreated, Approved, etc.) |
| `event_payload` | JSONB | No | - | Not null | Full event data as JSON |
| `published_at` | TIMESTAMP | No | now() | Not null | When event published |

---

## Entity Relationships

### Order ↔ OrderItem (One-to-Many)
```
Order (1) ──── (Many) OrderItem
├─ Cardinality: One order contains zero or more items
├─ Constraint: ON DELETE CASCADE (order deletion removes items)
└─ Query Pattern: "SELECT * FROM order_items WHERE order_id = ?"
```

### Order ↔ ProcessedMessages (Many-to-One)
```
ProcessedMessages (Many) ──── (1) Order
├─ Relationship: Messages processed for an order
├─ Constraint: Optional (some messages may not relate to orders)
└─ Purpose: Idempotency tracking
```

---

## Validation Rules

### Order Creation Request Validation

**Required Fields**:
- `customer_id`: Non-empty string, max 255 characters
- `total_amount`: Decimal ≥ 0.01
- `items`: Array with 1+ items

**Order-Level Validations**:
1. **Amount Validation**:
   - Must be positive (> 0)
   - Must be ≤ 999,999.99 (2-digit decimal support)
   - Must equal sum of line items (within 0.01 rounding tolerance)

2. **Items Validation**:
   - At least 1 item required
   - Max 1000 items per order
   - Total items quantity < 10,000 units

3. **Item-Level Validations**:
   - `product_id`: Non-empty, max 255 characters
   - `quantity`: Integer > 0, ≤ 10,000
   - `unit_price`: Decimal ≥ 0, ≤ 999,999.99
   - `line_total`: Matches quantity × unit_price (±0.01)

### Status Transition Validation

**Transition Rules** (enforced at application layer):

| From | To | Allowed | Condition |
|------|----|---------|-----------| 
| Pending | Processing | ✅ | Worker starts processing |
| Processing | Approved | ✅ | total_amount < threshold (€1000) |
| Processing | Rejected | ✅ | total_amount >= threshold (€1000) |
| Processing | Failed | ✅ | Error occurred during processing |
| Any | Failed | ✅ | System error; emergency fallback |
| Pending | Approved | ❌ | Must go through Processing |
| Pending | Rejected | ❌ | Must go through Processing |
| Approved | * | ❌ | Terminal state |
| Rejected | * | ❌ | Terminal state |
| Failed | * | ❌ | Terminal state |

---

## Database Schema SQL

### Create Tables

```sql
-- Enable UUID support
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Orders table
CREATE TABLE orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id VARCHAR(255) NOT NULL,
    total_amount DECIMAL(19,2) NOT NULL,
    status SMALLINT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    updated_at TIMESTAMP NOT NULL DEFAULT now(),
    processing_started_at TIMESTAMP,
    completed_at TIMESTAMP,
    error_message TEXT,
    approval_reason TEXT,
    rejection_reason TEXT,
    
    -- Constraints
    CONSTRAINT check_total_amount_positive CHECK (total_amount >= 0),
    CONSTRAINT check_valid_status CHECK (status IN (0, 1, 2, 3, 4)),
    CONSTRAINT check_completed_only_terminal 
        CHECK (completed_at IS NULL OR status IN (2, 3, 4))
);

-- Indexes for order queries
CREATE INDEX idx_orders_customer_id ON orders(customer_id);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_orders_created_at ON orders(created_at);
CREATE INDEX idx_orders_status_updated ON orders(status, updated_at);

-- OrderItems table
CREATE TABLE order_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL,
    product_id VARCHAR(255) NOT NULL,
    quantity INT NOT NULL,
    unit_price DECIMAL(19,2) NOT NULL,
    line_total DECIMAL(19,2) NOT NULL,
    description TEXT,
    
    -- Foreign key
    CONSTRAINT fk_order_items_order FOREIGN KEY (order_id) 
        REFERENCES orders(id) ON DELETE CASCADE,
    
    -- Constraints
    CONSTRAINT check_quantity_positive CHECK (quantity > 0),
    CONSTRAINT check_unit_price_non_negative CHECK (unit_price >= 0),
    CONSTRAINT check_line_total_non_negative CHECK (line_total >= 0),
    CONSTRAINT check_line_total_calculation 
        CHECK (ABS(line_total - (quantity * unit_price)) < 0.01)
);

-- Indexes for order_items queries
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
CREATE INDEX idx_order_items_product_id ON order_items(product_id);

-- ProcessedMessages table (for idempotency)
CREATE TABLE processed_messages (
    message_id VARCHAR(255) PRIMARY KEY,
    order_id UUID REFERENCES orders(id) ON DELETE SET NULL,
    processed_at TIMESTAMP NOT NULL DEFAULT now(),
    message_type VARCHAR(100) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'SUCCESS',
    error_details TEXT,
    
    -- Constraints
    CONSTRAINT check_valid_status CHECK (status IN ('SUCCESS', 'FAILURE', 'DLQ'))
);

-- Indexes for processed_messages queries
CREATE INDEX idx_processed_messages_order_id ON processed_messages(order_id, processed_at);
CREATE INDEX idx_processed_messages_type_date ON processed_messages(message_type, processed_at);

-- PublishedEvents table (optional, Phase 3+)
CREATE TABLE published_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id VARCHAR(255) NOT NULL UNIQUE,
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    event_type VARCHAR(100) NOT NULL,
    event_payload JSONB NOT NULL,
    published_at TIMESTAMP NOT NULL DEFAULT now()
);

CREATE INDEX idx_published_events_order_id ON published_events(order_id);
CREATE INDEX idx_published_events_type_date ON published_events(event_type, published_at);
```

### Create Indexes for Performance

```sql
-- Fast lookups by customer
CREATE INDEX idx_orders_customer_status ON orders(customer_id, status);

-- Recent orders query optimization
CREATE INDEX idx_orders_recent ON orders(created_at DESC) 
    WHERE status IN (0, 1);

-- Terminal orders query
CREATE INDEX idx_orders_completed ON orders(completed_at DESC) 
    WHERE completed_at IS NOT NULL;
```

---

## Migration Strategy

### Phase 1: Initial Schema
Deploy tables: `orders`, `order_items`, `processed_messages`

### Phase 2: Audit Trail
- Add columns to orders: `processing_started_at`, `completed_at`, `approval_reason`, `rejection_reason`
- Add indexes for status transition queries

### Phase 3: Event Audit (Optional)
Deploy table: `published_events` with JSONB payload storage

### Migration Management
- Use Entity Framework Core Code-First Migrations
- Store in: `ES2-SistemaPedidos.Shared/Data/Migrations/`
- Naming: `AddOrders_InitialSchema.cs`, `AddOrderItems_ForeignKey.cs`, etc.
- Each migration can be tested independently
- Rollback support via EF migrations

---

## Entity Framework Core Configuration

### DbContext Definition (Shared project)

```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<ProcessedMessage> ProcessedMessages { get; set; }
    public DbSet<PublishedEvent> PublishedEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Order entity configuration
        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.CustomerId).IsRequired().HasMaxLength(255);
            builder.Property(o => o.TotalAmount).HasPrecision(19, 2);
            builder.Property(o => o.Status).HasConversion<int>();
            
            // Relationships
            builder.HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(o => o.CustomerId);
            builder.HasIndex(o => o.Status);
            builder.HasIndex(o => o.CreatedAt);
            builder.HasIndex(o => new { o.Status, o.UpdatedAt });
        });

        // OrderItem entity configuration
        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.HasKey(oi => oi.Id);
            builder.Property(oi => oi.ProductId).IsRequired().HasMaxLength(255);
            builder.Property(oi => oi.Quantity).IsRequired();
            builder.Property(oi => oi.UnitPrice).HasPrecision(19, 2);
            builder.Property(oi => oi.LineTotal).HasPrecision(19, 2);
            
            // Index
            builder.HasIndex(oi => oi.OrderId);
        });

        // ProcessedMessage entity configuration
        modelBuilder.Entity<ProcessedMessage>(builder =>
        {
            builder.HasKey(pm => pm.MessageId);
            builder.Property(pm => pm.MessageType).IsRequired().HasMaxLength(100);
            builder.Property(pm => pm.Status).IsRequired().HasMaxLength(20);
            
            // Indexes
            builder.HasIndex(pm => new { pm.OrderId, pm.ProcessedAt });
            builder.HasIndex(pm => new { pm.MessageType, pm.ProcessedAt });
        });
    }
}
```

---

## Querying Patterns

### Find Order by ID
```csharp
var order = await dbContext.Orders
    .Include(o => o.Items)
    .FirstOrDefaultAsync(o => o.Id == orderId);
```

### Get Orders by Customer with Pagination
```csharp
var orders = await dbContext.Orders
    .Where(o => o.CustomerId == customerId)
    .OrderByDescending(o => o.CreatedAt)
    .Skip(skip)
    .Take(take)
    .ToListAsync();
```

### Find Pending Orders for Worker
```csharp
var pendingOrders = await dbContext.Orders
    .Where(o => o.Status == OrderStatus.Pending)
    .OrderBy(o => o.CreatedAt)
    .Take(100)
    .ToListAsync();
```

### Get Order Statistics
```csharp
var stats = await dbContext.Orders
    .Where(o => o.CompletedAt >= DateTime.UtcNow.AddDays(-1))
    .GroupBy(o => o.Status)
    .Select(g => new { Status = g.Key, Count = g.Count() })
    .ToListAsync();
```

### Check Message Idempotency
```csharp
var isProcessed = await dbContext.ProcessedMessages
    .AnyAsync(pm => pm.MessageId == messageId);
```

---

## Data Consistency Rules

### Invariants (Must Always Be True)

1. **Order Integrity**:
   - Every order has a non-empty customer_id
   - total_amount ≥ 0
   - status is valid enum value
   - updated_at ≥ created_at

2. **Order-Item Relationship**:
   - Every OrderItem references exactly one Order
   - Sum of OrderItem line_totals ≈ Order total_amount (within 0.01)
   - Deleting order cascades to items

3. **State Machine**:
   - Only allowed transitions occur
   - Terminal states (Approved, Rejected, Failed) are immutable
   - completed_at is set only for terminal states

4. **Message Processing**:
   - Each message processed at most once
   - ProcessedMessages record created before order status updated
   - Failed messages moved to DLQ after 5 retries

### Concurrency Handling

- Use optimistic locking via `updated_at` timestamp
- EF Core will include in WHERE clause for updates
- Retry logic on concurrency exception (rare)

---

## Backup & Recovery

### Data Backup Strategy
- PostgreSQL continuous archiving (WAL)
- Daily snapshots to object storage
- Point-in-time recovery capability

### Disaster Recovery Plan
1. Orders immutable once terminal state reached
2. Published events stored for replay
3. Message IDs tracked for deduplication

---

## Performance Considerations

### Query Optimization
- Indexes on foreign keys, status, created_at
- Composite index on (status, updated_at) for recent orders
- Avoid SELECT * queries; specify needed columns

### Scaling Strategies
- Connection pooling: 20 connections (configurable)
- Read replicas for analytics queries
- Archival of old completed orders to separate table

### Data Retention Policy
- Keep all orders indefinitely (audit trail)
- Purge processed_messages after 30 days (cleanup table)
- Archive published_events after 90 days

