namespace Ascenso.Programas.Domain.Excepciones;

public sealed class PeriodoAcademicoNoVigenteException : DomainException
{
    public const string CodigoError = "PERIODO_ACADEMICO_NO_VIGENTE";

    public PeriodoAcademicoNoVigenteException(string periodoAcademicoId, string motivo)
        : base(
            CodigoError,
            422,
            $"El periodo académico '{periodoAcademicoId}' no está vigente: {motivo}")
    {
        PeriodoAcademicoId = periodoAcademicoId;
        Motivo = motivo;
    }

    public string PeriodoAcademicoId { get; }

    public string Motivo { get; }
}
