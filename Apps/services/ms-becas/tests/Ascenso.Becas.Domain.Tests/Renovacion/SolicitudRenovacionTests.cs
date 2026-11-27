using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Renovacion.Agregados;
using Ascenso.Becas.Domain.Renovacion.Datos;

namespace Ascenso.Becas.Domain.Tests.Renovacion;

public sealed class SolicitudRenovacionTests
{
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);
    private static readonly string[] _codigosEstados =
    [
        "SOLICITADA_EN_CURSO", "APROBADA", "RECHAZADA", "EXPIRADA_CONCLUIDA"
    ];

    [Fact]
    public void LosCuatroEstadosUsanLosCodigosDelDdd()
    {
        Assert.Equal(_codigosEstados,
            Enum.GetValues<EstadoRenovacion>().Select(valor => valor.ACodigo()));
    }

    [Fact]
    public void SolicitarRegistraOrigenDestinoYPlazoSinCrearReservaFicticia()
    {
        var datos = Datos();
        var solicitud = SolicitudRenovacion.Solicitar(datos, Contexto());

        Assert.Equal(7, solicitud.Id.Version);
        Assert.Equal(datos.BecaId, solicitud.BecaId);
        Assert.Equal(datos.PeriodoOrigenId, solicitud.PeriodoOrigenId);
        Assert.Equal(datos.PeriodoSolicitadoId, solicitud.PeriodoSolicitadoId);
        Assert.Equal(EstadoRenovacion.SolicitadaEnCurso, solicitud.Estado);
        Assert.Equal(0m, solicitud.MontoReservadoRenovacion);
        Assert.Equal(1500m, solicitud.MontoSolicitado);
        Assert.Null(solicitud.ReservaPresupuestoId);
        Assert.Null(solicitud.FechaDecision);
        Assert.Equal(1, solicitud.Version);
    }

    [Fact]
    public void AprobarExigeReservaConfirmadaYNoPermiteOtraTransicion()
    {
        var solicitud = SolicitudRenovacion.Solicitar(Datos(), Contexto());
        Assert.Throws<InvalidOperationException>(() =>
            solicitud.Aprobar("Cumple las condiciones académicas", Contexto()));
        Assert.Equal(EstadoRenovacion.SolicitadaEnCurso, solicitud.Estado);

        solicitud.RegistrarReferenciaJobVencimiento("job-renovacion-1", Contexto());
        var reservaId = Guid.CreateVersion7();
        solicitud.ConfirmarReservaPresupuestaria(reservaId, 1500m, Contexto());
        solicitud.Aprobar("Cumple las condiciones académicas", Contexto());

        Assert.Equal(EstadoRenovacion.Aprobada, solicitud.Estado);
        Assert.Equal(reservaId, solicitud.ReservaPresupuestoId);
        Assert.Equal(1500m, solicitud.MontoReservadoRenovacion);
        Assert.Equal("job-renovacion-1", solicitud.ReferenciaJobVencimientoId);
        Assert.NotNull(solicitud.EvaluadoPorCoordinadorId);
        Assert.Equal(_fechaBase, solicitud.FechaDecision);
        Assert.Throws<InvalidOperationException>(() =>
            solicitud.Rechazar("Decisión posterior inválida", Contexto()));
        Assert.Throws<InvalidOperationException>(() =>
            solicitud.Expirar(Contexto(_fechaBase.AddDays(2))));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void NoCreaSolicitudSinRendimientoOPresupuesto(
        bool cumpleRendimiento, bool tienePresupuesto)
    {
        Assert.Throws<InvalidOperationException>(() =>
            SolicitudRenovacion.Solicitar(
                Datos(cumpleRendimiento, tienePresupuesto), Contexto()));
    }

    [Fact]
    public void RechazarConservaLaReservaParaSuLiberacionYBloqueaReapertura()
    {
        var solicitud = SolicitudRenovacion.Solicitar(Datos(), Contexto());
        var reservaId = Guid.CreateVersion7();
        solicitud.ConfirmarReservaPresupuestaria(reservaId, 1500m, Contexto());
        solicitud.Rechazar("Incumplimiento documentado", Contexto());

        Assert.Equal(EstadoRenovacion.Rechazada, solicitud.Estado);
        Assert.Equal(reservaId, solicitud.ReservaPresupuestoId);
        Assert.Equal("Incumplimiento documentado", solicitud.MotivoDecision);
        Assert.Throws<InvalidOperationException>(() =>
            solicitud.Aprobar("No corresponde reabrirla", Contexto()));
    }

    [Fact]
    public void ExpirarSoloTrasElPlazoYNoInventaDecisionDeCoordinador()
    {
        var solicitud = SolicitudRenovacion.Solicitar(Datos(), Contexto());
        Assert.Throws<InvalidOperationException>(() => solicitud.Expirar(Contexto()));
        var despuesDelPlazo = Contexto(_fechaBase.AddDays(2));

        solicitud.Expirar(despuesDelPlazo);

        Assert.Equal(EstadoRenovacion.ExpiradaConcluida, solicitud.Estado);
        Assert.Null(solicitud.EvaluadoPorCoordinadorId);
        Assert.Equal(despuesDelPlazo.Fecha, solicitud.FechaDecision);
        Assert.Throws<InvalidOperationException>(() =>
            solicitud.Expirar(despuesDelPlazo));
    }

    [Fact]
    public void RechazaSolicitudTardiaPeriodosIgualesYReservaInvalida()
    {
        var datos = Datos();
        Assert.Throws<ArgumentException>(() => SolicitudRenovacion.Solicitar(
            datos with { PeriodoSolicitadoId = datos.PeriodoOrigenId }, Contexto()));
        Assert.Throws<InvalidOperationException>(() => SolicitudRenovacion.Solicitar(
            datos, Contexto(_fechaBase.AddDays(2))));

        var solicitud = SolicitudRenovacion.Solicitar(datos, Contexto());
        Assert.Throws<ArgumentException>(() => solicitud.ConfirmarReservaPresupuestaria(
            Guid.Empty, 1500m, Contexto()));
        Assert.Throws<ArgumentException>(() => solicitud.ConfirmarReservaPresupuestaria(
            Guid.CreateVersion7(), 0m, Contexto()));
        Assert.Throws<ArgumentException>(() => solicitud.ConfirmarReservaPresupuestaria(
            Guid.CreateVersion7(), 1600m, Contexto()));
        Assert.Null(solicitud.ReservaPresupuestoId);
    }

    private static DatosSolicitudRenovacion Datos(
        bool cumpleRendimiento = true, bool tienePresupuesto = true)
        => new(Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), new DateOnly(2026, 11, 26),
            cumpleRendimiento, tienePresupuesto, 1500m);

    private static ContextoOperacion Contexto(DateTimeOffset? fecha = null)
        => new(Guid.CreateVersion7(), Guid.CreateVersion7(), fecha ?? _fechaBase);
}
