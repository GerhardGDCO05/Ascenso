using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Desembolsos.Eventos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Desembolsos.GenerarCalendarioInicial;

/// <summary>Repara una beca activa sin calendario de forma idempotente.</summary>
public sealed class GenerarCalendarioInicialHandler
    : ICommandHandler<GenerarCalendarioInicialCommand, bool>
{
    private readonly IBecaRepository _becas;
    private readonly ICalendarioDesembolsoRepository _calendarios;
    private readonly IPlanDesembolsoPrograma _planes;
    private readonly IUnitOfWork _unidad;
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IOutboxBeca _outbox;

    public GenerarCalendarioInicialHandler(IBecaRepository becas,
        ICalendarioDesembolsoRepository calendarios,
        IPlanDesembolsoPrograma planes, IUnitOfWork unidad,
        IContextoOperacionActual contextoActual, IClock reloj, IOutboxBeca outbox)
    {
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _calendarios = calendarios ?? throw new ArgumentNullException(nameof(calendarios));
        _planes = planes ?? throw new ArgumentNullException(nameof(planes));
        _unidad = unidad ?? throw new ArgumentNullException(nameof(unidad));
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public async Task<bool> HandleAsync(GenerarCalendarioInicialCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.BecaId == Guid.Empty)
            throw new SolicitudInvalidaException("La beca es obligatoria.");

        var creado = false;
        await _unidad.EjecutarEnTransaccionAsync(async token =>
        {
            var beca = await _becas.ObtenerBecaParaActualizarAsync(command.BecaId, token)
                ?? throw new RecursoNoEncontradoException("beca", command.BecaId);
            if (beca.Estado != EstadoBeca.Activa
                || await _calendarios.ObtenerPorBecaYPeriodoAsync(
                    beca.Id, beca.PeriodoVigenteActualId, token) is not null)
                return;

            var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj,
                exigirUsuario: false);
            var calendario = await PlanificadorCalendarioInicial.CrearAsync(
                beca, _planes, contexto, token);
            await _calendarios.AgregarAsync(calendario, token);
            foreach (var cuota in calendario.Desembolsos)
                await _outbox.EncolarAsync(new DesembolsoProgramadoEvento(
                    beca.Id, calendario.Id, cuota.Id,
                    contexto.CorrelacionId, contexto.Fecha),
                    calendario.Version, token);
            await _unidad.GuardarCambiosAsync(token);
            creado = true;
        }, NivelAislamientoTransaccion.Serializable, cancellationToken);

        return creado;
    }
}
