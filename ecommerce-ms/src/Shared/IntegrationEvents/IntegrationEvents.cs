namespace Shared.IntegrationEvents;

/// <summary>
/// Eventos de integração compartilhados entre microserviços.
/// Publicados via MassTransit/RabbitMQ.
/// </summary>

public sealed record CustomerCreatedIntegrationEvent(Guid CustomerId, string Name, string Email);
public sealed record ProductCreatedIntegrationEvent(Guid ProductId, string Sku, string Name, string Category);
