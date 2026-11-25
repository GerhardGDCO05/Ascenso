using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;

namespace Ascenso.Becas.Domain.Tests.Otorgamiento;

public sealed class SuspensionDesembolsoTests
{
    [Fact]
    public void SuspenderCreaRegistroYEventoSinLiberarSaldo()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva(montoPorPeriodo: 1500m);
        var contexto = PruebasOtorgamiento.Contexto(justificacion: "Suspensión documentada por coordinación");
        var inicio = DateOnly.FromDateTime(contexto.Fecha.UtcDateTime);
        var suspension = beca.Suspender(TipoSuspensionBeca.Manual,
            "Suspensión documentada", "Entregar constancia", inicio, null, true, contexto);

        Assert.Equal(EstadoBeca.Suspendida, beca.Estado);
        Assert.False(beca.HabilitadaParaDesembolso);
        Assert.Equal(suspension, Assert.Single(beca.Suspensiones));
        Assert.Equal(1500m, beca.MontoComprometidoPeriodoActual);
        var evento = Assert.Single(beca.EventosDominio.OfType<DesembolsosBecaSuspendidosEvento>());
        Assert.Equal(suspension.Id, evento.SuspensionId);
        Assert.Equal(1500m, evento.SaldoPendiente);
        Assert.Throws<TransicionEstadoInvalidaException>(() => beca.Suspender(TipoSuspensionBeca.Manual,
            "Suspensión documentada", "Entregar constancia", inicio, null, true, contexto));
    }

    [Fact]
    public void DatosInvalidosNoCambianEstadoNiRegistranSuspension()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();
        var contexto = PruebasOtorgamiento.Contexto(justificacion: "Suspensión documentada por coordinación");

        Assert.Throws<ArgumentException>(() => beca.Suspender(TipoSuspensionBeca.Manual,
            "Corto", "Entregar constancia", new DateOnly(2026, 11, 10), null, true, contexto));
        Assert.Equal(EstadoBeca.Activa, beca.Estado);
        Assert.Empty(beca.Suspensiones);
    }

    [Fact]
    public void ReactivarLevantaSuspensionYConservaSaldoPendiente()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva(montoPorPeriodo: 1500m);
        var contexto = PruebasOtorgamiento.Contexto(justificacion: "Suspensión documentada por coordinación");
        var suspension = beca.Suspender(TipoSuspensionBeca.Manual,
            "Suspensión documentada", "Entregar constancia",
            DateOnly.FromDateTime(contexto.Fecha.UtcDateTime), null, true, contexto);

        var reactivacion = PruebasOtorgamiento.Contexto(justificacion: "Reactivación autorizada por coordinación");
        Assert.Same(suspension, beca.Reactivar(reactivacion));
        Assert.Equal(EstadoBeca.Activa, beca.Estado);
        Assert.Equal(EstadoSuspension.Levantada, suspension.Estado);
        Assert.Equal(reactivacion.UsuarioAutorId, suspension.LevantadaPorCoordinadorId);
        Assert.Equal(1500m, beca.MontoComprometidoPeriodoActual);
        var evento = Assert.Single(beca.EventosDominio.OfType<DesembolsosBecaReanudadosEvento>());
        Assert.Equal(suspension.Id, evento.SuspensionId);
        Assert.Equal(1500m, evento.SaldoPendiente);
    }

    [Fact]
    public void PausaNoSeReactivaSinValidacionAcademica()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();
        var contexto = PruebasOtorgamiento.Contexto(justificacion: "Pausa académica aprobada por coordinación");
        var suspension = beca.Suspender(TipoSuspensionBeca.PausaAplazamiento,
            "Pausa académica aprobada", "Reporte académico y evaluación",
            DateOnly.FromDateTime(contexto.Fecha.UtcDateTime), null, true, contexto);

        var reactivacion = PruebasOtorgamiento.Contexto(justificacion: "Reactivación solicitada por coordinación");
        Assert.Throws<InvalidOperationException>(() => beca.Reactivar(reactivacion));
        Assert.Equal(EstadoBeca.Suspendida, beca.Estado);
        Assert.Equal(EstadoSuspension.Vigente, suspension.Estado);
        Assert.Empty(beca.EventosDominio.OfType<DesembolsosBecaReanudadosEvento>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RegistraSuspensionConRevisionOIndefinida(bool indefinida)
    {
        var becaId = Guid.CreateVersion7();
        var inicio = new DateOnly(2026, 11, 25);
        DateOnly? revision = indefinida ? null : inicio.AddDays(30);
        var suspension = SuspensionDesembolso.Crear(becaId,
            TipoSuspensionBeca.Manual, "Motivo documentado", "Presentar constancia",
            inicio, revision, indefinida, PruebasOtorgamiento.Contexto());

        Assert.Equal(becaId, suspension.BecaId);
        Assert.Equal(EstadoSuspension.Vigente, suspension.Estado);
        Assert.Equal(revision, suspension.FechaRevisionProgramada);
        Assert.Equal(indefinida, suspension.EsIndefinida);
        Assert.Equal(7, suspension.Id.Version);

        suspension.Levantar(PruebasOtorgamiento.Contexto());
        Assert.Equal(EstadoSuspension.Levantada, suspension.Estado);
        Assert.NotNull(suspension.LevantadaPorCoordinadorId);
        Assert.NotNull(suspension.FechaResolucion);
        Assert.Throws<InvalidOperationException>(() =>
            suspension.Levantar(PruebasOtorgamiento.Contexto()));
    }

    [Fact]
    public void RechazaSuspensionSinFechaNiMarcaIndefinida()
    {
        Assert.Throws<ArgumentException>(() => SuspensionDesembolso.Crear(
            Guid.CreateVersion7(), TipoSuspensionBeca.Manual,
            "Motivo documentado", "Presentar constancia",
            new DateOnly(2026, 11, 25), null, false,
            PruebasOtorgamiento.Contexto()));
    }
}
