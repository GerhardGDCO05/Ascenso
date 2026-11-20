using Ascenso.Postulaciones.Application.Postulantes.Comandos.ActualizarPerfilSocioeconomico;
using Ascenso.Postulaciones.Application.Postulantes.Comandos.RegistrarPostulante;
using Ascenso.Postulaciones.Application.Postulantes.Consultas.ObtenerMiPerfil;
using Ascenso.Postulaciones.Application.Postulantes.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Postulaciones.Api.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1/postulantes")]
public sealed class PostulantesController : ControllerBase
{
    private readonly RegistrarPostulanteCommandHandler _registrarHandler;
    private readonly ObtenerMiPerfilQueryHandler _obtenerMiPerfilHandler;
    private readonly ActualizarPerfilSocioeconomicoCommandHandler _actualizarPerfilHandler;

    public PostulantesController(
        RegistrarPostulanteCommandHandler registrarHandler,
        ObtenerMiPerfilQueryHandler obtenerMiPerfilHandler,
        ActualizarPerfilSocioeconomicoCommandHandler actualizarPerfilHandler)
    {
        _registrarHandler = registrarHandler ?? throw new ArgumentNullException(nameof(registrarHandler));
        _obtenerMiPerfilHandler = obtenerMiPerfilHandler ?? throw new ArgumentNullException(nameof(obtenerMiPerfilHandler));
        _actualizarPerfilHandler = actualizarPerfilHandler ?? throw new ArgumentNullException(nameof(actualizarPerfilHandler));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PostulanteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PostulanteDto>> Registrar(
        [FromBody] RegistrarPostulanteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new RegistrarPostulanteCommand(
            request.NumeroDocumentoIdentidad,
            request.Nombres,
            request.Apellidos,
            new DatosContactoDto(
                request.DatosContacto.CorreoElectronico,
                request.DatosContacto.Telefono),
            request.UniversidadAliadaId,
            request.CarreraId,
            request.PeriodoAcademicoId,
            request.CondicionAcademica,
            request.AceptaTratamientoDatos);

        var resultado = await _registrarHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(nameof(ObtenerMiPerfil), resultado);
    }

    [HttpGet("mio")]
    [ProducesResponseType(typeof(PostulanteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostulanteDto>> ObtenerMiPerfil(
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerMiPerfilHandler.HandleAsync(
            new ObtenerMiPerfilQuery(),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpPut("mio/socioeconomico")]
    [ProducesResponseType(typeof(PostulanteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PostulanteDto>> ActualizarPerfilSocioeconomico(
        [FromBody] ActualizarPerfilSocioeconomicoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new ActualizarPerfilSocioeconomicoCommand(
            request.EstadoCivil,
            request.PromedioActual,
            request.Trabaja,
            request.Ocupacion,
            request.IngresoMensualPostulante,
            request.MiembrosFamiliares?
                .Select(miembro => new DatosMiembroFamiliarCommand(
                    miembro.Parentesco,
                    miembro.Edad,
                    miembro.Ocupacion,
                    miembro.IngresoMensual))
                .ToArray()!);

        var resultado = await _actualizarPerfilHandler.HandleAsync(comando, cancellationToken);

        return Ok(resultado);
    }

    public sealed record class RegistrarPostulanteRequest(
        string NumeroDocumentoIdentidad,
        string Nombres,
        string Apellidos,
        DatosContactoRequest DatosContacto,
        Guid UniversidadAliadaId,
        Guid CarreraId,
        Guid PeriodoAcademicoId,
        string CondicionAcademica,
        bool AceptaTratamientoDatos);

    public sealed record class DatosContactoRequest(
        string CorreoElectronico,
        string Telefono);

    public sealed record class ActualizarPerfilSocioeconomicoRequest(
        string EstadoCivil,
        decimal? PromedioActual,
        bool? Trabaja,
        string Ocupacion,
        decimal? IngresoMensualPostulante,
        IReadOnlyList<DatosMiembroFamiliarRequest>? MiembrosFamiliares);

    public sealed record class DatosMiembroFamiliarRequest(
        string Parentesco,
        int? Edad,
        string Ocupacion,
        decimal? IngresoMensual);
}
