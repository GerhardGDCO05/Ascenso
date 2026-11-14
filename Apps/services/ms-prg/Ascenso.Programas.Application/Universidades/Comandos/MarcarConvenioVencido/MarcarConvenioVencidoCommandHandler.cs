using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Application.Universidades.DTOs;
using Ascenso.Programas.Application.Universidades.EventosIntegracion;
using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Universidades.Comandos.MarcarConvenioVencido;

public sealed class MarcarConvenioVencidoCommandHandler
{
    private readonly IUniversidadAliadaRepository _universidadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _eventPublisher;

    public MarcarConvenioVencidoCommandHandler(
        IUniversidadAliadaRepository universidadRepository,
        IUnitOfWork unitOfWork,
        IIntegrationEventPublisher eventPublisher)
    {
        _universidadRepository = universidadRepository ?? throw new ArgumentNullException(nameof(universidadRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    public async Task<ConvenioDto> HandleAsync(
        MarcarConvenioVencidoCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var universidad = await _universidadRepository.ObtenerPorIdAsync(command.UniversidadAliadaId, cancellationToken)
            ?? throw new UniversidadAliadaNoEncontradaException(command.UniversidadAliadaId);

        var convenio = universidad.MarcarConvenioVencido(command.ConvenioId, command.UsuarioModificacionId);

        var evento = new ConvenioUniversidadVencido(
            UuidV7.Nuevo(),
            DateTimeOffset.UtcNow,
            universidad.Id,
            convenio.Id,
            convenio.NumeroConvenio,
            universidad.TieneConvenioVigente());

        await _eventPublisher.PublicarAsync(evento, cancellationToken);
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return convenio.ADto();
    }
}
