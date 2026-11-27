using Ascenso.Becas.Application.Otorgamiento.Puertos;

namespace Ascenso.Becas.Application.Tests.Dobles;

public sealed class IntencionEmisionOfertaStoreFalso : IIntencionEmisionOfertaStore
{
    private readonly HashSet<Guid> _eliminacionesPendientes = [];

    public List<IntencionEmisionOferta> Pendientes { get; } = [];

    public Exception? ErrorGuardarAntes { get; set; }

    public Exception? ErrorGuardarDespues { get; set; }

    public void Confirmar()
    {
        Pendientes.RemoveAll(valor => _eliminacionesPendientes.Contains(valor.OfertaId));
        _eliminacionesPendientes.Clear();
    }

    public void Revertir() => _eliminacionesPendientes.Clear();

    public Task GuardarAsync(IntencionEmisionOferta intencion,
        CancellationToken cancellationToken = default)
    {
        if (ErrorGuardarAntes is not null)
            throw ErrorGuardarAntes;
        Pendientes.Add(intencion);
        if (ErrorGuardarDespues is not null)
            throw ErrorGuardarDespues;
        return Task.CompletedTask;
    }

    public Task<IntencionEmisionOferta?> ObtenerAsync(Guid ofertaId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Pendientes.SingleOrDefault(valor => valor.OfertaId == ofertaId));

    public Task<IntencionEmisionOferta?> ObtenerPorPostulacionAsync(Guid postulacionId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Pendientes.SingleOrDefault(valor =>
            valor.PostulacionId == postulacionId));

    public Task<IReadOnlyList<Guid>> ListarPendientesAsync(int limite,
        Guid? despuesDe = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Guid>>(Pendientes
            .Select(valor => valor.OfertaId)
            .Where(id => despuesDe is null || id.CompareTo(despuesDe.Value) > 0)
            .Order().Take(limite).ToArray());

    public Task EliminarEnTransaccionAsync(Guid ofertaId,
        CancellationToken cancellationToken = default)
    {
        _eliminacionesPendientes.Add(ofertaId);
        return Task.CompletedTask;
    }
}
