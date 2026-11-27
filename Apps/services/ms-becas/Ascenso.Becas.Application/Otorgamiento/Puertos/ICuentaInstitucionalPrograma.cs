namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

/// <summary>Fuente autorizada del snapshot cifrado de la cuenta institucional.</summary>
public interface ICuentaInstitucionalPrograma
{
    Task<string?> ObtenerSnapshotCifradoAsync(Guid programaBecaId,
        Guid periodoAcademicoId, CancellationToken cancellationToken = default);
}
