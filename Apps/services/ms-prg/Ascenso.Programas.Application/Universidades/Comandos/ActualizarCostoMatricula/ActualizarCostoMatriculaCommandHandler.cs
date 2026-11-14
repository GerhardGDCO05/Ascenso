using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Application.Universidades.DTOs;
using Ascenso.Programas.Application.Universidades.EventosIntegracion;
using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Programas.Application.Universidades.Comandos.ActualizarCostoMatricula;

public sealed class ActualizarCostoMatriculaCommandHandler
{
    private readonly IUniversidadAliadaRepository _universidadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IValidator<ActualizarCostoMatriculaCommand> _validator;

    public ActualizarCostoMatriculaCommandHandler(
        IUniversidadAliadaRepository universidadRepository,
        IUnitOfWork unitOfWork,
        IIntegrationEventPublisher eventPublisher,
        IValidator<ActualizarCostoMatriculaCommand> validator)
    {
        _universidadRepository = universidadRepository ?? throw new ArgumentNullException(nameof(universidadRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<CostoMatriculaReferenciaDto> HandleAsync(
        ActualizarCostoMatriculaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var universidad = await _universidadRepository.ObtenerPorIdAsync(command.UniversidadAliadaId, cancellationToken)
            ?? throw new UniversidadAliadaNoEncontradaException(command.UniversidadAliadaId);

        var costo = universidad.ActualizarCostoMatricula(
            command.CarreraElegibleId,
            command.PeriodoAcademicoId,
            command.MontoReferenciaMatricula,
            command.Moneda,
            command.UsuarioModificacionId);

        var evento = new CostoMatriculaActualizado(
            UuidV7.Nuevo(),
            DateTimeOffset.UtcNow,
            universidad.Id,
            costo.CarreraElegibleId,
            costo.Id,
            costo.PeriodoAcademicoId,
            costo.NumeroVersion,
            costo.MontoReferenciaMatricula,
            costo.Moneda);

        await _eventPublisher.PublicarAsync(evento, cancellationToken);
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return costo.ADto();
    }
}
