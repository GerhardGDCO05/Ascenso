using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Domain.Consolidacion.Servicios;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerRankingConvocatoria;

public sealed class ObtenerRankingConvocatoriaQueryHandler
{
    private readonly IRankingConvocatoriaRepository _rankingRepository;
    private readonly IDisponibilidadRankingClient _disponibilidadClient;
    private readonly IProveedorMontoEstimadoBeca _proveedorMonto;
    private readonly IValidator<ObtenerRankingConvocatoriaQuery> _validator;

    public ObtenerRankingConvocatoriaQueryHandler(
        IRankingConvocatoriaRepository rankingRepository,
        IDisponibilidadRankingClient disponibilidadClient,
        IProveedorMontoEstimadoBeca proveedorMonto,
        IValidator<ObtenerRankingConvocatoriaQuery> validator)
    {
        _rankingRepository = rankingRepository ?? throw new ArgumentNullException(nameof(rankingRepository));
        _disponibilidadClient = disponibilidadClient ?? throw new ArgumentNullException(nameof(disponibilidadClient));
        _proveedorMonto = proveedorMonto ?? throw new ArgumentNullException(nameof(proveedorMonto));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<RankingConvocatoriaDto> HandleAsync(
        ObtenerRankingConvocatoriaQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        ClasificacionRanking? filtroClasificacion = string.IsNullOrWhiteSpace(query.Clasificacion)
            ? null
            : CodigoEnum.DesdeCodigo<ClasificacionRanking>(query.Clasificacion);

        var resultados = await _rankingRepository.ListarPorConvocatoriaAsync(
            query.ConvocatoriaId,
            cancellationToken);

        if (resultados.Count > 0
            && resultados.All(r => r.EstadoConfirmacionRanking == EstadoRanking.Confirmado))
        {
            return RankingMapeador.ARankingConfirmadoDto(
                query.ConvocatoriaId,
                resultados,
                disponibilidad: null,
                filtroClasificacion: filtroClasificacion,
                pagina: query.Pagina,
                tamano: query.Tamano);
        }

        var disponibilidad = await _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(
            query.ConvocatoriaId,
            cancellationToken)
            ?? throw new ConvocatoriaNoEncontradaException();

        IReadOnlyDictionary<Guid, decimal> montos = resultados.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await _proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                query.ConvocatoriaId,
                disponibilidad.ProgramaBecaId,
                resultados.Select(r => r.PostulacionId).ToArray(),
                cancellationToken);

        var corte = ServicioCalculoRankingCorte.Calcular(
            query.ConvocatoriaId,
            resultados,
            montos,
            disponibilidad);

        return corte.ARankingProvisionalDto(
            filtroClasificacion: filtroClasificacion,
            pagina: query.Pagina,
            tamano: query.Tamano);
    }
}
