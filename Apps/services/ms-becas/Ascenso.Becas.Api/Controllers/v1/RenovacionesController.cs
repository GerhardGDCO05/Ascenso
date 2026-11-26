using Ascenso.Becas.Api.Seguridad;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Renovacion.Comandos.EvaluarRenovacion;
using Ascenso.Becas.Application.Renovacion.Comandos.SolicitarRenovacion;
using Ascenso.Becas.Application.Renovacion.Consultas.ListarRenovacionesBeca;
using Ascenso.Becas.Application.Renovacion.Consultas.ObtenerRenovacion;
using Ascenso.Becas.Application.Renovacion.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Becas.Api.Controllers.v1;

/// <summary>Solicitud, evaluación y consulta de renovaciones por periodo.</summary>
[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class RenovacionesController : ControllerBase
{
    private readonly ICommandDispatcher _comandos;
    private readonly IQueryDispatcher _consultas;
    private readonly IConsultasBeca _becas;
    private readonly IAuthorizationService _autorizacion;

    public RenovacionesController(ICommandDispatcher comandos,
        IQueryDispatcher consultas, IConsultasBeca becas,
        IAuthorizationService autorizacion)
    {
        _comandos = comandos ?? throw new ArgumentNullException(nameof(comandos));
        _consultas = consultas ?? throw new ArgumentNullException(nameof(consultas));
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _autorizacion = autorizacion ?? throw new ArgumentNullException(nameof(autorizacion));
    }

    [HttpPost("becas/{becaId:guid}/renovaciones")]
    [Authorize(Roles = PoliticasSeguridad.RolBecario)]
    [ProducesResponseType(typeof(SolicitudRenovacionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SolicitudRenovacionDto>> Solicitar(
        Guid becaId, [FromBody] SolicitarRenovacionRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        await AutorizarPropietarioAsync(becaId, cancellationToken);
        var resultado = await _comandos.EnviarAsync<
            SolicitarRenovacionCommand, SolicitudRenovacionDto>(
            new SolicitarRenovacionCommand(becaId,
                solicitud.PeriodoSolicitadoId, solicitud.MontoSolicitado),
            cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { solicitudId = resultado.Id },
            resultado);
    }

    [HttpPost("renovaciones/{solicitudId:guid}/evaluacion")]
    [Authorize(Roles = PoliticasSeguridad.RolCoordinadorPrograma)]
    [Authorize(Policy = PoliticasSeguridad.Requiere2FA)]
    [ProducesResponseType(typeof(SolicitudRenovacionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SolicitudRenovacionDto>> Evaluar(
        Guid solicitudId, [FromBody] EvaluarRenovacionRequest decision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decision);
        var solicitud = await ObtenerSolicitudAsync(solicitudId, cancellationToken);
        await AutorizarCoordinadorAsync(solicitud.BecaId, cancellationToken);
        var resultado = await _comandos.EnviarAsync<
            EvaluarRenovacionCommand, SolicitudRenovacionDto>(
            new EvaluarRenovacionCommand(solicitudId,
                decision.Aprobar, decision.MotivoDecision), cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("renovaciones/{solicitudId:guid}")]
    [Authorize(Roles = PoliticasSeguridad.RolBecario + ","
        + PoliticasSeguridad.RolCoordinadorPrograma + ","
        + PoliticasSeguridad.RolDireccion)]
    [ProducesResponseType(typeof(SolicitudRenovacionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SolicitudRenovacionDto>> Obtener(
        Guid solicitudId, CancellationToken cancellationToken = default)
    {
        var solicitud = await ObtenerSolicitudAsync(solicitudId, cancellationToken);
        await AutorizarLecturaAsync(solicitud.BecaId, cancellationToken);
        return Ok(solicitud);
    }

    [HttpGet("becas/{becaId:guid}/renovaciones")]
    [Authorize(Roles = PoliticasSeguridad.RolBecario + ","
        + PoliticasSeguridad.RolCoordinadorPrograma + ","
        + PoliticasSeguridad.RolDireccion)]
    [ProducesResponseType(typeof(PaginaRenovacionesDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaRenovacionesDto>> Listar(
        Guid becaId, [FromQuery] int pagina = 1, [FromQuery] int tamano = 20,
        CancellationToken cancellationToken = default)
    {
        await AutorizarLecturaAsync(becaId, cancellationToken);
        var resultado = await _consultas.ConsultarAsync<
            ListarRenovacionesBecaQuery, PaginaRenovacionesDto>(
            new ListarRenovacionesBecaQuery(becaId, pagina, tamano),
            cancellationToken);
        return Ok(resultado);
    }

    private Task<SolicitudRenovacionDto> ObtenerSolicitudAsync(Guid solicitudId,
        CancellationToken token)
        => _consultas.ConsultarAsync<ObtenerRenovacionQuery,
            SolicitudRenovacionDto>(new ObtenerRenovacionQuery(solicitudId), token);

    private async Task<RecursoBecaDto> ObtenerRecursoAsync(Guid becaId,
        CancellationToken token)
        => await _becas.ObtenerRecursoBecaAsync(becaId, token)
            ?? throw new RecursoNoEncontradoException("beca", becaId);

    private async Task AutorizarPropietarioAsync(Guid becaId,
        CancellationToken token)
    {
        var recurso = await ObtenerRecursoAsync(becaId, token);
        var resultado = await _autorizacion.AuthorizeAsync(User,
            new RecursoAutorizable(PostulanteId: recurso.PostulanteEstudianteId),
            PoliticasSeguridad.PropiedadPostulante);
        if (!resultado.Succeeded)
            throw new OperacionNoPermitidaException(
                "El usuario no es dueño de la beca.");
    }

    private async Task AutorizarCoordinadorAsync(Guid becaId,
        CancellationToken token)
    {
        var recurso = await ObtenerRecursoAsync(becaId, token);
        var resultado = await _autorizacion.AuthorizeAsync(User,
            new RecursoAutorizable(ProgramaId: recurso.ProgramaBecaId),
            PoliticasSeguridad.AlcancePrograma);
        if (!resultado.Succeeded)
            throw new OperacionNoPermitidaException(
                "Sin alcance sobre el programa de la beca.");
    }

    private async Task AutorizarLecturaAsync(Guid becaId,
        CancellationToken token)
    {
        var recurso = await ObtenerRecursoAsync(becaId, token);
        if (User.IsInRole(PoliticasSeguridad.RolBecario)
            && (await _autorizacion.AuthorizeAsync(User,
                new RecursoAutorizable(PostulanteId: recurso.PostulanteEstudianteId),
                PoliticasSeguridad.PropiedadPostulante)).Succeeded)
            return;
        if ((User.IsInRole(PoliticasSeguridad.RolCoordinadorPrograma)
                || User.IsInRole(PoliticasSeguridad.RolDireccion))
            && (await _autorizacion.AuthorizeAsync(User,
                new RecursoAutorizable(ProgramaId: recurso.ProgramaBecaId),
                PoliticasSeguridad.AlcancePrograma)).Succeeded)
            return;
        throw new OperacionNoPermitidaException(
            "Sin acceso a las renovaciones de esta beca.");
    }

    public sealed record SolicitarRenovacionRequest(
        Guid PeriodoSolicitadoId, decimal MontoSolicitado);

    public sealed record EvaluarRenovacionRequest(
        bool Aprobar, string MotivoDecision);
}
