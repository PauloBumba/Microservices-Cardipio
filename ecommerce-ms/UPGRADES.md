# O que foi adicionado nesta atualização

## Shared Kernel (src/Shared/)

### Shared.Domain
- `AggregateRoot` com `AggregateVersion` (versionamento otimista)
- `DomainEvent` base (record imutável com EventId + OccurredAt)
- `EntityBase` com igualdade por Id
- `ValueObject` com igualdade estrutural

### Shared.Application
- `ApiResponse<T>` — envelope de resposta padronizado (sem lançar exceção para erros de negócio)
- `IBaseCommand` — marker que ativa o TransactionBehavior
- `ICacheableQuery` — marker que ativa o CachingBehavior
- `LoggingBehavior` — log de entrada/saída por request
- `PerformanceBehavior` — warning se handler > 500ms
- `ValidationBehavior` — FluentValidation automático
- `TransactionBehavior` — commit automático, apenas se IsSuccess=true
- `CachingBehavior` — Redis/in-memory por CacheKey
- `CacheInvalidationBehavior` — invalida chaves após sucesso de commands
- `IUnitOfWorkAccessor` — interface mínima que o TransactionBehavior usa

### Shared.Infrastructure
- `OutboxMessage` com status Pending/Processed/DeadLetter, lock otimista e backoff
- `ProcessedEvent` — tabela de idempotência
- `OutboxProcessorBase` — base com backoff exponencial, lock otimista, idempotência

## Por microsserviço (Customer, Product, Order)
- Entidade herda `AggregateRoot` (não mais `Entity` local)
- Eventos herdam `DomainEvent` do Shared
- Handlers retornam `ApiResponse<T>` — sem `CommitAsync` manual
- Commands implementam `IBaseCommand` + `ICacheInvalidator`
- Queries implementam `ICacheableQuery`
- `DbContext` implementa `IUnitOfWorkAccessor` e serializa events no Outbox
- `ProcessedEvents` por microsserviço (isolamento)
- `OutboxProcessor` estende `OutboxProcessorBase` (sem código duplicado)
- Pipeline MediatR completo com 6 behaviors na ordem correta

## O que NÃO foi alterado
- MassTransit + RabbitMQ (mantido igual)
- Observabilidade (Prometheus, Grafana, OpenTelemetry) — mantida
- Testes (Order.Domain.Tests, Product.Domain.Tests) — mantidos
- docker-compose.yml — mantido
- AlertService / Notification — mantidos
