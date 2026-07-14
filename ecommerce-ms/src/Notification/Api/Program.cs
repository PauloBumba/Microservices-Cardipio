using Microsoft.EntityFrameworkCore;
using Notification.Infrastructure;
using Notification.Infrastructure.Persistence;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Shared.Infrastructure.Logging;
using Shared.Infrastructure.Middleware;
using Serilog;
var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
SerilogConfiguration.ConfigureSerilog(builder);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title="Notification API", Version="v1" }));
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("notification-service"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("MassTransit")
        .AddOtlpExporter(o => o.Endpoint = new Uri(builder.Configuration["Jaeger:Endpoint"] ?? "http://jaeger:4317")));
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgres")
    .AddRabbitMQ($"amqp://{builder.Configuration["RabbitMQ:Username"]}:{builder.Configuration["RabbitMQ:Password"]}@{builder.Configuration["RabbitMQ:Host"]}", name: "rabbitmq");
   

app.UseCorrelationId();
var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.MigrateAsync();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification API v1"));
app.UseRouting();
app.UseHttpMetrics();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics("/metrics");
app.MapGet("/", (TimeProvider timeProvider) => Results.Ok(new
{
    Service = "Notification Service",
    Status = "Running",
    Version = "1.0.0",
    Swagger = "/swagger",
    Health = "/health",
    Metrics = "/metrics",
    Timestamp = timeProvider.GetUtcNow().UtcDateTime
}));
app.Run();
