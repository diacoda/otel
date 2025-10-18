using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Json;
using otel.Controllers;

var builder = WebApplication.CreateBuilder(args);
const string serviceName = "roll-dice";

// Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File(new JsonFormatter(), "../logs/app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ActivitySource & Meter
var activitySource = new ActivitySource("Rolldice");
var meter = new Meter("roll-dice", "1.0");

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName))
    .WithTracing(t => t
        .AddSource("Rolldice")
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter()
        .AddMeter("roll-dice")
        .AddOtlpExporter()
        .AddPrometheusExporter());

// Register ActivitySource & Meter as DI
builder.Services.AddSingleton(activitySource);
builder.Services.AddSingleton(meter);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.MapControllers();
app.Run();
