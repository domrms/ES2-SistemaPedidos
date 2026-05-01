using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ES2_SistemaPedidos.Api.Security;

public static class PadroesAutenticacaoBearerSimples
{
    public const string EsquemaAutenticacao = "BearerSimples";
}

public sealed class ManipuladorAutenticacaoBearerSimples(
    IOptionsMonitor<AuthenticationSchemeOptions> opcoes,
    ILoggerFactory fabricaRegistrador,
    UrlEncoder codificador,
    IConfiguration configuracao)
    : AuthenticationHandler<AuthenticationSchemeOptions>(opcoes, fabricaRegistrador, codificador)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var autorizacao = Request.Headers.Authorization.ToString();
        if (!autorizacao.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Token Bearer ausente."));
        }

        var token = autorizacao["Bearer ".Length..].Trim();
        var tokensValidos = configuracao.GetSection("Autenticacao:TokensBearer").Get<string[]>()
            ?? configuracao.GetSection("Authentication:BearerTokens").Get<string[]>()
            ?? [configuracao["AUTH_BEARER_TOKEN"] ?? "dev-token"];

        if (!tokensValidos.Any(tokenValido => string.Equals(tokenValido, token, StringComparison.Ordinal)))
        {
            return Task.FromResult(AuthenticateResult.Fail("Token Bearer invalido."));
        }

        var identidade = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "usuario-token-bearer")],
            PadroesAutenticacaoBearerSimples.EsquemaAutenticacao);
        var principal = new ClaimsPrincipal(identidade);
        var ticket = new AuthenticationTicket(principal, PadroesAutenticacaoBearerSimples.EsquemaAutenticacao);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
