using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Application.Universidades.DTOs;
using Ascenso.Programas.Application.Universidades.EventosIntegracion;
using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Programas.Application.Universidades.Comandos.RegistrarNuevoConvenio;

public sealed class RegistrarNuevoConvenioCommandHandler
{
    private readonly IUniversidadAliadaRepository _universidadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IValidator<RegistrarNuevoConvenioCommand> _validator;

    public RegistrarNuevoConvenioCommandHandler(
        IUniversidadAliadaRepository universidadRepository,
        IUnitOfWork unitOfWork,
        IIntegrationEventPublisher eventPublisher,
        IValidator<RegistrarNuevoConvenioCommand> validator)
    {
        _universidadRepository = universidadRepository ?? throw new ArgumentNullException(nameof(universidadRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ConvenioDto> HandleAsync(
        RegistrarNuevoConvenioCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var universidad = await _universidadRepository.ObtenerPorIdAsync(command.UniversidadAliadaId, cancellationToken)
            ?? throw new UniversidadAliadaNoEncontradaException(command.UniversidadAliadaId);

        var convenio = universidad.RegistrarNuevoConvenio(
            command.NumeroConvenio,
            command.TerminosResumen,
            command.FechaInicio,
            command.FechaFin,
            command.UrlDocumentoPdfFirebase,
            command.PermiteIntercambioAutomaticoDatos,
            command.UsuarioModificacionId);

        var evento = new ConvenioUniversidadRegistrado(
            UuidV7.Nuevo(),
            DateTimeOffset.UtcNow,
            universidad.Id,
            convenio.Id,
            universidad.CodigoInstitucional,
            convenio.NumeroConvenio,
            convenio.FechaInicio,
            convenio.FechaFin,
            convenio.UrlDocumentoPdfFirebase);

        await _eventPublisher.PublicarAsync(evento, cancellationToken);
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return convenio.ADto();
    }
}
