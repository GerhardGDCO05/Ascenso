using Ascenso.Postulaciones.Domain.Interfaces.Contratos;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto del catálogo académico de programas y universidades aliadas.
/// </summary>
public interface ICatalogoAcademicoClient
{
    Task<UniversidadAliadaCatalogo> ObtenerUniversidadAsync(
        Guid universidadAliadaId,
        CancellationToken cancellationToken = default);
}
