using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.SeguimientoAcademico.Dtos;
using Ascenso.Becas.Application.SeguimientoAcademico.Puertos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.Datos;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Comandos.RegistrarReporteAcademico;

public sealed class RegistrarReporteAcademicoHandler
    : ICommandHandler<RegistrarReporteAcademicoCommand, ReporteAcademicoDto>
{
    private readonly IBecaRepository _becas;
    private readonly IReporteAcademicoRepository _reportes;
    private readonly ICalendarioAcademicoPrograma _calendario;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IOutboxBeca _outbox;
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;

    public RegistrarReporteAcademicoHandler(
        IBecaRepository becas, IReporteAcademicoRepository reportes,
        ICalendarioAcademicoPrograma calendario, IUnitOfWork unidadDeTrabajo,
        IOutboxBeca outbox, IContextoOperacionActual contextoActual, IClock reloj)
    {
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _reportes = reportes ?? throw new ArgumentNullException(nameof(reportes));
        _calendario = calendario ?? throw new ArgumentNullException(nameof(calendario));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
    }

    public async Task<ReporteAcademicoDto> HandleAsync(
        RegistrarReporteAcademicoCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ReporteAcademicoDto? resultado = null;
        await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async token =>
        {
            var beca = await _becas.ObtenerPorIdAsync(command.BecaId, token)
                ?? throw new RecursoNoEncontradoException("beca", command.BecaId);
            if (beca.PeriodoVigenteActualId != command.PeriodoAcademicoId
                || beca.Estado is not (EstadoBeca.Activa or EstadoBeca.Suspendida))
                throw new ConflictoOperacionException(
                    "La beca no está vigente en el periodo del reporte.");

            var fechaLimite = await _calendario.ObtenerFechaLimiteReporteAsync(
                beca.ProgramaBecaId, command.PeriodoAcademicoId, token);
            if (fechaLimite is null || fechaLimite == default)
                throw new ConfiguracionProgramaNoDisponibleException(beca.ProgramaBecaId,
                    "Falta la fecha límite oficial del reporte académico para este periodo.");

            var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj);
            ReporteAcademico reporte;
            try
            {
                reporte = ReporteAcademico.Registrar(new DatosReporteAcademico(
                    command.BecaId, command.PeriodoAcademicoId,
                    command.PromedioPeriodo, command.MateriasAprobadas,
                    command.MateriasReprobadas, command.MateriasCursando,
                    command.CreditosAprobados, command.EstadoMatricula,
                    command.UrlComprobanteNotasFirebase,
                    command.HashComprobanteSha256,
                    command.TamanioComprobanteBytes,
                    command.TipoMimeComprobante, command.FuenteEntrega,
                    command.UniversidadAliadaId, fechaLimite.Value,
                    command.HorasServicioCumplidas,
                    command.UrlComprobanteServicio,
                    command.HashComprobanteServicioSha256), contexto);
            }
            catch (ArgumentException excepcion)
            {
                throw new SolicitudInvalidaException(excepcion.Message);
            }

            reporte.PrepararAlertaTardia(beca.ProgramaBecaId, contexto);
            await _reportes.AgregarAsync(reporte, token);
            foreach (var evento in reporte.EventosDominio)
                await _outbox.EncolarAsync(evento, reporte.Version, token);
            await _unidadDeTrabajo.GuardarCambiosAsync(token);
            resultado = ReporteAcademicoDto.Desde(reporte);
        }, cancellationToken);

        return resultado ?? throw new InvalidOperationException(
            "El registro no produjo un reporte académico.");
    }
}
