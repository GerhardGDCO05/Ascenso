using System.Security.Cryptography;
using System.Text;
using Ascenso.Becas.Infrastructure.Seguridad;

namespace Ascenso.Becas.IntegrationTests.Seguridad;

public sealed class CifradorCuentaBancariaTests
{
    [Fact]
    public void ObtieneLaClaveDesdeUnSecretoDeEntornoYFallaSiFalta()
    {
        var nombre = $"BECAS_CUENTAS_TEST_{Guid.NewGuid():N}";
        Assert.Throws<InvalidOperationException>(() =>
            CifradorCuentaBancariaAesGcm.DesdeSecretoDeEntorno(nombre));

        Environment.SetEnvironmentVariable(nombre, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        try
        {
            var cifrador = CifradorCuentaBancariaAesGcm.DesdeSecretoDeEntorno(nombre);
            Assert.StartsWith("v1:", cifrador.Cifrar("01020000000000000001"), StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(nombre, null);
        }
    }

    [Fact]
    public void CifraConAesGcmSinExponerLaCuentaEnClaro()
    {
        var clave = RandomNumberGenerator.GetBytes(32);
        var cifrador = new CifradorCuentaBancariaAesGcm(Convert.ToBase64String(clave));
        const string cuenta = "01020000000000000001";

        var primera = cifrador.Cifrar(cuenta);
        var segunda = cifrador.Cifrar(cuenta);

        Assert.NotEqual(primera, segunda);
        Assert.DoesNotContain(cuenta, primera, StringComparison.Ordinal);
        var partes = primera.Split(':');
        Assert.Equal("v1", partes[0]);
        var plano = new byte[Convert.FromBase64String(partes[3]).Length];
        using var aes = new AesGcm(clave, 16);
        aes.Decrypt(Convert.FromBase64String(partes[1]), Convert.FromBase64String(partes[3]),
            Convert.FromBase64String(partes[2]), plano);
        Assert.Equal(cuenta, Encoding.UTF8.GetString(plano));
    }
}
