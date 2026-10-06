namespace PandaAPI.DTOs.Auth;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTimeOffset ExpiresAtBrasilia =>
        new DateTimeOffset(DateTime.SpecifyKind(ExpiresAtUtc, DateTimeKind.Utc))
            .ToOffset(TimeSpan.FromHours(-3));
}
