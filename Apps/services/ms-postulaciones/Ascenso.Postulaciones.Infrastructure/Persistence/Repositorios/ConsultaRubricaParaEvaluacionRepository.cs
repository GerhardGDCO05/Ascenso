using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del puerto segregado de consulta de rúbricas para el flujo de evaluación.
/// </summary>
public sealed class ConsultaRubricaParaEvaluacionRepository : IConsultaRubricaParaEvaluacionRepository
{
    private readonly PostulacionesDbContext _contexto;

    public ConsultaRubricaParaEvaluacionRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<RubricaEvaluacion?> ObtenerPorProgramaYConvocatoriaAsync(
        Guid programaBecaId,
        Guid convocatoriaId,
        CancellationToken cancellationToken = default)
    {
        if (programaBecaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del programa de beca es obligatorio.",
                nameof(programaBecaId));
        }

        if (convocatoriaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la convocatoria es obligatorio.",
                nameof(convocatoriaId));
        }

        return ConsultarPorProgramaYConvocatoria(programaBecaId, convocatoriaId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<RubricaEvaluacion> ConsultarPorProgramaYConvocatoria(
        Guid programaBecaId,
        Guid convocatoriaId)
        => _contexto.Rubricas
            .AsNoTracking()
            .Include(rubrica => rubrica.Criterios)
            .Where(rubrica => rubrica.ProgramaBecaId == programaBecaId
                && rubrica.ConvocatoriaId == convocatoriaId);
}
