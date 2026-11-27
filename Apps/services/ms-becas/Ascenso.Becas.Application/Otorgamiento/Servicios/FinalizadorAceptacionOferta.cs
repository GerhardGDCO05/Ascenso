using Ascenso.Becas.Application.Desembolsos;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Desembolsos.Eventos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace Ascenso.Becas.Application.Otorgamiento.Servicios;

/// <summary>Activa la reserva y confirma una única aceptación local.</summary>
public sealed class FinalizadorAceptacionOferta
{
    private readonly IBecaRepository _becas;
    private readonly ICalendarioDesembolsoRepository _calendarios;
    private readonly IIntencionAceptacionOfertaStore _intenciones;
    private readonly IProgramasClient _programas;
    private readonly IUnitOfWork _unidad;
    private readonly IOutboxBeca _outbox;
    private readonly IProgramadorExpiracionOfertas _programador;
    private readonly IMetricasOtorgamiento _metricas;
    private readonly ILogger<FinalizadorAceptacionOferta> _logger;

    public FinalizadorAceptacionOferta(IBecaRepository becas,
        ICalendarioDesembolsoRepository calendarios,
        IIntencionAceptacionOfertaStore intenciones,
        IProgramasClient programas, IUnitOfWork unidad,
        IOutboxBeca outbox, IProgramadorExpiracionOfertas programador,
        IMetricasOtorgamiento metricas,
        ILogger<FinalizadorAceptacionOferta> logger)
    {
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _calendarios = calendarios ?? throw new ArgumentNullException(nameof(calendarios));
        _intenciones = intenciones ?? throw new ArgumentNullException(nameof(intenciones));
        _programas = programas ?? throw new ArgumentNullException(nameof(programas));
        _unidad = unidad ?? throw new ArgumentNullException(nameof(unidad));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _programador = programador ?? throw new ArgumentNullException(nameof(programador));
        _metricas = metricas ?? throw new ArgumentNullException(nameof(metricas));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OfertaRespondidaDto> ConfirmarAsync(
        IntencionAceptacionOferta intencion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intencion);
        OfertaRespondidaDto? resultado = null;
        string? referenciaJob = null;
        var activada = false;
        await _unidad.EjecutarEnTransaccionAsync(async token =>
        {
            var referencia = await _becas.ObtenerPorOfertaAsync(
                intencion.OfertaId, token)
                ?? throw new RecursoNoEncontradoException("oferta", intencion.OfertaId);
            if (referencia.Id != intencion.BecaId)
                throw new ConflictoOperacionException(
                    "La beca no coincide con la intención de aceptación.");
            await _becas.AdquirirBloqueoEstudianteAsync(
                referencia.PostulanteEstudianteId, token);
            var beca = await _becas.ObtenerBecaParaActualizarAsync(
                intencion.BecaId, token)
                ?? throw new RecursoNoEncontradoException("beca", intencion.BecaId);
            if (beca.Oferta?.Id != intencion.OfertaId
                || beca.ProgramaBecaId != intencion.ProgramaBecaId
                || beca.Oferta.ReservaPresupuestoId
                    != intencion.ReservaPresupuestoId)
                throw new ConflictoOperacionException(
                    "La oferta cambió desde que se preparó su aceptación.");
            referenciaJob = beca.Oferta.ReferenciaJobExpiracionHangfireId;

            if (beca.Estado == EstadoBeca.Activa
                && beca.Oferta.Estado == EstadoOferta.Aceptada)
            {
                if (await _calendarios.ObtenerPorBecaYPeriodoAsync(beca.Id,
                    beca.PeriodoVigenteActualId, token) is null)
                    throw new ConflictoOperacionException(
                        "La beca activa no tiene calendario confirmado.");
                await _intenciones.EliminarEnTransaccionAsync(
                    intencion.OfertaId, token);
                await _unidad.GuardarCambiosAsync(token);
                resultado = CrearRespuesta(beca);
                return;
            }

            if (beca.Estado != EstadoBeca.Otorgada
                || beca.Oferta.Estado != EstadoOferta.PendienteRespuesta)
                throw new ConflictoOperacionException(
                    "La oferta ya no admite la aceptación pendiente.");
            var activas = await _becas.ObtenerActivasPorEstudianteAsync(
                beca.PostulanteEstudianteId, token);
            PoliticaNoDobleBeneficio.Validar(beca, activas);
            if (await _calendarios.ObtenerPorBecaYPeriodoAsync(beca.Id,
                beca.PeriodoVigenteActualId, token) is not null)
                throw new ConflictoOperacionException(
                    "La beca ya tiene calendario de desembolsos para el periodo.");
            var calendario = PlanificadorCalendarioInicial.CrearDesdePlan(
                beca, intencion.Plan, intencion.Contexto);

            try
            {
                beca.ValidarAceptacionOferta(intencion.OfertaId, intencion.Contexto);
            }
            catch (InvalidOperationException excepcion)
            {
                throw new ConflictoOperacionException(excepcion.Message);
            }

            try
            {
                await _programas.ActivarReservaAsync(
                    beca.ProgramaBecaId, beca.Oferta.ReservaPresupuestoId,
                    intencion.OfertaId, token);
            }
            catch (Exception excepcion) when (!token.IsCancellationRequested
                && excepcion is HttpRequestException or TimeoutException or TaskCanceledException)
            {
                throw new DependenciaNoDisponibleException("MS-PRG", excepcion);
            }

            try
            {
                beca.AceptarOferta(intencion.OfertaId, intencion.Contexto);
            }
            catch (InvalidOperationException excepcion)
            {
                throw new ConflictoOperacionException(excepcion.Message);
            }
            await _calendarios.AgregarAsync(calendario, token);
            foreach (var evento in beca.EventosDominio)
                await _outbox.EncolarAsync(evento, beca.Version, token);
            foreach (var cuota in calendario.Desembolsos)
                await _outbox.EncolarAsync(new DesembolsoProgramadoEvento(
                    beca.Id, calendario.Id, cuota.Id,
                    intencion.Contexto.CorrelacionId, intencion.Contexto.Fecha),
                    calendario.Version, token);
            await _intenciones.EliminarEnTransaccionAsync(
                intencion.OfertaId, token);
            await _unidad.GuardarCambiosAsync(token);
            resultado = CrearRespuesta(beca);
            activada = true;
        }, NivelAislamientoTransaccion.Serializable, cancellationToken);

        if (resultado is null)
            throw new InvalidOperationException(
                "La aceptación no produjo un resultado.");
        await CanceladorExpiracionOferta.CancelarAsync(_programador, _logger,
            intencion.OfertaId, referenciaJob);
        if (activada)
            _metricas.OfertaAceptada();
        return resultado;
    }

    private static OfertaRespondidaDto CrearRespuesta(Beca beca)
        => new(beca.Oferta!.Id, beca.Id,
            beca.Oferta.Estado.ACodigo(), beca.Estado.ACodigo());
}
