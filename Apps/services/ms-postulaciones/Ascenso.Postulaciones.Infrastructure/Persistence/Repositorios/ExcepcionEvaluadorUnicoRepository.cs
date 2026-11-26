using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del repositorio append-only de excepciones de evaluador único.
/// </summary>
public sealed class ExcepcionEvaluadorUnicoRepository : IExcepcionEvaluadorUnicoRepository
{
    private readonly PostulacionesDbContext _contexto;

    public ExcepcionEvaluadorUnicoRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public async Task<AutorizacionExcepcionEvaluadorUnico?> ObtenerPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        if (postulacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la postulación es obligatorio.",
                nameof(postulacionId));
        }

        return await ConsultarPorPostulacion(postulacionId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<AutorizacionExcepcionEvaluadorUnico> ConsultarPorPostulacion(Guid postulacionId)
        => _contexto.ExcepcionesEvaluadorUnico
            .AsNoTracking()
            .Where(autorizacion => autorizacion.PostulacionId == postulacionId);

    public async Task AgregarAsync(
        AutorizacionExcepcionEvaluadorUnico autorizacion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(autorizacion);

        await _contexto.ExcepcionesEvaluadorUnico.AddAsync(autorizacion, cancellationToken);
    }
}
