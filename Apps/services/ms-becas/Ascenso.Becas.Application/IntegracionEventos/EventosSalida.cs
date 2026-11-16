namespace Ascenso.Becas.Application.IntegracionEventos;

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
