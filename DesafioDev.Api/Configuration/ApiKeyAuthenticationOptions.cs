namespace DesafioDev.Api.Configuration;

public class ApiKeyAuthenticationOptions
{
    public const string SectionName = "ApiKey";
    public string Key { get; set; } = string.Empty;
    public string HeaderName { get; set; } = "X-API-Key";
}
