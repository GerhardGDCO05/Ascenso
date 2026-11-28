using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.ListaEspera.Agregados;

/// <summary>
/// Raíz de agregado independiente que representa la posición ordenada y el ciclo de vida
/// de una postulación elegible fuera de cupo dentro de la lista de espera de una convocatoria.
/// </summary>
public sealed class EntradaListaEspera : AggregateRoot
{
    public const int LongitudMaximaMotivoPromocion = 500;

    // Constructor requerido por los adaptadores de persistencia.
    private EntradaListaEspera()
    {
    }

    private EntradaListaEspera(
        Guid id,
        Guid convocatoriaId,
        Guid programaBecaId,
        Guid postulacionId,
        int posicionOrden,
        decimal montoEstimadoBecaSnapshot,
        DateTimeOffset fechaIngreso,
        Guid usuarioActorId)
        : base(id, fechaIngreso, usuarioActorId)
    {
        ConvocatoriaId = convocatoriaId;
        ProgramaBecaId = programaBecaId;
        PostulacionId = postulacionId;
        PosicionOrden = posicionOrden;
        MontoEstimadoBecaSnapshot = montoEstimadoBecaSnapshot;
        Estado = EstadoListaEspera.EnEspera;
        FechaIngreso = FechaCreacion;
    }

    public Guid ConvocatoriaId { get; private set; }

    public Guid ProgramaBecaId { get; private set; }

    public Guid PostulacionId { get; private set; }

    public int PosicionOrden { get; private set; }

    public decimal MontoEstimadoBecaSnapshot { get; private set; }

    public EstadoListaEspera Estado { get; private set; }

    public DateTimeOffset FechaIngreso { get; private set; }

    public DateTimeOffset? FechaElegibilidad { get; private set; }

    public DateTimeOffset? FechaPromocion { get; private set; }

    public Guid? PromovidoPorUsuarioId { get; private set; }

    public string? MotivoPromocion { get; private set; }

    public bool EstaPromovida => Estado == EstadoListaEspera.Promovida;

    public bool EstaPendiente => Estado != EstadoListaEspera.Promovida;

    public static EntradaListaEspera Crear(
        Guid convocatoriaId,
        Guid programaBecaId,
        Guid postulacionId,
        int posicionOrden,
        decimal montoEstimadoBecaSnapshot,
        DateTimeOffset fechaIngreso,
        Guid usuarioActorId)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ListaEsperaInvalidaException("El identificador de la convocatoria es obligatorio.");
        }

        if (programaBecaId == Guid.Empty)
        {
            throw new ListaEsperaInvalidaException("El identificador del programa de beca es obligatorio.");
        }

        if (postulacionId == Guid.Empty)
        {
            throw new ListaEsperaInvalidaException("El identificador de la postulación es obligatorio.");
        }

        if (posicionOrden < 1)
        {
            throw new ListaEsperaInvalidaException("La posición de orden en la lista de espera debe ser mayor o igual a 1.");
        }

        if (montoEstimadoBecaSnapshot <= 0.00m
            || decimal.Round(montoEstimadoBecaSnapshot, 2) != montoEstimadoBecaSnapshot)
        {
            throw new ListaEsperaInvalidaException(
                "El monto estimado de beca debe ser mayor a 0.00 con máximo dos decimales.");
        }

        ValidarArgumentosTemporalesYUsuario(fechaIngreso, usuarioActorId);

        return new EntradaListaEspera(
            UuidV7.Nuevo(),
            convocatoriaId,
            programaBecaId,
            postulacionId,
            posicionOrden,
            montoEstimadoBecaSnapshot,
            fechaIngreso,
            usuarioActorId);
    }

    public void MarcarElegiblePorVacante(DateTimeOffset fechaElegibilidad, Guid usuarioActorId)
    {
        ValidarArgumentosTemporalesYUsuario(fechaElegibilidad, usuarioActorId);

        if (Estado == EstadoListaEspera.Promovida)
        {
            throw new EntradaListaEsperaYaPromovidaException();
        }

        if (Estado == EstadoListaEspera.ElegiblePorVacante)
        {
            throw new EntradaListaEsperaYaElegibleException();
        }

        var fechaUtc = fechaElegibilidad.ToUniversalTime();
        Estado = EstadoListaEspera.ElegiblePorVacante;
        FechaElegibilidad = fechaUtc;
        ActualizarAuditoria(usuarioActorId, fechaUtc);
    }

    public void Promover(
        DateTimeOffset fechaPromocion,
        Guid promovidoPorUsuarioId,
        string? motivoPromocion = null)
    {
        ValidarArgumentosTemporalesYUsuario(fechaPromocion, promovidoPorUsuarioId);

        if (Estado == EstadoListaEspera.Promovida)
        {
            throw new EntradaListaEsperaYaPromovidaException();
        }

        string? motivoNormalizado = null;
        if (!string.IsNullOrWhiteSpace(motivoPromocion))
        {
            motivoNormalizado = motivoPromocion.Trim();
            if (motivoNormalizado.Length > LongitudMaximaMotivoPromocion)
            {
                throw new MotivoPromocionInvalidoException();
            }
        }

        var fechaUtc = fechaPromocion.ToUniversalTime();
        Estado = EstadoListaEspera.Promovida;
        FechaPromocion = fechaUtc;
        PromovidoPorUsuarioId = promovidoPorUsuarioId;
        MotivoPromocion = motivoNormalizado;
        ActualizarAuditoria(promovidoPorUsuarioId, fechaUtc);
    }

    private static void ValidarArgumentosTemporalesYUsuario(
        DateTimeOffset fechaOperacion,
        Guid usuarioActorId)
    {
        if (fechaOperacion == default)
        {
            throw new ArgumentException("La fecha de operación es obligatoria.", nameof(fechaOperacion));
        }

        if (usuarioActorId == Guid.Empty)
        {
            throw new ArgumentException("El usuario actor es obligatorio.", nameof(usuarioActorId));
        }
    }
}
