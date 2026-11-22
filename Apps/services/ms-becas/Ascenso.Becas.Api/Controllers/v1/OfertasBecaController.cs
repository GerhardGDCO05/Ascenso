using System.ComponentModel.DataAnnotations;
using Ascenso.Becas.Api.Seguridad;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Comandos.AceptarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.AnularOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.EmitirOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.RechazarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerOferta;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Becas.Api.Controllers.v1;

/// <summary>
/// Expone el ciclo de vida de la oferta de beca: emisión, consulta y respuesta
/// del postulante o de coordinación.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/becas/ofertas")]
public sealed class OfertasBecaController : ControllerBase
{
    private readonly ICommandDispatcher _comandos;
    private readonly IQueryDispatcher _consultas;
    private readonly IConsultasBeca _consultasBeca;
    private readonly IAuthorizationService _autorizacion;
    private readonly IPostulacionesClient _postulaciones;
    private readonly IAlmacenamientoArchivos _almacenamiento;

    public OfertasBecaController(
        ICommandDispatcher comandos,
        IQueryDispatcher consultas,
        IConsultasBeca consultasBeca,
        IAuthorizationService autorizacion,
        IPostulacionesClient postulaciones,
        IAlmacenamientoArchivos almacenamiento)
    {
        _comandos = comandos ?? throw new ArgumentNullException(nameof(comandos));
        _consultas = consultas ?? throw new ArgumentNullException(nameof(consultas));
        _consultasBeca = consultasBeca ?? throw new ArgumentNullException(nameof(consultasBeca));
        _autorizacion = autorizacion ?? throw new ArgumentNullException(nameof(autorizacion));
        _postulaciones = postulaciones ?? throw new ArgumentNullException(nameof(postulaciones));
        _almacenamiento = almacenamiento ?? throw new ArgumentNullException(nameof(almacenamiento));
    }

    /// <summary>
    /// Emite una oferta para una postulación adjudicable.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = PoliticasSeguridad.RolesAdministrativos)]
    [Authorize(Policy = PoliticasSeguridad.Requiere2FA)]
    [ProducesResponseType(typeof(OfertaEmitidaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<OfertaEmitidaDto>> Emitir(
        [FromBody] EmitirOfertaRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        if (solicitud.PostulacionId == Guid.Empty)
        {
            throw new ValidacionAplicacionException(
                new Dictionary<string, string[]>
                {
                    [nameof(EmitirOfertaRequest.PostulacionId)] =
                        ["La postulación es obligatoria."]
                });
        }

        var adjudicacion = await ObtenerAdjudicabilidadAsync(
            solicitud.PostulacionId,
            cancellationToken);

        // Una postulación no adjudicable puede no traer el programa. En ese
        // caso el caso de uso responderá con el error de negocio; evaluar el
        // alcance con un programa vacío lo ocultaría con un 403.
        if (adjudicacion.ProgramaBecaId != Guid.Empty)
        {
            await ValidarAlcanceProgramaAsync(
                adjudicacion.ProgramaBecaId,
                cancellationToken);
        }

        var resultado = await _comandos.EnviarAsync<
            EmitirOfertaBecaCommand,
            OfertaEmitidaDto>(
            new EmitirOfertaBecaCommand(solicitud.PostulacionId),
            cancellationToken);

        return CreatedAtAction(
            nameof(Obtener),
            new { ofertaId = resultado.OfertaId },
            resultado);
    }

    /// <summary>
    /// Consulta una oferta cuando el usuario es el postulante dueño o un
    /// coordinador con alcance sobre el programa.
    /// </summary>
    [HttpGet("{ofertaId:guid}")]
    [ProducesResponseType(typeof(OfertaBecaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OfertaBecaDto>> Obtener(
        Guid ofertaId,
        CancellationToken cancellationToken = default)
    {
        var recurso = await ObtenerRecursoAsync(ofertaId, cancellationToken);

        if (!await PuedeConsultarAsync(recurso, cancellationToken))
        {
            throw new OperacionNoPermitidaException(
                "El usuario no es dueño de la oferta ni tiene alcance sobre su programa.");
        }

        var resultado = await _consultas.ConsultarAsync<
            ObtenerOfertaQuery,
            OfertaBecaDto>(
            new ObtenerOfertaQuery(ofertaId),
            cancellationToken);

        var urlCarta = await _almacenamiento.ResolverUrlLecturaAsync(
            resultado.UrlCartaPdf,
            cancellationToken);

        return Ok(resultado with { UrlCartaPdf = urlCarta });
    }

    /// <summary>
    /// Acepta la oferta y activa la beca.
    /// </summary>
    [HttpPost("{ofertaId:guid}/aceptar")]
    [Authorize(Roles = PoliticasSeguridad.RolPostulante)]
    [ProducesResponseType(typeof(OfertaRespondidaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfertaRespondidaDto>> Aceptar(
        Guid ofertaId,
        CancellationToken cancellationToken = default)
    {
        var recurso = await ObtenerRecursoAsync(ofertaId, cancellationToken);
        await ValidarPropiedadAsync(recurso, cancellationToken);

        var resultado = await _comandos.EnviarAsync<
            AceptarOfertaBecaCommand,
            OfertaRespondidaDto>(
            new AceptarOfertaBecaCommand(ofertaId),
            cancellationToken);

        return Ok(resultado);
    }

    /// <summary>
    /// Rechaza formalmente una oferta pendiente.
    /// </summary>
    [HttpPost("{ofertaId:guid}/rechazar")]
    [Authorize(Roles = PoliticasSeguridad.RolPostulante)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Rechazar(
        Guid ofertaId,
        [FromBody] RechazarOfertaRequest? solicitud = null,
        CancellationToken cancellationToken = default)
    {
        var recurso = await ObtenerRecursoAsync(ofertaId, cancellationToken);
        await ValidarPropiedadAsync(recurso, cancellationToken);

        await _comandos.EnviarAsync(
            new RechazarOfertaBecaCommand(ofertaId, solicitud?.Motivo),
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Anula una oferta pendiente con una justificación de coordinación.
    /// </summary>
    [HttpPost("{ofertaId:guid}/anular")]
    [Authorize(Roles = PoliticasSeguridad.RolesAdministrativos)]
    [Authorize(Policy = PoliticasSeguridad.Requiere2FA)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Anular(
        Guid ofertaId,
        [FromBody] AnularOfertaRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var recurso = await ObtenerRecursoAsync(ofertaId, cancellationToken);
        await ValidarAlcanceProgramaAsync(recurso.ProgramaBecaId, cancellationToken);

        await _comandos.EnviarAsync(
            new AnularOfertaBecaCommand(ofertaId, solicitud.Justificacion),
            cancellationToken);

        return NoContent();
    }

    private async Task<ResultadoAdjudicabilidad> ObtenerAdjudicabilidadAsync(
        Guid postulacionId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _postulaciones.ObtenerAdjudicabilidadAsync(
                postulacionId,
                cancellationToken);
        }
        catch (Exception excepcion) when (
            !cancellationToken.IsCancellationRequested
            && excepcion is HttpRequestException or TimeoutException or TaskCanceledException)
        {
            throw new DependenciaNoDisponibleException("MS-POS", excepcion);
        }
    }

    private async Task<RecursoOfertaDto> ObtenerRecursoAsync(
        Guid ofertaId,
        CancellationToken cancellationToken)
        => await _consultasBeca.ObtenerRecursoOfertaAsync(ofertaId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("oferta", ofertaId);

    private async Task<bool> PuedeConsultarAsync(
        RecursoOfertaDto recurso,
        CancellationToken cancellationToken)
    {
        var esCoordinador = User.IsInRole(PoliticasSeguridad.RolCoordinadorPrograma)
            || User.IsInRole(PoliticasSeguridad.RolDireccion);

        if (esCoordinador)
        {
            var alcance = await _autorizacion.AuthorizeAsync(
                User,
                new RecursoAutorizable(ProgramaId: recurso.ProgramaBecaId),
                PoliticasSeguridad.AlcancePrograma);

            if (alcance.Succeeded)
            {
                return true;
            }
        }

        var propiedad = await _autorizacion.AuthorizeAsync(
            User,
            new RecursoAutorizable(PostulanteId: recurso.PostulanteEstudianteId),
            PoliticasSeguridad.PropiedadPostulante);

        return propiedad.Succeeded;
    }

    private async Task ValidarPropiedadAsync(
        RecursoOfertaDto recurso,
        CancellationToken cancellationToken)
    {
        var resultado = await _autorizacion.AuthorizeAsync(
            User,
            new RecursoAutorizable(PostulanteId: recurso.PostulanteEstudianteId),
            PoliticasSeguridad.PropiedadPostulante);

        if (!resultado.Succeeded)
        {
            throw new OperacionNoPermitidaException(
                "El usuario no es dueño de la oferta.");
        }
    }

    private async Task ValidarAlcanceProgramaAsync(
        Guid programaId,
        CancellationToken cancellationToken)
    {
        var resultado = await _autorizacion.AuthorizeAsync(
            User,
            new RecursoAutorizable(ProgramaId: programaId),
            PoliticasSeguridad.AlcancePrograma);

        if (!resultado.Succeeded)
        {
            throw new OperacionNoPermitidaException(
                "El usuario no tiene alcance sobre el programa de la oferta.");
        }
    }

    /// <summary>
    /// Cuerpo de la solicitud de emisión.
    /// </summary>
    public sealed record EmitirOfertaRequest(Guid PostulacionId);

    /// <summary>
    /// Cuerpo opcional de la solicitud de rechazo.
    /// </summary>
    public sealed record RechazarOfertaRequest(
        [StringLength(
            1000,
            ErrorMessage = "El motivo del rechazo no puede superar 1000 caracteres.")]
        string? Motivo);

    /// <summary>
    /// Cuerpo de la solicitud de anulación.
    /// </summary>
    public sealed record AnularOfertaRequest(
        [Required(ErrorMessage = "La justificación es obligatoria.")]
        [StringLength(
            1000,
            MinimumLength = 10,
            ErrorMessage = "La justificación debe tener entre 10 y 1000 caracteres.")]
        string Justificacion);
}
