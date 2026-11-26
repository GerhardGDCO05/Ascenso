namespace Ascenso.Becas.Application.Renovacion.Puertos;

/// <summary>Consulta el calendario oficial que MS-PRG asignó al programa y periodo.</summary>
public interface IVentanaRenovacionPrograma
{
    Task<VentanaRenovacionPrograma?> ObtenerAsync(
        Guid programaBecaId, Guid periodoSolicitadoId,
        CancellationToken cancellationToken = default);
}

public sealed record VentanaRenovacionPrograma(
    DateOnly Inicio, DateOnly Fin, bool Activo,
    bool? PermiteRenovacionTardiaExcepcional = null);
