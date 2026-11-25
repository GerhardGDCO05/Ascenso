using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Convocatorias.Entidades;
using Ascenso.Programas.Domain.Convocatorias.Eventos;
using Ascenso.Programas.Domain.Convocatorias.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;

namespace Ascenso.Programas.Domain.Convocatorias.Agregados;

/// <summary>
/// Raíz del agregado de convocatorias. Encapsula el ciclo borrador, publicada y cerrada,
/// los criterios de elegibilidad del ciclo y los documentos requeridos.
/// </summary>
public sealed class Convocatoria : AggregateRoot
{
    private readonly List<DocumentoRequerido> _documentosRequeridos = [];

    private Convocatoria()
    {
    }

    private Convocatoria(
        Guid id,
        Guid programaBecaId,
        string periodoAcademicoId,
        string codigo,
        string nombre,
        DateTimeOffset fechaApertura,
        DateTimeOffset fechaCierre,
        DateTimeOffset fechaLimiteSubsanacionDocumentos,
        DateTimeOffset fechaInicioEvaluacion,
        DateTimeOffset fechaCierreEvaluacion,
        bool requiereRevisionDocumentalPrevia,
        int cuposAsignados,
        CriteriosElegibilidadCiclo criteriosElegibilidad,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
        : base(id, fechaCreacion, usuarioCreacionId)
    {
        ProgramaBecaId = programaBecaId;
        PeriodoAcademicoId = periodoAcademicoId;
        Codigo = codigo;
        Nombre = nombre;
        FechaApertura = fechaApertura;
        FechaCierre = fechaCierre;
        FechaLimiteSubsanacionDocumentos = fechaLimiteSubsanacionDocumentos;
        FechaInicioEvaluacion = fechaInicioEvaluacion;
        FechaCierreEvaluacion = fechaCierreEvaluacion;
        RequiereRevisionDocumentalPrevia = requiereRevisionDocumentalPrevia;
        CuposAsignados = cuposAsignados;
        CriteriosElegibilidad = criteriosElegibilidad;
        Estado = EstadoConvocatoria.Borrador;
    }

    public Guid ProgramaBecaId { get; private set; }

    public string PeriodoAcademicoId { get; private set; } = string.Empty;

    public string Codigo { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public DateTimeOffset FechaApertura { get; private set; }

    public DateTimeOffset FechaCierre { get; private set; }

    public DateTimeOffset FechaLimiteSubsanacionDocumentos { get; private set; }

    public DateTimeOffset FechaInicioEvaluacion { get; private set; }

    public DateTimeOffset FechaCierreEvaluacion { get; private set; }

    public bool RequiereRevisionDocumentalPrevia { get; private set; }

    public int CuposAsignados { get; private set; }

    public CriteriosElegibilidadCiclo CriteriosElegibilidad { get; private set; } = null!;

    public EstadoConvocatoria Estado { get; private set; }

    public DateTimeOffset? FechaPublicacion { get; private set; }

    public IReadOnlyCollection<DocumentoRequerido> DocumentosRequeridos => _documentosRequeridos.AsReadOnly();

    public static Convocatoria CrearBorrador(
        Guid programaBecaId,
        string periodoAcademicoId,
        string codigo,
        string nombre,
        DateTimeOffset fechaApertura,
        DateTimeOffset fechaCierre,
        DateTimeOffset fechaLimiteSubsanacionDocumentos,
        DateTimeOffset fechaInicioEvaluacion,
        DateTimeOffset fechaCierreEvaluacion,
        bool requiereRevisionDocumentalPrevia,
        int cuposAsignados,
        CriteriosElegibilidadCiclo criteriosElegibilidad,
        Guid usuarioCreacionId,
        DateTimeOffset? fechaCreacion = null)
    {
        if (programaBecaId == Guid.Empty)
        {
            throw new ArgumentException("El programa de becas de la convocatoria es obligatorio.", nameof(programaBecaId));
        }

        if (string.IsNullOrWhiteSpace(periodoAcademicoId))
        {
            throw new ArgumentException("El periodo académico de la convocatoria es obligatorio.", nameof(periodoAcademicoId));
        }

        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException("El código de la convocatoria es obligatorio.", nameof(codigo));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre de la convocatoria es obligatorio.", nameof(nombre));
        }

        if (cuposAsignados < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cuposAsignados), "Los cupos asignados deben ser mayores o iguales a 1.");
        }

        ArgumentNullException.ThrowIfNull(criteriosElegibilidad);

        ValidarCronologia(
            fechaApertura,
            fechaCierre,
            fechaLimiteSubsanacionDocumentos,
            fechaInicioEvaluacion,
            fechaCierreEvaluacion,
            requiereRevisionDocumentalPrevia);

        var fechaEfectiva = (fechaCreacion ?? DateTimeOffset.UtcNow).ToUniversalTime();

        return new Convocatoria(
            UuidV7.Nuevo(fechaEfectiva),
            programaBecaId,
            periodoAcademicoId.Trim(),
            codigo.Trim().ToUpperInvariant(),
            nombre.Trim(),
            fechaApertura.ToUniversalTime(),
            fechaCierre.ToUniversalTime(),
            fechaLimiteSubsanacionDocumentos.ToUniversalTime(),
            fechaInicioEvaluacion.ToUniversalTime(),
            fechaCierreEvaluacion.ToUniversalTime(),
            requiereRevisionDocumentalPrevia,
            cuposAsignados,
            criteriosElegibilidad,
            fechaEfectiva,
            usuarioCreacionId);
    }

    public void Actualizar(
        string periodoAcademicoId,
        string nombre,
        DateTimeOffset fechaApertura,
        DateTimeOffset fechaCierre,
        DateTimeOffset fechaLimiteSubsanacionDocumentos,
        DateTimeOffset fechaInicioEvaluacion,
        DateTimeOffset fechaCierreEvaluacion,
        bool requiereRevisionDocumentalPrevia,
        int cuposAsignados,
        CriteriosElegibilidadCiclo criteriosElegibilidad,
        Guid usuarioModificacionId,
        DateTimeOffset? fechaModificacion = null)
    {
        AsegurarBorrador();

        ValidarUsuario(usuarioModificacionId, nameof(usuarioModificacionId));
        var fechaEfectiva = NormalizarInstante(
            fechaModificacion ?? DateTimeOffset.UtcNow,
            nameof(fechaModificacion));

        if (string.IsNullOrWhiteSpace(periodoAcademicoId))
        {
            throw new ArgumentException("El periodo académico de la convocatoria es obligatorio.", nameof(periodoAcademicoId));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre de la convocatoria es obligatorio.", nameof(nombre));
        }

        if (cuposAsignados < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cuposAsignados), "Los cupos asignados deben ser mayores o iguales a 1.");
        }

        ArgumentNullException.ThrowIfNull(criteriosElegibilidad);

        ValidarCronologia(
            fechaApertura,
            fechaCierre,
            fechaLimiteSubsanacionDocumentos,
            fechaInicioEvaluacion,
            fechaCierreEvaluacion,
            requiereRevisionDocumentalPrevia);

        PeriodoAcademicoId = periodoAcademicoId.Trim();
        Nombre = nombre.Trim();
        FechaApertura = fechaApertura.ToUniversalTime();
        FechaCierre = fechaCierre.ToUniversalTime();
        FechaLimiteSubsanacionDocumentos = fechaLimiteSubsanacionDocumentos.ToUniversalTime();
        FechaInicioEvaluacion = fechaInicioEvaluacion.ToUniversalTime();
        FechaCierreEvaluacion = fechaCierreEvaluacion.ToUniversalTime();
        RequiereRevisionDocumentalPrevia = requiereRevisionDocumentalPrevia;
        CuposAsignados = cuposAsignados;
        CriteriosElegibilidad = criteriosElegibilidad;

        ActualizarAuditoria(usuarioModificacionId, fechaEfectiva);
    }

    public DocumentoRequerido AgregarDocumento(
        string tipoDocumento,
        string nombre,
        string? descripcion,
        bool obligatorio,
        IReadOnlyCollection<string> tiposMimePermitidos,
        int tamanioMaximoMb,
        Guid usuarioModificacionId,
        DateTimeOffset? fechaModificacion = null)
    {
        AsegurarBorrador();

        ValidarUsuario(usuarioModificacionId, nameof(usuarioModificacionId));
        var fechaEfectiva = NormalizarInstante(
            fechaModificacion ?? DateTimeOffset.UtcNow,
            nameof(fechaModificacion));
        var documento = DocumentoRequerido.Crear(
            Id,
            tipoDocumento,
            nombre,
            descripcion,
            obligatorio,
            tiposMimePermitidos,
            tamanioMaximoMb,
            usuarioModificacionId,
            fechaEfectiva);

        _documentosRequeridos.Add(documento);
        ActualizarAuditoria(usuarioModificacionId, fechaEfectiva);

        return documento;
    }

    public void QuitarDocumento(
        Guid documentoId,
        Guid usuarioModificacionId,
        DateTimeOffset? fechaModificacion = null)
    {
        AsegurarBorrador();
        ValidarUsuario(usuarioModificacionId, nameof(usuarioModificacionId));
        var fechaEfectiva = NormalizarInstante(
            fechaModificacion ?? DateTimeOffset.UtcNow,
            nameof(fechaModificacion));

        var documento = _documentosRequeridos.SingleOrDefault(item => item.Id == documentoId)
            ?? throw new DocumentoRequeridoNoEncontradoException(Id, documentoId);

        _documentosRequeridos.Remove(documento);
        ActualizarAuditoria(usuarioModificacionId, fechaEfectiva);
    }

    public void Publicar(
        DateTimeOffset instanteActual,
        Guid usuarioModificacionId,
        Guid? correlacionId = null)
    {
        if (Estado != EstadoConvocatoria.Borrador)
        {
            throw new ConvocatoriaNoEditableException(Id, Estado);
        }

        if (_documentosRequeridos.Count == 0)
        {
            throw new ConvocatoriaSinDocumentosException(Id);
        }

        var ahoraUtc = NormalizarInstante(instanteActual, nameof(instanteActual));
        if (FechaCierre <= ahoraUtc)
        {
            throw new ConvocatoriaFechaCierreVencidaException(Id, FechaCierre);
        }

        ValidarUsuario(usuarioModificacionId, nameof(usuarioModificacionId));
        Estado = EstadoConvocatoria.Publicada;
        FechaPublicacion = ahoraUtc;
        ActualizarAuditoria(usuarioModificacionId, ahoraUtc);

        var correlacion = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(ahoraUtc);

        RegistrarEventoDominio(new ConvocatoriaPublicadaDominioEvent(
            Id,
            ProgramaBecaId,
            Codigo,
            FechaApertura,
            FechaCierre,
            CuposAsignados,
            correlacion,
            ahoraUtc));
    }

    /// <summary>
    /// Cierra la convocatoria si está publicada. Es idempotente: una convocatoria
    /// cerrada o cancelada no se modifica.
    /// </summary>
    public bool Cerrar(
        DateTimeOffset instanteCierre,
        Guid usuarioModificacionId,
        Guid? correlacionId = null)
    {
        if (Estado != EstadoConvocatoria.Publicada)
        {
            return false;
        }

        var fechaUtc = NormalizarInstante(instanteCierre, nameof(instanteCierre));
        if (fechaUtc < FechaCierre)
        {
            return false;
        }

        ValidarUsuario(usuarioModificacionId, nameof(usuarioModificacionId));
        Estado = EstadoConvocatoria.Cerrada;
        ActualizarAuditoria(usuarioModificacionId, fechaUtc);

        var correlacion = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaUtc);

        RegistrarEventoDominio(new ConvocatoriaCerradaDominioEvent(
            Id,
            ProgramaBecaId,
            Codigo,
            FechaCierre,
            correlacion,
            fechaUtc));

        return true;
    }

    private void AsegurarBorrador()
    {
        if (Estado != EstadoConvocatoria.Borrador)
        {
            throw new ConvocatoriaNoEditableException(Id, Estado);
        }
    }

    private static void ValidarUsuario(Guid usuarioId, string nombreParametro)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("El usuario de auditoría es obligatorio.", nombreParametro);
        }
    }

    private static DateTimeOffset NormalizarInstante(DateTimeOffset instante, string nombreParametro)
    {
        if (instante == default)
        {
            throw new ArgumentException("El instante de auditoría es obligatorio.", nombreParametro);
        }

        return instante.ToUniversalTime();
    }

    private static void ValidarCronologia(
        DateTimeOffset fechaApertura,
        DateTimeOffset fechaCierre,
        DateTimeOffset fechaLimiteSubsanacionDocumentos,
        DateTimeOffset fechaInicioEvaluacion,
        DateTimeOffset fechaCierreEvaluacion,
        bool requiereRevisionDocumentalPrevia)
    {
        if (fechaCierre <= fechaApertura)
        {
            throw new FechasConvocatoriaInvalidasException("la fecha de cierre debe ser posterior a la fecha de apertura.");
        }

        if (fechaLimiteSubsanacionDocumentos < fechaCierre)
        {
            throw new FechasConvocatoriaInvalidasException("el límite de subsanación no puede ser anterior al cierre.");
        }

        if (fechaCierreEvaluacion <= fechaInicioEvaluacion)
        {
            throw new FechasConvocatoriaInvalidasException("el cierre de evaluación debe ser posterior al inicio de evaluación.");
        }

        if (fechaCierreEvaluacion <= fechaLimiteSubsanacionDocumentos)
        {
            throw new FechasConvocatoriaInvalidasException("el cierre de evaluación debe ser posterior al límite de subsanación.");
        }

        if (requiereRevisionDocumentalPrevia && fechaInicioEvaluacion < fechaCierre)
        {
            throw new FechasConvocatoriaInvalidasException(
                "con revisión documental previa, la evaluación no puede iniciar antes del cierre de la convocatoria.");
        }

        if (!requiereRevisionDocumentalPrevia && fechaInicioEvaluacion < fechaApertura)
        {
            throw new FechasConvocatoriaInvalidasException(
                "sin revisión documental previa, la evaluación no puede iniciar antes de la apertura.");
        }
    }
}
