namespace Ascenso.Becas.Application.SeguimientoAcademico.Puertos;

/// <summary>Obtiene la fecha límite vigente definida por MS-PRG para el programa y periodo.</summary>
public interface ICalendarioAcademicoPrograma
{
    Task<DateOnly?> ObtenerFechaLimiteReporteAsync(
        Guid programaBecaId, Guid periodoAcademicoId,
        CancellationToken cancellationToken = default);
}
