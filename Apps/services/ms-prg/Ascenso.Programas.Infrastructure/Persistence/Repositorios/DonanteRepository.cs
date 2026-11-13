using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Donantes.Entidades;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Programas.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del repositorio de donantes y control de aportes financieros.
/// </summary>
public sealed class DonanteRepository : IDonanteRepository
{
    private readonly ProgramasDbContext _contexto;

    public DonanteRepository(ProgramasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<Donante?> ObtenerPorIdAsync(Guid donanteId, CancellationToken cancellationToken = default)
    {
        if (donanteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del donante es obligatorio.", nameof(donanteId));
        }

        return _contexto.Donantes
            .Include(donante => donante.Aportes)
            .SingleOrDefaultAsync(donante => donante.Id == donanteId, cancellationToken);
    }

    public Task<bool> ExisteAsync(Guid donanteId, CancellationToken cancellationToken = default)
    {
        if (donanteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del donante es obligatorio.", nameof(donanteId));
        }

        return _contexto.Donantes
            .AsNoTracking()
            .AnyAsync(donante => donante.Id == donanteId, cancellationToken);
    }

    public Task<bool> ExisteIdentificacionFiscalAsync(
        string identificacionFiscal,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identificacionFiscal))
        {
            throw new ArgumentException("La identificación fiscal es obligatoria.", nameof(identificacionFiscal));
        }

        var normalizada = identificacionFiscal.Trim().ToUpperInvariant();

        return _contexto.Donantes
            .AsNoTracking()
            .AnyAsync(donante => donante.IdentificacionFiscal == normalizada, cancellationToken);
    }

    public Task<bool> ExisteReferenciaBancariaAsync(
        string numeroReferenciaBancaria,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(numeroReferenciaBancaria))
        {
            throw new ArgumentException("El número de referencia bancaria es obligatorio.", nameof(numeroReferenciaBancaria));
        }

        var normalizada = numeroReferenciaBancaria.Trim().ToUpperInvariant();

        return _contexto.AportesDonante
            .AsNoTracking()
            .AnyAsync(aporte => aporte.NumeroReferenciaBancaria == normalizada, cancellationToken);
    }

    public Task<bool> TieneFondosComprometidosEnProgramaVigenteAsync(
        Guid donanteId,
        CancellationToken cancellationToken = default)
    {
        if (donanteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del donante es obligatorio.", nameof(donanteId));
        }

        // Antes de integrar las asignaciones por programa (RF-01), se protege cualquier compromiso reflejado en saldo.
        return _contexto.Donantes
            .AsNoTracking()
            .AnyAsync(
                donante => donante.Id == donanteId
                    && donante.SaldoTotalHistorico > donante.SaldoDisponibleAsignacion,
                cancellationToken);
    }

    public async Task AgregarAsync(Donante donante, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(donante);
        await _contexto.Donantes.AddAsync(donante, cancellationToken);
    }

    public async Task<(IReadOnlyList<Donante> Items, int TotalCount)> ConsultarPaginadoAsync(
        int pagina,
        int tamanoPagina,
        TipoDonante? tipoDonante,
        EstadoDonante? estadoDonante,
        string? busqueda,
        CancellationToken cancellationToken = default)
    {
        var consulta = _contexto.Donantes.AsNoTracking();

        if (tipoDonante.HasValue)
        {
            consulta = consulta.Where(donante => donante.TipoDonante == tipoDonante.Value);
        }

        if (estadoDonante.HasValue)
        {
            consulta = consulta.Where(donante => donante.EstadoDonante == estadoDonante.Value);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{busqueda.Trim()}%";
            consulta = consulta.Where(donante =>
                EF.Functions.ILike(donante.RazonSocialNombre, patron)
                || EF.Functions.ILike(donante.IdentificacionFiscal, patron));
        }

        var total = await consulta.CountAsync(cancellationToken);

        var items = await consulta
            .OrderBy(donante => donante.RazonSocialNombre)
            .ThenBy(donante => donante.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<AporteDonante>> ConsultarHistorialAportesAsync(
        Guid donanteId,
        string? periodoAcademicoId,
        CancellationToken cancellationToken = default)
    {
        if (donanteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del donante es obligatorio.", nameof(donanteId));
        }

        var consulta = _contexto.AportesDonante
            .AsNoTracking()
            .Where(aporte => aporte.DonanteId == donanteId);

        if (!string.IsNullOrWhiteSpace(periodoAcademicoId))
        {
            var periodo = periodoAcademicoId.Trim();
            consulta = consulta.Where(aporte => aporte.PeriodoAcademicoId == periodo);
        }

        return await consulta
            .OrderByDescending(aporte => aporte.FechaAporte)
            .ThenByDescending(aporte => aporte.FechaCreacion)
            .ToListAsync(cancellationToken);
    }
}

