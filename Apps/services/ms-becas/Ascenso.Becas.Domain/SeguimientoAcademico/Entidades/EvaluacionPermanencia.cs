using System.Globalization;
using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.ValueObjects;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Domain.SeguimientoAcademico.Entidades;

public sealed class EvaluacionPermanencia : EntidadAuditable
{
    private readonly List<CondicionPermanenciaEvaluada> _detalleCondiciones = [];

    private EvaluacionPermanencia()
    {
    }

    private EvaluacionPermanencia(ReporteAcademico reporte,
        CondicionesPermanencia condiciones, ContextoOperacion contexto)
        : base(Guid.CreateVersion7(), contexto.Fecha, contexto.UsuarioAutorId)
    {
        ReporteAcademicoId = reporte.Id;
        BecaId = reporte.BecaId;
        FechaEvaluacion = contexto.Fecha;
        _detalleCondiciones.Add(new(TipoCondicionPermanencia.PromedioMinimo,
            Formato(condiciones.PromedioMinimoRenovacion), Formato(reporte.PromedioPeriodo),
            reporte.PromedioPeriodo >= condiciones.PromedioMinimoRenovacion));
        _detalleCondiciones.Add(new(TipoCondicionPermanencia.MateriasMinimasAprobadas,
            Formato(condiciones.MateriasMinimasAprobadas), Formato(reporte.MateriasAprobadas),
            reporte.MateriasAprobadas >= condiciones.MateriasMinimasAprobadas));
        _detalleCondiciones.Add(new(TipoCondicionPermanencia.MaximoMateriasReprobadas,
            Formato(condiciones.MaximoMateriasReprobadas), Formato(reporte.MateriasReprobadas),
            reporte.MateriasReprobadas <= condiciones.MaximoMateriasReprobadas));
        _detalleCondiciones.Add(new(TipoCondicionPermanencia.HorasServicioPorPeriodo,
            Formato(condiciones.HorasServicioPorPeriodo),
            Formato(reporte.HorasServicioCumplidas ?? 0),
            (reporte.HorasServicioCumplidas ?? 0) >= condiciones.HorasServicioPorPeriodo));
        CumpleCondiciones = _detalleCondiciones.All(detalle => detalle.Cumple);
        ConsecuenciaRecomendada = CumpleCondiciones
            ? null : condiciones.ConsecuenciaIncumplimiento;
        EstadoConfirmacion = EstadoConsecuencia.PendienteConfirmacion;
    }

    public Guid ReporteAcademicoId { get; private set; }
    public Guid BecaId { get; private set; }
    public DateTimeOffset FechaEvaluacion { get; private set; }
    public bool CumpleCondiciones { get; private set; }
    public IReadOnlyList<CondicionPermanenciaEvaluada> DetalleCondiciones
        => _detalleCondiciones.AsReadOnly();
    public ConsecuenciaPermanencia? ConsecuenciaRecomendada { get; private set; }
    public ConsecuenciaPermanencia? ConsecuenciaAplicada { get; private set; }
    public EstadoConsecuencia EstadoConfirmacion { get; private set; }
    public string? MotivoDecision { get; private set; }
    public string? SiguientePasoBecario { get; private set; }
    public Guid? EvaluadoPorCoordinadorId { get; private set; }

    internal static EvaluacionPermanencia Crear(ReporteAcademico reporte,
        CondicionesPermanencia condiciones, ContextoOperacion contexto)
        => new(reporte, condiciones, contexto);

    internal void Decidir(bool confirmar, string motivoDecision,
        string siguientePasoBecario, ContextoOperacion contexto)
    {
        if (EstadoConfirmacion != EstadoConsecuencia.PendienteConfirmacion)
            throw new InvalidOperationException("La evaluación ya tiene una decisión.");
        if (string.IsNullOrWhiteSpace(motivoDecision)
            || string.IsNullOrWhiteSpace(siguientePasoBecario))
            throw new ArgumentException("La decisión y el siguiente paso son obligatorios.");
        EstadoConfirmacion = confirmar ? EstadoConsecuencia.Confirmada : EstadoConsecuencia.Rechazada;
        MotivoDecision = motivoDecision.Trim();
        SiguientePasoBecario = siguientePasoBecario.Trim();
        EvaluadoPorCoordinadorId = contexto.UsuarioAutorId;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    internal void MarcarConsecuenciaAplicada(ContextoOperacion contexto)
    {
        if (EstadoConfirmacion != EstadoConsecuencia.Confirmada
            || ConsecuenciaRecomendada is null || ConsecuenciaAplicada is not null)
            throw new InvalidOperationException("No existe una consecuencia aprobada pendiente de aplicar.");
        ConsecuenciaAplicada = ConsecuenciaRecomendada;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    private static string Formato(decimal valor)
        => valor.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Formato(int valor)
        => valor.ToString(CultureInfo.InvariantCulture);
}
