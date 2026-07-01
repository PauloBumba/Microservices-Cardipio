using AlertService.Core.Models;

namespace AlertService.Core.Interfaces;

public interface IIncidentAiService
{
    /// <summary>
    /// Envia o contexto completo do incidente para o Ollama e retorna análise de RCA.
    /// </summary>
    Task<AiAnalysis> AnalyzeAsync(IncidentContext context, CancellationToken ct = default);
}
