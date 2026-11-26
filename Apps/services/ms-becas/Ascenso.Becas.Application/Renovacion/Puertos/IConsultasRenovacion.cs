using Ascenso.Becas.Application.Renovacion.Dtos;

namespace Ascenso.Becas.Application.Renovacion.Puertos;

public interface IConsultasRenovacion
{
    Task<SolicitudRenovacionDto?> ObtenerAsync(Guid solicitudId,
        CancellationToken cancellationToken = default);

    Task<PaginaRenovacionesDto> ListarPorBecaAsync(Guid becaId,
        int pagina, int tamano, CancellationToken cancellationToken = default);
}
