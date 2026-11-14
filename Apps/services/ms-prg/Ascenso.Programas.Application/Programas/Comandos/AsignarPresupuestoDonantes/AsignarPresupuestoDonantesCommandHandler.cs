using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Application.Programas.DTOs;
using Ascenso.Programas.Application.Programas.EventosIntegracion;
using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using Ascenso.Programas.Domain.Programas.Eventos;
using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.AsignarPresupuestoDonantes;

public sealed class AsignarPresupuestoDonantesCommandHandler
{
    private readonly IProgramaBecaRepository _programaRepository;
    private readonly IDonanteRepository _donanteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IValidator<AsignarPresupuestoDonantesCommand> _validator;

    public AsignarPresupuestoDonantesCommandHandler(
        IProgramaBecaRepository programaRepository,
        IDonanteRepository donanteRepository,
        IUnitOfWork unitOfWork,
        IIntegrationEventPublisher eventPublisher,
        IValidator<AsignarPresupuestoDonantesCommand> validator)
    {
        _programaRepository = programaRepository ?? throw new ArgumentNullException(nameof(programaRepository));
        _donanteRepository = donanteRepository ?? throw new ArgumentNullException(nameof(donanteRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ProgramaBecaDetalleDto> HandleAsync(
        AsignarPresupuestoDonantesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var programa = await _programaRepository.ObtenerPorIdAsync(command.ProgramaBecaId, cancellationToken)
            ?? throw new ProgramaBecaNoEncontradoException(command.ProgramaBecaId);

        var donantesCargados = new Dictionary<Guid, Donante>();

        foreach (var item in command.Asignaciones)
        {
            if (!donantesCargados.TryGetValue(item.DonanteId, out var donante))
            {
                donante = await _donanteRepository.ObtenerPorIdAsync(item.DonanteId, cancellationToken)
                    ?? throw new DonanteNoEncontradoException(item.DonanteId);
                donantesCargados[item.DonanteId] = donante;
            }

            donante.ComprometerSaldoParaPrograma(item.MontoAsignado, command.AutorizadoPorUsuarioId);
            programa.AsignarFondoDonante(
                donante.Id,
                item.PeriodoAcademicoId,
                item.MontoAsignado,
                command.AutorizadoPorUsuarioId);
        }

        foreach (var eventoDominio in programa.EventosDominio.OfType<PresupuestoProgramaAsignadoDominioEvent>())
        {
            await _eventPublisher.PublicarAsync(
                new PresupuestoProgramaAsignado(
                    eventoDominio.EventId,
                    eventoDominio.ProgramaBecaId,
                    eventoDominio.AsignacionId,
                    eventoDominio.DonanteId,
                    eventoDominio.PeriodoAcademicoId,
                    eventoDominio.MontoAsignado,
                    eventoDominio.PresupuestoTotalResultante,
                    eventoDominio.CorrelationId,
                    eventoDominio.OccurredAt),
                cancellationToken);
        }

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return ProgramaBecaMapeador.ADetalleDto(programa);
    }
}
