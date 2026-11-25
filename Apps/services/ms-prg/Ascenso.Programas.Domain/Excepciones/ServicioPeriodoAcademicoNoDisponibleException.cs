namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ServicioPeriodoAcademicoNoDisponibleException : DomainException
{
    public const string CodigoError = "SERVICIO_PERIODO_ACADEMICO_NO_DISPONIBLE";

    public ServicioPeriodoAcademicoNoDisponibleException(string motivo)
        : base(
            CodigoError,
            503,
            $"No fue posible validar el periodo académico contra el calendario institucional: {motivo}")
    {
        Motivo = motivo;
    }

    public string Motivo { get; }
}
