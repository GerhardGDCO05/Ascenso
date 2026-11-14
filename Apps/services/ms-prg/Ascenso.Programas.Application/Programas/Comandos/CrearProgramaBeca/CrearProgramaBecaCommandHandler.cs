using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Application.Programas.DTOs;
using Ascenso.Programas.Application.Programas.EventosIntegracion;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using Ascenso.Programas.Domain.Programas.Agregados;
using Ascenso.Programas.Domain.Programas.Eventos;
using Ascenso.Programas.Domain.Programas.ObjetosValor;
using FluentValidation;

namespace Ascenso.Programas.Application.Programas.Comandos.CrearProgramaBeca;

public sealed class CrearProgramaBecaCommandHandler
{
    private readonly IProgramaBecaRepository _programaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IValidator<CrearProgramaBecaCommand> _validator;

    public CrearProgramaBecaCommandHandler(
        IProgramaBecaRepository programaRepository,
        IUnitOfWork unitOfWork,
        IIntegrationEventPublisher eventPublisher,
        IValidator<CrearProgramaBecaCommand> validator)
    {
        _programaRepository = programaRepository ?? throw new ArgumentNullException(nameof(programaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ProgramaBecaDetalleDto> HandleAsync(
        CrearProgramaBecaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var codigoNormalizado = command.Codigo.Trim().ToUpperInvariant();
        if (await _programaRepository.ExisteCodigoAsync(codigoNormalizado, cancellationToken))
        {
            throw new CodigoProgramaDuplicadoException(codigoNormalizado);
        }

        var modalidad = CodigoEnum.DesdeCodigo<ModalidadCobertura>(command.PoliticaCobertura.Modalidad);
        var politica = new PoliticaCobertura(
            modalidad,
            command.PoliticaCobertura.PorcentajeCobertura,
            command.PoliticaCobertura.MontoFijoPeriodo,
            command.PoliticaCobertura.Moneda);

        var programa = ProgramaBeca.Crear(
            codigoNormalizado,
            command.Nombre,
            command.Descripcion,
            politica,
            command.CupoTotal,
            command.DuracionPeriodos,
            command.PeriodoInicialId,
            command.VigenciaDesde,
            command.VigenciaHasta,
            command.PermiteAcumulacion,
            command.RequiereCompromisoFirmado,
            command.TerminosCompromisoEspecial,
            command.UsuarioCreacionId);

        await _programaRepository.AgregarAsync(programa, cancellationToken);

        foreach (var eventoDominio in programa.EventosDominio.OfType<ProgramaBecaCreadoDominioEvent>())
        {
            await _eventPublisher.PublicarAsync(
                new ProgramaBecaCreado(
                    eventoDominio.EventId,
                    eventoDominio.ProgramaBecaId,
                    eventoDominio.Codigo,
                    eventoDominio.Nombre,
                    eventoDominio.ModalidadCobertura.ACodigo(),
                    eventoDominio.CupoTotal,
                    eventoDominio.PeriodoInicialId,
                    eventoDominio.CorrelationId,
                    eventoDominio.OccurredAt),
                cancellationToken);
        }

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return ProgramaBecaMapeador.ADetalleDto(programa);
    }
}
