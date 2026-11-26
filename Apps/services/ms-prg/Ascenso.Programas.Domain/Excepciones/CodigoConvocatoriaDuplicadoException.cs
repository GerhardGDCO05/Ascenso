namespace Ascenso.Programas.Domain.Excepciones;

public sealed class CodigoConvocatoriaDuplicadoException : DomainException
{
    public const string CodigoError = "CODIGO_CONVOCATORIA_DUPLICADO";

    public CodigoConvocatoriaDuplicadoException(string codigo)
        : base(
            CodigoError,
            409,
            $"Ya existe una convocatoria registrada con el código '{codigo}'.")
    {
        CodigoConvocatoria = codigo;
    }

    public string CodigoConvocatoria { get; }
}
