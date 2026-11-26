using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Ascenso.Programas.Api.Seguridad;

/// <summary>
/// Esquema de arranque cuando Keycloak no está configurado: no autentica a nadie,
/// de modo que las rutas protegidas responden <c>401</c> en lugar de fallar con 500.
/// </summary>
public sealed class AutenticacionNoConfiguradaHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Esquema = "SinConfigurar";

    public AutenticacionNoConfiguradaHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());
}
