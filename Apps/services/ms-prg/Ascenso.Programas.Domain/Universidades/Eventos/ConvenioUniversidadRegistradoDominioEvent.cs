using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Universidades.Eventos;

/// <summary>
/// Evento de dominio emitido cuando se registra un convenio vigente para una universidad aliada.
/// </summary>
public sealed record class ConvenioUniversidadRegistradoDominioEvent : EventoDominio
{
    public ConvenioUniversidadRegistradoDominioEvent(
        Guid universidadAliadaId,
        Guid convenioId,
        string codigoInstitucional,
        string numeroConvenio,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        Guid correlacionId,
        DateTimeOffset ocurridoEn)
        : base(correlacionId, ocurridoEn)
    {
        UniversidadAliadaId = universidadAliadaId;
        ConvenioId = convenioId;
        CodigoInstitucional = codigoInstitucional;
        NumeroConvenio = numeroConvenio;
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
    }

    public Guid UniversidadAliadaId { get; }

    public Guid ConvenioId { get; }

    public string CodigoInstitucional { get; }

    public string NumeroConvenio { get; }

    public DateOnly FechaInicio { get; }

    public DateOnly FechaFin { get; }
}
