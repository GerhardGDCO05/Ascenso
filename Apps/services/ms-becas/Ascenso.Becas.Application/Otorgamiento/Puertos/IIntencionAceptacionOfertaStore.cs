using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;

namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

/// <summary>Guarda la aceptación antes de activar la reserva externa.</summary>
public interface IIntencionAceptacionOfertaStore
{
    Task GuardarAsync(IntencionAceptacionOferta intencion,
        CancellationToken cancellationToken = default);

    Task<IntencionAceptacionOferta?> ObtenerAsync(Guid ofertaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListarPendientesAsync(int limite,
        Guid? despuesDe = null, CancellationToken cancellationToken = default);

    Task EliminarEnTransaccionAsync(Guid ofertaId,
        CancellationToken cancellationToken = default);
}

/// <summary>Datos oficiales y contexto que se reutilizan en todos los reintentos.</summary>
public sealed record IntencionAceptacionOferta(
    Guid OfertaId,
    Guid BecaId,
    Guid ProgramaBecaId,
    Guid ReservaPresupuestoId,
    PlanDesembolsoPrograma Plan,
    ContextoOperacion Contexto);
