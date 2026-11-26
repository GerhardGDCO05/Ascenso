using Ascenso.Becas.Domain.Excepciones;

namespace Ascenso.Becas.Domain.Tests.Otorgamiento;

public sealed class ContabilidadDesembolsoBecaTests
{
    private const string Justificacion = "Cambio contable autorizado por coordinación.";

    [Fact]
    public void BloqueaDesembolsoCuandoElCompromisoNoEstaAprobado()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva(
            PruebasOtorgamiento.DatosBeca(requiereCompromiso: true),
            montoPorPeriodo: 1500m);

        var error = Assert.Throws<CompromisoNoFirmadoException>(
            beca.ExigirHabilitacionDesembolso);
        Assert.Equal(412, error.EstadoHttp);
        Assert.Equal("COMPROMISO_NO_FIRMADO", error.Codigo);
        Assert.Throws<CompromisoNoFirmadoException>(() =>
            beca.RegistrarDesembolsoConfirmado(100m, PruebasOtorgamiento.Contexto()));
        Assert.Equal(1500m, beca.MontoComprometidoPeriodoActual);
        Assert.Equal(0m, beca.MontoDesembolsadoPeriodoActual);
    }

    [Fact]
    public void ConfirmaSoloHastaElMontoAprobadoSinDobleConteo()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva(montoPorPeriodo: 1500m);
        beca.RegistrarDesembolsoConfirmado(100m, PruebasOtorgamiento.Contexto());
        Assert.Equal(1400m, beca.MontoComprometidoPeriodoActual);
        Assert.Equal(100m, beca.MontoDesembolsadoPeriodoActual);

        var error = Assert.Throws<ExcedeOfertaException>(() =>
            beca.RegistrarDesembolsoConfirmado(1400.01m, PruebasOtorgamiento.Contexto()));
        Assert.Equal(422, error.EstadoHttp);
        Assert.Equal("EXCEDE_OFERTA", error.Codigo);
        Assert.Equal(1400m, beca.MontoComprometidoPeriodoActual);
        Assert.Equal(100m, beca.MontoDesembolsadoPeriodoActual);

        beca.RegistrarDesembolsoConfirmado(1400m, PruebasOtorgamiento.Contexto());
        Assert.Equal(0m, beca.MontoComprometidoPeriodoActual);
        Assert.Equal(1500m, beca.MontoDesembolsadoPeriodoActual);
        Assert.Throws<ExcedeOfertaException>(() =>
            beca.RegistrarDesembolsoConfirmado(1m, PruebasOtorgamiento.Contexto()));
    }

    [Fact]
    public void SuspensionConservaElPendienteYRevocacionNoRecuperaLoPagado()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva(montoPorPeriodo: 1500m);
        beca.RegistrarDesembolsoConfirmado(250m, PruebasOtorgamiento.Contexto());
        PruebasOtorgamiento.Suspender(beca, PruebasOtorgamiento.Contexto(justificacion: Justificacion));

        Assert.Equal(1250m, beca.MontoComprometidoPeriodoActual);
        Assert.Equal(250m, beca.MontoDesembolsadoPeriodoActual);
        Assert.Throws<InvalidOperationException>(() =>
            beca.RegistrarDesembolsoConfirmado(1m, PruebasOtorgamiento.Contexto()));

        beca.Revocar(PruebasOtorgamiento.Contexto(justificacion: Justificacion,
            autorizacionOtpId: PruebasOtorgamiento.NuevoId()));
        Assert.Equal(0m, beca.MontoComprometidoPeriodoActual);
        Assert.Equal(250m, beca.MontoDesembolsadoPeriodoActual);
    }

    [Fact]
    public void ConclusionLiberaElSaldoPendienteSinAlterarLoPagado()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva(montoPorPeriodo: 1500m);
        beca.RegistrarDesembolsoConfirmado(300m, PruebasOtorgamiento.Contexto());
        beca.Concluir(PruebasOtorgamiento.Contexto(justificacion: Justificacion));

        Assert.Equal(0m, beca.MontoComprometidoPeriodoActual);
        Assert.Equal(300m, beca.MontoDesembolsadoPeriodoActual);
    }
}
