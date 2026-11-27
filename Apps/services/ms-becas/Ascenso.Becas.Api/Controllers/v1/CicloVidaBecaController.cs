using Ascenso.Becas.Api.Contexto;
using Ascenso.Becas.Api.Seguridad;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Comandos.CicloVidaBeca;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Becas.Api.Controllers.v1;

/// <summary>Transiciones administrativas del ciclo de vida de la beca.</summary>
[ApiController]
[Authorize(Roles = PoliticasSeguridad.RolesAdministrativos)]
[Authorize(Policy = PoliticasSeguridad.Requiere2FA)]
[Route("api/v1/becas/{becaId:guid}")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
public sealed class CicloVidaBecaController : ControllerBase
{
    private readonly ICommandDispatcher _comandos;
    private readonly IConsultasBeca _becas;
    private readonly IAuthorizationService _autorizacion;

    public CicloVidaBecaController(ICommandDispatcher comandos,
        IConsultasBeca becas, IAuthorizationService autorizacion)
    {
        _comandos = comandos ?? throw new ArgumentNullException(nameof(comandos));
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _autorizacion = autorizacion ?? throw new ArgumentNullException(nameof(autorizacion));
    }

    [HttpPost("suspender")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> Suspender(Guid becaId,
        [FromBody] SuspenderBecaRequest solicitud,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        await ExigirAlcanceAsync(becaId, cancellationToken);
        var suspensionId = await _comandos.EnviarAsync<SuspenderBecaCommand, Guid>(
            new(becaId, TipoSuspensionBeca.Manual, solicitud.Motivo,
                solicitud.RequisitosParaReanudar, solicitud.FechaInicio,
                solicitud.FechaRevision, solicitud.EsIndefinida,
                solicitud.Justificacion), cancellationToken);
        return Ok(suspensionId);
    }

    [HttpPost("reanudar")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> Reanudar(Guid becaId,
        [FromBody] ReanudarBecaRequest solicitud,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        await ExigirAlcanceAsync(becaId, cancellationToken);
        return Ok(await _comandos.EnviarAsync<ReanudarBecaCommand, Guid>(
            new(becaId, solicitud.Justificacion), cancellationToken));
    }

    [HttpPost("revocar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Revocar(Guid becaId,
        [FromBody] RevocarBecaRequest solicitud,
        [FromHeader(Name = ContextoOperacionHttp.EncabezadoAutorizacionOtp)]
            Guid? autorizacionOtpId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        if (autorizacionOtpId is null || autorizacionOtpId == Guid.Empty)
            throw new ValidacionAplicacionException(new Dictionary<string, string[]>
            {
                [ContextoOperacionHttp.EncabezadoAutorizacionOtp] =
                    ["Se requiere un identificador de autorización OTP válido."]
            });
        await ExigirAlcanceAsync(becaId, cancellationToken);
        await _comandos.EnviarAsync(new RevocarBecaDefinitivamenteCommand(
            becaId, solicitud.Justificacion, solicitud.CodigoOtp), cancellationToken);
        return Ok();
    }

    private async Task ExigirAlcanceAsync(Guid becaId, CancellationToken token)
    {
        var recurso = await _becas.ObtenerRecursoBecaAsync(becaId, token)
            ?? throw new RecursoNoEncontradoException("beca", becaId);
        var resultado = await _autorizacion.AuthorizeAsync(User,
            new RecursoAutorizable(ProgramaId: recurso.ProgramaBecaId),
            PoliticasSeguridad.AlcancePrograma);
        if (!resultado.Succeeded)
            throw new OperacionNoPermitidaException(
                "Sin alcance sobre el programa de la beca.");
    }

    public sealed record SuspenderBecaRequest(string Motivo,
        string RequisitosParaReanudar, DateOnly FechaInicio,
        DateOnly? FechaRevision, bool EsIndefinida, string Justificacion);

    public sealed record ReanudarBecaRequest(string Justificacion);

    public sealed record RevocarBecaRequest(string Justificacion, string CodigoOtp);
}
