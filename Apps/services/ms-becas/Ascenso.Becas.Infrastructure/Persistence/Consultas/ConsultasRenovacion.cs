using Ascenso.Becas.Application.Renovacion.Dtos;
using Ascenso.Becas.Application.Renovacion.Puertos;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence.Consultas;

public sealed class ConsultasRenovacion : IConsultasRenovacion
{
    private readonly BecasDbContext _contexto;

    public ConsultasRenovacion(BecasDbContext contexto)
        => _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));

    public async Task<SolicitudRenovacionDto?> ObtenerAsync(Guid solicitudId,
        CancellationToken cancellationToken = default)
    {
        var solicitud = await _contexto.SolicitudesRenovacion.AsNoTracking()
            .SingleOrDefaultAsync(valor => valor.Id == solicitudId,
                cancellationToken);
        return solicitud is null ? null : SolicitudRenovacionDto.Desde(solicitud);
    }

    public async Task<PaginaRenovacionesDto> ListarPorBecaAsync(Guid becaId,
        int pagina, int tamano, CancellationToken cancellationToken = default)
    {
        var consulta = _contexto.SolicitudesRenovacion.AsNoTracking()
            .Where(valor => valor.BecaId == becaId);
        var total = await consulta.CountAsync(cancellationToken);
        var solicitudes = await consulta
            .OrderByDescending(valor => valor.FechaSolicitud)
            .ThenByDescending(valor => valor.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancellationToken);
        return new PaginaRenovacionesDto(pagina, tamano, total,
            solicitudes.Select(SolicitudRenovacionDto.Desde).ToArray());
    }
}
