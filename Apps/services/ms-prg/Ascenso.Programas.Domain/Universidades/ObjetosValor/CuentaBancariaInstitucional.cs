namespace Ascenso.Programas.Domain.Universidades.ObjetosValor;

/// <summary>
/// Datos bancarios institucionales de la universidad aliada para desembolsos de matrícula.
/// </summary>
public sealed record class CuentaBancariaInstitucional
{
    private CuentaBancariaInstitucional()
    {
    }

    public CuentaBancariaInstitucional(
        string banco,
        string tipoCuenta,
        string numeroCuentaCifrado,
        string rifTitular)
    {
        if (string.IsNullOrWhiteSpace(banco))
        {
            throw new ArgumentException("El banco de la cuenta institucional es obligatorio.", nameof(banco));
        }

        if (string.IsNullOrWhiteSpace(tipoCuenta))
        {
            throw new ArgumentException("El tipo de cuenta institucional es obligatorio.", nameof(tipoCuenta));
        }

        if (string.IsNullOrWhiteSpace(numeroCuentaCifrado))
        {
            throw new ArgumentException("El número de cuenta cifrado es obligatorio.", nameof(numeroCuentaCifrado));
        }

        if (string.IsNullOrWhiteSpace(rifTitular))
        {
            throw new ArgumentException("El RIF del titular de la cuenta es obligatorio.", nameof(rifTitular));
        }

        Banco = banco.Trim();
        TipoCuenta = tipoCuenta.Trim();
        NumeroCuentaCifrado = numeroCuentaCifrado.Trim();
        RifTitular = rifTitular.Trim().ToUpperInvariant();
    }

    public string Banco { get; private set; } = string.Empty;

    public string TipoCuenta { get; private set; } = string.Empty;

    public string NumeroCuentaCifrado { get; private set; } = string.Empty;

    public string RifTitular { get; private set; } = string.Empty;
}
