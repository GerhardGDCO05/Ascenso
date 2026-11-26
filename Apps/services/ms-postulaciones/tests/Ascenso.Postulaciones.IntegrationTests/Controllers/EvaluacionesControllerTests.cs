using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Application.Evaluaciones.DTOs;
using Ascenso.Postulaciones.Application.Evaluaciones.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
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

public sealed class EvaluacionesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";
    private const string RolCoordinador = "COORDINADOR_PROGRAMA";
    private const string RolMiembroComite = "MIEMBRO_COMITE";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public EvaluacionesControllerTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    private static (
        ComiteEvaluador Comite,
        RubricaEvaluacion Rubrica,
        CriterioEvaluacion Criterio1,
        CriterioEvaluacion Criterio2,
        AsignacionEvaluacion Asignacion,
        Guid EvaluadorId,
        DateTimeOffset Ahora) CrearEscenario(
        EstadoRubrica estadoRubrica = EstadoRubrica.Publicada,
        DateTimeOffset? fechaLimitePersonalizada = null)
    {
        var ahora = DateTimeOffset.UtcNow;
        var programaBecaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var coordinadorId = Guid.NewGuid();
        var evaluadorId = Guid.NewGuid();

        var rubrica = RubricaEvaluacion.Crear(
            programaBecaId,
            convocatoriaId,
            "Rúbrica Mérito 2026",
            ahora.AddHours(-2),
            coordinadorId);
        var criterio1 = rubrica.AgregarCriterio(
            "Promedio académico",
            "Rendimiento académico",
            60.00m,
            0.00m,
            20.00m,
            ahora.AddHours(-2).AddMinutes(1),
            coordinadorId);
        var criterio2 = rubrica.AgregarCriterio(
            "Proyección social",
            "Liderazgo comunitario",
            40.00m,
            0.00m,
            10.00m,
            ahora.AddHours(-2).AddMinutes(2),
            coordinadorId);

        if (estadoRubrica != EstadoRubrica.Borrador)
        {
            rubrica.Publicar(ahora.AddHours(-2).AddMinutes(3), coordinadorId);
        }

        if (estadoRubrica == EstadoRubrica.Bloqueada)
        {
            rubrica.Bloquear(
                MotivoBloqueoRubrica.PrimeraCalificacion,
                ahora.AddHours(-2).AddMinutes(4),
                coordinadorId);
        }

        var comite = ComiteEvaluador.Crear(
            programaBecaId,
            convocatoriaId,
            "Comité Evaluación 2026",
            evaluadorId,
            ahora.AddHours(-1),
            coordinadorId);
        comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            ahora.AddHours(-1).AddMinutes(1),
            coordinadorId);

        var miembro = comite.Miembros.First(m => m.UsuarioEvaluadorId == evaluadorId);
        var fechaAsignacion = fechaLimitePersonalizada.HasValue
            ? fechaLimitePersonalizada.Value.AddDays(-5)
            : ahora.AddMinutes(-30);
        var fechaLimite = fechaLimitePersonalizada ?? ahora.AddDays(7);

        var asignacion = AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            Guid.NewGuid(),
            fechaAsignacion,
            fechaLimite,
            coordinadorId);

        return (comite, rubrica, criterio1, criterio2, asignacion, evaluadorId, ahora);
    }

    [Fact]
    public async Task Debe_Retornar201CreatedConLocationEnCreacionY200OkEnActualizacion_Cuando_EvaluadorGuardaBorrador()
    {
        var (comite, rubrica, criterio1, criterio2, asignacion, evaluadorId, _) = CrearEscenario();

        Evaluacion? evaluacionPersistida = null;

        var repoAsignacion = Substitute.For<IConsultaAsignacionParaEvaluacionRepository>();
        repoAsignacion.ObtenerParaActualizarPorIdAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(_ => asignacion);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoRubrica = Substitute.For<IConsultaRubricaParaEvaluacionRepository>();
        repoRubrica.ObtenerPorProgramaYConvocatoriaAsync(
                comite.ProgramaBecaId,
                comite.ConvocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns(rubrica);

        var repoEvaluacion = Substitute.For<IEvaluacionRepository>();
        repoEvaluacion.ObtenerParaActualizarPorAsignacionAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(_ => evaluacionPersistida);
        repoEvaluacion
            .When(r => r.AgregarAsync(Arg.Any<Evaluacion>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => evaluacionPersistida = callInfo.Arg<Evaluacion>());

        var bloqueadorRubrica = Substitute.For<IBloqueadorRubricaEvaluacion>();
        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(
            repoAsignacion: repoAsignacion,
            repoComites: repoComites,
            repoRubrica: repoRubrica,
            repoEvaluacion: repoEvaluacion,
            bloqueadorRubrica: bloqueadorRubrica,
            uow: uow);
        using var cliente = CrearClienteAutenticado(fabrica, evaluadorId, RolMiembroComite);

        var respCreacion = await cliente.PutAsJsonAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacion.Id}/borrador",
            new
            {
                calificaciones = new[]
                {
                    new
                    {
                        criterioEvaluacionId = criterio1.Id,
                        puntajeAsignado = 15.00m,
                        comentario = "Buen promedio"
                    }
                },
                comentariosGenerales = "Borrador inicial"
            });

        respCreacion.StatusCode.Should().Be(HttpStatusCode.Created);
        respCreacion.Headers.Location.Should().NotBeNull();
        respCreacion.Headers.Location!.ToString()
            .Should().Contain($"/api/v1/evaluaciones/asignaciones/{asignacion.Id}");

        var dtoCreado = await respCreacion.Content.ReadFromJsonAsync<EvaluacionDto>();
        dtoCreado.Should().NotBeNull();
        dtoCreado!.AsignacionEvaluacionId.Should().Be(asignacion.Id);
        dtoCreado.Estado.Should().Be("BORRADOR");
        dtoCreado.PuntajeTotalPonderado.Should().BeNull();
        dtoCreado.FechaEnvio.Should().BeNull();
        dtoCreado.ComentariosGenerales.Should().Be("Borrador inicial");
        dtoCreado.Calificaciones.Should().ContainSingle();
        dtoCreado.Version.Should().Be(1);
        asignacion.Estado.Should().Be(EstadoAsignacionEvaluacion.EnProgreso);

        await bloqueadorRubrica.Received(1)
            .BloquearPorPrimeraCalificacionAsync(rubrica.Id, Arg.Any<CancellationToken>());

        var respActualizacion = await cliente.PutAsJsonAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacion.Id}/borrador",
            new
            {
                calificaciones = new[]
                {
                    new
                    {
                        criterioEvaluacionId = criterio1.Id,
                        puntajeAsignado = 18.00m,
                        comentario = "Promedio verificado"
                    },
                    new
                    {
                        criterioEvaluacionId = criterio2.Id,
                        puntajeAsignado = 9.00m,
                        comentario = "Excelente liderazgo"
                    }
                },
                comentariosGenerales = "Borrador completo"
            });

        respActualizacion.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtoActualizado = await respActualizacion.Content.ReadFromJsonAsync<EvaluacionDto>();
        dtoActualizado.Should().NotBeNull();
        dtoActualizado!.Estado.Should().Be("BORRADOR");
        dtoActualizado.Calificaciones.Should().HaveCount(2);
        dtoActualizado.ComentariosGenerales.Should().Be("Borrador completo");
        dtoActualizado.Version.Should().Be(2);
    }

    [Fact]
    public async Task Debe_Retornar200OkConPuntajeTotalPonderadoYEstadoEnviadaDefinitiva_Cuando_EvaluadorEnviaEvaluacionCompleta()
    {
        var (comite, rubrica, criterio1, criterio2, asignacion, evaluadorId, ahora) = CrearEscenario(
            EstadoRubrica.Bloqueada);

        asignacion.IniciarEvaluacion(ahora.AddMinutes(-5), evaluadorId);
        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [
                new DatosCalificacionCriterio(criterio1.Id, 15.00m, "C1"),
                new DatosCalificacionCriterio(criterio2.Id, 8.00m, "C2")
            ],
            "Evaluación lista para envío",
            ahora.AddMinutes(-5),
            evaluadorId);

        var repoAsignacion = Substitute.For<IConsultaAsignacionParaEvaluacionRepository>();
        repoAsignacion.ObtenerParaActualizarPorIdAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(asignacion);

        var repoEvaluacion = Substitute.For<IEvaluacionRepository>();
        repoEvaluacion.ObtenerParaActualizarPorAsignacionAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(evaluacion);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoRubrica = Substitute.For<IConsultaRubricaParaEvaluacionRepository>();
        repoRubrica.ObtenerPorProgramaYConvocatoriaAsync(
                comite.ProgramaBecaId,
                comite.ConvocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns(rubrica);

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(
            repoAsignacion: repoAsignacion,
            repoComites: repoComites,
            repoRubrica: repoRubrica,
            repoEvaluacion: repoEvaluacion,
            uow: uow);
        using var cliente = CrearClienteAutenticado(fabrica, evaluadorId, RolMiembroComite);

        var respuesta = await cliente.PostAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacion.Id}/enviar",
            content: null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await respuesta.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("estado").GetString().Should().Be("ENVIADA_DEFINITIVA");

        var dto = await respuesta.Content.ReadFromJsonAsync<EvaluacionDto>();
        dto.Should().NotBeNull();
        dto!.Estado.Should().Be("ENVIADA_DEFINITIVA");
        dto.PuntajeTotalPonderado.Should().Be(77.00m);
        dto.FechaEnvio.Should().NotBeNull();
        asignacion.Estado.Should().Be(EstadoAsignacionEvaluacion.Completada);

        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar200OkEnEvaluacionPropiaY403EvaluacionConfidencialEnAsignacionAjena_Cuando_EvaluadorConsultaPorAsignacion()
    {
        var (_, rubrica, criterio1, _, asignacion, evaluadorId, ahora) = CrearEscenario();
        var otroEvaluadorId = Guid.NewGuid();

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(criterio1.Id, 16.00m, "Observación")],
            "Comentario propio",
            ahora,
            evaluadorId);

        var repoAsignacion = Substitute.For<IConsultaAsignacionParaEvaluacionRepository>();
        repoAsignacion.ObtenerPorIdAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(asignacion);
        repoAsignacion.ObtenerParaActualizarPorIdAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(asignacion);

        var repoEvaluacion = Substitute.For<IEvaluacionRepository>();
        repoEvaluacion.ObtenerPorAsignacionAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(evaluacion);

        using var fabrica = CrearFabrica(
            repoAsignacion: repoAsignacion,
            repoEvaluacion: repoEvaluacion);

        using var clientePropio = CrearClienteAutenticado(fabrica, evaluadorId, RolMiembroComite);
        var respPropia = await clientePropio.GetAsync($"/api/v1/evaluaciones/asignaciones/{asignacion.Id}");
        respPropia.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtoPropio = await respPropia.Content.ReadFromJsonAsync<EvaluacionDto>();
        dtoPropio.Should().NotBeNull();
        dtoPropio!.Id.Should().Be(evaluacion.Id);
        dtoPropio.Estado.Should().Be("BORRADOR");

        using var clienteAjeno = CrearClienteAutenticado(fabrica, otroEvaluadorId, RolMiembroComite);
        var respGetAjena = await clienteAjeno.GetAsync($"/api/v1/evaluaciones/asignaciones/{asignacion.Id}");
        respGetAjena.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await VerificarCodigoErrorAsync(respGetAjena, EvaluacionConfidencialException.CodigoError);

        var respPutAjena = await clienteAjeno.PutAsJsonAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacion.Id}/borrador",
            new
            {
                calificaciones = new[]
                {
                    new { criterioEvaluacionId = criterio1.Id, puntajeAsignado = 10.00m }
                }
            });
        respPutAjena.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await VerificarCodigoErrorAsync(respPutAjena, EvaluacionConfidencialException.CodigoError);

        var respEnviarAjena = await clienteAjeno.PostAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacion.Id}/enviar",
            content: null);
        respEnviarAjena.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await VerificarCodigoErrorAsync(respEnviarAjena, EvaluacionConfidencialException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar200OkConMisEvaluacionesPaginadasYFiltradasPorEstado_Cuando_EvaluadorConsultaListadoPropio()
    {
        var (_, rubrica, criterio1, _, asignacion, evaluadorId, ahora) = CrearEscenario();
        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(criterio1.Id, 14.00m, null)],
            null,
            ahora,
            evaluadorId);

        var repoEvaluacion = Substitute.For<IEvaluacionRepository>();
        repoEvaluacion.ListarPorEvaluadorAsync(
                evaluadorId,
                EstadoEvaluacion.Borrador,
                1,
                10,
                Arg.Any<CancellationToken>())
            .Returns(new PaginaResultado<Evaluacion>([evaluacion], 1));

        using var fabrica = CrearFabrica(repoEvaluacion: repoEvaluacion);
        using var cliente = CrearClienteAutenticado(fabrica, evaluadorId, RolMiembroComite);

        var respuesta = await cliente.GetAsync(
            "/api/v1/evaluaciones/mis-evaluaciones?estado=BORRADOR&pagina=1&tamano=10");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagina = await respuesta.Content.ReadFromJsonAsync<PaginaDto<ResumenMiEvaluacionDto>>();
        pagina.Should().NotBeNull();
        pagina!.Total.Should().Be(1);
        pagina.Pagina.Should().Be(1);
        pagina.Tamano.Should().Be(10);
        pagina.Items.Should().ContainSingle();
        pagina.Items[0].EvaluacionId.Should().Be(evaluacion.Id);
        pagina.Items[0].AsignacionEvaluacionId.Should().Be(asignacion.Id);
        pagina.Items[0].Estado.Should().Be("BORRADOR");
        pagina.Items[0].CantidadCriteriosCalificados.Should().Be(1);
    }

    [Fact]
    public async Task Debe_Retornar200OkSoloConEvaluacionesDefinitivasExcluyendoBorradores_Cuando_CoordinadorConsultaPorPostulacion()
    {
        var (comite, rubrica, criterio1, criterio2, asignacion1, evaluador1Id, ahora) = CrearEscenario(
            EstadoRubrica.Bloqueada);
        var miembro2 = comite.Miembros.First(m => m.UsuarioEvaluadorId != evaluador1Id);
        var evaluador2Id = miembro2.UsuarioEvaluadorId;
        var postulacionId = asignacion1.PostulacionId;
        var asignacion2 = AsignacionEvaluacion.Crear(
            comite,
            miembro2.Id,
            postulacionId,
            ahora.AddMinutes(-25),
            asignacion1.FechaLimiteEvaluacion,
            Guid.NewGuid());

        var evaluacionDefinitiva = Evaluacion.CrearBorrador(
            asignacion1,
            rubrica,
            [
                new DatosCalificacionCriterio(criterio1.Id, 20.00m, "C1"),
                new DatosCalificacionCriterio(criterio2.Id, 10.00m, "C2")
            ],
            "Definitiva",
            ahora.AddMinutes(-10),
            evaluador1Id);
        evaluacionDefinitiva.EnviarDefinitiva(
            rubrica,
            asignacion1.FechaLimiteEvaluacion,
            ahora.AddMinutes(-5),
            evaluador1Id);

        var evaluacionBorrador = Evaluacion.CrearBorrador(
            asignacion2,
            rubrica,
            [new DatosCalificacionCriterio(criterio1.Id, 12.00m, "Parcial")],
            "Borrador privado",
            ahora.AddMinutes(-8),
            evaluador2Id);

        var repoEvaluacion = Substitute.For<IEvaluacionRepository>();
        repoEvaluacion.ListarDefinitivasPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([evaluacionDefinitiva, evaluacionBorrador]);

        using var fabrica = CrearFabrica(repoEvaluacion: repoEvaluacion);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.GetAsync($"/api/v1/evaluaciones/postulaciones/{postulacionId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<EvaluacionesPostulacionDto>();
        dto.Should().NotBeNull();
        dto!.PostulacionId.Should().Be(postulacionId);
        dto.Evaluaciones.Should().ContainSingle();
        dto.Evaluaciones[0].Id.Should().Be(evaluacionDefinitiva.Id);
        dto.Evaluaciones[0].EvaluadorId.Should().Be(evaluador1Id);
        dto.Evaluaciones[0].Estado.Should().Be("ENVIADA_DEFINITIVA");
        dto.Evaluaciones[0].PuntajeTotalPonderado.Should().Be(100.00m);
    }

    [Theory]
    [InlineData("?estado=INVALIDO")]
    [InlineData("?pagina=0")]
    [InlineData("?tamano=0")]
    [InlineData("?tamano=101")]
    public async Task Debe_Retornar400BadRequest_Cuando_MisEvaluacionesRecibeParametrosInvalidos(string queryString)
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolMiembroComite);

        var respuesta = await cliente.GetAsync($"/api/v1/evaluaciones/mis-evaluaciones{queryString}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Debe_Retornar400BadRequest_Cuando_SolicitudesDeEvaluacionTienenDatosInvalidos()
    {
        using var fabrica = CrearFabrica();
        using var clienteEvaluador = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolMiembroComite);
        using var clienteCoordinador = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respPutVacio = await clienteEvaluador.PutAsJsonAsync(
            $"/api/v1/evaluaciones/asignaciones/{Guid.NewGuid()}/borrador",
            new { calificaciones = Array.Empty<object>() });
        respPutVacio.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respEnviarGuidVacio = await clienteEvaluador.PostAsync(
            $"/api/v1/evaluaciones/asignaciones/{Guid.Empty}/enviar",
            content: null);
        respEnviarGuidVacio.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respGetGuidVacio = await clienteEvaluador.GetAsync(
            $"/api/v1/evaluaciones/asignaciones/{Guid.Empty}");
        respGetGuidVacio.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respPostulacionGuidVacio = await clienteCoordinador.GetAsync(
            $"/api/v1/evaluaciones/postulaciones/{Guid.Empty}");
        respPostulacionGuidVacio.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respCoevaluadoresGuidVacio = await clienteEvaluador.GetAsync(
            $"/api/v1/evaluaciones/postulaciones/{Guid.Empty}/coevaluadores");
        respCoevaluadoresGuidVacio.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Debe_Retornar200OkConEvaluacionesDefinitivasDeCoevaluadoresSinIdentidadDelPar_Cuando_PeriodoRevisionHaCerrado()
    {
        var (comite, rubrica, criterio1, criterio2, asignacionPropia, evaluadorId, ahora) = CrearEscenario(
            EstadoRubrica.Bloqueada);
        var postulacionId = asignacionPropia.PostulacionId;
        var miembro2 = comite.Miembros.First(m => m.UsuarioEvaluadorId != evaluadorId);
        var coevaluadorId = miembro2.UsuarioEvaluadorId;
        var asignacionCoevaluador = AsignacionEvaluacion.Crear(
            comite,
            miembro2.Id,
            postulacionId,
            ahora.AddDays(-5),
            ahora.AddDays(5),
            Guid.NewGuid());

        var evaluacionCoevaluador = Evaluacion.CrearBorrador(
            asignacionCoevaluador,
            rubrica,
            [
                new DatosCalificacionCriterio(criterio1.Id, 18.00m, "Excelente promedio"),
                new DatosCalificacionCriterio(criterio2.Id, 9.00m, "Gran proyección")
            ],
            "Evaluación definitiva del coevaluador",
            ahora.AddHours(-4),
            coevaluadorId);
        evaluacionCoevaluador.EnviarDefinitiva(
            rubrica,
            asignacionCoevaluador.FechaLimiteEvaluacion,
            ahora.AddHours(-3),
            coevaluadorId);

        var fechaCierreRevision = ahora.AddHours(-1);
        var repoCoevaluadores = Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>();
        repoCoevaluadores.ObtenerAsignacionVigenteEvaluadorAsync(
                postulacionId,
                evaluadorId,
                Arg.Any<CancellationToken>())
            .Returns(asignacionPropia);
        repoCoevaluadores.ListarDefinitivasDeCoevaluadoresAsync(
                postulacionId,
                evaluadorId,
                Arg.Any<CancellationToken>())
            .Returns([evaluacionCoevaluador]);

        var convocatoriaClient = Substitute.For<IConvocatoriaEvaluacionClient>();
        convocatoriaClient.ObtenerVentanaRevisionAsync(
                asignacionPropia.ConvocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(
                asignacionPropia.ConvocatoriaId,
                EstadoConvocatoriaExterna.Cerrada,
                ahora.AddDays(-10),
                fechaCierreRevision));

        using var fabrica = CrearFabrica(
            repoCoevaluadores: repoCoevaluadores,
            convocatoriaClient: convocatoriaClient);
        using var cliente = CrearClienteAutenticado(fabrica, evaluadorId, RolMiembroComite);

        var respuesta = await cliente.GetAsync(
            $"/api/v1/evaluaciones/postulaciones/{postulacionId}/coevaluadores");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await respuesta.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var itemJson = doc.RootElement.GetProperty("evaluaciones")[0];
        itemJson.TryGetProperty("evaluadorId", out _).Should().BeFalse();
        itemJson.TryGetProperty("asignacionEvaluacionId", out _).Should().BeFalse();

        var dto = await respuesta.Content.ReadFromJsonAsync<EvaluacionesCoevaluadoresPostulacionDto>();
        dto.Should().NotBeNull();
        dto!.PostulacionId.Should().Be(postulacionId);
        dto.ConvocatoriaId.Should().Be(asignacionPropia.ConvocatoriaId);
        dto.FechaCierreRevision.Should().Be(fechaCierreRevision.ToUniversalTime());
        dto.Evaluaciones.Should().ContainSingle();
        dto.Evaluaciones[0].EvaluacionId.Should().Be(evaluacionCoevaluador.Id);
        dto.Evaluaciones[0].Estado.Should().Be("ENVIADA_DEFINITIVA");
        dto.Evaluaciones[0].PuntajeTotalPonderado.Should().Be(90.00m);
        dto.Evaluaciones[0].Calificaciones.Should().HaveCount(2);
    }

    [Fact]
    public async Task Debe_Retornar200OkConListaVacia_Cuando_NoExistenEvaluacionesDefinitivasDeCoevaluadoresTrasCierre()
    {
        var (_, _, _, _, asignacionPropia, evaluadorId, ahora) = CrearEscenario();
        var postulacionId = asignacionPropia.PostulacionId;
        var fechaCierreRevision = ahora.AddMinutes(-30);

        var repoCoevaluadores = Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>();
        repoCoevaluadores.ObtenerAsignacionVigenteEvaluadorAsync(
                postulacionId,
                evaluadorId,
                Arg.Any<CancellationToken>())
            .Returns(asignacionPropia);
        repoCoevaluadores.ListarDefinitivasDeCoevaluadoresAsync(
                postulacionId,
                evaluadorId,
                Arg.Any<CancellationToken>())
            .Returns([]);

        var convocatoriaClient = Substitute.For<IConvocatoriaEvaluacionClient>();
        convocatoriaClient.ObtenerVentanaRevisionAsync(
                asignacionPropia.ConvocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(
                asignacionPropia.ConvocatoriaId,
                EstadoConvocatoriaExterna.Publicada,
                ahora.AddDays(-5),
                fechaCierreRevision));

        using var fabrica = CrearFabrica(
            repoCoevaluadores: repoCoevaluadores,
            convocatoriaClient: convocatoriaClient);
        using var cliente = CrearClienteAutenticado(fabrica, evaluadorId, RolMiembroComite);

        var respuesta = await cliente.GetAsync(
            $"/api/v1/evaluaciones/postulaciones/{postulacionId}/coevaluadores");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<EvaluacionesCoevaluadoresPostulacionDto>();
        dto.Should().NotBeNull();
        dto!.PostulacionId.Should().Be(postulacionId);
        dto.Evaluaciones.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_Retornar403404409Y503EnProblemDetails_Cuando_FallanReglasDeVentanaRevisionOCoevaluadores()
    {
        var (comite, rubrica, criterio1, _, asignacion, evaluadorId, ahora) = CrearEscenario();
        var postulacionId = asignacion.PostulacionId;
        var evaluadorNoAsignadoId = Guid.NewGuid();

        var repoAsignacion = Substitute.For<IConsultaAsignacionParaEvaluacionRepository>();
        repoAsignacion.ObtenerParaActualizarPorIdAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(asignacion);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoRubrica = Substitute.For<IConsultaRubricaParaEvaluacionRepository>();
        repoRubrica.ObtenerPorProgramaYConvocatoriaAsync(
                comite.ProgramaBecaId,
                comite.ConvocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns(rubrica);

        var repoEvaluacion = Substitute.For<IEvaluacionRepository>();

        var repoCoevaluadores = Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>();
        repoCoevaluadores.ObtenerAsignacionVigenteEvaluadorAsync(
                postulacionId,
                evaluadorId,
                Arg.Any<CancellationToken>())
            .Returns(asignacion);
        repoCoevaluadores.ObtenerAsignacionVigenteEvaluadorAsync(
                postulacionId,
                evaluadorNoAsignadoId,
                Arg.Any<CancellationToken>())
            .Returns((AsignacionEvaluacion?)null);

        var convocatoriaClient = Substitute.For<IConvocatoriaEvaluacionClient>();

        using var fabrica = CrearFabrica(
            repoAsignacion: repoAsignacion,
            repoComites: repoComites,
            repoRubrica: repoRubrica,
            repoEvaluacion: repoEvaluacion,
            repoCoevaluadores: repoCoevaluadores,
            convocatoriaClient: convocatoriaClient);

        // 1. 403 EVALUACION_CONFIDENCIAL en coevaluadores sin asignación vigente
        using var clienteNoAsignado = CrearClienteAutenticado(fabrica, evaluadorNoAsignadoId, RolMiembroComite);
        var respConfidencial = await clienteNoAsignado.GetAsync(
            $"/api/v1/evaluaciones/postulaciones/{postulacionId}/coevaluadores");
        respConfidencial.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await VerificarCodigoErrorAsync(respConfidencial, EvaluacionConfidencialException.CodigoError);

        using var cliente = CrearClienteAutenticado(fabrica, evaluadorId, RolMiembroComite);

        // 2. 404 CONVOCATORIA_NO_ENCONTRADA
        convocatoriaClient.ObtenerVentanaRevisionAsync(
                asignacion.ConvocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns((VentanaRevisionConvocatoria?)null);

        var respConvocatoriaNoEncontrada = await cliente.GetAsync(
            $"/api/v1/evaluaciones/postulaciones/{postulacionId}/coevaluadores");
        respConvocatoriaNoEncontrada.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(
            respConvocatoriaNoEncontrada,
            ConvocatoriaNoEncontradaException.CodigoError);

        // 3. 409 PERIODO_REVISION_ABIERTO al consultar coevaluadores mientras la ventana sigue abierta
        convocatoriaClient.ObtenerVentanaRevisionAsync(
                asignacion.ConvocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(
                asignacion.ConvocatoriaId,
                EstadoConvocatoriaExterna.Publicada,
                ahora.AddDays(-2),
                ahora.AddDays(5)));

        var respPeriodoAbierto = await cliente.GetAsync(
            $"/api/v1/evaluaciones/postulaciones/{postulacionId}/coevaluadores");
        respPeriodoAbierto.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respPeriodoAbierto, PeriodoRevisionAbiertoException.CodigoError);

        // 4. 409 PERIODO_REVISION_CERRADO al guardar borrador después del cierre
        convocatoriaClient.ObtenerVentanaRevisionAsync(
                asignacion.ConvocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(
                asignacion.ConvocatoriaId,
                EstadoConvocatoriaExterna.Cerrada,
                ahora.AddDays(-10),
                ahora.AddMinutes(-5)));

        var respPeriodoCerrado = await cliente.PutAsJsonAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacion.Id}/borrador",
            new
            {
                calificaciones = new[]
                {
                    new { criterioEvaluacionId = criterio1.Id, puntajeAsignado = 15.00m }
                }
            });
        respPeriodoCerrado.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respPeriodoCerrado, PeriodoRevisionCerradoException.CodigoError);

        // 5. 409 PERIODO_REVISION_NO_INICIADO al guardar borrador antes del inicio
        convocatoriaClient.ObtenerVentanaRevisionAsync(
                asignacion.ConvocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(
                asignacion.ConvocatoriaId,
                EstadoConvocatoriaExterna.Borrador,
                ahora.AddDays(2),
                ahora.AddDays(10)));

        var respPeriodoNoIniciado = await cliente.PutAsJsonAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacion.Id}/borrador",
            new
            {
                calificaciones = new[]
                {
                    new { criterioEvaluacionId = criterio1.Id, puntajeAsignado = 15.00m }
                }
            });
        respPeriodoNoIniciado.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respPeriodoNoIniciado, PeriodoRevisionNoIniciadoException.CodigoError);

        // 6. 409 CONVOCATORIA_CANCELADA en consulta de coevaluadores y en envío
        convocatoriaClient.ObtenerVentanaRevisionAsync(
                asignacion.ConvocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns(new VentanaRevisionConvocatoria(
                asignacion.ConvocatoriaId,
                EstadoConvocatoriaExterna.Cancelada,
                ahora.AddDays(-5),
                ahora.AddDays(5)));

        var respConvocatoriaCancelada = await cliente.GetAsync(
            $"/api/v1/evaluaciones/postulaciones/{postulacionId}/coevaluadores");
        respConvocatoriaCancelada.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respConvocatoriaCancelada, ConvocatoriaCanceladaException.CodigoError);

        // 7. 503 DEPENDENCIA_NO_DISPONIBLE
        convocatoriaClient.ObtenerVentanaRevisionAsync(
                asignacion.ConvocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns<Task<VentanaRevisionConvocatoria?>>(_ => throw new DependenciaNoDisponibleException());

        var respDependenciaNoDisponible = await cliente.GetAsync(
            $"/api/v1/evaluaciones/postulaciones/{postulacionId}/coevaluadores");
        respDependenciaNoDisponible.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        await VerificarCodigoErrorAsync(
            respDependenciaNoDisponible,
            DependenciaNoDisponibleException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar404409Y422EnProblemDetailsConCodigosDeDominio_Cuando_FallanReglasDeNegocio()
    {
        var (comite, rubrica, criterio1, _, asignacion, evaluadorId, ahora) = CrearEscenario();

        var repoAsignacion = Substitute.For<IConsultaAsignacionParaEvaluacionRepository>();
        repoAsignacion.ObtenerParaActualizarPorIdAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(asignacion);
        repoAsignacion.ObtenerPorIdAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(asignacion);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoRubrica = Substitute.For<IConsultaRubricaParaEvaluacionRepository>();
        repoRubrica.ObtenerPorProgramaYConvocatoriaAsync(
                comite.ProgramaBecaId,
                comite.ConvocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns(rubrica);

        var repoEvaluacion = Substitute.For<IEvaluacionRepository>();

        using var fabrica = CrearFabrica(
            repoAsignacion: repoAsignacion,
            repoComites: repoComites,
            repoRubrica: repoRubrica,
            repoEvaluacion: repoEvaluacion);
        using var cliente = CrearClienteAutenticado(fabrica, evaluadorId, RolMiembroComite);

        // 404 ASIGNACION_NO_ENCONTRADA
        var respAsignacionInexistente = await cliente.GetAsync(
            $"/api/v1/evaluaciones/asignaciones/{Guid.NewGuid()}");
        respAsignacionInexistente.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(
            respAsignacionInexistente,
            AsignacionEvaluacionNoEncontradaException.CodigoError);

        // 404 EVALUACION_NO_ENCONTRADA
        var respEvaluacionInexistente = await cliente.GetAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacion.Id}");
        respEvaluacionInexistente.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(
            respEvaluacionInexistente,
            EvaluacionNoEncontradaException.CodigoError);

        // 422 PUNTAJE_FUERA_DE_RANGO
        var respFueraDeRango = await cliente.PutAsJsonAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacion.Id}/borrador",
            new
            {
                calificaciones = new[]
                {
                    new { criterioEvaluacionId = criterio1.Id, puntajeAsignado = 25.00m }
                }
            });
        respFueraDeRango.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(respFueraDeRango, PuntajeFueraDeRangoException.CodigoError);

        // 422 CRITERIO_NO_PERTENECE_A_RUBRICA
        var respCriterioAjeno = await cliente.PutAsJsonAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacion.Id}/borrador",
            new
            {
                calificaciones = new[]
                {
                    new { criterioEvaluacionId = Guid.NewGuid(), puntajeAsignado = 10.00m }
                }
            });
        respCriterioAjeno.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(
            respCriterioAjeno,
            CriterioNoPerteneceARubricaException.CodigoError);

        // 422 EVALUACION_INCOMPLETA al enviar borrador con 1 de 2 criterios
        var borradorIncompleto = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(criterio1.Id, 15.00m, null)],
            null,
            ahora,
            evaluadorId);
        repoEvaluacion.ObtenerParaActualizarPorAsignacionAsync(asignacion.Id, Arg.Any<CancellationToken>())
            .Returns(borradorIncompleto);

        var respIncompleta = await cliente.PostAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacion.Id}/enviar",
            content: null);
        respIncompleta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(respIncompleta, EvaluacionIncompletaException.CodigoError);

        // 409 EVALUACION_YA_ENVIADA
        var (comite2, rubrica2, c1, c2, asignacionEnviada, eval2Id, _) = CrearEscenario(EstadoRubrica.Bloqueada);
        asignacionEnviada.IniciarEvaluacion(ahora.AddMinutes(-4), eval2Id);
        var evaluacionEnviada = Evaluacion.CrearBorrador(
            asignacionEnviada,
            rubrica2,
            [
                new DatosCalificacionCriterio(c1.Id, 18.00m, null),
                new DatosCalificacionCriterio(c2.Id, 9.00m, null)
            ],
            null,
            ahora.AddMinutes(-4),
            eval2Id);
        evaluacionEnviada.EnviarDefinitiva(
            rubrica2,
            asignacionEnviada.FechaLimiteEvaluacion,
            ahora.AddMinutes(-2),
            eval2Id);
        asignacionEnviada.CompletarEvaluacion(ahora.AddMinutes(-2), eval2Id);

        repoComites.ObtenerPorIdAsync(comite2.Id, Arg.Any<CancellationToken>())
            .Returns(comite2);
        repoRubrica.ObtenerPorProgramaYConvocatoriaAsync(
                comite2.ProgramaBecaId,
                comite2.ConvocatoriaId,
                Arg.Any<CancellationToken>())
            .Returns(rubrica2);
        repoAsignacion.ObtenerParaActualizarPorIdAsync(asignacionEnviada.Id, Arg.Any<CancellationToken>())
            .Returns(asignacionEnviada);
        repoEvaluacion.ObtenerParaActualizarPorAsignacionAsync(asignacionEnviada.Id, Arg.Any<CancellationToken>())
            .Returns(evaluacionEnviada);

        using var clienteEval2 = CrearClienteAutenticado(fabrica, eval2Id, RolMiembroComite);
        var respReenviar = await clienteEval2.PostAsync(
            $"/api/v1/evaluaciones/asignaciones/{asignacionEnviada.Id}/enviar",
            content: null);
        respReenviar.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respReenviar, EvaluacionYaEnviadaException.CodigoError);
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IConsultaAsignacionParaEvaluacionRepository? repoAsignacion = null,
        IComiteEvaluadorRepository? repoComites = null,
        IConsultaRubricaParaEvaluacionRepository? repoRubrica = null,
        IEvaluacionRepository? repoEvaluacion = null,
        IConsultaEvaluacionesCoevaluadoresRepository? repoCoevaluadores = null,
        IConvocatoriaEvaluacionClient? convocatoriaClient = null,
        IBloqueadorRubricaEvaluacion? bloqueadorRubrica = null,
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
                servicios.RemoveAll<IBloqueadorRubricaEvaluacion>();
                servicios.RemoveAll<IPeriodoAcademicoClient>();
                servicios.RemoveAll<ICatalogoAcademicoClient>();
                servicios.RemoveAll<IConvocatoriaEvaluacionClient>();
                servicios.RemoveAll<IUnitOfWork>();

                var clienteConvocatoriaPorDefecto = Substitute.For<IConvocatoriaEvaluacionClient>();
                clienteConvocatoriaPorDefecto
                    .ObtenerVentanaRevisionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(callInfo =>
                    {
                        var ahora = DateTimeOffset.UtcNow;
                        return new VentanaRevisionConvocatoria(
                            callInfo.ArgAt<Guid>(0),
                            EstadoConvocatoriaExterna.Publicada,
                            ahora.AddDays(-10),
                            ahora.AddDays(30));
                    });

                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(repoComites ?? Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(repoEvaluacion ?? Substitute.For<IEvaluacionRepository>());
                servicios.AddSingleton(repoAsignacion ?? Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
                servicios.AddSingleton(repoRubrica ?? Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
                servicios.AddSingleton(repoCoevaluadores ?? Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>());
                servicios.AddSingleton(Substitute.For<IResultadoConsolidadoRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
                servicios.AddSingleton(bloqueadorRubrica ?? Substitute.For<IBloqueadorRubricaEvaluacion>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(convocatoriaClient ?? clienteConvocatoriaPorDefecto);
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
