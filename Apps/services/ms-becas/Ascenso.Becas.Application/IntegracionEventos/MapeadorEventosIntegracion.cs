using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Desembolsos.Eventos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;

namespace Ascenso.Becas.Application.IntegracionEventos;

/// <summary>
/// Traduce los eventos de dominio de otorgamiento a los contratos de
/// integración envueltos en <see cref="EventoIntegracion{T}"/>. El mapeo es
/// explícito para que un evento nuevo no se publique por accidente con un
/// contrato incompleto.
/// </summary>
public static class MapeadorEventosIntegracion
{
    public const string Productor = "ms-bec";

    public static object Mapear(IDomainEvent evento, int aggregateVersion = 0)
    {
        ArgumentNullException.ThrowIfNull(evento);

        return evento switch
        {
            DesembolsosBecaSuspendidosEvento item => Envolver(item, item.BecaId,
                new DesembolsosBecaSuspendidos(item.BecaId, item.SuspensionId,
                    item.ProgramaBecaId, item.PeriodoAcademicoId, item.SaldoPendiente), aggregateVersion),
            DesembolsosBecaReanudadosEvento item => Envolver(item, item.BecaId,
                new DesembolsosBecaReanudados(item.BecaId, item.SuspensionId,
                    item.ProgramaBecaId, item.PeriodoAcademicoId, item.SaldoPendiente), aggregateVersion),
            BecaRevocadaDefinitivamenteEvento item => Envolver(item, item.BecaId,
                new BecaRevocadaDefinitivamente(item.BecaId, item.ProgramaBecaId,
                    item.PeriodoAcademicoId, EstadoBeca.Revocada.ACodigo(),
                    TipoCierreBeca.RevocacionDefinitiva.ACodigo()), aggregateVersion),
            DesembolsoProgramadoEvento item => Envolver(item, item.BecaId,
                new DesembolsoProgramado(item.BecaId, item.CalendarioId, item.DesembolsoId), aggregateVersion),
            DesembolsoConfirmadoConOtpEvento item => Envolver(item, item.BecaId,
                new DesembolsoConfirmadoConOTP(item.BecaId, item.CalendarioId, item.DesembolsoId), aggregateVersion),
            DesembolsoFallidoEvento item => Envolver(item, item.BecaId,
                new DesembolsoFallido(item.BecaId, item.CalendarioId, item.DesembolsoId), aggregateVersion),
            DesembolsoDevueltoEvento item => Envolver(item, item.BecaId,
                new DesembolsoDevuelto(item.BecaId, item.CalendarioId, item.DesembolsoId), aggregateVersion),
            DesembolsoReintentadoEvento item => Envolver(item, item.BecaId,
                new DesembolsoReintentado(item.BecaId, item.CalendarioId, item.DesembolsoId), aggregateVersion),
            DesembolsoCanceladoEvento item => Envolver(item, item.BecaId,
                new DesembolsoCancelado(item.BecaId, item.CalendarioId, item.DesembolsoId), aggregateVersion),
            OfertaBecaEmitidaEvento emitida => Envolver(
                emitida,
                emitida.BecaId,
                new OfertaBecaEmitida(
                    emitida.BecaId,
                    emitida.OfertaId,
                    emitida.PostulacionId,
                    emitida.ProgramaBecaId,
                    emitida.PeriodoAcademicoId,
                    emitida.FechaVencimientoAceptacion,
                    EstadoOferta.PendienteRespuesta.ACodigo()),
                aggregateVersion),

            BecaActivadaEvento activada => Envolver(
                activada,
                activada.BecaId,
                new BecaActivada(
                    activada.BecaId,
                    activada.OfertaId,
                    activada.PostulacionId,
                    activada.ProgramaBecaId,
                    activada.PeriodoAcademicoId,
                    EstadoBeca.Activa.ACodigo()),
                aggregateVersion),

            OfertaBecaRechazadaEvento rechazada => Envolver(
                rechazada,
                rechazada.BecaId,
                new OfertaBecaRechazada(
                    rechazada.BecaId,
                    rechazada.OfertaId,
                    rechazada.PostulacionId,
                    rechazada.ProgramaBecaId,
                    rechazada.PeriodoAcademicoId,
                    EstadoOferta.RechazadaPorPostulante.ACodigo(),
                    TipoCierreBeca.OfertaRechazada.ACodigo()),
                aggregateVersion),

            OfertaBecaAnuladaEvento anulada => Envolver(
                anulada,
                anulada.BecaId,
                new OfertaBecaAnulada(
                    anulada.BecaId,
                    anulada.OfertaId,
                    anulada.PostulacionId,
                    anulada.ProgramaBecaId,
                    anulada.PeriodoAcademicoId,
                    EstadoOferta.AnuladaPorCoordinacion.ACodigo(),
                    TipoCierreBeca.OfertaAnulada.ACodigo()),
                aggregateVersion),

            OfertaBecaExpiradaEvento expirada => Envolver(
                expirada,
                expirada.BecaId,
                new OfertaBecaExpirada(
                    expirada.BecaId,
                    expirada.OfertaId,
                    expirada.PostulacionId,
                    expirada.ProgramaBecaId,
                    expirada.PeriodoAcademicoId,
                    EstadoOferta.Expirada.ACodigo(),
                    TipoCierreBeca.OfertaExpirada.ACodigo()),
                aggregateVersion),

            CompromisoBecarioCargadoEvento cargado => Envolver(
                cargado,
                cargado.BecaId,
                new CompromisoBecarioCargado(
                    cargado.BecaId,
                    cargado.CompromisoId,
                    cargado.NumeroIntentoCarga,
                    EstadoCompromiso.EnRevision.ACodigo()),
                aggregateVersion),

            CompromisoBecarioAprobadoEvento aprobado => Envolver(
                aprobado,
                aprobado.BecaId,
                new CompromisoBecarioAprobado(
                    aprobado.BecaId,
                    aprobado.CompromisoId,
                    EstadoCompromiso.Aprobado.ACodigo()),
                aggregateVersion),

            CompromisoBecarioRechazadoEvento rechazado => Envolver(
                rechazado,
                rechazado.BecaId,
                new CompromisoBecarioRechazado(
                    rechazado.BecaId,
                    rechazado.CompromisoId,
                    EstadoCompromiso.Rechazado.ACodigo()),
                aggregateVersion),

            _ => throw new InvalidOperationException(
                $"No existe contrato de integración para el evento '{evento.GetType().Name}'.")
        };
    }

    private static EventoIntegracion<TPayload> Envolver<TPayload>(
        IDomainEvent evento,
        Guid aggregateId,
        TPayload payload,
        int aggregateVersion)
        => new()
        {
            EventId = evento.EventId,
            OccurredAt = evento.OccurredAt,
            CorrelationId = evento.CorrelationId,
            Producer = Productor,
            AggregateId = aggregateId,
            AggregateVersion = aggregateVersion,
            Payload = payload
        };
}
