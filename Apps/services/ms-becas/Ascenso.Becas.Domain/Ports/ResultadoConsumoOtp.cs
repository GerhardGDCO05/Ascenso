namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Resultado del consumo de un código OTP.
/// </summary>
public sealed record ResultadoConsumoOtp(
    bool Consumido,
    string? CodigoError = null);
