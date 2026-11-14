using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Donantes.Eventos;

/// <summary>
/// Evento de dominio emitido tras registrar un aporte financiero confirmado para un donante.
/// </summary>
public sealed record class AporteDonanteRegistradoDominioEvent : EventoDominio
{
    public AporteDonanteRegistradoDominioEvent(
        Guid aporteDonanteId,
        Guid donanteId,
        decimal montoAporte,
        string moneda,
        string periodoAcademicoId,
        DateTimeOffset fechaAporte,
        string numeroReferenciaBancaria,
        string urlComprobanteFirebase,
        decimal saldoTotalHistoricoResultante,
        decimal saldoDisponibleResultante,
        Guid correlacionId,
        DateTimeOffset ocurridoEn)
        : base(correlacionId, ocurridoEn)
    {
        AporteDonanteId = aporteDonanteId;
        DonanteId = donanteId;
        MontoAporte = montoAporte;
        Moneda = moneda;
        PeriodoAcademicoId = periodoAcademicoId;
        FechaAporte = fechaAporte;
        NumeroReferenciaBancaria = numeroReferenciaBancaria;
        UrlComprobanteFirebase = urlComprobanteFirebase;
        SaldoTotalHistoricoResultante = saldoTotalHistoricoResultante;
        SaldoDisponibleResultante = saldoDisponibleResultante;
    }

    public Guid AporteDonanteId { get; }

    public Guid DonanteId { get; }

    public decimal MontoAporte { get; }

    public string Moneda { get; }

    public string PeriodoAcademicoId { get; }

    public DateTimeOffset FechaAporte { get; }

    public string NumeroReferenciaBancaria { get; }

    public string UrlComprobanteFirebase { get; }

    public decimal SaldoTotalHistoricoResultante { get; }

    public decimal SaldoDisponibleResultante { get; }
}

