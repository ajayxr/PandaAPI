namespace PandaAPI.Configuration;

public sealed class CnpjAiOptions
{
    public const string SectionName = "ExternalApis:CnpjAi";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
