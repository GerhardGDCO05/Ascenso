using System.Net;
using System.Net.Http.Json;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Renovacion.Comandos.EvaluarRenovacion;
using Ascenso.Becas.Application.Renovacion.Comandos.SolicitarRenovacion;
using Ascenso.Becas.Application.Renovacion.Consultas.ListarRenovacionesBeca;
using Ascenso.Becas.Application.Renovacion.Consultas.ObtenerRenovacion;
using Ascenso.Becas.Application.Renovacion.Dtos;
using Ascenso.Becas.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Controllers;

public sealed class RenovacionesControllerTests : IClassFixture<OfertasBecaApiFactory>
{
    private readonly OfertasBecaApiFactory _base;

    public RenovacionesControllerTests(OfertasBecaApiFactory fabrica)
        => _base = fabrica;

    [Fact]
    public async Task BecarioPropietarioSolicitaYAjenoRecibe403()
    {
        var becaId = Guid.CreateVersion7();
        var postulanteId = Guid.CreateVersion7();
        var solicitud = CrearSolicitud(becaId);
        var comandos = Substitute.For<ICommandDispatcher>();
        comandos.EnviarAsync<SolicitarRenovacionCommand, SolicitudRenovacionDto>(
            Arg.Any<SolicitarRenovacionCommand>(), Arg.Any<CancellationToken>())
            .Returns(solicitud);
        using var fabrica = CrearFabrica(becaId, postulanteId, comandos);
        using var propio = fabrica.CreateClient();
        Autenticar(propio, "BECARIO", postulanteId: postulanteId);
        var ruta = $"/api/v1/becas/{becaId}/renovaciones";

        var respuesta = await propio.PostAsJsonAsync(ruta,
            new
            {
                periodoSolicitadoId = solicitud.PeriodoSolicitadoId,
                montoSolicitado = 1600m
            });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        await comandos.Received(1).EnviarAsync<
            SolicitarRenovacionCommand, SolicitudRenovacionDto>(
            Arg.Is<SolicitarRenovacionCommand>(valor =>
                valor.BecaId == becaId
                && valor.PeriodoSolicitadoId == solicitud.PeriodoSolicitadoId),
            Arg.Any<CancellationToken>());

        using var ajeno = fabrica.CreateClient();
        Autenticar(ajeno, "BECARIO", postulanteId: Guid.CreateVersion7());
        var prohibida = await ajeno.PostAsJsonAsync(ruta,
            new
            {
                periodoSolicitadoId = solicitud.PeriodoSolicitadoId,
                montoSolicitado = 1600m
            });
        Assert.Equal(HttpStatusCode.Forbidden, prohibida.StatusCode);
    }

    [Fact]
    public async Task EvaluacionExigeCoordinadorConAlcanceYSegundoFactor()
    {
        var becaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var solicitud = CrearSolicitud(becaId);
        var comandos = Substitute.For<ICommandDispatcher>();
        comandos.EnviarAsync<EvaluarRenovacionCommand, SolicitudRenovacionDto>(
            Arg.Any<EvaluarRenovacionCommand>(), Arg.Any<CancellationToken>())
            .Returns(solicitud with { Estado = EstadoRenovacion.Rechazada });
        var consultas = Substitute.For<IQueryDispatcher>();
        consultas.ConsultarAsync<ObtenerRenovacionQuery, SolicitudRenovacionDto>(
            Arg.Any<ObtenerRenovacionQuery>(), Arg.Any<CancellationToken>())
            .Returns(solicitud);
        using var fabrica = CrearFabrica(becaId, Guid.CreateVersion7(),
            comandos, consultas, programaId);
        var ruta = $"/api/v1/renovaciones/{solicitud.Id}/evaluacion";
        var decision = new { aprobar = false, motivoDecision = "Incumplimiento comprobado" };

        using var sinFactor = fabrica.CreateClient();
        Autenticar(sinFactor, "COORDINADOR_PROGRAMA", programaId: programaId);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await sinFactor.PostAsJsonAsync(ruta, decision)).StatusCode);

        using var sinAlcance = fabrica.CreateClient();
        Autenticar(sinAlcance, "COORDINADOR_PROGRAMA",
            programaId: Guid.CreateVersion7(), con2Fa: true);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await sinAlcance.PostAsJsonAsync(ruta, decision)).StatusCode);

        using var coordinador = fabrica.CreateClient();
        Autenticar(coordinador, "COORDINADOR_PROGRAMA",
            programaId: programaId, con2Fa: true);
        Assert.Equal(HttpStatusCode.OK,
            (await coordinador.PostAsJsonAsync(ruta, decision)).StatusCode);
        await comandos.Received(1).EnviarAsync<
            EvaluarRenovacionCommand, SolicitudRenovacionDto>(
            Arg.Is<EvaluarRenovacionCommand>(valor =>
                valor.SolicitudRenovacionId == solicitud.Id && !valor.Aprobar),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsultaPaginadaRespetaPropiedad()
    {
        var becaId = Guid.CreateVersion7();
        var postulanteId = Guid.CreateVersion7();
        var solicitud = CrearSolicitud(becaId);
        var consultas = Substitute.For<IQueryDispatcher>();
        consultas.ConsultarAsync<ListarRenovacionesBecaQuery, PaginaRenovacionesDto>(
            Arg.Any<ListarRenovacionesBecaQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PaginaRenovacionesDto(2, 1, 1, [solicitud]));
        using var fabrica = CrearFabrica(becaId, postulanteId,
            Substitute.For<ICommandDispatcher>(), consultas);
        using var propio = fabrica.CreateClient();
        Autenticar(propio, "BECARIO", postulanteId: postulanteId);

        var respuesta = await propio.GetAsync(
            $"/api/v1/becas/{becaId}/renovaciones?pagina=2&tamano=1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await consultas.Received(1).ConsultarAsync<
            ListarRenovacionesBecaQuery, PaginaRenovacionesDto>(
            Arg.Is<ListarRenovacionesBecaQuery>(valor =>
                valor.BecaId == becaId && valor.Pagina == 2 && valor.Tamano == 1),
            Arg.Any<CancellationToken>());
        using var ajeno = fabrica.CreateClient();
        Autenticar(ajeno, "BECARIO", postulanteId: Guid.CreateVersion7());
        Assert.Equal(HttpStatusCode.Forbidden,
            (await ajeno.GetAsync(
                $"/api/v1/becas/{becaId}/renovaciones")).StatusCode);
    }

    private WebApplicationFactory<Program> CrearFabrica(Guid becaId,
        Guid postulanteId, ICommandDispatcher comandos,
        IQueryDispatcher? consultas = null, Guid? programaId = null)
        => _base.WithWebHostBuilder(builder => builder.ConfigureTestServices(servicios =>
        {
            var becas = Substitute.For<IConsultasBeca>();
            becas.ObtenerRecursoBecaAsync(becaId, Arg.Any<CancellationToken>())
                .Returns(new RecursoBecaDto(becaId,
                    programaId ?? Guid.CreateVersion7(), postulanteId));
            servicios.RemoveAll<IConsultasBeca>();
            servicios.AddSingleton(becas);
            servicios.RemoveAll<ICommandDispatcher>();
            servicios.AddSingleton(comandos);
            servicios.RemoveAll<IQueryDispatcher>();
            servicios.AddSingleton(consultas ?? Substitute.For<IQueryDispatcher>());
        }));

    private static SolicitudRenovacionDto CrearSolicitud(Guid becaId)
        => new(Guid.CreateVersion7(), becaId, Guid.CreateVersion7(),
            Guid.CreateVersion7(), new DateOnly(2026, 3, 15),
            EstadoRenovacion.SolicitadaEnCurso, 1600m, 0m,
            null, null, null);

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
}
