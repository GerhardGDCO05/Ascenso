using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Universidades.Eventos;

/// <summary>
/// Evento de dominio emitido cuando un convenio de universidad aliada transiciona a estado Vencido.
/// </summary>
public sealed record class ConvenioUniversidadVencidoDominioEvent : EventoDominio
{
    public ConvenioUniversidadVencidoDominioEvent(
        Guid universidadAliadaId,
        Guid convenioId,
        string numeroConvenio,
        bool mantieneConvenioVigente,
        Guid correlacionId,
        DateTimeOffset ocurridoEn)
        : base(correlacionId, ocurridoEn)
    {
        UniversidadAliadaId = universidadAliadaId;
        ConvenioId = convenioId;
        NumeroConvenio = numeroConvenio;
        MantieneConvenioVigente = mantieneConvenioVigente;
    }

    public Guid UniversidadAliadaId { get; }

    public Guid ConvenioId { get; }

    public string NumeroConvenio { get; }

    public bool MantieneConvenioVigente { get; }
}
