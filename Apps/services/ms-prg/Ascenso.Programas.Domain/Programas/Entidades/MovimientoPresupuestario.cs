using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;

namespace Ascenso.Programas.Domain.Programas.Entidades;

/// <summary>
/// Entidad inmutable (append-only) que registra cada variación financiera en el libro mayor de un programa de becas.
/// </summary>
public sealed class MovimientoPresupuestario : EntidadInmutable
{
    private MovimientoPresupuestario()
    {
    }

    private MovimientoPresupuestario(
        Guid id,
        Guid programaBecaId,
        TipoMovimientoPresupuestario tipoMovimiento,
        decimal monto,
        string moneda,
        Guid? referenciaExternaId,
        string periodoAcademicoId,
        string descripcion,
        DateTimeOffset fechaMovimiento,
        DateTimeOffset fechaCreacion,
        Guid usuarioAutorId,
        Guid correlacionId)
        : base(id, fechaCreacion, usuarioAutorId, correlacionId)
    {
        ProgramaBecaId = programaBecaId;
        TipoMovimiento = tipoMovimiento;
        Monto = monto;
        Moneda = moneda;
        ReferenciaExternaId = referenciaExternaId;
        PeriodoAcademicoId = periodoAcademicoId;
        Descripcion = descripcion;
        FechaMovimiento = fechaMovimiento;
    }

    public Guid ProgramaBecaId { get; private set; }

    public TipoMovimientoPresupuestario TipoMovimiento { get; private set; }

    public decimal Monto { get; private set; }

    public string Moneda { get; private set; } = "USD";

    public Guid? ReferenciaExternaId { get; private set; }

    public string PeriodoAcademicoId { get; private set; } = string.Empty;

    public string Descripcion { get; private set; } = string.Empty;

    public DateTimeOffset FechaMovimiento { get; private set; }

    public static MovimientoPresupuestario Crear(
        Guid programaBecaId,
        TipoMovimientoPresupuestario tipoMovimiento,
        decimal monto,
        string periodoAcademicoId,
        string descripcion,
        Guid usuarioAutorId,
        Guid? referenciaExternaId = null,
        DateTimeOffset? fechaMovimiento = null,
        Guid? correlacionId = null)
    {
        if (programaBecaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del programa es obligatorio.", nameof(programaBecaId));
        }

        if (!Enum.IsDefined(tipoMovimiento))
        {
            throw new ArgumentOutOfRangeException(nameof(tipoMovimiento), "El tipo de movimiento presupuestario no es válido.");
        }

        var montoRedondeado = decimal.Round(monto, 2, MidpointRounding.AwayFromZero);
        if (montoRedondeado < 0.00m || (tipoMovimiento != TipoMovimientoPresupuestario.CierrePeriodo && montoRedondeado <= 0.00m))
        {
            throw new ArgumentOutOfRangeException(nameof(monto), "El monto del movimiento presupuestario debe ser mayor que cero (o mayor o igual a cero en cierre de período).");
        }

        if (string.IsNullOrWhiteSpace(periodoAcademicoId))
        {
            throw new ArgumentException("El período académico es obligatorio.", nameof(periodoAcademicoId));
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentException("La descripción del movimiento presupuestario es obligatoria.", nameof(descripcion));
        }

        if (usuarioAutorId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del usuario autor es obligatorio.", nameof(usuarioAutorId));
        }

        var fechaUtc = (fechaMovimiento ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var correlacionEfectiva = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaUtc);

        return new MovimientoPresupuestario(
            UuidV7.Nuevo(fechaUtc),
            programaBecaId,
            tipoMovimiento,
            montoRedondeado,
            "USD",
            referenciaExternaId,
            periodoAcademicoId.Trim(),
            descripcion.Trim(),
            fechaUtc,
            fechaUtc,
            usuarioAutorId,
            correlacionEfectiva);
    }
}
