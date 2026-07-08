using AlertService.Core;
using AlertService.Infrastructure;
using AlertService.Infrastructure.MCP;
using ModelContextProtocol.AspNetCore;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "Alert Service / Incident Gateway", Version = "v2" }));

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()

    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ── OpenTelemetry: Tracing para LLM (Ollama) com convenções gen_ai* ─────
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("alert-service", serviceVersion: "2.1.0"))
    .WithTracing(tracing => tracing
        .AddSource("AlertService.AI")
        .AddSource("AlertService.MCP")
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(options =>
        {
            if (!string.IsNullOrEmpty(otlpEndpoint))
                options.Endpoint = new Uri(otlpEndpoint);
        }));

builder.Services.AddControllers();
builder.Services.AddSingleton<AlertDispatcher>();
builder.Services.AddInfrastructure(builder.Configuration);

// ── MCP Server: expõe tools para agentes externos (Cursor, Claude, etc.) ─────
var mcpServerEnabled = builder.Configuration.GetValue<bool>("FeatureFlags:McpServerEnabled");
if (mcpServerEnabled)
{
    builder.Services
        .AddMcpServer()
        .WithHttpTransport(options => options.Stateless = true)
        .WithTools<IncidentMcpTools>();
}

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Incident Gateway v2"));
app.UseSerilogRequestLogging();
app.MapControllers();

if (mcpServerEnabled)
    app.MapMcp("/mcp");

app.MapGet("/", () => Results.Ok(new
{
    Service = "Alert Service / Incident Gateway",
    Status = "Running",
    Version = "2.1.0",
    Swagger = "/swagger",
    McpEndpoint = mcpServerEnabled ? "/mcp" : null,
    Timestamp = DateTime.UtcNow
}));

Log.Information("🚨 AlertService (Incident Gateway v2.1) iniciado — MCP Server: {McpEnabled}", mcpServerEnabled);
app.Run();
