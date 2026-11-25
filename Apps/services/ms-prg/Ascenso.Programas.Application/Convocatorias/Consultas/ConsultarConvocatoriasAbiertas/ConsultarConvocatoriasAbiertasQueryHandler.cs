using Ascenso.Programas.Application.Convocatorias.Abstracciones;
using Ascenso.Programas.Application.Convocatorias.DTOs;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Convocatorias.Consultas.ConsultarConvocatoriasAbiertas;

public sealed class ConsultarConvocatoriasAbiertasQueryHandler
{
    private readonly IConvocatoriaRepository _convocatoriaRepository;
    private readonly IRelojConvocatorias _reloj;

    public ConsultarConvocatoriasAbiertasQueryHandler(
        IConvocatoriaRepository convocatoriaRepository,
        IRelojConvocatorias reloj)
    {
        _convocatoriaRepository = convocatoriaRepository ?? throw new ArgumentNullException(nameof(convocatoriaRepository));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
    }

    public async Task<IReadOnlyList<ConvocatoriaAbiertaDto>> HandleAsync(
        ConsultarConvocatoriasAbiertasQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var abiertas = await _convocatoriaRepository.ConsultarAbiertasAsync(_reloj.Ahora, cancellationToken);
        return abiertas.Select(ConvocatoriaMapeador.AAbiertaDto).ToArray();
    }
}
