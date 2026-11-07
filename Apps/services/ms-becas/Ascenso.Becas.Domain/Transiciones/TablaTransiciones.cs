using Ascenso.Becas.Domain.Excepciones;

namespace Ascenso.Becas.Domain.Transiciones;

/// <summary>
/// Tabla de transiciones de estado cerrada y testeable.
/// </summary>
public sealed class TablaTransiciones<TEstado>
    where TEstado : struct, Enum
{
    private readonly HashSet<(TEstado Origen, TEstado Destino)> _permitidas;
    private readonly HashSet<TEstado> _terminales;

    public TablaTransiciones(
        IEnumerable<(TEstado Origen, TEstado Destino)> permitidas,
        IEnumerable<TEstado> terminales)
    {
        ArgumentNullException.ThrowIfNull(permitidas);
        ArgumentNullException.ThrowIfNull(terminales);

        _permitidas = [.. permitidas];
        _terminales = [.. terminales];

        if (_permitidas.Count == 0)
        {
            throw new ArgumentException("La tabla debe contener al menos una transición permitida.", nameof(permitidas));
        }
    }

    public IReadOnlyCollection<(TEstado Origen, TEstado Destino)> Permitidas
        => _permitidas.ToArray();

    public IReadOnlyCollection<TEstado> Terminales
        => _terminales.ToArray();

    public bool EsPermitida(TEstado origen, TEstado destino)
        => _permitidas.Contains((origen, destino));

    public bool EsTerminal(TEstado estado)
        => _terminales.Contains(estado);

    public void Validar(TEstado origen, TEstado destino)
    {
        if (!EsPermitida(origen, destino))
        {
            throw new TransicionEstadoInvalidaException(origen, destino);
        }
    }
}
