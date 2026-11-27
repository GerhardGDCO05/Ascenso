using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Consultas.ConsultarAdjudicabilidadPostulacion;

public sealed class ConsultarAdjudicabilidadPostulacionQueryHandler
{
    private readonly IResultadoConsolidadoRepository _resultadoRepository;
    private readonly IEntradaListaEsperaRepository _entradaListaEsperaRepository;
    private readonly IValidator<ConsultarAdjudicabilidadPostulacionQuery> _validator;

    public ConsultarAdjudicabilidadPostulacionQueryHandler(
        IResultadoConsolidadoRepository resultadoRepository,
        IEntradaListaEsperaRepository entradaListaEsperaRepository,
        IValidator<ConsultarAdjudicabilidadPostulacionQuery> validator)
    {
        _resultadoRepository = resultadoRepository ?? throw new ArgumentNullException(nameof(resultadoRepository));
        _entradaListaEsperaRepository = entradaListaEsperaRepository
            ?? throw new ArgumentNullException(nameof(entradaListaEsperaRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<AdjudicabilidadPostulacionDto> HandleAsync(
        ConsultarAdjudicabilidadPostulacionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        var resultado = await _resultadoRepository
            .ObtenerPorPostulacionAsync(query.PostulacionId, cancellationToken)
            ?? throw new ResultadoConsolidadoNoEncontradoException();

        EntradaListaEspera? entradaListaEspera = null;
        if (resultado.EstadoConfirmacionRanking == EstadoRanking.Confirmado
            && resultado.Clasificacion == ClasificacionRanking.ListaDeEspera)
        {
            entradaListaEspera = await _entradaListaEsperaRepository
                .ObtenerPorPostulacionAsync(query.PostulacionId, cancellationToken);
        }

        return resultado.AAdjudicabilidadDto(entradaListaEspera);
    }
}
