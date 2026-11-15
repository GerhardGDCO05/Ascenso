namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando el periodo académico informado no corresponde al vigente.
/// </summary>
public sealed class PeriodoAcademicoInvalidoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "PERIODO_ACADEMICO_INVALIDO";

    public PeriodoAcademicoInvalidoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El periodo académico informado no corresponde al periodo vigente."
                : mensaje)
    {
    }
}
