using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Ascenso.Postulaciones.IntegrationTests.Controllers;

public sealed class ResultadosConsolidadosControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";
    private const string RolCoordinador = "COORDINADOR_PROGRAMA";
    private const string RolDireccion = "DIRECCION";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public ResultadosConsolidadosControllerTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    private static (
        ComiteEvaluador Comite,
        RubricaEvaluacion Rubrica,
        CriterioEvaluacion Criterio,
        AsignacionEvaluacion Asignacion1,
        AsignacionEvaluacion Asignacion2,
        Evaluacion Evaluacion1,
        Evaluacion Evaluacion2,
        Guid Evaluador1Id,
        Guid Evaluador2Id,
        DateTimeOffset Ahora) CrearEscenarioPostulacionCompleta(
        decimal nota1 = 14.00m,
        decimal nota2 = 19.00m)
    {
        var ahora = DateTimeOffset.UtcNow;
        var programaBecaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var coordinadorId = Guid.NewGuid();
        var evaluador1Id = Guid.NewGuid();
        var evaluador2Id = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();

        var rubrica = RubricaEvaluacion.Crear(
            programaBecaId,
            convocatoriaId,
            "Rúbrica Consolidación 2026",
            ahora.AddHours(-3),
            coordinadorId);
        var criterio = rubrica.AgregarCriterio(
            "Mérito global",
            "Puntaje sobre 20",
            100.00m,
            0.00m,
            20.00m,
            ahora.AddHours(-3).AddMinutes(1),
            coordinadorId);
        rubrica.Publicar(ahora.AddHours(-3).AddMinutes(2), coordinadorId);
        rubrica.Bloquear(MotivoBloqueoRubrica.PrimeraCalificacion, ahora.AddHours(-2), coordinadorId);

        var comite = ComiteEvaluador.Crear(
            programaBecaId,
            convocatoriaId,
            "Comité Consolidación 2026",
            evaluador1Id,
            ahora.AddHours(-2),
            coordinadorId);
        var miembro1 = comite.Miembros.First();
        var miembro2 = comite.AgregarMiembro(
            evaluador2Id,
            TipoEvaluador.Externo,
            ahora.AddHours(-2).AddMinutes(1),
            coordinadorId);

        var asignacion1 = AsignacionEvaluacion.Crear(
            comite,
            miembro1.Id,
            postulacionId,
            ahora.AddHours(-1),
            ahora.AddDays(5),
            coordinadorId);
        asignacion1.IniciarEvaluacion(ahora.AddMinutes(-50), evaluador1Id);
        var evaluacion1 = Evaluacion.CrearBorrador(
            asignacion1,
            rubrica,
            [new DatosCalificacionCriterio(criterio.Id, nota1, "Nota E1")],
            "Evaluación definitiva 1",
            ahora.AddMinutes(-50),
            evaluador1Id);
        evaluacion1.EnviarDefinitiva(
            rubrica,
            asignacion1.FechaLimiteEvaluacion,
            ahora.AddMinutes(-45),
            evaluador1Id);
        asignacion1.CompletarEvaluacion(ahora.AddMinutes(-45), evaluador1Id);

        var asignacion2 = AsignacionEvaluacion.Crear(
            comite,
            miembro2.Id,
            postulacionId,
            ahora.AddHours(-1),
            ahora.AddDays(5),
            coordinadorId);
        asignacion2.IniciarEvaluacion(ahora.AddMinutes(-40), evaluador2Id);
        var evaluacion2 = Evaluacion.CrearBorrador(
            asignacion2,
            rubrica,
            [new DatosCalificacionCriterio(criterio.Id, nota2, "Nota E2")],
            "Evaluación definitiva 2",
            ahora.AddMinutes(-40),
            evaluador2Id);
        evaluacion2.EnviarDefinitiva(
            rubrica,
            asignacion2.FechaLimiteEvaluacion,
            ahora.AddMinutes(-35),
            evaluador2Id);
        asignacion2.CompletarEvaluacion(ahora.AddMinutes(-35), evaluador2Id);

        return (comite, rubrica, criterio, asignacion1, asignacion2, evaluacion1, evaluacion2, evaluador1Id, evaluador2Id, ahora);
    }

    [Fact]
    public async Task Debe_Retornar201CreatedConLocationEnCreacionY200OkEnRecalculo_Cuando_CoordinadorConsolidaManualmente()
    {
        var (_, _, _, asignacion1, asignacion2, evaluacion1, evaluacion2, _, _, _) =
            CrearEscenarioPostulacionCompleta(nota1: 14.00m, nota2: 19.00m); // 70.00 y 95.00 -> promedio 82.50, dif 25.00
        var postulacionId = asignacion1.PostulacionId;
        var coordinadorId = Guid.NewGuid();

        ResultadoConsolidadoRanking? resultadoPersistido = null;

        var repoResultados = Substitute.For<IResultadoConsolidadoRepository>();
        repoResultados.ObtenerParaActualizarPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns(_ => resultadoPersistido);
        repoResultados
            .When(r => r.AgregarAsync(Arg.Any<ResultadoConsolidadoRanking>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => resultadoPersistido = callInfo.Arg<ResultadoConsolidadoRanking>());

        var repoDatos = Substitute.For<IConsultaDatosConsolidacionRepository>();
        repoDatos.ListarAsignacionesVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([asignacion1, asignacion2]);
        repoDatos.ListarEvaluacionesDefinitivasPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([evaluacion1, evaluacion2]);
        repoDatos.ObtenerExcepcionEvaluadorUnicoPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns((AutorizacionExcepcionEvaluadorUnico?)null);

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(
            repoResultados: repoResultados,
            repoDatos: repoDatos,
            uow: uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respCreacion = await cliente.PostAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}",
            content: null);

        respCreacion.StatusCode.Should().Be(HttpStatusCode.Created);
        respCreacion.Headers.Location.Should().NotBeNull();
        respCreacion.Headers.Location!.ToString()
            .Should().Contain($"/api/v1/resultados-consolidados/postulaciones/{postulacionId}");

        var jsonCreacion = await respCreacion.Content.ReadAsStringAsync();
        using (var doc = JsonDocument.Parse(jsonCreacion))
        {
            doc.RootElement.GetProperty("estadoConfirmacionRanking").GetString().Should().Be("PROVISIONAL");
        }

        var dtoCreado = await respCreacion.Content.ReadFromJsonAsync<ResultadoConsolidadoDto>();
        dtoCreado.Should().NotBeNull();
        dtoCreado!.PostulacionId.Should().Be(postulacionId);
        dtoCreado.CantidadEvaluacionesValidas.Should().Be(2);
        dtoCreado.PuntajeConsolidado.Should().Be(82.50m);
        dtoCreado.DiferenciaEntreEvaluadores.Should().Be(25.00m);
        dtoCreado.UmbralDiscrepanciaAplicado.Should().Be(20.00m);
        dtoCreado.AlertaDiscrepancia.Should().BeTrue();
        dtoCreado.DiscrepanciaRevisada.Should().BeFalse();
        dtoCreado.EstadoConfirmacionRanking.Should().Be("PROVISIONAL");
        dtoCreado.Version.Should().Be(1);

        var respRecalculo = await cliente.PostAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}",
            content: null);

        respRecalculo.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtoRecalculo = await respRecalculo.Content.ReadFromJsonAsync<ResultadoConsolidadoDto>();
        dtoRecalculo.Should().NotBeNull();
        dtoRecalculo!.Id.Should().Be(dtoCreado.Id);
        dtoRecalculo.PuntajeConsolidado.Should().Be(82.50m);
        dtoRecalculo.Version.Should().Be(1);

        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar200OkConRevisionRegistrada_Cuando_CoordinadorRevisaDiscrepanciaActiva()
    {
        var (_, _, _, asignacion1, _, _, _, _, _, ahora) =
            CrearEscenarioPostulacionCompleta(nota1: 14.00m, nota2: 19.00m);
        var postulacionId = asignacion1.PostulacionId;
        var coordinadorId = Guid.NewGuid();

        var resultado = ResultadoConsolidadoRanking.Crear(
            new ResultadoCalculoConsolidacion(
                asignacion1.ConvocatoriaId,
                postulacionId,
                excepcionEvaluadorUnico: false,
                justificacionEvaluadorUnico: null,
                autorizadorExcepcionId: null,
                cantidadEvaluacionesValidas: 2,
                puntajeConsolidado: 82.50m,
                diferenciaEntreEvaluadores: 25.00m,
                umbralDiscrepanciaAplicado: 20.00m,
                alertaDiscrepancia: true),
            ahora.AddMinutes(-10),
            coordinadorId);

        var repoResultados = Substitute.For<IResultadoConsolidadoRepository>();
        repoResultados.ObtenerParaActualizarPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns(resultado);

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(repoResultados: repoResultados, uow: uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}/discrepancia/revisar",
            new
            {
                comentario = "  Se verificó el acta de ambos evaluadores y se valida el promedio consolidado.  "
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<ResultadoConsolidadoDto>();
        dto.Should().NotBeNull();
        dto!.PostulacionId.Should().Be(postulacionId);
        dto.AlertaDiscrepancia.Should().BeTrue();
        dto.DiscrepanciaRevisada.Should().BeTrue();
        dto.ComentarioRevisionDiscrepancia.Should().Be(
            "Se verificó el acta de ambos evaluadores y se valida el promedio consolidado.");
        dto.RevisadoPorUsuarioId.Should().Be(coordinadorId);
        dto.FechaRevisionDiscrepancia.Should().NotBeNull();
        dto.Version.Should().Be(2);

        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar200OkConDetalleYNotasIndividualesOrdenadas_Cuando_SeConsultaPorPostulacion()
    {
        var (_, _, _, asignacion1, _, evaluacion1, evaluacion2, _, _, ahora) =
            CrearEscenarioPostulacionCompleta(nota1: 15.00m, nota2: 18.00m); // 75.00 y 90.00
        var postulacionId = asignacion1.PostulacionId;

        var resultado = ResultadoConsolidadoRanking.Crear(
            new ResultadoCalculoConsolidacion(
                asignacion1.ConvocatoriaId,
                postulacionId,
                excepcionEvaluadorUnico: false,
                justificacionEvaluadorUnico: null,
                autorizadorExcepcionId: null,
                cantidadEvaluacionesValidas: 2,
                puntajeConsolidado: 82.50m,
                diferenciaEntreEvaluadores: 15.00m,
                umbralDiscrepanciaAplicado: 20.00m,
                alertaDiscrepancia: false),
            ahora.AddMinutes(-5),
            Guid.NewGuid());

        var repoResultados = Substitute.For<IResultadoConsolidadoRepository>();
        repoResultados.ObtenerPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns(resultado);

        var repoDatos = Substitute.For<IConsultaDatosConsolidacionRepository>();
        repoDatos.ListarEvaluacionesDefinitivasPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([evaluacion2, evaluacion1]);

        using var fabrica = CrearFabrica(repoResultados: repoResultados, repoDatos: repoDatos);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolDireccion);

        var respuesta = await cliente.GetAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var detalle = await respuesta.Content.ReadFromJsonAsync<DetalleResultadoConsolidadoDto>();
        detalle.Should().NotBeNull();
        detalle!.Id.Should().Be(resultado.Id);
        detalle.PostulacionId.Should().Be(postulacionId);
        detalle.EstadoConfirmacionRanking.Should().Be("PROVISIONAL");
        detalle.Evaluaciones.Should().HaveCount(2);
        detalle.Evaluaciones.Select(e => e.EvaluacionId)
            .Should().Equal(evaluacion1.Id, evaluacion2.Id);
        detalle.Evaluaciones[0].PuntajeTotalPonderado.Should().Be(75.00m);
        detalle.Evaluaciones[1].PuntajeTotalPonderado.Should().Be(90.00m);
    }

    [Fact]
    public async Task Debe_Retornar200OkConListadoPaginadoYFiltrado_Cuando_SeConsultaPorConvocatoria()
    {
        var convocatoriaId = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;

        var resultado = ResultadoConsolidadoRanking.Crear(
            new ResultadoCalculoConsolidacion(
                convocatoriaId,
                Guid.NewGuid(),
                excepcionEvaluadorUnico: false,
                justificacionEvaluadorUnico: null,
                autorizadorExcepcionId: null,
                cantidadEvaluacionesValidas: 2,
                puntajeConsolidado: 84.00m,
                diferenciaEntreEvaluadores: 24.00m,
                umbralDiscrepanciaAplicado: 20.00m,
                alertaDiscrepancia: true),
            ahora,
            Guid.NewGuid());

        var repoResultados = Substitute.For<IResultadoConsolidadoRepository>();
        repoResultados.ListarPorConvocatoriaAsync(
                convocatoriaId,
                alertaDiscrepancia: true,
                discrepanciaRevisada: false,
                pagina: 1,
                tamano: 10,
                Arg.Any<CancellationToken>())
            .Returns(new PaginaResultado<ResultadoConsolidadoRanking>([resultado], 1));

        using var fabrica = CrearFabrica(repoResultados: repoResultados);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.GetAsync(
            $"/api/v1/resultados-consolidados/convocatorias/{convocatoriaId}?alertaDiscrepancia=true&discrepanciaRevisada=false&pagina=1&tamano=10");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagina = await respuesta.Content.ReadFromJsonAsync<PaginaDto<ResultadoConsolidadoDto>>();
        pagina.Should().NotBeNull();
        pagina!.Total.Should().Be(1);
        pagina.Pagina.Should().Be(1);
        pagina.Tamano.Should().Be(10);
        pagina.Items.Should().ContainSingle();
        pagina.Items[0].Id.Should().Be(resultado.Id);
        pagina.Items[0].AlertaDiscrepancia.Should().BeTrue();
        pagina.Items[0].DiscrepanciaRevisada.Should().BeFalse();
        pagina.Items[0].EstadoConfirmacionRanking.Should().Be("PROVISIONAL");
    }

    [Fact]
    public async Task Debe_Retornar400BadRequest_Cuando_SolicitudesDeConsolidacionTienenParametrosInvalidos()
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respConsolidarVacio = await cliente.PostAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{Guid.Empty}",
            content: null);
        respConsolidarVacio.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respRevisarComentarioCorto = await cliente.PostAsJsonAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{Guid.NewGuid()}/discrepancia/revisar",
            new { comentario = "corto" });
        respRevisarComentarioCorto.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respObtenerVacio = await cliente.GetAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{Guid.Empty}");
        respObtenerVacio.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respListarConvocatoriaVacia = await cliente.GetAsync(
            $"/api/v1/resultados-consolidados/convocatorias/{Guid.Empty}");
        respListarConvocatoriaVacia.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respListarPaginaCero = await cliente.GetAsync(
            $"/api/v1/resultados-consolidados/convocatorias/{Guid.NewGuid()}?pagina=0");
        respListarPaginaCero.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respListarTamanoExcesivo = await cliente.GetAsync(
            $"/api/v1/resultados-consolidados/convocatorias/{Guid.NewGuid()}?tamano=101");
        respListarTamanoExcesivo.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Debe_Retornar404409Y422EnProblemDetailsConCodigosDeDominio_Cuando_FallanReglasDeConsolidacionYRevision()
    {
        var (_, _, _, asignacion1, asignacion2, evaluacion1, _, _, _, ahora) =
            CrearEscenarioPostulacionCompleta(nota1: 15.00m, nota2: 18.00m);
        var postulacionId = asignacion1.PostulacionId;
        var postulacionInexistenteId = Guid.NewGuid();
        var coordinadorId = Guid.NewGuid();

        var repoResultados = Substitute.For<IResultadoConsolidadoRepository>();
        var repoDatos = Substitute.For<IConsultaDatosConsolidacionRepository>();

        using var fabrica = CrearFabrica(repoResultados: repoResultados, repoDatos: repoDatos);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        // 1. 404 RESULTADO_CONSOLIDADO_NO_ENCONTRADO en GET y POST revisar
        var respGet404 = await cliente.GetAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionInexistenteId}");
        respGet404.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respGet404, ResultadoConsolidadoNoEncontradoException.CodigoError);

        var respRevisar404 = await cliente.PostAsJsonAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionInexistenteId}/discrepancia/revisar",
            new { comentario = "Comentario válido con longitud suficiente." });
        respRevisar404.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respRevisar404, ResultadoConsolidadoNoEncontradoException.CodigoError);

        // 2. 409 EVALUACIONES_PENDIENTES cuando una asignación vigente no está COMPLETADA
        var comitePendiente = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Pendiente",
            Guid.NewGuid(),
            ahora.AddHours(-1),
            coordinadorId);
        var asignacionPendiente = AsignacionEvaluacion.Crear(
            comitePendiente,
            comitePendiente.Miembros.First().Id,
            postulacionId,
            ahora.AddMinutes(-30),
            ahora.AddDays(5),
            coordinadorId);

        repoDatos.ListarAsignacionesVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([asignacion1, asignacionPendiente]);
        repoDatos.ListarEvaluacionesDefinitivasPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([evaluacion1]);

        var respPendientes = await cliente.PostAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}",
            content: null);
        respPendientes.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respPendientes, EvaluacionesPendientesParaConsolidarException.CodigoError);

        // 3. 422 EVALUACIONES_INSUFICIENTES cuando solo hay 1 evaluación definitiva sin excepción
        repoDatos.ListarAsignacionesVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([asignacion1, asignacion2]);
        repoDatos.ListarEvaluacionesDefinitivasPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([evaluacion1]);

        var respInsuficientes = await cliente.PostAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}",
            content: null);
        respInsuficientes.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(respInsuficientes, EvaluadoresInsuficientesException.CodigoError);

        // 4. 422 DISCREPANCIA_NO_DETECTADA al revisar resultado sin alerta
        var resultadoSinAlerta = ResultadoConsolidadoRanking.Crear(
            new ResultadoCalculoConsolidacion(
                asignacion1.ConvocatoriaId,
                postulacionId,
                excepcionEvaluadorUnico: false,
                justificacionEvaluadorUnico: null,
                autorizadorExcepcionId: null,
                cantidadEvaluacionesValidas: 2,
                puntajeConsolidado: 82.50m,
                diferenciaEntreEvaluadores: 15.00m,
                umbralDiscrepanciaAplicado: 20.00m,
                alertaDiscrepancia: false),
            ahora.AddMinutes(-10),
            coordinadorId);

        repoResultados.ObtenerParaActualizarPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns(resultadoSinAlerta);

        var respSinAlerta = await cliente.PostAsJsonAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}/discrepancia/revisar",
            new { comentario = "Comentario válido con longitud suficiente." });
        respSinAlerta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(respSinAlerta, DiscrepanciaNoDetectadaException.CodigoError);

        // 5. 409 DISCREPANCIA_YA_REVISADA al revisar una segunda vez
        var resultadoYaRevisado = ResultadoConsolidadoRanking.Crear(
            new ResultadoCalculoConsolidacion(
                asignacion1.ConvocatoriaId,
                postulacionId,
                excepcionEvaluadorUnico: false,
                justificacionEvaluadorUnico: null,
                autorizadorExcepcionId: null,
                cantidadEvaluacionesValidas: 2,
                puntajeConsolidado: 80.00m,
                diferenciaEntreEvaluadores: 25.00m,
                umbralDiscrepanciaAplicado: 20.00m,
                alertaDiscrepancia: true),
            ahora.AddMinutes(-10),
            coordinadorId);
        resultadoYaRevisado.MarcarDiscrepanciaRevisada(
            "Revisión inicial ya registrada en el expediente.",
            ahora.AddMinutes(-5),
            coordinadorId);

        repoResultados.ObtenerParaActualizarPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns(resultadoYaRevisado);

        var respYaRevisada = await cliente.PostAsJsonAsync(
            $"/api/v1/resultados-consolidados/postulaciones/{postulacionId}/discrepancia/revisar",
            new { comentario = "Segundo intento de revisión sobre discrepancia." });
        respYaRevisada.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respYaRevisada, DiscrepanciaYaRevisadaException.CodigoError);
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IResultadoConsolidadoRepository? repoResultados = null,
        IConsultaDatosConsolidacionRepository? repoDatos = null,
        IProveedorParametrosConsolidacion? proveedorParametros = null,
        IUnitOfWork? uow = null)
    {
        return _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<IPostulanteRepository>();
                servicios.RemoveAll<IRubricaRepository>();
                servicios.RemoveAll<IComiteEvaluadorRepository>();
                servicios.RemoveAll<IAsignacionEvaluacionRepository>();
                servicios.RemoveAll<IExcepcionEvaluadorUnicoRepository>();
                servicios.RemoveAll<IEvaluacionRepository>();
                servicios.RemoveAll<IConsultaAsignacionParaEvaluacionRepository>();
                servicios.RemoveAll<IConsultaRubricaParaEvaluacionRepository>();
                servicios.RemoveAll<IConsultaEvaluacionesCoevaluadoresRepository>();
                servicios.RemoveAll<IResultadoConsolidadoRepository>();
                servicios.RemoveAll<IConsultaDatosConsolidacionRepository>();
                servicios.RemoveAll<IProveedorParametrosConsolidacion>();
                servicios.RemoveAll<IRankingConvocatoriaRepository>();
                servicios.RemoveAll<IConsultaEstadoEvaluacionesConvocatoriaRepository>();
                servicios.RemoveAll<IDisponibilidadRankingClient>();
                servicios.RemoveAll<IProveedorMontoEstimadoBeca>();
                servicios.RemoveAll<IPeriodoAcademicoClient>();
                servicios.RemoveAll<ICatalogoAcademicoClient>();
                servicios.RemoveAll<IConvocatoriaEvaluacionClient>();
                servicios.RemoveAll<IUnitOfWork>();

                var proveedorPorDefecto = Substitute.For<IProveedorParametrosConsolidacion>();
                proveedorPorDefecto.ObtenerParametrosAsync(
                        Arg.Any<Guid>(),
                        Arg.Any<Guid>(),
                        Arg.Any<CancellationToken>())
                    .Returns(ParametrosConsolidacion.PorDefecto);

                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(Substitute.For<IEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>());
                servicios.AddSingleton(repoResultados ?? Substitute.For<IResultadoConsolidadoRepository>());
                servicios.AddSingleton(repoDatos ?? Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(proveedorParametros ?? proveedorPorDefecto);
                servicios.AddSingleton(Substitute.For<IRankingConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IDisponibilidadRankingClient>());
                servicios.AddSingleton(Substitute.For<IProveedorMontoEstimadoBeca>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<IConvocatoriaEvaluacionClient>());
                servicios.AddSingleton(uow ?? Substitute.For<IUnitOfWork>());
                servicios.AddSingleton(TimeProvider.System);

                servicios.AddAuthentication(opciones =>
                    {
                        opciones.DefaultAuthenticateScheme = EsquemaPrueba;
                        opciones.DefaultChallengeScheme = EsquemaPrueba;
                        opciones.DefaultScheme = EsquemaPrueba;
                    })
                    .AddScheme<AuthenticationSchemeOptions, ManejadorAutenticacionPrueba>(
                        EsquemaPrueba,
                        _ => { });
            });
        });
    }

    private static HttpClient CrearClienteAutenticado(
        WebApplicationFactory<Program> fabrica,
        Guid usuarioId,
        string rol)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, usuarioId.ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, rol);
        return cliente;
    }

    private static async Task VerificarCodigoErrorAsync(HttpResponseMessage respuesta, string codigoEsperado)
    {
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        documento.RootElement.GetProperty("codigo").GetString().Should().Be(codigoEsperado);
    }

    private sealed class ManejadorAutenticacionPrueba : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public ManejadorAutenticacionPrueba(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderSub, out var valoresSub))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new("sub", valoresSub.FirstOrDefault() ?? string.Empty)
            };

            if (Request.Headers.TryGetValue(HeaderRol, out var valoresRol))
            {
                foreach (var rol in valoresRol.Where(valor => !string.IsNullOrWhiteSpace(valor)))
                {
                    claims.Add(new Claim("roles", rol!));
                }
            }

            var identidad = new ClaimsIdentity(claims, EsquemaPrueba, "sub", "roles");
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identidad), EsquemaPrueba)));
        }
    }
}
