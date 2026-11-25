using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;

namespace Ascenso.Becas.Domain.Desembolsos.Entidades;

public sealed class IncidenciaDesembolso : EntidadAuditable
{
    private IncidenciaDesembolso()
    {
    }

    private IncidenciaDesembolso(Guid id, ContextoOperacion contexto)
        : base(id, contexto.Fecha, contexto.UsuarioAutorId)
    {
    }

    public Guid DesembolsoId { get; private set; }

    public TipoIncidenciaDesembolso Tipo { get; private set; }

    public string Motivo { get; private set; } = string.Empty;

    public string ObservacionBanco { get; private set; } = string.Empty;

    public EstadoIncidencia Estado { get; private set; }

    public DateTimeOffset FechaRegistro { get; private set; }

    public Guid RegistradoPorUsuarioId { get; private set; }

    public Guid? ResueltoPorUsuarioId { get; private set; }

    public DateTimeOffset? FechaResolucion { get; private set; }

    internal static IncidenciaDesembolso Registrar(
        Guid desembolsoId,
        TipoIncidenciaDesembolso tipo,
        string motivo,
        string observacionBanco,
        ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        if (desembolsoId == Guid.Empty || !Enum.IsDefined(tipo)
            || string.IsNullOrWhiteSpace(motivo) || string.IsNullOrWhiteSpace(observacionBanco))
        {
            throw new ArgumentException("La incidencia requiere cuota, tipo, motivo y observación bancaria.");
        }

        return new IncidenciaDesembolso(UuidV7.Nuevo(), contexto)
        {
            DesembolsoId = desembolsoId,
            Tipo = tipo,
            Motivo = motivo.Trim(),
            ObservacionBanco = observacionBanco.Trim(),
            Estado = EstadoIncidencia.Abierta,
            FechaRegistro = contexto.Fecha,
            RegistradoPorUsuarioId = contexto.UsuarioAutorId
        };
    }

    internal void Resolver(ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        if (Estado != EstadoIncidencia.Abierta)
        {
            throw new InvalidOperationException("La incidencia ya está resuelta.");
        }

        Estado = EstadoIncidencia.Resuelta;
        ResueltoPorUsuarioId = contexto.UsuarioAutorId;
        FechaResolucion = contexto.Fecha;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }
}
