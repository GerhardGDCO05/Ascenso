using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Comandos.CicloVidaBeca;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Desembolsos.Datos;
using Ascenso.Becas.Domain.Desembolsos.Eventos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Ascenso.Becas.Infrastructure.Persistence.UnidadDeTrabajo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class CasosUsoCicloVidaBecaTests
{
    private readonly PostgreSqlFixture _fixture;

    public CasosUsoCicloVidaBecaTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SuspensionYReanudacionActualizanBecaYCuotasEnUnaTransaccion()
    {
        await using var db = _fixture.CrearContexto();
        var (becaId, calendarioId) = await CrearBecaYCalendarioAsync(db);
        var outbox = new OutboxPrueba();
        var casos = CrearCasos(db, outbox: outbox);
        var inicio = new DateOnly(2026, 9, 1);

        var suspensionId = await casos.HandleAsync(new SuspenderBecaCommand(
            becaId, TipoSuspensionBeca.Manual, "Suspensión documentada",
            "Entregar constancia", inicio, null, true,
            "Suspensión autorizada por coordinación"));
        db.ChangeTracker.Clear();
        var suspendida = await new BecaRepository(db).ObtenerPorIdAsync(becaId);
        Assert.NotNull(suspendida);
        Assert.Equal(EstadoBeca.Suspendida, suspendida.Estado);
        Assert.Equal(1500m, suspendida.MontoComprometidoPeriodoActual);
        Assert.Equal(suspensionId, Assert.Single(suspendida.Suspensiones).Id);
        Assert.All(await db.Desembolsos.Where(item => item.CalendarioDesembolsoId == calendarioId)
            .ToArrayAsync(), cuota => Assert.Equal(EstadoDesembolso.Suspendido, cuota.Estado));
        Assert.Single(outbox.Eventos.OfType<DesembolsosBecaSuspendidosEvento>());

        db.ChangeTracker.Clear();
        Assert.Equal(suspensionId, await casos.HandleAsync(new ReanudarBecaCommand(
            becaId, "Reactivación autorizada por coordinación")));
        db.ChangeTracker.Clear();
        var reactivada = await new BecaRepository(db).ObtenerPorIdAsync(becaId);
        Assert.NotNull(reactivada);
        Assert.Equal(EstadoBeca.Activa, reactivada.Estado);
        Assert.Equal(EstadoSuspension.Levantada, Assert.Single(reactivada.Suspensiones).Estado);
        Assert.All(await db.Desembolsos.Where(item => item.CalendarioDesembolsoId == calendarioId)
            .ToArrayAsync(), cuota => Assert.Equal(EstadoDesembolso.Programado, cuota.Estado));
        Assert.Single(outbox.Eventos.OfType<DesembolsosBecaReanudadosEvento>());
    }

    [Fact]
    public async Task SuspensionAcademicaExigeSuCasoDeUsoPropietario()
    {
        await using var db = _fixture.CrearContexto();
        var (becaId, _) = await CrearBecaYCalendarioAsync(db);
        var casos = CrearCasos(db);

        await Assert.ThrowsAsync<OperacionNoPermitidaException>(() => casos.HandleAsync(
            new SuspenderBecaCommand(becaId, TipoSuspensionBeca.IncumplimientoAcademico,
                "Incumplimiento académico", "Entregar reporte académico",
                new DateOnly(2026, 9, 1), null, true,
                "Suspensión solicitada por coordinación")));
        db.ChangeTracker.Clear();
        Assert.Equal(EstadoBeca.Activa, (await db.Becas.SingleAsync(item => item.Id == becaId)).Estado);
    }

    [Theory]
    [InlineData("OTP_EXPIRADO")]
    [InlineData("OTP_BLOQUEADO")]
    [InlineData("OTP_INVALIDO")]
    public async Task OtpRechazadoNoRevocaNiCancelaCuotas(string codigoError)
    {
        await using var db = _fixture.CrearContexto();
        var (becaId, calendarioId) = await CrearBecaYCalendarioAsync(db);
        var otp = new OtpPrueba(codigoError);
        var outbox = new OutboxPrueba();
        var casos = CrearCasos(db, otp, outbox);

        await Assert.ThrowsAsync<OperacionNoPermitidaException>(() => casos.HandleAsync(
            new RevocarBecaDefinitivamenteCommand(becaId,
                "Revocación documentada por coordinación", "123456")));

        db.ChangeTracker.Clear();
        Assert.Equal(EstadoBeca.Activa, (await db.Becas.SingleAsync(item => item.Id == becaId)).Estado);
        Assert.All(await db.Desembolsos.Where(item => item.CalendarioDesembolsoId == calendarioId)
            .ToArrayAsync(), cuota => Assert.Equal(EstadoDesembolso.Programado, cuota.Estado));
        Assert.Equal(TipoOperacionOTP.RevocarBeca, otp.UltimaOperacion);
        Assert.Empty(outbox.Eventos);
    }

    [Fact]
    public async Task RevocacionCancelaFuturasSinAlterarPagoConfirmado()
    {
        await using var db = _fixture.CrearContexto();
        var (becaId, calendarioId) = await CrearBecaYCalendarioAsync(db);
        var beca = await new BecaRepository(db).ObtenerPorIdAsync(becaId);
        var calendario = await new CalendarioDesembolsoRepository(db).ObtenerPorIdAsync(calendarioId);
        Assert.NotNull(beca);
        Assert.NotNull(calendario);
        var contexto = DatosPrueba.CrearContextoOperacion();
        calendario.RegistrarEjecucion(1, 100m, new DateOnly(2026, 9, 1),
            DestinatarioPago.Institucion, null,
            "v1:AAAAAAAAAAAAAAAA:AAAAAAAAAAAAAAAAAAAAAA==:AQ==",
            MetodoPago.TransferenciaBancaria, "REF-REV", contexto);
        calendario.Confirmar(1, Guid.CreateVersion7(), contexto);
        beca.RegistrarDesembolsoConfirmado(100m, contexto);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var otp = new OtpPrueba();
        var outbox = new OutboxPrueba();
        var casos = CrearCasos(db, otp, outbox);
        await casos.HandleAsync(new RevocarBecaDefinitivamenteCommand(becaId,
            "Revocación documentada por coordinación", "123456"));

        db.ChangeTracker.Clear();
        var revocada = await db.Becas.SingleAsync(item => item.Id == becaId);
        Assert.Equal(EstadoBeca.Revocada, revocada.Estado);
        Assert.Equal(0m, revocada.MontoComprometidoPeriodoActual);
        Assert.Equal(100m, revocada.MontoDesembolsadoPeriodoActual);
        var cuotas = await db.Desembolsos.Where(item => item.CalendarioDesembolsoId == calendarioId)
            .OrderBy(item => item.NumeroCuota).ToArrayAsync();
        Assert.Equal(EstadoDesembolso.PagadoConfirmado, cuotas[0].Estado);
        Assert.Equal(EstadoDesembolso.Cancelado, cuotas[1].Estado);
        Assert.Single(outbox.Eventos.OfType<DesembolsoCanceladoEvento>());
        Assert.Single(outbox.Eventos.OfType<BecaRevocadaDefinitivamenteEvento>());
        Assert.Equal(TipoOperacionOTP.RevocarBeca, otp.UltimaOperacion);
        Assert.Equal("BECA", otp.UltimoRecursoTipo);
        Assert.Equal(becaId, otp.UltimoRecursoId);
    }

    [Fact]
    public async Task RevocarOtorgadaCierraOfertaYCancelaJobTrasConfirmar()
    {
        await using var db = _fixture.CrearContexto();
        var datos = DatosPrueba.CrearDatosBeca();
        var beca = Ascenso.Becas.Domain.Otorgamiento.Agregados.Beca.CrearOtorgada(
            datos, DatosPrueba.CrearContextoOperacion());
        var oferta = beca.EmitirOferta(DatosPrueba.CrearDatosOferta(datos),
            DatosPrueba.CrearContextoOperacion());
        db.Becas.Add(beca);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var programador = new ProgramadorPrueba();
        var casos = CrearCasos(db, programador: programador);
        await casos.HandleAsync(new RevocarBecaDefinitivamenteCommand(beca.Id,
            "Revocación documentada por coordinación", "123456"));

        db.ChangeTracker.Clear();
        var cerrada = await new BecaRepository(db).ObtenerPorIdAsync(beca.Id);
        Assert.NotNull(cerrada);
        Assert.Equal(EstadoBeca.Revocada, cerrada.Estado);
        Assert.Equal(EstadoOferta.AnuladaPorCoordinacion, cerrada.Oferta!.Estado);
        Assert.Equal(TipoCierreBeca.RevocacionDefinitiva, cerrada.TipoCierre);
        Assert.Equal(oferta.ReferenciaJobExpiracionHangfireId, programador.UltimaCancelacion);
    }

    private static async Task<(Guid BecaId, Guid CalendarioId)> CrearBecaYCalendarioAsync(
        Infrastructure.Persistence.BecasDbContext db)
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        var inicio = new DateOnly(2026, 9, 1);
        var calendario = CalendarioDesembolso.Programar(beca.Id, beca.PeriodoVigenteActualId,
            FrecuenciaDesembolso.Mensual, DestinatarioPago.Institucion, 200m,
            inicio, inicio.AddMonths(1),
            [new CuotaProgramada(1, inicio, 100m),
                new CuotaProgramada(2, inicio.AddMonths(1), 100m)],
            DatosPrueba.CrearContextoOperacion());
        db.Becas.Add(beca);
        db.CalendariosDesembolso.Add(calendario);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return (beca.Id, calendario.Id);
    }

    private static CasosUsoCicloVidaBeca CrearCasos(
        Infrastructure.Persistence.BecasDbContext db,
        OtpPrueba? otp = null,
        OutboxPrueba? outbox = null,
        ProgramadorPrueba? programador = null)
        => new(new BecaRepository(db), new CalendarioDesembolsoRepository(db),
            new UnidadDeTrabajo(db), new ContextoPrueba(), new RelojPrueba(),
            otp ?? new OtpPrueba(), outbox ?? new OutboxPrueba(),
            programador ?? new ProgramadorPrueba(), NullLogger<CasosUsoCicloVidaBeca>.Instance);

    private sealed class ContextoPrueba : IContextoOperacionActual
    {
        public Guid? UsuarioId { get; } = Guid.CreateVersion7();
        public string CorrelationId { get; } = Guid.CreateVersion7().ToString("D");
        public string? DireccionIp => null;
        public Guid? AutorizacionOtpId { get; } = Guid.CreateVersion7();
    }

    private sealed class RelojPrueba : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class OtpPrueba(string? codigoError = null) : IOtpClient
    {
        public TipoOperacionOTP? UltimaOperacion { get; private set; }
        public string? UltimoRecursoTipo { get; private set; }
        public Guid? UltimoRecursoId { get; private set; }

        public Task<ResultadoConsumoOtp> ConsumirAsync(Guid autorizacionOtpId,
            Guid usuarioId, TipoOperacionOTP operacion, string recursoTipo,
            Guid recursoId, string huellaPayload, string codigo,
            CancellationToken cancellationToken = default)
        {
            UltimaOperacion = operacion;
            UltimoRecursoTipo = recursoTipo;
            UltimoRecursoId = recursoId;
            return Task.FromResult(new ResultadoConsumoOtp(codigoError is null, codigoError));
        }
    }

    private sealed class OutboxPrueba : IOutboxBeca
    {
        public List<IDomainEvent> Eventos { get; } = [];

        public Task EncolarAsync(IDomainEvent evento, int aggregateVersion = 0,
            CancellationToken cancellationToken = default)
        {
            Eventos.Add(evento);
            return Task.CompletedTask;
        }
    }

    private sealed class ProgramadorPrueba : IProgramadorExpiracionOfertas
    {
        public string? UltimaCancelacion { get; private set; }

        public Task<string> ProgramarAsync(Guid ofertaId, DateTimeOffset fechaVencimiento,
            CancellationToken cancellationToken = default)
            => Task.FromResult("job-prueba");

        public Task<bool> CancelarAsync(string referenciaJob,
            CancellationToken cancellationToken = default)
        {
            UltimaCancelacion = referenciaJob;
            return Task.FromResult(true);
        }
    }
}
