// ─────────────────────────────────────────────────────────────────────────────
// EXEMPLOS — como incrementar métricas de negócio nos handlers CQRS
// ─────────────────────────────────────────────────────────────────────────────

// ── 1. Order Service — CreateOrderCommandHandler ─────────────────────────────
//
// No handler existente, após salvar o pedido no banco:
//
//   BusinessMetrics.OrdersCreated.Inc();
//
// Exemplo completo:
/*
public sealed class CreateOrderCommandHandler(IOrderRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        // ... lógica existente ...
        await repo.AddAsync(order, ct);
        await uow.SaveChangesAsync(ct);

        BusinessMetrics.OrdersCreated.Inc(); // <- ADICIONE ESTA LINHA

        return order.ToDto();
    }
}
*/

// ── 2. Order Service — CancelOrderCommandHandler ─────────────────────────────
//
//   BusinessMetrics.OrdersCancelled.Inc();

// ── 3. Customer Service — CreateCustomerCommandHandler ───────────────────────
//
//   BusinessMetrics.CustomersCreated.Inc();

// ── 4. Product Service — ReserveStockCommandHandler ──────────────────────────
//
//   if (!reservado)
//       BusinessMetrics.StockReserveFailed.Inc();

// ── 5. Notification Service — OrderCreatedConsumer ───────────────────────────
//
//   BusinessMetrics.NotificationsSent.WithLabels("order_created").Inc();

// ── 6. Notification Service — CustomerCreatedConsumer ────────────────────────
//
//   BusinessMetrics.NotificationsSent.WithLabels("customer_created").Inc();

// ── 7. OutboxProcessor — após pub.Publish() com sucesso ──────────────────────
//
//   BusinessMetrics.OutboxProcessed.WithLabels("order-service").Inc();

// ─────────────────────────────────────────────────────────────────────────────
// REGISTRO NO DI (DependencyInjection.cs de cada serviço)
// ─────────────────────────────────────────────────────────────────────────────
/*
// Order service — DependencyInjection.cs
services.AddHostedService<OutboxProcessor>();
services.AddHostedService<OutboxMetricsCollector>(); // <- ADICIONE ESTA LINHA
*/

// ─────────────────────────────────────────────────────────────────────────────
// PACKAGE REFERENCE (não precisa adicionar — prometheus-net já está via API csproj)
// Se precisar referenciar explicitamente:
// <PackageReference Include="prometheus-net" Version="8.*" />
// ─────────────────────────────────────────────────────────────────────────────
