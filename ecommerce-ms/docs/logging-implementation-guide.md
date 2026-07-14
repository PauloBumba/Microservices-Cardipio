# Guia de Implementação - Arquitetura de Logging Corporativa

## Visão Geral

Este guia detalha a implementação completa da arquitetura de logging para o projeto E-commerce Microservices, seguindo boas práticas corporativas e preparação para evolução para microserviços.

## Estrutura de Pastas a Criar

```
ecommerce-ms/
├── src/
│   ├── Shared/
│   │   ├── Infrastructure/
│   │   │   ├── Logging/
│   │   │   │   ├── Categories/
│   │   │   │   ├── Enrichers/
│   │   │   │   ├── Sinks/
│   │   │   │   ├── Filters/
│   │   │   │   ├── Services/
│   │   │   │   └── SerilogConfiguration.cs
│   │   │   └── Audit/
│   │   ├── Application/
│   │   │   └── Audit/
│   └── [Service]/
│       └── Api/
│           └── Logs/
│               ├── Application/
│               ├── Audit/
│               ├── Security/
│               └── Background/
```

---

## Passo 1: Criar Estrutura de Categorias de Logs

### 1.1 Criar ApplicationLog.cs
**Arquivo:** `src/Shared/Infrastructure/Logging/Categories/ApplicationLog.cs`

```csharp
namespace Shared.Infrastructure.Logging.Categories;

public class ApplicationLog
{
    public string Category => "Application";
    public string Level { get; set; }
    public string Message { get; set; }
    public string Service { get; set; }
    public string Environment { get; set; }
    public string TimestampUtc { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? CorrelationId { get; set; }
    public string? UserId { get; set; }
    public string? MachineName { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}
```

### 1.2 Criar AuditLog.cs
**Arquivo:** `src/Shared/Infrastructure/Logging/Categories/AuditLog.cs`

```csharp
namespace Shared.Infrastructure.Logging.Categories;

public class AuditLog
{
    public string Category => "Audit";
    public Guid Id { get; set; }
    public string TimestampUtc { get; set; }
    public string Service { get; set; }
    public string Environment { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Action { get; set; }
    public string Resource { get; set; }
    public string ResourceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Changes { get; set; } = new();
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
}
```

### 1.3 Criar SecurityLog.cs
**Arquivo:** `src/Shared/Infrastructure/Logging/Categories/SecurityLog.cs`

```csharp
namespace Shared.Infrastructure.Logging.Categories;

public class SecurityLog
{
    public string Category => "Security";
    public string TimestampUtc { get; set; }
    public string Service { get; set; }
    public string Environment { get; set; }
    public string EventType { get; set; }  // Login, Logout, FailedAuth, PermissionDenied
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Resource { get; set; }
    public string? Details { get; set; }
    public string? CorrelationId { get; set; }
}
```

### 1.4 Criar BackgroundLog.cs
**Arquivo:** `src/Shared/Infrastructure/Logging/Categories/BackgroundLog.cs`

```csharp
namespace Shared.Infrastructure.Logging.Categories;

public class BackgroundLog
{
    public string Category => "Background";
    public string TimestampUtc { get; set; }
    public string Service { get; set; }
    public string JobName { get; set; }
    public string Status { get; set; }  // Started, Completed, Failed
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public string? CorrelationId { get; set; }
}
```

---

## Passo 2: Criar Enrichers

### 2.1 Criar TraceIdEnricher.cs
**Arquivo:** `src/Shared/Infrastructure/Logging/Enrichers/TraceIdEnricher.cs`

```csharp
using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Shared.Infrastructure.Logging.Enrichers;

public class TraceIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));
        }
    }
}
```

### 2.2 Criar CorrelationIdEnricher.cs
**Arquivo:** `src/Shared/Infrastructure/Logging/Enrichers/CorrelationIdEnricher.cs`

```csharp
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Shared.Infrastructure.Logging.Enrichers;

public class CorrelationIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (LogContext.TryGet("CorrelationId", out var correlationId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
        }
    }
}
```

### 2.3 Criar UserIdEnricher.cs
**Arquivo:** `src/Shared/Infrastructure/Logging/Enrichers/UserIdEnricher.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Shared.Infrastructure.Logging.Enrichers;

public class UserIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
        }
    }
}
```

### 2.4 Criar EnvironmentEnricher.cs
**Arquivo:** `src/Shared/Infrastructure/Logging/Enrichers/EnvironmentEnricher.cs`

```csharp
using Serilog.Core;
using Serilog.Events;

namespace Shared.Infrastructure.Logging.Enrichers;

public class EnvironmentEnricher : ILogEventEnricher
{
    private readonly string _environment;

    public EnvironmentEnricher(string environment)
    {
        _environment = environment;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Environment", _environment));
    }
}
```

---

## Passo 3: Criar Filtros

### 3.1 Criar SensitiveDataFilter.cs
**Arquivo:** `src/Shared/Infrastructure/Logging/Filters/SensitiveDataFilter.cs`

```csharp
using Serilog.Core;
using Serilog.Events;

namespace Shared.Infrastructure.Logging.Filters;

public class SensitiveDataFilter : ILogEventFilter
{
    private static readonly string[] SensitiveKeys = 
    {
        "password", "pwd", "secret", "token", "apikey", "creditcard", "ssn", "cpf",
        "senha", "segredo", "cartao", "cpf"
    };

    public bool IsEnabled(LogEvent logEvent)
    {
        SanitizeSensitiveData(logEvent);
        return true;
    }

    private void SanitizeSensitiveData(LogEvent logEvent)
    {
        foreach (var property in logEvent.Properties.ToList())
        {
            var key = property.Key.ToLower();
            if (SensitiveKeys.Any(s => key.Contains(s)))
            {
                logEvent.AddOrUpdateProperty(property.Key, 
                    new ScalarValue("***REDACTED***"));
            }
        }
    }
}
```

---

## Passo 4: Criar Serviços de Auditoria

### 4.1 Criar IAuditLogger.cs
**Arquivo:** `src/Shared/Infrastructure/Logging/Services/IAuditLogger.cs`

```csharp
namespace Shared.Infrastructure.Logging.Services;

public interface IAuditLogger
{
    Task LogAsync(AuditLog auditLog, CancellationToken ct = default);
    Task LogLoginAsync(string userId, string userName, string ipAddress, bool success, CancellationToken ct = default);
    Task LogDataAccessAsync(string userId, string resource, string resourceId, string action, CancellationToken ct = default);
    Task LogConfigurationChangeAsync(string userId, string setting, string oldValue, string newValue, CancellationToken ct = default);
}
```

### 4.2 Criar AuditLogger.cs
**Arquivo:** `src/Shared/Infrastructure/Logging/Services/AuditLogger.cs`

```csharp
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Logging.Categories;

namespace Shared.Infrastructure.Logging.Services;

public class AuditLogger(ILogger<AuditLogger> logger) : IAuditLogger
{
    public async Task LogAsync(AuditLog auditLog, CancellationToken ct = default)
    {
        using (Serilog.Context.LogContext.PushProperty("Category", "Audit"))
        using (Serilog.Context.LogContext.PushProperty("UserId", auditLog.UserId))
        using (Serilog.Context.LogContext.PushProperty("Action", auditLog.Action))
        using (Serilog.Context.LogContext.PushProperty("Resource", auditLog.Resource))
        {
            logger.LogInformation(
                "Audit: {Action} on {Resource} (ID: {ResourceId}) by {UserName} - Success: {Success}",
                auditLog.Action,
                auditLog.Resource,
                auditLog.ResourceId,
                auditLog.UserName,
                auditLog.Success);
            
            await Task.CompletedTask;
        }
    }

    public async Task LogLoginAsync(string userId, string userName, string ipAddress, bool success, CancellationToken ct = default)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow.ToString("o"),
            Service = "AuthService",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            UserId = userId,
            UserName = userName,
            Action = "Login",
            Resource = "Auth",
            ResourceId = userId,
            IpAddress = ipAddress,
            Success = success
        };

        await LogAsync(auditLog, ct);
    }

    public async Task LogDataAccessAsync(string userId, string resource, string resourceId, string action, CancellationToken ct = default)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow.ToString("o"),
            Service = Environment.GetEnvironmentVariable("ASPNETCORE_APPLICATIONNAME") ?? "Unknown",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            UserId = userId,
            UserName = userId,
            Action = action,
            Resource = resource,
            ResourceId = resourceId,
            Success = true
        };

        await LogAsync(auditLog, ct);
    }

    public async Task LogConfigurationChangeAsync(string userId, string setting, string oldValue, string newValue, CancellationToken ct = default)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow.ToString("o"),
            Service = Environment.GetEnvironmentVariable("ASPNETCORE_APPLICATIONNAME") ?? "Unknown",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            UserId = userId,
            UserName = userId,
            Action = "ConfigurationChange",
            Resource = "Configuration",
            ResourceId = setting,
            Changes = new Dictionary<string, object>
            {
                ["OldValue"] = oldValue,
                ["NewValue"] = newValue
            },
            Success = true
        };

        await LogAsync(auditLog, ct);
    }
}
```

---

## Passo 5: Criar SerilogConfiguration.cs

**Arquivo:** `src/Shared/Infrastructure/Logging/SerilogConfiguration.cs`

```csharp
using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Shared.Infrastructure.Logging.Enrichers;
using Shared.Infrastructure.Logging.Filters;

namespace Shared.Infrastructure.Logging;

public static class SerilogConfiguration
{
    public static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        var logPath = Path.Combine(builder.Environment.ContentRootPath, "Logs");
        
        // Criar diretórios de log
        Directory.CreateDirectory(Path.Combine(logPath, "Application"));
        Directory.CreateDirectory(Path.Combine(logPath, "Audit"));
        Directory.CreateDirectory(Path.Combine(logPath, "Security"));
        Directory.CreateDirectory(Path.Combine(logPath, "Background"));

        builder.Host.UseSerilog((ctx, lc) =>
        {
            lc.ReadFrom.Configuration(ctx.Configuration)
              .MinimumLevel.Information()
              .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
              .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
              .Enrich.FromLogContext()
              .Enrich.WithMachineName()
              .Enrich.WithProperty("Service", ctx.HostingEnvironment.ApplicationName)
              .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
              .Enrich.With(new TraceIdEnricher())
              .Enrich.With(new CorrelationIdEnricher())
              .Enrich.With(new EnvironmentEnricher(ctx.HostingEnvironment.EnvironmentName))
              .Filter.With(new SensitiveDataFilter())
              .WriteTo.Console(new JsonFormatter())
              .WriteTo.Async(a => a.File(
                  formatter: new JsonFormatter(),
                  path: Path.Combine(logPath, "Application", "application-.log"),
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 30,
                  outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
              .WriteTo.Async(a => a.File(
                  formatter: new JsonFormatter(),
                  path: Path.Combine(logPath, "Audit", "audit-.log"),
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 365, // 1 ano para compliance
                  outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
              .WriteTo.Async(a => a.File(
                  formatter: new JsonFormatter(),
                  path: Path.Combine(logPath, "Security", "security-.log"),
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 365))
              .WriteTo.Async(a => a.File(
                  formatter: new JsonFormatter(),
                  path: Path.Combine(logPath, "Background", "background-.log"),
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 30));

            // Configurar Loki se disponível
            var lokiUrl = ctx.Configuration["Loki:Url"];
            if (!string.IsNullOrEmpty(lokiUrl))
            {
                lc.WriteTo.GrafanaLoki(lokiUrl, labels: new[]
                {
                    new Serilog.Sinks.Grafana.Loki.LokiLabel { Key = "service", Value = ctx.HostingEnvironment.ApplicationName },
                    new Serilog.Sinks.Grafana.Loki.LokiLabel { Key = "environment", Value = ctx.HostingEnvironment.EnvironmentName }
                });
            }
        });
    }
}
```

---

## Passo 6: Criar CorrelationIdMiddleware

**Arquivo:** `src/Shared/Infrastructure/Middleware/CorrelationIdMiddleware.cs`

```csharp
using Serilog.Context;
using System.Diagnostics;

namespace Shared.Infrastructure.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();
        }

        context.Request.Headers[CorrelationIdHeader] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
```

**Extensão para registrar middleware:**

```csharp
// Arquivo: src/Shared/Infrastructure/Middleware/MiddlewareExtensions.cs
namespace Shared.Infrastructure.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
```

---

## Passo 7: Atualizar Program.cs de Cada Serviço

### 7.1 Adicionar IHttpContextAccessor no DI
```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<UserIdEnricher>();
```

### 7.2 Configurar Serilog
```csharp
using Shared.Infrastructure.Logging;

// Substituir a configuração atual do Serilog por:
SerilogConfiguration.ConfigureSerilog(builder);
```

### 7.3 Adicionar Middleware de CorrelationId
```csharp
using Shared.Infrastructure.Middleware;

// Antes de app.UseRouting():
app.UseCorrelationId();
```

### 7.4 Exemplo completo para Customer.Api/Program.cs:
```csharp
using Customer.Api.Middleware;
using Customer.Application;
using Customer.Infrastructure;
using Customer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Shared.Infrastructure.Logging;
using Shared.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
SerilogConfiguration.ConfigureSerilog(builder);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<UserIdEnricher>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "Customer API", Version = "v1" }));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("customer-service"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("MassTransit")
        .AddOtlpExporter(o => o.Endpoint = new Uri(builder.Configuration["Jaeger:Endpoint"] ?? "http://jaeger:4317")));
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgres")
    .AddRabbitMQ($"amqp://{builder.Configuration["RabbitMQ:Username"]}:{builder.Configuration["RabbitMQ:Password"]}@{builder.Configuration["RabbitMQ:Host"]}", name: "rabbitmq");

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<CustomerDbContext>().Database.MigrateAsync();

app.UseCorrelationId();
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Customer API v1"));
app.UseRouting(); 
app.UseHttpMetrics(); 
app.UseAuthorization();
app.MapControllers(); 
app.MapHealthChecks("/health");
app.MapMetrics("/metrics");
app.MapGet("/", (TimeProvider timeProvider) => Results.Ok(new
{
    Service = "Customer Service",
    Status = "Running",
    Version = "1.0.0",
    Swagger = "/swagger",
    Health = "/health",
    Metrics = "/metrics",
    Timestamp = timeProvider.GetUtcNow().UtcDateTime
}));
app.Run();
```

---

## Passo 8: Atualizar appsettings.json

### 8.1 Adicionar configuração do Loki (opcional)
```json
{
  "Loki": {
    "Url": "http://loki:3100"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  }
}
```

---

## Passo 9: Criar Endpoint Administrativo para Consulta de Logs

### 9.1 Criar IAuditRepository.cs
**Arquivo:** `src/Shared/Infrastructure/Audit/IAuditRepository.cs`

```csharp
namespace Shared.Infrastructure.Audit;

public interface IAuditRepository
{
    Task<IEnumerable<AuditLogEntity>> SearchAsync(
        DateTime from,
        DateTime to,
        string? userId = null,
        string? action = null,
        string? resource = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    Task<AuditLogEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
```

### 9.2 Criar AuditLogEntity.cs
**Arquivo:** `src/Shared/Infrastructure/Audit/AuditLogEntity.cs`

```csharp
using System.Text.Json;

namespace Shared.Infrastructure.Audit;

public class AuditLogEntity
{
    public Guid Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Service { get; set; }
    public string Environment { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Action { get; set; }
    public string Resource { get; set; }
    public string ResourceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public JsonDocument Changes { get; set; }
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
}
```

### 9.3 Criar LogsController.cs
**Arquivo:** `src/[Service]/Api/Controllers/LogsController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Audit;

namespace [Service].Api.Controllers;

[ApiController]
[Route("api/v1/admin/logs")]
[Authorize(Roles = "Admin")]
public class LogsController(IAuditRepository auditRepository, ILogger<LogsController> logger) : ControllerBase
{
    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLogs(
        DateTime? from = null,
        DateTime? to = null,
        string? userId = null,
        string? action = null,
        string? resource = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var logs = await auditRepository.SearchAsync(
            from ?? DateTime.UtcNow.AddDays(-7),
            to ?? DateTime.UtcNow,
            userId,
            action,
            resource,
            page,
            pageSize,
            ct);

        return Ok(logs);
    }

    [HttpGet("audit/{id}")]
    public async Task<IActionResult> GetAuditLog(Guid id, CancellationToken ct)
    {
        var log = await auditRepository.GetByIdAsync(id, ct);
        if (log == null) return NotFound();
        return Ok(log);
    }
}
```

---

## Passo 10: Atualizar Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
RUN mkdir -p /app/Logs/Application
RUN mkdir -p /app/Logs/Audit
RUN mkdir -p /app/Logs/Security
RUN mkdir -p /app/Logs/Background

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Customer/Api/Customer.Api.csproj", "src/Customer/Api/"]
RUN dotnet restore "src/Customer/Api/Customer.Api.csproj"
COPY . .
WORKDIR "/src/src/Customer/Api"
RUN dotnet build "Customer.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Customer.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
VOLUME ["/app/Logs"]
ENTRYPOINT ["dotnet", "Customer.Api.dll"]
```

---

## Passo 11: Configurar Kubernetes (Opcional)

### 11.1 Criar PVC para Logs
```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: customer-logs-pvc
  namespace: ecommerce
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 10Gi
  storageClassName: standard
```

### 11.2 Atualizar Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: customer-service
  namespace: ecommerce
spec:
  template:
    spec:
      containers:
      - name: customer-service
        image: customer-service:latest
        volumeMounts:
        - name: logs-volume
          mountPath: /app/Logs
      volumes:
      - name: logs-volume
        persistentVolumeClaim:
          claimName: customer-logs-pvc
```

---

## Passo 12: Adicionar Pacotes NuGet

Execute em cada projeto que precisa de logging:

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Grafana.Loki
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Sinks.Async
```

---

## Passo 13: Testar a Implementação

### 13.1 Verificar criação de diretórios
```bash
# Após iniciar a aplicação, verifique se os diretórios foram criados
ls -la Logs/
```

### 13.2 Verificar logs de aplicação
```bash
cat Logs/Application/application-20240113.log
```

### 13.3 Verificar logs de auditoria
```bash
cat Logs/Audit/audit-20240113.log
```

### 13.4 Verificar CorrelationId nos logs
```bash
# Faça uma requisição com header X-Correlation-ID
curl -H "X-Correlation-ID: test-123" http://localhost:8080/api/v1/customers

# Verifique se o CorrelationId aparece nos logs
grep "CorrelationId" Logs/Application/application-*.log
```

### 13.5 Verificar sanitização de dados sensíveis
```csharp
// Teste logando dados sensíveis
logger.LogInformation("User login: {Username}, {Password}", "user", "secret123");

// Verifique se o password foi redacted no log
cat Logs/Application/application-*.log | grep password
```

---

## Checklist de Implementação

- [ ] Criar estrutura de pastas em Shared/Infrastructure/Logging
- [ ] Criar classes de categorias (ApplicationLog, AuditLog, SecurityLog, BackgroundLog)
- [ ] Criar enrichers (TraceIdEnricher, CorrelationIdEnricher, UserIdEnricher, EnvironmentEnricher)
- [ ] Criar SensitiveDataFilter
- [ ] Criar IAuditLogger e AuditLogger
- [ ] Criar SerilogConfiguration
- [ ] Criar CorrelationIdMiddleware
- [ ] Adicionar pacotes NuGet em todos os serviços
- [ ] Atualizar Program.cs de cada serviço
- [ ] Atualizar appsettings.json
- [ ] Criar diretórios de logs em cada serviço
- [ ] Atualizar Dockerfile
- [ ] Testar criação de logs
- [ ] Testar CorrelationId
- [ ] Testar sanitização de dados sensíveis
- [ ] Testar endpoint administrativo (opcional)

---

## Considerações Importantes

1. **Performance:** Use Serilog.Sinks.Async para logging não bloqueante
2. **Armazenamento:** Configure retainedFileCountLimit para controlar espaço em disco
3. **Segurança:** Nunca logue senhas, tokens ou dados sensíveis
4. **Compliance:** Logs de auditoria devem ter retenção de pelo menos 1 ano
5. **Microserviços:** CorrelationId deve ser propagado entre todos os serviços
6. **Monitoramento:** Configure alertas no Grafana para erros em logs

---

## Próximos Passos Após Implementação

1. Configurar Loki + Promtail para centralização de logs
2. Criar dashboards no Grafana para visualização
3. Implementar serviço dedicado de auditoria (opcional)
4. Configurar rotação automática de logs antigos
5. Implementar compressão de logs (opcional)
6. Adicionar métricas de volume de logs

---

## Dúvidas Frequentes

**Q: Por que usar Serilog em vez do ILogger padrão?**  
A: Serilog oferece mais flexibilidade, enrichers, sinks e melhor integração com sistemas de observabilidade.

**Q: Preciso persistir logs em arquivo se já uso Loki?**  
A: Sim, para debugging offline e compliance. Loki é para centralização e análise.

**Q: Como garantir que logs de auditoria não sejam alterados?**  
A: Use WORM (Write Once Read Many) storage ou blockchain para imutabilidade.

**Q: Qual o tamanho máximo de arquivo de log?**  
A: Configure rollingInterval.Day e retainedFileCountLimit. Recomendado 30 dias para técnicos, 365 para auditoria.

**Q: Como propagar CorrelationId entre serviços?**  
A: Use middleware para ler/gravar header X-Correlation-ID em todas as requisições HTTP.
