using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Programas.Entidades;
using Ascenso.Programas.Domain.Programas.Eventos;
using Ascenso.Programas.Domain.Programas.ObjetosValor;

namespace Ascenso.Programas.Domain.Programas.Agregados;

/// <summary>
/// Raíz del agregado de Programas de Becas, composición de políticas/criterios y asignaciones presupuestarias.
/// </summary>
public sealed class ProgramaBeca : AggregateRoot
{
    private readonly List<AsignacionFondoPrograma> _asignacionesFondo = [];

    private ProgramaBeca()
    {
    }

    private ProgramaBeca(
        Guid id,
        string codigo,
        string nombre,
        string descripcion,
        PoliticaCobertura politicaCobertura,
        int cupoTotal,
        int duracionPeriodos,
        string periodoInicialId,
        DateOnly vigenciaDesde,
        DateOnly vigenciaHasta,
        bool permiteAcumulacion,
        bool requiereCompromisoFirmado,
        string? terminosCompromisoEspecial,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
        : base(id, fechaCreacion, usuarioCreacionId)
    {
        Codigo = codigo;
        Nombre = nombre;
        Descripcion = descripcion;
        PoliticaCobertura = politicaCobertura;
        CupoTotal = cupoTotal;
        CuposOcupados = 0;
        DuracionPeriodos = duracionPeriodos;
        PeriodoInicialId = periodoInicialId;
        VigenciaDesde = vigenciaDesde;
        VigenciaHasta = vigenciaHasta;
        PermiteAcumulacion = permiteAcumulacion;
        RequiereCompromisoFirmado = requiereCompromisoFirmado;
        TerminosCompromisoEspecial = terminosCompromisoEspecial;
        PresupuestoTotalAsignado = 0.00m;
        Moneda = "USD";
        Estado = EstadoProgramaBeca.Borrador;
    }

    public string Codigo { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public string Descripcion { get; private set; } = string.Empty;

    public PoliticaCobertura PoliticaCobertura { get; private set; } = null!;

    public int CupoTotal { get; private set; }

    public int CuposOcupados { get; private set; }

    public int DuracionPeriodos { get; private set; }

    public string PeriodoInicialId { get; private set; } = string.Empty;

    public DateOnly VigenciaDesde { get; private set; }

    public DateOnly VigenciaHasta { get; private set; }

    public bool PermiteAcumulacion { get; private set; }

    public bool RequiereCompromisoFirmado { get; private set; }

    public string? TerminosCompromisoEspecial { get; private set; }

    public decimal PresupuestoTotalAsignado { get; private set; }

    public string Moneda { get; private set; } = "USD";

    public EstadoProgramaBeca Estado { get; private set; }

    public CriteriosElegibilidadPrograma? CriteriosElegibilidad { get; private set; }

    public CondicionesPermanencia? CondicionesPermanencia { get; private set; }

    public IReadOnlyCollection<AsignacionFondoPrograma> AsignacionesFondo => _asignacionesFondo.AsReadOnly();

    public static ProgramaBeca Crear(
        string codigo,
        string nombre,
        string descripcion,
        PoliticaCobertura politicaCobertura,
        int cupoTotal,
        int duracionPeriodos,
        string periodoInicialId,
        DateOnly vigenciaDesde,
        DateOnly vigenciaHasta,
        bool permiteAcumulacion,
        bool requiereCompromisoFirmado,
        string? terminosCompromisoEspecial,
        Guid usuarioCreacionId,
        DateTimeOffset? fechaCreacion = null,
        Guid? correlacionId = null)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException("El código del programa es obligatorio.", nameof(codigo));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del programa es obligatorio.", nameof(nombre));
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentException("La descripción del programa es obligatoria.", nameof(descripcion));
        }

        ArgumentNullException.ThrowIfNull(politicaCobertura);

        if (cupoTotal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cupoTotal), "El cupo total debe ser mayor que cero.");
        }

        if (duracionPeriodos <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(duracionPeriodos), "La duración en períodos debe ser mayor que cero.");
        }

        if (string.IsNullOrWhiteSpace(periodoInicialId))
        {
            throw new ArgumentException("El período académico inicial es obligatorio.", nameof(periodoInicialId));
        }

        if (vigenciaHasta <= vigenciaDesde)
        {
            throw new FechasVigenciaProgramaInvalidasException(vigenciaDesde, vigenciaHasta);
        }

        var fechaEfectiva = (fechaCreacion ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var correlacionEfectiva = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaEfectiva);

        var codigoNormalizado = codigo.Trim().ToUpperInvariant();
        var terminosNormalizados = string.IsNullOrWhiteSpace(terminosCompromisoEspecial)
            ? null
            : terminosCompromisoEspecial.Trim();

        var programa = new ProgramaBeca(
            UuidV7.Nuevo(fechaEfectiva),
            codigoNormalizado,
            nombre.Trim(),
            descripcion.Trim(),
            politicaCobertura,
            cupoTotal,
            duracionPeriodos,
            periodoInicialId.Trim(),
            vigenciaDesde,
            vigenciaHasta,
            permiteAcumulacion,
            requiereCompromisoFirmado,
            terminosNormalizados,
            fechaEfectiva,
            usuarioCreacionId);

        programa.RegistrarEventoDominio(new ProgramaBecaCreadoDominioEvent(
            programa.Id,
            programa.Codigo,
            programa.Nombre,
            programa.PoliticaCobertura.Modalidad,
            programa.CupoTotal,
            programa.PeriodoInicialId,
            correlacionEfectiva,
            fechaEfectiva));

        return programa;
    }

    public AsignacionFondoPrograma AsignarFondoDonante(
        Guid donanteId,
        string periodoAcademicoId,
        decimal montoAsignado,
        Guid autorizadoPorUsuarioId,
        DateTimeOffset? fechaAsignacion = null,
        Guid? correlacionId = null)
    {
        if (Estado is not (EstadoProgramaBeca.Borrador or EstadoProgramaBeca.Configurado))
        {
            throw new PresupuestoEnEjecucionException(Id);
        }

        var fechaEfectiva = (fechaAsignacion ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var correlacionEfectiva = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaEfectiva);

        var asignacion = AsignacionFondoPrograma.CrearAsignada(
            Id,
            donanteId,
            periodoAcademicoId,
            montoAsignado,
            autorizadoPorUsuarioId,
            fechaEfectiva);

        _asignacionesFondo.Add(asignacion);
        PresupuestoTotalAsignado = decimal.Round(
            PresupuestoTotalAsignado + asignacion.MontoAsignado,
            2,
            MidpointRounding.AwayFromZero);

        ActualizarAuditoria(autorizadoPorUsuarioId, fechaEfectiva);

        RegistrarEventoDominio(new PresupuestoProgramaAsignadoDominioEvent(
            Id,
            asignacion.Id,
            asignacion.DonanteId,
            asignacion.PeriodoAcademicoId,
            asignacion.MontoAsignado,
            PresupuestoTotalAsignado,
            correlacionEfectiva,
            fechaEfectiva));

        return asignacion;
    }

    public (Guid DonanteId, decimal MontoRestituido) ReajustarAsignacionFondo(
        Guid asignacionId,
        decimal montoAReducir,
        Guid usuarioModificacionId,
        DateTimeOffset? fechaModificacion = null)
    {
        if (Estado is not (EstadoProgramaBeca.Borrador or EstadoProgramaBeca.Configurado))
        {
            throw new PresupuestoEnEjecucionException(Id);
        }

        var asignacion = _asignacionesFondo.SingleOrDefault(item => item.Id == asignacionId)
            ?? throw new AsignacionFondoNoEncontradaException(asignacionId);

        var fechaEfectiva = (fechaModificacion ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var montoReducido = asignacion.ReducirODesasignar(montoAReducir, usuarioModificacionId, fechaEfectiva);

        PresupuestoTotalAsignado = decimal.Round(
            PresupuestoTotalAsignado - montoReducido,
            2,
            MidpointRounding.AwayFromZero);

        if (PresupuestoTotalAsignado <= 0.00m && Estado == EstadoProgramaBeca.Configurado)
        {
            Estado = EstadoProgramaBeca.Borrador;
        }

        ActualizarAuditoria(usuarioModificacionId, fechaEfectiva);

        return (asignacion.DonanteId, montoReducido);
    }

    public void ConfigurarElegibilidadYPermanencia(
        CriteriosElegibilidadPrograma criteriosElegibilidad,
        CondicionesPermanencia condicionesPermanencia,
        bool todasLasUniversidadesTienenConvenioVigente,
        bool todasLasCarrerasSonElegibles,
        Guid usuarioModificacionId,
        DateTimeOffset? fechaConfiguracion = null,
        Guid? correlacionId = null)
    {
        ArgumentNullException.ThrowIfNull(criteriosElegibilidad);
        ArgumentNullException.ThrowIfNull(condicionesPermanencia);

        if (Estado is not (EstadoProgramaBeca.Borrador or EstadoProgramaBeca.Configurado))
        {
            throw new ProgramaNoConfigurableException(
                Id,
                $"El estado actual '{Estado}' no permite configurar elegibilidad.");
        }

        if (PresupuestoTotalAsignado <= 0.00m)
        {
            throw new ProgramaNoConfigurableException(
                Id,
                "El presupuesto total asignado debe ser mayor que 0.00 USD.");
        }

        if (CupoTotal <= 0)
        {
            throw new ProgramaNoConfigurableException(
                Id,
                "El cupo total del programa debe ser mayor que 0.");
        }

        if (!todasLasUniversidadesTienenConvenioVigente)
        {
            throw new ProgramaNoConfigurableException(
                Id,
                "Todas las universidades elegibles deben existir, estar activas y poseer un convenio vigente.");
        }

        if (!todasLasCarrerasSonElegibles)
        {
            throw new ProgramaNoConfigurableException(
                Id,
                "Todas las carreras elegibles deben pertenecer y estar activas en las universidades seleccionadas.");
        }

        var fechaEfectiva = (fechaConfiguracion ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var correlacionEfectiva = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaEfectiva);

        CriteriosElegibilidad = criteriosElegibilidad;
        CondicionesPermanencia = condicionesPermanencia;
        Estado = EstadoProgramaBeca.Configurado;

        ActualizarAuditoria(usuarioModificacionId, fechaEfectiva);

        RegistrarEventoDominio(new ProgramaBecaConfiguradoDominioEvent(
            Id,
            Codigo,
            PresupuestoTotalAsignado,
            CupoTotal,
            criteriosElegibilidad.UniversidadesElegiblesIds,
            criteriosElegibilidad.CarrerasElegiblesIds,
            correlacionEfectiva,
            fechaEfectiva));
    }

    public void Activar(Guid usuarioModificacionId, DateTimeOffset? fechaModificacion = null)
    {
        if (Estado != EstadoProgramaBeca.Configurado)
        {
            throw new ProgramaNoConfigurableException(
                Id,
                $"Solo un programa en estado Configurado puede activarse (estado actual: '{Estado}').");
        }

        Estado = EstadoProgramaBeca.Activo;
        ActualizarAuditoria(usuarioModificacionId, (fechaModificacion ?? DateTimeOffset.UtcNow).ToUniversalTime());
    }
}
