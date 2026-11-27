using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Datos;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;

namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

/// <summary>
/// Registra de forma independiente una emisión lista para confirmar con MS-POS.
/// La escritura inicial debe confirmar antes de llamar al servicio remoto.
/// </summary>
public interface IIntencionEmisionOfertaStore
{
    Task GuardarAsync(IntencionEmisionOferta intencion,
        CancellationToken cancellationToken = default);

    Task<IntencionEmisionOferta?> ObtenerAsync(Guid ofertaId,
        CancellationToken cancellationToken = default);

    Task<IntencionEmisionOferta?> ObtenerPorPostulacionAsync(Guid postulacionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListarPendientesAsync(int limite,
        Guid? despuesDe = null,
        CancellationToken cancellationToken = default);

    /// <summary>Elimina la intención en la misma transacción que confirma la beca.</summary>
    Task EliminarEnTransaccionAsync(Guid ofertaId,
        CancellationToken cancellationToken = default);
}

/// <summary>Snapshot suficiente para reconstruir la misma oferta y su Outbox.</summary>
public sealed record IntencionEmisionOferta(
    Guid BecaId,
    Guid OfertaId,
    Guid PostulacionId,
    DatosCreacionBeca DatosBeca,
    DatosOfertaBeca DatosOferta,
    ContextoOperacion Contexto)
{
    public Beca Reconstruir()
    {
        var beca = Beca.CrearOtorgada(DatosBeca, Contexto, BecaId);
        beca.EmitirOferta(DatosOferta, Contexto, OfertaId);
        return beca;
    }
}
