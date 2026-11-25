using Ascenso.Programas.Application.Convocatorias.Abstracciones;
using Ascenso.Programas.Application.Convocatorias.EventosIntegracion;
using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Domain.Convocatorias.Eventos;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Convocatorias.Comandos.CerrarConvocatoriasVencidas;

public sealed class CerrarConvocatoriasVencidasCommandHandler
{
    private readonly IConvocatoriaRepository _convocatoriaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRelojConvocatorias _reloj;
    private readonly IIntegrationEventPublisher _eventPublisher;

    public CerrarConvocatoriasVencidasCommandHandler(
        IConvocatoriaRepository convocatoriaRepository,
        IUnitOfWork unitOfWork,
        IRelojConvocatorias reloj,
        IIntegrationEventPublisher eventPublisher)
    {
        _convocatoriaRepository = convocatoriaRepository ?? throw new ArgumentNullException(nameof(convocatoriaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    public async Task<int> HandleAsync(
        CerrarConvocatoriasVencidasCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.UsuarioSistemaId == Guid.Empty)
        {
            throw new ArgumentException("El usuario de sistema es obligatorio.", nameof(command));
        }

        var ahora = _reloj.Ahora;
        var vencidas = await _convocatoriaRepository.ObtenerPublicadasVencidasAsync(ahora, cancellationToken);
        if (vencidas.Count == 0)
        {
            return 0;
        }

        var cerradas = 0;

        foreach (var convocatoria in vencidas)
        {
            if (!convocatoria.Cerrar(ahora, command.UsuarioSistemaId))
            {
                continue;
            }

            cerradas++;
            _convocatoriaRepository.Actualizar(convocatoria);

            foreach (var eventoDominio in convocatoria.EventosDominio.OfType<ConvocatoriaCerradaDominioEvent>())
            {
                await _eventPublisher.PublicarAsync(
                    new ConvocatoriaCerrada(
                        eventoDominio.EventId,
                        eventoDominio.ConvocatoriaId,
                        eventoDominio.ProgramaBecaId,
                        eventoDominio.Codigo,
                        eventoDominio.FechaCierre,
                        eventoDominio.CorrelationId,
                        eventoDominio.OccurredAt),
                    cancellationToken);
            }
        }

        if (cerradas > 0)
        {
            await _unitOfWork.GuardarCambiosAsync(cancellationToken);
        }

        return cerradas;
    }
}
