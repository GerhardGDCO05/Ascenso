using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Rubricas.DTOs;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Rubricas.Comandos.AgregarCriterio;

public sealed class AgregarCriterioCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IRubricaRepository _rubricaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<AgregarCriterioCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public AgregarCriterioCommandHandler(
        IUsuarioActual usuarioActual,
        IRubricaRepository rubricaRepository,
        IUnitOfWork unitOfWork,
        IValidator<AgregarCriterioCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _rubricaRepository = rubricaRepository ?? throw new ArgumentNullException(nameof(rubricaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RubricaDto> HandleAsync(
        AgregarCriterioCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var rubrica = await _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(command.RubricaId, cancellationToken)
            ?? throw new RubricaNoEncontradaException();

        rubrica.AgregarCriterio(
            command.Nombre,
            command.Descripcion,
            command.PesoPorcentual,
            command.PuntajeMinimo,
            command.PuntajeMaximo,
            _timeProvider.GetUtcNow(),
            usuarioId);

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return rubrica.ADto();
    }
}
