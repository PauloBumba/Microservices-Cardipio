using Shared.Infrastructure.Idempotency;

namespace Customer.Infrastructure.Idempotency;

/// <summary>Tabela de idempotência específica do Customer microsserviço.</summary>
public class CustomerProcessedEvent : ProcessedEvent { }
