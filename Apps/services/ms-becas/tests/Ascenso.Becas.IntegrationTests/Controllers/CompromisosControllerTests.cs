using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Comandos.CargarCompromisoBecario;
using Ascenso.Becas.Application.Otorgamiento.Comandos.RevisarCompromisoBecario;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Controllers;

public sealed class CompromisosControllerTests : IClassFixture<OfertasBecaApiFactory>
{
    private const string EncabezadoIdempotencia = "Idempotency-Key";
    private const string EncabezadoRoles = "X-Test-Roles";
    private const string EncabezadoPostulante = "X-Test-Postulante";
    private const string EncabezadoProgramas = "X-Test-Programas";
    private const string EncabezadoAcr = "X-Test-Acr";

    private static readonly byte[] _pdfValido =
        Encoding.UTF8.GetBytes("%PDF-1.7\ncontenido del compromiso firmado");

    private readonly OfertasBecaApiFactory _fabricaBase;

    public CompromisosControllerTests(OfertasBecaApiFactory fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_ExponerLasRutasDeCompromisos_ConLosCodigosDeRespuesta_EnOpenApi()
    {
        using var cliente = _fabricaBase.CreateClient();

        var respuesta = await cliente.GetAsync("/openapi/v1.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        var rutas = documento.RootElement.GetProperty("paths");

        VerificarOperacion(
            rutas,
            "/api/v1/becas/{becaId}/compromisos",
            "post",
            "201",
            "400",
            "401",
            "403",
            "404",
            "409",
            "503");

        VerificarOperacion(
            rutas,
            "/api/v1/becas/compromisos/{compromisoId}/revision",
            "post",
            "200",
            "400",
            "401",
            "403",
            "404",
            "409");
    }

    [Fact]
    public async Task Debe_CargarElCompromiso_Cuando_ElBecarioEsDueno()
    {
        var becaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var postulanteId = Guid.NewGuid();
        var resultadoEsperado = new CompromisoCargadoDto(
            Guid.NewGuid(),
            1,
            "EN_REVISION");

        var comandos = Substitute.For<ICommandDispatcher>();
        comandos
            .EnviarAsync<CargarCompromisoBecarioCommand, CompromisoCargadoDto>(
                Arg.Any<CargarCompromisoBecarioCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(resultadoEsperado));

        var consultasBeca = CrearConsultasBeca(becaId, programaId, postulanteId);

        using var fabrica = CrearFabrica(
            comandos: comandos,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoBecario(cliente, postulanteId);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-compromiso-carga-0001");

        using var formulario = CrearFormulario(_pdfValido);
        var respuesta = await cliente.PostAsync(
            $"/api/v1/becas/{becaId}/compromisos",
            formulario);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<CompromisoCargadoDto>();
        cuerpo.Should().BeEquivalentTo(resultadoEsperado);

        await comandos.Received(1).EnviarAsync<
            CargarCompromisoBecarioCommand,
            CompromisoCargadoDto>(
            Arg.Is<CargarCompromisoBecarioCommand>(comando =>
                comando.BecaId == becaId
                && comando.NombreArchivo == "compromiso.pdf"
                && comando.TipoMime == "application/pdf"
                && comando.Contenido.SequenceEqual(_pdfValido)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Responder403_Cuando_ElBecarioCargaEnUnaBecaAjena()
    {
        var becaId = Guid.NewGuid();
        var consultasBeca = CrearConsultasBeca(
            becaId,
            Guid.NewGuid(),
            Guid.NewGuid());
        var comandos = Substitute.For<ICommandDispatcher>();

        using var fabrica = CrearFabrica(
            comandos: comandos,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoBecario(cliente, Guid.NewGuid());
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-compromiso-carga-0002");

        using var formulario = CrearFormulario(_pdfValido);
        var respuesta = await cliente.PostAsync(
            $"/api/v1/becas/{becaId}/compromisos",
            formulario);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await comandos.DidNotReceive().EnviarAsync<
            CargarCompromisoBecarioCommand,
            CompromisoCargadoDto>(
            Arg.Any<CargarCompromisoBecarioCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Responder404_Cuando_LaBecaDelCompromisoNoExiste()
    {
        var comandos = Substitute.For<ICommandDispatcher>();

        using var fabrica = CrearFabrica(comandos: comandos);
        using var cliente = fabrica.CreateClient();
        AutenticarComoBecario(cliente, Guid.NewGuid());
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-compromiso-carga-0003");

        using var formulario = CrearFormulario(_pdfValido);
        var respuesta = await cliente.PostAsync(
            $"/api/v1/becas/{Guid.NewGuid()}/compromisos",
            formulario);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await comandos.DidNotReceive().EnviarAsync<
            CargarCompromisoBecarioCommand,
            CompromisoCargadoDto>(
            Arg.Any<CargarCompromisoBecarioCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Responder400_Cuando_ElArchivoNoEsPdf()
    {
        var becaId = Guid.NewGuid();
        var postulanteId = Guid.NewGuid();
        var consultasBeca = CrearConsultasBeca(
            becaId,
            Guid.NewGuid(),
            postulanteId);

        using var fabrica = CrearFabrica(
            consultasBeca: consultasBeca,
            usarDespachadorReal: true);
        using var cliente = fabrica.CreateClient();
        AutenticarComoBecario(cliente, postulanteId);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-compromiso-carga-0004");

        using var formulario = CrearFormulario(
            Encoding.UTF8.GetBytes("no es un pdf"),
            "compromiso.txt",
            "text/plain");
        var respuesta = await cliente.PostAsync(
            $"/api/v1/becas/{becaId}/compromisos",
            formulario);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var problema = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        problema.RootElement.GetProperty("code").GetString()
            .Should().Be("SOLICITUD_INVALIDA");
    }

    [Fact]
    public async Task Debe_Responder400_Cuando_ElArchivoSuperaCincoMegabytes()
    {
        var becaId = Guid.NewGuid();
        var postulanteId = Guid.NewGuid();
        var consultasBeca = CrearConsultasBeca(
            becaId,
            Guid.NewGuid(),
            postulanteId);
        var comandos = Substitute.For<ICommandDispatcher>();

        using var fabrica = CrearFabrica(
            comandos: comandos,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoBecario(cliente, postulanteId);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-compromiso-carga-0005");

        var archivoGrande = new byte[(5 * 1024 * 1024) + 1];
        _pdfValido.CopyTo(archivoGrande, 0);

        using var formulario = CrearFormulario(archivoGrande);
        var respuesta = await cliente.PostAsync(
            $"/api/v1/becas/{becaId}/compromisos",
            formulario);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var problema = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        problema.RootElement.GetProperty("code").GetString()
            .Should().Be("ARCHIVO_COMPROMISO_INVALIDO");

        await comandos.DidNotReceive().EnviarAsync<
            CargarCompromisoBecarioCommand,
            CompromisoCargadoDto>(
            Arg.Any<CargarCompromisoBecarioCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Responder401_Cuando_NoHayAutenticacion()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        using var formulario = CrearFormulario(_pdfValido);
        var respuesta = await cliente.PostAsync(
            $"/api/v1/becas/{Guid.NewGuid()}/compromisos",
            formulario);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Debe_RevisarElCompromiso_Cuando_ElCoordinadorTiene2FAYAlcance()
    {
        var compromisoId = Guid.NewGuid();
        var becaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var resultadoEsperado = new CompromisoRevisadoDto(
            compromisoId,
            "APROBADO",
            true);

        var comandos = Substitute.For<ICommandDispatcher>();
        comandos
            .EnviarAsync<RevisarCompromisoBecarioCommand, CompromisoRevisadoDto>(
                Arg.Any<RevisarCompromisoBecarioCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(resultadoEsperado));

        var consultasBeca = CrearConsultasBeca(
            compromisoId,
            becaId,
            programaId,
            Guid.NewGuid());

        using var fabrica = CrearFabrica(
            comandos: comandos,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, programaId);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-compromiso-revision-0001");

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/compromisos/{compromisoId}/revision",
            new { aprobar = true, observaciones = "Documento conforme." });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<CompromisoRevisadoDto>();
        cuerpo.Should().BeEquivalentTo(resultadoEsperado);

        await comandos.Received(1).EnviarAsync<
            RevisarCompromisoBecarioCommand,
            CompromisoRevisadoDto>(
            Arg.Is<RevisarCompromisoBecarioCommand>(comando =>
                comando.CompromisoId == compromisoId
                && comando.Aprobar
                && comando.Observaciones == "Documento conforme."),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Responder403_Cuando_ElCoordinadorNoTieneAlcance()
    {
        var compromisoId = Guid.NewGuid();
        var becaId = Guid.NewGuid();
        var programaDeLaBeca = Guid.NewGuid();
        var programaDelToken = Guid.NewGuid();
        var comandos = Substitute.For<ICommandDispatcher>();

        var consultasBeca = CrearConsultasBeca(
            compromisoId,
            becaId,
            programaDeLaBeca,
            Guid.NewGuid());

        using var fabrica = CrearFabrica(
            comandos: comandos,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, programaDelToken);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-compromiso-revision-0002");

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/compromisos/{compromisoId}/revision",
            new { aprobar = true });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await comandos.DidNotReceive().EnviarAsync<
            RevisarCompromisoBecarioCommand,
            CompromisoRevisadoDto>(
            Arg.Any<RevisarCompromisoBecarioCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Responder404_Cuando_ElCompromisoNoExiste()
    {
        var comandos = Substitute.For<ICommandDispatcher>();

        using var fabrica = CrearFabrica(comandos: comandos);
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, Guid.NewGuid());
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-compromiso-revision-0003");

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/compromisos/{Guid.NewGuid()}/revision",
            new { aprobar = false, observaciones = "Ilegible." });

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await comandos.DidNotReceive().EnviarAsync<
            RevisarCompromisoBecarioCommand,
            CompromisoRevisadoDto>(
            Arg.Any<RevisarCompromisoBecarioCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Responder401_Cuando_ElCoordinadorNoPresenta2FA()
    {
        var compromisoId = Guid.NewGuid();
        var becaId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var comandos = Substitute.For<ICommandDispatcher>();

        var consultasBeca = CrearConsultasBeca(
            compromisoId,
            becaId,
            programaId,
            Guid.NewGuid());

        using var fabrica = CrearFabrica(
            comandos: comandos,
            consultasBeca: consultasBeca);
        using var cliente = fabrica.CreateClient();
        AutenticarComoCoordinador(cliente, programaId, con2FA: false);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            "clave-compromiso-revision-0004");

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/compromisos/{compromisoId}/revision",
            new { aprobar = true });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await comandos.DidNotReceive().EnviarAsync<
            RevisarCompromisoBecarioCommand,
            CompromisoRevisadoDto>(
            Arg.Any<RevisarCompromisoBecarioCommand>(),
            Arg.Any<CancellationToken>());
    }

    private WebApplicationFactory<Program> CrearFabrica(
        ICommandDispatcher? comandos = null,
        IConsultasBeca? consultasBeca = null,
        bool usarDespachadorReal = false)
        => _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureTestServices(servicios =>
            {
                if (!usarDespachadorReal)
                {
                    servicios.RemoveAll<ICommandDispatcher>();
                    servicios.AddSingleton(
                        comandos ?? Substitute.For<ICommandDispatcher>());
                }

                servicios.RemoveAll<IConsultasBeca>();
                servicios.AddSingleton(
                    consultasBeca ?? Substitute.For<IConsultasBeca>());
            });
        });

    private static IConsultasBeca CrearConsultasBeca(
        Guid becaId,
        Guid programaId,
        Guid postulanteId)
    {
        var consultas = Substitute.For<IConsultasBeca>();
        consultas
            .ObtenerRecursoBecaAsync(
                becaId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RecursoBecaDto?>(
                new RecursoBecaDto(
                    becaId,
                    programaId,
                    postulanteId)));

        return consultas;
    }

    private static IConsultasBeca CrearConsultasBeca(
        Guid compromisoId,
        Guid becaId,
        Guid programaId,
        Guid postulanteId)
    {
        var consultas = Substitute.For<IConsultasBeca>();
        consultas
            .ObtenerRecursoCompromisoAsync(
                compromisoId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RecursoCompromisoDto?>(
                new RecursoCompromisoDto(
                    compromisoId,
                    becaId,
                    programaId,
                    postulanteId)));

        return consultas;
    }

    private static MultipartFormDataContent CrearFormulario(
        byte[] contenido,
        string nombreArchivo = "compromiso.pdf",
        string tipoMime = "application/pdf")
    {
        var formulario = new MultipartFormDataContent();
        var archivo = new ByteArrayContent(contenido);
        archivo.Headers.ContentType = new MediaTypeHeaderValue(tipoMime);
        formulario.Add(archivo, "archivo", nombreArchivo);
        return formulario;
    }

    private static void AutenticarComoBecario(
        HttpClient cliente,
        Guid postulanteId)
    {
        cliente.DefaultRequestHeaders.Add(
            EncabezadoRoles,
            "BECARIO");
        cliente.DefaultRequestHeaders.Add(
            EncabezadoPostulante,
            postulanteId.ToString());
    }

    private static void AutenticarComoCoordinador(
        HttpClient cliente,
        Guid programaId,
        bool con2FA = true)
    {
        cliente.DefaultRequestHeaders.Add(
            EncabezadoRoles,
            "COORDINADOR_PROGRAMA");
        cliente.DefaultRequestHeaders.Add(
            EncabezadoProgramas,
            programaId.ToString());

        if (con2FA)
        {
            cliente.DefaultRequestHeaders.Add(
                EncabezadoAcr,
                "urn:ascenso:loa:2fa");
        }
    }

    private static void VerificarOperacion(
        JsonElement rutas,
        string ruta,
        string metodo,
        params string[] codigos)
    {
        rutas.TryGetProperty(ruta, out var operaciones).Should().BeTrue(
            $"OpenAPI debe exponer la ruta {ruta}.");

        operaciones.TryGetProperty(metodo, out var operacion).Should().BeTrue(
            $"OpenAPI debe exponer el método {metodo} para {ruta}.");

        operacion.TryGetProperty("responses", out var respuestas).Should().BeTrue(
            $"OpenAPI debe declarar respuestas para {metodo} {ruta}.");

        foreach (var codigo in codigos)
        {
            respuestas.TryGetProperty(codigo, out _).Should().BeTrue(
                $"OpenAPI debe declarar el código {codigo} para {metodo} {ruta}.");
        }
    }
}
