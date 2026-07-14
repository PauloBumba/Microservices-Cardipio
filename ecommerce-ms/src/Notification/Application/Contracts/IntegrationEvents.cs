using Shared.IntegrationEvents;

namespace Notification.Application.Contracts;

/// Contrato local do OrderCreatedDomainEvent publicado pelo Order Service.
/// O AssemblyQualifiedName no Outbox corresponde ao tipo do Order.Domain.
/// Aqui usamos o mesmo Type name para o MassTransit rotear corretamente.

public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId, string OrderNumber, Guid CustomerId,
    decimal TotalAmount, string Currency,
    IReadOnlyList<OrderItemLine> Items);

public sealed record OrderItemLine(Guid ProductId, string Sku, int Quantity, decimal UnitPrice);

// Usar o evento compartilhado do Shared

