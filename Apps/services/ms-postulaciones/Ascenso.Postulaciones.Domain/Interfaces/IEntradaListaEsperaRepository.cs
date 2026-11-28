using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de persistencia y consulta para el agregado raíz <see cref="EntradaListaEspera"/>.
/// </summary>
public interface IEntradaListaEsperaRepository
{
    Task AgregarRangoAsync(
        IReadOnlyCollection<EntradaListaEspera> entradas,
        CancellationToken cancellationToken = default);

    Task<EntradaListaEspera?> ObtenerPorIdAsync(
        Guid entradaListaEsperaId,
        CancellationToken cancellationToken = default);

    Task<EntradaListaEspera?> ObtenerPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EntradaListaEspera>> ListarPorConvocatoriaAsync(
        Guid convocatoriaId,
        EstadoListaEspera? estado = null,
        CancellationToken cancellationToken = default);

    Task<EntradaListaEspera?> ObtenerSiguientePendientePorConvocatoriaAsync(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default);
}
