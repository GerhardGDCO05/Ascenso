namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ReservasPendientesEnPeriodoException : DomainException
{
    public const string CodigoError = "RESERVAS_PENDIENTES_EN_PERIODO";

    public ReservasPendientesEnPeriodoException(Guid programaId, string periodoAcademicoId, int cantidadPendientes)
        : base(
            CodigoError,
            409,
            $"No es posible cerrar el período '{periodoAcademicoId}' del programa '{programaId}' porque aún existen {cantidadPendientes} reserva(s) pendiente(s) en estado Reservada.")
    {
        ProgramaId = programaId;
        PeriodoAcademicoId = periodoAcademicoId;
        CantidadPendientes = cantidadPendientes;
    }

    public Guid ProgramaId { get; }

    public string PeriodoAcademicoId { get; }

    public int CantidadPendientes { get; }
}
