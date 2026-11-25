using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
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

public sealed class AsignacionesEvaluacionControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";
    private const string RolCoordinador = "COORDINADOR_PROGRAMA";
    private const string RolMiembroComite = "MIEMBRO_COMITE";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public AsignacionesEvaluacionControllerTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_Retornar201CreatedLocationYCodigosEnMayusculas_Cuando_AsignacionManualEsValida()
    {
        var coordinadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var evaluador1Id = Guid.NewGuid();
        var evaluador2Id = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;
        var fechaLimite = ahora.AddDays(10);

        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Asignación Manual",
            evaluador1Id,
            ahora.AddHours(-1),
            coordinadorId);
        var miembro2 = comite.AgregarMiembro(evaluador2Id, TipoEvaluador.Externo, ahora.AddMinutes(-30), coordinadorId);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoAsignaciones = Substitute.For<IAsignacionEvaluacionRepository>();
        repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([]);

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(repoComites, repoAsignaciones, uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/asignaciones-evaluacion",
            new
            {
                comiteId = comite.Id,
                postulacionId,
                fechaLimiteEvaluacion = fechaLimite,
                usuariosEvaluadoresIds = new[] { evaluador1Id, evaluador2Id }
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        respuesta.Headers.Location.Should().NotBeNull();
        respuesta.Headers.Location!.ToString()
            .Should().Contain($"/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}");

        var json = await respuesta.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var asignacionesJson = doc.RootElement.GetProperty("asignaciones");
        asignacionesJson.GetArrayLength().Should().Be(2);
        asignacionesJson[0].GetProperty("estado").GetString().Should().Be("PENDIENTE");
        asignacionesJson[1].GetProperty("estado").GetString().Should().Be("PENDIENTE");

        var dto = await respuesta.Content.ReadFromJsonAsync<AsignacionesPostulacionDto>();
        dto.Should().NotBeNull();
        dto!.PostulacionId.Should().Be(postulacionId);
        dto.Asignaciones.Should().HaveCount(2);
        dto.Asignaciones.Should().Contain(a =>
            a.ComiteEvaluadorId == comite.Id
            && a.ConvocatoriaId == comite.ConvocatoriaId
            && a.UsuarioEvaluadorId == evaluador1Id
            && a.Estado == "PENDIENTE"
            && a.Version == 1);
        dto.Asignaciones.Should().Contain(a =>
            a.MiembroComiteId == miembro2.Id
            && a.UsuarioEvaluadorId == evaluador2Id
            && a.Estado == "PENDIENTE");

        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar201CreatedPorMenorCarga_Cuando_AsignacionAutomaticaEsValida()
    {
        var coordinadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var evaluador1Id = Guid.NewGuid();
        var evaluador2Id = Guid.NewGuid();
        var evaluador3Id = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;

        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Asignación Automática",
            evaluador1Id,
            ahora.AddHours(-1),
            coordinadorId);
        var miembro1 = comite.Miembros.First();
        var miembro2 = comite.AgregarMiembro(evaluador2Id, TipoEvaluador.Interno, ahora.AddMinutes(-30), coordinadorId);
        var miembro3 = comite.AgregarMiembro(evaluador3Id, TipoEvaluador.Externo, ahora.AddMinutes(-20), coordinadorId);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoAsignaciones = Substitute.For<IAsignacionEvaluacionRepository>();
        repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([]);
        repoAsignaciones.ObtenerCargaPorMiembroAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>
            {
                [miembro1.Id] = 4,
                [miembro2.Id] = 1
            });

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(repoComites, repoAsignaciones, uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/asignaciones-evaluacion",
            new
            {
                comiteId = comite.Id,
                postulacionId,
                fechaLimiteEvaluacion = ahora.AddDays(7),
                cantidadEvaluadores = 2
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await respuesta.Content.ReadFromJsonAsync<AsignacionesPostulacionDto>();
        dto.Should().NotBeNull();
        dto!.Asignaciones.Select(a => a.MiembroComiteId)
            .Should().Equal(miembro3.Id, miembro2.Id);
        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar400BadRequest_Cuando_AsignarEvaluadoresTieneDatosInvalidos()
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/asignaciones-evaluacion",
            new
            {
                comiteId = Guid.Empty,
                postulacionId = Guid.Empty,
                fechaLimiteEvaluacion = default(DateTimeOffset)
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Debe_Retornar404ConComiteNoEncontrado_Cuando_ComiteNoExisteEnAsignacionYEnCarga()
    {
        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ComiteEvaluador?)null);

        using var fabrica = CrearFabrica(repoComites);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respAsignar = await cliente.PostAsJsonAsync(
            "/api/v1/asignaciones-evaluacion",
            new
            {
                comiteId = Guid.NewGuid(),
                postulacionId = Guid.NewGuid(),
                fechaLimiteEvaluacion = DateTimeOffset.UtcNow.AddDays(5),
                usuariosEvaluadoresIds = new[] { Guid.NewGuid(), Guid.NewGuid() }
            });
        respAsignar.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respAsignar, ComiteNoEncontradoException.CodigoError);

        var respCarga = await cliente.GetAsync(
            $"/api/v1/asignaciones-evaluacion/comites/{Guid.NewGuid()}/carga");
        respCarga.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respCarga, ComiteNoEncontradoException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar409ConAsignacionDuplicada_Cuando_EvaluadorYaEstaVigenteEnLaPostulacion()
    {
        var coordinadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var evaluador1Id = Guid.NewGuid();
        var evaluador2Id = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;

        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Duplicado",
            evaluador1Id,
            ahora.AddHours(-1),
            coordinadorId);
        var miembro1 = comite.Miembros.First();
        var miembro2 = comite.AgregarMiembro(evaluador2Id, TipoEvaluador.Interno, ahora.AddMinutes(-30), coordinadorId);

        var asignacionPrevia1 = AsignacionEvaluacion.Crear(
            comite,
            miembro1.Id,
            postulacionId,
            ahora.AddMinutes(-10),
            ahora.AddDays(5),
            coordinadorId);
        var asignacionPrevia2 = AsignacionEvaluacion.Crear(
            comite,
            miembro2.Id,
            postulacionId,
            ahora.AddMinutes(-9),
            ahora.AddDays(5),
            coordinadorId);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoAsignaciones = Substitute.For<IAsignacionEvaluacionRepository>();
        repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([asignacionPrevia1, asignacionPrevia2]);

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(repoComites, repoAsignaciones, uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/asignaciones-evaluacion",
            new
            {
                comiteId = comite.Id,
                postulacionId,
                fechaLimiteEvaluacion = ahora.AddDays(7),
                usuariosEvaluadoresIds = new[] { evaluador1Id }
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respuesta, AsignacionDuplicadaException.CodigoError);
        await uow.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar422ConEvaluacionesInsuficientes_Cuando_TotalResultanteEsMenorADos()
    {
        var coordinadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var evaluador1Id = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;

        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Insuficiente",
            evaluador1Id,
            ahora.AddHours(-1),
            coordinadorId);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoAsignaciones = Substitute.For<IAsignacionEvaluacionRepository>();
        repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([]);

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(repoComites, repoAsignaciones, uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/asignaciones-evaluacion",
            new
            {
                comiteId = comite.Id,
                postulacionId,
                fechaLimiteEvaluacion = ahora.AddDays(7),
                usuariosEvaluadoresIds = new[] { evaluador1Id, evaluador1Id }
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(respuesta, EvaluadoresInsuficientesException.CodigoError);
        await uow.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar422ConEvaluadorNoMiembroActivo_Cuando_UsuarioNoPerteneceAlComite()
    {
        var coordinadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var evaluador1Id = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;

        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Miembro",
            evaluador1Id,
            ahora.AddHours(-1),
            coordinadorId);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoAsignaciones = Substitute.For<IAsignacionEvaluacionRepository>();
        repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([]);

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(repoComites, repoAsignaciones, uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/asignaciones-evaluacion",
            new
            {
                comiteId = comite.Id,
                postulacionId,
                fechaLimiteEvaluacion = ahora.AddDays(7),
                usuariosEvaluadoresIds = new[] { evaluador1Id, Guid.NewGuid() }
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(respuesta, EvaluadorNoMiembroActivoException.CodigoError);
        await uow.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar422ConAsignacionInvalida_Cuando_FechaLimiteNoEsFutura()
    {
        var coordinadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var evaluador1Id = Guid.NewGuid();
        var evaluador2Id = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;

        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Fecha",
            evaluador1Id,
            ahora.AddHours(-1),
            coordinadorId);
        comite.AgregarMiembro(evaluador2Id, TipoEvaluador.Externo, ahora.AddMinutes(-30), coordinadorId);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoAsignaciones = Substitute.For<IAsignacionEvaluacionRepository>();
        repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([]);

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(repoComites, repoAsignaciones, uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/asignaciones-evaluacion",
            new
            {
                comiteId = comite.Id,
                postulacionId,
                fechaLimiteEvaluacion = ahora.AddMinutes(-15),
                usuariosEvaluadoresIds = new[] { evaluador1Id, evaluador2Id }
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(respuesta, AsignacionEvaluacionInvalidaException.CodigoError);
        await uow.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar422ConComiteInactivo_Cuando_PersistenciaLanzaComiteInactivoException()
    {
        var coordinadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var evaluador1Id = Guid.NewGuid();
        var evaluador2Id = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;

        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Inactivo",
            evaluador1Id,
            ahora.AddHours(-1),
            coordinadorId);
        comite.AgregarMiembro(evaluador2Id, TipoEvaluador.Externo, ahora.AddMinutes(-30), coordinadorId);
        typeof(ComiteEvaluador).GetProperty(nameof(ComiteEvaluador.Activo))!
            .SetValue(comite, false);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoAsignaciones = Substitute.For<IAsignacionEvaluacionRepository>();
        repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([]);

        var uow = Substitute.For<IUnitOfWork>();

        using var fabrica = CrearFabrica(repoComites, repoAsignaciones, uow);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/asignaciones-evaluacion",
            new
            {
                comiteId = comite.Id,
                postulacionId,
                fechaLimiteEvaluacion = ahora.AddDays(5),
                usuariosEvaluadoresIds = new[] { evaluador1Id, evaluador2Id }
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await VerificarCodigoErrorAsync(respuesta, ComiteInactivoException.CodigoError);
        await uow.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar200OkConAsignacionesPorPostulacionYCargaPorEvaluador_Cuando_CoordinadorConsulta()
    {
        var coordinadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var evaluador1Id = Guid.NewGuid();
        var evaluador2Id = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;

        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Consultas",
            evaluador1Id,
            ahora.AddHours(-1),
            coordinadorId);
        var miembro1 = comite.Miembros.First();
        var miembro2 = comite.AgregarMiembro(evaluador2Id, TipoEvaluador.Externo, ahora.AddMinutes(-30), coordinadorId);

        var asignacion1 = AsignacionEvaluacion.Crear(
            comite,
            miembro1.Id,
            postulacionId,
            ahora.AddMinutes(-10),
            ahora.AddDays(5),
            coordinadorId);
        var asignacion2 = AsignacionEvaluacion.Crear(
            comite,
            miembro2.Id,
            postulacionId,
            ahora.AddMinutes(-9),
            ahora.AddDays(5),
            coordinadorId);

        var repoComites = Substitute.For<IComiteEvaluadorRepository>();
        repoComites.ObtenerPorIdAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(comite);

        var repoAsignaciones = Substitute.For<IAsignacionEvaluacionRepository>();
        repoAsignaciones.ListarPorPostulacionAsync(postulacionId, Arg.Any<CancellationToken>())
            .Returns([asignacion1, asignacion2]);
        repoAsignaciones.ObtenerCargaPorMiembroAsync(comite.Id, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>
            {
                [miembro1.Id] = 2
            });

        using var fabrica = CrearFabrica(repoComites, repoAsignaciones);
        using var cliente = CrearClienteAutenticado(fabrica, coordinadorId, RolCoordinador);

        var respPostulacion = await cliente.GetAsync(
            $"/api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}");
        respPostulacion.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtoPostulacion = await respPostulacion.Content.ReadFromJsonAsync<AsignacionesPostulacionDto>();
        dtoPostulacion.Should().NotBeNull();
        dtoPostulacion!.PostulacionId.Should().Be(postulacionId);
        dtoPostulacion.Asignaciones.Should().HaveCount(2);
        dtoPostulacion.Asignaciones.All(a => a.Estado == "PENDIENTE").Should().BeTrue();

        var respCarga = await cliente.GetAsync(
            $"/api/v1/asignaciones-evaluacion/comites/{comite.Id}/carga");
        respCarga.StatusCode.Should().Be(HttpStatusCode.OK);
        var carga = await respCarga.Content.ReadFromJsonAsync<IReadOnlyList<CargaEvaluadorDto>>();
        carga.Should().NotBeNull();
        carga.Should().HaveCount(2);
        carga![0].MiembroComiteId.Should().Be(miembro2.Id);
        carga[0].Tipo.Should().Be("EXTERNO");
        carga[0].Rol.Should().Be("EVALUADOR");
        carga[0].AsignacionesAbiertas.Should().Be(0);
        carga[1].MiembroComiteId.Should().Be(miembro1.Id);
        carga[1].Tipo.Should().Be("INTERNO");
        carga[1].Rol.Should().Be("JEFATURA");
        carga[1].AsignacionesAbiertas.Should().Be(2);
    }

    [Fact]
    public async Task Debe_Retornar200OkSoloDelUsuarioDelTokenYSinCamposDeEvaluacionCiega_Cuando_MiembroConsultaMisAsignaciones()
    {
        var coordinadorId = Guid.NewGuid();
        var evaluadorAutenticadoId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;

        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Ciego",
            evaluadorAutenticadoId,
            ahora.AddHours(-1),
            coordinadorId);
        var miembro = comite.Miembros.First();

        var asignacion = AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            postulacionId,
            ahora.AddMinutes(-10),
            ahora.AddDays(5),
            coordinadorId);

        var repoAsignaciones = Substitute.For<IAsignacionEvaluacionRepository>();
        repoAsignaciones.ListarPorEvaluadorAsync(
                evaluadorAutenticadoId,
                EstadoAsignacionEvaluacion.Pendiente,
                1,
                10,
                Arg.Any<CancellationToken>())
            .Returns(new PaginaResultado<AsignacionEvaluacion>([asignacion], 1));

        using var fabrica = CrearFabrica(repoAsignaciones: repoAsignaciones);
        using var cliente = CrearClienteAutenticado(fabrica, evaluadorAutenticadoId, RolMiembroComite);

        var respuesta = await cliente.GetAsync(
            "/api/v1/asignaciones-evaluacion/mis-asignaciones?estado=PENDIENTE&pagina=1&tamano=10");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await respuesta.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var itemJson = doc.RootElement.GetProperty("items")[0];
        var nombresPropiedades = itemJson.EnumerateObject().Select(p => p.Name).ToArray();
        nombresPropiedades.Should().BeEquivalentTo([
            "asignacionEvaluacionId",
            "postulacionId",
            "convocatoriaId",
            "estado",
            "fechaAsignacion",
            "fechaLimiteEvaluacion"
        ]);

        var pagina = await respuesta.Content.ReadFromJsonAsync<PaginaDto<MiAsignacionDto>>();
        pagina.Should().NotBeNull();
        pagina!.Pagina.Should().Be(1);
        pagina.Tamano.Should().Be(10);
        pagina.Total.Should().Be(1);
        pagina.Items.Should().ContainSingle();
        pagina.Items[0].AsignacionEvaluacionId.Should().Be(asignacion.Id);
        pagina.Items[0].PostulacionId.Should().Be(postulacionId);
        pagina.Items[0].ConvocatoriaId.Should().Be(comite.ConvocatoriaId);
        pagina.Items[0].Estado.Should().Be("PENDIENTE");
    }

    [Theory]
    [InlineData("?estado=INVALIDO")]
    [InlineData("?pagina=0")]
    [InlineData("?tamano=0")]
    [InlineData("?tamano=101")]
    public async Task Debe_Retornar400BadRequest_Cuando_MisAsignacionesRecibeParametrosInvalidos(string queryString)
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolMiembroComite);

        var respuesta = await cliente.GetAsync($"/api/v1/asignaciones-evaluacion/mis-asignaciones{queryString}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IComiteEvaluadorRepository? repoComites = null,
        IAsignacionEvaluacionRepository? repoAsignaciones = null,
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
                servicios.RemoveAll<IPeriodoAcademicoClient>();
                servicios.RemoveAll<ICatalogoAcademicoClient>();
                servicios.RemoveAll<IUnitOfWork>();

                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(Substitute.For<IRubricaRepository>());
                servicios.AddSingleton(repoComites ?? Substitute.For<IComiteEvaluadorRepository>());
                servicios.AddSingleton(repoAsignaciones ?? Substitute.For<IAsignacionEvaluacionRepository>());
                servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
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
