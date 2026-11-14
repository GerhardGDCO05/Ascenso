using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Donantes.Comandos.DesactivarDonante;

public sealed class DesactivarDonanteCommandHandler
{
    private readonly IDonanteRepository _donanteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DesactivarDonanteCommandHandler(
        IDonanteRepository donanteRepository,
        IUnitOfWork unitOfWork)
    {
        _donanteRepository = donanteRepository ?? throw new ArgumentNullException(nameof(donanteRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<DonanteDetalleDto> HandleAsync(
        DesactivarDonanteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.DonanteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del donante es obligatorio.", nameof(command));
        }

        if (command.UsuarioModificacionId == Guid.Empty)
        {
            throw new ArgumentException("El usuario modificador es obligatorio.", nameof(command));
        }

        var donante = await _donanteRepository.ObtenerPorIdAsync(command.DonanteId, cancellationToken)
            ?? throw new DonanteNoEncontradoException(command.DonanteId);

        var tieneFondosComprometidos = await _donanteRepository
            .TieneFondosComprometidosEnProgramaVigenteAsync(command.DonanteId, cancellationToken);

        donante.Desactivar(tieneFondosComprometidos, command.UsuarioModificacionId);

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return donante.ADetalleDto();
    }
}

