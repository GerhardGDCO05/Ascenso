using Ascenso.Programas.Application.Convocatorias.Abstracciones;
using Ascenso.Programas.Application.Convocatorias.DTOs;
using Ascenso.Programas.Application.Convocatorias.EventosIntegracion;
using Ascenso.Programas.Application.Convocatorias.Servicios;
using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Domain.Convocatorias.Eventos;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Convocatorias.Comandos.PublicarConvocatoria;

public sealed class PublicarConvocatoriaCommandHandler
{
    private readonly IConvocatoriaRepository _convocatoriaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ValidadorDisponibilidadConvocatoria _validadorDisponibilidad;
    private readonly IConvocatoriaTransactionExecutor _transaccion;
    private readonly IRelojConvocatorias _reloj;
    private readonly IIntegrationEventPublisher _eventPublisher;

    public PublicarConvocatoriaCommandHandler(
        IConvocatoriaRepository convocatoriaRepository,
        IUnitOfWork unitOfWork,
        ValidadorDisponibilidadConvocatoria validadorDisponibilidad,
        IConvocatoriaTransactionExecutor transaccion,
        IRelojConvocatorias reloj,
        IIntegrationEventPublisher eventPublisher)
    {
        _convocatoriaRepository = convocatoriaRepository ?? throw new ArgumentNullException(nameof(convocatoriaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validadorDisponibilidad = validadorDisponibilidad ?? throw new ArgumentNullException(nameof(validadorDisponibilidad));
        _transaccion = transaccion ?? throw new ArgumentNullException(nameof(transaccion));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    public async Task<ConvocatoriaDetalleDto> HandleAsync(
        PublicarConvocatoriaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ConvocatoriaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la convocatoria es obligatorio.", nameof(command));
        }

        if (command.UsuarioModificacionId == Guid.Empty)
        {
            throw new ArgumentException("El usuario que publica la convocatoria es obligatorio.", nameof(command));
        }

        var convocatoria = await _convocatoriaRepository.ObtenerPorIdAsync(command.ConvocatoriaId, cancellationToken)
            ?? throw new ConvocatoriaNoEncontradaException(command.ConvocatoriaId);

        if (convocatoria.Estado != EstadoConvocatoria.Borrador)
        {
            throw new ConvocatoriaNoEditableException(convocatoria.Id, convocatoria.Estado);
        }

        await _validadorDisponibilidad.ValidarPeriodoAsync(
            convocatoria.PeriodoAcademicoId,
            cancellationToken);

        return await _transaccion.ExecuteAsync(async token =>
        {
            await _validadorDisponibilidad.ValidarProgramaAsync(
                convocatoria.ProgramaBecaId,
                convocatoria.CuposAsignados,
                convocatoria.CriteriosElegibilidad,
                token);

            convocatoria.Publicar(_reloj.Ahora, command.UsuarioModificacionId);
            _convocatoriaRepository.Actualizar(convocatoria);

            foreach (var eventoDominio in convocatoria.EventosDominio.OfType<ConvocatoriaPublicadaDominioEvent>())
            {
                await _eventPublisher.PublicarAsync(
                    new ConvocatoriaPublicada(
                        eventoDominio.EventId,
                        eventoDominio.ConvocatoriaId,
                        eventoDominio.ProgramaBecaId,
                        eventoDominio.Codigo,
                        eventoDominio.FechaApertura,
                        eventoDominio.FechaCierre,
                        eventoDominio.CuposAsignados,
                        eventoDominio.CorrelationId,
                        eventoDominio.OccurredAt),
                    token);
            }

            await _unitOfWork.GuardarCambiosAsync(token);

            return ConvocatoriaMapeador.ADetalleDto(convocatoria);
        }, cancellationToken);
    }
}
