using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de persistencia y consulta para el agregado <see cref="ResultadoConsolidadoRanking"/>.
/// </summary>
public interface IResultadoConsolidadoRepository
{
    Task<ResultadoConsolidadoRanking?> ObtenerPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default);

    Task<ResultadoConsolidadoRanking?> ObtenerParaActualizarPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default);

    Task<ResultadoConsolidadoRanking?> RecargarParaActualizarPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default);

    Task<PaginaResultado<ResultadoConsolidadoRanking>> ListarPorConvocatoriaAsync(
        Guid convocatoriaId,
        bool? alertaDiscrepancia,
        bool? discrepanciaRevisada,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        ResultadoConsolidadoRanking resultado,
        CancellationToken cancellationToken = default);
}
