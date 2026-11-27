using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerResultadoConsolidadoPostulacion;

public sealed class ObtenerResultadoConsolidadoPostulacionQueryHandler
{
    private readonly IResultadoConsolidadoRepository _resultadoRepository;
    private readonly IConsultaDatosConsolidacionRepository _consultaDatosRepository;
    private readonly IValidator<ObtenerResultadoConsolidadoPostulacionQuery> _validator;

    public ObtenerResultadoConsolidadoPostulacionQueryHandler(
        IResultadoConsolidadoRepository resultadoRepository,
        IConsultaDatosConsolidacionRepository consultaDatosRepository,
        IValidator<ObtenerResultadoConsolidadoPostulacionQuery> validator)
    {
        _resultadoRepository = resultadoRepository ?? throw new ArgumentNullException(nameof(resultadoRepository));
        _consultaDatosRepository = consultaDatosRepository ?? throw new ArgumentNullException(nameof(consultaDatosRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<DetalleResultadoConsolidadoDto> HandleAsync(
        ObtenerResultadoConsolidadoPostulacionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        var resultado = await _resultadoRepository
            .ObtenerPorPostulacionAsync(query.PostulacionId, cancellationToken)
            ?? throw new ResultadoConsolidadoNoEncontradoException();

        var evaluaciones = await _consultaDatosRepository
            .ListarEvaluacionesDefinitivasPorPostulacionAsync(query.PostulacionId, cancellationToken);

        return resultado.ADetalleDto(evaluaciones);
    }
}
