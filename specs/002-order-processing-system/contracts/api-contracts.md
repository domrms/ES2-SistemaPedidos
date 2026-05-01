# API Contracts: Order Processing System

**Date**: 2026-05-01  
**Version**: 1.0.0  
**Status**: Complete

## Overview

This document defines the REST API contracts for the ES2-SistemaPedidos order processing system. All endpoints require bearer token authentication.

---

## Authentication

All API requests must include a Bearer token in the Authorization header:

```
Authorization: Bearer {token}
```

**Response if missing or invalid**:
```
HTTP/1.1 401 Unauthorized
Content-Type: application/json

{
  "error": "Unauthorized",
  "message": "Missing or invalid Bearer token"
}
```

---

## Base URL

- **Development**: `http://localhost:5000`
- **Production**: `https://api.es2-pedidos.example.com`

---

## Endpoints

### 1. Create Order

**POST** `/api/orders`

Creates a new order with items. Publishes OrderCreatedEvent to SNS.

#### Request

```http
POST /api/orders HTTP/1.1
Authorization: Bearer {token}
Content-Type: application/json

{
  "customerId": "CUST-001",
  "items": [
    {
      "productId": "PROD-A",
      "quantity": 2,
      "unitPrice": 300.00,
      "description": "Premium Widget"
    },
    {
      "productId": "PROD-B",
      "quantity": 1,
      "unitPrice": 150.00,
      "description": "Standard Widget"
    }
  ],
  "totalAmount": 750.00
}
```

#### Request Schema

| Field | Type | Required | Constraints | Description |
|-------|------|----------|-----------|-------------|
| `customerId` | string | ✅ | Max 255 chars, non-empty | Customer identifier |
| `items` | array | ✅ | 1-1000 items | Order line items |
| `items[].productId` | string | ✅ | Max 255 chars, non-empty | Product identifier |
| `items[].quantity` | integer | ✅ | > 0, ≤ 10,000 | Units ordered |
| `items[].unitPrice` | decimal | ✅ | ≥ 0, ≤ 999,999.99 | Price per unit (EUR) |
| `items[].description` | string | ❌ | Max 500 chars | Optional item description |
| `totalAmount` | decimal | ✅ | > 0, ≤ 999,999.99 | Must match sum of line items ±0.01 |

#### Success Response

```http
HTTP/1.1 201 Created
Content-Type: application/json
Location: /api/orders/{orderId}

{
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "customerId": "CUST-001",
  "status": "Pending",
  "totalAmount": 750.00,
  "itemCount": 2,
  "createdAt": "2026-05-01T10:30:00Z",
  "updatedAt": "2026-05-01T10:30:00Z"
}
```

#### Error Responses

**400 Bad Request** - Validation error:
```json
{
  "error": "ValidationFailed",
  "message": "Order validation failed",
  "details": [
    {
      "field": "totalAmount",
      "error": "Total amount must be greater than 0"
    },
    {
      "field": "items",
      "error": "At least 1 item required"
    }
  ]
}
```

**400 Bad Request** - Amount mismatch:
```json
{
  "error": "ValidationFailed",
  "message": "Order total does not match sum of items",
  "details": {
    "provided": 750.00,
    "calculated": 751.50,
    "tolerance": 0.01
  }
}
```

**500 Internal Server Error** - Database or messaging unavailable:
```json
{
  "error": "InternalServerError",
  "message": "Failed to create order. Please try again.",
  "requestId": "req-12345"
}
```

**503 Service Unavailable** - Dependency timeout:
```json
{
  "error": "ServiceUnavailable",
  "message": "Database or messaging service temporarily unavailable",
  "retryAfter": 30
}
```

#### Performance Target

- **Median**: < 100ms
- **P95**: < 300ms
- **P99**: < 500ms

---

### 2. Get Order by ID

**GET** `/api/orders/{orderId}`

Retrieves complete order details including items and current status.

#### Request

```http
GET /api/orders/550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Authorization: Bearer {token}
```

#### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `orderId` | UUID | Order identifier |

#### Success Response

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "customerId": "CUST-001",
  "status": "Processing",
  "totalAmount": 750.00,
  "items": [
    {
      "itemId": "550e8400-e29b-41d4-a716-446655440010",
      "productId": "PROD-A",
      "quantity": 2,
      "unitPrice": 300.00,
      "lineTotal": 600.00,
      "description": "Premium Widget"
    },
    {
      "itemId": "550e8400-e29b-41d4-a716-446655440011",
      "productId": "PROD-B",
      "quantity": 1,
      "unitPrice": 150.00,
      "lineTotal": 150.00,
      "description": "Standard Widget"
    }
  ],
  "createdAt": "2026-05-01T10:30:00Z",
  "updatedAt": "2026-05-01T10:30:15Z",
  "processingStartedAt": "2026-05-01T10:30:10Z",
  "completedAt": null,
  "approvalReason": null,
  "rejectionReason": null,
  "errorMessage": null
}
```

#### Response Schema

| Field | Type | Description |
|-------|------|-------------|
| `orderId` | UUID | Order identifier |
| `customerId` | string | Customer identifier |
| `status` | enum | Current status: Pending, Processing, Approved, Rejected, Failed |
| `totalAmount` | decimal | Total order amount in EUR |
| `items` | array | Array of OrderItem objects |
| `createdAt` | datetime | ISO 8601 timestamp of creation |
| `updatedAt` | datetime | ISO 8601 timestamp of last update |
| `processingStartedAt` | datetime\|null | When Worker started processing (null if not started) |
| `completedAt` | datetime\|null | When order reached terminal state (null if pending/processing) |
| `approvalReason` | string\|null | Reason for approval (if Approved) |
| `rejectionReason` | string\|null | Reason for rejection (if Rejected) |
| `errorMessage` | string\|null | Error details (if Failed) |

#### Error Responses

**404 Not Found**:
```json
{
  "error": "OrderNotFound",
  "message": "Order with ID 550e8400-e29b-41d4-a716-446655440000 not found"
}
```

**400 Bad Request** - Invalid UUID:
```json
{
  "error": "InvalidRequest",
  "message": "Invalid order ID format. Must be a valid UUID."
}
```

#### Performance Target

- **Median**: < 50ms
- **P95**: < 100ms
- **P99**: < 150ms

---

### 3. List Orders

**GET** `/api/orders`

Retrieves paginated list of orders for a customer with optional filtering.

#### Request

```http
GET /api/orders?customerId=CUST-001&status=Approved&skip=0&take=20&dateFrom=2026-05-01&dateTo=2026-05-02 HTTP/1.1
Authorization: Bearer {token}
```

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `customerId` | string | ✅ | - | Filter by customer (required) |
| `status` | enum | ❌ | all | Filter by status (Pending, Processing, Approved, Rejected, Failed) |
| `skip` | integer | ❌ | 0 | Number of records to skip |
| `take` | integer | ❌ | 20 | Number of records to return (max 100) |
| `dateFrom` | date | ❌ | - | ISO 8601 date; orders created on or after |
| `dateTo` | date | ❌ | - | ISO 8601 date; orders created on or before |

#### Success Response

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "orders": [
    {
      "orderId": "550e8400-e29b-41d4-a716-446655440000",
      "customerId": "CUST-001",
      "status": "Approved",
      "totalAmount": 750.00,
      "itemCount": 2,
      "createdAt": "2026-05-01T10:30:00Z",
      "updatedAt": "2026-05-01T10:30:45Z",
      "completedAt": "2026-05-01T10:30:45Z"
    }
  ],
  "pagination": {
    "skip": 0,
    "take": 20,
    "total": 150,
    "hasMore": true,
    "pageCount": 8
  }
}
```

#### Error Responses

**400 Bad Request** - Invalid parameters:
```json
{
  "error": "InvalidRequest",
  "message": "Invalid query parameters",
  "details": [
    {
      "parameter": "take",
      "error": "take must be between 1 and 100"
    }
  ]
}
```

---

### 4. Get Order Health Check

**GET** `/api/health`

Simple health check endpoint (no authentication required).

#### Request

```http
GET /api/health HTTP/1.1
```

#### Success Response

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "status": "healthy",
  "timestamp": "2026-05-01T10:30:00Z",
  "version": "1.0.0",
  "dependencies": {
    "database": "connected",
    "messaging": "connected"
  }
}
```

#### Degraded Response

```http
HTTP/1.1 503 Service Unavailable
Content-Type: application/json

{
  "status": "degraded",
  "timestamp": "2026-05-01T10:30:00Z",
  "version": "1.0.0",
  "dependencies": {
    "database": "connected",
    "messaging": "disconnected"
  }
}
```

---

## Common Status Codes

| Code | Description | When Used |
|------|-------------|-----------|
| `200 OK` | Success | GET requests successful |
| `201 Created` | Resource created | POST order successful |
| `400 Bad Request` | Client error | Validation failure, malformed request |
| `401 Unauthorized` | Auth failed | Missing/invalid Bearer token |
| `404 Not Found` | Resource not found | Order doesn't exist |
| `500 Internal Server Error` | Server error | Unexpected error, database failure |
| `503 Service Unavailable` | Temporary unavailable | Database/messaging timeout |

---

## Rate Limiting

Recommended (not enforced in MVP):
- 100 requests per 10 seconds per API key
- Returns `429 Too Many Requests` when exceeded
- Retry-After header indicates wait time

---

## API Response Format

All responses follow this format:

**Success (2xx)**:
```json
{
  "data": { /* response body */ },
  "metadata": {
    "timestamp": "2026-05-01T10:30:00Z",
    "requestId": "req-12345"
  }
}
```

**Error (4xx, 5xx)**:
```json
{
  "error": "ErrorCode",
  "message": "Human-readable message",
  "details": { /* optional context */ },
  "metadata": {
    "timestamp": "2026-05-01T10:30:00Z",
    "requestId": "req-12345"
  }
}
```

---

## Idempotency

Order creation is idempotent via unique order ID generation:
- Same request → Same UUID returned
- Retry safe: no duplicate orders created
- Implemented via database unique constraint

---

## Versioning

API version in URL: `/api/v1/orders`, `/api/v2/orders` (for future major versions)

Current version: `v1`

---

## API Timeout

- All endpoints must respond within 30 seconds
- Longer operations use async processing with callback webhooks (future)

---

## CORS

Development:
- Allow origins: `localhost:3000`, `localhost:8080`

Production:
- Restrict to known frontend domains
- Configuration via environment variables

