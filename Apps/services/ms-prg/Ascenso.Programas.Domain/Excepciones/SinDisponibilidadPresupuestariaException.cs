namespace Ascenso.Programas.Domain.Excepciones;

public sealed class SinDisponibilidadPresupuestariaException : DomainException
{
    public const string CodigoError = "SIN_DISPONIBILIDAD_PRESUPUESTARIA";

    public SinDisponibilidadPresupuestariaException(Guid programaId, decimal montoSolicitado)
        : base(
            CodigoError,
            409,
            $"El programa '{programaId}' no cuenta con cupos disponibles o saldo presupuestario suficiente para cubrir la reserva de {montoSolicitado:0.00} USD (RN-PRG-03).")
    {
        ProgramaId = programaId;
        MontoSolicitado = montoSolicitado;
    }

    public Guid ProgramaId { get; }

    public decimal MontoSolicitado { get; }
}
