using System.Net;
using System.Text.Json;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Ascenso.Postulaciones.IntegrationTests.Controllers;

public sealed class SwaggerApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _fabricaBase;

    public SwaggerApiTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_DocumentarPutSocioeconomicoYSusEsquemas_Cuando_EntornoEsDevelopment()
    {
        using var fabrica = _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.UseEnvironment("Development");
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton(Substitute.For<IUsuarioActual>());
                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(Substitute.For<IEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>());
                servicios.AddSingleton(Substitute.For<IResultadoConsolidadoRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
                servicios.AddSingleton(Substitute.For<IRankingConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaJefaturaComiteRepository>());
                servicios.AddSingleton(Substitute.For<IVerificadorConflictoInteresDecisor>());
                servicios.AddSingleton(Substitute.For<IDisponibilidadRankingClient>());
                servicios.AddSingleton(Substitute.For<IProveedorMontoEstimadoBeca>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<IConvocatoriaEvaluacionClient>());
                servicios.AddSingleton(Substitute.For<IUnitOfWork>());
                servicios.AddSingleton(TimeProvider.System);
            });
        });
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;

        var ruta = raiz.GetProperty("paths")
            .GetProperty("/api/v1/postulantes/mio/socioeconomico");
        var operacionPut = ruta.GetProperty("put");
        var respuestas = operacionPut.GetProperty("responses");

        foreach (var codigo in new[] { "200", "400", "401", "404", "409", "422" })
        {
            respuestas.TryGetProperty(codigo, out _).Should().BeTrue();
        }

        var esquemaRespuesta = respuestas.GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();
        esquemaRespuesta.Should().Contain("PostulanteDto");

        var esquemaSolicitud = operacionPut.GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();
        esquemaSolicitud.Should().Contain("ActualizarPerfilSocioeconomicoRequest");

        var esquemas = raiz.GetProperty("components").GetProperty("schemas");
        esquemas.TryGetProperty("ActualizarPerfilSocioeconomicoRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("DatosMiembroFamiliarRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("PostulanteDto", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Debe_DocumentarEndpointsDeRubricasYSusEsquemas_Cuando_EntornoEsDevelopment()
    {
        using var fabrica = _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.UseEnvironment("Development");
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton(Substitute.For<IUsuarioActual>());
                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(Substitute.For<IEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>());
                servicios.AddSingleton(Substitute.For<IResultadoConsolidadoRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
                servicios.AddSingleton(Substitute.For<IRankingConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaJefaturaComiteRepository>());
                servicios.AddSingleton(Substitute.For<IVerificadorConflictoInteresDecisor>());
                servicios.AddSingleton(Substitute.For<IDisponibilidadRankingClient>());
                servicios.AddSingleton(Substitute.For<IProveedorMontoEstimadoBeca>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<IConvocatoriaEvaluacionClient>());
                servicios.AddSingleton(Substitute.For<IUnitOfWork>());
                servicios.AddSingleton(TimeProvider.System);
            });
        });
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;
        var rutas = raiz.GetProperty("paths");

        VerificarRespuestas(rutas.GetProperty("/api/v1/rubricas").GetProperty("post"), "201", ["201", "400", "401", "403", "409"]);
        VerificarRespuestas(rutas.GetProperty("/api/v1/rubricas/{id}").GetProperty("get"), "200", ["200", "401", "403", "404"]);
        VerificarRespuestas(rutas.GetProperty("/api/v1/rubricas/{id}/criterios").GetProperty("post"), "201", ["201", "400", "401", "403", "404", "409"]);

        var rutaCriterio = rutas.GetProperty("/api/v1/rubricas/{id}/criterios/{criterioId}");
        VerificarRespuestas(rutaCriterio.GetProperty("put"), "200", ["200", "400", "401", "403", "404", "409"]);
        VerificarRespuestas(rutaCriterio.GetProperty("delete"), "200", ["200", "401", "403", "404", "409"]);

        VerificarRespuestas(rutas.GetProperty("/api/v1/rubricas/{id}/publicar").GetProperty("post"), "200", ["200", "401", "403", "404", "409", "422"]);
        rutas.TryGetProperty("/api/v1/rubricas/{id}/bloquear", out _).Should().BeFalse();

        var esquemas = raiz.GetProperty("components").GetProperty("schemas");
        esquemas.TryGetProperty("CrearRubricaRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("GuardarCriterioRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("RubricaDto", out var esquemaRubricaDto).Should().BeTrue();
        esquemas.TryGetProperty("CriterioEvaluacionDto", out _).Should().BeTrue();

        var propiedadesRubricaDto = esquemaRubricaDto.GetProperty("properties");
        propiedadesRubricaDto.TryGetProperty("motivoBloqueo", out _).Should().BeTrue();
        propiedadesRubricaDto.TryGetProperty("fechaBloqueo", out _).Should().BeTrue();
        propiedadesRubricaDto.TryGetProperty("soloLectura", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Debe_DocumentarEndpointsDeComitesEvaluadoresYSusEsquemas_Cuando_EntornoEsDevelopment()
    {
        using var fabrica = _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.UseEnvironment("Development");
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton(Substitute.For<IUsuarioActual>());
                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(Substitute.For<IEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>());
                servicios.AddSingleton(Substitute.For<IResultadoConsolidadoRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
                servicios.AddSingleton(Substitute.For<IRankingConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaJefaturaComiteRepository>());
                servicios.AddSingleton(Substitute.For<IVerificadorConflictoInteresDecisor>());
                servicios.AddSingleton(Substitute.For<IDisponibilidadRankingClient>());
                servicios.AddSingleton(Substitute.For<IProveedorMontoEstimadoBeca>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<IConvocatoriaEvaluacionClient>());
                servicios.AddSingleton(Substitute.For<IUnitOfWork>());
                servicios.AddSingleton(TimeProvider.System);
            });
        });
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;
        var rutas = raiz.GetProperty("paths");

        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/comites-evaluadores").GetProperty("post"),
            "201",
            ["201", "400", "401", "403", "409", "422"],
            "ComiteEvaluadorDto");
        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/comites-evaluadores/{id}").GetProperty("get"),
            "200",
            ["200", "401", "403", "404"],
            "ComiteEvaluadorDto");
        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/comites-evaluadores/programas/{programaBecaId}/convocatorias/{convocatoriaId}").GetProperty("get"),
            "200",
            ["200", "401", "403", "404"],
            "ComiteEvaluadorDto");
        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/comites-evaluadores/{id}/miembros").GetProperty("post"),
            "201",
            ["201", "400", "401", "403", "404", "409"],
            "ComiteEvaluadorDto");
        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/comites-evaluadores/{id}/miembros/{miembroId}").GetProperty("delete"),
            "200",
            ["200", "401", "403", "404", "422"],
            "ComiteEvaluadorDto");

        var esquemas = raiz.GetProperty("components").GetProperty("schemas");
        esquemas.TryGetProperty("CrearComiteEvaluadorRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("AgregarMiembroComiteRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("ComiteEvaluadorDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("MiembroComiteDto", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Debe_DocumentarEndpointsDeAsignacionesEvaluacionYSusEsquemas_Cuando_EntornoEsDevelopment()
    {
        using var fabrica = _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.UseEnvironment("Development");
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton(Substitute.For<IUsuarioActual>());
                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(Substitute.For<IEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>());
                servicios.AddSingleton(Substitute.For<IResultadoConsolidadoRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
                servicios.AddSingleton(Substitute.For<IRankingConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaJefaturaComiteRepository>());
                servicios.AddSingleton(Substitute.For<IVerificadorConflictoInteresDecisor>());
                servicios.AddSingleton(Substitute.For<IDisponibilidadRankingClient>());
                servicios.AddSingleton(Substitute.For<IProveedorMontoEstimadoBeca>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<IConvocatoriaEvaluacionClient>());
                servicios.AddSingleton(Substitute.For<IUnitOfWork>());
                servicios.AddSingleton(TimeProvider.System);
            });
        });
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;
        var rutas = raiz.GetProperty("paths");

        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/asignaciones-evaluacion").GetProperty("post"),
            "201",
            ["201", "400", "401", "403", "404", "409", "422"],
            "AsignacionesPostulacionDto");
        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}").GetProperty("get"),
            "200",
            ["200", "401", "403"],
            "AsignacionesPostulacionDto");

        var respuestasCarga = rutas
            .GetProperty("/api/v1/asignaciones-evaluacion/comites/{comiteId}/carga")
            .GetProperty("get")
            .GetProperty("responses");
        foreach (var codigo in new[] { "200", "401", "403", "404" })
        {
            respuestasCarga.TryGetProperty(codigo, out _).Should().BeTrue();
        }

        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/asignaciones-evaluacion/mis-asignaciones").GetProperty("get"),
            "200",
            ["200", "400", "401", "403"],
            "MiAsignacionDto");

        var rutaExcepcion = rutas.GetProperty(
            "/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}/excepcion-evaluador-unico");
        VerificarRespuestasConEsquema(
            rutaExcepcion.GetProperty("post"),
            "201",
            ["201", "400", "401", "403", "409", "422"],
            "ExcepcionEvaluadorUnicoDto");
        VerificarRespuestasConEsquema(
            rutaExcepcion.GetProperty("get"),
            "200",
            ["200", "401", "403", "404"],
            "ExcepcionEvaluadorUnicoDto");

        var esquemas = raiz.GetProperty("components").GetProperty("schemas");
        esquemas.TryGetProperty("AsignarEvaluadoresPostulacionRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("AutorizarExcepcionEvaluadorUnicoRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("AsignacionesPostulacionDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("AsignacionEvaluacionDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("CargaEvaluadorDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("MiAsignacionDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("ExcepcionEvaluadorUnicoDto", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Debe_DocumentarEndpointsDeEvaluacionesYSusEsquemas_Cuando_EntornoEsDevelopment()
    {
        using var fabrica = _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.UseEnvironment("Development");
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton(Substitute.For<IUsuarioActual>());
                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(Substitute.For<IEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>());
                servicios.AddSingleton(Substitute.For<IResultadoConsolidadoRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
                servicios.AddSingleton(Substitute.For<IRankingConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaJefaturaComiteRepository>());
                servicios.AddSingleton(Substitute.For<IVerificadorConflictoInteresDecisor>());
                servicios.AddSingleton(Substitute.For<IDisponibilidadRankingClient>());
                servicios.AddSingleton(Substitute.For<IProveedorMontoEstimadoBeca>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<IConvocatoriaEvaluacionClient>());
                servicios.AddSingleton(Substitute.For<IUnitOfWork>());
                servicios.AddSingleton(TimeProvider.System);
            });
        });
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;
        var rutas = raiz.GetProperty("paths");

        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/evaluaciones/asignaciones/{asignacionEvaluacionId}/borrador").GetProperty("put"),
            "200",
            ["200", "201", "400", "401", "403", "404", "409", "422", "503"],
            "EvaluacionDto");
        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/evaluaciones/asignaciones/{asignacionEvaluacionId}/enviar").GetProperty("post"),
            "200",
            ["200", "400", "401", "403", "404", "409", "422", "503"],
            "EvaluacionDto");
        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/evaluaciones/asignaciones/{asignacionEvaluacionId}").GetProperty("get"),
            "200",
            ["200", "400", "401", "403", "404"],
            "EvaluacionDto");
        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/evaluaciones/mis-evaluaciones").GetProperty("get"),
            "200",
            ["200", "400", "401", "403"],
            "ResumenMiEvaluacionDto");
        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/evaluaciones/postulaciones/{postulacionId}").GetProperty("get"),
            "200",
            ["200", "400", "401", "403"],
            "EvaluacionesPostulacionDto");
        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/evaluaciones/postulaciones/{postulacionId}/coevaluadores").GetProperty("get"),
            "200",
            ["200", "400", "401", "403", "404", "409", "503"],
            "EvaluacionesCoevaluadoresPostulacionDto");

        var esquemas = raiz.GetProperty("components").GetProperty("schemas");
        esquemas.TryGetProperty("GuardarBorradorEvaluacionRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("CalificacionCriterioRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("EvaluacionDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("CalificacionCriterioDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("ResumenMiEvaluacionDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("EvaluacionSupervisionDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("EvaluacionesPostulacionDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("EvaluacionCoevaluadorDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("EvaluacionesCoevaluadoresPostulacionDto", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Debe_DocumentarEndpointsDeResultadosConsolidadosYSusEsquemas_Cuando_EntornoEsDevelopment()
    {
        using var fabrica = _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.UseEnvironment("Development");
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton(Substitute.For<IUsuarioActual>());
                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(Substitute.For<IEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>());
                servicios.AddSingleton(Substitute.For<IResultadoConsolidadoRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
                servicios.AddSingleton(Substitute.For<IRankingConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaJefaturaComiteRepository>());
                servicios.AddSingleton(Substitute.For<IVerificadorConflictoInteresDecisor>());
                servicios.AddSingleton(Substitute.For<IDisponibilidadRankingClient>());
                servicios.AddSingleton(Substitute.For<IProveedorMontoEstimadoBeca>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<IConvocatoriaEvaluacionClient>());
                servicios.AddSingleton(Substitute.For<IUnitOfWork>());
                servicios.AddSingleton(TimeProvider.System);
            });
        });
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;
        var rutas = raiz.GetProperty("paths");

        var rutaPostulacion = rutas.GetProperty("/api/v1/resultados-consolidados/postulaciones/{postulacionId}");
        VerificarRespuestasConEsquema(
            rutaPostulacion.GetProperty("post"),
            "200",
            ["200", "201", "400", "401", "403", "409", "422"],
            "ResultadoConsolidadoDto");
        VerificarRespuestasConEsquema(
            rutaPostulacion.GetProperty("get"),
            "200",
            ["200", "400", "401", "403", "404"],
            "DetalleResultadoConsolidadoDto");

        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/resultados-consolidados/postulaciones/{postulacionId}/discrepancia/revisar").GetProperty("post"),
            "200",
            ["200", "400", "401", "403", "404", "409", "422"],
            "ResultadoConsolidadoDto");

        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/resultados-consolidados/convocatorias/{convocatoriaId}").GetProperty("get"),
            "200",
            ["200", "400", "401", "403"],
            "ResultadoConsolidadoDto");

        var esquemas = raiz.GetProperty("components").GetProperty("schemas");
        esquemas.TryGetProperty("RevisarDiscrepanciaConsolidacionRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("ResultadoConsolidadoDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("DetalleResultadoConsolidadoDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("NotaEvaluacionIndividualDto", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Debe_DocumentarEndpointsDeRankingsYAdjudicabilidadInternaYSusEsquemas_Cuando_EntornoEsDevelopment()
    {
        using var fabrica = _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.UseEnvironment("Development");
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton(Substitute.For<IUsuarioActual>());
                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(Substitute.For<IEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>());
                servicios.AddSingleton(Substitute.For<IResultadoConsolidadoRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
                servicios.AddSingleton(Substitute.For<IRankingConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaJefaturaComiteRepository>());
                servicios.AddSingleton(Substitute.For<IVerificadorConflictoInteresDecisor>());
                servicios.AddSingleton(Substitute.For<IDisponibilidadRankingClient>());
                servicios.AddSingleton(Substitute.For<IProveedorMontoEstimadoBeca>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<IConvocatoriaEvaluacionClient>());
                servicios.AddSingleton(Substitute.For<IUnitOfWork>());
                servicios.AddSingleton(TimeProvider.System);
            });
        });
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;
        var rutas = raiz.GetProperty("paths");

        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/rankings/convocatorias/{convocatoriaId}").GetProperty("get"),
            "200",
            ["200", "400", "401", "403", "404", "503"],
            "RankingConvocatoriaDto");

        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/rankings/convocatorias/{convocatoriaId}/confirmar").GetProperty("post"),
            "200",
            ["200", "400", "401", "403", "404", "409", "422", "503"],
            "RankingConvocatoriaDto");

        VerificarRespuestasConEsquema(
            rutas.GetProperty("/api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver").GetProperty("post"),
            "200",
            ["200", "400", "401", "403", "404", "409", "422", "503"],
            "RankingConvocatoriaDto");

        VerificarRespuestasConEsquema(
            rutas.GetProperty("/internal/v1/postulaciones/{postulacionId}/adjudicable").GetProperty("get"),
            "200",
            ["200", "400", "401", "403", "404"],
            "AdjudicabilidadPostulacionDto");

        var esquemas = raiz.GetProperty("components").GetProperty("schemas");
        esquemas.TryGetProperty("ConfirmarRankingConvocatoriaRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("ResolverCasoLimiteRankingRequest", out _).Should().BeTrue();
        esquemas.TryGetProperty("RankingConvocatoriaDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("ElementoRankingDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("DisponibilidadRankingDto", out _).Should().BeTrue();
        esquemas.TryGetProperty("AdjudicabilidadPostulacionDto", out _).Should().BeTrue();
    }

    private static void VerificarRespuestas(
        JsonElement operacion,
        string codigoExito,
        string[] codigosEsperados)
        => VerificarRespuestasConEsquema(operacion, codigoExito, codigosEsperados, "RubricaDto");

    private static void VerificarRespuestasConEsquema(
        JsonElement operacion,
        string codigoExito,
        string[] codigosEsperados,
        string nombreEsquema)
    {
        var respuestas = operacion.GetProperty("responses");
        foreach (var codigo in codigosEsperados)
        {
            respuestas.TryGetProperty(codigo, out _).Should().BeTrue();
        }

        var esquemaRespuesta = respuestas.GetProperty(codigoExito)
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();
        esquemaRespuesta.Should().Contain(nombreEsquema);
    }
}
