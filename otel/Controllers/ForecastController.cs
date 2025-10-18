using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using otel.Models;

namespace otel.Controllers;

[ApiController]
[Route("[controller]")]
public class ForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ActivitySource _activitySource;
    private readonly Counter<long> _requestCounter;

    public ForecastController(ActivitySource activitySource, Meter meter)
    {
        _activitySource = activitySource;
        _requestCounter = meter.CreateCounter<long>("forecast_requests_total");
    }

    [HttpGet]
    public IEnumerable<WeatherForecast> Get()
    {
        _requestCounter.Add(1); // increment metric
        using var activity = _activitySource.StartActivity("weatherforecast");

        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)]
            ))
            .ToArray();

        return forecast;
    }
}
