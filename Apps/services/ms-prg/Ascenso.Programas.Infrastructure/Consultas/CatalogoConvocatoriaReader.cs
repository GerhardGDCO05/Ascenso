using Ascenso.Programas.Application.Convocatorias.Abstracciones;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Universidades.Agregados;
using Ascenso.Programas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Programas.Infrastructure.Consultas;

/// <summary>
/// Lee el programa, sus cupos, convenios vigentes y carreras elegibles sin modificar
/// los repositorios de los requerimientos de programas y universidades.
/// </summary>
public sealed class CatalogoConvocatoriaReader : ICatalogoConvocatoriaReader
{
    private readonly ProgramasDbContext _contexto;

    public CatalogoConvocatoriaReader(ProgramasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public async Task<ProgramaParaConvocatoria?> ObtenerProgramaAsync(
        Guid programaBecaId,
        DateOnly fechaReferencia,
        CancellationToken cancellationToken = default)
    {
        if (programaBecaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del programa de becas es obligatorio.", nameof(programaBecaId));
        }

        if (_contexto.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "La lectura bloqueante del programa debe ejecutarse dentro de una transacción.");
        }

        var programa = await _contexto.ProgramasBeca
            .FromSqlInterpolated(
                $"SELECT * FROM programas.programas_beca WHERE id = {programaBecaId} FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == programaBecaId, cancellationToken);

        if (programa is null)
        {
            return null;
        }

        var universidadesElegibles = programa.CriteriosElegibilidad?.UniversidadesElegiblesIds ?? Array.Empty<Guid>();
        var carrerasElegibles = programa.CriteriosElegibilidad?.CarrerasElegiblesIds ?? Array.Empty<Guid>();

        var universidades = new List<UniversidadAliada>();

        if (universidadesElegibles.Count > 0)
        {
            universidades = await _contexto.UniversidadesAliadas
                .AsNoTracking()
                .Include(universidad => universidad.Convenios)
                .Include(universidad => universidad.Carreras)
                .Where(universidad => universidadesElegibles.Contains(universidad.Id))
                .ToListAsync(cancellationToken);
        }

        var universidadesVigentes = universidades
            .Where(universidad => universidad.Estado == EstadoUniversidad.Activa
                && universidad.Convenios.Any(convenio => convenio.Estado == EstadoConvenio.Vigente
                    && convenio.FechaFin >= fechaReferencia))
            .ToList();

        var universidadesConConvenioVigente = universidadesVigentes
            .Select(universidad => universidad.Id)
            .Distinct()
            .ToArray();

        var carrerasConConvenioVigente = universidadesVigentes
            .SelectMany(universidad => universidad.Carreras)
            .Where(carrera => carrera.Activa && carrerasElegibles.Contains(carrera.Id))
            .Select(carrera => carrera.Id)
            .Distinct()
            .ToArray();

        return new ProgramaParaConvocatoria(
            programa.Id,
            programa.Codigo,
            programa.Nombre,
            programa.Estado,
            programa.CuposDisponibles,
            programa.PresupuestoTotalAsignado,
            universidadesConConvenioVigente,
            carrerasConConvenioVigente);
    }
}
