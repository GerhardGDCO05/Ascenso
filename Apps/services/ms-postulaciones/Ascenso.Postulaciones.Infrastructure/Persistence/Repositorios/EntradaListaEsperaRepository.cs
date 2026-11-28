using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del repositorio de entradas de lista de espera.
/// </summary>
public sealed class EntradaListaEsperaRepository : IEntradaListaEsperaRepository
{
    private readonly PostulacionesDbContext _contexto;

    public EntradaListaEsperaRepository(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public async Task AgregarRangoAsync(
        IReadOnlyCollection<EntradaListaEspera> entradas,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entradas);

        if (entradas.Count == 0)
        {
            return;
        }

        if (entradas.Any(entrada => entrada is null))
        {
            throw new ArgumentException(
                "La colección de entradas de lista de espera no puede contener elementos nulos.",
                nameof(entradas));
        }

        await _contexto.EntradasListaEspera.AddRangeAsync(entradas, cancellationToken);
    }

    public Task<EntradaListaEspera?> ObtenerPorIdAsync(
        Guid entradaListaEsperaId,
        CancellationToken cancellationToken = default)
    {
        if (entradaListaEsperaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la entrada de lista de espera es obligatorio.",
                nameof(entradaListaEsperaId));
        }

        return ConsultarPorId(entradaListaEsperaId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<EntradaListaEspera> ConsultarPorId(Guid entradaListaEsperaId)
        => _contexto.EntradasListaEspera
            .AsNoTracking()
            .Where(entrada => entrada.Id == entradaListaEsperaId);

    public Task<EntradaListaEspera?> ObtenerPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        if (postulacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la postulación es obligatorio.",
                nameof(postulacionId));
        }

        return ConsultarPorPostulacion(postulacionId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<EntradaListaEspera> ConsultarPorPostulacion(Guid postulacionId)
        => _contexto.EntradasListaEspera
            .AsNoTracking()
            .Where(entrada => entrada.PostulacionId == postulacionId);

    public async Task<IReadOnlyList<EntradaListaEspera>> ListarPorConvocatoriaAsync(
        Guid convocatoriaId,
        EstadoListaEspera? estado = null,
        CancellationToken cancellationToken = default)
    {
        ValidarConvocatoriaId(convocatoriaId);

        if (estado.HasValue && !Enum.IsDefined(estado.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(estado),
                "El estado de lista de espera especificado no es válido.");
        }

        return await ConsultarPorConvocatoria(convocatoriaId, estado)
            .ToListAsync(cancellationToken);
    }

    internal IQueryable<EntradaListaEspera> ConsultarPorConvocatoria(
        Guid convocatoriaId,
        EstadoListaEspera? estado = null)
    {
        var consulta = _contexto.EntradasListaEspera
            .AsNoTracking()
            .Where(entrada => entrada.ConvocatoriaId == convocatoriaId);

        if (estado.HasValue)
        {
            var estadoFiltro = estado.Value;
            consulta = consulta.Where(entrada => entrada.Estado == estadoFiltro);
        }

        return consulta
            .OrderBy(entrada => entrada.PosicionOrden)
            .ThenBy(entrada => entrada.Id);
    }

    public Task<EntradaListaEspera?> ObtenerSiguientePendientePorConvocatoriaAsync(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default)
    {
        ValidarConvocatoriaId(convocatoriaId);

        return ConsultarSiguientePendientePorConvocatoria(convocatoriaId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    internal IQueryable<EntradaListaEspera> ConsultarSiguientePendientePorConvocatoria(Guid convocatoriaId)
        => _contexto.EntradasListaEspera
            .AsNoTracking()
            .Where(entrada => entrada.ConvocatoriaId == convocatoriaId
                && entrada.Estado != EstadoListaEspera.Promovida)
            .OrderBy(entrada => entrada.PosicionOrden)
            .ThenBy(entrada => entrada.Id);

    private static void ValidarConvocatoriaId(Guid convocatoriaId)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la convocatoria es obligatorio.",
                nameof(convocatoriaId));
        }
    }
}
