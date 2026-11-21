using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Rubricas.DTOs;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;

namespace Ascenso.Postulaciones.Application.Rubricas.Comandos.EliminarCriterio;

public sealed class EliminarCriterioCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IRubricaRepository _rubricaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public EliminarCriterioCommandHandler(
        IUsuarioActual usuarioActual,
        IRubricaRepository rubricaRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _rubricaRepository = rubricaRepository ?? throw new ArgumentNullException(nameof(rubricaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RubricaDto> HandleAsync(
        EliminarCriterioCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var usuarioId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var rubrica = await _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(command.RubricaId, cancellationToken)
            ?? throw new RubricaNoEncontradaException();

        rubrica.EliminarCriterio(
            command.CriterioId,
            _timeProvider.GetUtcNow(),
            usuarioId);

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return rubrica.ADto();
    }
}
