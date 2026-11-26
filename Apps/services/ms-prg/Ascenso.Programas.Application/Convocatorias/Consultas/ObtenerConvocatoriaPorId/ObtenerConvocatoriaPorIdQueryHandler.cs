using Ascenso.Programas.Application.Convocatorias.DTOs;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Convocatorias.Consultas.ObtenerConvocatoriaPorId;

public sealed class ObtenerConvocatoriaPorIdQueryHandler
{
    private readonly IConvocatoriaRepository _convocatoriaRepository;

    public ObtenerConvocatoriaPorIdQueryHandler(IConvocatoriaRepository convocatoriaRepository)
    {
        _convocatoriaRepository = convocatoriaRepository ?? throw new ArgumentNullException(nameof(convocatoriaRepository));
    }

    public async Task<ConvocatoriaDetalleDto> HandleAsync(
        ObtenerConvocatoriaPorIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ConvocatoriaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la convocatoria es obligatorio.", nameof(query));
        }

        var convocatoria = await _convocatoriaRepository.ObtenerPorIdAsync(query.ConvocatoriaId, cancellationToken)
            ?? throw new ConvocatoriaNoEncontradaException(query.ConvocatoriaId);

        return ConvocatoriaMapeador.ADetalleDto(convocatoria);
    }
}
