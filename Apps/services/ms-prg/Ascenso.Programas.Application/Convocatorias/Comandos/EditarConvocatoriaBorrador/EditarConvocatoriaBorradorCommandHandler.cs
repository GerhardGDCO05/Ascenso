using Ascenso.Programas.Application.Convocatorias.Abstracciones;
using Ascenso.Programas.Application.Convocatorias.DTOs;
using Ascenso.Programas.Application.Convocatorias.Servicios;
using Ascenso.Programas.Domain.Convocatorias.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Programas.Application.Convocatorias.Comandos.EditarConvocatoriaBorrador;

public sealed class EditarConvocatoriaBorradorCommandHandler
{
    private readonly IConvocatoriaRepository _convocatoriaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ValidadorDisponibilidadConvocatoria _validadorDisponibilidad;
    private readonly IConvocatoriaTransactionExecutor _transaccion;
    private readonly IValidator<EditarConvocatoriaBorradorCommand> _validator;

    public EditarConvocatoriaBorradorCommandHandler(
        IConvocatoriaRepository convocatoriaRepository,
        IUnitOfWork unitOfWork,
        ValidadorDisponibilidadConvocatoria validadorDisponibilidad,
        IConvocatoriaTransactionExecutor transaccion,
        IValidator<EditarConvocatoriaBorradorCommand> validator)
    {
        _convocatoriaRepository = convocatoriaRepository ?? throw new ArgumentNullException(nameof(convocatoriaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validadorDisponibilidad = validadorDisponibilidad ?? throw new ArgumentNullException(nameof(validadorDisponibilidad));
        _transaccion = transaccion ?? throw new ArgumentNullException(nameof(transaccion));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ConvocatoriaDetalleDto> HandleAsync(
        EditarConvocatoriaBorradorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var convocatoria = await _convocatoriaRepository.ObtenerPorIdAsync(command.ConvocatoriaId, cancellationToken)
            ?? throw new ConvocatoriaNoEncontradaException(command.ConvocatoriaId);

        if (convocatoria.Estado != EstadoConvocatoria.Borrador)
        {
            throw new ConvocatoriaNoEditableException(convocatoria.Id, convocatoria.Estado);
        }

        var criterios = new CriteriosElegibilidadCiclo(
            command.CriteriosElegibilidad.PromedioMinimo,
            command.CriteriosElegibilidad.EdadMinima,
            command.CriteriosElegibilidad.EdadMaxima,
            command.CriteriosElegibilidad.TopeIngresoFamiliar,
            command.CriteriosElegibilidad.UniversidadesPermitidasIds,
            command.CriteriosElegibilidad.CarrerasPermitidasIds);

        await _validadorDisponibilidad.ValidarPeriodoAsync(command.PeriodoAcademicoId, cancellationToken);

        return await _transaccion.ExecuteAsync(async token =>
        {
            await _validadorDisponibilidad.ValidarProgramaAsync(
                convocatoria.ProgramaBecaId,
                command.CuposAsignados,
                criterios,
                token);

            convocatoria.Actualizar(
                command.PeriodoAcademicoId,
                command.Nombre,
                CalendarioConvocatoria.NormalizarInicioDelDia(command.FechaApertura),
                CalendarioConvocatoria.NormalizarFinDelDia(command.FechaCierre),
                CalendarioConvocatoria.NormalizarFinDelDia(command.FechaLimiteSubsanacionDocumentos),
                CalendarioConvocatoria.NormalizarInicioDelDia(command.FechaInicioEvaluacion),
                CalendarioConvocatoria.NormalizarFinDelDia(command.FechaCierreEvaluacion),
                command.RequiereRevisionDocumentalPrevia,
                command.CuposAsignados,
                criterios,
                command.UsuarioModificacionId);

            _convocatoriaRepository.Actualizar(convocatoria);
            await _unitOfWork.GuardarCambiosAsync(token);

            return ConvocatoriaMapeador.ADetalleDto(convocatoria);
        }, cancellationToken);
    }
}
