using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;

namespace Ascenso.Becas.Domain.Tests.Otorgamiento;

public sealed class TransicionesBecaTests
{
    private const string Justificacion = "Justificación suficiente para la operación.";

    [Fact]
    public void Debe_ActivarLaBeca_Cuando_LaOfertaEstaVigente()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var contexto = PruebasOtorgamiento.Contexto(PruebasOtorgamiento.FechaBase.AddDays(1));

        beca.AceptarOferta(beca.Oferta!.Id, contexto);

        Assert.Equal(EstadoBeca.Activa, beca.Estado);
        Assert.Equal(DateOnly.FromDateTime(contexto.Fecha.UtcDateTime), beca.FechaActivacion);
        Assert.True(beca.HabilitadaParaDesembolso);
        Assert.Equal(3, beca.Version);
    }

    [Fact]
    public void Debe_ActivarSinHabilitarDesembolso_Cuando_ElProgramaExigeCompromiso()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva(
            PruebasOtorgamiento.DatosBeca(requiereCompromiso: true));

        Assert.Equal(EstadoBeca.Activa, beca.Estado);
        Assert.False(beca.HabilitadaParaDesembolso);
        Assert.False(beca.CompromisoFirmadoAprobado);
        Assert.NotNull(beca.Compromiso);
        Assert.Equal(EstadoCompromiso.PendienteCarga, beca.Compromiso.Estado);
    }

    [Fact]
    public void Debe_SuspenderYReactivarLaBeca_Cuando_LasJustificacionesSonValidas()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();
        var suspension = PruebasOtorgamiento.Contexto(PruebasOtorgamiento.FechaBase.AddDays(5), Justificacion);

        beca.Suspender(suspension);

        Assert.Equal(EstadoBeca.Suspendida, beca.Estado);
        Assert.False(beca.HabilitadaParaDesembolso);
        Assert.False(beca.Historial.Last().EstadoTerminalIrreversible);

        var reactivacion = PruebasOtorgamiento.Contexto(PruebasOtorgamiento.FechaBase.AddDays(6), Justificacion);
        beca.Reactivar(reactivacion);

        Assert.Equal(EstadoBeca.Activa, beca.Estado);
        Assert.True(beca.HabilitadaParaDesembolso);
    }

    [Fact]
    public void Debe_RenovarLaBeca_Cuando_EstaActivaYHayJustificacion()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();
        var contexto = PruebasOtorgamiento.Contexto(PruebasOtorgamiento.FechaBase.AddDays(30), Justificacion);

        beca.Renovar(contexto);

        Assert.Equal(EstadoBeca.Renovada, beca.Estado);
        Assert.NotNull(beca.FechaConclusionORevocacion);
        Assert.False(beca.HabilitadaParaDesembolso);
        Assert.True(beca.Historial.Last().EstadoTerminalIrreversible);
    }

    [Fact]
    public void Debe_ConcluirLaBeca_Cuando_EstaActivaYHayJustificacion()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();
        var contexto = PruebasOtorgamiento.Contexto(PruebasOtorgamiento.FechaBase.AddDays(120), Justificacion);

        beca.Concluir(contexto);

        Assert.Equal(EstadoBeca.Concluida, beca.Estado);
        Assert.NotNull(beca.FechaConclusionORevocacion);
        Assert.True(beca.Historial.Last().EstadoTerminalIrreversible);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Debe_RevocarDefinitivamenteLaBeca_Cuando_EstaActivaOSuspendida(bool suspender)
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();
        var fecha = PruebasOtorgamiento.FechaBase.AddDays(10);

        if (suspender)
        {
            beca.Suspender(PruebasOtorgamiento.Contexto(fecha, Justificacion));
        }

        var contexto = PruebasOtorgamiento.Contexto(
            fecha.AddDays(1),
            Justificacion,
            autorizacionOtpId: PruebasOtorgamiento.NuevoId());

        beca.Revocar(contexto);

        Assert.Equal(EstadoBeca.Revocada, beca.Estado);
        Assert.Equal(TipoCierreBeca.RevocacionDefinitiva, beca.TipoCierre);
        Assert.False(beca.HabilitadaParaDesembolso);
        Assert.True(beca.Historial.Last().EstadoTerminalIrreversible);
        Assert.Equal(contexto.AutorizacionOtpId, beca.Historial.Last().AutorizacionOtpId);
    }

    [Fact]
    public void Debe_RechazarLaReactivacion_Cuando_LaBecaNoEstaSuspendida()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();
        var contexto = PruebasOtorgamiento.Contexto(justificacion: Justificacion);

        Assert.Throws<TransicionEstadoInvalidaException>(() => beca.Reactivar(contexto));
        Assert.Equal(EstadoBeca.Activa, beca.Estado);
    }

    [Theory]
    [InlineData("suspender")]
    [InlineData("renovar")]
    [InlineData("concluir")]
    [InlineData("reactivar")]
    [InlineData("revocar")]
    public void Debe_RechazarLasTransicionesNoPermitidas_Cuando_LaBecaEstaOtorgada(string operacion)
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var contexto = PruebasOtorgamiento.Contexto(justificacion: Justificacion);

        Action accion = operacion switch
        {
            "suspender" => () => beca.Suspender(contexto),
            "renovar" => () => beca.Renovar(contexto),
            "concluir" => () => beca.Concluir(contexto),
            "reactivar" => () => beca.Reactivar(contexto),
            "revocar" => () => beca.Revocar(contexto),
            _ => throw new InvalidOperationException("Operación de prueba no soportada.")
        };

        Assert.Throws<TransicionEstadoInvalidaException>(accion);
        Assert.Equal(EstadoBeca.Otorgada, beca.Estado);
    }

    [Theory]
    [InlineData(EstadoBeca.Renovada)]
    [InlineData(EstadoBeca.Concluida)]
    [InlineData(EstadoBeca.Revocada)]
    public void Debe_RechazarCualquierNuevaTransicion_Cuando_LaBecaEsTerminal(EstadoBeca estadoTerminal)
    {
        var beca = CrearBecaEnEstado(estadoTerminal);
        var contexto = PruebasOtorgamiento.Contexto(justificacion: Justificacion);

        Assert.Throws<TransicionEstadoInvalidaException>(() => beca.Suspender(contexto));
        Assert.Throws<TransicionEstadoInvalidaException>(() => beca.Reactivar(contexto));
        Assert.Throws<TransicionEstadoInvalidaException>(() => beca.Renovar(contexto));
        Assert.Throws<TransicionEstadoInvalidaException>(() => beca.Concluir(contexto));
        Assert.Throws<TransicionEstadoInvalidaException>(() => beca.Revocar(contexto));
        Assert.Equal(estadoTerminal, beca.Estado);
    }

    [Fact]
    public void Debe_RechazarLaActivacion_Cuando_LaOfertaEstaVencida()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var vencimiento = beca.Oferta!.FechaVencimientoAceptacion;
        var estadoAnterior = beca.Estado;
        var historialAnterior = beca.Historial.Count;
        var eventosAnteriores = beca.EventosDominio.Count;

        Assert.Throws<OfertaVencidaException>(
            () => beca.AceptarOferta(
                beca.Oferta.Id,
                PruebasOtorgamiento.Contexto(vencimiento.AddSeconds(1))));

        Assert.Equal(estadoAnterior, beca.Estado);
        Assert.Equal(EstadoOferta.PendienteRespuesta, beca.Oferta.Estado);
        Assert.Equal(historialAnterior, beca.Historial.Count);
        Assert.Equal(eventosAnteriores, beca.EventosDominio.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("corta")]
    public void Debe_RechazarLaRevocacion_Cuando_LaJustificacionEsInsuficiente(string justificacion)
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();
        var historialAnterior = beca.Historial.Count;
        var eventosAnteriores = beca.EventosDominio.Count;
        var contexto = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(10),
            justificacion);

        Assert.Throws<JustificacionRequeridaException>(() => beca.Revocar(contexto));

        Assert.Equal(EstadoBeca.Activa, beca.Estado);
        Assert.Null(beca.TipoCierre);
        Assert.Equal(historialAnterior, beca.Historial.Count);
        Assert.Equal(eventosAnteriores, beca.EventosDominio.Count);
    }

    private static Beca CrearBecaEnEstado(EstadoBeca estado)
    {
        var beca = estado switch
        {
            EstadoBeca.Otorgada => PruebasOtorgamiento.CrearBecaConOferta(),
            EstadoBeca.Activa => PruebasOtorgamiento.CrearBecaActiva(),
            EstadoBeca.Suspendida => CrearSuspendida(),
            EstadoBeca.Renovada => CrearRenovada(),
            EstadoBeca.Concluida => CrearConcluida(),
            EstadoBeca.Revocada => CrearRevocada(),
            _ => throw new ArgumentOutOfRangeException(nameof(estado))
        };

        return beca;
    }

    private static Beca CrearSuspendida()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();
        beca.Suspender(PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(5),
            Justificacion));
        return beca;
    }

    private static Beca CrearRenovada()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();
        beca.Renovar(PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(30),
            Justificacion));
        return beca;
    }

    private static Beca CrearConcluida()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();
        beca.Concluir(PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(120),
            Justificacion));
        return beca;
    }

    private static Beca CrearRevocada()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();
        beca.Revocar(PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(10),
            Justificacion));
        return beca;
    }
}
