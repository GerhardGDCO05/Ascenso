using Microsoft.Extensions.Configuration;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>
/// Configuración de los clientes HTTP internos. Cada cliente apunta a la raíz
/// del microservicio proveedor y comparte el timeout máximo exigido por el
/// contrato de integración.
/// </summary>
public sealed class ClientesOptions
{
    public int TimeoutSegundos { get; init; } = 3;

    public ClienteHttpOptions Postulaciones { get; init; } = new();

    public ClienteHttpOptions Programas { get; init; } = new();

    public ClienteHttpOptions Plataforma { get; init; } = new();

    public static ClientesOptions Leer(IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        var seccion = configuracion.GetSection("Clientes");

        return new ClientesOptions
        {
            TimeoutSegundos = LeerEntero(seccion["TimeoutSegundos"], 3),
            Postulaciones = LeerCliente(seccion.GetSection("Postulaciones")),
            Programas = LeerCliente(seccion.GetSection("Programas")),
            Plataforma = LeerCliente(seccion.GetSection("Plataforma"))
        };
    }

    private static ClienteHttpOptions LeerCliente(IConfigurationSection seccion)
        => new()
        {
            BaseUrl = seccion["BaseUrl"] ?? string.Empty,
            UsaServiceToken = !string.Equals(
                seccion["UsaServiceToken"],
                "false",
                StringComparison.OrdinalIgnoreCase)
        };

    private static int LeerEntero(string? valor, int predeterminado)
        => int.TryParse(
            valor,
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out var numero)
            && numero > 0
            ? numero
            : predeterminado;
}

/// <summary>
/// Dirección base y política de autenticación de un cliente interno.
/// </summary>
public sealed class ClienteHttpOptions
{
    public string BaseUrl { get; init; } = string.Empty;

    public bool UsaServiceToken { get; init; } = true;
}
