using Ascenso.Programas.Application.Convocatorias.DTOs;
using Ascenso.Programas.Application.Convocatorias.Abstracciones;
using Ascenso.Programas.Application.Convocatorias.Servicios;
using Ascenso.Programas.Domain.Convocatorias.Agregados;
using Ascenso.Programas.Domain.Convocatorias.ObjetosValor;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Programas.Application.Convocatorias.Comandos.CrearConvocatoriaBorrador;

public sealed class CrearConvocatoriaBorradorCommandHandler
{
    private readonly IConvocatoriaRepository _convocatoriaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ValidadorDisponibilidadConvocatoria _validadorDisponibilidad;
    private readonly IConvocatoriaTransactionExecutor _transaccion;
    private readonly IValidator<CrearConvocatoriaBorradorCommand> _validator;

    public CrearConvocatoriaBorradorCommandHandler(
        IConvocatoriaRepository convocatoriaRepository,
        IUnitOfWork unitOfWork,
        ValidadorDisponibilidadConvocatoria validadorDisponibilidad,
        IConvocatoriaTransactionExecutor transaccion,
        IValidator<CrearConvocatoriaBorradorCommand> validator)
    {
        _convocatoriaRepository = convocatoriaRepository ?? throw new ArgumentNullException(nameof(convocatoriaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validadorDisponibilidad = validadorDisponibilidad ?? throw new ArgumentNullException(nameof(validadorDisponibilidad));
        _transaccion = transaccion ?? throw new ArgumentNullException(nameof(transaccion));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ConvocatoriaDetalleDto> HandleAsync(
        CrearConvocatoriaBorradorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var codigoNormalizado = command.Codigo.Trim().ToUpperInvariant();
        if (await _convocatoriaRepository.ExisteCodigoAsync(codigoNormalizado, cancellationToken))
        {
            throw new CodigoConvocatoriaDuplicadoException(codigoNormalizado);
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
                command.ProgramaBecaId,
                command.CuposAsignados,
                criterios,
                token);

            var convocatoria = Convocatoria.CrearBorrador(
                command.ProgramaBecaId,
                command.PeriodoAcademicoId,
                codigoNormalizado,
                command.Nombre,
                CalendarioConvocatoria.NormalizarInicioDelDia(command.FechaApertura),
                CalendarioConvocatoria.NormalizarFinDelDia(command.FechaCierre),
                CalendarioConvocatoria.NormalizarFinDelDia(command.FechaLimiteSubsanacionDocumentos),
                CalendarioConvocatoria.NormalizarInicioDelDia(command.FechaInicioEvaluacion),
                CalendarioConvocatoria.NormalizarFinDelDia(command.FechaCierreEvaluacion),
                command.RequiereRevisionDocumentalPrevia,
                command.CuposAsignados,
                criterios,
                command.UsuarioCreacionId);

            await _convocatoriaRepository.AgregarAsync(convocatoria, token);
            await _unitOfWork.GuardarCambiosAsync(token);

            return ConvocatoriaMapeador.ADetalleDto(convocatoria);
        }, cancellationToken);
    }
}
