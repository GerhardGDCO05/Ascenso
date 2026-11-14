using Ascenso.Programas.Application.Programas.DTOs;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Programas.Consultas.ObtenerProgramaPorId;

public sealed class ObtenerProgramaPorIdQueryHandler
{
    private readonly IProgramaBecaRepository _programaRepository;

    public ObtenerProgramaPorIdQueryHandler(IProgramaBecaRepository programaRepository)
    {
        _programaRepository = programaRepository ?? throw new ArgumentNullException(nameof(programaRepository));
    }

    public async Task<ProgramaBecaDetalleDto> HandleAsync(
        ObtenerProgramaPorIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ProgramaBecaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del programa de becas es obligatorio.", nameof(query));
        }

        var programa = await _programaRepository.ObtenerPorIdAsync(query.ProgramaBecaId, cancellationToken)
            ?? throw new ProgramaBecaNoEncontradoException(query.ProgramaBecaId);

        return ProgramaBecaMapeador.ADetalleDto(programa);
    }
}
