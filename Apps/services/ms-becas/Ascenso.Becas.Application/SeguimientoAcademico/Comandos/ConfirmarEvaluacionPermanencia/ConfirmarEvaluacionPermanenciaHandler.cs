using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento;
using Ascenso.Becas.Application.SeguimientoAcademico.Dtos;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Comandos.ConfirmarEvaluacionPermanencia;

/// <summary>Registra la decisión explícita sin aplicar todavía una consecuencia a la beca.</summary>
public sealed class ConfirmarEvaluacionPermanenciaHandler
    : ICommandHandler<ConfirmarEvaluacionPermanenciaCommand, EvaluacionPermanenciaDto>
{
    private readonly IReporteAcademicoRepository _reportes;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;

    public ConfirmarEvaluacionPermanenciaHandler(
        IReporteAcademicoRepository reportes, IUnitOfWork unidadDeTrabajo,
        IContextoOperacionActual contextoActual, IClock reloj)
    {
        _reportes = reportes ?? throw new ArgumentNullException(nameof(reportes));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
    }

    public async Task<EvaluacionPermanenciaDto> HandleAsync(
        ConfirmarEvaluacionPermanenciaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        EvaluacionPermanenciaDto? resultado = null;
        await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async token =>
        {
            var reporte = await _reportes.ObtenerPorIdAsync(command.ReporteAcademicoId, token)
                ?? throw new RecursoNoEncontradoException(
                    "reporte académico", command.ReporteAcademicoId);
            var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj,
                command.MotivoDecision);
            try
            {
                reporte.DecidirConsecuencia(command.Confirmar,
                    command.MotivoDecision, command.SiguientePasoBecario, contexto);
                reporte.ResolverRevision(command.Confirmar, contexto);
            }
            catch (ArgumentException excepcion)
            {
                throw new SolicitudInvalidaException(excepcion.Message);
            }
            catch (InvalidOperationException excepcion)
            {
                throw new ConflictoOperacionException(excepcion.Message);
            }

            resultado = EvaluacionPermanenciaDto.Desde(reporte.Evaluacion!);
            await _unidadDeTrabajo.GuardarCambiosAsync(token);
        }, cancellationToken);
        return resultado ?? throw new InvalidOperationException(
            "La confirmación no produjo un resultado.");
    }
}
