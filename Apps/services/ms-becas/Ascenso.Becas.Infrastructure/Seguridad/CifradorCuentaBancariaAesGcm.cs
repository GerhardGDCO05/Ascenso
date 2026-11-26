using System.Security.Cryptography;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Infrastructure.Seguridad;

/// <summary>Formato versionado: v1:nonce:tag:ciphertext, todo en Base64.</summary>
public sealed class CifradorCuentaBancariaAesGcm : ICifradorCuentaBancaria
{
    private readonly byte[] _clave;

    public CifradorCuentaBancariaAesGcm(string claveBase64)
    {
        _clave = Convert.FromBase64String(claveBase64);
        if (_clave.Length != 32)
        {
            throw new ArgumentException("La clave AES-GCM debe tener 32 bytes.", nameof(claveBase64));
        }
    }

    public static CifradorCuentaBancariaAesGcm DesdeSecretoDeEntorno(string nombreVariable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreVariable);
        var clave = Environment.GetEnvironmentVariable(nombreVariable)
            ?? throw new InvalidOperationException($"Falta el secreto {nombreVariable}.");
        return new CifradorCuentaBancariaAesGcm(clave);
    }

    public string Cifrar(string numeroCuenta)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroCuenta);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plano = System.Text.Encoding.UTF8.GetBytes(numeroCuenta);
        var cifrado = new byte[plano.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_clave, 16);
        aes.Encrypt(nonce, plano, cifrado, tag);
        CryptographicOperations.ZeroMemory(plano);
        return $"v1:{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(tag)}:{Convert.ToBase64String(cifrado)}";
    }
}
