namespace Ascenso.Programas.Application.Universidades.Comandos.ActualizarCostoMatricula;

public sealed record class ActualizarCostoMatriculaCommand(
    Guid UniversidadAliadaId,
    Guid CarreraElegibleId,
    string PeriodoAcademicoId,
    decimal MontoReferenciaMatricula,
    string Moneda,
    Guid UsuarioModificacionId);
