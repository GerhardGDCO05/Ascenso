using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Donantes.Consultas.ObtenerDonantePorId;

public sealed class ObtenerDonantePorIdQueryHandler
{
    private readonly IDonanteRepository _donanteRepository;

    public ObtenerDonantePorIdQueryHandler(IDonanteRepository donanteRepository)
    {
        _donanteRepository = donanteRepository ?? throw new ArgumentNullException(nameof(donanteRepository));
    }

    public async Task<DonanteDetalleDto> HandleAsync(
        ObtenerDonantePorIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.DonanteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del donante es obligatorio.", nameof(query));
        }

        var donante = await _donanteRepository.ObtenerPorIdAsync(query.DonanteId, cancellationToken)
            ?? throw new DonanteNoEncontradoException(query.DonanteId);

        return donante.ADetalleDto();
    }
}

