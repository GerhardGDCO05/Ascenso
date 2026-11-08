namespace Ascenso.Becas.Domain.Enums;

/// <summary>
/// Operaciones que pueden exigir una autorización OTP.
/// </summary>
public enum TipoOperacionOTP
{
    ConfirmarDesembolso = 1,
    RevocarBeca = 2,
    ModificarDatosPago = 3,
    EmitirOfertaBeca = 4,
    AnularOfertaBeca = 5
}
