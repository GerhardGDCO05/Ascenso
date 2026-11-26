using Ascenso.Postulaciones.Application.Comites.DTOs;
using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Comites.Comandos.CrearComiteEvaluador;

public sealed class CrearComiteEvaluadorCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IComiteEvaluadorRepository _comiteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CrearComiteEvaluadorCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public CrearComiteEvaluadorCommandHandler(
        IUsuarioActual usuarioActual,
        IComiteEvaluadorRepository comiteRepository,
        IUnitOfWork unitOfWork,
        IValidator<CrearComiteEvaluadorCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _comiteRepository = comiteRepository ?? throw new ArgumentNullException(nameof(comiteRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ComiteEvaluadorDto> HandleAsync(
        CrearComiteEvaluadorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioId = _usuarioActual.ObtenerUsuarioIdRequerido();

        if (await _comiteRepository.ExistePorProgramaYConvocatoriaAsync(
                command.ProgramaBecaId,
                command.ConvocatoriaId,
                cancellationToken))
        {
            throw new ComiteDuplicadoException();
        }

        var comite = ComiteEvaluador.Crear(
            command.ProgramaBecaId,
            command.ConvocatoriaId,
            command.Nombre,
            command.JefaturaUsuarioId,
            _timeProvider.GetUtcNow(),
            usuarioId);

        await _comiteRepository.AgregarAsync(comite, cancellationToken);
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return comite.ADto();
    }
}
