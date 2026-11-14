using Ascenso.Programas.Application.Universidades.DTOs;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Programas.Application.Universidades.Comandos.AgregarCarreraElegible;

public sealed class AgregarCarreraElegibleCommandHandler
{
    private readonly IUniversidadAliadaRepository _universidadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<AgregarCarreraElegibleCommand> _validator;

    public AgregarCarreraElegibleCommandHandler(
        IUniversidadAliadaRepository universidadRepository,
        IUnitOfWork unitOfWork,
        IValidator<AgregarCarreraElegibleCommand> validator)
    {
        _universidadRepository = universidadRepository ?? throw new ArgumentNullException(nameof(universidadRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<CarreraElegibleDto> HandleAsync(
        AgregarCarreraElegibleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var universidad = await _universidadRepository.ObtenerPorIdAsync(command.UniversidadAliadaId, cancellationToken)
            ?? throw new UniversidadAliadaNoEncontradaException(command.UniversidadAliadaId);

        var carrera = universidad.AgregarCarreraElegible(
            command.CodigoCarrera,
            command.Nombre,
            command.DuracionSemestres,
            command.UsuarioModificacionId);

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return carrera.ADto();
    }
}
