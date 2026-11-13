using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;

namespace Ascenso.Programas.Domain.Donantes.Eventos;

/// <summary>
/// Evento de dominio emitido cuando se registra un nuevo donante en el catálogo de MS-PRG.
/// </summary>
public sealed record class DonanteRegistradoDominioEvent : EventoDominio
{
    public DonanteRegistradoDominioEvent(
        Guid donanteId,
        TipoDonante tipoDonante,
        string razonSocialNombre,
        string identificacionFiscal,
        string moneda,
        Guid correlacionId,
        DateTimeOffset ocurridoEn)
        : base(correlacionId, ocurridoEn)
    {
        DonanteId = donanteId;
        TipoDonante = tipoDonante;
        RazonSocialNombre = razonSocialNombre;
        IdentificacionFiscal = identificacionFiscal;
        Moneda = moneda;
    }

    public Guid DonanteId { get; }

    public TipoDonante TipoDonante { get; }

    public string RazonSocialNombre { get; }

    public string IdentificacionFiscal { get; }

    public string Moneda { get; }
}

