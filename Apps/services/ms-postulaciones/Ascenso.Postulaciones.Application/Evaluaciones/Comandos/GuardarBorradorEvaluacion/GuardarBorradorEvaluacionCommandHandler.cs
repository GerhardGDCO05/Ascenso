using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Evaluaciones.DTOs;
using Ascenso.Postulaciones.Application.Evaluaciones.Interfaces;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Evaluaciones.Comandos.GuardarBorradorEvaluacion;

public sealed class GuardarBorradorEvaluacionCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IConsultaAsignacionParaEvaluacionRepository _asignacionRepository;
    private readonly IConvocatoriaEvaluacionClient _convocatoriaClient;
    private readonly IComiteEvaluadorRepository _comiteRepository;
    private readonly IConsultaRubricaParaEvaluacionRepository _rubricaRepository;
    private readonly IEvaluacionRepository _evaluacionRepository;
    private readonly IBloqueadorRubricaEvaluacion _bloqueadorRubrica;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<GuardarBorradorEvaluacionCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public GuardarBorradorEvaluacionCommandHandler(
        IUsuarioActual usuarioActual,
        IConsultaAsignacionParaEvaluacionRepository asignacionRepository,
        IConvocatoriaEvaluacionClient convocatoriaClient,
        IComiteEvaluadorRepository comiteRepository,
        IConsultaRubricaParaEvaluacionRepository rubricaRepository,
        IEvaluacionRepository evaluacionRepository,
        IBloqueadorRubricaEvaluacion bloqueadorRubrica,
        IUnitOfWork unitOfWork,
        IValidator<GuardarBorradorEvaluacionCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _asignacionRepository = asignacionRepository ?? throw new ArgumentNullException(nameof(asignacionRepository));
        _convocatoriaClient = convocatoriaClient ?? throw new ArgumentNullException(nameof(convocatoriaClient));
        _comiteRepository = comiteRepository ?? throw new ArgumentNullException(nameof(comiteRepository));
        _rubricaRepository = rubricaRepository ?? throw new ArgumentNullException(nameof(rubricaRepository));
        _evaluacionRepository = evaluacionRepository ?? throw new ArgumentNullException(nameof(evaluacionRepository));
        _bloqueadorRubrica = bloqueadorRubrica ?? throw new ArgumentNullException(nameof(bloqueadorRubrica));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ResultadoGuardarBorradorEvaluacionDto> HandleAsync(
        GuardarBorradorEvaluacionCommand command,
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

        var ahora = _timeProvider.GetUtcNow();

        var ventanaRevision = await _convocatoriaClient
            .ObtenerVentanaRevisionAsync(asignacion.ConvocatoriaId, cancellationToken)
            ?? throw new ConvocatoriaNoEncontradaException();
        ventanaRevision.AsegurarPeriodoAbiertoParaEvaluacion(ahora);

        var comite = await _comiteRepository
            .ObtenerPorIdAsync(asignacion.ComiteEvaluadorId, cancellationToken)
            ?? throw new ComiteNoEncontradoException();

        var rubrica = await _rubricaRepository
            .ObtenerPorProgramaYConvocatoriaAsync(
                comite.ProgramaBecaId,
                comite.ConvocatoriaId,
                cancellationToken)
            ?? throw new RubricaNoEncontradaException();

        var evaluacion = await _evaluacionRepository
            .ObtenerParaActualizarPorAsignacionAsync(asignacion.Id, cancellationToken);

        var datosCalificaciones = command.Calificaciones
            .Select(item => new DatosCalificacionCriterio(
                item.CriterioEvaluacionId,
                item.PuntajeAsignado,
                item.Comentario))
            .ToArray();

        bool esCreacion;

        if (evaluacion is null)
        {
            evaluacion = Evaluacion.CrearBorrador(
                asignacion,
                rubrica,
                datosCalificaciones,
                command.ComentariosGenerales,
                ahora,
                usuarioEvaluadorId);

            asignacion.IniciarEvaluacion(ahora, usuarioEvaluadorId);
            await _evaluacionRepository.AgregarAsync(evaluacion, cancellationToken);
            esCreacion = true;
        }
        else
        {
            evaluacion.ActualizarBorrador(
                rubrica,
                datosCalificaciones,
                command.ComentariosGenerales,
                asignacion.FechaLimiteEvaluacion,
                ahora,
                usuarioEvaluadorId);

            asignacion.IniciarEvaluacion(ahora, usuarioEvaluadorId);
            esCreacion = false;
        }

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        if (rubrica.Estado == EstadoRubrica.Publicada)
        {
            await _bloqueadorRubrica.BloquearPorPrimeraCalificacionAsync(
                rubrica.Id,
                cancellationToken);
        }

        return new ResultadoGuardarBorradorEvaluacionDto(evaluacion.ADto(), esCreacion);
    }
}
