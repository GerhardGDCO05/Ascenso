using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Domain.Tests.Desembolsos;

public sealed class DatosPagoBecarioTests
{
    [Fact]
    public void SoloConservaCuentaCifradaYMarcaLaAnteriorComoReemplazada()
    {
        var contexto = new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var datos = DatosPagoBecario.Crear(Guid.NewGuid(), Guid.NewGuid(), "Banco", "0102",
            TipoCuentaBancaria.Ahorro, "01020000000000000001", "V1234567",
            Guid.NewGuid(), new CifradorPrueba(), contexto);

        Assert.Equal("cifrado", datos.NumeroCuentaCifradoAesGcm);
        Assert.Equal(EstadoDatosPago.Activo, datos.Estado);
        Assert.Throws<InvalidOperationException>(() => datos.Reemplazar(contexto));
        var contextoConOtp = new ContextoOperacion(contexto.UsuarioAutorId, contexto.CorrelacionId,
            contexto.Fecha, autorizacionOtpId: Guid.NewGuid());
        datos.Reemplazar(contextoConOtp);
        Assert.Equal(EstadoDatosPago.Reemplazado, datos.Estado);
        Assert.Equal(contextoConOtp.AutorizacionOtpId, datos.AutorizacionOtpModificacionId);
        Assert.Throws<InvalidOperationException>(() => datos.Reemplazar(contextoConOtp));
    }

    private sealed class CifradorPrueba : ICifradorCuentaBancaria
    {
        public string Cifrar(string numeroCuenta) => "cifrado";
    }
}
