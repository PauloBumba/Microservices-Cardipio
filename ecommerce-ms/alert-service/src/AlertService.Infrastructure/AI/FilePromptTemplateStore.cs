using AlertService.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlertService.Infrastructure.AI;

public sealed class PromptTemplateStoreOptions
{
    public string FilePath { get; set; } = "data/incident-prompt-template.txt";
}

public sealed class FilePromptTemplateStore(
    IOptions<PromptTemplateStoreOptions> options,
    ILogger<FilePromptTemplateStore>     logger) : IPromptTemplateStore
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<string> GetTemplateAsync(CancellationToken ct = default)
    {
        var path = options.Value.FilePath;
        if (!File.Exists(path))
            return GetDefaultTemplate();

        await _lock.WaitAsync(ct);
        try
        {
            var content = await File.ReadAllTextAsync(path, ct);
            return string.IsNullOrWhiteSpace(content) ? GetDefaultTemplate() : content;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SetTemplateAsync(string template, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(template))
            throw new ArgumentException("Template não pode ser vazio.", nameof(template));

        var path = options.Value.FilePath;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await _lock.WaitAsync(ct);
        try
        {
            await File.WriteAllTextAsync(path, template, ct);
            logger.LogInformation("Prompt template atualizado em {Path}", path);
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task ResetToDefaultAsync(CancellationToken ct = default) =>
        SetTemplateAsync(GetDefaultTemplate(), ct);

    public string GetDefaultTemplate() => """
        Você é um SRE especialista em aplicações distribuídas. Analise o incidente abaixo e responda APENAS em JSON válido.
        Formato obrigatório:
        {
          "root_cause": "descrição técnica da causa raiz",
          "probable_cause": "deploy | database | overload | network | memory_leak | external_dependency | configuration | unknown",
          "severity": "Low | Medium | High | Critical",
          "operational_decision": "Ignore | Observe | Notify | Escalate",
          "confidence": 0.0,
          "impact": "impacto provável para usuário/sistema, máximo 1 frase",
          "evidence": ["evidência objetiva 1", "evidência objetiva 2"],
          "human_summary": "resumo em português para o time, máximo 2 frases",
          "suggestions": ["ação 1", "ação 2", "ação 3"]
        }

        Critério de decisão operacional:
        - Ignore: ruído, resolved sem impacto, no_data isolado ou evidência insuficiente.
        - Observe: warning sem evidência forte; registre mas não interrompa.
        - Notify: incidente real que merece mensagem em canal leve.
        - Escalate: critical, serviço down, perda de pedidos, outbox parado, fila crítica ou impacto claro ao usuário.
        Use confidence entre 0 e 1. Evidence deve conter apenas fatos vindos do alerta, logs ou métricas.
        Responda SOMENTE com o JSON, sem explicações adicionais.
        """;
}
