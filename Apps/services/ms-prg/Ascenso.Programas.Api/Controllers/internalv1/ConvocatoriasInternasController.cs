using Ascenso.Programas.Api.Seguridad;
using Ascenso.Programas.Application.Convocatorias.Consultas.ConsultarConvocatoriasAbiertas;
using Ascenso.Programas.Application.Convocatorias.Consultas.ObtenerConvocatoriaPorId;
using Ascenso.Programas.Application.Convocatorias.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Programas.Api.Controllers.internalv1;

/// <summary>
/// Contrato interno de solo lectura para MS-POS. No expone escrituras y exige
/// el permiso <c>convocatorias:leer</c>.
/// </summary>
[ApiController]
[Route("internal/v1/convocatorias")]
[Authorize(Policy = PermisosConvocatoria.Leer)]
public sealed class ConvocatoriasInternasController : ControllerBase
{
    private readonly ObtenerConvocatoriaPorIdQueryHandler _obtenerPorIdHandler;
    private readonly ConsultarConvocatoriasAbiertasQueryHandler _consultarAbiertasHandler;

    public ConvocatoriasInternasController(
        ObtenerConvocatoriaPorIdQueryHandler obtenerPorIdHandler,
        ConsultarConvocatoriasAbiertasQueryHandler consultarAbiertasHandler)
    {
        _obtenerPorIdHandler = obtenerPorIdHandler ?? throw new ArgumentNullException(nameof(obtenerPorIdHandler));
        _consultarAbiertasHandler = consultarAbiertasHandler ?? throw new ArgumentNullException(nameof(consultarAbiertasHandler));
    }

    [HttpGet("{convocatoriaId:guid}")]
    [ProducesResponseType(typeof(ConvocatoriaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConvocatoriaDetalleDto>> ObtenerPorId(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerPorIdHandler.HandleAsync(
            new ObtenerConvocatoriaPorIdQuery(convocatoriaId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("abiertas")]
    [ProducesResponseType(typeof(IReadOnlyList<ConvocatoriaAbiertaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ConvocatoriaAbiertaDto>>> ConsultarAbiertas(
        CancellationToken cancellationToken = default)
    {
        var resultado = await _consultarAbiertasHandler.HandleAsync(
            new ConsultarConvocatoriasAbiertasQuery(),
            cancellationToken);

        return Ok(resultado);
    }
}
