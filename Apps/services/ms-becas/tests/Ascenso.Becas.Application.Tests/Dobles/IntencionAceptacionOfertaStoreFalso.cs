using Ascenso.Becas.Application.Otorgamiento.Puertos;

namespace Ascenso.Becas.Application.Tests.Dobles;

public sealed class IntencionAceptacionOfertaStoreFalso : IIntencionAceptacionOfertaStore
{
    private readonly HashSet<Guid> _eliminaciones = [];
    public List<IntencionAceptacionOferta> Pendientes { get; } = [];

    public void Confirmar()
    {
        Pendientes.RemoveAll(item => _eliminaciones.Contains(item.OfertaId));
        _eliminaciones.Clear();
    }

    public void Revertir() => _eliminaciones.Clear();

    public Task GuardarAsync(IntencionAceptacionOferta intencion,
        CancellationToken cancellationToken = default)
    {
        Pendientes.Add(intencion);
        return Task.CompletedTask;
    }

    public Task<IntencionAceptacionOferta?> ObtenerAsync(Guid ofertaId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Pendientes.SingleOrDefault(item => item.OfertaId == ofertaId));

    public Task<IReadOnlyList<Guid>> ListarPendientesAsync(int limite,
        Guid? despuesDe = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Guid>>(Pendientes
            .Select(item => item.OfertaId)
            .Where(id => despuesDe is null || id.CompareTo(despuesDe.Value) > 0)
            .Order().Take(limite).ToArray());

    public Task EliminarEnTransaccionAsync(Guid ofertaId,
        CancellationToken cancellationToken = default)
    {
        _eliminaciones.Add(ofertaId);
        return Task.CompletedTask;
    }
}
