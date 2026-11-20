using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Postulantes.Entidades;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;
using OcupacionEnum = Ascenso.Postulaciones.Domain.Enums.Ocupacion;

namespace Ascenso.Postulaciones.Domain.Postulantes.Agregados;

/// <summary>
/// Raíz de agregado que representa el perfil base de un postulante dentro del
/// proceso de admisión de la Fundación.
/// </summary>
public sealed class Postulante : AggregateRoot
{
    private const int LongitudMinimaDocumento = 5;
    private const int LongitudMaximaDocumento = 30;
    private const int LongitudMinimaNombre = 2;
    private const int LongitudMaximaNombre = 100;
    private const decimal PromedioMinimo = 0m;
    private const decimal PromedioMaximo = 20m;
    private const int DecimalesMaximos = 2;

    private readonly List<MiembroFamiliar> _miembrosFamiliares = [];

    // Constructor requerido por los adaptadores de persistencia.
    private Postulante()
    {
    }

    private Postulante(
        Guid? personaId,
        Guid usuarioId,
        string numeroDocumentoIdentidad,
        string nombres,
        string apellidos,
        DatosContacto datosContacto,
        Guid universidadAliadaId,
        Guid carreraId,
        Guid periodoActualId,
        CondicionAcademicaIngreso condicionAcademica,
        DateTimeOffset fechaRegistro)
        : base(UuidV7.Nuevo(), fechaRegistro, usuarioId)
    {
        PersonaId = personaId;
        UsuarioId = usuarioId;
        NumeroDocumentoIdentidad = numeroDocumentoIdentidad;
        Nombres = nombres;
        Apellidos = apellidos;
        DatosContacto = datosContacto;
        UniversidadAliadaId = universidadAliadaId;
        CarreraId = carreraId;
        PeriodoActualId = periodoActualId;
        CondicionAcademica = condicionAcademica;
        AceptaTratamientoDatos = true;
        EstadoPerfil = EstadoPerfil.Incompleto;
        FechaRegistro = FechaCreacion;
    }

    public Guid? PersonaId { get; private set; }

    public Guid UsuarioId { get; private set; }

    public string NumeroDocumentoIdentidad { get; private set; } = string.Empty;

    public string Nombres { get; private set; } = string.Empty;

    public string Apellidos { get; private set; } = string.Empty;

    public string NombreCompleto => $"{Nombres} {Apellidos}";

    public DatosContacto DatosContacto { get; private set; } = null!;

    public Guid UniversidadAliadaId { get; private set; }

    public Guid CarreraId { get; private set; }

    public Guid PeriodoActualId { get; private set; }

    public CondicionAcademicaIngreso CondicionAcademica { get; private set; }

    public bool AceptaTratamientoDatos { get; private set; }

    public EstadoPerfil EstadoPerfil { get; private set; }

    public DateTimeOffset FechaRegistro { get; private set; }

    public EstadoCivil? EstadoCivil { get; private set; }

    public decimal? PromedioActual { get; private set; }

    public bool? Trabaja { get; private set; }

    public Ocupacion? Ocupacion { get; private set; }

    public decimal? IngresoMensualPostulante { get; private set; }

    public int? NumeroIntegrantesFamilia { get; private set; }

    public decimal? IngresoFamiliarMensualTotal { get; private set; }

    public IReadOnlyCollection<MiembroFamiliar> MiembrosFamiliares => _miembrosFamiliares.AsReadOnly();

    public static Postulante Crear(
        Guid? personaId,
        Guid usuarioId,
        string numeroDocumentoIdentidad,
        string nombres,
        string apellidos,
        DatosContacto datosContacto,
        Guid universidadAliadaId,
        Guid carreraId,
        Guid periodoActualId,
        CondicionAcademicaIngreso condicionAcademica,
        bool aceptaTratamientoDatos,
        DateTimeOffset fechaRegistro)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new NoAutenticadoException();
        }

        if (datosContacto is null)
        {
            throw new DatosPersonalesInvalidosException("Los datos de contacto son obligatorios.");
        }

        if (universidadAliadaId == Guid.Empty)
        {
            throw new ArgumentException("La universidad aliada es obligatoria.", nameof(universidadAliadaId));
        }

        if (carreraId == Guid.Empty)
        {
            throw new ArgumentException("La carrera es obligatoria.", nameof(carreraId));
        }

        if (periodoActualId == Guid.Empty)
        {
            throw new ArgumentException("El periodo académico es obligatorio.", nameof(periodoActualId));
        }

        if (fechaRegistro == default)
        {
            throw new ArgumentException("La fecha de registro es obligatoria.", nameof(fechaRegistro));
        }

        if (!Enum.IsDefined(condicionAcademica))
        {
            throw new CondicionAcademicaInvalidaException();
        }

        if (!aceptaTratamientoDatos)
        {
            throw new ExcepcionReglaNegocio(
                "TRATAMIENTO_DATOS_NO_ACEPTADO",
                "El postulante debe aceptar el tratamiento de sus datos personales.",
                422);
        }

        return new Postulante(
            personaId == Guid.Empty ? null : personaId,
            usuarioId,
            NormalizarDocumento(numeroDocumentoIdentidad),
            NormalizarNombre(nombres, nameof(nombres)),
            NormalizarNombre(apellidos, nameof(apellidos)),
            datosContacto,
            universidadAliadaId,
            carreraId,
            periodoActualId,
            condicionAcademica,
            fechaRegistro.ToUniversalTime());
    }

    public void RegistrarPerfilSocioeconomico(
        EstadoCivil estadoCivil,
        decimal promedioActual,
        bool trabaja,
        Ocupacion ocupacion,
        decimal ingresoMensualPostulante,
        IReadOnlyCollection<DatosMiembroFamiliar> miembrosFamiliares,
        Guid usuarioModificacionId,
        DateTimeOffset fechaModificacion)
    {
        if (!Enum.IsDefined(estadoCivil))
        {
            throw new CatalogoInvalidoException(nameof(estadoCivil));
        }

        if (!Enum.IsDefined(ocupacion))
        {
            throw new CatalogoInvalidoException(nameof(ocupacion));
        }

        ValidarPromedioActual(promedioActual);
        ValidarIngresoMensual(ingresoMensualPostulante, "El ingreso mensual del postulante");
        ValidarCondicionLaboral(trabaja, ocupacion);

        if (miembrosFamiliares is null)
        {
            throw new MiembroFamiliarInvalidoException(
                "La colección de miembros familiares es obligatoria.");
        }

        if (usuarioModificacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El usuario de modificación es obligatorio.",
                nameof(usuarioModificacionId));
        }

        if (fechaModificacion == default)
        {
            throw new ArgumentException(
                "La fecha de modificación es obligatoria.",
                nameof(fechaModificacion));
        }

        var nuevosMiembros = new List<MiembroFamiliar>(miembrosFamiliares.Count);

        foreach (var datosMiembro in miembrosFamiliares)
        {
            if (datosMiembro is null)
            {
                throw new MiembroFamiliarInvalidoException(
                    "Los datos de cada miembro familiar son obligatorios.");
            }

            nuevosMiembros.Add(
                MiembroFamiliar.Crear(Id, datosMiembro, fechaModificacion, usuarioModificacionId));
        }

        EstadoCivil = estadoCivil;
        PromedioActual = promedioActual;
        Trabaja = trabaja;
        Ocupacion = ocupacion;
        IngresoMensualPostulante = ingresoMensualPostulante;

        _miembrosFamiliares.Clear();
        _miembrosFamiliares.AddRange(nuevosMiembros);

        NumeroIntegrantesFamilia = _miembrosFamiliares.Count + 1;
        IngresoFamiliarMensualTotal = decimal.Round(
            ingresoMensualPostulante + nuevosMiembros.Sum(miembro => miembro.IngresoMensual),
            DecimalesMaximos,
            MidpointRounding.AwayFromZero);

        ActualizarAuditoria(usuarioModificacionId, fechaModificacion);
        EstadoPerfil = EstadoPerfil.Completo;
    }

    private static void ValidarPromedioActual(decimal promedioActual)
    {
        if (promedioActual is < PromedioMinimo or > PromedioMaximo)
        {
            throw new DatosSocioeconomicosInvalidosException(
                $"El promedio actual debe estar entre {PromedioMinimo} y {PromedioMaximo}.");
        }

        if (decimal.Round(promedioActual, DecimalesMaximos) != promedioActual)
        {
            throw new DatosSocioeconomicosInvalidosException(
                $"El promedio actual admite como máximo {DecimalesMaximos} decimales.");
        }
    }

    private static void ValidarIngresoMensual(decimal ingresoMensual, string campo)
    {
        if (ingresoMensual < 0m)
        {
            throw new DatosSocioeconomicosInvalidosException(
                $"{campo} no puede ser negativo.");
        }

        if (decimal.Round(ingresoMensual, DecimalesMaximos) != ingresoMensual)
        {
            throw new DatosSocioeconomicosInvalidosException(
                $"{campo} admite como máximo {DecimalesMaximos} decimales.");
        }
    }

    private static void ValidarCondicionLaboral(bool trabaja, Ocupacion ocupacion)
    {
        var esCoherente = trabaja
            ? ocupacion is OcupacionEnum.Empleado or OcupacionEnum.Independiente or OcupacionEnum.Otro
            : ocupacion is OcupacionEnum.Estudiante
                or OcupacionEnum.LaboresHogar
                or OcupacionEnum.Jubilado
                or OcupacionEnum.NoTrabaja
                or OcupacionEnum.Otro;

        if (!esCoherente)
        {
            throw new CondicionLaboralIncoherenteException(
                "La ocupación declarada no es coherente con la condición laboral.");
        }
    }

    private static string NormalizarDocumento(string numeroDocumentoIdentidad)
    {
        if (string.IsNullOrWhiteSpace(numeroDocumentoIdentidad))
        {
            throw new DatosPersonalesInvalidosException(
                "El número de documento de identidad es obligatorio.");
        }

        var documentoNormalizado = numeroDocumentoIdentidad.Trim();

        if (documentoNormalizado.Length is < LongitudMinimaDocumento or > LongitudMaximaDocumento)
        {
            throw new DatosPersonalesInvalidosException(
                $"El número de documento de identidad debe tener entre {LongitudMinimaDocumento} y {LongitudMaximaDocumento} caracteres.");
        }

        return documentoNormalizado;
    }

    private static string NormalizarNombre(string nombre, string nombreParametro)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DatosPersonalesInvalidosException(
                $"El campo '{nombreParametro}' es obligatorio.");
        }

        var nombreNormalizado = nombre.Trim();

        if (nombreNormalizado.Length is < LongitudMinimaNombre or > LongitudMaximaNombre)
        {
            throw new DatosPersonalesInvalidosException(
                $"El campo '{nombreParametro}' debe tener entre {LongitudMinimaNombre} y {LongitudMaximaNombre} caracteres.");
        }

        foreach (var caracter in nombreNormalizado)
        {
            if (!char.IsLetter(caracter) && !char.IsWhiteSpace(caracter) && caracter is not ('\'' or '-'))
            {
                throw new DatosPersonalesInvalidosException(
                    $"El campo '{nombreParametro}' contiene caracteres no permitidos.");
            }
        }

        return nombreNormalizado;
    }
}
