using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerExcepcionEvaluadorUnico;

public sealed class ObtenerExcepcionEvaluadorUnicoQueryHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IExcepcionEvaluadorUnicoRepository _excepcionRepository;
    private readonly IValidator<ObtenerExcepcionEvaluadorUnicoQuery> _validator;

    public ObtenerExcepcionEvaluadorUnicoQueryHandler(
        IUsuarioActual usuarioActual,
        IExcepcionEvaluadorUnicoRepository excepcionRepository,
        IValidator<ObtenerExcepcionEvaluadorUnicoQuery> validator)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _excepcionRepository = excepcionRepository ?? throw new ArgumentNullException(nameof(excepcionRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ExcepcionEvaluadorUnicoDto> HandleAsync(
        ObtenerExcepcionEvaluadorUnicoQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        _ = _usuarioActual.ObtenerUsuarioIdRequerido();

        var autorizacion = await _excepcionRepository
            .ObtenerPorPostulacionAsync(query.PostulacionId, cancellationToken)
            ?? throw new ExcepcionEvaluadorUnicoNoEncontradaException();

        return autorizacion.ADto();
    }
}
