namespace Ascenso.Postulaciones.Application.ListaEspera.Comandos.RegistrarVacanteLiberada;

public sealed record class RegistrarVacanteLiberadaCommand(
    Guid ConvocatoriaId,
    string? MotivoVacante = null)
{
    public const int LongitudMaximaMotivoVacante = 500;
}
