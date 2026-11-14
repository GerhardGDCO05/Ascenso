namespace Ascenso.Programas.Domain.Excepciones;

public sealed class FechasConvenioInvalidasException : DomainException
{
    public const string CodigoError = "FECHAS_CONVENIO_INVALIDAS";

    public FechasConvenioInvalidasException(DateOnly fechaInicio, DateOnly fechaFin)
        : base(
            CodigoError,
            400,
            $"La fecha de fin del convenio ({fechaFin:yyyy-MM-dd}) debe ser posterior a la fecha de inicio ({fechaInicio:yyyy-MM-dd}).")
    {
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
    }

    public DateOnly FechaInicio { get; }

    public DateOnly FechaFin { get; }
}
