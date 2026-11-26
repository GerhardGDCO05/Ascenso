using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ascenso.Becas.Application.Renovacion.Puertos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.Renovacion.Eventos;
using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.Datos;
using Ascenso.Becas.Infrastructure.Trabajos;
using Ascenso.Becas.IntegrationTests.Persistencia;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.EndToEnd;

[Collection(ColeccionExtremoAExtremo.Nombre)]
public sealed class RenovacionExtremoAExtremoTests
{
    private readonly ExtremoAExtremoFixture _fixture;

    public RenovacionExtremoAExtremoTests(ExtremoAExtremoFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task SolicitudYRechazoConservanBecaSinSucesora()
    {
        var beca = await SembrarBecaAsync(conReporte: true);
        var periodoNuevo = Guid.CreateVersion7();
        using var baseFabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var fabrica = CrearFabrica(baseFabrica, beca,
            periodoNuevo, new DateTimeOffset(2026, 3, 1, 12, 0, 0,
                TimeSpan.Zero));
        using var becario = CrearCliente(fabrica, "BECARIO",
            postulanteId: beca.PostulanteEstudianteId);
        var solicitudId = await SolicitarAsync(becario, beca.Id, periodoNuevo);

        using var coordinador = CrearCliente(fabrica, "COORDINADOR_PROGRAMA",
            programaId: beca.ProgramaBecaId, con2Fa: true);
        var rechazo = await coordinador.PostAsJsonAsync(
            $"/api/v1/renovaciones/{solicitudId}/evaluacion",
            new { aprobar = false, motivoDecision = "Rendimiento insuficiente comprobado" });

        Assert.Equal(HttpStatusCode.OK, rechazo.StatusCode);
        await using var lectura = _fixture.CrearContexto();
        var actual = await lectura.Becas.SingleAsync(valor => valor.Id == beca.Id);
        Assert.Equal(EstadoBeca.Activa, actual.Estado);
        Assert.False(actual.TieneRenovacionEnCurso);
        Assert.False(await lectura.Becas.AnyAsync(valor =>
            valor.BecaAnteriorId == beca.Id));
        Assert.Equal(EstadoRenovacion.Rechazada,
            (await lectura.SolicitudesRenovacion.SingleAsync(valor =>
                valor.Id == solicitudId)).Estado);
        Assert.Contains(baseFabrica.Outbox.Eventos.OfType<
            RenovacionBecaSolicitadaEvento>(), valor =>
            valor.SolicitudRenovacionId == solicitudId);
    }

    [Fact]
    public async Task AprobacionSinReservaRealNoCreaSucesoraNiAlteraMontos()
    {
        var beca = await SembrarBecaAsync(conReporte: true);
        var periodoNuevo = Guid.CreateVersion7();
        using var baseFabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var fabrica = CrearFabrica(baseFabrica, beca,
            periodoNuevo, new DateTimeOffset(2026, 3, 1, 12, 0, 0,
                TimeSpan.Zero));
        using var becario = CrearCliente(fabrica, "BECARIO",
            postulanteId: beca.PostulanteEstudianteId);
        var solicitudId = await SolicitarAsync(becario, beca.Id, periodoNuevo);

        using var coordinador = CrearCliente(fabrica, "COORDINADOR_PROGRAMA",
            programaId: beca.ProgramaBecaId, con2Fa: true);
        var aprobacion = await coordinador.PostAsJsonAsync(
            $"/api/v1/renovaciones/{solicitudId}/evaluacion",
            new { aprobar = true, motivoDecision = "Cumple condiciones académicas" });

        Assert.Equal(HttpStatusCode.Conflict, aprobacion.StatusCode);
        await using var lectura = _fixture.CrearContexto();
        var actual = await lectura.Becas.SingleAsync(valor => valor.Id == beca.Id);
        Assert.Equal(EstadoBeca.Activa, actual.Estado);
        Assert.Equal(1500m, actual.MontoComprometidoPeriodoActual);
        Assert.Equal(EstadoRenovacion.SolicitadaEnCurso,
            (await lectura.SolicitudesRenovacion.SingleAsync(valor =>
                valor.Id == solicitudId)).Estado);
        Assert.False(await lectura.Becas.AnyAsync(valor =>
            valor.BecaAnteriorId == beca.Id));
    }

    [Fact]
    public async Task VencimientoSinSolicitudConcluyeSinCrearRenovacion()
    {
        var beca = await SembrarBecaAsync(conReporte: false);
        var periodoNuevo = Guid.CreateVersion7();
        var cupos = Substitute.For<ILiberacionCupoPrograma>();
        cupos.LiberarPorConclusionAsync(beca.ProgramaBecaId, beca.Id,
            beca.PeriodoVigenteActualId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(true));
        using var baseFabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var fabrica = CrearFabrica(baseFabrica, beca,
            periodoNuevo, new DateTimeOffset(2026, 3, 16, 12, 0, 0,
                TimeSpan.Zero), cupos);
        using var cliente = fabrica.CreateClient();
        using var alcance = fabrica.Services.CreateScope();
        var job = alcance.ServiceProvider.GetRequiredService<ConcluirBecaJob>();

        await job.Ejecutar(beca.Id, periodoNuevo);
        await job.Ejecutar(beca.Id, periodoNuevo);

        await using var lectura = _fixture.CrearContexto();
        Assert.Equal(EstadoBeca.Concluida,
            (await lectura.Becas.SingleAsync(valor => valor.Id == beca.Id)).Estado);
        Assert.False(await lectura.SolicitudesRenovacion.AnyAsync(valor =>
            valor.BecaId == beca.Id));
        Assert.False(await lectura.Becas.AnyAsync(valor =>
            valor.BecaAnteriorId == beca.Id));
        await cupos.Received(1).LiberarPorConclusionAsync(
            beca.ProgramaBecaId, beca.Id, beca.PeriodoVigenteActualId,
            Arg.Any<CancellationToken>());
    }

    private async Task<Beca> SembrarBecaAsync(bool conReporte)
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        await using var contexto = _fixture.CrearContexto();
        contexto.Becas.Add(beca);
        if (conReporte)
            contexto.ReportesAcademicos.Add(CrearReporteAprobado(beca));
        await contexto.SaveChangesAsync();
        return beca;
    }

    private static ReporteAcademico CrearReporteAprobado(Beca beca)
    {
        var contexto = DatosPrueba.CrearContextoOperacion();
        var reporte = ReporteAcademico.Registrar(new DatosReporteAcademico(
            beca.Id, beca.PeriodoVigenteActualId, 17m, 5, 0, 0, 20,
            EstadoMatricula.Matriculado, "gs://becas/reportes/notas.pdf",
            new string('a', 64), 1024, "application/pdf", FuenteReporte.Becario,
            Guid.CreateVersion7(), new DateOnly(2026, 3, 1), 40,
            "gs://becas/servicio/constancia.pdf", new string('b', 64)), contexto);
        reporte.IniciarRevision(contexto);
        reporte.Evaluar(beca.Oferta!.CondicionesSnapshot, contexto);
        reporte.DecidirConsecuencia(true, "Cumple las condiciones",
            "Solicitar renovación del periodo siguiente", contexto);
        reporte.ResolverRevision(true, contexto);
        return reporte;
    }

    private static WebApplicationFactory<Program> CrearFabrica(
        FabricaApiExtremoAExtremo fabrica, Beca beca, Guid periodoNuevo,
        DateTimeOffset fecha, ILiberacionCupoPrograma? cupos = null)
        => fabrica.WithWebHostBuilder(builder => builder.ConfigureTestServices(servicios =>
        {
            var reloj = Substitute.For<IClock>();
            reloj.UtcNow.Returns(fecha);
            var ventanas = Substitute.For<IVentanaRenovacionPrograma>();
            ventanas.ObtenerAsync(beca.ProgramaBecaId, periodoNuevo,
                Arg.Any<CancellationToken>())
                .Returns(new VentanaRenovacionPrograma(
                    new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 15),
                    true, false));
            var presupuestos = Substitute.For<IPresupuestoRenovacionPrograma>();
            presupuestos.TieneDisponibilidadAsync(beca.ProgramaBecaId,
                periodoNuevo, 1600m, Arg.Any<CancellationToken>())
                .Returns(true);
            servicios.RemoveAll<IClock>();
            servicios.AddSingleton(reloj);
            servicios.RemoveAll<IVentanaRenovacionPrograma>();
            servicios.AddSingleton(ventanas);
            servicios.RemoveAll<IPresupuestoRenovacionPrograma>();
            servicios.AddSingleton(presupuestos);
            servicios.RemoveAll<ILiberacionCupoPrograma>();
            servicios.AddSingleton(cupos ?? Substitute.For<ILiberacionCupoPrograma>());
        }));

    private static HttpClient CrearCliente(WebApplicationFactory<Program> fabrica,
        string rol, Guid? postulanteId = null, Guid? programaId = null,
        bool con2Fa = false)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Test-Roles", rol);
        if (postulanteId.HasValue)
            cliente.DefaultRequestHeaders.Add("X-Test-Postulante",
                postulanteId.Value.ToString());
        if (programaId.HasValue)
            cliente.DefaultRequestHeaders.Add("X-Test-Programas",
                programaId.Value.ToString());
        if (con2Fa)
            cliente.DefaultRequestHeaders.Add("X-Test-Acr", "urn:ascenso:loa:2fa");
        cliente.DefaultRequestHeaders.Add("Idempotency-Key",
            Guid.CreateVersion7().ToString());
        return cliente;
    }

    private static async Task<Guid> SolicitarAsync(HttpClient cliente,
        Guid becaId, Guid periodoNuevo)
    {
        var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/{becaId}/renovaciones",
            new { periodoSolicitadoId = periodoNuevo, montoSolicitado = 1600m });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        using var contenido = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        return contenido.RootElement.GetProperty("id").GetGuid();
    }
}
