using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Order.Api.Middleware;
using Order.Application;
using Order.Infrastructure;
using Order.Infrastructure.Persistence;
using Prometheus;
using Serilog;
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext().Enrich.WithProperty("Service", "order-service").WriteTo.Console());
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title="Order API", Version="v1" }));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("order-service"))
        .AddAspNetCoreInstrumentation().AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation().AddSource("MassTransit")
        .AddOtlpExporter(o => o.Endpoint = new Uri(builder.Configuration["Jaeger:Endpoint"] ?? "http://jaeger:4317")));
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgres");
    
var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<OrderDbContext>().Database.MigrateAsync();
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order API v1"));
app.UseRouting(); app.UseHttpMetrics(); app.UseAuthorization();
app.MapControllers(); app.MapHealthChecks("/health"); app.MapMetrics("/metrics");
app.MapGet("/", () => Results.Ok(new
{
    Service = "Order Service",
    Status = "Running",
    Version = "1.0.0",
    Swagger = "/swagger",
    Health = "/health",
    Metrics = "/metrics",
    Timestamp = DateTime.UtcNow
}));
app.Run();
