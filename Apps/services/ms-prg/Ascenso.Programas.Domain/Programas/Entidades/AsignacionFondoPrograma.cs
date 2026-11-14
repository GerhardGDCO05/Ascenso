using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;

namespace Ascenso.Programas.Domain.Programas.Entidades;

/// <summary>
/// Entidad interna que registra una asignación presupuestaria de un donante hacia un programa de becas.
/// </summary>
public sealed class AsignacionFondoPrograma : EntidadAuditable<Guid>
{
    private AsignacionFondoPrograma()
    {
    }

    private AsignacionFondoPrograma(
        Guid id,
        Guid programaBecaId,
        Guid donanteId,
        string periodoAcademicoId,
        decimal montoAsignado,
        DateTimeOffset fechaAsignacion,
        Guid autorizadoPorUsuarioId,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
        : base(id, fechaCreacion, usuarioCreacionId)
    {
        ProgramaBecaId = programaBecaId;
        DonanteId = donanteId;
        PeriodoAcademicoId = periodoAcademicoId;
        MontoAsignado = montoAsignado;
        Estado = EstadoAsignacionFondo.Asignada;
        FechaAsignacion = fechaAsignacion;
        AutorizadoPorUsuarioId = autorizadoPorUsuarioId;
    }

    public Guid ProgramaBecaId { get; private set; }

    public Guid DonanteId { get; private set; }

    public string PeriodoAcademicoId { get; private set; } = string.Empty;

    public decimal MontoAsignado { get; private set; }

    public EstadoAsignacionFondo Estado { get; private set; }

    public DateTimeOffset FechaAsignacion { get; private set; }

    public Guid AutorizadoPorUsuarioId { get; private set; }

    internal static AsignacionFondoPrograma CrearAsignada(
        Guid programaBecaId,
        Guid donanteId,
        string periodoAcademicoId,
        decimal montoAsignado,
        Guid autorizadoPorUsuarioId,
        DateTimeOffset fechaAsignacion)
    {
        if (programaBecaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del programa es obligatorio.", nameof(programaBecaId));
        }

        if (donanteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del donante es obligatorio.", nameof(donanteId));
        }

        if (string.IsNullOrWhiteSpace(periodoAcademicoId))
        {
            throw new ArgumentException("El período académico es obligatorio.", nameof(periodoAcademicoId));
        }

        var montoRedondeado = decimal.Round(montoAsignado, 2, MidpointRounding.AwayFromZero);
        if (montoRedondeado <= 0.00m)
        {
            throw new ArgumentOutOfRangeException(nameof(montoAsignado), "El monto asignado debe ser mayor que cero.");
        }

        if (autorizadoPorUsuarioId == Guid.Empty)
        {
            throw new ArgumentException("El usuario que autoriza la asignación es obligatorio.", nameof(autorizadoPorUsuarioId));
        }

        var fechaUtc = fechaAsignacion.ToUniversalTime();

        return new AsignacionFondoPrograma(
            UuidV7.Nuevo(fechaUtc),
            programaBecaId,
            donanteId,
            periodoAcademicoId.Trim(),
            montoRedondeado,
            fechaUtc,
            autorizadoPorUsuarioId,
            fechaUtc,
            autorizadoPorUsuarioId);
    }

    internal decimal ReducirODesasignar(
        decimal montoAReducir,
        Guid usuarioModificacionId,
        DateTimeOffset fechaModificacion)
    {
        if (Estado != EstadoAsignacionFondo.Asignada)
        {
            throw new InvalidOperationException("La asignación ya se encuentra desasignada.");
        }

        var montoRedondeado = decimal.Round(montoAReducir, 2, MidpointRounding.AwayFromZero);
        if (montoRedondeado <= 0.00m || montoRedondeado > MontoAsignado)
        {
            throw new ArgumentOutOfRangeException(
                nameof(montoAReducir),
                "El monto a reducir debe ser mayor que cero y no superar el monto asignado actual.");
        }

        var nuevoMonto = decimal.Round(MontoAsignado - montoRedondeado, 2, MidpointRounding.AwayFromZero);
        MontoAsignado = nuevoMonto;
        if (nuevoMonto == 0.00m)
        {
            Estado = EstadoAsignacionFondo.Desasignada;
        }

        ActualizarAuditoria(usuarioModificacionId, fechaModificacion.ToUniversalTime());
        return montoRedondeado;
    }
}
