using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Desembolsos;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.AceptarOfertaBeca;

/// <summary>Prepara una intención duradera antes de activar la reserva externa.</summary>
public sealed class AceptarOfertaBecaHandler
    : ICommandHandler<AceptarOfertaBecaCommand, OfertaRespondidaDto>
{
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IBecaRepository _becas;
    private readonly IUnitOfWork _unidad;
    private readonly ICalendarioDesembolsoRepository _calendarios;
    private readonly IPlanDesembolsoPrograma _planes;
    private readonly IIntencionAceptacionOfertaStore _intenciones;
    private readonly FinalizadorAceptacionOferta _finalizador;

    public AceptarOfertaBecaHandler(IContextoOperacionActual contextoActual,
        IClock reloj, IBecaRepository becas, IUnitOfWork unidad,
        ICalendarioDesembolsoRepository calendarios, IPlanDesembolsoPrograma planes,
        IIntencionAceptacionOfertaStore intenciones,
        FinalizadorAceptacionOferta finalizador)
    {
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _unidad = unidad ?? throw new ArgumentNullException(nameof(unidad));
        _calendarios = calendarios ?? throw new ArgumentNullException(nameof(calendarios));
        _planes = planes ?? throw new ArgumentNullException(nameof(planes));
        _intenciones = intenciones ?? throw new ArgumentNullException(nameof(intenciones));
        _finalizador = finalizador ?? throw new ArgumentNullException(nameof(finalizador));
    }

    public async Task<OfertaRespondidaDto> HandleAsync(
        AceptarOfertaBecaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var intencion = await _intenciones.ObtenerAsync(command.OfertaId, cancellationToken);
        if (intencion is null)
        {
            var referencia = await _becas.ObtenerPorOfertaAsync(command.OfertaId, cancellationToken)
                ?? throw new RecursoNoEncontradoException("oferta", command.OfertaId);
            await _unidad.EjecutarEnTransaccionAsync(async token =>
            {
                await _becas.AdquirirBloqueoEstudianteAsync(
                    referencia.PostulanteEstudianteId, token);
                var beca = await _becas.ObtenerBecaParaActualizarAsync(referencia.Id, token)
                    ?? throw new RecursoNoEncontradoException("beca", referencia.Id);
                var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj);
                try
                {
                    beca.ValidarAceptacionOferta(command.OfertaId, contexto);
                }
                catch (InvalidOperationException excepcion)
                {
                    throw new ConflictoOperacionException(excepcion.Message);
                }
                var activas = await _becas.ObtenerActivasPorEstudianteAsync(
                    beca.PostulanteEstudianteId, token);
                PoliticaNoDobleBeneficio.Validar(beca, activas);
                if (await _calendarios.ObtenerPorBecaYPeriodoAsync(
                    beca.Id, beca.PeriodoVigenteActualId, token) is not null)
                    throw new ConflictoOperacionException(
                        "La beca ya tiene calendario de desembolsos para el periodo.");
                var plan = await _planes.ObtenerAsync(beca.ProgramaBecaId,
                    beca.PeriodoVigenteActualId, token);
                PlanificadorCalendarioInicial.CrearDesdePlan(beca, plan, contexto);
                intencion = new IntencionAceptacionOferta(command.OfertaId,
                    beca.Id, beca.ProgramaBecaId,
                    beca.Oferta!.ReservaPresupuestoId, plan!, contexto);
                await _intenciones.GuardarAsync(intencion, token);
            }, NivelAislamientoTransaccion.Serializable, cancellationToken);
        }
        return await _finalizador.ConfirmarAsync(intencion
            ?? throw new InvalidOperationException("No se preparó la aceptación."),
            cancellationToken);
    }
}
