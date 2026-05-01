# ES2-SistemaPedidos.sln

## Visão Geral

O projeto **ES2-SistemaPedidos** será uma solução .NET organizada em monorepo contendo múltiplos projetos dentro do mesmo repositório para simular uma arquitetura moderna orientada a eventos utilizando serviços AWS localmente via LocalStack.

A solução terá:

* API HTTP (.NET)
* Worker (.NET)
* Biblioteca compartilhada (Shared)
* PostgreSQL
* LocalStack (SNS + SQS)
* Docker Compose
* Testes automatizados

---

## Objetivo do Sistema

Simular um sistema de processamento de pedidos com fluxo assíncrono.

```text
Cliente envia pedido
↓
API registra pedido
↓
Evento enviado ao SNS
↓
SNS repassa ao SQS
↓
Worker consome fila
↓
Processa pedido
↓
Atualiza banco
```

---

## Estrutura do Repositório

```text
ES2-SistemaPedidos/
├── ES2-SistemaPedidos.sln
├── src/
│   ├── ES2-SistemaPedidos.Api/
│   ├── ES2-SistemaPedidos.Worker/
│   └── ES2-SistemaPedidos.Shared/
├── tests/
│   ├── unit/
│   ├── integration/
│   └── e2e/
├── docker/
│   ├── docker-compose.yml
│   └── init-aws.sh
├── docs/
│   └── arquitetura.md
├── README.md
└── .gitignore
```

---

## Projetos da Solution

## ES2-SistemaPedidos.Api

Aplicação ASP.NET Core Web API.

### Responsabilidades

* Receber requisições HTTP
* Validar payloads
* Salvar pedidos no banco
* Publicar eventos no SNS
* Consultar pedidos

### Endpoints sugeridos

```http
POST /orders
GET /orders/{id}
GET /orders
```

---

## ES2-SistemaPedidos.Worker

Worker Service .NET executando em background.

### Responsabilidades

* Consumir mensagens do SQS
* Processar pedidos
* Aplicar regras de negócio
* Atualizar status no banco

---

## ES2-SistemaPedidos.Shared

Biblioteca compartilhada entre API e Worker.

### Responsabilidades

Centralizar tudo que mais de um projeto utiliza.

### Exemplos

* DTOs
* Enums
* Contratos de eventos
* Models compartilhados
* Helpers comuns
* Constantes

### Exemplo de Enum

```csharp
public enum OrderStatus
{
    Pending,
    Processing,
    Approved,
    Rejected
}
```

### Exemplo de Evento

```csharp
public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
}
```

### Por que usar Shared?

Sem Shared você duplicaria código:

* mesma enum na API
* mesma enum no Worker
* mesmo contrato de evento em dois lugares

Com Shared:

* uma fonte de verdade
* menos erros
* manutenção simples

---

## Domínio do Sistema

### Entidade Pedido

```text
Order
- Id
- CustomerName
- TotalAmount
- CreatedAt
- Status
```

### Status possíveis

```text
Pending
Processing
Approved
Rejected
Failed
```

---

## Fluxo Funcional

## Criar Pedido

### Request

```json
{
  "customerName": "João Silva",
  "totalAmount": 250.00
}
```

### Processo

```text
1. API recebe pedido
2. Salva no banco com status Pending
3. Publica evento OrderCreated
4. Retorna 201 Created
```

---

## Processamento Assíncrono

### Worker recebe mensagem

```text
OrderId = 123
Total = 250
```

### Regra sugerida

```text
Se Total < 1000 => Approved
Se Total >= 1000 => Rejected
```

---

## Infraestrutura AWS Local

### LocalStack

Serviços simulados:

* SNS
* SQS

### Recursos

#### Topic

```text
orders-topic
```

#### Queue

```text
orders-queue
```

#### Subscription

```text
orders-topic → orders-queue
```

---

## Banco de Dados

### PostgreSQL

Tabela:

```text
orders
```

Campos:

```text
id
customer_name
total_amount
status
created_at
updated_at
```

---

## Docker Compose

Serviços:

```text
api
worker
postgres
localstack
```

---

## Estratégia de Testes

### Unitários

* regras de negócio
* validações
* handlers

### Integração

* API + banco
* publish SNS
* leitura SQS

### E2E

```text
POST /orders
↓
worker processa
↓
GET /orders/{id}
↓
status final
```

---

## Configurações

### appsettings.Development.json

```json
{
  "ConnectionStrings": {},
  "AWS": {
    "ServiceURL": "http://localhost:4566"
  }
}
```

---

## Evoluções Futuras

* Retry policy
* Dead Letter Queue
* Logs estruturados
* OpenTelemetry
* Separar Worker em microserviço real

---

## Benefícios Técnicos

Este projeto demonstra:

* ASP.NET Core
* Worker Services
* AWS SNS/SQS
* Docker
* PostgreSQL
* Arquitetura assíncrona
* Testes automatizados

---

## Estrutura Final

```text
ES2-SistemaPedidos.sln
├── src/ES2-SistemaPedidos.Api
├── src/ES2-SistemaPedidos.Worker
├── src/ES2-SistemaPedidos.Shared
├── tests/unit
├── tests/integration
└── tests/e2e
```
