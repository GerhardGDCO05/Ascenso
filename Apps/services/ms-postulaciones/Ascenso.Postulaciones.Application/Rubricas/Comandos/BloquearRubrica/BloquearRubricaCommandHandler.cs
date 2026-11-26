using Ascenso.Postulaciones.Application.Rubricas.DTOs;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;

namespace Ascenso.Postulaciones.Application.Rubricas.Comandos.BloquearRubrica;

public sealed class BloquearRubricaCommandHandler
{
    private readonly IRubricaRepository _rubricaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public BloquearRubricaCommandHandler(
        IRubricaRepository rubricaRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _rubricaRepository = rubricaRepository ?? throw new ArgumentNullException(nameof(rubricaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RubricaDto> HandleAsync(
        BloquearRubricaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rubrica = await _rubricaRepository
            .ObtenerParaActualizarPorIdAsync(command.RubricaId, cancellationToken)
            ?? throw new RubricaNoEncontradaException();

        var cambioAplicado = rubrica.Bloquear(
            command.Motivo,
            _timeProvider.GetUtcNow(),
            UsuarioSistema.Id);

        if (!cambioAplicado)
        {
            return rubrica.ADto();
        }

        try
        {
            await _unitOfWork.GuardarCambiosAsync(cancellationToken);

            return rubrica.ADto();
        }
        catch (ExcepcionConflictoConcurrencia)
        {
            var rubricaActual = await _rubricaRepository
                .ObtenerPorIdAsync(command.RubricaId, cancellationToken);

            if (rubricaActual is not null && rubricaActual.Estado == EstadoRubrica.Bloqueada)
            {
                return rubricaActual.ADto();
            }

            throw;
        }
    }
}
