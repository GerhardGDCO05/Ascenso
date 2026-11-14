using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;

namespace Ascenso.Programas.Domain.Programas.Entidades;

/// <summary>
/// Entidad auditable que representa una reserva activa, confirmada o liberada de cupo y saldo presupuestario.
/// </summary>
public sealed class ReservaPresupuestaria : EntidadAuditable<Guid>
{
    private ReservaPresupuestaria()
    {
    }

    private ReservaPresupuestaria(
        Guid id,
        Guid programaBecaId,
        Guid referenciaExternaId,
        string periodoAcademicoId,
        decimal monto,
        string moneda,
        string descripcion,
        DateTimeOffset fechaReserva,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
        : base(id, fechaCreacion, usuarioCreacionId)
    {
        ProgramaBecaId = programaBecaId;
        ReferenciaExternaId = referenciaExternaId;
        PeriodoAcademicoId = periodoAcademicoId;
        Monto = monto;
        Moneda = moneda;
        Descripcion = descripcion;
        Estado = EstadoReservaPresupuestaria.Reservada;
        FechaReserva = fechaReserva;
    }

    public Guid ProgramaBecaId { get; private set; }

    public Guid ReferenciaExternaId { get; private set; }

    public string PeriodoAcademicoId { get; private set; } = string.Empty;

    public decimal Monto { get; private set; }

    public string Moneda { get; private set; } = "USD";

    public string Descripcion { get; private set; } = string.Empty;

    public EstadoReservaPresupuestaria Estado { get; private set; }

    public string? MotivoLiberacion { get; private set; }

    public DateTimeOffset FechaReserva { get; private set; }

    public DateTimeOffset? FechaResolucion { get; private set; }

    public static ReservaPresupuestaria CrearReservada(
        Guid programaBecaId,
        Guid referenciaExternaId,
        string periodoAcademicoId,
        decimal monto,
        string descripcion,
        Guid usuarioCreacionId,
        DateTimeOffset? fechaReserva = null)
    {
        if (programaBecaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del programa es obligatorio.", nameof(programaBecaId));
        }

        if (referenciaExternaId == Guid.Empty)
        {
            throw new ArgumentException("La referencia externa de la reserva es obligatoria.", nameof(referenciaExternaId));
        }

        if (string.IsNullOrWhiteSpace(periodoAcademicoId))
        {
            throw new ArgumentException("El período académico de la reserva es obligatorio.", nameof(periodoAcademicoId));
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

        if (usuarioCreacionId == Guid.Empty)
        {
            throw new ArgumentException("El usuario que solicita la reserva es obligatorio.", nameof(usuarioCreacionId));
        }

        var fechaUtc = (fechaReserva ?? DateTimeOffset.UtcNow).ToUniversalTime();

        return new ReservaPresupuestaria(
            UuidV7.Nuevo(fechaUtc),
            programaBecaId,
            referenciaExternaId,
            periodoAcademicoId.Trim(),
            montoRedondeado,
            "USD",
            descripcion.Trim(),
            fechaUtc,
            fechaUtc,
            usuarioCreacionId);
    }

    internal void Confirmar(Guid usuarioModificacionId, DateTimeOffset fechaConfirmacion)
    {
        if (Estado != EstadoReservaPresupuestaria.Reservada)
        {
            throw new TransicionEstadoReservaInvalidaException(
                ReferenciaExternaId,
                Estado,
                EstadoReservaPresupuestaria.Confirmada);
        }

        var fechaUtc = fechaConfirmacion.ToUniversalTime();
        Estado = EstadoReservaPresupuestaria.Confirmada;
        FechaResolucion = fechaUtc;
        ActualizarAuditoria(usuarioModificacionId, fechaUtc);
    }

    internal void Liberar(string motivo, Guid usuarioModificacionId, DateTimeOffset fechaLiberacion)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException("El motivo de liberación de la reserva es obligatorio.", nameof(motivo));
        }

        if (Estado != EstadoReservaPresupuestaria.Reservada)
        {
            throw new TransicionEstadoReservaInvalidaException(
                ReferenciaExternaId,
                Estado,
                EstadoReservaPresupuestaria.Liberada);
        }

        var fechaUtc = fechaLiberacion.ToUniversalTime();
        Estado = EstadoReservaPresupuestaria.Liberada;
        MotivoLiberacion = motivo.Trim();
        FechaResolucion = fechaUtc;
        ActualizarAuditoria(usuarioModificacionId, fechaUtc);
    }
}
