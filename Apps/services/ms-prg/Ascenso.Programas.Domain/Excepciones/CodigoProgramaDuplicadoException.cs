namespace Ascenso.Programas.Domain.Excepciones;

public sealed class CodigoProgramaDuplicadoException : DomainException
{
    public const string CodigoError = "CODIGO_PROGRAMA_DUPLICADO";

    public CodigoProgramaDuplicadoException(string codigo)
        : base(
            CodigoError,
            409,
            $"Ya existe un programa de becas registrado con el código '{codigo}'.")
    {
        CodigoPrograma = codigo;
    }

    public string CodigoPrograma { get; }
}
