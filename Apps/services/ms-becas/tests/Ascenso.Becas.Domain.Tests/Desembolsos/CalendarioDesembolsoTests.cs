using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Desembolsos.Datos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Transiciones;

namespace Ascenso.Becas.Domain.Tests.Desembolsos;

public sealed class CalendarioDesembolsoTests
{
    private static readonly DateOnly _inicio = new(2026, 9, 1);
    private static readonly DateOnly _fin = new(2026, 10, 31);
    private const string SnapshotCifrado = "v1:AAAAAAAAAAAAAAAA:AAAAAAAAAAAAAAAAAAAAAA==:AQ==";

    [Fact]
    public void ProgramaCuotasCuandoElTotalEsIgualAlTecho()
    {
        var calendario = Crear(100m, [new(1, _inicio, 40m), new(2, _fin, 60m)]);

        Assert.Equal(100m, calendario.SumaCuotasProgramadas);
        Assert.Equal(2, calendario.NumeroCuotas);
        Assert.Equal(EstadoPlanDesembolso.Programado, calendario.EstadoPlan);
        Assert.Equal([1, 2], calendario.Desembolsos.Select(cuota => cuota.NumeroCuota));
        Assert.All(calendario.Desembolsos, cuota => Assert.Equal(EstadoDesembolso.Programado, cuota.Estado));
    }

    [Fact]
    public void RechazaCuotasQueSuperanElTecho()
    {
        var error = Assert.Throws<ArgumentException>(() => Crear(99.99m, [new(1, _inicio, 40m), new(2, _fin, 60m)]));
        Assert.Contains("EXCEDE_OFERTA", error.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10.001)]
    public void RechazaMontosInvalidos(decimal monto)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Crear(100m, [new(1, _inicio, monto)]));
    }

    [Fact]
    public void RechazaTechoNegativoYCuotaSinMonto()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Crear(-1m, [new(1, _inicio, 1m)]));
        Assert.Throws<ArgumentException>(() => Crear(100m, [new(1, _inicio, 0m)]));
    }

    [Fact]
    public void RechazaNumerosDuplicadosYFechasFueraDelPeriodo()
    {
        Assert.Throws<ArgumentException>(() => Crear(100m, [new(1, _inicio, 20m), new(1, _fin, 20m)]));
        Assert.Throws<ArgumentException>(() => Crear(100m, [new(1, _fin.AddDays(1), 20m)]));
        Assert.Throws<ArgumentException>(() => Crear(100m, [new(1, _fin, 20m), new(2, _inicio, 20m)]));
    }

    [Fact]
    public void MantieneExactamenteLasTransicionesDelDdd()
    {
        var esperadas = new HashSet<(EstadoDesembolso, EstadoDesembolso)>
        {
            (EstadoDesembolso.Programado, EstadoDesembolso.EnProceso),
            (EstadoDesembolso.Programado, EstadoDesembolso.Suspendido),
            (EstadoDesembolso.Suspendido, EstadoDesembolso.Programado),
            (EstadoDesembolso.EnProceso, EstadoDesembolso.PagadoConfirmado),
            (EstadoDesembolso.EnProceso, EstadoDesembolso.Fallido),
            (EstadoDesembolso.EnProceso, EstadoDesembolso.Devuelto),
            (EstadoDesembolso.Fallido, EstadoDesembolso.Programado),
            (EstadoDesembolso.Devuelto, EstadoDesembolso.Programado),
            (EstadoDesembolso.Programado, EstadoDesembolso.Cancelado),
            (EstadoDesembolso.Suspendido, EstadoDesembolso.Cancelado)
        };

        Assert.Equal(esperadas.Count, TransicionesDesembolso.Tabla.Permitidas.Count);
        Assert.True(esperadas.SetEquals(TransicionesDesembolso.Tabla.Permitidas));
        Assert.Equal(
            [EstadoDesembolso.PagadoConfirmado, EstadoDesembolso.Cancelado],
            TransicionesDesembolso.Tabla.Terminales.OrderBy(estado => estado));

        foreach (var origen in Enum.GetValues<EstadoDesembolso>())
        {
            foreach (var destino in Enum.GetValues<EstadoDesembolso>())
            {
                Assert.Equal(esperadas.Contains((origen, destino)), TransicionesDesembolso.Tabla.EsPermitida(origen, destino));
            }
        }
    }

    [Fact]
    public void ReintentoConservaElDesembolsoAnteriorYExigeResolverIncidencia()
    {
        var calendario = Crear(100m, [new(1, _inicio, 100m)]);
        var contexto = new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var original = calendario.ObtenerCuotaVigente(1);
        calendario.RegistrarEjecucion(1, 100m, _inicio, DestinatarioPago.Becario,
            Guid.NewGuid(), SnapshotCifrado, MetodoPago.TransferenciaBancaria, "ABC-1", contexto);
        var incidencia = calendario.ReportarIncidencia(1, EstadoDesembolso.Fallido,
            "Cuenta rechazada", "El banco rechazó la transferencia", contexto);

        Assert.Throws<InvalidOperationException>(() => calendario.Reintentar(1, contexto));
        calendario.ResolverIncidencia(1, incidencia.Id, contexto);
        var reintento = calendario.Reintentar(1, contexto);

        Assert.Equal(original.Id, reintento.DesembolsoOrigenReintentoId);
        Assert.Equal(EstadoDesembolso.Fallido, original.Estado);
        Assert.False(original.EsVigente);
        Assert.True(reintento.EsVigente);
        Assert.Equal(2, reintento.IntentosEjecucion);
        Assert.Equal(2, calendario.Desembolsos.Count);
    }

    [Theory]
    [InlineData(EstadoDesembolso.Fallido)]
    [InlineData(EstadoDesembolso.Devuelto)]
    public void IncidenciaExigeObservacionBancaria(EstadoDesembolso estado)
    {
        var calendario = Crear(100m, [new(1, _inicio, 100m)]);
        var contexto = new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        calendario.RegistrarEjecucion(1, 100m, _inicio, DestinatarioPago.Becario,
            Guid.NewGuid(), SnapshotCifrado, MetodoPago.TransferenciaBancaria, "ABC-2", contexto);

        Assert.Throws<ArgumentException>(() => calendario.ReportarIncidencia(
            1, estado, "Rechazo bancario", " ", contexto));
        Assert.Equal(EstadoDesembolso.EnProceso, calendario.ObtenerCuotaVigente(1).Estado);

        var incidencia = calendario.ReportarIncidencia(
            1, estado, "Rechazo bancario", "Fondos rechazados por el banco", contexto);
        Assert.Equal(EstadoIncidencia.Abierta, incidencia.Estado);
        calendario.ResolverIncidencia(1, incidencia.Id, contexto);
        Assert.Equal(EstadoIncidencia.Resuelta, incidencia.Estado);
    }

    [Fact]
    public void EjecucionYConfirmacionGuardanDatosYOtpSinPermitirCambiosTerminales()
    {
        var calendario = Crear(100m, [new(1, _inicio, 100m)]);
        var contexto = new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var cuentaId = Guid.NewGuid();
        var cuota = calendario.ObtenerCuotaVigente(1);

        Assert.Throws<ArgumentException>(() => calendario.RegistrarEjecucion(1, 100m, _inicio,
            DestinatarioPago.Becario, cuentaId, "01020000000000000001",
            MetodoPago.TransferenciaBancaria, "REF-1", contexto));
        Assert.Equal(EstadoDesembolso.Programado, cuota.Estado);

        calendario.RegistrarEjecucion(1, 100m, _inicio, DestinatarioPago.Becario,
            cuentaId, SnapshotCifrado, MetodoPago.TransferenciaBancaria, "REF-1", contexto);
        Assert.Equal(EstadoDesembolso.EnProceso, cuota.Estado);
        Assert.Equal(100m, cuota.MontoEjecutado);
        Assert.Equal(_inicio, cuota.FechaEjecucionEfectiva);
        Assert.Equal(cuentaId, cuota.DatosPagoBecarioId);
        Assert.Equal(SnapshotCifrado, cuota.CuentaBancariaDestinoSnapshot);
        Assert.Equal(contexto.UsuarioAutorId, cuota.RegistradoPorUsuarioId);
        Assert.Equal(1, cuota.IntentosEjecucion);
        Assert.Throws<ArgumentException>(() => calendario.Confirmar(1, Guid.Empty, contexto));
        Assert.Throws<TransicionEstadoInvalidaException>(() => calendario.SuspenderCuota(
            1, EstadoBeca.Suspendida, Guid.NewGuid(), null, contexto));

        var otpId = Guid.NewGuid();
        calendario.Confirmar(1, otpId, contexto);
        Assert.Equal(EstadoDesembolso.PagadoConfirmado, cuota.Estado);
        Assert.Equal(otpId, cuota.AutorizacionOtpConfirmacionId);
        Assert.Equal(contexto.UsuarioAutorId, cuota.ConfirmadoPorUsuarioId);
        Assert.Equal(contexto.Fecha, cuota.FechaConfirmacion);
        Assert.Throws<TransicionEstadoInvalidaException>(() => calendario.CancelarCuota(
            1, EstadoBeca.Revocada, contexto));
        Assert.Throws<TransicionEstadoInvalidaException>(() => calendario.RegistrarEjecucion(
            1, 100m, _inicio, DestinatarioPago.Becario, cuentaId,
            SnapshotCifrado, MetodoPago.TransferenciaBancaria, "REF-2", contexto));
    }

    [Fact]
    public void SuspensionReanudacionYCancelacionRespetanElEstadoDeBeca()
    {
        var calendario = Crear(100m, [new(1, _inicio, 100m)]);
        var contexto = new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var cuota = calendario.ObtenerCuotaVigente(1);
        var suspensionId = Guid.NewGuid();
        var pausaId = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(() => calendario.SuspenderCuota(
            1, EstadoBeca.Activa, suspensionId, pausaId, contexto));
        calendario.SuspenderCuota(1, EstadoBeca.Suspendida, suspensionId, pausaId, contexto);
        Assert.Equal(EstadoDesembolso.Suspendido, cuota.Estado);
        Assert.Equal(suspensionId, cuota.SuspensionDesembolsoId);
        Assert.Equal(pausaId, cuota.PausaAplazamientoId);
        Assert.Throws<InvalidOperationException>(() => calendario.ReanudarCuota(1, EstadoBeca.Suspendida, contexto));

        calendario.ReanudarCuota(1, EstadoBeca.Activa, contexto);
        Assert.Equal(EstadoDesembolso.Programado, cuota.Estado);
        Assert.Null(cuota.SuspensionDesembolsoId);
        Assert.Null(cuota.PausaAplazamientoId);
        Assert.Throws<InvalidOperationException>(() => calendario.CancelarCuota(1, EstadoBeca.Activa, contexto));
        calendario.CancelarCuota(1, EstadoBeca.Concluida, contexto);
        Assert.Equal(EstadoDesembolso.Cancelado, cuota.Estado);
        Assert.Throws<TransicionEstadoInvalidaException>(() => calendario.ReanudarCuota(1, EstadoBeca.Activa, contexto));
    }

    [Fact]
    public void SuspendeTodasLasCuotasProgramadasSinCancelarlas()
    {
        var calendario = Crear(300m,
            [new(1, _inicio, 100m), new(2, _inicio.AddDays(30), 200m)]);
        var contexto = new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var suspensionId = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(() => calendario.SuspenderCuotasProgramadas(
            Guid.NewGuid(), EstadoBeca.Suspendida, suspensionId, null, contexto));
        Assert.Equal(2, calendario.SuspenderCuotasProgramadas(
            calendario.BecaId, EstadoBeca.Suspendida, suspensionId, null, contexto));
        Assert.All(calendario.Desembolsos, cuota =>
        {
            Assert.Equal(EstadoDesembolso.Suspendido, cuota.Estado);
            Assert.Equal(suspensionId, cuota.SuspensionDesembolsoId);
        });
        Assert.Equal(0, calendario.SuspenderCuotasProgramadas(
            calendario.BecaId, EstadoBeca.Suspendida, suspensionId, null, contexto));
    }

    [Fact]
    public void ReanudaSoloLasCuotasDeLaSuspensionIndicada()
    {
        var calendario = Crear(300m,
            [new(1, _inicio, 100m), new(2, _inicio.AddDays(30), 200m)]);
        var contexto = new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var suspensionId = Guid.NewGuid();
        calendario.SuspenderCuotasProgramadas(
            calendario.BecaId, EstadoBeca.Suspendida, suspensionId, null, contexto);

        Assert.Throws<InvalidOperationException>(() => calendario.ReanudarCuotasSuspendidas(
            calendario.BecaId, EstadoBeca.Suspendida, suspensionId, contexto));
        Assert.Equal(0, calendario.ReanudarCuotasSuspendidas(
            calendario.BecaId, EstadoBeca.Activa, Guid.NewGuid(), contexto));
        Assert.Equal(2, calendario.ReanudarCuotasSuspendidas(
            calendario.BecaId, EstadoBeca.Activa, suspensionId, contexto));
        Assert.All(calendario.Desembolsos, cuota =>
        {
            Assert.Equal(EstadoDesembolso.Programado, cuota.Estado);
            Assert.Null(cuota.SuspensionDesembolsoId);
        });
    }

    [Fact]
    public void CuotasDePausaNoSeReanudanSinValidacionAcademica()
    {
        var calendario = Crear(100m, [new(1, _inicio, 100m)]);
        var contexto = new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var suspensionId = Guid.NewGuid();
        calendario.SuspenderCuotasProgramadas(
            calendario.BecaId, EstadoBeca.Suspendida, suspensionId, Guid.NewGuid(), contexto);

        Assert.Throws<InvalidOperationException>(() => calendario.ReanudarCuotasSuspendidas(
            calendario.BecaId, EstadoBeca.Activa, suspensionId, contexto));
        Assert.Equal(EstadoDesembolso.Suspendido, calendario.ObtenerCuotaVigente(1).Estado);
    }

    [Fact]
    public void RevocacionCancelaSoloCuotasFuturasYPreparaEventos()
    {
        var calendario = Crear(300m,
            [new(1, _inicio, 100m), new(2, _inicio.AddDays(30), 100m),
                new(3, _inicio.AddDays(45), 100m)]);
        var contexto = new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        calendario.RegistrarEjecucion(1, 100m, _inicio, DestinatarioPago.Becario,
            Guid.NewGuid(), SnapshotCifrado, MetodoPago.TransferenciaBancaria, "REF-1", contexto);
        calendario.Confirmar(1, Guid.NewGuid(), contexto);
        calendario.SuspenderCuota(3, EstadoBeca.Suspendida, Guid.NewGuid(), null, contexto);

        Assert.Throws<InvalidOperationException>(() => calendario.CancelarCuotasFuturasPorRevocacion(
            calendario.BecaId, EstadoBeca.Activa, contexto));
        Assert.Equal(2, calendario.CancelarCuotasFuturasPorRevocacion(
            calendario.BecaId, EstadoBeca.Revocada, contexto));
        Assert.Equal(EstadoDesembolso.PagadoConfirmado, calendario.ObtenerCuotaVigente(1).Estado);
        Assert.Equal(EstadoDesembolso.Cancelado, calendario.ObtenerCuotaVigente(2).Estado);
        Assert.Equal(EstadoDesembolso.Cancelado, calendario.ObtenerCuotaVigente(3).Estado);
        Assert.Equal(2, calendario.EventosDominio.Count(item =>
            item is Ascenso.Becas.Domain.Desembolsos.Eventos.DesembolsoCanceladoEvento));
        Assert.Equal(0, calendario.CancelarCuotasFuturasPorRevocacion(
            calendario.BecaId, EstadoBeca.Revocada, contexto));
    }

    private static CalendarioDesembolso Crear(decimal maximo, CuotaProgramada[] cuotas)
        => CalendarioDesembolso.Programar(
            Guid.NewGuid(),
            Guid.NewGuid(),
            FrecuenciaDesembolso.Mensual,
            DestinatarioPago.Becario,
            maximo,
            _inicio,
            _fin,
            cuotas,
            new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow));
}
