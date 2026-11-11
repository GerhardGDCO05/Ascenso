using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;

namespace Ascenso.Becas.Domain.Otorgamiento.Entidades;

/// <summary>
/// Registro inmutable de una transición de estado del agregado de beca.
/// No expone operaciones de modificación ni de eliminación.
/// </summary>
public sealed class HistorialEstadoBeca : EntidadInmutable
{
    private HistorialEstadoBeca()
    {
    }

    private HistorialEstadoBeca(
        Guid id,
        Guid becaId,
        Guid? ofertaBecaId,
        Guid? compromisoBecarioId,
        TipoEntidadHistorial tipoEntidad,
        EstadoBeca? estadoBecaAnterior,
        EstadoBeca? estadoBecaNuevo,
        EstadoOferta? estadoOfertaAnterior,
        EstadoOferta? estadoOfertaNuevo,
        EstadoCompromiso? estadoCompromisoAnterior,
        EstadoCompromiso? estadoCompromisoNuevo,
        DateTimeOffset fechaCambio,
        string motivo,
        string? justificacionDetallada,
        Guid? autorizacionOtpId,
        bool estadoTerminalIrreversible,
        ContextoOperacion contexto)
        : base(id, fechaCambio, contexto.UsuarioAutorId, contexto.CorrelacionId)
    {
        if (becaId == Guid.Empty)
        {
            throw new ArgumentException("La beca del historial es obligatoria.", nameof(becaId));
        }

        if (!Enum.IsDefined(tipoEntidad))
        {
            throw new ArgumentOutOfRangeException(nameof(tipoEntidad), "El tipo de entidad del historial no es válido.");
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException("El motivo del cambio es obligatorio.", nameof(motivo));
        }

        BecaId = becaId;
        OfertaBecaId = ofertaBecaId;
        CompromisoBecarioId = compromisoBecarioId;
        TipoEntidad = tipoEntidad;
        EstadoBecaAnterior = estadoBecaAnterior;
        EstadoBecaNuevo = estadoBecaNuevo;
        EstadoOfertaAnterior = estadoOfertaAnterior;
        EstadoOfertaNuevo = estadoOfertaNuevo;
        EstadoCompromisoAnterior = estadoCompromisoAnterior;
        EstadoCompromisoNuevo = estadoCompromisoNuevo;
        FechaCambio = fechaCambio.ToUniversalTime();
        Motivo = motivo.Trim();
        JustificacionDetallada = string.IsNullOrWhiteSpace(justificacionDetallada)
            ? null
            : justificacionDetallada.Trim();
        AutorizacionOtpId = autorizacionOtpId;
        EstadoTerminalIrreversible = estadoTerminalIrreversible;
    }

    public Guid BecaId { get; }

    public Guid? OfertaBecaId { get; }

    public Guid? CompromisoBecarioId { get; }

    public TipoEntidadHistorial TipoEntidad { get; }

    public EstadoBeca? EstadoBecaAnterior { get; }

    public EstadoBeca? EstadoBecaNuevo { get; }

    public EstadoOferta? EstadoOfertaAnterior { get; }

    public EstadoOferta? EstadoOfertaNuevo { get; }

    public EstadoCompromiso? EstadoCompromisoAnterior { get; }

    public EstadoCompromiso? EstadoCompromisoNuevo { get; }

    public DateTimeOffset FechaCambio { get; }

    public string Motivo { get; } = string.Empty;

    public string? JustificacionDetallada { get; }

    public Guid? AutorizacionOtpId { get; }

    public bool EstadoTerminalIrreversible { get; }

    internal static HistorialEstadoBeca ParaBeca(
        Guid becaId,
        EstadoBeca? estadoAnterior,
        EstadoBeca estadoNuevo,
        string motivo,
        ContextoOperacion contexto,
        bool estadoTerminalIrreversible)
    {
        if (!Enum.IsDefined(estadoNuevo))
        {
            throw new ArgumentOutOfRangeException(nameof(estadoNuevo), "El estado nuevo de la beca no es válido.");
        }

        if (estadoAnterior is { } anterior && !Enum.IsDefined(anterior))
        {
            throw new ArgumentOutOfRangeException(nameof(estadoAnterior), "El estado anterior de la beca no es válido.");
        }

        return new HistorialEstadoBeca(
            UuidV7.Nuevo(),
            becaId,
            null,
            null,
            TipoEntidadHistorial.Beca,
            estadoAnterior,
            estadoNuevo,
            null,
            null,
            null,
            null,
            contexto.Fecha,
            motivo,
            contexto.Justificacion,
            contexto.AutorizacionOtpId,
            estadoTerminalIrreversible,
            contexto);
    }

    internal static HistorialEstadoBeca ParaOferta(
        Guid becaId,
        Guid ofertaBecaId,
        EstadoOferta? estadoAnterior,
        EstadoOferta estadoNuevo,
        string motivo,
        ContextoOperacion contexto,
        bool estadoTerminalIrreversible)
    {
        if (ofertaBecaId == Guid.Empty)
        {
            throw new ArgumentException("La oferta del historial es obligatoria.", nameof(ofertaBecaId));
        }

        if (!Enum.IsDefined(estadoNuevo))
        {
            throw new ArgumentOutOfRangeException(nameof(estadoNuevo), "El estado nuevo de la oferta no es válido.");
        }

        if (estadoAnterior is { } anterior && !Enum.IsDefined(anterior))
        {
            throw new ArgumentOutOfRangeException(nameof(estadoAnterior), "El estado anterior de la oferta no es válido.");
        }

        return new HistorialEstadoBeca(
            UuidV7.Nuevo(),
            becaId,
            ofertaBecaId,
            null,
            TipoEntidadHistorial.OfertaBeca,
            null,
            null,
            estadoAnterior,
            estadoNuevo,
            null,
            null,
            contexto.Fecha,
            motivo,
            contexto.Justificacion,
            contexto.AutorizacionOtpId,
            estadoTerminalIrreversible,
            contexto);
    }

    internal static HistorialEstadoBeca ParaCompromiso(
        Guid becaId,
        Guid compromisoBecarioId,
        EstadoCompromiso? estadoAnterior,
        EstadoCompromiso estadoNuevo,
        string motivo,
        ContextoOperacion contexto)
    {
        if (compromisoBecarioId == Guid.Empty)
        {
            throw new ArgumentException("El compromiso del historial es obligatorio.", nameof(compromisoBecarioId));
        }

        if (!Enum.IsDefined(estadoNuevo))
        {
            throw new ArgumentOutOfRangeException(nameof(estadoNuevo), "El estado nuevo del compromiso no es válido.");
        }

        if (estadoAnterior is { } anterior && !Enum.IsDefined(anterior))
        {
            throw new ArgumentOutOfRangeException(
                nameof(estadoAnterior),
                "El estado anterior del compromiso no es válido.");
        }

        return new HistorialEstadoBeca(
            UuidV7.Nuevo(),
            becaId,
            null,
            compromisoBecarioId,
            TipoEntidadHistorial.CompromisoBecario,
            null,
            null,
            null,
            null,
            estadoAnterior,
            estadoNuevo,
            contexto.Fecha,
            motivo,
            contexto.Justificacion,
            contexto.AutorizacionOtpId,
            estadoNuevo == EstadoCompromiso.Aprobado,
            contexto);
    }
}
