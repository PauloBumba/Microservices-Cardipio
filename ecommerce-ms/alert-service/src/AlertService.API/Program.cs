using AlertService.Core;
using AlertService.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "Customer API", Version = "v1" }));
// ── Serilog ───────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ── Serviços ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddSingleton<AlertDispatcher>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Customer API v1"));
app.UseSerilogRequestLogging();
app.MapControllers();


app.MapGet("/", () => Results.Ok(new
{
    Service = "Customer Service",
    Status = "Running",
    Version = "1.0.0",
    Swagger = "/swagger",

  
    Timestamp = DateTime.UtcNow
}));
Log.Information("🚨 AlertService iniciado na porta {Port}", 
    builder.Configuration["ASPNETCORE_URLS"] ?? "8080");

app.Run();
