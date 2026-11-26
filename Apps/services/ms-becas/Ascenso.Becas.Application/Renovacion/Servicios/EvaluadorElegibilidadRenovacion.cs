using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Renovacion.Puertos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.Renovacion.Datos;

namespace Ascenso.Becas.Application.Renovacion.Servicios;

/// <summary>Valida los requisitos previos sin crear una solicitud ni una beca sucesora.</summary>
public sealed class EvaluadorElegibilidadRenovacion
{
    private readonly IBecaRepository _becas;
    private readonly IReporteAcademicoRepository _reportes;
    private readonly IVentanaRenovacionPrograma _ventanas;
    private readonly IPresupuestoRenovacionPrograma _presupuestos;
    private readonly IClock _reloj;

    public EvaluadorElegibilidadRenovacion(
        IBecaRepository becas, IReporteAcademicoRepository reportes,
        IVentanaRenovacionPrograma ventanas,
        IPresupuestoRenovacionPrograma presupuestos, IClock reloj)
    {
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _reportes = reportes ?? throw new ArgumentNullException(nameof(reportes));
        _ventanas = ventanas ?? throw new ArgumentNullException(nameof(ventanas));
        _presupuestos = presupuestos ?? throw new ArgumentNullException(nameof(presupuestos));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
    }

    public async Task<DatosSolicitudRenovacion> EvaluarAsync(
        Guid becaId, Guid periodoSolicitadoId, decimal montoSolicitado,
        CancellationToken cancellationToken = default)
    {
        if (becaId == Guid.Empty || periodoSolicitadoId == Guid.Empty
            || montoSolicitado <= 0m)
            throw new SolicitudInvalidaException(
                "La beca, el periodo y el monto de renovación son obligatorios.");

        var beca = await _becas.ObtenerPorIdAsync(becaId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("beca", becaId);
        if (beca.Estado != EstadoBeca.Activa
            || beca.PeriodoVigenteActualId == periodoSolicitadoId
            || beca.PeriodosCursados >= beca.DuracionTotalPeriodos)
            throw new ConflictoOperacionException(
                "La beca no admite una renovación para el periodo solicitado.");

        var ventana = await _ventanas.ObtenerAsync(
            beca.ProgramaBecaId, periodoSolicitadoId, cancellationToken);
        if (ventana is null || !ventana.Activo || ventana.Inicio == default
            || ventana.Fin == default || ventana.Fin < ventana.Inicio)
            throw new ConfiguracionProgramaNoDisponibleException(beca.ProgramaBecaId,
                "Falta una ventana de renovación oficial y activa para este periodo.");

        var fechaActual = DateOnly.FromDateTime(_reloj.UtcNow.UtcDateTime);
        if (fechaActual < ventana.Inicio || fechaActual > ventana.Fin)
            throw new ConflictoOperacionException(
                "La solicitud está fuera de la ventana de renovación.");

        var reporte = await _reportes.ObtenerPorBecaYPeriodoParaConsultaAsync(
            becaId, beca.PeriodoVigenteActualId, cancellationToken);
        if (reporte?.Estado != EstadoReporteAcademico.Aprobado
            || reporte.Evaluacion?.EstadoConfirmacion != EstadoConsecuencia.Confirmada
            || !reporte.Evaluacion.CumpleCondiciones)
            throw new ConflictoOperacionException(
                "La beca no tiene una evaluación académica aprobada y cumplida en el periodo de origen.");

        var disponible = await _presupuestos.TieneDisponibilidadAsync(
            beca.ProgramaBecaId, periodoSolicitadoId, montoSolicitado,
            cancellationToken);
        if (disponible is null)
            throw new ConfiguracionProgramaNoDisponibleException(beca.ProgramaBecaId,
                "MS-PRG no confirmó la disponibilidad presupuestaria de la renovación.");
        if (!disponible.Value)
            throw new ConflictoOperacionException(
                "No hay presupuesto disponible para la renovación.");

        return new DatosSolicitudRenovacion(becaId,
            beca.PeriodoVigenteActualId, periodoSolicitadoId,
            ventana.Fin, true, true);
    }
}
