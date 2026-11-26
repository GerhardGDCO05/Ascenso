using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Entidades;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Evaluaciones.Servicios;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;

namespace Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;

/// <summary>
/// Raíz de agregado que representa la evaluación cuantitativa realizada por un miembro de comité
/// sobre una postulación asignada.
/// </summary>
public sealed class Evaluacion : AggregateRoot
{
    public const int LongitudMaximaComentariosGenerales = 2000;

    private readonly List<CalificacionCriterio> _calificaciones = [];

    // Constructor requerido por los adaptadores de persistencia.
    private Evaluacion()
    {
    }

    private Evaluacion(
        Guid id,
        Guid asignacionEvaluacionId,
        Guid postulacionId,
        Guid convocatoriaId,
        Guid rubricaEvaluacionId,
        Guid evaluadorId,
        string? comentariosGenerales,
        DateTimeOffset fechaInicio,
        Guid usuarioEvaluadorId)
        : base(id, fechaInicio, usuarioEvaluadorId)
    {
        AsignacionEvaluacionId = asignacionEvaluacionId;
        PostulacionId = postulacionId;
        ConvocatoriaId = convocatoriaId;
        RubricaEvaluacionId = rubricaEvaluacionId;
        EvaluadorId = evaluadorId;
        FechaInicio = FechaCreacion;
        ComentariosGenerales = comentariosGenerales;
        Estado = EstadoEvaluacion.Borrador;
    }

    public Guid AsignacionEvaluacionId { get; private set; }

    public Guid PostulacionId { get; private set; }

    public Guid ConvocatoriaId { get; private set; }

    public Guid RubricaEvaluacionId { get; private set; }

    public Guid EvaluadorId { get; private set; }

    public DateTimeOffset FechaInicio { get; private set; }

    public DateTimeOffset? FechaEnvio { get; private set; }

    public decimal? PuntajeTotalPonderado { get; private set; }

    public string? ComentariosGenerales { get; private set; }

    public EstadoEvaluacion Estado { get; private set; }

    public string? MotivoInvalidacion { get; private set; }

    public IReadOnlyCollection<CalificacionCriterio> Calificaciones => _calificaciones.AsReadOnly();

    public static Evaluacion CrearBorrador(
        AsignacionEvaluacion asignacion,
        RubricaEvaluacion rubrica,
        IReadOnlyCollection<DatosCalificacionCriterio> calificaciones,
        string? comentariosGenerales,
        DateTimeOffset ahora,
        Guid usuarioEvaluadorId)
    {
        ArgumentNullException.ThrowIfNull(asignacion);
        ArgumentNullException.ThrowIfNull(rubrica);
        ArgumentNullException.ThrowIfNull(calificaciones);
        ValidarArgumentosTemporalesYUsuario(ahora, usuarioEvaluadorId);

        if (asignacion.UsuarioEvaluadorId != usuarioEvaluadorId)
        {
            throw new EvaluacionConfidencialException();
        }

        switch (asignacion.Estado)
        {
            case EstadoAsignacionEvaluacion.Pendiente:
            case EstadoAsignacionEvaluacion.EnProgreso:
                break;

            case EstadoAsignacionEvaluacion.Completada:
                throw new EvaluacionYaEnviadaException();

            default:
                throw new AsignacionNoEvaluableException();
        }

        if (ahora > asignacion.FechaLimiteEvaluacion)
        {
            throw new PlazoEvaluacionVencidoException();
        }

        if (rubrica.Estado == EstadoRubrica.Borrador)
        {
            throw new RubricaNoPublicadaException();
        }

        if (rubrica.ConvocatoriaId != asignacion.ConvocatoriaId)
        {
            throw new EvaluacionInvalidaException(
                "La rúbrica de evaluación no corresponde a la convocatoria de la asignación.");
        }

        var comentariosNormalizados = NormalizarComentariosGenerales(comentariosGenerales);
        var paresValidados = ValidarDatosCalificaciones(rubrica, calificaciones);
        var evaluacionId = UuidV7.Nuevo();

        var nuevasCalificaciones = new List<CalificacionCriterio>(paresValidados.Count);
        foreach (var (criterio, datos) in paresValidados)
        {
            nuevasCalificaciones.Add(
                CalificacionCriterio.Crear(evaluacionId, criterio, datos, ahora, usuarioEvaluadorId));
        }

        var evaluacion = new Evaluacion(
            evaluacionId,
            asignacion.Id,
            asignacion.PostulacionId,
            asignacion.ConvocatoriaId,
            rubrica.Id,
            asignacion.UsuarioEvaluadorId,
            comentariosNormalizados,
            ahora,
            usuarioEvaluadorId);

        evaluacion._calificaciones.AddRange(nuevasCalificaciones);

        return evaluacion;
    }

    public void ActualizarBorrador(
        RubricaEvaluacion rubrica,
        IReadOnlyCollection<DatosCalificacionCriterio> calificaciones,
        string? comentariosGenerales,
        DateTimeOffset fechaLimiteEvaluacion,
        DateTimeOffset ahora,
        Guid usuarioEvaluadorId)
    {
        ArgumentNullException.ThrowIfNull(rubrica);
        ArgumentNullException.ThrowIfNull(calificaciones);
        ValidarArgumentosTemporalesYUsuario(ahora, usuarioEvaluadorId);
        ValidarFechaLimite(fechaLimiteEvaluacion);

        AsegurarPertenencia(usuarioEvaluadorId);
        AsegurarEstadoBorrador();

        if (ahora > fechaLimiteEvaluacion)
        {
            throw new PlazoEvaluacionVencidoException();
        }

        ValidarRubricaCoherente(rubrica);

        var comentariosNormalizados = NormalizarComentariosGenerales(comentariosGenerales);
        var paresValidados = ValidarDatosCalificaciones(rubrica, calificaciones);

        // Validar rangos antes de mutar cualquier calificación existente o la colección.
        foreach (var (criterio, datos) in paresValidados)
        {
            if (datos.PuntajeAsignado < criterio.PuntajeMinimo
                || datos.PuntajeAsignado > criterio.PuntajeMaximo)
            {
                throw new PuntajeFueraDeRangoException(
                    criterio.Nombre,
                    criterio.PuntajeMinimo,
                    criterio.PuntajeMaximo,
                    datos.PuntajeAsignado);
            }
        }

        var existentesPorCriterioId = _calificaciones.ToDictionary(c => c.CriterioEvaluacionId);
        var criteriosSolicitados = new HashSet<Guid>(paresValidados.Count);

        foreach (var (criterio, datos) in paresValidados)
        {
            criteriosSolicitados.Add(criterio.Id);

            if (existentesPorCriterioId.TryGetValue(criterio.Id, out var existente))
            {
                existente.Actualizar(criterio, datos, ahora, usuarioEvaluadorId);
            }
            else
            {
                _calificaciones.Add(
                    CalificacionCriterio.Crear(Id, criterio, datos, ahora, usuarioEvaluadorId));
            }
        }

        _calificaciones.RemoveAll(c => !criteriosSolicitados.Contains(c.CriterioEvaluacionId));

        ComentariosGenerales = comentariosNormalizados;
        ActualizarAuditoria(usuarioEvaluadorId, ahora);
    }

    public void EnviarDefinitiva(
        RubricaEvaluacion rubrica,
        DateTimeOffset fechaLimiteEvaluacion,
        DateTimeOffset ahora,
        Guid usuarioEvaluadorId)
    {
        ArgumentNullException.ThrowIfNull(rubrica);
        ValidarArgumentosTemporalesYUsuario(ahora, usuarioEvaluadorId);
        ValidarFechaLimite(fechaLimiteEvaluacion);

        AsegurarPertenencia(usuarioEvaluadorId);
        AsegurarEstadoBorrador();

        if (ahora > fechaLimiteEvaluacion)
        {
            throw new PlazoEvaluacionVencidoException();
        }

        ValidarRubricaCoherente(rubrica);

        var puntajeTotal = ServicioCalculoPuntajeEvaluacion.CalcularPuntajeTotalPonderado(
            rubrica,
            _calificaciones);

        ActualizarAuditoria(usuarioEvaluadorId, ahora);
        PuntajeTotalPonderado = puntajeTotal;
        FechaEnvio = FechaUltimaModificacion;
        Estado = EstadoEvaluacion.EnviadaDefinitiva;
    }

    private void AsegurarPertenencia(Guid usuarioEvaluadorId)
    {
        if (EvaluadorId != usuarioEvaluadorId)
        {
            throw new EvaluacionConfidencialException();
        }
    }

    private void AsegurarEstadoBorrador()
    {
        switch (Estado)
        {
            case EstadoEvaluacion.Borrador:
                return;

            case EstadoEvaluacion.EnviadaDefinitiva:
                throw new EvaluacionYaEnviadaException();

            case EstadoEvaluacion.InvalidadaPorConflicto:
                throw new EvaluacionInvalidadaException();

            default:
                throw new EvaluacionInvalidaException();
        }
    }

    private void ValidarRubricaCoherente(RubricaEvaluacion rubrica)
    {
        if (rubrica.Estado == EstadoRubrica.Borrador)
        {
            throw new RubricaNoPublicadaException();
        }

        if (rubrica.Id != RubricaEvaluacionId)
        {
            throw new EvaluacionInvalidaException(
                "La rúbrica de evaluación indicada no coincide con la rúbrica asociada a la evaluación.");
        }
    }

    private static List<(CriterioEvaluacion Criterio, DatosCalificacionCriterio Datos)> ValidarDatosCalificaciones(
        RubricaEvaluacion rubrica,
        IReadOnlyCollection<DatosCalificacionCriterio> calificaciones)
    {
        if (calificaciones.Count == 0)
        {
            throw new EvaluacionInvalidaException(
                "La evaluación debe registrar al menos una calificación de criterio.");
        }

        var criteriosPorId = rubrica.Criterios.ToDictionary(criterio => criterio.Id);
        var criteriosVistos = new HashSet<Guid>(calificaciones.Count);
        var resultado = new List<(CriterioEvaluacion, DatosCalificacionCriterio)>(calificaciones.Count);

        foreach (var datos in calificaciones)
        {
            if (datos is null)
            {
                throw new EvaluacionInvalidaException(
                    "Las calificaciones no pueden contener elementos nulos.");
            }

            if (!criteriosVistos.Add(datos.CriterioEvaluacionId))
            {
                throw new EvaluacionInvalidaException(
                    "No se permiten calificaciones duplicadas para un mismo criterio.");
            }

            if (!criteriosPorId.TryGetValue(datos.CriterioEvaluacionId, out var criterio))
            {
                throw new CriterioNoPerteneceARubricaException();
            }

            resultado.Add((criterio, datos));
        }

        return resultado;
    }

    private static string? NormalizarComentariosGenerales(string? comentariosGenerales)
    {
        if (string.IsNullOrWhiteSpace(comentariosGenerales))
        {
            return null;
        }

        var normalizados = comentariosGenerales.Trim();
        if (normalizados.Length > LongitudMaximaComentariosGenerales)
        {
            throw new EvaluacionInvalidaException(
                $"Los comentarios generales no pueden superar los {LongitudMaximaComentariosGenerales} caracteres.");
        }

        return normalizados;
    }

    private static void ValidarArgumentosTemporalesYUsuario(
        DateTimeOffset ahora,
        Guid usuarioEvaluadorId)
    {
        if (ahora == default)
        {
            throw new ArgumentException("La fecha de operación es obligatoria.", nameof(ahora));
        }

        if (usuarioEvaluadorId == Guid.Empty)
        {
            throw new ArgumentException(
                "El usuario evaluador es obligatorio.",
                nameof(usuarioEvaluadorId));
        }
    }

    private static void ValidarFechaLimite(DateTimeOffset fechaLimiteEvaluacion)
    {
        if (fechaLimiteEvaluacion == default)
        {
            throw new ArgumentException(
                "La fecha límite de evaluación es obligatoria.",
                nameof(fechaLimiteEvaluacion));
        }
    }
}
