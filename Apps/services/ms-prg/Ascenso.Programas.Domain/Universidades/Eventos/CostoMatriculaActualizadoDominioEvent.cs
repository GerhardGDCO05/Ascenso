using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Universidades.Eventos;

/// <summary>
/// Evento de dominio emitido cuando se publica una nueva versión vigente del costo de matrícula de una carrera.
/// </summary>
public sealed record class CostoMatriculaActualizadoDominioEvent : EventoDominio
{
    public CostoMatriculaActualizadoDominioEvent(
        Guid universidadAliadaId,
        Guid carreraElegibleId,
        Guid costoMatriculaReferenciaId,
        string periodoAcademicoId,
        int numeroVersion,
        decimal montoReferenciaMatricula,
        string moneda,
        Guid correlacionId,
        DateTimeOffset ocurridoEn)
        : base(correlacionId, ocurridoEn)
    {
        UniversidadAliadaId = universidadAliadaId;
        CarreraElegibleId = carreraElegibleId;
        CostoMatriculaReferenciaId = costoMatriculaReferenciaId;
        PeriodoAcademicoId = periodoAcademicoId;
        NumeroVersion = numeroVersion;
        MontoReferenciaMatricula = montoReferenciaMatricula;
        Moneda = moneda;
    }

    public Guid UniversidadAliadaId { get; }

    public Guid CarreraElegibleId { get; }

    public Guid CostoMatriculaReferenciaId { get; }

    public string PeriodoAcademicoId { get; }

    public int NumeroVersion { get; }

    public decimal MontoReferenciaMatricula { get; }

    public string Moneda { get; }
}
