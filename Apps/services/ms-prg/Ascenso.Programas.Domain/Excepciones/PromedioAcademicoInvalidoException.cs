namespace Ascenso.Programas.Domain.Excepciones;

public sealed class PromedioAcademicoInvalidoException : DomainException
{
    public const string CodigoError = "PROMEDIO_ACADEMICO_INVALIDO";

    public PromedioAcademicoInvalidoException(string campo, decimal valor)
        : base(
            CodigoError,
            400,
            $"El promedio '{campo}' ({valor:0.00}) debe encontrarse en el rango permitido de 0.00 a 20.00.")
    {
        Campo = campo;
        Valor = valor;
    }

    public string Campo { get; }

    public decimal Valor { get; }
}
