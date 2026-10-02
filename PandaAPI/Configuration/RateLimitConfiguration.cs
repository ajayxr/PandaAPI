using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace PandaAPI.Middleware;

public static class RateLimitConfiguration
{
    public static void AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var registerLimit =
            configuration.GetValue<int>("Rate_Limit_Register");

        var registerWindow =
            configuration.GetValue<int>("Rate_Limit_Register_Window_Seconds");

        var validateLimit =
            configuration.GetValue<int>("Rate_Limit_Validate");

        var validateWindow =
            configuration.GetValue<int>("Rate_Limit_Validate_Window_Seconds");

        var loginLimit = configuration.GetValue<int>("Rate_Limit_Login");
        var loginWindow = configuration.GetValue<int>("Rate_Limit_Login_Window_Seconds");
        var healthLiveLimit = configuration.GetValue<int>("Rate_Limit_Health_Live");
        var healthLiveWindow = configuration.GetValue<int>("Rate_Limit_Health_Live_Window_Seconds");
        var healthReadyLimit = configuration.GetValue<int>("Rate_Limit_Health_Ready");
        var healthReadyWindow = configuration.GetValue<int>("Rate_Limit_Health_Ready_Window_Seconds");

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("register", config =>
            {
                config.PermitLimit = registerLimit;
                config.Window = TimeSpan.FromSeconds(registerWindow);
                config.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("validate", config =>
            {
                config.PermitLimit = validateLimit;
                config.Window = TimeSpan.FromSeconds(validateWindow);
                config.QueueLimit = 0;
            });

            options.AddPolicy("login", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = loginLimit,
                        Window = TimeSpan.FromSeconds(loginWindow),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy("health-live", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = healthLiveLimit,
                        Window = TimeSpan.FromSeconds(healthLiveWindow),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy("health-ready", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = healthReadyLimit,
                        Window = TimeSpan.FromSeconds(healthReadyWindow),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });
    }
}