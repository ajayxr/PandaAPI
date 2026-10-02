using Microsoft.EntityFrameworkCore;
using PandaAPI.Data;
using Microsoft.AspNetCore.RateLimiting;
using PandaAPI.Middleware;
using PandaAPI.Services;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.HttpOverrides;
using PandaAPI.Interfaces;
using PandaAPI.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PandaAPI.Configuration;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<CnpjPdfService>();
builder.Services.AddOptions<CnpjAiOptions>()
    .BindConfiguration(CnpjAiOptions.SectionName)
    .Validate(options =>
        Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) &&
        baseUri.Scheme == Uri.UriSchemeHttps,
        "ExternalApis:CnpjAi:BaseUrl deve ser uma URL HTTPS absoluta.")
    .Validate(options =>
        !string.IsNullOrWhiteSpace(options.ApiKey) &&
        !options.ApiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase),
        "Configure ExternalApis:CnpjAi:ApiKey com o token, sem o prefixo 'Bearer '.")
    .ValidateOnStart();

builder.Services.AddHttpClient<GetCnpjService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<CnpjAiOptions>>().Value;
    var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : $"{options.BaseUrl}/";
    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
});
builder.Services.AddAuthorization();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
    Encoding.UTF8.GetByteCount(jwtOptions.SecretKey) < 32)
{
    throw new InvalidOperationException(
        "Configure Jwt:Issuer, Jwt:Audience e Jwt:SecretKey; o segredo deve possuir ao menos 32 bytes.");
}

builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true, 
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        var components = document.Components ??= new OpenApiComponents();
        var schemes = components.SecuritySchemes
            ??= new Dictionary<string, IOpenApiSecurityScheme>();

        schemes.Add("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Informe o token JWT gerado pelo endpoint de login."
        });

        document.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            }
        ];

        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, _) =>
    {
        var path = context.Description.RelativePath?.TrimStart('/');
        if (path is "health/live" or "health/ready")
        {
            operation.Security = [];
        }

        return Task.CompletedTask;
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não foi encontrada.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DatabaseHealthCheck>(
        "database",
        tags: ["ready"],
        timeout: TimeSpan.FromSeconds(3));

builder.Services.AddRateLimiting(builder.Configuration);

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();
}
 
app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.MapStaticAssets();

var enableSwagger = app.Environment.IsDevelopment() ||
                    builder.Configuration.GetValue<bool>("EnableSwagger");

if (enableSwagger)
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "PandaAPI v1"));
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
