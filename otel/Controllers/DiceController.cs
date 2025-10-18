using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace otel.Controllers;

[ApiController]
[Route("[controller]")]
public class DiceController : ControllerBase
{
    private readonly ILogger<DiceController> _logger;
    private readonly ActivitySource _activitySource;
    private readonly Counter<long> _diceCounter;

    public DiceController(ILogger<DiceController> logger, ActivitySource activitySource, Meter meter)
    {
        _logger = logger;
        _activitySource = activitySource;
        _diceCounter = meter.CreateCounter<long>("dice_rolls_total");
    }

    [HttpGet("{player?}")]
    public string RollDice(string? player)
    {
        _diceCounter.Add(1); // increment dice metric
        using var activity = _activitySource.StartActivity("rolldice");

        var result = Random.Shared.Next(1, 7);

        if (string.IsNullOrEmpty(player))
            _logger.LogInformation("Anonymous player is rolling the dice: {result}", result);
        else
            _logger.LogInformation("{player} is rolling the dice: {result}", player, result);

        return result.ToString();
    }
}
