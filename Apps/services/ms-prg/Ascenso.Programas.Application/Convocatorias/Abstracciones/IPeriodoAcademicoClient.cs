namespace Ascenso.Programas.Application.Convocatorias.Abstracciones;

/// <summary>
/// Datos mínimos del periodo académico publicados por el calendario institucional.
/// </summary>
public sealed record PeriodoAcademicoInformacion(
    Guid Id,
    string Nombre,
    string Estado);

/// <summary>
/// Puerto hacia MS-PLT para validar la existencia y vigencia del periodo académico.
/// </summary>
public interface IPeriodoAcademicoClient
{
    Task<PeriodoAcademicoInformacion?> ObtenerPorIdAsync(
        string periodoAcademicoId,
        CancellationToken cancellationToken = default);
}
