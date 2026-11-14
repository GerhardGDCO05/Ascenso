using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Application.Programas.DTOs;
using Ascenso.Programas.Application.Programas.EventosIntegracion;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using Ascenso.Programas.Domain.Programas.Eventos;
using Ascenso.Programas.Domain.Programas.ObjetosValor;
using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.ConfigurarCriteriosElegibilidad;

public sealed class ConfigurarCriteriosElegibilidadCommandHandler
{
    private readonly IProgramaBecaRepository _programaRepository;
    private readonly IUniversidadAliadaRepository _universidadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IValidator<ConfigurarCriteriosElegibilidadCommand> _validator;

    public ConfigurarCriteriosElegibilidadCommandHandler(
        IProgramaBecaRepository programaRepository,
        IUniversidadAliadaRepository universidadRepository,
        IUnitOfWork unitOfWork,
        IIntegrationEventPublisher eventPublisher,
        IValidator<ConfigurarCriteriosElegibilidadCommand> validator)
    {
        _programaRepository = programaRepository ?? throw new ArgumentNullException(nameof(programaRepository));
        _universidadRepository = universidadRepository ?? throw new ArgumentNullException(nameof(universidadRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ProgramaBecaDetalleDto> HandleAsync(
        ConfigurarCriteriosElegibilidadCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var programa = await _programaRepository.ObtenerPorIdAsync(command.ProgramaBecaId, cancellationToken)
            ?? throw new ProgramaBecaNoEncontradoException(command.ProgramaBecaId);

        var criterios = new CriteriosElegibilidadPrograma(
            command.CriteriosElegibilidad.PromedioMinimoIngreso,
            command.CriteriosElegibilidad.EdadMinima,
            command.CriteriosElegibilidad.EdadMaxima,
            command.CriteriosElegibilidad.TopeIngresoFamiliarMensual,
            command.CriteriosElegibilidad.UniversidadesElegiblesIds,
            command.CriteriosElegibilidad.CarrerasElegiblesIds);

        var frecuencia = CodigoEnum.DesdeCodigo<FrecuenciaReporteAcademico>(
            command.CondicionesPermanencia.FrecuenciaReportes);
        var consecuencia = CodigoEnum.DesdeCodigo<ConsecuenciaIncumplimiento>(
            command.CondicionesPermanencia.ConsecuenciaIncumplimiento);

        var condiciones = new CondicionesPermanencia(
            command.CondicionesPermanencia.PromedioMinimoRenovacion,
            command.CondicionesPermanencia.MateriasMinimasAprobadas,
            command.CondicionesPermanencia.MaximoMateriasReprobadas,
            command.CondicionesPermanencia.HorasServicioPorPeriodo,
            frecuencia,
            consecuencia,
            command.CondicionesPermanencia.MargenRiesgoPromedio,
            command.CondicionesPermanencia.DuracionMaximaPausaPeriodos,
            command.CondicionesPermanencia.PermiteRenovacionTardiaExcepcional);

        var todasLasUniversidadesTienenConvenioVigente = true;
        var carrerasActivasEnUniversidades = new HashSet<Guid>();

        foreach (var universidadId in criterios.UniversidadesElegiblesIds)
        {
            var universidad = await _universidadRepository.ObtenerPorIdAsync(universidadId, cancellationToken);
            if (universidad is null || !universidad.TieneConvenioVigente())
            {
                todasLasUniversidadesTienenConvenioVigente = false;
                continue;
            }

            foreach (var carrera in universidad.Carreras.Where(c => c.Activa))
            {
                carrerasActivasEnUniversidades.Add(carrera.Id);
            }
        }

        var todasLasCarrerasSonElegibles = criterios.CarrerasElegiblesIds
            .All(carreraId => carrerasActivasEnUniversidades.Contains(carreraId));

        programa.ConfigurarElegibilidadYPermanencia(
            criterios,
            condiciones,
            todasLasUniversidadesTienenConvenioVigente,
            todasLasCarrerasSonElegibles,
            command.UsuarioModificacionId);

        foreach (var eventoDominio in programa.EventosDominio.OfType<ProgramaBecaConfiguradoDominioEvent>())
        {
            await _eventPublisher.PublicarAsync(
                new ProgramaBecaConfigurado(
                    eventoDominio.EventId,
                    eventoDominio.ProgramaBecaId,
                    eventoDominio.Codigo,
                    eventoDominio.PresupuestoTotalAsignado,
                    eventoDominio.CupoTotal,
                    eventoDominio.UniversidadesElegiblesIds,
                    eventoDominio.CarrerasElegiblesIds,
                    eventoDominio.CorrelationId,
                    eventoDominio.OccurredAt),
                cancellationToken);
        }

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return ProgramaBecaMapeador.ADetalleDto(programa);
    }
}
