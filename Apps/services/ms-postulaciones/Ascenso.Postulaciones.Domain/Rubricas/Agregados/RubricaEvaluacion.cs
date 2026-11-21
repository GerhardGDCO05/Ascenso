using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;

namespace Ascenso.Postulaciones.Domain.Rubricas.Agregados;

/// <summary>
/// Raíz de agregado que representa la rúbrica de evaluación cuantitativa de un programa
/// y convocatoria de beca.
/// </summary>
public sealed class RubricaEvaluacion : AggregateRoot
{
    public const int LongitudMaximaNombre = 200;
    public const decimal SumaObjetivoPesos = 100.00m;

    private readonly List<CriterioEvaluacion> _criterios = [];

    // Constructor requerido por los adaptadores de persistencia.
    private RubricaEvaluacion()
    {
    }

    private RubricaEvaluacion(
        Guid programaBecaId,
        Guid convocatoriaId,
        string nombre,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
        : base(UuidV7.Nuevo(), fechaCreacion, usuarioCreacionId)
    {
        ProgramaBecaId = programaBecaId;
        ConvocatoriaId = convocatoriaId;
        Nombre = nombre;
        Estado = EstadoRubrica.Borrador;
    }

    public Guid ProgramaBecaId { get; private set; }

    public Guid ConvocatoriaId { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public EstadoRubrica Estado { get; private set; }

    public DateTimeOffset? FechaPublicacion { get; private set; }

    public IReadOnlyCollection<CriterioEvaluacion> Criterios => _criterios.AsReadOnly();

    public decimal SumaPesos => _criterios.Count == 0
        ? 0.00m
        : _criterios.Sum(criterio => criterio.PesoPorcentual);

    public decimal DiferenciaPara100 => SumaObjetivoPesos - SumaPesos;

    public static RubricaEvaluacion Crear(
        Guid programaBecaId,
        Guid convocatoriaId,
        string nombre,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
    {
        if (programaBecaId == Guid.Empty)
        {
            throw new RubricaEvaluacionInvalidaException(
                "El identificador del programa de beca es obligatorio.");
        }

        if (convocatoriaId == Guid.Empty)
        {
            throw new RubricaEvaluacionInvalidaException(
                "El identificador de la convocatoria es obligatorio.");
        }

        var nombreNormalizado = NormalizarNombre(nombre);

        return new RubricaEvaluacion(
            programaBecaId,
            convocatoriaId,
            nombreNormalizado,
            fechaCreacion,
            usuarioCreacionId);
    }

    public CriterioEvaluacion AgregarCriterio(
        string nombre,
        string descripcion,
        decimal pesoPorcentual,
        decimal puntajeMinimo,
        decimal puntajeMaximo,
        DateTimeOffset fechaModificacion,
        Guid usuarioModificacionId)
    {
        AsegurarEstadoBorrador();

        var siguienteOrden = _criterios.Count == 0
            ? 1
            : _criterios.Max(criterio => criterio.Orden) + 1;

        var criterio = CriterioEvaluacion.Crear(
            Id,
            nombre,
            descripcion,
            pesoPorcentual,
            puntajeMinimo,
            puntajeMaximo,
            siguienteOrden,
            fechaModificacion,
            usuarioModificacionId);

        ActualizarAuditoria(usuarioModificacionId, fechaModificacion);
        _criterios.Add(criterio);

        return criterio;
    }

    public CriterioEvaluacion EditarCriterio(
        Guid criterioId,
        string nombre,
        string descripcion,
        decimal pesoPorcentual,
        decimal puntajeMinimo,
        decimal puntajeMaximo,
        DateTimeOffset fechaModificacion,
        Guid usuarioModificacionId)
    {
        AsegurarEstadoBorrador();

        var criterio = ObtenerCriterioRequerido(criterioId);

        criterio.Actualizar(
            nombre,
            descripcion,
            pesoPorcentual,
            puntajeMinimo,
            puntajeMaximo,
            fechaModificacion,
            usuarioModificacionId);

        ActualizarAuditoria(usuarioModificacionId, fechaModificacion);

        return criterio;
    }

    public void EliminarCriterio(
        Guid criterioId,
        DateTimeOffset fechaModificacion,
        Guid usuarioModificacionId)
    {
        AsegurarEstadoBorrador();

        var criterio = ObtenerCriterioRequerido(criterioId);

        ActualizarAuditoria(usuarioModificacionId, fechaModificacion);
        _criterios.Remove(criterio);
    }

    public void Publicar(
        DateTimeOffset fechaPublicacion,
        Guid usuarioPublicacionId)
    {
        AsegurarEstadoBorrador();

        if (SumaPesos != SumaObjetivoPesos)
        {
            throw new PesosInvalidosException(SumaPesos, DiferenciaPara100);
        }

        ActualizarAuditoria(usuarioPublicacionId, fechaPublicacion);
        Estado = EstadoRubrica.Publicada;
        FechaPublicacion = FechaUltimaModificacion;
    }

    private void AsegurarEstadoBorrador()
    {
        if (Estado != EstadoRubrica.Borrador)
        {
            throw new RubricaBloqueadaException();
        }
    }

    private CriterioEvaluacion ObtenerCriterioRequerido(Guid criterioId)
        => _criterios.FirstOrDefault(criterio => criterio.Id == criterioId)
            ?? throw new CriterioNoEncontradoException();

    private static string NormalizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new RubricaEvaluacionInvalidaException(
                "El nombre de la rúbrica de evaluación es obligatorio.");
        }

        var nombreNormalizado = nombre.Trim();

        if (nombreNormalizado.Length > LongitudMaximaNombre)
        {
            throw new RubricaEvaluacionInvalidaException(
                $"El nombre de la rúbrica de evaluación no puede superar los {LongitudMaximaNombre} caracteres.");
        }

        return nombreNormalizado;
    }
}
