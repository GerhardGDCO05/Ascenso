using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Interfaces;
using Ascenso.Programas.Domain.Universidades.Agregados;
using Ascenso.Programas.Domain.Universidades.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Programas.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del repositorio de universidades aliadas, convenios, carreras y costos.
/// </summary>
public sealed class UniversidadAliadaRepository : IUniversidadAliadaRepository
{
    private readonly ProgramasDbContext _contexto;

    public UniversidadAliadaRepository(ProgramasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<UniversidadAliada?> ObtenerPorIdAsync(Guid universidadId, CancellationToken cancellationToken = default)
    {
        if (universidadId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la universidad aliada es obligatorio.", nameof(universidadId));
        }

        return _contexto.UniversidadesAliadas
            .Include(universidad => universidad.Convenios)
            .Include(universidad => universidad.Carreras)
                .ThenInclude(carrera => carrera.CostosMatricula)
            .SingleOrDefaultAsync(universidad => universidad.Id == universidadId, cancellationToken);
    }

    public Task<bool> ExisteCodigoInstitucionalAsync(
        string codigoInstitucional,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoInstitucional))
        {
            throw new ArgumentException("El código institucional es obligatorio.", nameof(codigoInstitucional));
        }

        var normalizado = codigoInstitucional.Trim().ToUpperInvariant();

        return _contexto.UniversidadesAliadas
            .AsNoTracking()
            .AnyAsync(universidad => universidad.CodigoInstitucional == normalizado, cancellationToken);
    }

    public async Task AgregarAsync(UniversidadAliada universidad, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(universidad);
        await _contexto.UniversidadesAliadas.AddAsync(universidad, cancellationToken);
    }

    public async Task<(IReadOnlyList<UniversidadAliada> Items, int TotalCount)> ConsultarPaginadoAsync(
        int pagina,
        int tamanoPagina,
        EstadoUniversidad? estado,
        string? busqueda,
        CancellationToken cancellationToken = default)
    {
        var consulta = _contexto.UniversidadesAliadas
            .AsNoTracking()
            .Include(universidad => universidad.Convenios)
            .Include(universidad => universidad.Carreras)
            .AsQueryable();

        if (estado.HasValue)
        {
            consulta = consulta.Where(universidad => universidad.Estado == estado.Value);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{busqueda.Trim()}%";
            consulta = consulta.Where(universidad =>
                EF.Functions.ILike(universidad.Nombre, patron)
                || EF.Functions.ILike(universidad.CodigoInstitucional, patron));
        }

        var total = await consulta.CountAsync(cancellationToken);

        var items = await consulta
            .OrderBy(universidad => universidad.Nombre)
            .ThenBy(universidad => universidad.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<CostoMatriculaReferencia>> ConsultarHistorialCostosCarreraAsync(
        Guid universidadId,
        Guid carreraId,
        string? periodoAcademicoId,
        CancellationToken cancellationToken = default)
    {
        if (universidadId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la universidad aliada es obligatorio.", nameof(universidadId));
        }

        if (carreraId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la carrera elegible es obligatorio.", nameof(carreraId));
        }

        var consulta = from costo in _contexto.CostosMatriculaReferencia.AsNoTracking()
                       join carrera in _contexto.CarrerasElegibles.AsNoTracking()
                           on costo.CarreraElegibleId equals carrera.Id
                       where carrera.UniversidadAliadaId == universidadId
                           && carrera.Id == carreraId
                       select costo;

        if (!string.IsNullOrWhiteSpace(periodoAcademicoId))
        {
            var periodo = periodoAcademicoId.Trim();
            consulta = consulta.Where(costo => costo.PeriodoAcademicoId == periodo);
        }

        return await consulta
            .OrderByDescending(costo => costo.PeriodoAcademicoId)
            .ThenByDescending(costo => costo.NumeroVersion)
            .ToListAsync(cancellationToken);
    }
}
