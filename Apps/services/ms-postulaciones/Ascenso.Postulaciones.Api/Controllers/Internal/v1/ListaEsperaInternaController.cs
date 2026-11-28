using Ascenso.Postulaciones.Api.Controllers.v1;
using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Application.ListaEspera.Comandos.RegistrarVacanteLiberada;
using Ascenso.Postulaciones.Application.ListaEspera.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Postulaciones.Api.Controllers.Internal.v1;

[ApiController]
[Route("internal/v1/lista-espera")]
public sealed class ListaEsperaInternaController : ControllerBase
{
    private readonly RegistrarVacanteLiberadaCommandHandler _registrarVacanteHandler;

    public ListaEsperaInternaController(
        RegistrarVacanteLiberadaCommandHandler registrarVacanteHandler)
    {
        _registrarVacanteHandler = registrarVacanteHandler
            ?? throw new ArgumentNullException(nameof(registrarVacanteHandler));
    }

    [HttpPost("convocatorias/{convocatoriaId:guid}/vacantes-liberadas")]
    [Authorize(Policy = PoliticasAutorizacionListaEspera.RegistroVacanteListaEsperaInterna)]
    [ProducesResponseType(typeof(EntradaListaEsperaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EntradaListaEsperaDto>> RegistrarVacanteLiberada(
        [FromRoute] Guid convocatoriaId,
        [FromBody] ListaEsperaController.RegistrarVacanteLiberadaRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _registrarVacanteHandler.HandleAsync(
            new RegistrarVacanteLiberadaCommand(
                convocatoriaId,
                request?.MotivoVacante),
            cancellationToken);

        return Ok(resultado);
    }
}
