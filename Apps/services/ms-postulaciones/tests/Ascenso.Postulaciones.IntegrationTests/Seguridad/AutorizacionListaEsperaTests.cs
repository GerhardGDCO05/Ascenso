using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Application.ListaEspera.DTOs;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Ascenso.Postulaciones.IntegrationTests.Seguridad;

public sealed class AutorizacionListaEsperaTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwt";
    private const string HeaderSub = "X-Test-Sub";
    private const string HeaderRol = "X-Test-Rol";
    private const string HeaderScope = "X-Test-Scope";
    private const string HeaderScp = "X-Test-Scp";
    private const string HeaderPermiso = "X-Test-Permiso";

    private readonly WebApplicationFactory<Program> _fabricaBase;

    public AutorizacionListaEsperaTests(WebApplicationFactory<Program> fabricaBase)
    {
        _fabricaBase = fabricaBase;
    }

    [Fact]
    public async Task Debe_RegistrarPoliticasDeListaEsperaConRolesScopesYPermisosDefinidos()
    {
        PoliticasAutorizacionListaEspera.ConsultaListaEspera.Should().Be("ConsultaListaEspera");
        PoliticasAutorizacionListaEspera.GestionListaEspera.Should().Be("GestionListaEspera");
        PoliticasAutorizacionListaEspera.RegistroVacanteListaEsperaInterna.Should().Be("RegistroVacanteListaEsperaInterna");
        PoliticasAutorizacionListaEspera.ScopeEscrituraInterna.Should().Be("postulaciones.internal.write");
        PoliticasAutorizacionListaEspera.PermisoEscrituraInterna.Should().Be("postulaciones:escribir");

        using var fabrica = CrearFabrica();
        using var scope = fabrica.Services.CreateScope();
        var proveedorPoliticas = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();
        var servicioAutorizacion = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var politicaConsulta = await proveedorPoliticas.GetPolicyAsync(PoliticasAutorizacionListaEspera.ConsultaListaEspera);
        var politicaGestion = await proveedorPoliticas.GetPolicyAsync(PoliticasAutorizacionListaEspera.GestionListaEspera);
        var politicaInterna = await proveedorPoliticas.GetPolicyAsync(PoliticasAutorizacionListaEspera.RegistroVacanteListaEsperaInterna);

        politicaConsulta.Should().NotBeNull();
        politicaGestion.Should().NotBeNull();
        politicaInterna.Should().NotBeNull();

        var coordinador = CrearPrincipal(rol: "COORDINADOR_PROGRAMA");
        var direccion = CrearPrincipal(rol: "DIRECCION");
        var miembroComite = CrearPrincipal(rol: "MIEMBRO_COMITE");
        var adminFinanciero = CrearPrincipal(rol: "ADMIN_FINANCIERO");
        var servicioScopeWrite = CrearPrincipal(scope: "openid postulaciones.internal.write profile");
        var servicioScpWrite = CrearPrincipal(scp: "postulaciones.internal.write");
        var servicioPermisoWrite = CrearPrincipal(permiso: "postulaciones:escribir");
        var servicioScopeRead = CrearPrincipal(scope: "postulaciones.internal.read");

        (await servicioAutorizacion.AuthorizeAsync(coordinador, null, politicaConsulta!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(direccion, null, politicaConsulta!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(miembroComite, null, politicaConsulta!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(adminFinanciero, null, politicaConsulta!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(servicioScopeWrite, null, politicaConsulta!)).Succeeded.Should().BeFalse();

        (await servicioAutorizacion.AuthorizeAsync(coordinador, null, politicaGestion!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(direccion, null, politicaGestion!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(miembroComite, null, politicaGestion!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(adminFinanciero, null, politicaGestion!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(servicioScopeWrite, null, politicaGestion!)).Succeeded.Should().BeFalse();

        (await servicioAutorizacion.AuthorizeAsync(coordinador, null, politicaInterna!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(servicioScopeWrite, null, politicaInterna!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(servicioScpWrite, null, politicaInterna!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(servicioPermisoWrite, null, politicaInterna!)).Succeeded.Should().BeTrue();
        (await servicioAutorizacion.AuthorizeAsync(direccion, null, politicaInterna!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(miembroComite, null, politicaInterna!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(adminFinanciero, null, politicaInterna!)).Succeeded.Should().BeFalse();
        (await servicioAutorizacion.AuthorizeAsync(servicioScopeRead, null, politicaInterna!)).Succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData(PoliticasAutorizacionListaEspera.ConsultaListaEspera)]
    [InlineData(PoliticasAutorizacionListaEspera.GestionListaEspera)]
    [InlineData(PoliticasAutorizacionListaEspera.RegistroVacanteListaEsperaInterna)]
    public async Task Debe_Retornar403ConPermisoDenegado_Cuando_EndpointTienePoliticaDeListaEsperaYResultadoEsForbidden(
        string nombrePolitica)
    {
        var manejador = new ManejadorResultadoAutorizacionRubricas();
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/api/v1/lista-espera/convocatorias/prueba";
        contexto.Response.Body = new MemoryStream();
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute(nombrePolitica)),
            "EndpointListaEsperaPrueba");
        contexto.SetEndpoint(endpoint);

        var politicaAutorizacion = new AuthorizationPolicyBuilder()
            .RequireRole(PoliticasAutorizacionListaEspera.RolCoordinadorPrograma)
            .Build();
        var resultadoEvaluacion = PolicyAuthorizationResult.Forbid();

        await manejador.HandleAsync(
            _ => Task.CompletedTask,
            contexto,
            politicaAutorizacion,
            resultadoEvaluacion);

        contexto.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        contexto.Response.Body.Seek(0, SeekOrigin.Begin);
        using var documento = await JsonDocument.ParseAsync(contexto.Response.Body);
        documento.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status403Forbidden);
        documento.RootElement.GetProperty("codigo").GetString()
            .Should().Be(ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
    }

    [Fact]
    public async Task Debe_AutorizarACoordinadorProgramaEnLecturaVacantePublicaEInternaYPromociones()
    {
        var (convocatoriaId, entrada1Id, fabrica) = CrearFabricaConEscenarioActivo();
        using var fabricaViva = fabrica;
        using var cliente = fabricaViva.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.CreateVersion7().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, "COORDINADOR_PROGRAMA");

        var respLista = await cliente.GetAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}");
        var respSiguiente = await cliente.GetAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/siguiente");
        var respVacantePublica = await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/vacantes-liberadas", null);
        var respVacanteInterna = await cliente.PostAsync($"/internal/v1/lista-espera/convocatorias/{convocatoriaId}/vacantes-liberadas", null);
        var respPromoverPorId = await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/entradas/{entrada1Id}/promover", null);
        var respPromoverSiguiente = await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/promover-siguiente", null);

        respLista.StatusCode.Should().Be(HttpStatusCode.OK);
        respSiguiente.StatusCode.Should().Be(HttpStatusCode.OK);
        respVacantePublica.StatusCode.Should().Be(HttpStatusCode.OK);
        respVacanteInterna.StatusCode.Should().Be(HttpStatusCode.OK);
        respPromoverPorId.StatusCode.Should().Be(HttpStatusCode.OK);
        respPromoverSiguiente.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Debe_AutorizarADireccionSoloEnLecturaYRechazarCon403PermisoDenegadoEnMutacionesPublicasEInternas()
    {
        var (convocatoriaId, entrada1Id, fabrica) = CrearFabricaConEscenarioActivo();
        using var fabricaViva = fabrica;
        using var cliente = fabricaViva.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.CreateVersion7().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, "DIRECCION");

        var respLista = await cliente.GetAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}");
        var respSiguiente = await cliente.GetAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/siguiente");

        respLista.StatusCode.Should().Be(HttpStatusCode.OK);
        respSiguiente.StatusCode.Should().Be(HttpStatusCode.OK);

        await Verificar403PermisoDenegadoAsync(
            await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/vacantes-liberadas", null));
        await Verificar403PermisoDenegadoAsync(
            await cliente.PostAsync($"/internal/v1/lista-espera/convocatorias/{convocatoriaId}/vacantes-liberadas", null));
        await Verificar403PermisoDenegadoAsync(
            await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/entradas/{entrada1Id}/promover", null));
        await Verificar403PermisoDenegadoAsync(
            await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/promover-siguiente", null));
    }

    [Theory]
    [InlineData("MIEMBRO_COMITE")]
    [InlineData("ADMIN_FINANCIERO")]
    public async Task Debe_RechazarCon403PermisoDenegadoEnTodosLosEndpoints_Cuando_RolNoEstaAutorizado(string rol)
    {
        var (convocatoriaId, entrada1Id, fabrica) = CrearFabricaConEscenarioActivo();
        using var fabricaViva = fabrica;
        using var cliente = fabricaViva.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderSub, Guid.CreateVersion7().ToString());
        cliente.DefaultRequestHeaders.Add(HeaderRol, rol);

        await Verificar403PermisoDenegadoAsync(
            await cliente.GetAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}"));
        await Verificar403PermisoDenegadoAsync(
            await cliente.GetAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/siguiente"));
        await Verificar403PermisoDenegadoAsync(
            await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/vacantes-liberadas", null));
        await Verificar403PermisoDenegadoAsync(
            await cliente.PostAsync($"/internal/v1/lista-espera/convocatorias/{convocatoriaId}/vacantes-liberadas", null));
        await Verificar403PermisoDenegadoAsync(
            await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/entradas/{entrada1Id}/promover", null));
        await Verificar403PermisoDenegadoAsync(
            await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/promover-siguiente", null));
    }

    [Theory]
    [InlineData(HeaderScope, "postulaciones.internal.write")]
    [InlineData(HeaderScp, "postulaciones.internal.write")]
    [InlineData(HeaderPermiso, "postulaciones:escribir")]
    public async Task Debe_AutorizarTokenInternoEscrituraSoloEnVacanteInternaYRechazar403EnEndpointsPublicos(
        string nombreHeader,
        string valorHeader)
    {
        var (convocatoriaId, entrada1Id, fabrica) = CrearFabricaConEscenarioActivo();
        using var fabricaViva = fabrica;
        using var cliente = fabricaViva.CreateClient();
        cliente.DefaultRequestHeaders.Add(nombreHeader, valorHeader);

        var respInterna = await cliente.PostAsync(
            $"/internal/v1/lista-espera/convocatorias/{convocatoriaId}/vacantes-liberadas",
            null);
        respInterna.StatusCode.Should().Be(HttpStatusCode.OK);

        await Verificar403PermisoDenegadoAsync(
            await cliente.GetAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}"));
        await Verificar403PermisoDenegadoAsync(
            await cliente.GetAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/siguiente"));
        await Verificar403PermisoDenegadoAsync(
            await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/vacantes-liberadas", null));
        await Verificar403PermisoDenegadoAsync(
            await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/entradas/{entrada1Id}/promover", null));
        await Verificar403PermisoDenegadoAsync(
            await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/promover-siguiente", null));
    }

    [Fact]
    public async Task Debe_RechazarCon403PermisoDenegadoTokenSoloLecturaInternaEnRegistroInternoDeVacante()
    {
        var (convocatoriaId, _, fabrica) = CrearFabricaConEscenarioActivo();
        using var fabricaViva = fabrica;
        using var cliente = fabricaViva.CreateClient();
        cliente.DefaultRequestHeaders.Add(HeaderScope, "postulaciones.internal.read");

        await Verificar403PermisoDenegadoAsync(
            await cliente.PostAsync($"/internal/v1/lista-espera/convocatorias/{convocatoriaId}/vacantes-liberadas", null));
    }

    [Fact]
    public async Task Debe_Retornar401UnauthorizedEnTodosLosEndpoints_Cuando_NoSeEnviaToken()
    {
        var (convocatoriaId, entrada1Id, fabrica) = CrearFabricaConEscenarioActivo();
        using var fabricaViva = fabrica;
        using var cliente = fabricaViva.CreateClient();

        (await cliente.GetAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await cliente.GetAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/siguiente"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/vacantes-liberadas", null))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await cliente.PostAsync($"/internal/v1/lista-espera/convocatorias/{convocatoriaId}/vacantes-liberadas", null))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/entradas/{entrada1Id}/promover", null))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await cliente.PostAsync($"/api/v1/lista-espera/convocatorias/{convocatoriaId}/promover-siguiente", null))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task Verificar403PermisoDenegadoAsync(HttpResponseMessage respuesta)
    {
        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        documento.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status403Forbidden);
        documento.RootElement.GetProperty("codigo").GetString()
            .Should().Be(ManejadorResultadoAutorizacionRubricas.CodigoPermisoDenegado);
    }

    private static ClaimsPrincipal CrearPrincipal(
        string? rol = null,
        string? scope = null,
        string? scp = null,
        string? permiso = null)
    {
        var claims = new List<Claim> { new("sub", Guid.CreateVersion7().ToString()) };
        if (!string.IsNullOrWhiteSpace(rol))
        {
            claims.Add(new Claim("roles", rol));
        }

        if (!string.IsNullOrWhiteSpace(scope))
        {
            claims.Add(new Claim("scope", scope));
        }

        if (!string.IsNullOrWhiteSpace(scp))
        {
            claims.Add(new Claim("scp", scp));
        }

        if (!string.IsNullOrWhiteSpace(permiso))
        {
            claims.Add(new Claim("permissions", permiso));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, EsquemaPrueba, "sub", "roles"));
    }

    private (Guid ConvocatoriaId, Guid Entrada1Id, WebApplicationFactory<Program> Fabrica) CrearFabricaConEscenarioActivo()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var p1 = Guid.CreateVersion7();
        var p2 = Guid.CreateVersion7();
        var fecha = new DateTimeOffset(2026, 11, 27, 9, 0, 0, TimeSpan.Zero);

        var e1 = EntradaListaEspera.Crear(convocatoriaId, programaId, p1, 1, 1500.00m, fecha, actorId);
        var e2 = EntradaListaEspera.Crear(convocatoriaId, programaId, p2, 2, 1400.00m, fecha, actorId);

        var repoLista = Substitute.For<IEntradaListaEsperaRepository>();
        repoLista.ListarPorConvocatoriaAsync(convocatoriaId, null, Arg.Any<CancellationToken>())
            .Returns([e1, e2]);
        repoLista.ObtenerSiguientePendientePorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(e1);

        var gestor = Substitute.For<IGestorConcurrenciaListaEspera>();
        gestor.EjecutarConBloqueoConvocatoriaAsync(
                convocatoriaId,
                Arg.Any<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<EntradaListaEsperaDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(llamada =>
            {
                var accion = llamada.ArgAt<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<EntradaListaEsperaDto>>>(1);
                return accion([e1, e2], llamada.ArgAt<CancellationToken>(2));
            });
        gestor.EjecutarConBloqueoConvocatoriaAsync(
                convocatoriaId,
                Arg.Any<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<PromocionListaEsperaDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(llamada =>
            {
                var accion = llamada.ArgAt<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<PromocionListaEsperaDto>>>(1);
                return accion([e1, e2], llamada.ArgAt<CancellationToken>(2));
            });

        var repoResultados = Substitute.For<IResultadoConsolidadoRepository>();
        repoResultados.ObtenerPorPostulacionAsync(p1, Arg.Any<CancellationToken>())
            .Returns(CrearResultado(convocatoriaId, p1, 3, 1500.00m));
        repoResultados.ObtenerPorPostulacionAsync(p2, Arg.Any<CancellationToken>())
            .Returns(CrearResultado(convocatoriaId, p2, 4, 1400.00m));

        var disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
        disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new DisponibilidadProgramaRanking(
                convocatoriaId,
                programaId,
                cuposAsignadosConvocatoria: 5,
                cuposDisponiblesPrograma: 3,
                presupuestoDisponibleBruto: 10000.00m,
                presupuestoComprometidoRenovaciones: 1000.00m,
                moneda: "USD"));

        return (convocatoriaId, e1.Id, CrearFabrica(repoLista, gestor, repoResultados, disponibilidadClient));
    }

    private static ResultadoConsolidadoRanking CrearResultado(
        Guid convocatoriaId,
        Guid postulacionId,
        int posicionRanking,
        decimal monto)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            convocatoriaId: convocatoriaId,
            postulacionId: postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: 84.00m,
            diferenciaEntreEvaluadores: 4.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: false);

        var resultado = ResultadoConsolidadoRanking.Crear(
            calculo,
            new DateTimeOffset(2026, 11, 27, 8, 0, 0, TimeSpan.Zero),
            Guid.CreateVersion7());

        resultado.ConfirmarRanking(
            posicionRanking,
            monto,
            ClasificacionRanking.ListaDeEspera,
            new DateTimeOffset(2026, 11, 27, 9, 0, 0, TimeSpan.Zero),
            Guid.CreateVersion7());

        return resultado;
    }

    private WebApplicationFactory<Program> CrearFabrica(
        IEntradaListaEsperaRepository? repoLista = null,
        IGestorConcurrenciaListaEspera? gestor = null,
        IResultadoConsolidadoRepository? repoResultados = null,
        IDisponibilidadRankingClient? disponibilidadClient = null)
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
                servicios.RemoveAll<IConsultaJefaturaComiteRepository>();
                servicios.RemoveAll<IVerificadorConflictoInteresDecisor>();
                servicios.RemoveAll<IEntradaListaEsperaRepository>();
                servicios.RemoveAll<IGestorConcurrenciaListaEspera>();
                servicios.RemoveAll<IDisponibilidadRankingClient>();
                servicios.RemoveAll<IProveedorMontoEstimadoBeca>();
                servicios.RemoveAll<IPeriodoAcademicoClient>();
                servicios.RemoveAll<ICatalogoAcademicoClient>();
                servicios.RemoveAll<IConvocatoriaEvaluacionClient>();
                servicios.RemoveAll<IUnitOfWork>();

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
                servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
                servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
                servicios.AddSingleton(Substitute.For<IRankingConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
                servicios.AddSingleton(Substitute.For<IConsultaJefaturaComiteRepository>());
                servicios.AddSingleton(Substitute.For<IVerificadorConflictoInteresDecisor>());
                servicios.AddSingleton(repoLista ?? Substitute.For<IEntradaListaEsperaRepository>());
                servicios.AddSingleton(gestor ?? Substitute.For<IGestorConcurrenciaListaEspera>());
                servicios.AddSingleton(disponibilidadClient ?? Substitute.For<IDisponibilidadRankingClient>());
                servicios.AddSingleton(Substitute.For<IProveedorMontoEstimadoBeca>());
                servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
                servicios.AddSingleton(Substitute.For<IConvocatoriaEvaluacionClient>());
                servicios.AddSingleton(Substitute.For<IUnitOfWork>());
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
            var tieneSub = Request.Headers.TryGetValue(HeaderSub, out var valoresSub);
            var tieneRol = Request.Headers.TryGetValue(HeaderRol, out var valoresRol);
            var tieneScope = Request.Headers.TryGetValue(HeaderScope, out var valoresScope);
            var tieneScp = Request.Headers.TryGetValue(HeaderScp, out var valoresScp);
            var tienePermiso = Request.Headers.TryGetValue(HeaderPermiso, out var valoresPermiso);

            if (!tieneSub && !tieneRol && !tieneScope && !tieneScp && !tienePermiso)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>();

            if (tieneSub)
            {
                claims.Add(new Claim("sub", valoresSub.FirstOrDefault() ?? string.Empty));
            }

            if (tieneRol)
            {
                foreach (var rol in valoresRol.Where(valor => !string.IsNullOrWhiteSpace(valor)))
                {
                    claims.Add(new Claim("roles", rol!));
                }
            }

            if (tieneScope)
            {
                foreach (var scope in valoresScope.Where(valor => !string.IsNullOrWhiteSpace(valor)))
                {
                    claims.Add(new Claim("scope", scope!));
                }
            }

            if (tieneScp)
            {
                foreach (var scp in valoresScp.Where(valor => !string.IsNullOrWhiteSpace(valor)))
                {
                    claims.Add(new Claim("scp", scp!));
                }
            }

            if (tienePermiso)
            {
                foreach (var permiso in valoresPermiso.Where(valor => !string.IsNullOrWhiteSpace(valor)))
                {
                    claims.Add(new Claim("permissions", permiso!));
                }
            }

            var identidad = new ClaimsIdentity(claims, EsquemaPrueba, "sub", "roles");
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identidad), EsquemaPrueba)));
        }
    }
}
