using Ascenso.Programas.Domain.Convocatorias.Agregados;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Programas.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Repositorio EF Core del agregado de convocatorias.
/// </summary>
public sealed class ConvocatoriaRepository : IConvocatoriaRepository
{
    private readonly ProgramasDbContext _contexto;

    public ConvocatoriaRepository(ProgramasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<Convocatoria?> ObtenerPorIdAsync(Guid convocatoriaId, CancellationToken cancellationToken = default)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la convocatoria es obligatorio.", nameof(convocatoriaId));
        }

        return _contexto.Convocatorias
            .Include(convocatoria => convocatoria.DocumentosRequeridos)
            .SingleOrDefaultAsync(convocatoria => convocatoria.Id == convocatoriaId, cancellationToken);
    }

    public Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException("El código de la convocatoria es obligatorio.", nameof(codigo));
        }

        var normalizado = codigo.Trim().ToUpperInvariant();

        return _contexto.Convocatorias
            .AsNoTracking()
            .AnyAsync(convocatoria => convocatoria.Codigo == normalizado, cancellationToken);
    }

    public async Task AgregarAsync(Convocatoria convocatoria, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(convocatoria);
        await _contexto.Convocatorias.AddAsync(convocatoria, cancellationToken);
    }

    public void Actualizar(Convocatoria convocatoria)
    {
        ArgumentNullException.ThrowIfNull(convocatoria);

        var entrada = _contexto.Entry(convocatoria);
        if (entrada.State == EntityState.Detached)
        {
            throw new InvalidOperationException(
                "La convocatoria debe obtenerse mediante este repositorio antes de actualizarla.");
        }

        entrada.State = EntityState.Modified;
    }

    public async Task<(IReadOnlyList<Convocatoria> Items, int TotalCount)> ConsultarPaginadoAsync(
        int pagina,
        int tamanoPagina,
        EstadoConvocatoria? estado,
        Guid? programaBecaId,
        string? busqueda,
        CancellationToken cancellationToken = default)
    {
        var consulta = _contexto.Convocatorias
            .AsNoTracking()
            .Include(convocatoria => convocatoria.DocumentosRequeridos)
            .AsQueryable();

        if (estado.HasValue)
        {
            consulta = consulta.Where(convocatoria => convocatoria.Estado == estado.Value);
        }

        if (programaBecaId.HasValue && programaBecaId.Value != Guid.Empty)
        {
            var programaId = programaBecaId.Value;
            consulta = consulta.Where(convocatoria => convocatoria.ProgramaBecaId == programaId);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{busqueda.Trim()}%";
            consulta = consulta.Where(convocatoria =>
                EF.Functions.ILike(convocatoria.Nombre, patron)
                || EF.Functions.ILike(convocatoria.Codigo, patron));
        }

        var total = await consulta.CountAsync(cancellationToken);

        var items = await consulta
            .OrderByDescending(convocatoria => convocatoria.FechaApertura)
            .ThenByDescending(convocatoria => convocatoria.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Convocatoria>> ConsultarAbiertasAsync(
        DateTimeOffset instanteActual,
        CancellationToken cancellationToken = default)
    {
        var ahora = instanteActual.ToUniversalTime();

        return await _contexto.Convocatorias
            .AsNoTracking()
            .Include(convocatoria => convocatoria.DocumentosRequeridos)
            .Where(convocatoria => convocatoria.Estado == EstadoConvocatoria.Publicada
                && convocatoria.FechaApertura <= ahora
                && convocatoria.FechaCierre >= ahora)
            .OrderBy(convocatoria => convocatoria.FechaApertura)
            .ThenBy(convocatoria => convocatoria.Codigo)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Convocatoria>> ObtenerPublicadasVencidasAsync(
        DateTimeOffset instanteActual,
        CancellationToken cancellationToken = default)
    {
        var ahora = instanteActual.ToUniversalTime();

        return await _contexto.Convocatorias
            .Include(convocatoria => convocatoria.DocumentosRequeridos)
            .Where(convocatoria => convocatoria.Estado == EstadoConvocatoria.Publicada
                && convocatoria.FechaCierre <= ahora)
            .OrderBy(convocatoria => convocatoria.FechaCierre)
            .ToListAsync(cancellationToken);
    }
}
