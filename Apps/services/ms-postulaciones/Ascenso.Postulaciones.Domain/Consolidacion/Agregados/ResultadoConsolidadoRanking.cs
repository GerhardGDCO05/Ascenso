using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Consolidacion.Agregados;

/// <summary>
/// Raíz de agregado que consolida la puntuación definitiva de una postulación,
/// gestiona la trazabilidad de la excepción de evaluador único y audita la revisión de discrepancias.
/// </summary>
public sealed class ResultadoConsolidadoRanking : AggregateRoot
{
    public const int LongitudMinimaComentarioRevision = 10;

    public const int LongitudMaximaComentarioRevision = 1000;

    public const int LongitudMinimaJustificacionCasoLimite =
        CasoLimiteRankingDetectado.LongitudMinimaJustificacion;

    public const int LongitudMaximaJustificacionCasoLimite =
        CasoLimiteRankingDetectado.LongitudMaximaJustificacion;

    public const int LongitudMaximaJustificacionExcepcion =
        AutorizacionExcepcionEvaluadorUnico.LongitudMaximaJustificacion;

    // Constructor requerido por los adaptadores de persistencia.
    private ResultadoConsolidadoRanking()
    {
    }

    private ResultadoConsolidadoRanking(
        Guid id,
        ResultadoCalculoConsolidacion calculo,
        DateTimeOffset fechaConsolidacion,
        Guid usuarioActorId)
        : base(id, fechaConsolidacion, usuarioActorId)
    {
        ConvocatoriaId = calculo.ConvocatoriaId;
        PostulacionId = calculo.PostulacionId;
        AplicarCalculo(calculo, FechaCreacion);
        EstadoConfirmacionRanking = EstadoRanking.Provisional;
    }

    public Guid ConvocatoriaId { get; private set; }

    public Guid PostulacionId { get; private set; }

    public bool ExcepcionEvaluadorUnico { get; private set; }

    public string? JustificacionEvaluadorUnico { get; private set; }

    public Guid? AutorizadorExcepcionId { get; private set; }

    public int CantidadEvaluacionesValidas { get; private set; }

    public decimal PuntajeConsolidado { get; private set; }

    public decimal DiferenciaEntreEvaluadores { get; private set; }

    public decimal UmbralDiscrepanciaAplicado { get; private set; }

    public bool AlertaDiscrepancia { get; private set; }

    public bool DiscrepanciaRevisada { get; private set; }

    public string? ComentarioRevisionDiscrepancia { get; private set; }

    public Guid? RevisadoPorUsuarioId { get; private set; }

    public DateTimeOffset? FechaRevisionDiscrepancia { get; private set; }

    public EstadoRanking EstadoConfirmacionRanking { get; private set; }

    public DateTimeOffset FechaConsolidacion { get; private set; }

    public int? PosicionRanking { get; private set; }

    public decimal? MontoEstimadoBeca { get; private set; }

    public ClasificacionRanking? Clasificacion { get; private set; }

    public DateTimeOffset? FechaConfirmacionRanking { get; private set; }

    public Guid? ConfirmadoPorUsuarioId { get; private set; }

    public TipoCasoLimite? TipoCasoLimite { get; private set; }

    public bool? SeleccionadoEnCasoLimite { get; private set; }

    public string? JustificacionDesempateCasoLimite { get; private set; }

    public Guid? DecididoPorUsuarioId { get; private set; }

    public DateTimeOffset? FechaDecisionCasoLimite { get; private set; }

    public bool TieneDecisionCasoLimite => TipoCasoLimite.HasValue;

    public bool EsAdjudicable
        => EstadoConfirmacionRanking == EstadoRanking.Confirmado
            && Clasificacion == ClasificacionRanking.DentroDeCupo;

    public static ResultadoConsolidadoRanking Crear(
        ResultadoCalculoConsolidacion calculo,
        DateTimeOffset fechaConsolidacion,
        Guid usuarioActorId)
    {
        ArgumentNullException.ThrowIfNull(calculo);

        return new ResultadoConsolidadoRanking(
            UuidV7.Nuevo(),
            calculo,
            fechaConsolidacion,
            usuarioActorId);
    }

    public bool CoincideConCalculo(ResultadoCalculoConsolidacion calculo)
    {
        ArgumentNullException.ThrowIfNull(calculo);

        return ConvocatoriaId == calculo.ConvocatoriaId
            && PostulacionId == calculo.PostulacionId
            && ExcepcionEvaluadorUnico == calculo.ExcepcionEvaluadorUnico
            && string.Equals(JustificacionEvaluadorUnico, calculo.JustificacionEvaluadorUnico, StringComparison.Ordinal)
            && AutorizadorExcepcionId == calculo.AutorizadorExcepcionId
            && CantidadEvaluacionesValidas == calculo.CantidadEvaluacionesValidas
            && PuntajeConsolidado == calculo.PuntajeConsolidado
            && DiferenciaEntreEvaluadores == calculo.DiferenciaEntreEvaluadores
            && UmbralDiscrepanciaAplicado == calculo.UmbralDiscrepanciaAplicado
            && AlertaDiscrepancia == calculo.AlertaDiscrepancia;
    }

    public bool Recalcular(
        ResultadoCalculoConsolidacion calculo,
        DateTimeOffset fechaConsolidacion,
        Guid usuarioActorId)
    {
        ArgumentNullException.ThrowIfNull(calculo);
        ValidarArgumentosTemporalesYUsuario(fechaConsolidacion, usuarioActorId);

        if (EstadoConfirmacionRanking == EstadoRanking.Confirmado)
        {
            throw new RankingYaConfirmadoException();
        }

        if (calculo.PostulacionId != PostulacionId || calculo.ConvocatoriaId != ConvocatoriaId)
        {
            throw new ConsolidacionInvalidaException(
                "El cálculo de consolidación no corresponde a la convocatoria o postulación del resultado existente.");
        }

        if (CoincideConCalculo(calculo))
        {
            return false;
        }

        if (TieneDecisionCasoLimite)
        {
            throw new CasoLimiteYaResueltoException(
                "No es posible recalcular un resultado consolidado que ya cuenta con una decisión inalterable de caso límite.");
        }

        var fechaUtc = fechaConsolidacion.ToUniversalTime();
        AplicarCalculo(calculo, fechaUtc);
        DiscrepanciaRevisada = false;
        ComentarioRevisionDiscrepancia = null;
        RevisadoPorUsuarioId = null;
        FechaRevisionDiscrepancia = null;
        ActualizarAuditoria(usuarioActorId, fechaUtc);

        return true;
    }

    public void MarcarDiscrepanciaRevisada(
        string comentario,
        DateTimeOffset fechaRevision,
        Guid usuarioRevisorId)
    {
        ValidarArgumentosTemporalesYUsuario(fechaRevision, usuarioRevisorId);

        if (EstadoConfirmacionRanking == EstadoRanking.Confirmado)
        {
            throw new RankingYaConfirmadoException();
        }

        if (!AlertaDiscrepancia)
        {
            throw new DiscrepanciaNoDetectadaException();
        }

        if (DiscrepanciaRevisada)
        {
            throw new DiscrepanciaYaRevisadaException();
        }

        if (string.IsNullOrWhiteSpace(comentario))
        {
            throw new ComentarioRevisionDiscrepanciaInvalidoException();
        }

        var comentarioNormalizado = comentario.Trim();
        if (comentarioNormalizado.Length < LongitudMinimaComentarioRevision
            || comentarioNormalizado.Length > LongitudMaximaComentarioRevision)
        {
            throw new ComentarioRevisionDiscrepanciaInvalidoException();
        }

        var fechaUtc = fechaRevision.ToUniversalTime();
        DiscrepanciaRevisada = true;
        ComentarioRevisionDiscrepancia = comentarioNormalizado;
        RevisadoPorUsuarioId = usuarioRevisorId;
        FechaRevisionDiscrepancia = fechaUtc;
        ActualizarAuditoria(usuarioRevisorId, fechaUtc);
    }

    public void ConfirmarRanking(
        int posicionRanking,
        decimal montoEstimadoBeca,
        ClasificacionRanking clasificacion,
        DateTimeOffset fechaConfirmacion,
        Guid usuarioConfirmadorId)
    {
        ValidarArgumentosTemporalesYUsuario(fechaConfirmacion, usuarioConfirmadorId);

        if (EstadoConfirmacionRanking == EstadoRanking.Confirmado)
        {
            throw new RankingYaConfirmadoException();
        }

        if (posicionRanking < 1)
        {
            throw new RankingInvalidoException("La posición de ranking debe ser mayor o igual a 1.");
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

        var fechaUtc = fechaConfirmacion.ToUniversalTime();
        PosicionRanking = posicionRanking;
        MontoEstimadoBeca = montoEstimadoBeca;
        Clasificacion = clasificacion;
        FechaConfirmacionRanking = fechaUtc;
        ConfirmadoPorUsuarioId = usuarioConfirmadorId;
        EstadoConfirmacionRanking = EstadoRanking.Confirmado;
        ActualizarAuditoria(usuarioConfirmadorId, fechaUtc);
    }

    public void RegistrarDecisionCasoLimite(
        TipoCasoLimite tipoCasoLimite,
        bool seleccionado,
        string justificacion,
        DateTimeOffset fechaDecision,
        Guid usuarioDecisorId)
    {
        ValidarArgumentosTemporalesYUsuario(fechaDecision, usuarioDecisorId);

        if (EstadoConfirmacionRanking == EstadoRanking.Confirmado)
        {
            throw new RankingYaConfirmadoException();
        }

        if (TieneDecisionCasoLimite)
        {
            throw new CasoLimiteYaResueltoException();
        }

        if (!Enum.IsDefined(tipoCasoLimite))
        {
            throw new CasoLimiteInvalidoException("El tipo de caso límite indicado no es válido.");
        }

        if (string.IsNullOrWhiteSpace(justificacion))
        {
            throw new JustificacionCasoLimiteInvalidaException();
        }

        var justificacionNormalizada = justificacion.Trim();
        if (justificacionNormalizada.Length < LongitudMinimaJustificacionCasoLimite
            || justificacionNormalizada.Length > LongitudMaximaJustificacionCasoLimite)
        {
            throw new JustificacionCasoLimiteInvalidaException();
        }

        var fechaUtc = fechaDecision.ToUniversalTime();
        TipoCasoLimite = tipoCasoLimite;
        SeleccionadoEnCasoLimite = seleccionado;
        JustificacionDesempateCasoLimite = justificacionNormalizada;
        DecididoPorUsuarioId = usuarioDecisorId;
        FechaDecisionCasoLimite = fechaUtc;
        ActualizarAuditoria(usuarioDecisorId, fechaUtc);
    }

    private void AplicarCalculo(ResultadoCalculoConsolidacion calculo, DateTimeOffset fechaConsolidacionUtc)
    {
        ExcepcionEvaluadorUnico = calculo.ExcepcionEvaluadorUnico;
        JustificacionEvaluadorUnico = calculo.JustificacionEvaluadorUnico;
        AutorizadorExcepcionId = calculo.AutorizadorExcepcionId;
        CantidadEvaluacionesValidas = calculo.CantidadEvaluacionesValidas;
        PuntajeConsolidado = calculo.PuntajeConsolidado;
        DiferenciaEntreEvaluadores = calculo.DiferenciaEntreEvaluadores;
        UmbralDiscrepanciaAplicado = calculo.UmbralDiscrepanciaAplicado;
        AlertaDiscrepancia = calculo.AlertaDiscrepancia;
        FechaConsolidacion = fechaConsolidacionUtc;
    }

    private static void ValidarArgumentosTemporalesYUsuario(
        DateTimeOffset fechaOperacion,
        Guid usuarioActorId)
    {
        if (fechaOperacion == default)
        {
            throw new ArgumentException("La fecha de operación es obligatoria.", nameof(fechaOperacion));
        }

        if (usuarioActorId == Guid.Empty)
        {
            throw new ArgumentException("El usuario actor es obligatorio.", nameof(usuarioActorId));
        }
    }
}
