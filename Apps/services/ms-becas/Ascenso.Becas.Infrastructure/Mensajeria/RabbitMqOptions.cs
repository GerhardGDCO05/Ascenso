namespace Ascenso.Becas.Infrastructure.Mensajeria;

/// <summary>
/// Configuración del transporte RabbitMQ y del registro de consumidores.
/// </summary>
public sealed class RabbitMqOptions
{
    public bool Habilitada { get; init; } = true;

    public string Host { get; init; } = "localhost";

    public ushort Port { get; init; } = 5672;

    public string VirtualHost { get; init; } = "/";

    public string Username { get; init; } = "guest";

    public string Password { get; init; } = "guest";

    public ushort PrefetchCount { get; init; } = 16;
}

/// <summary>
/// Nombres estables de intercambios y colas usados por MS-BEC.
/// </summary>
public static class NombresMensajeria
{
    public const string PrefijoIntercambioSalida = "Ascenso.Becas:";

    public const string PrefijoIntercambioEntrada = "Ascenso.Programas:";

    public const string ColaProgramaBecaConfigurado = "ms-bec.programa-beca-configurado";

    public const string ColaCostoMatriculaActualizado = "ms-bec.costo-matricula-actualizado";

    public const string ColaConvenioUniversidadRegistrado = "ms-bec.convenio-universidad-registrado";

    public const string ColaConvenioUniversidadVencido = "ms-bec.convenio-universidad-vencido";

    public static string IntercambioSalida(string evento)
        => $"{PrefijoIntercambioSalida}{evento}";

    public static string IntercambioEntrada(string evento)
        => $"{PrefijoIntercambioEntrada}{evento}";
}
