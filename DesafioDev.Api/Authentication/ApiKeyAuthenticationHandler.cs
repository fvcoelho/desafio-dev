using System.Security.Claims;
using System.Text.Encodings.Web;
using DesafioDev.Api.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DesafioDev.Api.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeySchemeName = "ApiKey";
    private readonly ApiKeyAuthenticationOptions _apiKeyOptions;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _apiKeyOptions = configuration
            .GetSection(ApiKeyAuthenticationOptions.SectionName)
            .Get<ApiKeyAuthenticationOptions>() ?? new ApiKeyAuthenticationOptions();
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrEmpty(_apiKeyOptions.Key))
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key not configured on server"));
        }

        if (!Request.Headers.TryGetValue(_apiKeyOptions.HeaderName, out var apiKeyHeader))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing {_apiKeyOptions.HeaderName} header"));
        }

        var providedApiKey = apiKeyHeader.ToString();

        if (!_apiKeyOptions.Key.Equals(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "ApiKeyUser"),
            new Claim(ClaimTypes.AuthenticationMethod, ApiKeySchemeName)
        };

        var identity = new ClaimsIdentity(claims, ApiKeySchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeySchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.Append("WWW-Authenticate", $"ApiKey realm=\"FinanceApp\", header=\"{_apiKeyOptions.HeaderName}\"");
        return Task.CompletedTask;
    }
}
