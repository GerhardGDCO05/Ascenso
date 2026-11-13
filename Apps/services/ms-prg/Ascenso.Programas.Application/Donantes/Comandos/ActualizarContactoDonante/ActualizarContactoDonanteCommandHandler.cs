using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Domain.Donantes.ObjetosValor;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Programas.Application.Donantes.Comandos.ActualizarContactoDonante;

public sealed class ActualizarContactoDonanteCommandHandler
{
    private readonly IDonanteRepository _donanteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ActualizarContactoDonanteCommand> _validator;

    public ActualizarContactoDonanteCommandHandler(
        IDonanteRepository donanteRepository,
        IUnitOfWork unitOfWork,
        IValidator<ActualizarContactoDonanteCommand> validator)
    {
        _donanteRepository = donanteRepository ?? throw new ArgumentNullException(nameof(donanteRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<DonanteDetalleDto> HandleAsync(
        ActualizarContactoDonanteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var donante = await _donanteRepository.ObtenerPorIdAsync(command.DonanteId, cancellationToken)
            ?? throw new DonanteNoEncontradoException(command.DonanteId);

        var contacto = new ContactoDonante(
            command.ContactoPrincipal.NombreRepresentante,
            command.ContactoPrincipal.Cargo,
            command.ContactoPrincipal.EmailContacto,
            command.ContactoPrincipal.TelefonoContacto);

        donante.ActualizarContacto(
            command.Email,
            command.Telefono,
            command.DireccionFiscal,
            contacto,
            command.UsuarioModificacionId);

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return donante.ADetalleDto();
    }
}

