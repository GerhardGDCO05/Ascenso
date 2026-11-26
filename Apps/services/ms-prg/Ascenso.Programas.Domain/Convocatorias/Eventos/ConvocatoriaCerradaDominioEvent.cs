using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Convocatorias.Eventos;

/// <summary>
/// Evento de dominio emitido cuando una convocatoria publicada alcanza su fecha de cierre.
/// </summary>
public sealed record class ConvocatoriaCerradaDominioEvent : EventoDominio
{
    public ConvocatoriaCerradaDominioEvent(
        Guid convocatoriaId,
        Guid programaBecaId,
        string codigo,
        DateTimeOffset fechaCierre,
        Guid correlacionId,
        DateTimeOffset ocurridoEn)
        : base(correlacionId, ocurridoEn)
    {
        ConvocatoriaId = convocatoriaId;
        ProgramaBecaId = programaBecaId;
        Codigo = codigo;
        FechaCierre = fechaCierre;
    }

    public Guid ConvocatoriaId { get; }

    public Guid ProgramaBecaId { get; }

    public string Codigo { get; }

    public DateTimeOffset FechaCierre { get; }
}
