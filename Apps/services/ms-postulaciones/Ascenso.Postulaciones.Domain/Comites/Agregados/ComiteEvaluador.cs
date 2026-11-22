using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Comites.Agregados;

/// <summary>
/// Raíz de agregado que representa el comité evaluador conformado para un programa
/// y convocatoria de beca.
/// </summary>
public sealed class ComiteEvaluador : AggregateRoot
{
    public const int LongitudMaximaNombre = 200;

    private readonly List<MiembroComite> _miembros = [];

    // Constructor requerido por los adaptadores de persistencia.
    private ComiteEvaluador()
    {
    }

    private ComiteEvaluador(
        Guid programaBecaId,
        Guid convocatoriaId,
        string nombre,
        Guid jefaturaUsuarioId,
        DateTimeOffset fechaCreacion,
        Guid usuarioConformaId)
        : base(UuidV7.Nuevo(), fechaCreacion, usuarioConformaId)
    {
        ProgramaBecaId = programaBecaId;
        ConvocatoriaId = convocatoriaId;
        Nombre = nombre;
        JefaturaUsuarioId = jefaturaUsuarioId;
        FechaConformacion = DateOnly.FromDateTime(FechaCreacion.UtcDateTime);
        ConformadoPorUsuarioId = usuarioConformaId;
        Activo = true;

        var miembroJefatura = MiembroComite.Crear(
            Id,
            jefaturaUsuarioId,
            TipoEvaluador.Interno,
            RolMiembroComite.Jefatura,
            FechaCreacion,
            usuarioConformaId);

        _miembros.Add(miembroJefatura);
    }

    public Guid ProgramaBecaId { get; private set; }

    public Guid ConvocatoriaId { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public Guid JefaturaUsuarioId { get; private set; }

    public DateOnly FechaConformacion { get; private set; }

    public Guid ConformadoPorUsuarioId { get; private set; }

    public bool Activo { get; private set; }

    public IReadOnlyCollection<MiembroComite> Miembros => _miembros.AsReadOnly();

    public static ComiteEvaluador Crear(
        Guid programaBecaId,
        Guid convocatoriaId,
        string nombre,
        Guid jefaturaUsuarioId,
        DateTimeOffset fechaCreacion,
        Guid usuarioConformaId)
    {
        if (programaBecaId == Guid.Empty)
        {
            throw new ComiteEvaluadorInvalidoException(
                "El identificador del programa de beca es obligatorio.");
        }

        if (convocatoriaId == Guid.Empty)
        {
            throw new ComiteEvaluadorInvalidoException(
                "El identificador de la convocatoria es obligatorio.");
        }

        if (jefaturaUsuarioId == Guid.Empty)
        {
            throw new ComiteEvaluadorInvalidoException(
                "El identificador de la jefatura del comité es obligatorio.");
        }

        var nombreNormalizado = NormalizarNombre(nombre);

        return new ComiteEvaluador(
            programaBecaId,
            convocatoriaId,
            nombreNormalizado,
            jefaturaUsuarioId,
            fechaCreacion,
            usuarioConformaId);
    }

    public MiembroComite AgregarMiembro(
        Guid usuarioEvaluadorId,
        TipoEvaluador tipo,
        DateTimeOffset fechaAsignacion,
        Guid usuarioModificacionId)
    {
        if (usuarioEvaluadorId == Guid.Empty)
        {
            throw new ComiteEvaluadorInvalidoException(
                "El identificador del usuario evaluador es obligatorio.");
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipo),
                tipo,
                "El tipo de evaluador no es válido.");
        }

        if (fechaAsignacion == default)
        {
            throw new ArgumentException(
                "La fecha de asignación es obligatoria.",
                nameof(fechaAsignacion));
        }

        if (usuarioModificacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El usuario de modificación es obligatorio.",
                nameof(usuarioModificacionId));
        }

        if (_miembros.Any(miembro => miembro.Activo && miembro.UsuarioEvaluadorId == usuarioEvaluadorId))
        {
            throw new MiembroComiteDuplicadoException();
        }

        var miembro = MiembroComite.Crear(
            Id,
            usuarioEvaluadorId,
            tipo,
            RolMiembroComite.Evaluador,
            fechaAsignacion,
            usuarioModificacionId);

        ActualizarAuditoria(usuarioModificacionId, fechaAsignacion);
        _miembros.Add(miembro);

        return miembro;
    }

    public bool DesvincularMiembro(
        Guid miembroId,
        DateTimeOffset fechaDesvinculacion,
        Guid usuarioModificacionId)
    {
        if (fechaDesvinculacion == default)
        {
            throw new ArgumentException(
                "La fecha de desvinculación es obligatoria.",
                nameof(fechaDesvinculacion));
        }

        if (usuarioModificacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El usuario de modificación es obligatorio.",
                nameof(usuarioModificacionId));
        }

        var miembro = _miembros.FirstOrDefault(item => item.Id == miembroId)
            ?? throw new MiembroComiteNoEncontradoException();

        if (miembro.Rol == RolMiembroComite.Jefatura)
        {
            throw new JefaturaNoDesvinculableException();
        }

        var desvinculado = miembro.Desvincular(fechaDesvinculacion, usuarioModificacionId);
        if (!desvinculado)
        {
            return false;
        }

        ActualizarAuditoria(usuarioModificacionId, fechaDesvinculacion);

        return true;
    }

    private static string NormalizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ComiteEvaluadorInvalidoException(
                "El nombre del comité evaluador es obligatorio.");
        }

        var nombreNormalizado = nombre.Trim();

        if (nombreNormalizado.Length > LongitudMaximaNombre)
        {
            throw new ComiteEvaluadorInvalidoException(
                $"El nombre del comité evaluador no puede superar los {LongitudMaximaNombre} caracteres.");
        }

        return nombreNormalizado;
    }
}
