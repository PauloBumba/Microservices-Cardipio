using AlertService.Core.Models;

namespace AlertService.Core.Interfaces;

public interface ILokiClient
{
    /// <summary>
    /// Busca logs no Loki no intervalo [from, to].
    /// </summary>
    /// <param name="service">Valor do label {service} no Loki.</param>
    /// <param name="from">Início da janela de busca.</param>
    /// <param name="to">Fim da janela de busca.</param>
    /// <param name="limit">Máximo de linhas retornadas (default: 100).</param>
    Task<IReadOnlyList<LogEntry>> QueryRangeAsync(
        string service,
        DateTimeOffset from,
        DateTimeOffset to,
        int limit = 100,
        CancellationToken ct = default);
}
