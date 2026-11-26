using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;

/// <summary>
/// Objeto de valor inmutable que representa una entrada evaluada dentro del ranking de una convocatoria.
/// </summary>
public sealed record class ElementoRankingEvaluado : ValueObject
{
    public ElementoRankingEvaluado(
        Guid resultadoConsolidadoId,
        Guid postulacionId,
        int posicionRanking,
        decimal puntajeConsolidado,
        decimal montoEstimadoBeca,
        ClasificacionRanking clasificacion,
        bool empatadoEnLineaDeCorte,
        bool alertaDiscrepancia,
        bool discrepanciaRevisada,
        bool afectaAdvertenciaDiscrepanciaEnCorte,
        bool excepcionEvaluadorUnico,
        DateTimeOffset fechaConsolidacion)
    {
        if (resultadoConsolidadoId == Guid.Empty)
        {
            throw new RankingInvalidoException("El identificador del resultado consolidado es obligatorio.");
        }

        if (postulacionId == Guid.Empty)
        {
            throw new RankingInvalidoException("El identificador de la postulación es obligatorio.");
        }

        if (posicionRanking < 1)
        {
            throw new RankingInvalidoException("La posición de ranking debe ser mayor o igual a 1.");
        }

        if (puntajeConsolidado < ResultadoCalculoConsolidacion.PuntajeMinimo
            || puntajeConsolidado > ResultadoCalculoConsolidacion.PuntajeMaximo
            || decimal.Round(puntajeConsolidado, 2) != puntajeConsolidado)
        {
            throw new RankingInvalidoException(
                "El puntaje consolidado debe estar comprendido entre 0.00 y 100.00 con máximo dos decimales.");
        }

        if (montoEstimadoBeca <= 0.00m || decimal.Round(montoEstimadoBeca, 2) != montoEstimadoBeca)
        {
            throw new RankingInvalidoException(
                "El monto estimado de beca debe ser mayor a 0.00 con máximo dos decimales.");
        }

        if (!Enum.IsDefined(clasificacion))
        {
            throw new RankingInvalidoException("La clasificación de ranking indicada no es válida.");
        }

        if (discrepanciaRevisada && !alertaDiscrepancia)
        {
            throw new RankingInvalidoException(
                "No puede marcarse una discrepancia como revisada sin una alerta de discrepancia activa.");
        }

        if (afectaAdvertenciaDiscrepanciaEnCorte && (!alertaDiscrepancia || discrepanciaRevisada))
        {
            throw new RankingInvalidoException(
                "Solo una alerta de discrepancia sin revisar puede activar la advertencia en línea de corte.");
        }

        if (fechaConsolidacion == default)
        {
            throw new RankingInvalidoException("La fecha de consolidación es obligatoria.");
        }

        ResultadoConsolidadoId = resultadoConsolidadoId;
        PostulacionId = postulacionId;
        PosicionRanking = posicionRanking;
        PuntajeConsolidado = puntajeConsolidado;
        MontoEstimadoBeca = montoEstimadoBeca;
        Clasificacion = clasificacion;
        EmpatadoEnLineaDeCorte = empatadoEnLineaDeCorte;
        AlertaDiscrepancia = alertaDiscrepancia;
        DiscrepanciaRevisada = discrepanciaRevisada;
        AfectaAdvertenciaDiscrepanciaEnCorte = afectaAdvertenciaDiscrepanciaEnCorte;
        ExcepcionEvaluadorUnico = excepcionEvaluadorUnico;
        FechaConsolidacion = fechaConsolidacion.ToUniversalTime();
    }

    public Guid ResultadoConsolidadoId { get; }

    public Guid PostulacionId { get; }

    public int PosicionRanking { get; }

    public decimal PuntajeConsolidado { get; }

    public decimal MontoEstimadoBeca { get; }

    public ClasificacionRanking Clasificacion { get; }

    public bool EmpatadoEnLineaDeCorte { get; }

    public bool AlertaDiscrepancia { get; }

    public bool DiscrepanciaRevisada { get; }

    public bool AfectaAdvertenciaDiscrepanciaEnCorte { get; }

    public bool ExcepcionEvaluadorUnico { get; }

    public DateTimeOffset FechaConsolidacion { get; }
}
