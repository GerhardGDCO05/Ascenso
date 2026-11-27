using System.Net;
using System.Net.Http.Json;
using Ascenso.Becas.Api.Contexto;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Comandos.CicloVidaBeca;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Controllers;

public sealed class CicloVidaBecaControllerTests : IClassFixture<OfertasBecaApiFactory>
{
    private readonly OfertasBecaApiFactory _base;

    public CicloVidaBecaControllerTests(OfertasBecaApiFactory fabrica)
        => _base = fabrica;

    [Fact]
    public async Task DespachaLasTresTransicionesConAlcanceYOtpExplicito()
    {
        var becaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var comandos = Substitute.For<ICommandDispatcher>();
        var suspensionId = Guid.CreateVersion7();
        comandos.EnviarAsync<SuspenderBecaCommand, Guid>(
            Arg.Any<SuspenderBecaCommand>(), Arg.Any<CancellationToken>())
            .Returns(suspensionId);
        comandos.EnviarAsync<ReanudarBecaCommand, Guid>(
            Arg.Any<ReanudarBecaCommand>(), Arg.Any<CancellationToken>())
            .Returns(suspensionId);
        comandos.EnviarAsync(Arg.Any<RevocarBecaDefinitivamenteCommand>(),
            Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        using var fabrica = CrearFabrica(becaId, programaId, comandos);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, programaId);

        using var suspendida = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/{becaId}/suspender", new
            {
                motivo = "Pausa administrativa temporal",
                requisitosParaReanudar = "Presentar constancia",
                fechaInicio = "2026-03-01",
                fechaRevision = "2026-03-15",
                esIndefinida = false,
                justificacion = "Solicitud documentada"
            });
        using var reanudada = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/{becaId}/reanudar",
            new { justificacion = "Documentos verificados" });
        cliente.DefaultRequestHeaders.Add(ContextoOperacionHttp.EncabezadoAutorizacionOtp,
            Guid.CreateVersion7().ToString());
        using var revocada = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/{becaId}/revocar",
            new { justificacion = "Incumplimiento grave comprobado", codigoOtp = "123456" });

        Assert.Equal(HttpStatusCode.OK, suspendida.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reanudada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, revocada.StatusCode);
        await comandos.Received(1).EnviarAsync<SuspenderBecaCommand, Guid>(
            Arg.Is<SuspenderBecaCommand>(valor => valor.BecaId == becaId),
            Arg.Any<CancellationToken>());
        await comandos.Received(1).EnviarAsync<ReanudarBecaCommand, Guid>(
            Arg.Is<ReanudarBecaCommand>(valor => valor.BecaId == becaId),
            Arg.Any<CancellationToken>());
        await comandos.Received(1).EnviarAsync(
            Arg.Is<RevocarBecaDefinitivamenteCommand>(valor =>
                valor.BecaId == becaId && valor.CodigoOtp == "123456"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RechazaSinAlcanceSinDespachar()
    {
        var becaId = Guid.CreateVersion7();
        var comandos = Substitute.For<ICommandDispatcher>();
        using var fabrica = CrearFabrica(becaId, Guid.CreateVersion7(), comandos);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, Guid.CreateVersion7());

        using var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/{becaId}/reanudar",
            new { justificacion = "Reanudación documentada" });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        await comandos.DidNotReceive().EnviarAsync<ReanudarBecaCommand, Guid>(
            Arg.Any<ReanudarBecaCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RechazaSinSegundoFactor()
    {
        var becaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        using var fabrica = CrearFabrica(becaId, programaId);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, programaId, con2Fa: false);

        using var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/{becaId}/reanudar",
            new { justificacion = "Reanudación documentada" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task RevocacionRechazaAutorizacionOtpAusente()
    {
        var becaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var comandos = Substitute.For<ICommandDispatcher>();
        using var fabrica = CrearFabrica(becaId, programaId, comandos);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, programaId);

        using var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/{becaId}/revocar",
            new { justificacion = "Incumplimiento grave comprobado", codigoOtp = "123456" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        await comandos.DidNotReceive().EnviarAsync(
            Arg.Any<RevocarBecaDefinitivamenteCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MovimientoNoDisponibleSeExponeComo503()
    {
        var becaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var comandos = Substitute.For<ICommandDispatcher>();
        comandos.EnviarAsync<ReanudarBecaCommand, Guid>(
            Arg.Any<ReanudarBecaCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Guid>(
                new DependenciaNoDisponibleException("MS-PRG: movimientos-presupuesto")));
        using var fabrica = CrearFabrica(becaId, programaId, comandos);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, programaId);

        using var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/{becaId}/reanudar",
            new { justificacion = "Reanudación documentada" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
    }

    private WebApplicationFactory<Program> CrearFabrica(Guid becaId, Guid programaId,
        ICommandDispatcher? comandos = null)
        => _base.WithWebHostBuilder(builder => builder.ConfigureTestServices(servicios =>
        {
            var becas = Substitute.For<IConsultasBeca>();
            becas.ObtenerRecursoBecaAsync(becaId, Arg.Any<CancellationToken>())
                .Returns(new RecursoBecaDto(becaId, programaId, Guid.CreateVersion7()));
            servicios.RemoveAll<IConsultasBeca>();
            servicios.AddSingleton(becas);
            servicios.RemoveAll<ICommandDispatcher>();
            servicios.AddSingleton(comandos ?? Substitute.For<ICommandDispatcher>());
        }));

    private static void Autenticar(HttpClient cliente, Guid programaId,
        bool con2Fa = true)
    {
        cliente.DefaultRequestHeaders.Add("X-Test-Roles", "COORDINADOR_PROGRAMA");
        cliente.DefaultRequestHeaders.Add("X-Test-Programas", programaId.ToString());
        cliente.DefaultRequestHeaders.Add("Idempotency-Key", $"beca-api-{Guid.NewGuid():N}");
        if (con2Fa)
            cliente.DefaultRequestHeaders.Add("X-Test-Acr", "urn:ascenso:loa:2fa");
    }
}
