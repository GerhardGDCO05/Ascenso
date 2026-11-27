using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Comandos.RevisarDiscrepanciaConsolidacion;

public sealed class RevisarDiscrepanciaConsolidacionCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IResultadoConsolidadoRepository _resultadoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RevisarDiscrepanciaConsolidacionCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public RevisarDiscrepanciaConsolidacionCommandHandler(
        IUsuarioActual usuarioActual,
        IResultadoConsolidadoRepository resultadoRepository,
        IUnitOfWork unitOfWork,
        IValidator<RevisarDiscrepanciaConsolidacionCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _resultadoRepository = resultadoRepository ?? throw new ArgumentNullException(nameof(resultadoRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ResultadoConsolidadoDto> HandleAsync(
        RevisarDiscrepanciaConsolidacionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioRevisorId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var resultado = await _resultadoRepository
            .ObtenerParaActualizarPorPostulacionAsync(command.PostulacionId, cancellationToken)
            ?? throw new ResultadoConsolidadoNoEncontradoException();

        resultado.MarcarDiscrepanciaRevisada(
            command.Comentario,
            _timeProvider.GetUtcNow(),
            usuarioRevisorId);

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return resultado.ADto();
    }
}
