namespace Ascenso.Programas.Application.Universidades.EventosIntegracion;

public sealed record class CostoMatriculaActualizado(
    Guid EventoId,
    DateTimeOffset OcurridoEn,
    Guid UniversidadAliadaId,
    Guid CarreraElegibleId,
    Guid CostoMatriculaReferenciaId,
    string PeriodoAcademicoId,
    int NumeroVersion,
    decimal MontoReferenciaMatricula,
    string Moneda);
