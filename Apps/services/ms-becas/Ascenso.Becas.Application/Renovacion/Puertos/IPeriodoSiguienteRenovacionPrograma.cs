namespace Ascenso.Becas.Application.Renovacion.Puertos;

/// <summary>Identifica el siguiente periodo y su ventana desde el calendario oficial.</summary>
public interface IPeriodoSiguienteRenovacionPrograma
{
    Task<PeriodoSiguienteRenovacionPrograma?> ObtenerAsync(
        Guid programaBecaId, Guid periodoOrigenId,
        CancellationToken cancellationToken = default);
}

public sealed record PeriodoSiguienteRenovacionPrograma(
    Guid ProgramaBecaId, Guid PeriodoOrigenId, Guid PeriodoSiguienteId,
    VentanaRenovacionPrograma Ventana);
