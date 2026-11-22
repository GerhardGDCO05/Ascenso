using Ascenso.Postulaciones.Application.Comites.DTOs;
using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Comites.Comandos.DesvincularMiembroComite;

public sealed class DesvincularMiembroComiteCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IComiteEvaluadorRepository _comiteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<DesvincularMiembroComiteCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public DesvincularMiembroComiteCommandHandler(
        IUsuarioActual usuarioActual,
        IComiteEvaluadorRepository comiteRepository,
        IUnitOfWork unitOfWork,
        IValidator<DesvincularMiembroComiteCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _comiteRepository = comiteRepository ?? throw new ArgumentNullException(nameof(comiteRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ComiteEvaluadorDto> HandleAsync(
        DesvincularMiembroComiteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var comite = await _comiteRepository
            .ObtenerParaActualizarPorIdAsync(command.ComiteId, cancellationToken)
            ?? throw new ComiteNoEncontradoException();

        var desvinculado = comite.DesvincularMiembro(
            command.MiembroId,
            _timeProvider.GetUtcNow(),
            usuarioId);

        if (!desvinculado)
        {
            return comite.ADto();
        }

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return comite.ADto();
    }
}
