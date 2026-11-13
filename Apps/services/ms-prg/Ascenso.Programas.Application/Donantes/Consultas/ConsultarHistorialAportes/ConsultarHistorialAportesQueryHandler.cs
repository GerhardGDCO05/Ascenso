using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Donantes.Consultas.ConsultarHistorialAportes;

public sealed class ConsultarHistorialAportesQueryHandler
{
    private readonly IDonanteRepository _donanteRepository;

    public ConsultarHistorialAportesQueryHandler(IDonanteRepository donanteRepository)
    {
        _donanteRepository = donanteRepository ?? throw new ArgumentNullException(nameof(donanteRepository));
    }

    public async Task<IReadOnlyList<AporteDonanteDto>> HandleAsync(
        ConsultarHistorialAportesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.DonanteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del donante es obligatorio.", nameof(query));
        }

        if (!await _donanteRepository.ExisteAsync(query.DonanteId, cancellationToken))
        {
            throw new DonanteNoEncontradoException(query.DonanteId);
        }

        var periodoFiltro = string.IsNullOrWhiteSpace(query.PeriodoAcademicoId)
            ? null
            : query.PeriodoAcademicoId.Trim();

        var aportes = await _donanteRepository.ConsultarHistorialAportesAsync(
            query.DonanteId,
            periodoFiltro,
            cancellationToken);

        return aportes.Select(aporte => aporte.ADto()).ToList();
    }
}

