using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Renovacion.Puertos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Renovacion.Comandos.ConcluirBecaPorVencimiento;

/// <summary>Concluye sin crear una solicitud ficticia, tras liberar el cupo ocupado.</summary>
public sealed class ConcluirBecaPorVencimientoHandler
    : ICommandHandler<ConcluirBecaPorVencimientoCommand, bool>
{
    private readonly IBecaRepository _becas;
    private readonly ISolicitudRenovacionRepository _solicitudes;
    private readonly IVentanaRenovacionPrograma _ventanas;
    private readonly ILiberacionCupoPrograma _cupos;
    private readonly IUnitOfWork _unidad;
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IOutboxBeca _outbox;

    public ConcluirBecaPorVencimientoHandler(
        IBecaRepository becas, ISolicitudRenovacionRepository solicitudes,
        IVentanaRenovacionPrograma ventanas, ILiberacionCupoPrograma cupos,
        IUnitOfWork unidad, IContextoOperacionActual contextoActual, IClock reloj,
        IOutboxBeca outbox)
    {
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _solicitudes = solicitudes ?? throw new ArgumentNullException(nameof(solicitudes));
        _ventanas = ventanas ?? throw new ArgumentNullException(nameof(ventanas));
        _cupos = cupos ?? throw new ArgumentNullException(nameof(cupos));
        _unidad = unidad ?? throw new ArgumentNullException(nameof(unidad));
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public async Task<bool> HandleAsync(ConcluirBecaPorVencimientoCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.BecaId == Guid.Empty || command.PeriodoSolicitadoId == Guid.Empty)
            throw new SolicitudInvalidaException(
                "La beca y el periodo siguiente son obligatorios.");

        var referencia = await _becas.ObtenerPorIdAsync(command.BecaId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("beca", command.BecaId);
        if (referencia.Estado != EstadoBeca.Activa)
            return false;
        if (referencia.PeriodoVigenteActualId == command.PeriodoSolicitadoId)
            throw new SolicitudInvalidaException(
                "El periodo siguiente debe diferir del periodo vigente.");

        var ventana = await _ventanas.ObtenerAsync(referencia.ProgramaBecaId,
            command.PeriodoSolicitadoId, cancellationToken);
        if (ventana is null || !ventana.Activo || ventana.Inicio == default || ventana.Fin == default
            || ventana.Fin < ventana.Inicio
            || ventana.PermiteRenovacionTardiaExcepcional is null)
            throw new ConfiguracionProgramaNoDisponibleException(
                referencia.ProgramaBecaId,
                "No están confirmadas la ventana y la política de renovación tardía.");
        if (ventana.PermiteRenovacionTardiaExcepcional.Value
            || DateOnly.FromDateTime(_reloj.UtcNow.UtcDateTime) <= ventana.Fin)
            return false;

        var concluida = false;
        await _unidad.EjecutarEnTransaccionAsync(async token =>
        {
            var beca = await _becas.ObtenerBecaParaActualizarAsync(command.BecaId, token)
                ?? throw new RecursoNoEncontradoException("beca", command.BecaId);
            if (beca.Estado != EstadoBeca.Activa || beca.TieneRenovacionEnCurso
                || await _solicitudes.ExistePorBecaYPeriodoAsync(
                    beca.Id, command.PeriodoSolicitadoId, token))
                return;

            var liberado = await _cupos.LiberarPorConclusionAsync(
                beca.ProgramaBecaId, beca.Id, beca.PeriodoVigenteActualId, token);
            if (liberado is not true)
                throw new DependenciaNoDisponibleException("MS-PRG",
                    new InvalidOperationException(
                        "MS-PRG no confirmó la liberación del cupo ocupado."));

            var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj,
                "Venció la ventana de renovación sin solicitud.", exigirUsuario: false);
            beca.ConcluirPorVencimientoRenovacion(command.PeriodoSolicitadoId,
                contexto);
            foreach (var evento in beca.EventosDominio)
                await _outbox.EncolarAsync(evento, beca.Version, token);
            await _unidad.GuardarCambiosAsync(token);
            concluida = true;
        }, NivelAislamientoTransaccion.Serializable, cancellationToken);

        return concluida;
    }
}
