using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Json; // for JSON output

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter()) // keep JSON logs on console
    .WriteTo.File(new JsonFormatter(), "../logs/app.log", rollingInterval: RollingInterval.Day) // JSON logs to file
    .CreateLogger();

ActivitySource MyActivitySource = new("Rolldice");
var myMeter = new Meter("roll-dice", "1.0");

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "roll-dice";
builder.Host.UseSerilog();

builder.Logging.AddOpenTelemetry(options =>
{
    options
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName))
        .AddConsoleExporter();
});
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddSource("Rolldice")
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter((exporterOptions, metricReaderOptions) =>
        {
            metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
        })
        .AddMeter("roll-dice")
        .AddOtlpExporter((exporterOptions, metricReaderOptions) =>
        {
            exporterOptions.Endpoint = new Uri("http://localhost:9090/api/v1/otlp/v1/metrics");
            exporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
            metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
        })
        .AddPrometheusExporter());

/* builder.Logging.AddOpenTelemetry(options =>
{
    options
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName))
        .AddConsoleExporter(options =>
        {
            options.Targets = ConsoleExporterOutputTargets.Console;
        });
});
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});
 */
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        using var parent = MyActivitySource.StartActivity("weatherforecast");
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.MapGet("/rolldice/{player?}", HandleRollDice)
    .WithName("RollDice");

app.Run();

string HandleRollDice([FromServices] ILogger<Program> logger, string? player)
{
    myMeter.CreateCounter<long>("dice_rolls_total").Add(1);
    using var parent = MyActivitySource.StartActivity("rolldice");

    var result = RollDice();

    if (string.IsNullOrEmpty(player))
    {
        logger.LogInformation("Anonymous player is rolling the dice: {result}", result);
    }
    else
    {
        logger.LogInformation("{player} is rolling the dice: {result}", player, result);
    }

    return result.ToString(CultureInfo.InvariantCulture);
}

int RollDice()
{
    return Random.Shared.Next(1, 7);
}


record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
