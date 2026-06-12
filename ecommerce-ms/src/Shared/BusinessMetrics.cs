// ─────────────────────────────────────────────────────────────────────────────
// Arquivo: src/Shared/BusinessMetrics.cs
//
// Coloque este arquivo em um projeto compartilhado (ex: Shared.Infrastructure)
// ou copie a versão específica em cada serviço conforme necessidade.
//
// Requer: prometheus-net (já está nos projetos via UseHttpMetrics)
// ─────────────────────────────────────────────────────────────────────────────

using Prometheus;

namespace Shared.Metrics;

/// <summary>
/// Métricas de negócio e Outbox expostas via Prometheus.
/// Registre como Singleton no DI de cada serviço que precisar.
/// </summary>
public sealed class BusinessMetrics
{
    // ── Outbox ──────────────────────────────────────────────────────────────
    public static readonly Gauge OutboxPending = Metrics
        .CreateGauge("outbox_pending_messages_total",
            "Mensagens do Outbox ainda não processadas",
            new GaugeConfiguration { LabelNames = ["service"] });

    public static readonly Gauge OutboxFailed = Metrics
        .CreateGauge("outbox_failed_messages_total",
            "Mensagens do Outbox com RetryCount esgotado (>= 5)",
            new GaugeConfiguration { LabelNames = ["service"] });

    public static readonly Counter OutboxProcessed = Metrics
        .CreateCounter("outbox_processed_messages_total",
            "Mensagens do Outbox processadas com sucesso",
            new CounterConfiguration { LabelNames = ["service"] });

    // ── Pedidos ──────────────────────────────────────────────────────────────
    public static readonly Counter OrdersCreated = Metrics
        .CreateCounter("business_orders_created_total",
            "Total de pedidos criados com sucesso");

    public static readonly Counter OrdersCancelled = Metrics
        .CreateCounter("business_orders_cancelled_total",
            "Total de pedidos cancelados");

    // ── Clientes ─────────────────────────────────────────────────────────────
    public static readonly Counter CustomersCreated = Metrics
        .CreateCounter("business_customers_created_total",
            "Total de clientes cadastrados");

    // ── Estoque ──────────────────────────────────────────────────────────────
    public static readonly Counter StockReserveFailed = Metrics
        .CreateCounter("business_stock_reserve_failed_total",
            "Total de tentativas de reserva de estoque com falha (sem estoque)");

    // ── Notificações ─────────────────────────────────────────────────────────
    public static readonly Counter NotificationsSent = Metrics
        .CreateCounter("business_notifications_sent_total",
            "Total de notificações enviadas",
            new CounterConfiguration { LabelNames = ["type"] });
}
