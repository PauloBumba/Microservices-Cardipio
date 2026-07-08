using AlertService.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlertService.API.Controllers;

/// <summary>
/// Permite ler/editar o template de prompt usado na análise de incidentes
/// pela IA, direto pelo Swagger — sem precisar rebuildar o serviço.
/// Usa o mesmo IPromptTemplateStore exposto também via MCP.
/// </summary>
[ApiController]
[Route("api/prompt")]
public sealed class PromptController(
    IPromptTemplateStore promptStore,
    ILogger<PromptController> logger) : ControllerBase
{
    public sealed record SetPromptRequest(string Template);

    /// <summary>Retorna o template de prompt atualmente em uso.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var template = await promptStore.GetTemplateAsync(ct);
        return Ok(new { template });
    }

    /// <summary>Substitui o template de prompt em uso.</summary>
    [HttpPut]
    public async Task<IActionResult> Set([FromBody] SetPromptRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Template))
            return BadRequest(new { error = "Template não pode ser vazio." });

        await promptStore.SetTemplateAsync(request.Template, ct);
        logger.LogInformation("Prompt template atualizado via API por requisição HTTP");
        return Ok(new { updated = true });
    }

    /// <summary>Restaura o template de prompt padrão de fábrica.</summary>
    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        await promptStore.ResetToDefaultAsync(ct);
        return Ok(new { reset = true });
    }

    /// <summary>Retorna o template padrão de fábrica, sem aplicá-lo (só pra consulta/comparação).</summary>
    [HttpGet("default")]
    public IActionResult GetDefault() => Ok(new { template = promptStore.GetDefaultTemplate() });
}
