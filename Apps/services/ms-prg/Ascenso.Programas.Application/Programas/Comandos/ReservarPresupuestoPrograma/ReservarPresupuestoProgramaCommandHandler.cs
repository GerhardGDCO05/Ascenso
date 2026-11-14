using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Application.Programas.DTOs;
using Ascenso.Programas.Application.Programas.EventosIntegracion;
using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.ReservarPresupuestoPrograma;

public sealed class ReservarPresupuestoProgramaCommandHandler
{
    private readonly IProgramaBecaRepository _programaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IValidator<ReservarPresupuestoProgramaCommand> _validator;

    public ReservarPresupuestoProgramaCommandHandler(
        IProgramaBecaRepository programaRepository,
        IUnitOfWork unitOfWork,
        IIntegrationEventPublisher eventPublisher,
        IValidator<ReservarPresupuestoProgramaCommand> validator)
    {
        _programaRepository = programaRepository ?? throw new ArgumentNullException(nameof(programaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ReservaPresupuestariaResponse> HandleAsync(
        ReservarPresupuestoProgramaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var (programa, reserva, movimiento, yaExistia) = await _programaRepository.ReservarPresupuestoAtomicoAsync(
            command.ProgramaBecaId,
            command.ReferenciaExternaId,
            command.PeriodoAcademicoId,
            command.Monto,
            command.Descripcion,
            command.UsuarioAutorId,
            cancellationToken);

        if (!yaExistia)
        {
            var ahora = DateTimeOffset.UtcNow;
            await _eventPublisher.PublicarAsync(
                new PresupuestoProgramaReservado(
                    UuidV7.Nuevo(ahora),
                    programa.Id,
                    reserva.Id,
                    reserva.ReferenciaExternaId,
                    reserva.PeriodoAcademicoId,
                    reserva.Monto,
                    programa.PresupuestoDisponible,
                    programa.CuposDisponibles,
                    movimiento.CorrelacionId,
                    movimiento.FechaMovimiento),
                cancellationToken);

            await _unitOfWork.GuardarCambiosAsync(cancellationToken);
        }

        return ProgramaBecaMapeador.AReservaResponse(reserva, programa, yaExistia);
    }
}
