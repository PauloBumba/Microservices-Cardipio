using AlertService.Core;
using AlertService.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

app.UseSerilogRequestLogging();
app.MapControllers();

Log.Information("🚨 AlertService iniciado na porta {Port}", 
    builder.Configuration["ASPNETCORE_URLS"] ?? "8080");

app.Run();
