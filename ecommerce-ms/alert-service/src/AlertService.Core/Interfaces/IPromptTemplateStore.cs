namespace AlertService.Core.Interfaces;

/// <summary>
/// Permite ler/editar o template de instruções usado no prompt da IA
/// sem precisar recompilar o serviço — inclusive via tools MCP.
/// </summary>
public interface IPromptTemplateStore
{
    Task<string> GetTemplateAsync(CancellationToken ct = default);
    Task SetTemplateAsync(string template, CancellationToken ct = default);
    Task ResetToDefaultAsync(CancellationToken ct = default);
    string GetDefaultTemplate();
}
