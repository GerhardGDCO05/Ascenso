using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Application.Comites.Comandos.AgregarMiembroComite;

public sealed record class AgregarMiembroComiteCommand(
    Guid ComiteId,
    Guid UsuarioEvaluadorId,
    TipoEvaluador Tipo);
