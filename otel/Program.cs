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
const string serviceVersion = "1.0.0";

// Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File(new JsonFormatter(), "../logs/app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(t => t
        .AddSource(serviceName, serviceVersion)
        .AddAspNetCoreInstrumentation(o =>
        {
            o.RecordException = true;
            o.Filter = ctx =>
            {
                var path = ctx.Request.Path;
                return !(
                    path.StartsWithSegments("/health") ||
                    path.StartsWithSegments("/metrics") ||
                    path.StartsWithSegments("/telemetry")
                );
            };
        })
        .AddHttpClientInstrumentation(o => o.RecordException = true)
        .AddConsoleExporter()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter()
        .AddMeter(serviceName, serviceVersion)
        .AddOtlpExporter()
        .AddPrometheusExporter());

var activitySource = new ActivitySource(serviceName, serviceVersion);
var meter = new Meter(serviceName, serviceVersion);

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
