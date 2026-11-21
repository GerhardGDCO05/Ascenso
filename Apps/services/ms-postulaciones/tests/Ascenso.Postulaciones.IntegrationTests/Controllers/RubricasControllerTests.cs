using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Application.Rubricas.DTOs;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
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

public sealed class RubricasControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";
    private const string RolCoordinador = "COORDINADOR_PROGRAMA";
    private const string RolAdminFinanciero = "ADMIN_FINANCIERO";
    private const string RolMiembroComite = "MIEMBRO_COMITE";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public RubricasControllerTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_Retornar201CreatedYLocation_Cuando_CrearRubricaEsValida()
    {
        var usuarioId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var repo = Substitute.For<IRubricaRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        repo.ExistePorProgramaYConvocatoriaAsync(programaId, convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(false);

        using var fabrica = CrearFabrica(repo, uow);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/rubricas",
            new
            {
                programaBecaId = programaId,
                convocatoriaId,
                nombre = "Rúbrica Beca Excelencia 2026-II"
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        respuesta.Headers.Location.Should().NotBeNull();

        var dto = await respuesta.Content.ReadFromJsonAsync<RubricaDto>();
        dto.Should().NotBeNull();
        dto!.ProgramaBecaId.Should().Be(programaId);
        dto.ConvocatoriaId.Should().Be(convocatoriaId);
        dto.Nombre.Should().Be("Rúbrica Beca Excelencia 2026-II");
        dto.Estado.Should().Be("BORRADOR");
        dto.Criterios.Should().BeEmpty();
        dto.SumaPesos.Should().Be(0.00m);
        dto.DiferenciaPara100.Should().Be(100.00m);
        respuesta.Headers.Location!.ToString().Should().Contain($"/api/v1/rubricas/{dto.Id}");
        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar400BadRequest_Cuando_CrearRubricaEsInvalida()
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/rubricas",
            new
            {
                programaBecaId = Guid.Empty,
                convocatoriaId = Guid.Empty,
                nombre = ""
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Debe_Retornar401Unauthorized_Cuando_CrearRubricaNoTieneToken()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/rubricas",
            new
            {
                programaBecaId = Guid.NewGuid(),
                convocatoriaId = Guid.NewGuid(),
                nombre = "Rúbrica"
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(RolAdminFinanciero)]
    [InlineData(RolMiembroComite)]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_CrearRubricaConRolNoAutorizado(string rol)
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), rol);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/rubricas",
            new
            {
                programaBecaId = Guid.NewGuid(),
                convocatoriaId = Guid.NewGuid(),
                nombre = "Rúbrica"
            });

        await VerificarPermisoDenegadoAsync(respuesta);
    }

    [Fact]
    public async Task Debe_Retornar409ConflictConRubricaDuplicada_Cuando_YaExisteRubricaParaProgramaYConvocatoria()
    {
        var programaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var repo = Substitute.For<IRubricaRepository>();
        repo.ExistePorProgramaYConvocatoriaAsync(programaId, convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(true);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/rubricas",
            new
            {
                programaBecaId = programaId,
                convocatoriaId,
                nombre = "Rúbrica Duplicada"
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respuesta, RubricaDuplicadaException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar200Ok_Cuando_ObtenerRubricaExiste()
    {
        var usuarioId = Guid.NewGuid();
        var rubrica = CrearRubricaConCriterios(usuarioId, ("Mérito académico", 60.00m), ("Situación socioeconómica", 30.50m));
        var repo = Substitute.For<IRubricaRepository>();
        repo.ObtenerPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(rubrica);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.GetAsync($"/api/v1/rubricas/{rubrica.Id}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<RubricaDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(rubrica.Id);
        dto.Estado.Should().Be("BORRADOR");
        dto.Criterios.Should().HaveCount(2);
        dto.SumaPesos.Should().Be(90.50m);
        dto.DiferenciaPara100.Should().Be(9.50m);
    }

    [Fact]
    public async Task Debe_Retornar401Unauthorized_Cuando_ObtenerRubricaNoTieneToken()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync($"/api/v1/rubricas/{Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(RolAdminFinanciero)]
    [InlineData(RolMiembroComite)]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_ObtenerRubricaConRolNoAutorizado(string rol)
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), rol);

        var respuesta = await cliente.GetAsync($"/api/v1/rubricas/{Guid.NewGuid()}");

        await VerificarPermisoDenegadoAsync(respuesta);
    }

    [Fact]
    public async Task Debe_Retornar404NotFound_Cuando_ObtenerRubricaNoExiste()
    {
        var repo = Substitute.For<IRubricaRepository>();
        repo.ObtenerPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((RubricaEvaluacion?)null);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.GetAsync($"/api/v1/rubricas/{Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respuesta, RubricaNoEncontradaException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar201Created_Cuando_AgregarCriterioEsValido()
    {
        var usuarioId = Guid.NewGuid();
        var rubrica = CrearRubricaConCriterios(usuarioId);
        var repo = Substitute.For<IRubricaRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(rubrica);

        using var fabrica = CrearFabrica(repo, uow);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/rubricas/{rubrica.Id}/criterios",
            new
            {
                nombre = "Mérito académico",
                descripcion = "Promedio ponderado histórico",
                pesoPorcentual = 30.50m,
                puntajeMinimo = 0.00m,
                puntajeMaximo = 10.00m
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await respuesta.Content.ReadFromJsonAsync<RubricaDto>();
        dto.Should().NotBeNull();
        dto!.Criterios.Should().ContainSingle();
        dto.Criterios[0].Nombre.Should().Be("Mérito académico");
        dto.Criterios[0].Orden.Should().Be(1);
        dto.SumaPesos.Should().Be(30.50m);
        dto.DiferenciaPara100.Should().Be(69.50m);
        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar400BadRequest_Cuando_AgregarCriterioEsInvalido()
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/rubricas/{Guid.NewGuid()}/criterios",
            new
            {
                nombre = "",
                descripcion = "",
                pesoPorcentual = 0m,
                puntajeMinimo = 10m,
                puntajeMaximo = 5m
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Debe_Retornar401Unauthorized_Cuando_AgregarCriterioNoTieneToken()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/rubricas/{Guid.NewGuid()}/criterios",
            CrearPayloadCriterioValido());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(RolAdminFinanciero)]
    [InlineData(RolMiembroComite)]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_AgregarCriterioConRolNoAutorizado(string rol)
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), rol);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/rubricas/{Guid.NewGuid()}/criterios",
            CrearPayloadCriterioValido());

        await VerificarPermisoDenegadoAsync(respuesta);
    }

    [Fact]
    public async Task Debe_Retornar404NotFound_Cuando_AgregarCriterioARubricaInexistente()
    {
        var repo = Substitute.For<IRubricaRepository>();
        repo.ObtenerParaActualizarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((RubricaEvaluacion?)null);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/rubricas/{Guid.NewGuid()}/criterios",
            CrearPayloadCriterioValido());

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respuesta, RubricaNoEncontradaException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar409ConflictConRubricaBloqueada_Cuando_AgregarCriterioARubricaPublicada()
    {
        var usuarioId = Guid.NewGuid();
        var rubrica = CrearRubricaPublicada(usuarioId);
        var repo = Substitute.For<IRubricaRepository>();
        repo.ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(rubrica);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/rubricas/{rubrica.Id}/criterios",
            CrearPayloadCriterioValido());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respuesta, RubricaBloqueadaException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar200Ok_Cuando_EditarCriterioEsValido()
    {
        var usuarioId = Guid.NewGuid();
        var rubrica = CrearRubricaConCriterios(usuarioId, ("Criterio inicial", 30.00m));
        var criterioId = rubrica.Criterios.Single().Id;
        var repo = Substitute.For<IRubricaRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(rubrica);

        using var fabrica = CrearFabrica(repo, uow);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/v1/rubricas/{rubrica.Id}/criterios/{criterioId}",
            new
            {
                nombre = "Criterio editado",
                descripcion = "Descripción editada",
                pesoPorcentual = 45.25m,
                puntajeMinimo = 1.00m,
                puntajeMaximo = 15.00m
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<RubricaDto>();
        dto.Should().NotBeNull();
        dto!.Criterios.Should().ContainSingle();
        dto.Criterios[0].Nombre.Should().Be("Criterio editado");
        dto.Criterios[0].PesoPorcentual.Should().Be(45.25m);
        dto.SumaPesos.Should().Be(45.25m);
        dto.DiferenciaPara100.Should().Be(54.75m);
        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar400BadRequest_Cuando_EditarCriterioEsInvalido()
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/v1/rubricas/{Guid.NewGuid()}/criterios/{Guid.NewGuid()}",
            new
            {
                nombre = "",
                descripcion = "",
                pesoPorcentual = 150m,
                puntajeMinimo = -1m,
                puntajeMaximo = 10m
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Debe_Retornar401Unauthorized_Cuando_EditarCriterioNoTieneToken()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/v1/rubricas/{Guid.NewGuid()}/criterios/{Guid.NewGuid()}",
            CrearPayloadCriterioValido());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(RolAdminFinanciero)]
    [InlineData(RolMiembroComite)]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_EditarCriterioConRolNoAutorizado(string rol)
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), rol);

        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/v1/rubricas/{Guid.NewGuid()}/criterios/{Guid.NewGuid()}",
            CrearPayloadCriterioValido());

        await VerificarPermisoDenegadoAsync(respuesta);
    }

    [Fact]
    public async Task Debe_Retornar404NotFound_Cuando_EditarCriterioInexistente()
    {
        var usuarioId = Guid.NewGuid();
        var rubrica = CrearRubricaConCriterios(usuarioId, ("Criterio inicial", 30.00m));
        var repo = Substitute.For<IRubricaRepository>();
        repo.ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(rubrica);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/v1/rubricas/{rubrica.Id}/criterios/{Guid.NewGuid()}",
            CrearPayloadCriterioValido());

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respuesta, CriterioNoEncontradoException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar409ConflictConRubricaBloqueada_Cuando_EditarCriterioEnRubricaPublicada()
    {
        var usuarioId = Guid.NewGuid();
        var rubrica = CrearRubricaPublicada(usuarioId);
        var criterioId = rubrica.Criterios.First().Id;
        var repo = Substitute.For<IRubricaRepository>();
        repo.ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(rubrica);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/v1/rubricas/{rubrica.Id}/criterios/{criterioId}",
            CrearPayloadCriterioValido());

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respuesta, RubricaBloqueadaException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar200Ok_Cuando_EliminarCriterioEsValido()
    {
        var usuarioId = Guid.NewGuid();
        var rubrica = CrearRubricaConCriterios(usuarioId, ("Criterio 1", 60.00m), ("Criterio 2", 30.00m));
        var criterioId = rubrica.Criterios.First().Id;
        var repo = Substitute.For<IRubricaRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(rubrica);

        using var fabrica = CrearFabrica(repo, uow);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.DeleteAsync($"/api/v1/rubricas/{rubrica.Id}/criterios/{criterioId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<RubricaDto>();
        dto.Should().NotBeNull();
        dto!.Criterios.Should().ContainSingle();
        dto.SumaPesos.Should().Be(30.00m);
        dto.DiferenciaPara100.Should().Be(70.00m);
        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar401Unauthorized_Cuando_EliminarCriterioNoTieneToken()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.DeleteAsync($"/api/v1/rubricas/{Guid.NewGuid()}/criterios/{Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(RolAdminFinanciero)]
    [InlineData(RolMiembroComite)]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_EliminarCriterioConRolNoAutorizado(string rol)
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), rol);

        var respuesta = await cliente.DeleteAsync($"/api/v1/rubricas/{Guid.NewGuid()}/criterios/{Guid.NewGuid()}");

        await VerificarPermisoDenegadoAsync(respuesta);
    }

    [Fact]
    public async Task Debe_Retornar404NotFound_Cuando_EliminarCriterioInexistente()
    {
        var usuarioId = Guid.NewGuid();
        var rubrica = CrearRubricaConCriterios(usuarioId, ("Criterio 1", 60.00m));
        var repo = Substitute.For<IRubricaRepository>();
        repo.ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(rubrica);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.DeleteAsync($"/api/v1/rubricas/{rubrica.Id}/criterios/{Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respuesta, CriterioNoEncontradoException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar409ConflictConRubricaBloqueada_Cuando_EliminarCriterioEnRubricaPublicada()
    {
        var usuarioId = Guid.NewGuid();
        var rubrica = CrearRubricaPublicada(usuarioId);
        var criterioId = rubrica.Criterios.First().Id;
        var repo = Substitute.For<IRubricaRepository>();
        repo.ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(rubrica);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.DeleteAsync($"/api/v1/rubricas/{rubrica.Id}/criterios/{criterioId}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respuesta, RubricaBloqueadaException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar200Ok_Cuando_PublicarRubricaEsValido()
    {
        var usuarioId = Guid.NewGuid();
        var rubrica = CrearRubricaConCriterios(
            usuarioId,
            ("Criterio 1", 33.33m),
            ("Criterio 2", 33.33m),
            ("Criterio 3", 33.34m));
        var repo = Substitute.For<IRubricaRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(rubrica);

        using var fabrica = CrearFabrica(repo, uow);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.PostAsync($"/api/v1/rubricas/{rubrica.Id}/publicar", content: null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await respuesta.Content.ReadFromJsonAsync<RubricaDto>();
        dto.Should().NotBeNull();
        dto!.Estado.Should().Be("PUBLICADA");
        dto.FechaPublicacion.Should().NotBeNull();
        dto.SumaPesos.Should().Be(100.00m);
        dto.DiferenciaPara100.Should().Be(0.00m);
        await uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_Retornar401Unauthorized_Cuando_PublicarRubricaNoTieneToken()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsync($"/api/v1/rubricas/{Guid.NewGuid()}/publicar", content: null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(RolAdminFinanciero)]
    [InlineData(RolMiembroComite)]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_PublicarRubricaConRolNoAutorizado(string rol)
    {
        using var fabrica = CrearFabrica();
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), rol);

        var respuesta = await cliente.PostAsync($"/api/v1/rubricas/{Guid.NewGuid()}/publicar", content: null);

        await VerificarPermisoDenegadoAsync(respuesta);
    }

    [Fact]
    public async Task Debe_Retornar404NotFound_Cuando_PublicarRubricaInexistente()
    {
        var repo = Substitute.For<IRubricaRepository>();
        repo.ObtenerParaActualizarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((RubricaEvaluacion?)null);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, Guid.NewGuid(), RolCoordinador);

        var respuesta = await cliente.PostAsync($"/api/v1/rubricas/{Guid.NewGuid()}/publicar", content: null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await VerificarCodigoErrorAsync(respuesta, RubricaNoEncontradaException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar409ConflictConRubricaBloqueada_Cuando_PublicarRubricaYaPublicada()
    {
        var usuarioId = Guid.NewGuid();
        var rubrica = CrearRubricaPublicada(usuarioId);
        var repo = Substitute.For<IRubricaRepository>();
        repo.ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(rubrica);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.PostAsync($"/api/v1/rubricas/{rubrica.Id}/publicar", content: null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarCodigoErrorAsync(respuesta, RubricaBloqueadaException.CodigoError);
    }

    [Fact]
    public async Task Debe_Retornar422UnprocessableEntityConPesosInvalidos_Cuando_PublicarRubricaNoSuma100()
    {
        var usuarioId = Guid.NewGuid();
        var rubrica = CrearRubricaConCriterios(usuarioId, ("Criterio 1", 99.99m));
        var repo = Substitute.For<IRubricaRepository>();
        repo.ObtenerParaActualizarPorIdAsync(rubrica.Id, Arg.Any<CancellationToken>())
            .Returns(rubrica);

        using var fabrica = CrearFabrica(repo);
        using var cliente = CrearClienteAutenticado(fabrica, usuarioId, RolCoordinador);

        var respuesta = await cliente.PostAsync($"/api/v1/rubricas/{rubrica.Id}/publicar", content: null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        documento.RootElement.GetProperty("codigo").GetString()
            .Should().Be(PesosInvalidosException.CodigoError);
        documento.RootElement.GetProperty("detail").GetString()
            .Should().Contain("faltan 0.01");
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IRubricaRepository? repo = null,
        IUnitOfWork? uow = null)
    {
        return _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<IPostulanteRepository>();
                servicios.RemoveAll<IRubricaRepository>();
                servicios.RemoveAll<IPeriodoAcademicoClient>();
                servicios.RemoveAll<ICatalogoAcademicoClient>();
                servicios.RemoveAll<IUnitOfWork>();

                servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
                servicios.AddSingleton(repo ?? Substitute.For<IRubricaRepository>());
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

    private static RubricaEvaluacion CrearRubricaConCriterios(
        Guid usuarioId,
        params (string Nombre, decimal Peso)[] criterios)
    {
        var ahora = DateTimeOffset.UtcNow;
        var rubrica = RubricaEvaluacion.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Rúbrica de Evaluación",
            ahora,
            usuarioId);

        foreach (var (nombre, peso) in criterios)
        {
            rubrica.AgregarCriterio(
                nombre,
                $"Descripción de {nombre}",
                peso,
                0.00m,
                20.00m,
                ahora,
                usuarioId);
        }

        return rubrica;
    }

    private static RubricaEvaluacion CrearRubricaPublicada(Guid usuarioId)
    {
        var rubrica = CrearRubricaConCriterios(usuarioId, ("Criterio único", 100.00m));
        rubrica.Publicar(DateTimeOffset.UtcNow, usuarioId);
        return rubrica;
    }

    private static object CrearPayloadCriterioValido()
        => new
        {
            nombre = "Mérito académico",
            descripcion = "Promedio ponderado histórico",
            pesoPorcentual = 30.50m,
            puntajeMinimo = 0.00m,
            puntajeMaximo = 10.00m
        };

    private static async Task VerificarPermisoDenegadoAsync(HttpResponseMessage respuesta)
    {
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await VerificarCodigoErrorAsync(respuesta, ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
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
