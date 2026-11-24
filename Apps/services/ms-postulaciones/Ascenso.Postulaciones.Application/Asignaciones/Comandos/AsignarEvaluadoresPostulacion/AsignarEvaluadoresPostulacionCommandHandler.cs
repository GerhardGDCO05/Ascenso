using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Servicios;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Asignaciones.Comandos.AsignarEvaluadoresPostulacion;

public sealed class AsignarEvaluadoresPostulacionCommandHandler
{
    private static readonly IReadOnlyDictionary<Guid, int> _cargaVacia =
        new Dictionary<Guid, int>();

    private readonly IUsuarioActual _usuarioActual;
    private readonly IComiteEvaluadorRepository _comiteRepository;
    private readonly IAsignacionEvaluacionRepository _asignacionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<AsignarEvaluadoresPostulacionCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public AsignarEvaluadoresPostulacionCommandHandler(
        IUsuarioActual usuarioActual,
        IComiteEvaluadorRepository comiteRepository,
        IAsignacionEvaluacionRepository asignacionRepository,
        IUnitOfWork unitOfWork,
        IValidator<AsignarEvaluadoresPostulacionCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _comiteRepository = comiteRepository ?? throw new ArgumentNullException(nameof(comiteRepository));
        _asignacionRepository = asignacionRepository ?? throw new ArgumentNullException(nameof(asignacionRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AsignacionesPostulacionDto> HandleAsync(
        AsignarEvaluadoresPostulacionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var comite = await _comiteRepository
            .ObtenerPorIdAsync(command.ComiteId, cancellationToken)
            ?? throw new ComiteNoEncontradoException();

        var vigentes = await _asignacionRepository
            .ListarVigentesPorPostulacionAsync(command.PostulacionId, cancellationToken);

        var cargaPorMiembro = command.CantidadEvaluadores.HasValue
            ? await _asignacionRepository.ObtenerCargaPorMiembroAsync(comite.Id, cancellationToken)
            : _cargaVacia;

        var nuevasAsignaciones = ServicioAsignacionEvaluadores.Asignar(
            comite,
            command.PostulacionId,
            command.UsuariosEvaluadoresIds,
            command.CantidadEvaluadores,
            vigentes,
            cargaPorMiembro,
            _timeProvider.GetUtcNow(),
            command.FechaLimiteEvaluacion,
            usuarioId);

        await _asignacionRepository.AgregarRangoAsync(nuevasAsignaciones, cancellationToken);
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return new AsignacionesPostulacionDto(
            command.PostulacionId,
            nuevasAsignaciones.Select(asignacion => asignacion.ADto()).ToArray());
    }
}
