using Microsoft.EntityFrameworkCore;
using Notification.Infrastructure;
using Notification.Infrastructure.Persistence;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext().Enrich.WithProperty("Service", "notification-service").WriteTo.Console());
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title="Notification API", Version="v1" }));
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("notification-service"))
        .AddAspNetCoreInstrumentation().AddSource("MassTransit")
        .AddOtlpExporter(o => o.Endpoint = new Uri(builder.Configuration["Jaeger:Endpoint"] ?? "http://jaeger:4317")));
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgres")
    .AddRabbitMQ(rabbitConnectionString:
        $"amqp://{builder.Configuration["RabbitMQ:Username"]}:{builder.Configuration["RabbitMQ:Password"]}@{builder.Configuration["RabbitMQ:Host"]}",
        name: "rabbitmq");
var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.MigrateAsync();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification API v1"));
app.UseRouting(); app.UseHttpMetrics(); app.UseAuthorization();
app.MapControllers(); app.MapHealthChecks("/health"); app.MapMetrics("/metrics");
app.Run();
