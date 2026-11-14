using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Interfaces;
using Ascenso.Programas.Domain.Programas.Agregados;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Programas.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del repositorio de programas de becas y sus asignaciones de fondos.
/// </summary>
public sealed class ProgramaBecaRepository : IProgramaBecaRepository
{
    private readonly ProgramasDbContext _contexto;

    public ProgramaBecaRepository(ProgramasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<ProgramaBeca?> ObtenerPorIdAsync(Guid programaId, CancellationToken cancellationToken = default)
    {
        if (programaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del programa de becas es obligatorio.", nameof(programaId));
        }

        return _contexto.ProgramasBeca
            .Include(programa => programa.AsignacionesFondo)
            .SingleOrDefaultAsync(programa => programa.Id == programaId, cancellationToken);
    }

    public Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException("El código del programa es obligatorio.", nameof(codigo));
        }

        var normalizado = codigo.Trim().ToUpperInvariant();

        return _contexto.ProgramasBeca
            .AsNoTracking()
            .AnyAsync(programa => programa.Codigo == normalizado, cancellationToken);
    }

    public async Task AgregarAsync(ProgramaBeca programa, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(programa);
        await _contexto.ProgramasBeca.AddAsync(programa, cancellationToken);
    }

    public async Task<(IReadOnlyList<ProgramaBeca> Items, int TotalCount)> ConsultarPaginadoAsync(
        int pagina,
        int tamanoPagina,
        EstadoProgramaBeca? estado,
        string? periodoInicialId,
        ModalidadCobertura? modalidadCobertura,
        string? busqueda,
        CancellationToken cancellationToken = default)
    {
        var consulta = _contexto.ProgramasBeca
            .AsNoTracking()
            .AsQueryable();

        if (estado.HasValue)
        {
            consulta = consulta.Where(programa => programa.Estado == estado.Value);
        }

        if (!string.IsNullOrWhiteSpace(periodoInicialId))
        {
            var periodo = periodoInicialId.Trim();
            consulta = consulta.Where(programa => programa.PeriodoInicialId == periodo);
        }

        if (modalidadCobertura.HasValue)
        {
            consulta = consulta.Where(programa => programa.PoliticaCobertura.Modalidad == modalidadCobertura.Value);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{busqueda.Trim()}%";
            consulta = consulta.Where(programa =>
                EF.Functions.ILike(programa.Nombre, patron)
                || EF.Functions.ILike(programa.Codigo, patron));
        }

        var total = await consulta.CountAsync(cancellationToken);

        var items = await consulta
            .OrderBy(programa => programa.Nombre)
            .ThenBy(programa => programa.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
