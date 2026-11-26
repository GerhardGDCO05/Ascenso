namespace Ascenso.Programas.Infrastructure.Integraciones;

/// <summary>
/// Configuración del contrato interno de periodos académicos de MS-PLT.
/// </summary>
public sealed class PeriodoAcademicoOptions
{
    public const string Seccion = "ServiciosExternos:PeriodoAcademico";

    public string? UrlBase { get; set; }

    public int TimeoutSegundos { get; set; } = 10;
}
