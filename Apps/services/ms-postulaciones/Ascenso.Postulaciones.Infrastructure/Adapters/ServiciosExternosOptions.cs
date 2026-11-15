namespace Ascenso.Postulaciones.Infrastructure.Adapters;

public sealed class ServiciosExternosOptions
{
    public const string Seccion = "ServiciosExternos";

    public ServicioExternoEndpointOptions Plataforma { get; init; } = new();

    public ServicioExternoEndpointOptions Programas { get; init; } = new();

    public int TimeoutSegundos { get; init; } = 3;
}

public sealed class ServicioExternoEndpointOptions
{
    public string UrlBase { get; init; } = string.Empty;
}

public sealed class PeriodoActualOptions
{
    public const string Seccion = "PeriodoActual";

    public string FallbackId { get; init; } = string.Empty;
}
