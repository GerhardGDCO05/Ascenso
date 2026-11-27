using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del puerto de consulta de jefatura activa de comité evaluador por convocatoria.
/// </summary>
public sealed class ConsultaJefaturaComiteRepository : IConsultaJefaturaComiteRepository
{
    private readonly PostulacionesDbContext _contexto;

    public ConsultaJefaturaComiteRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public async Task<bool> EsJefaturaActivaEnConvocatoriaAsync(
        Guid convocatoriaId,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        ValidarArgumentos(convocatoriaId, usuarioId);

        return await ConsultarComiteConJefaturaActiva(convocatoriaId, usuarioId)
            .AnyAsync(cancellationToken);
    }

    internal IQueryable<ComiteEvaluador> ConsultarComiteConJefaturaActiva(
        Guid convocatoriaId,
        Guid usuarioId)
        => FiltrarComiteConJefaturaActiva(
            _contexto.ComitesEvaluadores.AsNoTracking(),
            convocatoriaId,
            usuarioId);

    internal static IQueryable<ComiteEvaluador> FiltrarComiteConJefaturaActiva(
        IQueryable<ComiteEvaluador> consulta,
        Guid convocatoriaId,
        Guid usuarioId)
        => consulta.Where(comite =>
            comite.ConvocatoriaId == convocatoriaId
            && comite.Activo
            && comite.JefaturaUsuarioId == usuarioId);

    private static void ValidarArgumentos(Guid convocatoriaId, Guid usuarioId)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la convocatoria es obligatorio.",
                nameof(convocatoriaId));
        }

        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del usuario es obligatorio.",
                nameof(usuarioId));
        }
    }
}
