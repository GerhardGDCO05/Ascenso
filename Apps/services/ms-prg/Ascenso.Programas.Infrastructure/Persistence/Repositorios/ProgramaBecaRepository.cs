using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using Ascenso.Programas.Domain.Programas.Agregados;
using Ascenso.Programas.Domain.Programas.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Programas.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del repositorio de programas de becas, asignaciones de fondos,
/// reservas atómicas concurrentes (C2 / RN-PRG-03) y movimientos presupuestarios.
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
            .Include(programa => programa.ReservasPresupuestarias)
            .Include(programa => programa.MovimientosPresupuestarios)
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

    public async Task<(ProgramaBeca Programa, ReservaPresupuestaria Reserva, MovimientoPresupuestario Movimiento, bool YaExistia)> ReservarPresupuestoAtomicoAsync(
        Guid programaId,
        Guid referenciaExternaId,
        string periodoAcademicoId,
        decimal monto,
        string descripcion,
        Guid usuarioAutorId,
        CancellationToken cancellationToken = default)
    {
        if (programaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del programa de becas es obligatorio.", nameof(programaId));
        }

        if (referenciaExternaId == Guid.Empty)
        {
            throw new ArgumentException("La referencia externa de la reserva es obligatoria.", nameof(referenciaExternaId));
        }

        if (string.IsNullOrWhiteSpace(periodoAcademicoId))
        {
            throw new ArgumentException("El período académico es obligatorio.", nameof(periodoAcademicoId));
        }

        var montoRedondeado = decimal.Round(monto, 2, MidpointRounding.AwayFromZero);
        if (montoRedondeado <= 0.00m)
        {
            throw new ArgumentOutOfRangeException(nameof(monto), "El monto a reservar debe ser mayor que cero.");
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentException("La descripción de la reserva es obligatoria.", nameof(descripcion));
        }

        if (usuarioAutorId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del usuario autor es obligatorio.", nameof(usuarioAutorId));
        }

        var periodoNormalizado = periodoAcademicoId.Trim();

        await using var transaccion = _contexto.Database.CurrentTransaction is null
            ? await _contexto.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var reservaExistente = await _contexto.ReservasPresupuestarias
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.ProgramaBecaId == programaId && item.ReferenciaExternaId == referenciaExternaId,
                cancellationToken);

        if (reservaExistente is not null)
        {
            if (reservaExistente.Estado == EstadoReservaPresupuestaria.Reservada
                && reservaExistente.Monto == montoRedondeado
                && string.Equals(reservaExistente.PeriodoAcademicoId, periodoNormalizado, StringComparison.OrdinalIgnoreCase))
            {
                var programaExistente = await ObtenerPorIdAsync(programaId, cancellationToken)
                    ?? throw new ProgramaBecaNoEncontradoException(programaId);

                var movimientoExistente = await _contexto.MovimientosPresupuestarios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item => item.ProgramaBecaId == programaId
                            && item.ReferenciaExternaId == referenciaExternaId
                            && item.TipoMovimiento == TipoMovimientoPresupuestario.ReservaCupo,
                        cancellationToken)
                    ?? MovimientoPresupuestario.Crear(
                        programaId,
                        TipoMovimientoPresupuestario.ReservaCupo,
                        montoRedondeado,
                        periodoNormalizado,
                        reservaExistente.Descripcion,
                        usuarioAutorId,
                        referenciaExternaId,
                        reservaExistente.FechaReserva);

                return (programaExistente, reservaExistente, movimientoExistente, true);
            }

            throw new ReservaConParametrosDistintosException(programaId, referenciaExternaId);
        }

        var ahoraUtc = DateTimeOffset.UtcNow;

        var filasAfectadas = await _contexto.ProgramasBeca
            .Where(programa => programa.Id == programaId
                && (programa.Estado == EstadoProgramaBeca.Activo || programa.Estado == EstadoProgramaBeca.Configurado)
                && (programa.CuposOcupados + programa.CuposReservados < programa.CupoTotal)
                && (programa.PresupuestoTotalAsignado - programa.PresupuestoReservado - programa.PresupuestoEjecutado >= montoRedondeado))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(programa => programa.PresupuestoReservado, programa => programa.PresupuestoReservado + montoRedondeado)
                    .SetProperty(programa => programa.CuposReservados, programa => programa.CuposReservados + 1)
                    .SetProperty(programa => programa.FechaUltimaModificacion, ahoraUtc)
                    .SetProperty(programa => programa.UsuarioModificacionId, usuarioAutorId)
                    .SetProperty(programa => programa.Version, programa => programa.Version + 1),
                cancellationToken);

        if (filasAfectadas == 0)
        {
            var programaActual = await _contexto.ProgramasBeca
                .AsNoTracking()
                .SingleOrDefaultAsync(programa => programa.Id == programaId, cancellationToken)
                ?? throw new ProgramaBecaNoEncontradoException(programaId);

            if (programaActual.Estado is not (EstadoProgramaBeca.Activo or EstadoProgramaBeca.Configurado))
            {
                throw new EstadoProgramaInvalidoParaOperacionException(
                    programaId,
                    programaActual.Estado,
                    nameof(ReservarPresupuestoAtomicoAsync));
            }

            throw new SinDisponibilidadPresupuestariaException(programaId, montoRedondeado);
        }

        var reserva = ReservaPresupuestaria.CrearReservada(
            programaId,
            referenciaExternaId,
            periodoNormalizado,
            montoRedondeado,
            descripcion,
            usuarioAutorId,
            ahoraUtc);

        var movimiento = MovimientoPresupuestario.Crear(
            programaId,
            TipoMovimientoPresupuestario.ReservaCupo,
            montoRedondeado,
            periodoNormalizado,
            descripcion,
            usuarioAutorId,
            referenciaExternaId,
            ahoraUtc);

        await _contexto.ReservasPresupuestarias.AddAsync(reserva, cancellationToken);
        await _contexto.MovimientosPresupuestarios.AddAsync(movimiento, cancellationToken);
        await _contexto.GuardarCambiosAsync(cancellationToken);

        if (transaccion is not null)
        {
            await transaccion.CommitAsync(cancellationToken);
        }

        foreach (var entrada in _contexto.ChangeTracker.Entries<ProgramaBeca>().Where(e => e.Entity.Id == programaId).ToArray())
        {
            entrada.State = EntityState.Detached;
        }

        var programaActualizado = await ObtenerPorIdAsync(programaId, cancellationToken)
            ?? throw new ProgramaBecaNoEncontradoException(programaId);

        return (programaActualizado, reserva, movimiento, false);
    }

    public async Task<(IReadOnlyList<MovimientoPresupuestario> Items, int TotalCount)> ConsultarMovimientosPaginadosAsync(
        Guid programaId,
        int pagina,
        int tamanoPagina,
        TipoMovimientoPresupuestario? tipoMovimiento,
        string? periodoAcademicoId,
        Guid? referenciaExternaId,
        CancellationToken cancellationToken = default)
    {
        if (programaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del programa de becas es obligatorio.", nameof(programaId));
        }

        var consulta = _contexto.MovimientosPresupuestarios
            .AsNoTracking()
            .Where(movimiento => movimiento.ProgramaBecaId == programaId);

        if (tipoMovimiento.HasValue)
        {
            consulta = consulta.Where(movimiento => movimiento.TipoMovimiento == tipoMovimiento.Value);
        }

        if (!string.IsNullOrWhiteSpace(periodoAcademicoId))
        {
            var periodo = periodoAcademicoId.Trim();
            consulta = consulta.Where(movimiento => movimiento.PeriodoAcademicoId == periodo);
        }

        if (referenciaExternaId.HasValue && referenciaExternaId.Value != Guid.Empty)
        {
            var referencia = referenciaExternaId.Value;
            consulta = consulta.Where(movimiento => movimiento.ReferenciaExternaId == referencia);
        }

        var total = await consulta.CountAsync(cancellationToken);

        var items = await consulta
            .OrderByDescending(movimiento => movimiento.FechaMovimiento)
            .ThenByDescending(movimiento => movimiento.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
