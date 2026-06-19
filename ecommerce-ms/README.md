# ECommerce Microservices

Sistema de e-commerce distribuído com .NET 9, DDD, Clean Architecture, CQRS e observabilidade completa.

---

## Stack Tecnológica

| Componente       | Tecnologia                          |
|------------------|-------------------------------------|
| Framework        | ASP.NET Core 9                      |
| ORM              | EF Core 9 + Npgsql (PostgreSQL)     |
| CQRS             | MediatR 12                          |
| Mensageria       | MassTransit 8 + RabbitMQ            |
| Cache            | Redis (IDistributedCache)           |
| Validação        | FluentValidation 11                 |
| Resiliência      | Polly 7 (Retry + Circuit Breaker)   |
| Tracing          | OpenTelemetry + Jaeger              |
| Métricas         | Prometheus + Grafana                |
| Logs             | Serilog                             |
| Containers       | Docker + Docker Compose             |
| Orquestração     | Kubernetes (manifests prontos)      |

---

## Pré-requisitos

- **Docker Desktop 4.x+** com pelo menos 4 GB de RAM alocados
- **.NET SDK 9.0** (apenas para desenvolvimento local)

---

## Quick Start

```bash
# 1. Clone o repositório
git clone <url>
cd ecommerce-ms

# 2. Suba toda a infraestrutura e os serviços
docker-compose up -d --build

# 3. Aguarde ~40s para todos os healthchecks passarem
docker-compose ps
```

---

## URLs de Acesso

| Serviço               | URL                              |
|-----------------------|----------------------------------|
| Customer API Swagger  | http://localhost:5001/swagger    |
| Product API Swagger   | http://localhost:5002/swagger    |
| Order API Swagger     | http://localhost:5003/swagger    |
| Notification Swagger  | http://localhost:5004/swagger    |
| RabbitMQ Management   | http://localhost:15672 (rabbit/rabbit123) |
| Jaeger Tracing        | http://localhost:16686           |
| Prometheus            | http://localhost:9090            |
| Grafana               | http://localhost:3000 (admin/admin123)    |

---

## Fluxo E2E para Testar

```
# 1. Criar cliente
POST http://localhost:5001/api/v1/customers
{
  "name": "Paulo Bumba",
  "email": "paulo@email.com",
  "phone": "+55 49 99999-0000",
  "street": "Rua das Flores, 123",
  "city": "Videira",
  "state": "SC",
  "zipCode": "89560-000",
  "country": "Brasil"
}

# 2. Criar produto com estoque
POST http://localhost:5002/api/v1/products
{
  "name": "Notebook Pro",
  "description": "Notebook de alto desempenho",
  "sku": "NOTE-PRO-001",
  "price": 4999.99,
  "currency": "BRL",
  "initialStock": 50,
  "category": "Eletronicos"
}

# 3. Criar pedido (Order chama Product via HTTP + Polly)
POST http://localhost:5003/api/v1/orders
{
  "customerId": "<id do cliente>",
  "street": "Rua das Flores, 123",
  "city": "Videira",
  "state": "SC",
  "zipCode": "89560-000",
  "country": "Brasil",
  "items": [
    { "productId": "<id do produto>", "quantity": 2 }
  ]
}

# 4. Verificar notificação gerada
GET http://localhost:5004/api/v1/notifications

# 5. Verificar trace no Jaeger
http://localhost:16686 → selecionar "order-service"
```

---

## Arquitetura de Camadas (por serviço)

```
src/<Service>/
├── Domain/           ← Entities, Value Objects, Domain Events, Repositories (interfaces)
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Events/
│   ├── Exceptions/
│   └── Repositories/
├── Application/      ← CQRS (Commands/Queries/Handlers), DTOs, Behaviors, Validators
│   ├── Behaviors/    (Logging + Validation pipeline)
│   ├── DTOs/
│   ├── Features/
│   │   └── <Entity>/
│   │       ├── Commands/
│   │       └── Queries/
│   └── Mappings/
├── Infrastructure/   ← EF Core, Repositories (impl), Outbox, MassTransit, HttpClient
│   ├── Persistence/
│   │   └── Configurations/
│   ├── Outbox/
│   ├── Repositories/
│   └── Http/ (somente Order)
└── Api/              ← Controllers, Middleware, Program.cs, Dockerfile
    ├── Controllers/
    └── Middleware/
```

---

## Padrões Implementados

| Padrão                | Onde                                              |
|-----------------------|---------------------------------------------------|
| Clean Architecture    | Separação em 4 camadas sem inversão de dependência|
| DDD                   | Entities com comportamento, Value Objects, Eventos|
| CQRS                  | Commands (write) e Queries (read) via MediatR     |
| Outbox Pattern        | Customer, Product, Order — at-least-once delivery |
| Repository Pattern    | GetByIdAsync (read) + GetByIdTrackedAsync (write)  |
| Problem Details RFC 7807 | GlobalExceptionHandler em todos os serviços    |
| Polly Retry           | 3x backoff exponencial (2s/4s/8s)                |
| Polly Circuit Breaker | 5 falhas → abre 1 minuto → Half-Open              |
| Dead Letter Queue     | Notification Service — filas DLQ via MassTransit  |
| Redis Cache           | GetProductByIdHandler — TTL 5 minutos             |
| OpenTelemetry         | Tracing distribuído em todos os serviços          |
| Prometheus Metrics    | /metrics endpoint em todos os serviços            |
| HealthChecks          | /health com postgres + redis + rabbitmq           |


make help           # lista tudo
make up             # sobe toda a stack
make migrate        # migrations em todos os serviços de uma vez
make seed           # popula dados de teste
make test           # unit + api end-to-end
make outbox-status  # vê mensagens pendentes no Outbox de cada banco
make deadletter-requeue  # reprocessa mensagens mortas
make rabbit-ui / jaeger / grafana  # abre as UIs


make k6-smoke   # 1 VU, 1 min — sanidade antes de qualquer coisa
make k6-load    # 50 VUs, 5 min — carga normal com métricas de cache hit
make k6-stress  # até 200 VUs — acha o ponto de ruptura
make k6-spike   # pico repentino de 250 VUs — simula flash sale
make k6-soak    # 30 VUs por 30 min — detecta memory leak
make k6-all     # roda smoke → load → stress em sequência