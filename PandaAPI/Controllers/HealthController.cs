using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PandaAPI.Controllers;

[ApiController]
[Route("health")]
[Tags("Health")]
[AllowAnonymous]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    [HttpGet("live", Name = "HealthLive")]
    [EnableRateLimiting("health-live")]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public Task<ActionResult<HealthCheckResponse>> Live(CancellationToken cancellationToken)
    {
        return GetHealthStatusAsync("live", cancellationToken);
    }

    [HttpGet("ready", Name = "HealthReady")]
    [EnableRateLimiting("health-ready")]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public Task<ActionResult<HealthCheckResponse>> Ready(CancellationToken cancellationToken)
    {
        return GetHealthStatusAsync("ready", cancellationToken);
    }

    private async Task<ActionResult<HealthCheckResponse>> GetHealthStatusAsync(
        string tag,
        CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(
            check => check.Tags.Contains(tag),
            cancellationToken);
        var response = new HealthCheckResponse(report.Status.ToString());

        return report.Status == HealthStatus.Unhealthy
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, response)
            : Ok(response);
    }
}

public sealed record HealthCheckResponse(string Status);
