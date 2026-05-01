using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ES2_SistemaPedidos.Api.Security;

public static class SimpleBearerAuthenticationDefaults
{
    public const string AuthenticationScheme = "SimpleBearer";
}

public sealed class SimpleBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Bearer token."));
        }

        var token = authorization["Bearer ".Length..].Trim();
        var validTokens = configuration.GetSection("Authentication:BearerTokens").Get<string[]>()
            ?? [configuration["AUTH_BEARER_TOKEN"] ?? "dev-token"];

        if (!validTokens.Any(validToken => string.Equals(validToken, token, StringComparison.Ordinal)))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Bearer token."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "bearer-token-user")],
            SimpleBearerAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SimpleBearerAuthenticationDefaults.AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
