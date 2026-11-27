using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Renovacion.Comandos.ExpirarSolicitudRenovacion;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Trabajos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Trabajos;

public sealed class ReconciliarRenovacionesJobTests
{
    [Fact]
    public async Task ErrorHttpDeUnaReservaNoDetieneLaSolicitudSiguiente()
    {
        var primera = Guid.CreateVersion7();
        var segunda = Guid.CreateVersion7();
        var fecha = new DateOnly(2026, 3, 15);
        var solicitudes = Substitute.For<ISolicitudRenovacionRepository>();
        solicitudes.ObtenerEnCursoVencidasAsync(
                Arg.Any<DateOnly>(), Arg.Any<int>(), Arg.Any<DateOnly?>(),
                Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SolicitudRenovacionVencida>>(
                [new(primera, fecha), new(segunda, fecha)]));
        var despachador = Substitute.For<ICommandDispatcher>();
        despachador.EnviarAsync<ExpirarSolicitudRenovacionCommand, bool>(
                Arg.Any<ExpirarSolicitudRenovacionCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(llamada => llamada.ArgAt<ExpirarSolicitudRenovacionCommand>(0)
                .SolicitudId == primera
                ? Task.FromException<bool>(new HttpRequestException("MS-PRG no respondió"))
                : Task.FromResult(true));
        var reloj = Substitute.For<IClock>();
        reloj.UtcNow.Returns(new DateTimeOffset(2026, 3, 16, 12, 0, 0,
            TimeSpan.Zero));
        var job = new ReconciliarRenovacionesJob(solicitudes, despachador, reloj,
            Options.Create(new OpcionesTrabajosHangfire()),
            NullLogger<ReconciliarRenovacionesJob>.Instance);

        await job.Ejecutar();

        await despachador.Received(1)
            .EnviarAsync<ExpirarSolicitudRenovacionCommand, bool>(
                Arg.Is<ExpirarSolicitudRenovacionCommand>(valor =>
                    valor.SolicitudId == segunda), Arg.Any<CancellationToken>());
    }
}
