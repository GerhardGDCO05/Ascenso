namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

/// <summary>
/// Acceso a las proyecciones locales de programa, costo de matrícula y
/// convenio que mantiene el servicio a partir de los eventos de <c>MS-PRG</c>.
/// </summary>
public interface IProyeccionesOtorgamiento
{
    Task<ProyeccionOtorgamiento?> ObtenerAsync(
        Guid programaBecaId,
        Guid carreraId,
        Guid periodoAcademicoId,
        Guid universidadAliadaId,
        DateOnly fecha,
        CancellationToken cancellationToken = default);
}
