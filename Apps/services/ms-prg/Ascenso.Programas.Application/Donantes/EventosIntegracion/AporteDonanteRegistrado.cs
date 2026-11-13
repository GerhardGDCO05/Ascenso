namespace Ascenso.Programas.Application.Donantes.EventosIntegracion;

/// <summary>
/// Evento de integración publicado mediante Transactional Outbox cuando se confirma un aporte financiero.
/// </summary>
public sealed record class AporteDonanteRegistrado(
    Guid EventoId,
    DateTimeOffset OcurridoEn,
    Guid DonanteId,
    Guid AporteDonanteId,
    decimal MontoAporte,
    string Moneda,
    string PeriodoAcademicoId,
    string NumeroReferenciaBancaria,
    string UrlComprobanteFirebase,
    decimal SaldoTotalHistorico,
    decimal SaldoDisponibleAsignacion);

