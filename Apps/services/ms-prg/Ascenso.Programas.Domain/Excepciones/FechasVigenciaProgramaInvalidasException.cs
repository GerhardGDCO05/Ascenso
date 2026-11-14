namespace Ascenso.Programas.Domain.Excepciones;

public sealed class FechasVigenciaProgramaInvalidasException : DomainException
{
    public const string CodigoError = "FECHAS_VIGENCIA_PROGRAMA_INVALIDAS";

    public FechasVigenciaProgramaInvalidasException(DateOnly vigenciaDesde, DateOnly vigenciaHasta)
        : base(
            CodigoError,
            400,
            $"La fecha de fin de vigencia ({vigenciaHasta:yyyy-MM-dd}) debe ser posterior a la fecha de inicio ({vigenciaDesde:yyyy-MM-dd}).")
    {
        VigenciaDesde = vigenciaDesde;
        VigenciaHasta = vigenciaHasta;
    }

    public DateOnly VigenciaDesde { get; }

    public DateOnly VigenciaHasta { get; }
}
