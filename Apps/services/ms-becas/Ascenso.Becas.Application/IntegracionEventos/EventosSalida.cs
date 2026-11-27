namespace Ascenso.Becas.Application.IntegracionEventos;

public sealed record RenovacionBecaSolicitada(
    Guid SolicitudRenovacionId, Guid BecaId, Guid PeriodoOrigenId,
    Guid PeriodoSolicitadoId);

public sealed record RenovacionBecaAprobada(
    Guid SolicitudRenovacionId, Guid BecaAnteriorId, Guid BecaSucesoraId,
    Guid CadenaBecaId, Guid PeriodoOrigenId, Guid PeriodoNuevoId);

public sealed record BecaActivadaNuevoPeriodo(
    Guid BecaId, Guid BecaAnteriorId, Guid CadenaBecaId, Guid PeriodoNuevoId);

public sealed record BecaConcluidaPorVencimientoRenovacion(
    Guid BecaId, Guid CadenaBecaId, Guid ProgramaBecaId,
    Guid PeriodoOrigenId, Guid PeriodoSolicitadoId);

public sealed record DesembolsosBecaSuspendidos(
    Guid BecaId, Guid SuspensionId, Guid ProgramaBecaId,
    Guid PeriodoAcademicoId, decimal SaldoPendiente);

public sealed record DesembolsosBecaReanudados(
    Guid BecaId, Guid SuspensionId, Guid ProgramaBecaId,
    Guid PeriodoAcademicoId, decimal SaldoPendiente);

public sealed record BecaRevocadaDefinitivamente(
    Guid BecaId, Guid ProgramaBecaId, Guid PeriodoAcademicoId,
    string EstadoBeca, string TipoCierre);

public sealed record DesembolsoProgramado(Guid BecaId, Guid CalendarioId, Guid DesembolsoId);
public sealed record DesembolsoConfirmadoConOTP(Guid BecaId, Guid CalendarioId, Guid DesembolsoId);
public sealed record DesembolsoFallido(Guid BecaId, Guid CalendarioId, Guid DesembolsoId);
public sealed record DesembolsoDevuelto(Guid BecaId, Guid CalendarioId, Guid DesembolsoId);
public sealed record DesembolsoReintentado(Guid BecaId, Guid CalendarioId, Guid DesembolsoId);
public sealed record DesembolsoCancelado(Guid BecaId, Guid CalendarioId, Guid DesembolsoId);

/// <summary>
/// Payload de <c>OfertaBecaEmitida</c>. Solo contiene identificadores y el
/// estado resultante de la oferta.
/// </summary>
public sealed record OfertaBecaEmitida(
    Guid BecaId,
    Guid OfertaId,
    Guid PostulacionId,
    Guid ProgramaBecaId,
    Guid PeriodoAcademicoId,
    DateTimeOffset FechaVencimientoAceptacion,
    string EstadoOferta);

/// <summary>
/// Payload de <c>BecaActivada</c>. No incluye datos del estudiante ni montos
/// socioeconómicos.
/// </summary>
public sealed record BecaActivada(
    Guid BecaId,
    Guid OfertaId,
    Guid PostulacionId,
    Guid ProgramaBecaId,
    Guid PeriodoAcademicoId,
    string EstadoBeca);

/// <summary>
/// Payload de <c>OfertaBecaRechazada</c>.
/// </summary>
public sealed record OfertaBecaRechazada(
    Guid BecaId,
    Guid OfertaId,
    Guid PostulacionId,
    Guid ProgramaBecaId,
    Guid PeriodoAcademicoId,
    string EstadoOferta,
    string TipoCierre);

/// <summary>
/// Payload de <c>OfertaBecaAnulada</c>.
/// </summary>
public sealed record OfertaBecaAnulada(
    Guid BecaId,
    Guid OfertaId,
    Guid PostulacionId,
    Guid ProgramaBecaId,
    Guid PeriodoAcademicoId,
    string EstadoOferta,
    string TipoCierre);

/// <summary>
/// Payload de <c>OfertaBecaExpirada</c>.
/// </summary>
public sealed record OfertaBecaExpirada(
    Guid BecaId,
    Guid OfertaId,
    Guid PostulacionId,
    Guid ProgramaBecaId,
    Guid PeriodoAcademicoId,
    string EstadoOferta,
    string TipoCierre);

/// <summary>
/// Payload de <c>CompromisoBecarioCargado</c>.
/// </summary>
public sealed record CompromisoBecarioCargado(
    Guid BecaId,
    Guid CompromisoId,
    int NumeroIntentoCarga,
    string EstadoCompromiso);

/// <summary>
/// Payload de <c>CompromisoBecarioAprobado</c>.
/// </summary>
public sealed record CompromisoBecarioAprobado(
    Guid BecaId,
    Guid CompromisoId,
    string EstadoCompromiso);

/// <summary>
/// Payload de <c>CompromisoBecarioRechazado</c>.
/// </summary>
public sealed record CompromisoBecarioRechazado(
    Guid BecaId,
    Guid CompromisoId,
    string EstadoCompromiso);

/// <summary>Hito de entrega sin datos académicos personales.</summary>
public sealed record ReporteAcademicoEntregado(
    Guid ReporteAcademicoId, Guid BecaId, Guid PeriodoAcademicoId,
    DateOnly FechaLimitePeriodo, bool EntregaTardia);

/// <summary>Solicitud de alerta a MS-PLT para el coordinador del programa.</summary>
public sealed record AlertaReporteAcademicoTardio(
    Guid ReporteAcademicoId, Guid BecaId, Guid ProgramaBecaId,
    Guid PeriodoAcademicoId, DateOnly FechaLimitePeriodo);

/// <summary>Resultado de permanencia sin calificaciones ni identidad personal.</summary>
public sealed record EvaluacionPermanenciaRegistrada(
    Guid EvaluacionPermanenciaId, Guid ReporteAcademicoId, Guid BecaId,
    bool CumpleCondiciones, string? ConsecuenciaRecomendada);

/// <summary>Consecuencia ejecutada sobre una evaluación confirmada.</summary>
public sealed record ConsecuenciaPermanenciaAplicada(
    Guid EvaluacionPermanenciaId, Guid ReporteAcademicoId, Guid BecaId,
    string ConsecuenciaAplicada);
