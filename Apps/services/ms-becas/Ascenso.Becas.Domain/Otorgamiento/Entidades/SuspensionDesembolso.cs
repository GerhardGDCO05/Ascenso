using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;

namespace Ascenso.Becas.Domain.Otorgamiento.Entidades;

/// <summary>Registro de la suspensión de desembolsos de una beca.</summary>
public sealed class SuspensionDesembolso : EntidadAuditable
{
    private SuspensionDesembolso()
    {
    }

    private SuspensionDesembolso(Guid becaId, TipoSuspensionBeca tipo,
        string motivo, string requisitos, DateOnly fechaInicio,
        DateOnly? fechaRevision, bool esIndefinida, ContextoOperacion contexto)
        : base(Guid.CreateVersion7(), contexto.Fecha, contexto.UsuarioAutorId)
    {
        BecaId = becaId;
        TipoSuspension = tipo;
        MotivoSuspension = motivo;
        RequisitosParaReanudar = requisitos;
        FechaInicio = fechaInicio;
        FechaRevisionProgramada = fechaRevision;
        EsIndefinida = esIndefinida;
        Estado = EstadoSuspension.Vigente;
        RegistradaPorCoordinadorId = contexto.UsuarioAutorId;
    }

    public Guid BecaId { get; private set; }
    public TipoSuspensionBeca TipoSuspension { get; private set; }
    public string MotivoSuspension { get; private set; } = string.Empty;
    public string RequisitosParaReanudar { get; private set; } = string.Empty;
    public DateOnly FechaInicio { get; private set; }
    public DateOnly? FechaRevisionProgramada { get; private set; }
    public bool EsIndefinida { get; private set; }
    public EstadoSuspension Estado { get; private set; }
    public Guid RegistradaPorCoordinadorId { get; private set; }
    public Guid? LevantadaPorCoordinadorId { get; private set; }
    public DateTimeOffset? FechaResolucion { get; private set; }

    public static SuspensionDesembolso Crear(Guid becaId, TipoSuspensionBeca tipo,
        string motivo, string requisitos, DateOnly fechaInicio,
        DateOnly? fechaRevision, bool esIndefinida, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        if (becaId == Guid.Empty || !Enum.IsDefined(tipo)
            || string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 10
            || string.IsNullOrWhiteSpace(requisitos) || fechaInicio == default
            || esIndefinida == fechaRevision.HasValue
            || (fechaRevision.HasValue && fechaRevision.Value < fechaInicio))
            throw new ArgumentException("Los datos de la suspensión son inválidos.");

        return new SuspensionDesembolso(becaId, tipo, motivo.Trim(), requisitos.Trim(),
            fechaInicio, fechaRevision, esIndefinida, contexto);
    }

    public void Levantar(ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        if (Estado != EstadoSuspension.Vigente)
            throw new InvalidOperationException("La suspensión ya fue levantada.");
        Estado = EstadoSuspension.Levantada;
        LevantadaPorCoordinadorId = contexto.UsuarioAutorId;
        FechaResolucion = contexto.Fecha;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }
}
