using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Donantes.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Programas.Application.Donantes.Comandos.RegistrarDonante;

public sealed class RegistrarDonanteCommandHandler
{
    private readonly IDonanteRepository _donanteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegistrarDonanteCommand> _validator;

    public RegistrarDonanteCommandHandler(
        IDonanteRepository donanteRepository,
        IUnitOfWork unitOfWork,
        IValidator<RegistrarDonanteCommand> validator)
    {
        _donanteRepository = donanteRepository ?? throw new ArgumentNullException(nameof(donanteRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<DonanteDetalleDto> HandleAsync(
        RegistrarDonanteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var identificacionNormalizada = command.IdentificacionFiscal.Trim().ToUpperInvariant();
        if (await _donanteRepository.ExisteIdentificacionFiscalAsync(identificacionNormalizada, cancellationToken))
        {
            throw new IdentificacionFiscalDuplicadaException(identificacionNormalizada);
        }

        _ = DonanteMapeador.IntentarConvertirEnum<TipoDonante>(command.TipoDonante, out var tipoDonante);

        var contacto = new ContactoDonante(
            command.ContactoPrincipal.NombreRepresentante,
            command.ContactoPrincipal.Cargo,
            command.ContactoPrincipal.EmailContacto,
            command.ContactoPrincipal.TelefonoContacto);

        var donante = Donante.Crear(
            tipoDonante,
            command.RazonSocialNombre,
            identificacionNormalizada,
            command.Email,
            command.Telefono,
            command.DireccionFiscal,
            contacto,
            command.UsuarioCreacionId);

        await _donanteRepository.AgregarAsync(donante, cancellationToken);
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return donante.ADetalleDto();
    }
}

