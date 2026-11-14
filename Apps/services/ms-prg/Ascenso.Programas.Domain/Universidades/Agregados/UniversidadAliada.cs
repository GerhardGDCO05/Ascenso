using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Universidades.Entidades;
using Ascenso.Programas.Domain.Universidades.Eventos;
using Ascenso.Programas.Domain.Universidades.ObjetosValor;

namespace Ascenso.Programas.Domain.Universidades.Agregados;

/// <summary>
/// Raíz del agregado de Universidades Aliadas, Convenios, Carreras Elegibles y Costos de Matrícula.
/// </summary>
public sealed class UniversidadAliada : AggregateRoot
{
    private readonly List<Convenio> _convenios = [];
    private readonly List<CarreraElegible> _carreras = [];

    private UniversidadAliada()
    {
    }

    private UniversidadAliada(
        Guid id,
        string codigoInstitucional,
        string nombre,
        string direccion,
        ContactoEnlace contactoEnlace,
        CuentaBancariaInstitucional? cuentaBancariaInstitucional,
        Guid? usuarioEnlaceId,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
        : base(id, fechaCreacion, usuarioCreacionId)
    {
        CodigoInstitucional = codigoInstitucional;
        Nombre = nombre;
        Direccion = direccion;
        ContactoEnlace = contactoEnlace;
        CuentaBancariaInstitucional = cuentaBancariaInstitucional;
        UsuarioEnlaceId = usuarioEnlaceId;
        Estado = EstadoUniversidad.Activa;
    }

    public string CodigoInstitucional { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public string Direccion { get; private set; } = string.Empty;

    public ContactoEnlace ContactoEnlace { get; private set; } = null!;

    public CuentaBancariaInstitucional? CuentaBancariaInstitucional { get; private set; }

    public Guid? UsuarioEnlaceId { get; private set; }

    public EstadoUniversidad Estado { get; private set; }

    public IReadOnlyCollection<Convenio> Convenios => _convenios.AsReadOnly();

    public IReadOnlyCollection<CarreraElegible> Carreras => _carreras.AsReadOnly();

    public static UniversidadAliada CrearConConvenioInicial(
        string codigoInstitucional,
        string nombre,
        string direccion,
        ContactoEnlace contactoEnlace,
        CuentaBancariaInstitucional? cuentaBancariaInstitucional,
        Guid? usuarioEnlaceId,
        string numeroConvenio,
        string terminosResumen,
        DateOnly fechaInicioConvenio,
        DateOnly fechaFinConvenio,
        string urlDocumentoPdfFirebase,
        bool permiteIntercambioAutomaticoDatos,
        Guid usuarioCreacionId,
        DateTimeOffset? fechaCreacion = null,
        Guid? correlacionId = null)
    {
        if (string.IsNullOrWhiteSpace(codigoInstitucional))
        {
            throw new ArgumentException("El código institucional de la universidad es obligatorio.", nameof(codigoInstitucional));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre de la universidad aliada es obligatorio.", nameof(nombre));
        }

        if (string.IsNullOrWhiteSpace(direccion))
        {
            throw new ArgumentException("La dirección de la universidad aliada es obligatoria.", nameof(direccion));
        }

        ArgumentNullException.ThrowIfNull(contactoEnlace);

        if (usuarioEnlaceId.HasValue && usuarioEnlaceId.Value == Guid.Empty)
        {
            throw new ArgumentException("El identificador de usuario enlace no puede ser vacío.", nameof(usuarioEnlaceId));
        }

        var fechaEfectiva = (fechaCreacion ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var correlacionEfectiva = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaEfectiva);

        var universidad = new UniversidadAliada(
            UuidV7.Nuevo(fechaEfectiva),
            codigoInstitucional.Trim().ToUpperInvariant(),
            nombre.Trim(),
            direccion.Trim(),
            contactoEnlace,
            cuentaBancariaInstitucional,
            usuarioEnlaceId,
            fechaEfectiva,
            usuarioCreacionId);

        var convenioInicial = Convenio.CrearVigente(
            universidad.Id,
            numeroConvenio,
            terminosResumen,
            fechaInicioConvenio,
            fechaFinConvenio,
            urlDocumentoPdfFirebase,
            permiteIntercambioAutomaticoDatos,
            usuarioCreacionId,
            fechaEfectiva);

        universidad._convenios.Add(convenioInicial);

        universidad.RegistrarEventoDominio(new ConvenioUniversidadRegistradoDominioEvent(
            universidad.Id,
            convenioInicial.Id,
            universidad.CodigoInstitucional,
            convenioInicial.NumeroConvenio,
            convenioInicial.FechaInicio,
            convenioInicial.FechaFin,
            correlacionEfectiva,
            fechaEfectiva));

        return universidad;
    }

    public Convenio RegistrarNuevoConvenio(
        string numeroConvenio,
        string terminosResumen,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        string urlDocumentoPdfFirebase,
        bool permiteIntercambioAutomaticoDatos,
        Guid usuarioModificacionId,
        DateTimeOffset? fechaRegistro = null,
        Guid? correlacionId = null)
    {
        var fechaEfectiva = (fechaRegistro ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var correlacionEfectiva = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaEfectiva);

        var convenio = Convenio.CrearVigente(
            Id,
            numeroConvenio,
            terminosResumen,
            fechaInicio,
            fechaFin,
            urlDocumentoPdfFirebase,
            permiteIntercambioAutomaticoDatos,
            usuarioModificacionId,
            fechaEfectiva);

        _convenios.Add(convenio);
        ActualizarAuditoria(usuarioModificacionId, fechaEfectiva);

        RegistrarEventoDominio(new ConvenioUniversidadRegistradoDominioEvent(
            Id,
            convenio.Id,
            CodigoInstitucional,
            convenio.NumeroConvenio,
            convenio.FechaInicio,
            convenio.FechaFin,
            correlacionEfectiva,
            fechaEfectiva));

        return convenio;
    }

    public Convenio MarcarConvenioVencido(
        Guid convenioId,
        Guid usuarioModificacionId,
        DateTimeOffset? fechaModificacion = null,
        Guid? correlacionId = null)
    {
        var convenio = _convenios.SingleOrDefault(item => item.Id == convenioId)
            ?? throw new ConvenioNoEncontradoException(convenioId);

        var fechaEfectiva = (fechaModificacion ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var correlacionEfectiva = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaEfectiva);

        convenio.MarcarVencido(usuarioModificacionId, fechaEfectiva);
        ActualizarAuditoria(usuarioModificacionId, fechaEfectiva);

        var mantieneVigente = TieneConvenioVigente(DateOnly.FromDateTime(fechaEfectiva.UtcDateTime));

        RegistrarEventoDominio(new ConvenioUniversidadVencidoDominioEvent(
            Id,
            convenio.Id,
            convenio.NumeroConvenio,
            mantieneVigente,
            correlacionEfectiva,
            fechaEfectiva));

        return convenio;
    }

    public bool TieneConvenioVigente(DateOnly? fechaReferencia = null)
    {
        if (Estado != EstadoUniversidad.Activa)
        {
            return false;
        }

        var fecha = fechaReferencia ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return _convenios.Any(convenio => convenio.EstaVigenteEn(fecha));
    }

    public void ValidarConvenioVigenteParaNuevasPostulaciones(DateOnly? fechaReferencia = null)
    {
        if (!TieneConvenioVigente(fechaReferencia))
        {
            throw new ConvenioUniversidadVencidoException(Id);
        }
    }

    public CarreraElegible AgregarCarreraElegible(
        string codigoCarrera,
        string nombre,
        int duracionSemestres,
        Guid usuarioModificacionId,
        DateTimeOffset? fechaModificacion = null)
    {
        if (string.IsNullOrWhiteSpace(codigoCarrera))
        {
            throw new ArgumentException("El código de la carrera es obligatorio.", nameof(codigoCarrera));
        }

        var codigoNormalizado = codigoCarrera.Trim().ToUpperInvariant();
        if (_carreras.Any(carrera => string.Equals(carrera.CodigoCarrera, codigoNormalizado, StringComparison.Ordinal)))
        {
            throw new CodigoCarreraDuplicadoException(Id, codigoNormalizado);
        }

        var fechaEfectiva = (fechaModificacion ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var nuevaCarrera = CarreraElegible.Crear(
            Id,
            codigoNormalizado,
            nombre,
            duracionSemestres,
            usuarioModificacionId,
            fechaEfectiva);

        _carreras.Add(nuevaCarrera);
        ActualizarAuditoria(usuarioModificacionId, fechaEfectiva);

        return nuevaCarrera;
    }

    public CostoMatriculaReferencia ActualizarCostoMatricula(
        Guid carreraId,
        string periodoAcademicoId,
        decimal montoReferenciaMatricula,
        string moneda,
        Guid usuarioModificacionId,
        DateTimeOffset? fechaVigencia = null,
        Guid? correlacionId = null)
    {
        var carrera = _carreras.SingleOrDefault(item => item.Id == carreraId)
            ?? throw new CarreraElegibleNoEncontradaException(carreraId);

        var fechaEfectiva = (fechaVigencia ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var correlacionEfectiva = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaEfectiva);

        var nuevoCosto = carrera.PublicarNuevoCosto(
            periodoAcademicoId,
            montoReferenciaMatricula,
            moneda,
            usuarioModificacionId,
            fechaEfectiva);

        ActualizarAuditoria(usuarioModificacionId, fechaEfectiva);

        RegistrarEventoDominio(new CostoMatriculaActualizadoDominioEvent(
            Id,
            carrera.Id,
            nuevoCosto.Id,
            nuevoCosto.PeriodoAcademicoId,
            nuevoCosto.NumeroVersion,
            nuevoCosto.MontoReferenciaMatricula,
            nuevoCosto.Moneda,
            correlacionEfectiva,
            fechaEfectiva));

        return nuevoCosto;
    }
}
