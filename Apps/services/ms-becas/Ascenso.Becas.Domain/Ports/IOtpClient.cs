using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Puerto de consumo de códigos OTP para operaciones sensibles.
/// </summary>
public interface IOtpClient
{
    Task<ResultadoConsumoOtp> ConsumirAsync(
        Guid usuarioId,
        TipoOperacionOTP operacion,
        Guid recursoId,
        string huellaPayload,
        string codigo,
        CancellationToken cancellationToken = default);
}
