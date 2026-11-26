using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;

/// <summary>
/// Objeto de valor inmutable con la fotografía determinista resultante del cálculo de consolidación.
/// </summary>
public sealed record class ResultadoCalculoConsolidacion : ValueObject
{
    public const decimal PuntajeMinimo = 0.00m;

    public const decimal PuntajeMaximo = 100.00m;

    public ResultadoCalculoConsolidacion(
        Guid convocatoriaId,
        Guid postulacionId,
        bool excepcionEvaluadorUnico,
        string? justificacionEvaluadorUnico,
        Guid? autorizadorExcepcionId,
        int cantidadEvaluacionesValidas,
        decimal puntajeConsolidado,
        decimal diferenciaEntreEvaluadores,
        decimal umbralDiscrepanciaAplicado,
        bool alertaDiscrepancia)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ConsolidacionInvalidaException(
                "El identificador de la convocatoria es obligatorio.");
        }

        if (postulacionId == Guid.Empty)
        {
            throw new ConsolidacionInvalidaException(
                "El identificador de la postulación es obligatorio.");
        }

        ValidarPuntajeEnEscala(puntajeConsolidado, "El puntaje consolidado");
        ValidarPuntajeEnEscala(diferenciaEntreEvaluadores, "La diferencia entre evaluadores");

        if (umbralDiscrepanciaAplicado <= ParametrosConsolidacion.UmbralMinimoExclusivo
            || umbralDiscrepanciaAplicado > ParametrosConsolidacion.UmbralMaximo
            || decimal.Round(umbralDiscrepanciaAplicado, 2) != umbralDiscrepanciaAplicado)
        {
            throw new ConsolidacionInvalidaException(
                "El umbral de discrepancia aplicado debe estar en (0.00, 100.00] con máximo dos decimales.");
        }

        string? justificacionNormalizada;

        if (excepcionEvaluadorUnico)
        {
            if (cantidadEvaluacionesValidas != 1)
            {
                throw new ConsolidacionInvalidaException(
                    "Una consolidación con excepción de evaluador único debe tener exactamente una evaluación válida.");
            }

            if (string.IsNullOrWhiteSpace(justificacionEvaluadorUnico))
            {
                throw new ConsolidacionInvalidaException(
                    "La justificación de la excepción de evaluador único es obligatoria.");
            }

            justificacionNormalizada = justificacionEvaluadorUnico.Trim();
            if (justificacionNormalizada.Length > AutorizacionExcepcionEvaluadorUnico.LongitudMaximaJustificacion)
            {
                throw new ConsolidacionInvalidaException(
                    $"La justificación de la excepción no puede superar los {AutorizacionExcepcionEvaluadorUnico.LongitudMaximaJustificacion} caracteres.");
            }

            if (!autorizadorExcepcionId.HasValue || autorizadorExcepcionId.Value == Guid.Empty)
            {
                throw new ConsolidacionInvalidaException(
                    "El autorizador de la excepción de evaluador único es obligatorio.");
            }

            if (diferenciaEntreEvaluadores != 0.00m)
            {
                throw new ConsolidacionInvalidaException(
                    "La diferencia entre evaluadores debe ser 0.00 cuando aplica la excepción de evaluador único.");
            }

            if (alertaDiscrepancia)
            {
                throw new ConsolidacionInvalidaException(
                    "No puede existir alerta de discrepancia cuando aplica la excepción de evaluador único.");
            }
        }
        else
        {
            if (cantidadEvaluacionesValidas < ParametrosConsolidacion.EvaluadoresMinimosPorDefecto)
            {
                throw new ConsolidacionInvalidaException(
                    "Una consolidación multi-evaluador requiere al menos dos evaluaciones válidas.");
            }

            if (justificacionEvaluadorUnico is not null)
            {
                throw new ConsolidacionInvalidaException(
                    "No debe registrarse justificación de excepción en una consolidación multi-evaluador.");
            }

            if (autorizadorExcepcionId.HasValue)
            {
                throw new ConsolidacionInvalidaException(
                    "No debe registrarse autorizador de excepción en una consolidación multi-evaluador.");
            }

            var alertaEsperada = diferenciaEntreEvaluadores > umbralDiscrepanciaAplicado;
            if (alertaDiscrepancia != alertaEsperada)
            {
                throw new ConsolidacionInvalidaException(
                    "El indicador de alerta de discrepancia no es coherente con la diferencia y el umbral aplicado.");
            }

            justificacionNormalizada = null;
        }

        ConvocatoriaId = convocatoriaId;
        PostulacionId = postulacionId;
        ExcepcionEvaluadorUnico = excepcionEvaluadorUnico;
        JustificacionEvaluadorUnico = justificacionNormalizada;
        AutorizadorExcepcionId = autorizadorExcepcionId;
        CantidadEvaluacionesValidas = cantidadEvaluacionesValidas;
        PuntajeConsolidado = puntajeConsolidado;
        DiferenciaEntreEvaluadores = diferenciaEntreEvaluadores;
        UmbralDiscrepanciaAplicado = umbralDiscrepanciaAplicado;
        AlertaDiscrepancia = alertaDiscrepancia;
    }

    public Guid ConvocatoriaId { get; }

    public Guid PostulacionId { get; }

    public bool ExcepcionEvaluadorUnico { get; }

    public string? JustificacionEvaluadorUnico { get; }

    public Guid? AutorizadorExcepcionId { get; }

    public int CantidadEvaluacionesValidas { get; }

    public decimal PuntajeConsolidado { get; }

    public decimal DiferenciaEntreEvaluadores { get; }

    public decimal UmbralDiscrepanciaAplicado { get; }

    public bool AlertaDiscrepancia { get; }

    private static void ValidarPuntajeEnEscala(decimal valor, string nombreCampo)
    {
        if (valor < PuntajeMinimo || valor > PuntajeMaximo)
        {
            throw new ConsolidacionInvalidaException(
                $"{nombreCampo} debe estar comprendido entre {PuntajeMinimo:0.00} y {PuntajeMaximo:0.00}.");
        }

        if (decimal.Round(valor, 2) != valor)
        {
            throw new ConsolidacionInvalidaException(
                $"{nombreCampo} no puede tener más de dos decimales.");
        }
    }
}
