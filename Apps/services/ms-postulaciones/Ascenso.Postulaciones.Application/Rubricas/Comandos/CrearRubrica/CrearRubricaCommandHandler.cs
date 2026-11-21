using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Rubricas.DTOs;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Rubricas.Comandos.CrearRubrica;

public sealed class CrearRubricaCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IRubricaRepository _rubricaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CrearRubricaCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public CrearRubricaCommandHandler(
        IUsuarioActual usuarioActual,
        IRubricaRepository rubricaRepository,
        IUnitOfWork unitOfWork,
        IValidator<CrearRubricaCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _rubricaRepository = rubricaRepository ?? throw new ArgumentNullException(nameof(rubricaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RubricaDto> HandleAsync(
        CrearRubricaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioId = _usuarioActual.ObtenerUsuarioIdRequerido();

        if (await _rubricaRepository.ExistePorProgramaYConvocatoriaAsync(
                command.ProgramaBecaId,
                command.ConvocatoriaId,
                cancellationToken))
        {
            throw new RubricaDuplicadaException();
        }

        var rubrica = RubricaEvaluacion.Crear(
            command.ProgramaBecaId,
            command.ConvocatoriaId,
            command.Nombre,
            _timeProvider.GetUtcNow(),
            usuarioId);

        await _rubricaRepository.AgregarAsync(rubrica, cancellationToken);
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return rubrica.ADto();
    }
}
