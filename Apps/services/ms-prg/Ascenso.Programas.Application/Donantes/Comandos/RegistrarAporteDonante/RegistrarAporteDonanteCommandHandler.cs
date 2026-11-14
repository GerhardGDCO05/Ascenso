using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Application.Donantes.EventosIntegracion;
using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Programas.Application.Donantes.Comandos.RegistrarAporteDonante;

public sealed class RegistrarAporteDonanteCommandHandler
{
    private readonly IDonanteRepository _donanteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IValidator<RegistrarAporteDonanteCommand> _validator;

    public RegistrarAporteDonanteCommandHandler(
        IDonanteRepository donanteRepository,
        IUnitOfWork unitOfWork,
        IIntegrationEventPublisher eventPublisher,
        IValidator<RegistrarAporteDonanteCommand> validator)
    {
        _donanteRepository = donanteRepository ?? throw new ArgumentNullException(nameof(donanteRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<AporteDonanteDto> HandleAsync(
        RegistrarAporteDonanteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var donante = await _donanteRepository.ObtenerPorIdAsync(command.DonanteId, cancellationToken)
            ?? throw new DonanteNoEncontradoException(command.DonanteId);

        var referenciaNormalizada = command.NumeroReferenciaBancaria.Trim().ToUpperInvariant();
        if (await _donanteRepository.ExisteReferenciaBancariaAsync(referenciaNormalizada, cancellationToken))
        {
            throw new ReferenciaBancariaDuplicadaException(referenciaNormalizada);
        }

        var aporte = donante.RegistrarAporte(
            command.MontoAporte,
            command.Moneda,
            command.PeriodoAcademicoId,
            command.FechaAporte,
            command.BancoEmisor,
            referenciaNormalizada,
            command.UrlComprobanteFirebase,
            command.RegistradoPorUsuarioId,
            command.Observaciones);

        var eventoIntegracion = new AporteDonanteRegistrado(
            UuidV7.Nuevo(),
            DateTimeOffset.UtcNow,
            donante.Id,
            aporte.Id,
            aporte.MontoAporte,
            aporte.Moneda,
            aporte.PeriodoAcademicoId,
            aporte.NumeroReferenciaBancaria,
            aporte.UrlComprobanteFirebase,
            donante.SaldoTotalHistorico,
            donante.SaldoDisponibleAsignacion);

        await _eventPublisher.PublicarAsync(eventoIntegracion, cancellationToken);
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return aporte.ADto();
    }
}

