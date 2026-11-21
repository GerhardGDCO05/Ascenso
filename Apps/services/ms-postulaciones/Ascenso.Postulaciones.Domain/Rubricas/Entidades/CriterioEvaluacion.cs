using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Rubricas.Entidades;

/// <summary>
/// Criterio de evaluación que forma parte del agregado de una rúbrica de evaluación.
/// </summary>
public sealed class CriterioEvaluacion : EntidadAuditable
{
    public const int LongitudMaximaNombre = 200;
    public const int LongitudMaximaDescripcion = 1000;
    public const decimal PesoMinimoExclusivo = 0m;
    public const decimal PesoMaximo = 100.00m;
    public const decimal PuntajeMinimoPermitido = 0.00m;
    public const decimal PuntajeMaximoPermitido = 1000.00m;
    public const int DecimalesMaximos = 2;

    // Constructor requerido por los adaptadores de persistencia.
    private CriterioEvaluacion()
    {
    }

    private CriterioEvaluacion(
        Guid rubricaEvaluacionId,
        string nombre,
        string descripcion,
        decimal pesoPorcentual,
        decimal puntajeMinimo,
        decimal puntajeMaximo,
        int orden,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
        : base(UuidV7.Nuevo(), fechaCreacion, usuarioCreacionId)
    {
        RubricaEvaluacionId = rubricaEvaluacionId;
        Nombre = nombre;
        Descripcion = descripcion;
        PesoPorcentual = pesoPorcentual;
        PuntajeMinimo = puntajeMinimo;
        PuntajeMaximo = puntajeMaximo;
        Orden = orden;
    }

    public Guid RubricaEvaluacionId { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public string Descripcion { get; private set; } = string.Empty;

    public decimal PesoPorcentual { get; private set; }

    public decimal PuntajeMinimo { get; private set; }

    public decimal PuntajeMaximo { get; private set; }

    public int Orden { get; private set; }

    public static CriterioEvaluacion Crear(
        Guid rubricaEvaluacionId,
        string nombre,
        string descripcion,
        decimal pesoPorcentual,
        decimal puntajeMinimo,
        decimal puntajeMaximo,
        int orden,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
    {
        if (rubricaEvaluacionId == Guid.Empty)
        {
            throw new CriterioEvaluacionInvalidoException(
                "El criterio de evaluación debe estar asociado a una rúbrica.");
        }

        if (orden <= 0)
        {
            throw new CriterioEvaluacionInvalidoException(
                "El orden del criterio de evaluación debe ser un número entero positivo.");
        }

        var nombreNormalizado = NormalizarNombre(nombre);
        var descripcionNormalizada = NormalizarDescripcion(descripcion);
        ValidarPesoPorcentual(pesoPorcentual);
        ValidarRangoPuntaje(puntajeMinimo, puntajeMaximo);

        return new CriterioEvaluacion(
            rubricaEvaluacionId,
            nombreNormalizado,
            descripcionNormalizada,
            pesoPorcentual,
            puntajeMinimo,
            puntajeMaximo,
            orden,
            fechaCreacion,
            usuarioCreacionId);
    }

    internal void Actualizar(
        string nombre,
        string descripcion,
        decimal pesoPorcentual,
        decimal puntajeMinimo,
        decimal puntajeMaximo,
        DateTimeOffset fechaModificacion,
        Guid usuarioModificacionId)
    {
        var nombreNormalizado = NormalizarNombre(nombre);
        var descripcionNormalizada = NormalizarDescripcion(descripcion);
        ValidarPesoPorcentual(pesoPorcentual);
        ValidarRangoPuntaje(puntajeMinimo, puntajeMaximo);

        ActualizarAuditoria(usuarioModificacionId, fechaModificacion);

        Nombre = nombreNormalizado;
        Descripcion = descripcionNormalizada;
        PesoPorcentual = pesoPorcentual;
        PuntajeMinimo = puntajeMinimo;
        PuntajeMaximo = puntajeMaximo;
    }

    private static string NormalizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new CriterioEvaluacionInvalidoException(
                "El nombre del criterio de evaluación es obligatorio.");
        }

        var nombreNormalizado = nombre.Trim();

        if (nombreNormalizado.Length > LongitudMaximaNombre)
        {
            throw new CriterioEvaluacionInvalidoException(
                $"El nombre del criterio de evaluación no puede superar los {LongitudMaximaNombre} caracteres.");
        }

        return nombreNormalizado;
    }

    private static string NormalizarDescripcion(string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new CriterioEvaluacionInvalidoException(
                "La descripción del criterio de evaluación es obligatoria.");
        }

        var descripcionNormalizada = descripcion.Trim();

        if (descripcionNormalizada.Length > LongitudMaximaDescripcion)
        {
            throw new CriterioEvaluacionInvalidoException(
                $"La descripción del criterio de evaluación no puede superar los {LongitudMaximaDescripcion} caracteres.");
        }

        return descripcionNormalizada;
    }

    private static void ValidarPesoPorcentual(decimal pesoPorcentual)
    {
        if (pesoPorcentual is <= PesoMinimoExclusivo or > PesoMaximo)
        {
            throw new CriterioEvaluacionInvalidoException(
                "El peso porcentual debe ser mayor que 0 y menor o igual que 100.00.");
        }

        if (decimal.Round(pesoPorcentual, DecimalesMaximos) != pesoPorcentual)
        {
            throw new CriterioEvaluacionInvalidoException(
                $"El peso porcentual admite como máximo {DecimalesMaximos} decimales.");
        }
    }

    private static void ValidarRangoPuntaje(decimal puntajeMinimo, decimal puntajeMaximo)
    {
        if (puntajeMinimo is < PuntajeMinimoPermitido or > PuntajeMaximoPermitido)
        {
            throw new CriterioEvaluacionInvalidoException(
                "El puntaje mínimo debe estar entre 0.00 y 1000.00.");
        }

        if (decimal.Round(puntajeMinimo, DecimalesMaximos) != puntajeMinimo)
        {
            throw new CriterioEvaluacionInvalidoException(
                $"El puntaje mínimo admite como máximo {DecimalesMaximos} decimales.");
        }

        if (puntajeMaximo is < PuntajeMinimoPermitido or > PuntajeMaximoPermitido)
        {
            throw new CriterioEvaluacionInvalidoException(
                "El puntaje máximo debe estar entre 0.00 y 1000.00.");
        }

        if (decimal.Round(puntajeMaximo, DecimalesMaximos) != puntajeMaximo)
        {
            throw new CriterioEvaluacionInvalidoException(
                $"El puntaje máximo admite como máximo {DecimalesMaximos} decimales.");
        }

        if (puntajeMinimo >= puntajeMaximo)
        {
            throw new CriterioEvaluacionInvalidoException(
                "El puntaje mínimo debe ser estrictamente menor que el puntaje máximo.");
        }
    }
}
