using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.SeguimientoAcademico.Dtos;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.SeguimientoAcademico.Eventos;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Comandos.EvaluarPermanencia;

/// <summary>Evalúa contra las condiciones vigentes del periodo de la beca.</summary>
public sealed class EvaluarPermanenciaHandler
    : ICommandHandler<EvaluarPermanenciaCommand, EvaluacionPermanenciaDto>
{
    private readonly IReporteAcademicoRepository _reportes;
    private readonly IBecaRepository _becas;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IOutboxBeca _outbox;

    public EvaluarPermanenciaHandler(
        IReporteAcademicoRepository reportes, IBecaRepository becas,
        IUnitOfWork unidadDeTrabajo, IContextoOperacionActual contextoActual,
        IClock reloj, IOutboxBeca outbox)
    {
        _reportes = reportes ?? throw new ArgumentNullException(nameof(reportes));
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public async Task<EvaluacionPermanenciaDto> HandleAsync(
        EvaluarPermanenciaCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        EvaluacionPermanenciaDto? resultado = null;

        await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async token =>
        {
            var reporte = await _reportes.ObtenerPorIdAsync(command.ReporteAcademicoId, token)
                ?? throw new RecursoNoEncontradoException("reporte académico", command.ReporteAcademicoId);
            var beca = await _becas.ObtenerPorIdAsync(reporte.BecaId, token)
                ?? throw new RecursoNoEncontradoException("beca", reporte.BecaId);
            if (beca.PeriodoVigenteActualId != reporte.PeriodoAcademicoId)
                throw new ConflictoOperacionException(
                    "El reporte no corresponde al periodo vigente de la beca.");
            var condiciones = beca.CondicionesPermanenciaSnapshot
                ?? beca.Oferta?.CondicionesSnapshot
                ?? throw new ConfiguracionProgramaNoDisponibleException(
                    beca.ProgramaBecaId,
                    "No existe un snapshot completo de condiciones de permanencia para la beca.");
            var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj);

            try
            {
                reporte.IniciarRevision(contexto);
                resultado = EvaluacionPermanenciaDto.Desde(
                    reporte.Evaluar(condiciones, contexto));
                _reportes.PrepararNuevaEvaluacion(reporte);
            }
            catch (InvalidOperationException excepcion)
            {
                throw new ConflictoOperacionException(excepcion.Message);
            }

            foreach (var evento in reporte.EventosDominio
                .OfType<EvaluacionPermanenciaRegistradaEvento>())
                await _outbox.EncolarAsync(evento, reporte.Version, token);
            await _unidadDeTrabajo.GuardarCambiosAsync(token);
        }, cancellationToken);

        return resultado ?? throw new InvalidOperationException(
            "La evaluación no produjo un resultado.");
    }
}
