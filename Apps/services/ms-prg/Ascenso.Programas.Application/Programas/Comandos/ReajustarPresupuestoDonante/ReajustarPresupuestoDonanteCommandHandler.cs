using Ascenso.Programas.Application.Programas.DTOs;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.ReajustarPresupuestoDonante;

public sealed class ReajustarPresupuestoDonanteCommandHandler
{
    private readonly IProgramaBecaRepository _programaRepository;
    private readonly IDonanteRepository _donanteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ReajustarPresupuestoDonanteCommand> _validator;

    public ReajustarPresupuestoDonanteCommandHandler(
        IProgramaBecaRepository programaRepository,
        IDonanteRepository donanteRepository,
        IUnitOfWork unitOfWork,
        IValidator<ReajustarPresupuestoDonanteCommand> validator)
    {
        _programaRepository = programaRepository ?? throw new ArgumentNullException(nameof(programaRepository));
        _donanteRepository = donanteRepository ?? throw new ArgumentNullException(nameof(donanteRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ProgramaBecaDetalleDto> HandleAsync(
        ReajustarPresupuestoDonanteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var programa = await _programaRepository.ObtenerPorIdAsync(command.ProgramaBecaId, cancellationToken)
            ?? throw new ProgramaBecaNoEncontradoException(command.ProgramaBecaId);

        var (donanteId, montoRestituido) = programa.ReajustarAsignacionFondo(
            command.AsignacionId,
            command.MontoAReducir,
            command.UsuarioModificacionId);

        var donante = await _donanteRepository.ObtenerPorIdAsync(donanteId, cancellationToken)
            ?? throw new DonanteNoEncontradoException(donanteId);

        donante.ReintegrarSaldoDePrograma(montoRestituido, command.UsuarioModificacionId);

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return ProgramaBecaMapeador.ADetalleDto(programa);
    }
}
