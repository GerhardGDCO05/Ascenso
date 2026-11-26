using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.SeguimientoAcademico.Dtos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.SeguimientoAcademico.Eventos;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Comandos.AplicarConsecuenciaPermanencia;

/// <summary>
/// Ejecuta el llamado de atención. Las transiciones monetarias permanecen
/// cerradas hasta contar con los movimientos confirmados de MS-PRG.
/// </summary>
public sealed class AplicarConsecuenciaPermanenciaHandler
    : ICommandHandler<AplicarConsecuenciaPermanenciaCommand, EvaluacionPermanenciaDto>
{
    private readonly IReporteAcademicoRepository _reportes;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IOutboxBeca _outbox;

    public AplicarConsecuenciaPermanenciaHandler(
        IReporteAcademicoRepository reportes, IUnitOfWork unidadDeTrabajo,
        IContextoOperacionActual contextoActual, IClock reloj,
        IOutboxBeca outbox)
    {
        _reportes = reportes ?? throw new ArgumentNullException(nameof(reportes));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public async Task<EvaluacionPermanenciaDto> HandleAsync(
        AplicarConsecuenciaPermanenciaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        EvaluacionPermanenciaDto? resultado = null;
        await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async token =>
        {
            var reporte = await _reportes.ObtenerPorIdAsync(command.ReporteAcademicoId, token)
                ?? throw new RecursoNoEncontradoException(
                    "reporte académico", command.ReporteAcademicoId);
            if (reporte.Evaluacion?.EstadoConfirmacion != EstadoConsecuencia.Confirmada)
                throw new ConflictoOperacionException(
                    "La consecuencia requiere confirmación del coordinador.");
            if (reporte.Evaluacion.ConsecuenciaRecomendada != ConsecuenciaPermanencia.LlamadoAtencion)
                throw new ConflictoOperacionException(
                    "La suspensión o revocación académica requiere primero la coordinación financiera de MS-PRG.");

            var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj);
            try
            {
                reporte.MarcarConsecuenciaAplicada(contexto);
            }
            catch (InvalidOperationException excepcion)
            {
                throw new ConflictoOperacionException(excepcion.Message);
            }
            resultado = EvaluacionPermanenciaDto.Desde(reporte.Evaluacion);
            foreach (var evento in reporte.EventosDominio
                .OfType<ConsecuenciaPermanenciaAplicadaEvento>())
                await _outbox.EncolarAsync(evento, reporte.Version, token);
            await _unidadDeTrabajo.GuardarCambiosAsync(token);
        }, cancellationToken);
        return resultado ?? throw new InvalidOperationException(
            "La aplicación no produjo un resultado.");
    }
}
