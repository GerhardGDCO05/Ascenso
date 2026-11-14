namespace Ascenso.Programas.Domain.Excepciones;

public sealed class PresupuestoEnEjecucionException : DomainException
{
    public const string CodigoError = "PRESUPUESTO_EN_EJECUCION";

    public PresupuestoEnEjecucionException(Guid programaId)
        : base(
            CodigoError,
            409,
            $"No es posible reducir o desasignar fondos del programa '{programaId}' porque se encuentra en ejecución o fuera de edición (RN-PRG-02).")
    {
        ProgramaId = programaId;
    }

    public Guid ProgramaId { get; }
}
