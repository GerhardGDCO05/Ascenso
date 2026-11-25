using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Convocatorias.Eventos;

/// <summary>
/// Evento de dominio emitido cuando una convocatoria pasa de borrador a publicada.
/// </summary>
public sealed record class ConvocatoriaPublicadaDominioEvent : EventoDominio
{
    public ConvocatoriaPublicadaDominioEvent(
        Guid convocatoriaId,
        Guid programaBecaId,
        string codigo,
        DateTimeOffset fechaApertura,
        DateTimeOffset fechaCierre,
        int cuposAsignados,
        Guid correlacionId,
        DateTimeOffset ocurridoEn)
        : base(correlacionId, ocurridoEn)
    {
        ConvocatoriaId = convocatoriaId;
        ProgramaBecaId = programaBecaId;
        Codigo = codigo;
        FechaApertura = fechaApertura;
        FechaCierre = fechaCierre;
        CuposAsignados = cuposAsignados;
    }

    public Guid ConvocatoriaId { get; }

    public Guid ProgramaBecaId { get; }

    public string Codigo { get; }

    public DateTimeOffset FechaApertura { get; }

    public DateTimeOffset FechaCierre { get; }

    public int CuposAsignados { get; }
}
