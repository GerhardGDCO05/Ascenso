using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Application.Programas.DTOs;
using Ascenso.Programas.Application.Programas.EventosIntegracion;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using Ascenso.Programas.Domain.Programas.Eventos;
using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.ConfirmarReservaPresupuesto;

public sealed class ConfirmarReservaPresupuestoCommandHandler
{
    private readonly IProgramaBecaRepository _programaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IValidator<ConfirmarReservaPresupuestoCommand> _validator;

    public ConfirmarReservaPresupuestoCommandHandler(
        IProgramaBecaRepository programaRepository,
        IUnitOfWork unitOfWork,
        IIntegrationEventPublisher eventPublisher,
        IValidator<ConfirmarReservaPresupuestoCommand> validator)
    {
        _programaRepository = programaRepository ?? throw new ArgumentNullException(nameof(programaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ReservaPresupuestariaResponse> HandleAsync(
        ConfirmarReservaPresupuestoCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var programa = await _programaRepository.ObtenerPorIdAsync(command.ProgramaBecaId, cancellationToken)
            ?? throw new ProgramaBecaNoEncontradoException(command.ProgramaBecaId);

        var (reserva, _) = programa.ConfirmarReservaPresupuesto(
            command.ReferenciaExternaId,
            command.Descripcion,
            command.UsuarioModificacionId);

        var eventoDominio = programa.EventosDominio
            .OfType<PresupuestoProgramaConfirmadoDominioEvent>()
            .LastOrDefault();

        if (eventoDominio is not null)
        {
            await _eventPublisher.PublicarAsync(
                new PresupuestoProgramaConfirmado(
                    eventoDominio.EventId,
                    eventoDominio.ProgramaBecaId,
                    eventoDominio.ReservaId,
                    eventoDominio.ReferenciaExternaId,
                    eventoDominio.PeriodoAcademicoId,
                    eventoDominio.MontoConfirmado,
                    eventoDominio.PresupuestoEjecutadoResultante,
                    eventoDominio.CuposOcupadosResultantes,
                    eventoDominio.CorrelationId,
                    eventoDominio.OccurredAt),
                cancellationToken);
        }

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return ProgramaBecaMapeador.AReservaResponse(reserva, programa);
    }
}
