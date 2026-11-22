using Ascenso.Becas.Api.Seguridad;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerBeca;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Becas.Api.Controllers.v1;

/// <summary>
/// Expone la consulta de una beca para su postulante dueño o para un
/// coordinador con alcance sobre el programa.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/becas")]
public sealed class BecasController : ControllerBase
{
    private readonly IQueryDispatcher _consultas;
    private readonly IConsultasBeca _consultasBeca;
    private readonly IAuthorizationService _autorizacion;
    private readonly IAlmacenamientoArchivos _almacenamiento;

    public BecasController(
        IQueryDispatcher consultas,
        IConsultasBeca consultasBeca,
        IAuthorizationService autorizacion,
        IAlmacenamientoArchivos almacenamiento)
    {
        _consultas = consultas ?? throw new ArgumentNullException(nameof(consultas));
        _consultasBeca = consultasBeca ?? throw new ArgumentNullException(nameof(consultasBeca));
        _autorizacion = autorizacion ?? throw new ArgumentNullException(nameof(autorizacion));
        _almacenamiento = almacenamiento ?? throw new ArgumentNullException(nameof(almacenamiento));
    }

    /// <summary>
    /// Consulta una beca cuando el usuario es el postulante dueño o un
    /// coordinador con alcance sobre el programa.
    /// </summary>
    [HttpGet("{becaId:guid}")]
    [ProducesResponseType(typeof(BecaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BecaDto>> Obtener(
        Guid becaId,
        CancellationToken cancellationToken = default)
    {
        var recurso = await ObtenerRecursoAsync(becaId, cancellationToken);

        if (!await PuedeConsultarAsync(recurso, cancellationToken))
        {
            throw new OperacionNoPermitidaException(
                "El usuario no es dueño de la beca ni tiene alcance sobre su programa.");
        }

        var resultado = await _consultas.ConsultarAsync<ObtenerBecaQuery, BecaDto>(
            new ObtenerBecaQuery(becaId),
            cancellationToken);

        var oferta = resultado.Oferta is null
            ? null
            : resultado.Oferta with
            {
                UrlCartaPdf = await _almacenamiento.ResolverUrlLecturaAsync(
                    resultado.Oferta.UrlCartaPdf,
                    cancellationToken)
            };
        var compromiso = resultado.Compromiso is null
            ? null
            : resultado.Compromiso with
            {
                UrlPlantillaGenerada = await ResolverUrlOpcionalAsync(
                    resultado.Compromiso.UrlPlantillaGenerada,
                    cancellationToken),
                UrlPdfFirmado = await ResolverUrlOpcionalAsync(
                    resultado.Compromiso.UrlPdfFirmado,
                    cancellationToken)
            };

        return Ok(resultado with
        {
            Oferta = oferta,
            Compromiso = compromiso
        });
    }

    private async Task<string?> ResolverUrlOpcionalAsync(
        string? referencia,
        CancellationToken cancellationToken)
        => string.IsNullOrWhiteSpace(referencia)
            ? referencia
            : await _almacenamiento.ResolverUrlLecturaAsync(
                referencia,
                cancellationToken);

    private async Task<RecursoBecaDto> ObtenerRecursoAsync(
        Guid becaId,
        CancellationToken cancellationToken)
        => await _consultasBeca.ObtenerRecursoBecaAsync(becaId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("beca", becaId);

    private async Task<bool> PuedeConsultarAsync(
        RecursoBecaDto recurso,
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
}
