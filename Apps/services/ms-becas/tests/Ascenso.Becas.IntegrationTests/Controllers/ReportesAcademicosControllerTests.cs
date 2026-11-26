using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.SeguimientoAcademico.Comandos.EvaluarPermanencia;
using Ascenso.Becas.Application.SeguimientoAcademico.Comandos.RegistrarReporteAcademico;
using Ascenso.Becas.Application.SeguimientoAcademico.Dtos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.Datos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Controllers;

public sealed class ReportesAcademicosControllerTests : IClassFixture<OfertasBecaApiFactory>
{
    private static readonly byte[] _pdf = Encoding.UTF8.GetBytes("%PDF-1.7\nnotas");
    private readonly OfertasBecaApiFactory _base;

    public ReportesAcademicosControllerTests(OfertasBecaApiFactory fabrica)
        => _base = fabrica;

    [Fact]
    public async Task BecarioPropietarioCargaPdfYRecibe201()
    {
        var becaId = Guid.CreateVersion7();
        var postulanteId = Guid.CreateVersion7();
        var resultado = CrearReporte(becaId);
        var comandos = Substitute.For<ICommandDispatcher>();
        comandos.EnviarAsync<RegistrarReporteAcademicoCommand, ReporteAcademicoDto>(
            Arg.Any<RegistrarReporteAcademicoCommand>(), Arg.Any<CancellationToken>())
            .Returns(ReporteAcademicoDto.Desde(resultado));
        var archivos = CrearAlmacen();
        using var fabrica = CrearFabrica(becaId, postulanteId, comandos, archivos);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, "BECARIO", postulanteId: postulanteId);
        using var formulario = CrearFormulario();

        var respuesta = await cliente.PostAsync(Ruta(becaId), formulario);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        await comandos.Received(1).EnviarAsync<
            RegistrarReporteAcademicoCommand, ReporteAcademicoDto>(
            Arg.Is<RegistrarReporteAcademicoCommand>(comando =>
                comando.BecaId == becaId
                && comando.FuenteEntrega == FuenteReporte.Becario
                && comando.HashComprobanteSha256 == Convert.ToHexString(SHA256.HashData(_pdf))
                && comando.TamanioComprobanteBytes == _pdf.Length),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HashIncorrectoResponde400SinSubirArchivo()
    {
        var becaId = Guid.CreateVersion7();
        var postulanteId = Guid.CreateVersion7();
        var archivos = CrearAlmacen();
        using var fabrica = CrearFabrica(becaId, postulanteId,
            Substitute.For<ICommandDispatcher>(), archivos);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, "BECARIO", postulanteId: postulanteId);
        using var formulario = CrearFormulario(hash: new string('0', 64));

        var respuesta = await cliente.PostAsync(Ruta(becaId), formulario);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        await archivos.DidNotReceive().SubirAsync(Arg.Any<Stream>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RechazaArchivoMayorA5MbAntesDeSubirlo()
    {
        var becaId = Guid.CreateVersion7();
        var postulanteId = Guid.CreateVersion7();
        var archivos = CrearAlmacen();
        using var fabrica = CrearFabrica(becaId, postulanteId,
            Substitute.For<ICommandDispatcher>(), archivos);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, "BECARIO", postulanteId: postulanteId);
        using var formulario = CrearFormulario(contenido: new byte[5 * 1024 * 1024 + 1]);

        var respuesta = await cliente.PostAsync(Ruta(becaId), formulario);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        await archivos.DidNotReceive().SubirAsync(Arg.Any<Stream>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RechazaArchivoSinFirmaPdf()
    {
        var becaId = Guid.CreateVersion7();
        var postulanteId = Guid.CreateVersion7();
        var archivos = CrearAlmacen();
        using var fabrica = CrearFabrica(becaId, postulanteId,
            Substitute.For<ICommandDispatcher>(), archivos);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, "BECARIO", postulanteId: postulanteId);
        using var formulario = CrearFormulario(contenido: Encoding.UTF8.GetBytes("no es pdf"));

        var respuesta = await cliente.PostAsync(Ruta(becaId), formulario);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        await archivos.DidNotReceive().SubirAsync(Arg.Any<Stream>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BecarioAjenoYUniversidadSinVinculoResponden403()
    {
        var becaId = Guid.CreateVersion7();
        using var fabrica = CrearFabrica(becaId, Guid.CreateVersion7(),
            Substitute.For<ICommandDispatcher>(), CrearAlmacen());
        using var becario = fabrica.CreateClient();
        Autenticar(becario, "BECARIO", postulanteId: Guid.CreateVersion7());
        using var formularioBecario = CrearFormulario();
        var respuestaBecario = await becario.PostAsync(Ruta(becaId), formularioBecario);

        using var universidad = fabrica.CreateClient();
        Autenticar(universidad, "ENLACE_UNIVERSIDAD");
        using var formularioUniversidad = CrearFormulario();
        var respuestaUniversidad = await universidad.PostAsync(
            Ruta(becaId), formularioUniversidad);

        Assert.Equal(HttpStatusCode.Forbidden, respuestaBecario.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, respuestaUniversidad.StatusCode);
    }

    [Fact]
    public async Task CoordinadorConAlcanceY2FaEvaluaCon200()
    {
        var becaId = Guid.CreateVersion7();
        var reporte = CrearReporte(becaId);
        var evaluacion = new EvaluacionPermanenciaDto(Guid.CreateVersion7(),
            reporte.Id, becaId, DateTimeOffset.UtcNow, true, [], null, null,
            EstadoConsecuencia.PendienteConfirmacion, null, null, null);
        var comandos = Substitute.For<ICommandDispatcher>();
        comandos.EnviarAsync<EvaluarPermanenciaCommand, EvaluacionPermanenciaDto>(
            Arg.Any<EvaluarPermanenciaCommand>(), Arg.Any<CancellationToken>())
            .Returns(evaluacion);
        using var fabrica = CrearFabrica(becaId, Guid.CreateVersion7(),
            comandos, CrearAlmacen(), reporte);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, "COORDINADOR_PROGRAMA", programaId: _programaId,
            con2Fa: true);

        var respuesta = await cliente.PostAsync(
            $"{Ruta(becaId)}/{reporte.Id}/evaluaciones", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await comandos.Received(1).EnviarAsync<
            EvaluarPermanenciaCommand, EvaluacionPermanenciaDto>(
            Arg.Is<EvaluarPermanenciaCommand>(comando =>
                comando.ReporteAcademicoId == reporte.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluacionRepetidaResponde409()
    {
        var becaId = Guid.CreateVersion7();
        var reporte = CrearReporte(becaId);
        var comandos = Substitute.For<ICommandDispatcher>();
        comandos.EnviarAsync<EvaluarPermanenciaCommand, EvaluacionPermanenciaDto>(
            Arg.Any<EvaluarPermanenciaCommand>(), Arg.Any<CancellationToken>())
            .Returns<EvaluacionPermanenciaDto>(_ =>
                throw new ConflictoOperacionException("El reporte ya fue evaluado."));
        using var fabrica = CrearFabrica(becaId, Guid.CreateVersion7(),
            comandos, CrearAlmacen(), reporte);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, "COORDINADOR_PROGRAMA", programaId: _programaId,
            con2Fa: true);

        var respuesta = await cliente.PostAsync(
            $"{Ruta(becaId)}/{reporte.Id}/evaluaciones", null);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task CoordinadorSinAlcanceNoPuedeEvaluar()
    {
        var becaId = Guid.CreateVersion7();
        var reporte = CrearReporte(becaId);
        var comandos = Substitute.For<ICommandDispatcher>();
        using var fabrica = CrearFabrica(becaId, Guid.CreateVersion7(),
            comandos, CrearAlmacen(), reporte);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, "COORDINADOR_PROGRAMA",
            programaId: Guid.CreateVersion7(), con2Fa: true);

        var respuesta = await cliente.PostAsync(
            $"{Ruta(becaId)}/{reporte.Id}/evaluaciones", null);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        await comandos.DidNotReceive().EnviarAsync<
            EvaluarPermanenciaCommand, EvaluacionPermanenciaDto>(
            Arg.Any<EvaluarPermanenciaCommand>(), Arg.Any<CancellationToken>());
    }

    private static readonly Guid _programaId = Guid.CreateVersion7();

    private WebApplicationFactory<Program> CrearFabrica(
        Guid becaId, Guid postulanteId, ICommandDispatcher comandos,
        IAlmacenamientoArchivos archivos, ReporteAcademico? reporte = null)
        => _base.WithWebHostBuilder(builder => builder.ConfigureTestServices(servicios =>
        {
            var becas = Substitute.For<IConsultasBeca>();
            becas.ObtenerRecursoBecaAsync(becaId, Arg.Any<CancellationToken>())
                .Returns(new RecursoBecaDto(becaId, _programaId, postulanteId));
            var reportes = Substitute.For<IReporteAcademicoRepository>();
            if (reporte is not null)
                reportes.ObtenerParaConsultaAsync(reporte.Id, Arg.Any<CancellationToken>())
                    .Returns(reporte);
            servicios.RemoveAll<ICommandDispatcher>();
            servicios.AddSingleton(comandos);
            servicios.RemoveAll<IConsultasBeca>();
            servicios.AddSingleton(becas);
            servicios.RemoveAll<IReporteAcademicoRepository>();
            servicios.AddSingleton(reportes);
            servicios.RemoveAll<IAlmacenamientoArchivos>();
            servicios.AddSingleton(archivos);
        }));

    private static IAlmacenamientoArchivos CrearAlmacen()
    {
        var archivos = Substitute.For<IAlmacenamientoArchivos>();
        archivos.SubirAsync(Arg.Any<Stream>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ArchivoAlmacenado("gs://becas/reportes/notas.pdf",
                Convert.ToHexString(SHA256.HashData(_pdf)), _pdf.Length));
        return archivos;
    }

    private static MultipartFormDataContent CrearFormulario(
        string? hash = null, byte[]? contenido = null)
    {
        contenido ??= _pdf;
        var formulario = new MultipartFormDataContent();
        formulario.Add(new StringContent(Guid.CreateVersion7().ToString()), "periodoAcademicoId");
        formulario.Add(new StringContent("17"), "promedioPeriodo");
        formulario.Add(new StringContent("5"), "materiasAprobadas");
        formulario.Add(new StringContent("0"), "materiasReprobadas");
        formulario.Add(new StringContent("0"), "materiasCursando");
        formulario.Add(new StringContent("20"), "creditosAprobados");
        formulario.Add(new StringContent("MATRICULADO"), "estadoMatricula");
        formulario.Add(new StringContent(Guid.CreateVersion7().ToString()), "universidadAliadaId");
        formulario.Add(new StringContent(hash ?? Convert.ToHexString(SHA256.HashData(contenido))),
            "hashSha256");
        var archivo = new ByteArrayContent(contenido);
        archivo.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        formulario.Add(archivo, "archivo", "notas.pdf");
        return formulario;
    }

    private static void Autenticar(HttpClient cliente, string rol,
        Guid? postulanteId = null, Guid? programaId = null, bool con2Fa = false)
    {
        cliente.DefaultRequestHeaders.Add("X-Test-Roles", rol);
        if (postulanteId.HasValue)
            cliente.DefaultRequestHeaders.Add("X-Test-Postulante", postulanteId.Value.ToString());
        if (programaId.HasValue)
            cliente.DefaultRequestHeaders.Add("X-Test-Programas", programaId.Value.ToString());
        if (con2Fa)
            cliente.DefaultRequestHeaders.Add("X-Test-Acr", "urn:ascenso:loa:2fa");
        cliente.DefaultRequestHeaders.Add("Idempotency-Key", Guid.CreateVersion7().ToString());
    }

    private static string Ruta(Guid becaId)
        => $"/api/v1/becas/{becaId}/reportes-academicos";

    private static ReporteAcademico CrearReporte(Guid becaId)
        => ReporteAcademico.Registrar(new DatosReporteAcademico(
            becaId, Guid.CreateVersion7(), 17m, 5, 0, 0, 20,
            EstadoMatricula.Matriculado, "gs://becas/reportes/notas.pdf",
            new string('a', 64), 1024, "application/pdf", FuenteReporte.Becario,
            Guid.CreateVersion7(), new DateOnly(2026, 11, 25)),
            new ContextoOperacion(Guid.CreateVersion7(), Guid.CreateVersion7(),
                DateTimeOffset.UtcNow));
}
