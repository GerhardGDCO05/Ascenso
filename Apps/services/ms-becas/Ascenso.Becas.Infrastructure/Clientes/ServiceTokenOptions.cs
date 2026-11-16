using Microsoft.Extensions.Configuration;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>
/// Configuración del <em>client credentials grant</em> usado para obtener los
/// <em>Service Tokens</em> de Keycloak. El secreto se inyecta por variable de
/// entorno o gestor de secretos y nunca se registra en logs.
/// </summary>
public sealed class ServiceTokenOptions
{
    public string TokenEndpoint { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public int MargenRenovacionSegundos { get; init; } = 30;

    public int TimeoutSegundos { get; init; } = 3;

    public static ServiceTokenOptions Leer(IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        const string Seccion = "Keycloak:ServiceToken";
        var endpoint = configuracion[$"{Seccion}:TokenEndpoint"];

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            var autoridad = configuracion[$"{Seccion}:Authority"]
                ?? configuracion["Jwt:Authority"];

            if (!string.IsNullOrWhiteSpace(autoridad))
            {
                endpoint = $"{autoridad.TrimEnd('/')}/protocol/openid-connect/token";
            }
        }

        return new ServiceTokenOptions
        {
            TokenEndpoint = endpoint ?? string.Empty,
            ClientId = configuracion[$"{Seccion}:ClientId"] ?? string.Empty,
            ClientSecret = configuracion[$"{Seccion}:ClientSecret"] ?? string.Empty,
            MargenRenovacionSegundos = LeerEntero(
                configuracion[$"{Seccion}:MargenRenovacionSegundos"],
                30),
            TimeoutSegundos = LeerEntero(
                configuracion[$"{Seccion}:TimeoutSegundos"],
                3)
        };
    }

    private static int LeerEntero(string? valor, int predeterminado)
        => int.TryParse(
            valor,
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out var numero)
            && numero >= 0
            ? numero
            : predeterminado;
}
