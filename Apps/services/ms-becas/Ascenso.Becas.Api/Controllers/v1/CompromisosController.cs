using System.ComponentModel.DataAnnotations;
using Ascenso.Becas.Api.Seguridad;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Comandos.CargarCompromisoBecario;
using Ascenso.Becas.Application.Otorgamiento.Comandos.RevisarCompromisoBecario;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Becas.Api.Controllers.v1;

/// <summary>
/// Expone la carga del compromiso firmado por el becario y su revisión por
/// coordinación.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/becas")]
public sealed class CompromisosController : ControllerBase
{
    private readonly ICommandDispatcher _comandos;
    private readonly IConsultasBeca _consultasBeca;
    private readonly IAuthorizationService _autorizacion;

    public CompromisosController(
        ICommandDispatcher comandos,
        IConsultasBeca consultasBeca,
        IAuthorizationService autorizacion)
    {
        _comandos = comandos ?? throw new ArgumentNullException(nameof(comandos));
        _consultasBeca = consultasBeca ?? throw new ArgumentNullException(nameof(consultasBeca));
        _autorizacion = autorizacion ?? throw new ArgumentNullException(nameof(autorizacion));
    }

    /// <summary>
    /// Carga un compromiso firmado en PDF para una beca del becario
    /// autenticado.
    /// </summary>
    [HttpPost("{becaId:guid}/compromisos")]
    [Authorize(Roles = PoliticasSeguridad.RolBecario)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CompromisoCargadoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CompromisoCargadoDto>> Cargar(
        Guid becaId,
        [FromForm] CargarCompromisoRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var recurso = await ObtenerRecursoBecaAsync(becaId, cancellationToken);
        await ValidarPropiedadAsync(recurso, cancellationToken);

        var archivo = solicitud.Archivo;

        if (archivo is null || archivo.Length == 0)
        {
            throw new ArchivoCompromisoInvalidoException(
                "El archivo del compromiso no puede estar vacío.");
        }

        // La guarda evita materializar en memoria un archivo que el validador
        // rechazará de todos modos.
        if (archivo.Length > CargarCompromisoBecarioValidator.TamanioMaximoBytes)
        {
            throw new ArchivoCompromisoInvalidoException(
                "El archivo del compromiso no puede superar 5 MB.");
        }

        byte[] contenido;

        await using (var memoria = new MemoryStream((int)archivo.Length))
        {
            await archivo.CopyToAsync(memoria, cancellationToken);
            contenido = memoria.ToArray();
        }

        var resultado = await _comandos.EnviarAsync<
            CargarCompromisoBecarioCommand,
            CompromisoCargadoDto>(
            new CargarCompromisoBecarioCommand(
                becaId,
                archivo.FileName,
                archivo.ContentType,
                contenido),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    /// <summary>
    /// Aprueba o rechaza un compromiso en revisión cuando el coordinador tiene
    /// segundo factor y alcance sobre el programa de la beca.
    /// </summary>
    [HttpPost("compromisos/{compromisoId:guid}/revision")]
    [Authorize(Roles = PoliticasSeguridad.RolesAdministrativos)]
    [Authorize(Policy = PoliticasSeguridad.Requiere2FA)]
    [ProducesResponseType(typeof(CompromisoRevisadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CompromisoRevisadoDto>> Revisar(
        Guid compromisoId,
        [FromBody] RevisarCompromisoRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var recurso = await ObtenerRecursoCompromisoAsync(
            compromisoId,
            cancellationToken);

        await ValidarAlcanceProgramaAsync(
            recurso.ProgramaBecaId,
            cancellationToken);

        var resultado = await _comandos.EnviarAsync<
            RevisarCompromisoBecarioCommand,
            CompromisoRevisadoDto>(
            new RevisarCompromisoBecarioCommand(
                compromisoId,
                solicitud.Aprobar,
                solicitud.Observaciones),
            cancellationToken);

        return Ok(resultado);
    }

    private async Task<RecursoBecaDto> ObtenerRecursoBecaAsync(
        Guid becaId,
        CancellationToken cancellationToken)
        => await _consultasBeca.ObtenerRecursoBecaAsync(becaId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("beca", becaId);

    private async Task<RecursoCompromisoDto> ObtenerRecursoCompromisoAsync(
        Guid compromisoId,
        CancellationToken cancellationToken)
        => await _consultasBeca.ObtenerRecursoCompromisoAsync(
                compromisoId,
                cancellationToken)
            ?? throw new RecursoNoEncontradoException("compromiso", compromisoId);

    private async Task ValidarPropiedadAsync(
        RecursoBecaDto recurso,
        CancellationToken cancellationToken)
    {
        var resultado = await _autorizacion.AuthorizeAsync(
            User,
            new RecursoAutorizable(PostulanteId: recurso.PostulanteEstudianteId),
            PoliticasSeguridad.PropiedadPostulante);

        if (!resultado.Succeeded)
        {
            throw new OperacionNoPermitidaException(
                "El usuario no es dueño de la beca.");
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
                "El usuario no tiene alcance sobre el programa de la beca.");
        }
    }

    /// <summary>
    /// Formulario multipart con el PDF del compromiso firmado.
    /// </summary>
    public sealed record CargarCompromisoRequest(IFormFile? Archivo);

    /// <summary>
    /// Cuerpo de la solicitud de revisión.
    /// </summary>
    public sealed record RevisarCompromisoRequest(
        bool Aprobar,
        [StringLength(
            1000,
            ErrorMessage = "Las observaciones no pueden superar 1000 caracteres.")]
        string? Observaciones);
}
