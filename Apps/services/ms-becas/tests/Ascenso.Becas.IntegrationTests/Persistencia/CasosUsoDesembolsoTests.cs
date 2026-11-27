using System.Security.Cryptography;
using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Desembolsos;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Comandos.CicloVidaBeca;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Desembolsos.Datos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Ascenso.Becas.Infrastructure.Persistence.UnidadDeTrabajo;
using Ascenso.Becas.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class CasosUsoDesembolsoTests
{
    private readonly PostgreSqlFixture _fixture;

    public CasosUsoDesembolsoTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ProgramaEjecutaFallaResuelveYReintentaSinPerderLaCuotaAnterior()
    {
        await using var db = _fixture.CrearContexto();
        var datos = DatosPrueba.CrearDatosBeca();
        var beca = DatosPrueba.CrearBecaActiva(datos, DatosPrueba.CrearContextoOperacion());
        db.Becas.Add(beca);
        await db.SaveChangesAsync();
        var casos = CrearCasos(db);
        var fecha = new DateOnly(2026, 9, 1);

        var calendarioId = await casos.HandleAsync(new ProgramarCalendarioDesembolsoCommand(
            beca.Id, beca.PeriodoVigenteActualId, FrecuenciaDesembolso.Mensual,
            DestinatarioPago.Becario, 100m, fecha, fecha,
            [new CuotaProgramada(1, fecha, 100m)]));
        Assert.Single(await casos.HandleAsync(new ObtenerCalendarioDesembolsosQuery(beca.Id)));

        var ejecutado = await casos.HandleAsync(new RegistrarEjecucionDesembolsoCommand(
            calendarioId, 1, 100m, fecha, DestinatarioPago.Institucion, null,
            "v1:AAAAAAAAAAAAAAAA:AAAAAAAAAAAAAAAAAAAAAA==:AQ==",
            MetodoPago.TransferenciaBancaria, "REF-1"));
        Assert.Equal("EN_PROCESO", ejecutado.Estado);
        var incidenciaId = await casos.HandleAsync(new RegistrarIncidenciaDesembolsoCommand(
            calendarioId, 1, EstadoDesembolso.Fallido, "Rechazo",
            "El banco rechazó el abono"));
        db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            casos.HandleAsync(new ReintentarDesembolsoCommand(calendarioId, 1)));
        await casos.HandleAsync(new ResolverIncidenciaDesembolsoCommand(calendarioId, 1, incidenciaId));
        var reintento = await casos.HandleAsync(new ReintentarDesembolsoCommand(calendarioId, 1));
        Assert.Equal(ejecutado.Id, reintento.DesembolsoOrigenReintentoId);
        Assert.Equal("PROGRAMADO", reintento.Estado);
        Assert.Equal(2, (await casos.HandleAsync(new ObtenerDesembolsosQuery(calendarioId))).Count);
        Assert.Single(await casos.HandleAsync(new ObtenerDesembolsosQuery(calendarioId, 2, 1)));

        db.ChangeTracker.Clear();
        Assert.Equal(2, await db.Desembolsos.CountAsync(item =>
            item.CalendarioDesembolsoId == calendarioId && item.NumeroCuota == 1));
    }

    [Fact]
    public async Task RechazaUnCalendarioQueSuperaLaOferta()
    {
        await using var db = _fixture.CrearContexto();
        var datos = DatosPrueba.CrearDatosBeca();
        var beca = DatosPrueba.CrearBecaActiva(datos, DatosPrueba.CrearContextoOperacion());
        db.Becas.Add(beca);
        await db.SaveChangesAsync();
        var casos = CrearCasos(db);
        var fecha = new DateOnly(2026, 9, 1);

        var error = await Assert.ThrowsAsync<ArgumentException>(() => casos.HandleAsync(
            new ProgramarCalendarioDesembolsoCommand(beca.Id, beca.PeriodoVigenteActualId,
                FrecuenciaDesembolso.Mensual, DestinatarioPago.Becario, 1501m,
                fecha, fecha, [new CuotaProgramada(1, fecha, 1501m)])));
        Assert.Contains("EXCEDE_OFERTA", error.Message);
    }

    [Fact]
    public async Task ActualizaDatosPagoYConfirmaDesembolsoConAutorizacion()
    {
        await using var db = _fixture.CrearContexto();
        var datos = DatosPrueba.CrearDatosBeca();
        var beca = DatosPrueba.CrearBecaActiva(datos, DatosPrueba.CrearContextoOperacion());
        db.Becas.Add(beca);
        await db.SaveChangesAsync();
        var movimientos = new MovimientosPrueba();
        var casos = CrearCasos(db, movimientos: movimientos);
        var fecha = new DateOnly(2026, 9, 1);
        var cuentaId = await casos.HandleAsync(new ActualizarDatosPagoBecarioCommand(
            beca.Id, "Banco", "0102", TipoCuentaBancaria.Ahorro,
            "01020000000000000001", "V1234567", "123456"));
        var calendarioId = await casos.HandleAsync(new ProgramarCalendarioDesembolsoCommand(
            beca.Id, beca.PeriodoVigenteActualId, FrecuenciaDesembolso.Mensual,
            DestinatarioPago.Becario, 100m, fecha, fecha,
            [new CuotaProgramada(1, fecha, 100m)]));
        var cuenta = await db.DatosPagoBecarios.SingleAsync(item => item.Id == cuentaId);
        await Assert.ThrowsAsync<ConflictoOperacionException>(() => casos.HandleAsync(
            new RegistrarEjecucionDesembolsoCommand(calendarioId, 1, 100m, fecha,
                DestinatarioPago.Becario, Guid.NewGuid(), string.Empty,
                MetodoPago.TransferenciaBancaria, "REF-FALSA")));
        await casos.HandleAsync(new RegistrarEjecucionDesembolsoCommand(
            calendarioId, 1, 100m, fecha, DestinatarioPago.Becario, cuentaId,
            string.Empty,
            MetodoPago.TransferenciaBancaria, "REF-OK"));
        var confirmado = await casos.HandleAsync(new ConfirmarDesembolsoCommand(
            calendarioId, 1, "123456"));

        Assert.Equal("PAGADO_CONFIRMADO", confirmado.Estado);
        var movimiento = Assert.Single(movimientos.Registrados);
        Assert.Equal(TipoMovimientoPresupuestoPrograma.DesembolsoConfirmado,
            movimiento.Tipo);
        Assert.Equal(100m, movimiento.Monto);
        Assert.Equal($"desembolso-confirmado-{confirmado.Id:N}",
            movimiento.ClaveIdempotencia);
        db.ChangeTracker.Clear();
        var becaActualizada = await db.Becas.SingleAsync(item => item.Id == beca.Id);
        Assert.Equal(1400m, becaActualizada.MontoComprometidoPeriodoActual);
        Assert.Equal(100m, becaActualizada.MontoDesembolsadoPeriodoActual);
        Assert.NotNull((await db.Desembolsos.SingleAsync(item => item.Id == confirmado.Id))
            .AutorizacionOtpConfirmacionId);
        Assert.Equal(cuenta.NumeroCuentaCifradoAesGcm,
            (await db.Desembolsos.SingleAsync(item => item.Id == confirmado.Id))
                .CuentaBancariaDestinoSnapshot);
        Assert.DoesNotContain("01020000000000000001", cuenta.NumeroCuentaCifradoAesGcm,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task SinCuentaInstitucionalOficialNoAceptaSnapshotDelRequest()
    {
        await using var db = _fixture.CrearContexto();
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        db.Becas.Add(beca);
        await db.SaveChangesAsync();
        var casos = CrearCasos(db, cuentaInstitucional:
            new CuentaInstitucionalPrueba { Snapshot = null });
        var fecha = new DateOnly(2026, 9, 1);
        var calendarioId = await casos.HandleAsync(new ProgramarCalendarioDesembolsoCommand(
            beca.Id, beca.PeriodoVigenteActualId, FrecuenciaDesembolso.Mensual,
            DestinatarioPago.Institucion, 100m, fecha, fecha,
            [new CuotaProgramada(1, fecha, 100m)]));

        await Assert.ThrowsAsync<ConfiguracionProgramaNoDisponibleException>(() =>
            casos.HandleAsync(new RegistrarEjecucionDesembolsoCommand(calendarioId,
                1, 100m, fecha, DestinatarioPago.Institucion, null,
                "v1:AAAAAAAAAAAAAAAA:AAAAAAAAAAAAAAAAAAAAAA==:AQ==",
                MetodoPago.TransferenciaBancaria, "REF-NO-OFICIAL")));

        db.ChangeTracker.Clear();
        Assert.Equal(EstadoDesembolso.Programado,
            (await db.Desembolsos.SingleAsync(item => item.CalendarioDesembolsoId == calendarioId)).Estado);
    }

    [Fact]
    public async Task SinMovimientoContableNoConsumeOtpNiConfirmaDesembolso()
    {
        await using var db = _fixture.CrearContexto();
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        db.Becas.Add(beca);
        await db.SaveChangesAsync();
        var otp = new OtpPrueba();
        var movimientos = new MovimientosPrueba { Disponible = false };
        var casos = CrearCasos(db, otp, movimientos);
        var fecha = new DateOnly(2026, 9, 1);
        var calendarioId = await casos.HandleAsync(new ProgramarCalendarioDesembolsoCommand(
            beca.Id, beca.PeriodoVigenteActualId, FrecuenciaDesembolso.Mensual,
            DestinatarioPago.Institucion, 100m, fecha, fecha,
            [new CuotaProgramada(1, fecha, 100m)]));
        await casos.HandleAsync(new RegistrarEjecucionDesembolsoCommand(calendarioId,
            1, 100m, fecha, DestinatarioPago.Institucion, null, string.Empty,
            MetodoPago.TransferenciaBancaria, "REF-SIN-MOVIMIENTO"));

        await Assert.ThrowsAsync<DependenciaNoDisponibleException>(() =>
            casos.HandleAsync(new ConfirmarDesembolsoCommand(calendarioId, 1, "123456")));

        db.ChangeTracker.Clear();
        Assert.Equal(EstadoDesembolso.EnProceso,
            (await db.Desembolsos.SingleAsync(item => item.CalendarioDesembolsoId == calendarioId)).Estado);
        Assert.Equal(1500m,
            (await db.Becas.SingleAsync(item => item.Id == beca.Id)).MontoComprometidoPeriodoActual);
        Assert.Equal(0, otp.Consumos);
        Assert.Empty(movimientos.Registrados);
    }

    [Fact]
    public void ValidadoresRechazanEntradasSinEstadoOMontoValido()
    {
        var fecha = new DateOnly(2026, 9, 1);
        var calendario = new ProgramarCalendarioDesembolsoCommand(Guid.Empty, Guid.Empty,
            (FrecuenciaDesembolso)99, DestinatarioPago.Becario, -1m,
            fecha, fecha, []);
        var ejecucion = new RegistrarEjecucionDesembolsoCommand(Guid.Empty, 0, -1m,
            default, DestinatarioPago.Becario, null, string.Empty,
            MetodoPago.TransferenciaBancaria, string.Empty);

        Assert.False(new ProgramarCalendarioDesembolsoValidator().Validate(calendario).IsValid);
        Assert.False(new RegistrarEjecucionDesembolsoValidator().Validate(ejecucion).IsValid);
        Assert.False(new RegistrarIncidenciaDesembolsoValidator().Validate(
            new RegistrarIncidenciaDesembolsoCommand(Guid.NewGuid(), 1,
                EstadoDesembolso.Programado, "motivo", "banco")).IsValid);
        Assert.False(new ObtenerDesembolsosValidator().Validate(
            new ObtenerDesembolsosQuery(Guid.NewGuid(), 0, 101)).IsValid);
    }

    [Theory]
    [InlineData("OTP_EXPIRADO")]
    [InlineData("OTP_BLOQUEADO")]
    public async Task OtpRechazadoNoModificaDatosDePagoNiConfirmaCuota(string codigoError)
    {
        await using var db = _fixture.CrearContexto();
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        db.Becas.Add(beca);
        await db.SaveChangesAsync();

        var otp = new OtpPrueba(codigoError);
        var casos = CrearCasos(db, otp);
        var fecha = new DateOnly(2026, 9, 1);
        var errorCuenta = await Assert.ThrowsAsync<OperacionNoPermitidaException>(() =>
            casos.HandleAsync(new ActualizarDatosPagoBecarioCommand(beca.Id, "Banco",
                "0102", TipoCuentaBancaria.Ahorro, "01020000000000000001", "V1234567", "123456")));
        Assert.Contains(codigoError, errorCuenta.Message);
        Assert.Empty(await db.DatosPagoBecarios.Where(item => item.BecaId == beca.Id).ToListAsync());

        var calendarioId = await casos.HandleAsync(new ProgramarCalendarioDesembolsoCommand(
            beca.Id, beca.PeriodoVigenteActualId, FrecuenciaDesembolso.Mensual,
            DestinatarioPago.Institucion, 100m, fecha, fecha,
            [new CuotaProgramada(1, fecha, 100m)]));
        await casos.HandleAsync(new RegistrarEjecucionDesembolsoCommand(calendarioId, 1,
            100m, fecha, DestinatarioPago.Institucion, null,
            "v1:AAAAAAAAAAAAAAAA:AAAAAAAAAAAAAAAAAAAAAA==:AQ==",
            MetodoPago.TransferenciaBancaria, "REF-OTP"));
        var errorPago = await Assert.ThrowsAsync<OperacionNoPermitidaException>(() =>
            casos.HandleAsync(new ConfirmarDesembolsoCommand(calendarioId, 1, "123456")));
        Assert.Contains(codigoError, errorPago.Message);
        db.ChangeTracker.Clear();
        Assert.Equal(EstadoDesembolso.EnProceso,
            (await db.Desembolsos.SingleAsync(item => item.CalendarioDesembolsoId == calendarioId)).Estado);
        var becaActual = await db.Becas.SingleAsync(item => item.Id == beca.Id);
        Assert.Equal(0m, becaActual.MontoDesembolsadoPeriodoActual);
    }

    [Fact]
    public async Task RevocacionQueObtieneElBloqueoPrimeroImpideConfirmarYConsumirOtp()
    {
        await using var preparacion = _fixture.CrearContexto();
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        preparacion.Becas.Add(beca);
        await preparacion.SaveChangesAsync();
        var fecha = new DateOnly(2026, 9, 1);
        var casosPreparacion = CrearCasos(preparacion);
        var calendarioId = await casosPreparacion.HandleAsync(new ProgramarCalendarioDesembolsoCommand(
            beca.Id, beca.PeriodoVigenteActualId, FrecuenciaDesembolso.Mensual,
            DestinatarioPago.Institucion, 100m, fecha, fecha,
            [new CuotaProgramada(1, fecha, 100m)]));
        await casosPreparacion.HandleAsync(new RegistrarEjecucionDesembolsoCommand(calendarioId,
            1, 100m, fecha, DestinatarioPago.Institucion, null,
            "v1:AAAAAAAAAAAAAAAA:AAAAAAAAAAAAAAAAAAAAAA==:AQ==",
            MetodoPago.TransferenciaBancaria, "REF-CONC"));

        await using var contextoRevocacion = _fixture.CrearContexto();
        await using var contextoConfirmacion = _fixture.CrearContexto();
        await using var transaccion = await contextoRevocacion.Database.BeginTransactionAsync();
        var bloqueada = await new BecaRepository(contextoRevocacion)
            .ObtenerBecaParaActualizarAsync(beca.Id);
        Assert.NotNull(bloqueada);
        var otp = new OtpPrueba();
        var confirmacion = CrearCasos(contextoConfirmacion, otp).HandleAsync(
            new ConfirmarDesembolsoCommand(calendarioId, 1, "123456"));
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        Assert.False(confirmacion.IsCompleted);
        Assert.Equal(0, otp.Consumos);

        var otpRevocacion = new OtpPrueba();
        var cicloVida = new CasosUsoCicloVidaBeca(
            new BecaRepository(contextoRevocacion),
            new CalendarioDesembolsoRepository(contextoRevocacion),
            new UnidadDeTrabajo(contextoRevocacion),
            new ContextoPrueba(), new RelojPrueba(), otpRevocacion,
            new OutboxPrueba(), new ProgramadorPrueba(),
            NullLogger<CasosUsoCicloVidaBeca>.Instance,
            new MovimientosPrueba(),
            new IntencionAceptacionOfertaStore(contextoRevocacion));
        await cicloVida.HandleAsync(new RevocarBecaDefinitivamenteCommand(
            beca.Id, "Revocación justificada", "123456"));
        await transaccion.CommitAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            confirmacion.WaitAsync(TimeSpan.FromSeconds(15)));
        Assert.Equal(0, otp.Consumos);
        Assert.Equal(1, otpRevocacion.Consumos);
        Assert.Equal(TipoOperacionOTP.RevocarBeca, otpRevocacion.UltimaOperacion);
        await using var comprobacion = _fixture.CrearContexto();
        Assert.Equal(EstadoDesembolso.EnProceso,
            (await comprobacion.Desembolsos.SingleAsync(item =>
                item.CalendarioDesembolsoId == calendarioId)).Estado);
    }

    private static CasosUsoDesembolso CrearCasos(Infrastructure.Persistence.BecasDbContext db,
        IOtpClient? otp = null, IMovimientoPresupuestoPrograma? movimientos = null,
        ICuentaInstitucionalPrograma? cuentaInstitucional = null)
        => new(
            new CalendarioDesembolsoRepository(db),
            new DatosPagoBecarioRepository(db),
            new BecaRepository(db),
            new UnidadDeTrabajo(db),
            new ContextoPrueba(),
            new RelojPrueba(),
            new CifradorCuentaBancariaAesGcm(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))),
            otp ?? new OtpPrueba(),
            new OutboxPrueba(), movimientos ?? new MovimientosPrueba(),
            cuentaInstitucional ?? new CuentaInstitucionalPrueba());

    private sealed class MovimientosPrueba : IMovimientoPresupuestoPrograma
    {
        public bool Disponible { get; set; } = true;
        public List<MovimientoPresupuestoPrograma> Registrados { get; } = [];

        public Task AsegurarDisponibleAsync(CancellationToken cancellationToken = default)
            => Disponible ? Task.CompletedTask
                : throw new DependenciaNoDisponibleException("MS-PRG: movimientos-presupuesto");

        public Task RegistrarAsync(MovimientoPresupuestoPrograma movimiento,
            CancellationToken cancellationToken = default)
        {
            Registrados.Add(movimiento);
            return Task.CompletedTask;
        }
    }

    private sealed class CuentaInstitucionalPrueba : ICuentaInstitucionalPrograma
    {
        public string? Snapshot { get; set; } =
            "v1:AAAAAAAAAAAAAAAA:AAAAAAAAAAAAAAAAAAAAAA==:AQ==";

        public Task<string?> ObtenerSnapshotCifradoAsync(Guid programaBecaId,
            Guid periodoAcademicoId, CancellationToken cancellationToken = default)
            => Task.FromResult(Snapshot);
    }

    private sealed class OutboxPrueba : IOutboxBeca
    {
        public Task EncolarAsync(IDomainEvent evento, int aggregateVersion = 0,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ContextoPrueba : IContextoOperacionActual
    {
        public Guid? UsuarioId { get; } = Guid.NewGuid();
        public string CorrelationId { get; } = Guid.NewGuid().ToString("D");
        public string? DireccionIp => null;
        public Guid? AutorizacionOtpId { get; } = Guid.NewGuid();
    }

    private sealed class RelojPrueba : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class OtpPrueba : IOtpClient
    {
        private readonly string? _codigoError;

        public OtpPrueba(string? codigoError = null) => _codigoError = codigoError;

        public int Consumos { get; private set; }

        public TipoOperacionOTP? UltimaOperacion { get; private set; }

        public Task<ResultadoConsumoOtp> ConsumirAsync(Guid autorizacionOtpId,
            Guid usuarioId, TipoOperacionOTP operacion,
            string recursoTipo, Guid recursoId, string huellaPayload, string codigo,
            CancellationToken cancellationToken = default)
        {
            Consumos++;
            UltimaOperacion = operacion;
            return Task.FromResult(new ResultadoConsumoOtp(_codigoError is null, _codigoError));
        }
    }

    private sealed class ProgramadorPrueba : IProgramadorExpiracionOfertas
    {
        public Task<string> ProgramarAsync(Guid ofertaId, DateTimeOffset fechaVencimiento,
            CancellationToken cancellationToken = default)
            => Task.FromResult("job-prueba");

        public Task<bool> CancelarAsync(string referenciaJob,
            CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
