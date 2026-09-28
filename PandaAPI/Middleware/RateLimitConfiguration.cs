using Microsoft.AspNetCore.RateLimiting;

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

        services.AddRateLimiter(options =>
        {
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
        });
    }
}