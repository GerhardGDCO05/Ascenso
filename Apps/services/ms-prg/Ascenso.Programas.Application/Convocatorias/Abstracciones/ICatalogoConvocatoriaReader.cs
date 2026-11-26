using Ascenso.Programas.Domain.Enums;

namespace Ascenso.Programas.Application.Convocatorias.Abstracciones;

/// <summary>
/// Instantánea de solo lectura del programa de becas usada para validar una convocatoria.
/// </summary>
public sealed record ProgramaParaConvocatoria(
    Guid ProgramaId,
    string Codigo,
    string Nombre,
    EstadoProgramaBeca Estado,
    int CuposDisponibles,
    decimal PresupuestoTotalAsignado,
    IReadOnlyCollection<Guid> UniversidadesConConvenioVigente,
    IReadOnlyCollection<Guid> CarrerasElegiblesConConvenioVigente);

/// <summary>
/// Puerto de lectura del programa, sus cupos y sus convenios vigentes, sin acoplarse
/// a los repositorios de otros requerimientos funcionales.
/// </summary>
public interface ICatalogoConvocatoriaReader
{
    Task<ProgramaParaConvocatoria?> ObtenerProgramaAsync(
        Guid programaBecaId,
        DateOnly fechaReferencia,
        CancellationToken cancellationToken = default);
}
