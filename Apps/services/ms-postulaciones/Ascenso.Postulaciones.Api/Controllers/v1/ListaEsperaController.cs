using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Application.ListaEspera.Comandos.PromoverCandidatoListaEspera;
using Ascenso.Postulaciones.Application.ListaEspera.Comandos.RegistrarVacanteLiberada;
using Ascenso.Postulaciones.Application.ListaEspera.Consultas.ObtenerListaEsperaConvocatoria;
using Ascenso.Postulaciones.Application.ListaEspera.Consultas.ObtenerSiguienteCandidatoListaEspera;
using Ascenso.Postulaciones.Application.ListaEspera.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Postulaciones.Api.Controllers.v1;

[ApiController]
[Route("api/v1/lista-espera")]
public sealed class ListaEsperaController : ControllerBase
{
    private readonly ObtenerListaEsperaConvocatoriaQueryHandler _obtenerListaHandler;
    private readonly ObtenerSiguienteCandidatoListaEsperaQueryHandler _obtenerSiguienteHandler;
    private readonly RegistrarVacanteLiberadaCommandHandler _registrarVacanteHandler;
    private readonly PromoverCandidatoListaEsperaCommandHandler _promoverCandidatoHandler;

    public ListaEsperaController(
        ObtenerListaEsperaConvocatoriaQueryHandler obtenerListaHandler,
        ObtenerSiguienteCandidatoListaEsperaQueryHandler obtenerSiguienteHandler,
        RegistrarVacanteLiberadaCommandHandler registrarVacanteHandler,
        PromoverCandidatoListaEsperaCommandHandler promoverCandidatoHandler)
    {
        _obtenerListaHandler = obtenerListaHandler ?? throw new ArgumentNullException(nameof(obtenerListaHandler));
        _obtenerSiguienteHandler = obtenerSiguienteHandler ?? throw new ArgumentNullException(nameof(obtenerSiguienteHandler));
        _registrarVacanteHandler = registrarVacanteHandler ?? throw new ArgumentNullException(nameof(registrarVacanteHandler));
        _promoverCandidatoHandler = promoverCandidatoHandler ?? throw new ArgumentNullException(nameof(promoverCandidatoHandler));
    }

    [HttpGet("convocatorias/{convocatoriaId:guid}")]
    [Authorize(Policy = PoliticasAutorizacionListaEspera.ConsultaListaEspera)]
    [ProducesResponseType(typeof(ListaEsperaConvocatoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ListaEsperaConvocatoriaDto>> ObtenerListaEsperaPorConvocatoria(
        [FromRoute] Guid convocatoriaId,
        [FromQuery] string? estado = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = ObtenerListaEsperaConvocatoriaQuery.TamanoPorDefecto,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerListaHandler.HandleAsync(
            new ObtenerListaEsperaConvocatoriaQuery(
                convocatoriaId,
                estado,
                pagina,
                tamano),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("convocatorias/{convocatoriaId:guid}/siguiente")]
    [Authorize(Policy = PoliticasAutorizacionListaEspera.ConsultaListaEspera)]
    [ProducesResponseType(typeof(EntradaListaEsperaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EntradaListaEsperaDto>> ObtenerSiguienteCandidato(
        [FromRoute] Guid convocatoriaId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerSiguienteHandler.HandleAsync(
            new ObtenerSiguienteCandidatoListaEsperaQuery(convocatoriaId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpPost("convocatorias/{convocatoriaId:guid}/vacantes-liberadas")]
    [Authorize(Policy = PoliticasAutorizacionListaEspera.GestionListaEspera)]
    [ProducesResponseType(typeof(EntradaListaEsperaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EntradaListaEsperaDto>> RegistrarVacanteLiberada(
        [FromRoute] Guid convocatoriaId,
        [FromBody] RegistrarVacanteLiberadaRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _registrarVacanteHandler.HandleAsync(
            new RegistrarVacanteLiberadaCommand(
                convocatoriaId,
                request?.MotivoVacante),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpPost("convocatorias/{convocatoriaId:guid}/entradas/{entradaListaEsperaId:guid}/promover")]
    [Authorize(Policy = PoliticasAutorizacionListaEspera.GestionListaEspera)]
    [ProducesResponseType(typeof(PromocionListaEsperaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PromocionListaEsperaDto>> PromoverCandidatoPorEntrada(
        [FromRoute] Guid convocatoriaId,
        [FromRoute] Guid entradaListaEsperaId,
        [FromBody] PromoverCandidatoListaEsperaRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _promoverCandidatoHandler.HandleAsync(
            new PromoverCandidatoListaEsperaCommand(
                convocatoriaId,
                entradaListaEsperaId,
                request?.MotivoPromocion),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpPost("convocatorias/{convocatoriaId:guid}/promover-siguiente")]
    [Authorize(Policy = PoliticasAutorizacionListaEspera.GestionListaEspera)]
    [ProducesResponseType(typeof(PromocionListaEsperaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PromocionListaEsperaDto>> PromoverSiguienteCandidato(
        [FromRoute] Guid convocatoriaId,
        [FromBody] PromoverCandidatoListaEsperaRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _promoverCandidatoHandler.HandleAsync(
            new PromoverCandidatoListaEsperaCommand(
                convocatoriaId,
                EntradaListaEsperaId: null,
                MotivoPromocion: request?.MotivoPromocion),
            cancellationToken);

        return Ok(resultado);
    }

    public sealed record class RegistrarVacanteLiberadaRequest(string? MotivoVacante = null);

    public sealed record class PromoverCandidatoListaEsperaRequest(string? MotivoPromocion = null);
}
