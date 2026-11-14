namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ReferenciaBancariaDuplicadaException : DomainException
{
    public const string CodigoError = "REFERENCIA_DUPLICADA";

    public ReferenciaBancariaDuplicadaException(string numeroReferenciaBancaria)
        : base(
            CodigoError,
            409,
            $"Ya existe un aporte registrado con el número de referencia bancaria '{numeroReferenciaBancaria}' (RN-PRG-04).")
    {
        NumeroReferenciaBancaria = numeroReferenciaBancaria;
    }

    public string NumeroReferenciaBancaria { get; }
}

