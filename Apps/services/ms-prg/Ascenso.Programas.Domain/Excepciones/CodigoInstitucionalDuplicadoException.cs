namespace Ascenso.Programas.Domain.Excepciones;

public sealed class CodigoInstitucionalDuplicadoException : DomainException
{
    public const string CodigoError = "CODIGO_INSTITUCIONAL_DUPLICADO";

    public CodigoInstitucionalDuplicadoException(string codigoInstitucional)
        : base(
            CodigoError,
            409,
            $"Ya existe una universidad aliada registrada con el código institucional '{codigoInstitucional}'.")
    {
        CodigoInstitucional = codigoInstitucional;
    }

    public string CodigoInstitucional { get; }
}
