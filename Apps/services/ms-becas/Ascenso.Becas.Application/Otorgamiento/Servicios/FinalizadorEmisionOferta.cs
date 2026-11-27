using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Otorgamiento.Servicios;

/// <summary>Repite la misma transición remota y confirma una única oferta local.</summary>
public sealed class FinalizadorEmisionOferta
{
    private readonly IPostulacionesClient _postulaciones;
    private readonly IProgramasClient _programas;
    private readonly IIntencionEmisionOfertaStore _intenciones;
    private readonly IBecaRepository _becas;
    private readonly IUnitOfWork _unidad;
    private readonly IOutboxBeca _outbox;

    public FinalizadorEmisionOferta(IPostulacionesClient postulaciones,
        IProgramasClient programas,
        IIntencionEmisionOfertaStore intenciones, IBecaRepository becas,
        IUnitOfWork unidad, IOutboxBeca outbox)
    {
        _postulaciones = postulaciones ?? throw new ArgumentNullException(nameof(postulaciones));
        _programas = programas ?? throw new ArgumentNullException(nameof(programas));
        _intenciones = intenciones ?? throw new ArgumentNullException(nameof(intenciones));
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _unidad = unidad ?? throw new ArgumentNullException(nameof(unidad));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public async Task<OfertaEmitidaDto> ConfirmarAsync(
        IntencionEmisionOferta intencion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intencion);
        try
        {
            await _programas.ConfirmarReservaAsync(
                intencion.DatosBeca.ProgramaBecaId,
                intencion.DatosOferta.ReservaPresupuestoId,
                intencion.OfertaId, cancellationToken);
        }
        catch (Exception excepcion) when (!cancellationToken.IsCancellationRequested
            && excepcion is HttpRequestException or TimeoutException or TaskCanceledException)
        {
            throw new DependenciaNoDisponibleException("MS-PRG", excepcion);
        }

        try
        {
            await _postulaciones.OtorgarAsync(intencion.PostulacionId,
                intencion.OfertaId, cancellationToken);
        }
        catch (Exception excepcion) when (!cancellationToken.IsCancellationRequested
            && excepcion is HttpRequestException or TimeoutException or TaskCanceledException)
        {
            throw new DependenciaNoDisponibleException("MS-POS", excepcion);
        }

        await _unidad.EjecutarEnTransaccionAsync(async token =>
        {
            var existente = await _becas.ObtenerPorOfertaAsync(intencion.OfertaId, token);
            if (existente is null)
            {
                var beca = intencion.Reconstruir();
                await _becas.AgregarAsync(beca, token);
                foreach (var evento in beca.EventosDominio)
                    await _outbox.EncolarAsync(evento, beca.Version, token);
            }
            else if (existente.Id != intencion.BecaId)
            {
                throw new ConflictoOperacionException(
                    "La oferta existente no coincide con la intención de emisión.");
            }

            await _intenciones.EliminarEnTransaccionAsync(intencion.OfertaId, token);
            await _unidad.GuardarCambiosAsync(token);
        }, cancellationToken);

        return new OfertaEmitidaDto(intencion.OfertaId, intencion.BecaId,
            EstadoOferta.PendienteRespuesta.ACodigo(),
            intencion.Contexto.Fecha.AddDays(Beca.DiasVigenciaOferta));
    }
}
