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
        DateTimeOffset fechaConsolidacion,
        TipoCasoLimite? tipoCasoLimite = null,
        bool? seleccionadoEnCasoLimite = null,
        string? justificacionDesempateCasoLimite = null,
        Guid? decididoPorUsuarioId = null,
        DateTimeOffset? fechaDecisionCasoLimite = null,
        bool casoLimiteResuelto = false)
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

        var tieneAlgunDatoCasoLimite = tipoCasoLimite.HasValue
            || seleccionadoEnCasoLimite.HasValue
            || justificacionDesempateCasoLimite is not null
            || decididoPorUsuarioId.HasValue
            || fechaDecisionCasoLimite.HasValue;

        var tieneTodosLosDatosCasoLimite = tipoCasoLimite.HasValue
            && seleccionadoEnCasoLimite.HasValue
            && !string.IsNullOrWhiteSpace(justificacionDesempateCasoLimite)
            && decididoPorUsuarioId.HasValue
            && fechaDecisionCasoLimite.HasValue;

        if (tieneAlgunDatoCasoLimite && !tieneTodosLosDatosCasoLimite)
        {
            throw new RankingInvalidoException(
                "Los metadatos de decisión de caso límite deben estar completamente poblados o completamente nulos.");
        }

        string? justificacionNormalizada = null;
        DateTimeOffset? fechaDecisionUtc = null;

        if (tieneTodosLosDatosCasoLimite)
        {
            if (!Enum.IsDefined(tipoCasoLimite!.Value))
            {
                throw new RankingInvalidoException("El tipo de caso límite indicado no es válido.");
            }

            justificacionNormalizada = justificacionDesempateCasoLimite!.Trim();
            if (justificacionNormalizada.Length < CasoLimiteRankingDetectado.LongitudMinimaJustificacion
                || justificacionNormalizada.Length > CasoLimiteRankingDetectado.LongitudMaximaJustificacion)
            {
                throw new RankingInvalidoException(
                    $"La justificación del caso límite debe tener entre {CasoLimiteRankingDetectado.LongitudMinimaJustificacion} y {CasoLimiteRankingDetectado.LongitudMaximaJustificacion} caracteres.");
            }

            if (decididoPorUsuarioId!.Value == Guid.Empty)
            {
                throw new RankingInvalidoException(
                    "El identificador del usuario decisor del caso límite es obligatorio.");
            }

            if (fechaDecisionCasoLimite!.Value == default)
            {
                throw new RankingInvalidoException(
                    "La fecha de decisión del caso límite es obligatoria.");
            }

            fechaDecisionUtc = fechaDecisionCasoLimite.Value.ToUniversalTime();
        }

        if (casoLimiteResuelto && !tipoCasoLimite.HasValue)
        {
            throw new RankingInvalidoException(
                "No puede marcarse un elemento como caso límite resuelto sin metadatos de decisión.");
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
        TipoCasoLimite = tipoCasoLimite;
        SeleccionadoEnCasoLimite = seleccionadoEnCasoLimite;
        JustificacionDesempateCasoLimite = justificacionNormalizada;
        DecididoPorUsuarioId = decididoPorUsuarioId;
        FechaDecisionCasoLimite = fechaDecisionUtc;
        CasoLimiteResuelto = casoLimiteResuelto;
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

    public TipoCasoLimite? TipoCasoLimite { get; }

    public bool? SeleccionadoEnCasoLimite { get; }

    public string? JustificacionDesempateCasoLimite { get; }

    public Guid? DecididoPorUsuarioId { get; }

    public DateTimeOffset? FechaDecisionCasoLimite { get; }

    public bool CasoLimiteResuelto { get; }
}
