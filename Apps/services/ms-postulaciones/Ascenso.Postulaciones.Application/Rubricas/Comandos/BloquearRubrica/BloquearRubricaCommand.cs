using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Application.Rubricas.Comandos.BloquearRubrica;

public sealed record class BloquearRubricaCommand(
    Guid RubricaId,
    MotivoBloqueoRubrica Motivo);
