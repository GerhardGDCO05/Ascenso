using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Servicios;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Asignaciones.Comandos.AutorizarExcepcionEvaluadorUnico;

public sealed class AutorizarExcepcionEvaluadorUnicoCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IExcepcionEvaluadorUnicoRepository _excepcionRepository;
    private readonly IAsignacionEvaluacionRepository _asignacionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<AutorizarExcepcionEvaluadorUnicoCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public AutorizarExcepcionEvaluadorUnicoCommandHandler(
        IUsuarioActual usuarioActual,
        IExcepcionEvaluadorUnicoRepository excepcionRepository,
        IAsignacionEvaluacionRepository asignacionRepository,
        IUnitOfWork unitOfWork,
        IValidator<AutorizarExcepcionEvaluadorUnicoCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _excepcionRepository = excepcionRepository ?? throw new ArgumentNullException(nameof(excepcionRepository));
        _asignacionRepository = asignacionRepository ?? throw new ArgumentNullException(nameof(asignacionRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ExcepcionEvaluadorUnicoDto> HandleAsync(
        AutorizarExcepcionEvaluadorUnicoCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var existente = await _excepcionRepository
            .ObtenerPorPostulacionAsync(command.PostulacionId, cancellationToken);

        var vigentes = await _asignacionRepository
            .ListarVigentesPorPostulacionAsync(command.PostulacionId, cancellationToken);

        var autorizacion = ServicioExcepcionEvaluadorUnico.Autorizar(
            command.PostulacionId,
            command.Justificacion,
            vigentes,
            yaAutorizada: existente is not null,
            _timeProvider.GetUtcNow(),
            usuarioId,
            UuidV7.Nuevo());

        await _excepcionRepository.AgregarAsync(autorizacion, cancellationToken);
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return autorizacion.ADto();
    }
}
