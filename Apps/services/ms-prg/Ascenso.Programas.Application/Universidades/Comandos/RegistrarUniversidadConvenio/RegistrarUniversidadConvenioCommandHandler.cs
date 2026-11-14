using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Application.Universidades.DTOs;
using Ascenso.Programas.Application.Universidades.EventosIntegracion;
using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using Ascenso.Programas.Domain.Universidades.Agregados;
using Ascenso.Programas.Domain.Universidades.ObjetosValor;
using FluentValidation;

namespace Ascenso.Programas.Application.Universidades.Comandos.RegistrarUniversidadConvenio;

public sealed class RegistrarUniversidadConvenioCommandHandler
{
    private readonly IUniversidadAliadaRepository _universidadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IValidator<RegistrarUniversidadConvenioCommand> _validator;

    public RegistrarUniversidadConvenioCommandHandler(
        IUniversidadAliadaRepository universidadRepository,
        IUnitOfWork unitOfWork,
        IIntegrationEventPublisher eventPublisher,
        IValidator<RegistrarUniversidadConvenioCommand> validator)
    {
        _universidadRepository = universidadRepository ?? throw new ArgumentNullException(nameof(universidadRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<UniversidadDetalleDto> HandleAsync(
        RegistrarUniversidadConvenioCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var codigoNormalizado = command.CodigoInstitucional.Trim().ToUpperInvariant();
        if (await _universidadRepository.ExisteCodigoInstitucionalAsync(codigoNormalizado, cancellationToken))
        {
            throw new CodigoInstitucionalDuplicadoException(codigoNormalizado);
        }

        var contacto = new ContactoEnlace(
            command.ContactoEnlace.Nombre,
            command.ContactoEnlace.Email,
            command.ContactoEnlace.Telefono);

        var cuenta = command.CuentaBancariaInstitucional is null
            ? null
            : new CuentaBancariaInstitucional(
                command.CuentaBancariaInstitucional.Banco,
                command.CuentaBancariaInstitucional.TipoCuenta,
                command.CuentaBancariaInstitucional.NumeroCuentaCifrado,
                command.CuentaBancariaInstitucional.RifTitular);

        var universidad = UniversidadAliada.CrearConConvenioInicial(
            codigoNormalizado,
            command.Nombre,
            command.Direccion,
            contacto,
            cuenta,
            command.UsuarioEnlaceId,
            command.NumeroConvenio,
            command.TerminosResumen,
            command.FechaInicioConvenio,
            command.FechaFinConvenio,
            command.UrlDocumentoPdfFirebase,
            command.PermiteIntercambioAutomaticoDatos,
            command.UsuarioCreacionId);

        var convenioInicial = universidad.Convenios.Single();

        await _universidadRepository.AgregarAsync(universidad, cancellationToken);

        var evento = new ConvenioUniversidadRegistrado(
            UuidV7.Nuevo(),
            DateTimeOffset.UtcNow,
            universidad.Id,
            convenioInicial.Id,
            universidad.CodigoInstitucional,
            convenioInicial.NumeroConvenio,
            convenioInicial.FechaInicio,
            convenioInicial.FechaFin,
            convenioInicial.UrlDocumentoPdfFirebase);

        await _eventPublisher.PublicarAsync(evento, cancellationToken);
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return universidad.ADetalleDto();
    }
}
