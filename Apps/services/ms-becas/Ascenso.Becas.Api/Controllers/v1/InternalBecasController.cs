using Ascenso.Becas.Api.Seguridad;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ConsultarBecasActivas;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Becas.Api.Controllers.v1;

/// <summary>
/// Expone la consulta interna de becas activas consumida por otros
/// microservicios con un <em>Service Token</em> y el alcance interno.
/// </summary>
[ApiController]
[Route("internal/v1/becas")]
[Authorize(Policy = PoliticasSeguridad.ServiceAccountInterno)]
public sealed class InternalBecasController : ControllerBase
{
    private readonly IQueryDispatcher _consultas;

    public InternalBecasController(IQueryDispatcher consultas)
    {
        _consultas = consultas ?? throw new ArgumentNullException(nameof(consultas));
    }

    /// <summary>
    /// Devuelve las becas activas del estudiante en el periodo indicado, sin
    /// datos personales ni bancarios.
    /// </summary>
    [HttpGet("activas")]
    [ProducesResponseType(
        typeof(IReadOnlyList<BecaActivaDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BecaActivaDto>>> ConsultarActivas(
        [FromQuery] Guid postulanteId,
        [FromQuery] Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        if (postulanteId == Guid.Empty || periodoId == Guid.Empty)
        {
            throw new ValidacionAplicacionException(
                new Dictionary<string, string[]>
                {
                    [nameof(postulanteId)] =
                        ["El identificador del estudiante es obligatorio."],
                    [nameof(periodoId)] =
                        ["El identificador del periodo académico es obligatorio."]
                });
        }

        var resultado = await _consultas.ConsultarAsync<
            ConsultarBecasActivasQuery,
            IReadOnlyList<BecaActivaDto>>(
            new ConsultarBecasActivasQuery(postulanteId, periodoId),
            cancellationToken);

        return Ok(resultado);
    }
}
