using Ascenso.Programas.Application.Programas.DTOs;

namespace Ascenso.Programas.Application.Programas.Comandos.ConfigurarCriteriosElegibilidad;

public sealed record ConfigurarCriteriosElegibilidadCommand(
    Guid ProgramaBecaId,
    CriteriosElegibilidadProgramaDto CriteriosElegibilidad,
    CondicionesPermanenciaDto CondicionesPermanencia,
    Guid UsuarioModificacionId);
