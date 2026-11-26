using System.Security.Cryptography;
using Ascenso.Becas.Api.Seguridad;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.SeguimientoAcademico.Comandos.EvaluarPermanencia;
using Ascenso.Becas.Application.SeguimientoAcademico.Comandos.RegistrarReporteAcademico;
using Ascenso.Becas.Application.SeguimientoAcademico.Consultas.ObtenerEvaluacionPermanencia;
using Ascenso.Becas.Application.SeguimientoAcademico.Consultas.ObtenerReporteAcademico;
using Ascenso.Becas.Application.SeguimientoAcademico.Dtos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Becas.Api.Controllers.v1;

/// <summary>Entrega y evaluación de reportes académicos por periodo.</summary>
[ApiController]
[Authorize]
[Route("api/v1/becas/{becaId:guid}/reportes-academicos")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
public sealed class ReportesAcademicosController : ControllerBase
{
    private const int TamanioMaximoPdf = 5 * 1024 * 1024;
    private readonly ICommandDispatcher _comandos;
    private readonly IQueryDispatcher _consultas;
    private readonly IConsultasBeca _becas;
    private readonly IReporteAcademicoRepository _reportes;
    private readonly IAlmacenamientoArchivos _archivos;
    private readonly IAuthorizationService _autorizacion;
    private readonly IUniversidadBecaAutorizacion _universidades;

    public ReportesAcademicosController(
        ICommandDispatcher comandos, IQueryDispatcher consultas,
        IConsultasBeca becas, IReporteAcademicoRepository reportes,
        IAlmacenamientoArchivos archivos, IAuthorizationService autorizacion,
        IUniversidadBecaAutorizacion universidades)
    {
        _comandos = comandos ?? throw new ArgumentNullException(nameof(comandos));
        _consultas = consultas ?? throw new ArgumentNullException(nameof(consultas));
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _reportes = reportes ?? throw new ArgumentNullException(nameof(reportes));
        _archivos = archivos ?? throw new ArgumentNullException(nameof(archivos));
        _autorizacion = autorizacion ?? throw new ArgumentNullException(nameof(autorizacion));
        _universidades = universidades ?? throw new ArgumentNullException(nameof(universidades));
    }

    [HttpPost]
    [Authorize(Roles = PoliticasSeguridad.RolBecario + "," + PoliticasSeguridad.RolEnlaceUniversidad)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ReporteAcademicoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ReporteAcademicoDto>> Registrar(
        Guid becaId, [FromForm] RegistrarReporteRequest solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        var recurso = await ObtenerRecursoBecaAsync(becaId, cancellationToken);
        var fuente = await AutorizarEntregaAsync(recurso,
            solicitud.UniversidadAliadaId, cancellationToken);
        var archivo = solicitud.Archivo;
        if (archivo is null || archivo.Length is <= 0 or > TamanioMaximoPdf
            || !string.Equals(archivo.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetExtension(archivo.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new SolicitudInvalidaException("El comprobante debe ser un PDF de hasta 5 MB.");

        byte[] contenido;
        await using (var memoria = new MemoryStream((int)archivo.Length))
        {
            await archivo.CopyToAsync(memoria, cancellationToken);
            contenido = memoria.ToArray();
        }
        if (contenido.Length is <= 0 or > TamanioMaximoPdf
            || !contenido.AsSpan().StartsWith("%PDF-"u8))
            throw new SolicitudInvalidaException("El contenido no corresponde a un PDF válido.");

        var hash = Convert.ToHexString(SHA256.HashData(contenido));
        if (solicitud.HashSha256?.Length != 64
            || !string.Equals(solicitud.HashSha256, hash, StringComparison.OrdinalIgnoreCase))
            throw new SolicitudInvalidaException("El hash SHA-256 del PDF no coincide.");

        ArchivoAlmacenado almacenado;
        try
        {
            await using var flujo = new MemoryStream(contenido, writable: false);
            almacenado = await _archivos.SubirAsync(flujo, "application/pdf",
                $"becas/{becaId:N}/reportes-academicos/{Guid.CreateVersion7():N}.pdf",
                cancellationToken);
        }
        catch (Exception excepcion) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DependenciaNoDisponibleException("almacenamiento", excepcion);
        }

        if (string.IsNullOrWhiteSpace(almacenado.Url)
            || almacenado.TamanioBytes != contenido.Length
            || !string.Equals(almacenado.HashSha256, hash, StringComparison.OrdinalIgnoreCase))
        {
            await EliminarArchivoSilenciosoAsync(almacenado.Url);
            throw new DependenciaNoDisponibleException("almacenamiento");
        }

        try
        {
            var resultado = await _comandos.EnviarAsync<
                RegistrarReporteAcademicoCommand, ReporteAcademicoDto>(
                new RegistrarReporteAcademicoCommand(becaId,
                    solicitud.PeriodoAcademicoId, solicitud.PromedioPeriodo,
                    solicitud.MateriasAprobadas, solicitud.MateriasReprobadas,
                    solicitud.MateriasCursando, solicitud.CreditosAprobados,
                    solicitud.EstadoMatricula, almacenado.Url, hash,
                    contenido.Length, "application/pdf", fuente,
                    solicitud.UniversidadAliadaId, solicitud.HorasServicioCumplidas,
                    solicitud.UrlComprobanteServicio,
                    solicitud.HashComprobanteServicioSha256), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, resultado);
        }
        catch
        {
            await EliminarArchivoSilenciosoAsync(almacenado.Url);
            throw;
        }
    }

    [HttpPost("{reporteId:guid}/evaluaciones")]
    [Authorize(Roles = PoliticasSeguridad.RolCoordinadorPrograma)]
    [Authorize(Policy = PoliticasSeguridad.Requiere2FA)]
    [ProducesResponseType(typeof(EvaluacionPermanenciaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EvaluacionPermanenciaDto>> Evaluar(
        Guid becaId, Guid reporteId, CancellationToken cancellationToken = default)
    {
        await AutorizarCoordinadorAsync(becaId, reporteId, cancellationToken);
        var resultado = await _comandos.EnviarAsync<
            EvaluarPermanenciaCommand, EvaluacionPermanenciaDto>(
            new EvaluarPermanenciaCommand(reporteId), cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("{reporteId:guid}")]
    [ProducesResponseType(typeof(ReporteAcademicoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReporteAcademicoDto>> Obtener(
        Guid becaId, Guid reporteId, CancellationToken cancellationToken = default)
    {
        await AutorizarLecturaAsync(becaId, reporteId, cancellationToken);
        var reporte = await _consultas.ConsultarAsync<
            ObtenerReporteAcademicoQuery, ReporteAcademicoDto>(
            new ObtenerReporteAcademicoQuery(reporteId), cancellationToken);
        return Ok(reporte with
        {
            UrlComprobanteNotasFirebase = await _archivos.ResolverUrlLecturaAsync(
                reporte.UrlComprobanteNotasFirebase, cancellationToken)
        });
    }

    [HttpGet("{reporteId:guid}/evaluacion")]
    [ProducesResponseType(typeof(EvaluacionPermanenciaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EvaluacionPermanenciaDto>> ObtenerEvaluacion(
        Guid becaId, Guid reporteId, CancellationToken cancellationToken = default)
    {
        var reporte = await AutorizarLecturaAsync(becaId, reporteId, cancellationToken);
        var evaluacionId = reporte.Evaluacion?.Id
            ?? throw new RecursoNoEncontradoException("evaluación de permanencia", reporteId);
        var resultado = await _consultas.ConsultarAsync<
            ObtenerEvaluacionPermanenciaQuery, EvaluacionPermanenciaDto>(
            new ObtenerEvaluacionPermanenciaQuery(evaluacionId), cancellationToken);
        return Ok(resultado);
    }

    private async Task<FuenteReporte> AutorizarEntregaAsync(
        RecursoBecaDto beca, Guid universidadId, CancellationToken token)
    {
        if (User.IsInRole(PoliticasSeguridad.RolBecario)
            && (await _autorizacion.AuthorizeAsync(User,
                new RecursoAutorizable(PostulanteId: beca.PostulanteEstudianteId),
                PoliticasSeguridad.PropiedadPostulante)).Succeeded)
            return FuenteReporte.Becario;

        if (User.IsInRole(PoliticasSeguridad.RolEnlaceUniversidad)
            && await _universidades.PuedeEntregarAsync(User, beca.BecaId, universidadId, token))
            return FuenteReporte.Universidad;

        throw new OperacionNoPermitidaException(
            "El usuario no puede entregar reportes para esta beca.");
    }

    private async Task<ReporteAcademicoDto> AutorizarLecturaAsync(
        Guid becaId, Guid reporteId, CancellationToken token)
    {
        var beca = await ObtenerRecursoBecaAsync(becaId, token);
        var reporte = await _reportes.ObtenerParaConsultaAsync(reporteId, token)
            ?? throw new RecursoNoEncontradoException("reporte académico", reporteId);
        if (reporte.BecaId != becaId)
            throw new RecursoNoEncontradoException("reporte académico", reporteId);

        var propiedad = User.IsInRole(PoliticasSeguridad.RolBecario)
            && (await _autorizacion.AuthorizeAsync(User,
                new RecursoAutorizable(PostulanteId: beca.PostulanteEstudianteId),
                PoliticasSeguridad.PropiedadPostulante)).Succeeded;
        var alcance = User.IsInRole(PoliticasSeguridad.RolCoordinadorPrograma)
            && (await _autorizacion.AuthorizeAsync(User,
                new RecursoAutorizable(ProgramaId: beca.ProgramaBecaId),
                PoliticasSeguridad.AlcancePrograma)).Succeeded;
        if (!propiedad && !alcance)
            throw new OperacionNoPermitidaException("Sin acceso al reporte académico.");

        return ReporteAcademicoDto.Desde(reporte);
    }

    private async Task AutorizarCoordinadorAsync(
        Guid becaId, Guid reporteId, CancellationToken token)
    {
        var beca = await ObtenerRecursoBecaAsync(becaId, token);
        var reporte = await _reportes.ObtenerParaConsultaAsync(reporteId, token)
            ?? throw new RecursoNoEncontradoException("reporte académico", reporteId);
        if (reporte.BecaId != becaId)
            throw new RecursoNoEncontradoException("reporte académico", reporteId);
        var alcance = await _autorizacion.AuthorizeAsync(User,
            new RecursoAutorizable(ProgramaId: beca.ProgramaBecaId),
            PoliticasSeguridad.AlcancePrograma);
        if (!alcance.Succeeded)
            throw new OperacionNoPermitidaException("Sin alcance sobre el programa de la beca.");
    }

    private async Task<RecursoBecaDto> ObtenerRecursoBecaAsync(Guid becaId, CancellationToken token)
        => await _becas.ObtenerRecursoBecaAsync(becaId, token)
            ?? throw new RecursoNoEncontradoException("beca", becaId);

    private async Task EliminarArchivoSilenciosoAsync(string referencia)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(referencia))
                await _archivos.EliminarAsync(referencia, CancellationToken.None);
        }
        catch
        {
            // La compensación no oculta la causa original.
        }
    }

    public sealed record RegistrarReporteRequest(
        Guid PeriodoAcademicoId, decimal PromedioPeriodo,
        int MateriasAprobadas, int MateriasReprobadas, int MateriasCursando,
        int CreditosAprobados, EstadoMatricula EstadoMatricula,
        Guid UniversidadAliadaId, IFormFile? Archivo, string? HashSha256,
        int? HorasServicioCumplidas, string? UrlComprobanteServicio,
        string? HashComprobanteServicioSha256);
}
