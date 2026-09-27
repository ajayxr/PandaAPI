using Microsoft.AspNetCore.RateLimiting;
using PandaAPI.Middleware;
using PandaAPI.Services;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<CpfService>();
builder.Services.AddAuthorization();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        var components = document.Components ??= new OpenApiComponents();
        var schemes = components.SecuritySchemes
            ??= new Dictionary<string, IOpenApiSecurityScheme>();

        schemes.Add("ApiKey", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            Name = "X-Api-Key",
            In = ParameterLocation.Header,
            Description = "Digite sua chave de API."
        });

        document.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("ApiKey", document)] = []
            }
        ];

        return Task.CompletedTask;
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("default", config =>
    {
        config.PermitLimit = 30;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueLimit = 0;
    });
});

var app = builder.Build();

app.MapStaticAssets();

var enableSwagger = app.Environment.IsDevelopment() ||
                    builder.Configuration.GetValue<bool>("EnableSwagger");

if (enableSwagger)
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "PandaAPI v1"));
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("default");

app.Run();
