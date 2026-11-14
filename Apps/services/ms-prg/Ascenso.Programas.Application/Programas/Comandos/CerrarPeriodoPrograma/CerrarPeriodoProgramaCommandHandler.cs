using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Application.Programas.DTOs;
using Ascenso.Programas.Application.Programas.EventosIntegracion;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using Ascenso.Programas.Domain.Programas.Eventos;
using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.CerrarPeriodoPrograma;

public sealed class CerrarPeriodoProgramaCommandHandler
{
    private readonly IProgramaBecaRepository _programaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IValidator<CerrarPeriodoProgramaCommand> _validator;

    public CerrarPeriodoProgramaCommandHandler(
        IProgramaBecaRepository programaRepository,
        IUnitOfWork unitOfWork,
        IIntegrationEventPublisher eventPublisher,
        IValidator<CerrarPeriodoProgramaCommand> validator)
    {
        _programaRepository = programaRepository ?? throw new ArgumentNullException(nameof(programaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<CierrePeriodoProgramaResponse> HandleAsync(
        CerrarPeriodoProgramaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var programa = await _programaRepository.ObtenerPorIdAsync(command.ProgramaBecaId, cancellationToken)
            ?? throw new ProgramaBecaNoEncontradoException(command.ProgramaBecaId);

        var movimiento = programa.CerrarPeriodo(
            command.PeriodoAcademicoId,
            command.Observaciones,
            command.CerrarProgramaDefinitivamente,
            command.UsuarioModificacionId);

        var eventoDominio = programa.EventosDominio
            .OfType<PeriodoProgramaCerradoDominioEvent>()
            .LastOrDefault();

        if (eventoDominio is not null)
        {
            await _eventPublisher.PublicarAsync(
                new PeriodoProgramaCerrado(
                    eventoDominio.EventId,
                    eventoDominio.ProgramaBecaId,
                    eventoDominio.PeriodoAcademicoId,
                    eventoDominio.SaldoNoEjecutadoConsolidado,
                    eventoDominio.PresupuestoEjecutadoTotal,
                    eventoDominio.EstadoResultante.ACodigo(),
                    eventoDominio.CorrelationId,
                    eventoDominio.OccurredAt),
                cancellationToken);
        }

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return new CierrePeriodoProgramaResponse(
            programa.Id,
            movimiento.PeriodoAcademicoId,
            programa.Estado.ACodigo(),
            movimiento.Monto,
            programa.PresupuestoEjecutado,
            ProgramaBecaMapeador.AMovimientoResponse(movimiento),
            ProgramaBecaMapeador.ABalanceResponse(programa));
    }
}
