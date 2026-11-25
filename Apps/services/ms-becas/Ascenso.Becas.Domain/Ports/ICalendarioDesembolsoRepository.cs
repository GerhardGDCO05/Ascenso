using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Desembolsos.Entidades;

namespace Ascenso.Becas.Domain.Ports;

public interface ICalendarioDesembolsoRepository
{
    Task AgregarAsync(CalendarioDesembolso calendario, CancellationToken cancellationToken = default);

    Task<CalendarioDesembolso?> ObtenerPorIdAsync(Guid calendarioId, CancellationToken cancellationToken = default);

    Task<CalendarioDesembolso?> ObtenerPorBecaYPeriodoAsync(
        Guid becaId, Guid periodoId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalendarioDesembolso>> ObtenerPorBecaAsync(
        Guid becaId, int pagina, int tamanoPagina, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Desembolso>> ObtenerCuotasAsync(
        Guid calendarioId, int pagina, int tamanoPagina,
        CancellationToken cancellationToken = default);
}
