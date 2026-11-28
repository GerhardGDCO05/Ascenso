using Ascenso.Postulaciones.Application.ListaEspera.DTOs;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.ListaEspera.Consultas.ObtenerSiguienteCandidatoListaEspera;

public sealed class ObtenerSiguienteCandidatoListaEsperaQueryHandler
{
    private readonly IEntradaListaEsperaRepository _entradaListaEsperaRepository;
    private readonly IValidator<ObtenerSiguienteCandidatoListaEsperaQuery> _validator;

    public ObtenerSiguienteCandidatoListaEsperaQueryHandler(
        IEntradaListaEsperaRepository entradaListaEsperaRepository,
        IValidator<ObtenerSiguienteCandidatoListaEsperaQuery> validator)
    {
        _entradaListaEsperaRepository = entradaListaEsperaRepository
            ?? throw new ArgumentNullException(nameof(entradaListaEsperaRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<EntradaListaEsperaDto> HandleAsync(
        ObtenerSiguienteCandidatoListaEsperaQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        var siguiente = await _entradaListaEsperaRepository
            .ObtenerSiguientePendientePorConvocatoriaAsync(query.ConvocatoriaId, cancellationToken)
            ?? throw new ListaEsperaSinCandidatosException();

        return siguiente.ADto();
    }
}
