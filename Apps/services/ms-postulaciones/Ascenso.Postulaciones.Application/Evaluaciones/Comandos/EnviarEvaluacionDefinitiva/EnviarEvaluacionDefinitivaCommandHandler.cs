using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Evaluaciones.DTOs;
using Ascenso.Postulaciones.Application.Evaluaciones.Interfaces;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Evaluaciones.Comandos.EnviarEvaluacionDefinitiva;

public sealed class EnviarEvaluacionDefinitivaCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IConsultaAsignacionParaEvaluacionRepository _asignacionRepository;
    private readonly IEvaluacionRepository _evaluacionRepository;
    private readonly IComiteEvaluadorRepository _comiteRepository;
    private readonly IConsultaRubricaParaEvaluacionRepository _rubricaRepository;
    private readonly IBloqueadorRubricaEvaluacion _bloqueadorRubrica;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<EnviarEvaluacionDefinitivaCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public EnviarEvaluacionDefinitivaCommandHandler(
        IUsuarioActual usuarioActual,
        IConsultaAsignacionParaEvaluacionRepository asignacionRepository,
        IEvaluacionRepository evaluacionRepository,
        IComiteEvaluadorRepository comiteRepository,
        IConsultaRubricaParaEvaluacionRepository rubricaRepository,
        IBloqueadorRubricaEvaluacion bloqueadorRubrica,
        IUnitOfWork unitOfWork,
        IValidator<EnviarEvaluacionDefinitivaCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _asignacionRepository = asignacionRepository ?? throw new ArgumentNullException(nameof(asignacionRepository));
        _evaluacionRepository = evaluacionRepository ?? throw new ArgumentNullException(nameof(evaluacionRepository));
        _comiteRepository = comiteRepository ?? throw new ArgumentNullException(nameof(comiteRepository));
        _rubricaRepository = rubricaRepository ?? throw new ArgumentNullException(nameof(rubricaRepository));
        _bloqueadorRubrica = bloqueadorRubrica ?? throw new ArgumentNullException(nameof(bloqueadorRubrica));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<EvaluacionDto> HandleAsync(
        EnviarEvaluacionDefinitivaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioEvaluadorId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var asignacion = await _asignacionRepository
            .ObtenerParaActualizarPorIdAsync(command.AsignacionEvaluacionId, cancellationToken)
            ?? throw new AsignacionEvaluacionNoEncontradaException();

        if (asignacion.UsuarioEvaluadorId != usuarioEvaluadorId)
        {
            throw new EvaluacionConfidencialException();
        }

        var evaluacion = await _evaluacionRepository
            .ObtenerParaActualizarPorAsignacionAsync(asignacion.Id, cancellationToken)
            ?? throw new EvaluacionNoEncontradaException();

        var comite = await _comiteRepository
            .ObtenerPorIdAsync(asignacion.ComiteEvaluadorId, cancellationToken)
            ?? throw new ComiteNoEncontradoException();

        var rubrica = await _rubricaRepository
            .ObtenerPorProgramaYConvocatoriaAsync(
                comite.ProgramaBecaId,
                comite.ConvocatoriaId,
                cancellationToken)
            ?? throw new RubricaNoEncontradaException();

        var ahora = _timeProvider.GetUtcNow();

        evaluacion.EnviarDefinitiva(
            rubrica,
            asignacion.FechaLimiteEvaluacion,
            ahora,
            usuarioEvaluadorId);

        asignacion.CompletarEvaluacion(ahora, usuarioEvaluadorId);

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        if (rubrica.Estado == EstadoRubrica.Publicada)
        {
            await _bloqueadorRubrica.BloquearPorPrimeraCalificacionAsync(
                rubrica.Id,
                cancellationToken);
        }

        return evaluacion.ADto();
    }
}
