using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Programas.Entidades;
using Ascenso.Programas.Domain.Programas.Eventos;
using Ascenso.Programas.Domain.Programas.ObjetosValor;

namespace Ascenso.Programas.Domain.Programas.Agregados;

/// <summary>
/// Raíz del agregado de Programas de Becas, composición de políticas/criterios, asignaciones presupuestarias,
/// reservas atómicas y libro mayor de movimientos presupuestarios.
/// </summary>
public sealed class ProgramaBeca : AggregateRoot
{
    private readonly List<AsignacionFondoPrograma> _asignacionesFondo = [];
    private readonly List<ReservaPresupuestaria> _reservasPresupuestarias = [];
    private readonly List<MovimientoPresupuestario> _movimientosPresupuestarios = [];

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
        CuposReservados = 0;
        CuposOcupados = 0;
        DuracionPeriodos = duracionPeriodos;
        PeriodoInicialId = periodoInicialId;
        VigenciaDesde = vigenciaDesde;
        VigenciaHasta = vigenciaHasta;
        PermiteAcumulacion = permiteAcumulacion;
        RequiereCompromisoFirmado = requiereCompromisoFirmado;
        TerminosCompromisoEspecial = terminosCompromisoEspecial;
        PresupuestoTotalAsignado = 0.00m;
        PresupuestoReservado = 0.00m;
        PresupuestoEjecutado = 0.00m;
        Moneda = "USD";
        Estado = EstadoProgramaBeca.Borrador;
    }

    public string Codigo { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public string Descripcion { get; private set; } = string.Empty;

    public PoliticaCobertura PoliticaCobertura { get; private set; } = null!;

    public int CupoTotal { get; private set; }

    public int CuposReservados { get; private set; }

    public int CuposOcupados { get; private set; }

    public int CuposDisponibles => CupoTotal - CuposReservados - CuposOcupados;

    public int DuracionPeriodos { get; private set; }

    public string PeriodoInicialId { get; private set; } = string.Empty;

    public DateOnly VigenciaDesde { get; private set; }

    public DateOnly VigenciaHasta { get; private set; }

    public bool PermiteAcumulacion { get; private set; }

    public bool RequiereCompromisoFirmado { get; private set; }

    public string? TerminosCompromisoEspecial { get; private set; }

    public decimal PresupuestoTotalAsignado { get; private set; }

    public decimal PresupuestoReservado { get; private set; }

    public decimal PresupuestoEjecutado { get; private set; }

    public decimal PresupuestoDisponible => decimal.Round(
        PresupuestoTotalAsignado - PresupuestoReservado - PresupuestoEjecutado,
        2,
        MidpointRounding.AwayFromZero);

    public string Moneda { get; private set; } = "USD";

    public EstadoProgramaBeca Estado { get; private set; }

    public CriteriosElegibilidadPrograma? CriteriosElegibilidad { get; private set; }

    public CondicionesPermanencia? CondicionesPermanencia { get; private set; }

    public IReadOnlyCollection<AsignacionFondoPrograma> AsignacionesFondo => _asignacionesFondo.AsReadOnly();

    public IReadOnlyCollection<ReservaPresupuestaria> ReservasPresupuestarias => _reservasPresupuestarias.AsReadOnly();

    public IReadOnlyCollection<MovimientoPresupuestario> MovimientosPresupuestarios => _movimientosPresupuestarios.AsReadOnly();

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

        var movimiento = MovimientoPresupuestario.Crear(
            Id,
            TipoMovimientoPresupuestario.AsignacionDonante,
            asignacion.MontoAsignado,
            asignacion.PeriodoAcademicoId,
            $"Asignación de fondo del donante '{donanteId}' para el período '{asignacion.PeriodoAcademicoId}'.",
            autorizadoPorUsuarioId,
            asignacion.Id,
            fechaEfectiva,
            correlacionEfectiva);

        _movimientosPresupuestarios.Add(movimiento);

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

        var montoRedondeado = decimal.Round(montoAReducir, 2, MidpointRounding.AwayFromZero);
        var nuevoPresupuestoTotal = decimal.Round(
            PresupuestoTotalAsignado - montoRedondeado,
            2,
            MidpointRounding.AwayFromZero);

        if (nuevoPresupuestoTotal < PresupuestoReservado + PresupuestoEjecutado)
        {
            throw new PresupuestoEnEjecucionException(Id);
        }

        var fechaEfectiva = (fechaModificacion ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var montoReducido = asignacion.ReducirODesasignar(montoAReducir, usuarioModificacionId, fechaEfectiva);

        PresupuestoTotalAsignado = decimal.Round(
            PresupuestoTotalAsignado - montoReducido,
            2,
            MidpointRounding.AwayFromZero);

        var movimiento = MovimientoPresupuestario.Crear(
            Id,
            TipoMovimientoPresupuestario.ReajusteDonante,
            montoReducido,
            asignacion.PeriodoAcademicoId,
            $"Reajuste presupuestario de asignación '{asignacion.Id}' del donante '{asignacion.DonanteId}'.",
            usuarioModificacionId,
            asignacion.Id,
            fechaEfectiva);

        _movimientosPresupuestarios.Add(movimiento);

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

    public (ReservaPresupuestaria Reserva, MovimientoPresupuestario Movimiento, bool YaExistia) ReservarPresupuesto(
        Guid referenciaExternaId,
        string periodoAcademicoId,
        decimal monto,
        string descripcion,
        Guid usuarioAutorId,
        DateTimeOffset? fechaReserva = null,
        Guid? correlacionId = null)
    {
        if (referenciaExternaId == Guid.Empty)
        {
            throw new ArgumentException("La referencia externa de la reserva es obligatoria.", nameof(referenciaExternaId));
        }

        if (string.IsNullOrWhiteSpace(periodoAcademicoId))
        {
            throw new ArgumentException("El período académico es obligatorio.", nameof(periodoAcademicoId));
        }

        var montoRedondeado = decimal.Round(monto, 2, MidpointRounding.AwayFromZero);
        if (montoRedondeado <= 0.00m)
        {
            throw new ArgumentOutOfRangeException(nameof(monto), "El monto a reservar debe ser mayor que cero.");
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentException("La descripción de la reserva es obligatoria.", nameof(descripcion));
        }

        var periodoNormalizado = periodoAcademicoId.Trim();
        var reservaExistente = _reservasPresupuestarias.SingleOrDefault(
            item => item.ReferenciaExternaId == referenciaExternaId);

        if (reservaExistente is not null)
        {
            if (reservaExistente.Estado == EstadoReservaPresupuestaria.Reservada
                && reservaExistente.Monto == montoRedondeado
                && string.Equals(reservaExistente.PeriodoAcademicoId, periodoNormalizado, StringComparison.OrdinalIgnoreCase))
            {
                var movimientoExistente = _movimientosPresupuestarios.FirstOrDefault(
                    item => item.ReferenciaExternaId == referenciaExternaId
                        && item.TipoMovimiento == TipoMovimientoPresupuestario.ReservaCupo);

                return (reservaExistente, movimientoExistente!, true);
            }

            throw new ReservaConParametrosDistintosException(Id, referenciaExternaId);
        }

        if (Estado is not (EstadoProgramaBeca.Activo or EstadoProgramaBeca.Configurado))
        {
            throw new EstadoProgramaInvalidoParaOperacionException(Id, Estado, nameof(ReservarPresupuesto));
        }

        if (CuposDisponibles < 1 || PresupuestoDisponible < montoRedondeado)
        {
            throw new SinDisponibilidadPresupuestariaException(Id, montoRedondeado);
        }

        var fechaEfectiva = (fechaReserva ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var correlacionEfectiva = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaEfectiva);

        var reserva = ReservaPresupuestaria.CrearReservada(
            Id,
            referenciaExternaId,
            periodoNormalizado,
            montoRedondeado,
            descripcion,
            usuarioAutorId,
            fechaEfectiva);

        var movimiento = MovimientoPresupuestario.Crear(
            Id,
            TipoMovimientoPresupuestario.ReservaCupo,
            montoRedondeado,
            periodoNormalizado,
            descripcion,
            usuarioAutorId,
            referenciaExternaId,
            fechaEfectiva,
            correlacionEfectiva);

        _reservasPresupuestarias.Add(reserva);
        _movimientosPresupuestarios.Add(movimiento);

        PresupuestoReservado = decimal.Round(
            PresupuestoReservado + montoRedondeado,
            2,
            MidpointRounding.AwayFromZero);
        CuposReservados += 1;

        ActualizarAuditoria(usuarioAutorId, fechaEfectiva);

        RegistrarEventoDominio(new PresupuestoProgramaReservadoDominioEvent(
            Id,
            reserva.Id,
            reserva.ReferenciaExternaId,
            reserva.PeriodoAcademicoId,
            reserva.Monto,
            PresupuestoDisponible,
            CuposDisponibles,
            correlacionEfectiva,
            fechaEfectiva));

        return (reserva, movimiento, false);
    }

    public (ReservaPresupuestaria Reserva, MovimientoPresupuestario Movimiento) ConfirmarReservaPresupuesto(
        Guid referenciaExternaId,
        string? descripcion,
        Guid usuarioModificacionId,
        DateTimeOffset? fechaConfirmacion = null,
        Guid? correlacionId = null)
    {
        if (referenciaExternaId == Guid.Empty)
        {
            throw new ArgumentException("La referencia externa de la reserva es obligatoria.", nameof(referenciaExternaId));
        }

        var reserva = _reservasPresupuestarias.SingleOrDefault(
            item => item.ReferenciaExternaId == referenciaExternaId)
            ?? throw new ReservaPresupuestariaNoEncontradaException(Id, referenciaExternaId);

        var fechaEfectiva = (fechaConfirmacion ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var correlacionEfectiva = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaEfectiva);

        reserva.Confirmar(usuarioModificacionId, fechaEfectiva);

        PresupuestoReservado = decimal.Round(
            PresupuestoReservado - reserva.Monto,
            2,
            MidpointRounding.AwayFromZero);
        PresupuestoEjecutado = decimal.Round(
            PresupuestoEjecutado + reserva.Monto,
            2,
            MidpointRounding.AwayFromZero);
        CuposReservados -= 1;
        CuposOcupados += 1;

        var descripcionEfectiva = string.IsNullOrWhiteSpace(descripcion)
            ? $"Confirmación de desembolso de reserva '{referenciaExternaId}'."
            : descripcion.Trim();

        var movimiento = MovimientoPresupuestario.Crear(
            Id,
            TipoMovimientoPresupuestario.ConfirmacionDesembolso,
            reserva.Monto,
            reserva.PeriodoAcademicoId,
            descripcionEfectiva,
            usuarioModificacionId,
            referenciaExternaId,
            fechaEfectiva,
            correlacionEfectiva);

        _movimientosPresupuestarios.Add(movimiento);

        ActualizarAuditoria(usuarioModificacionId, fechaEfectiva);

        RegistrarEventoDominio(new PresupuestoProgramaConfirmadoDominioEvent(
            Id,
            reserva.Id,
            reserva.ReferenciaExternaId,
            reserva.PeriodoAcademicoId,
            reserva.Monto,
            PresupuestoEjecutado,
            CuposOcupados,
            correlacionEfectiva,
            fechaEfectiva));

        return (reserva, movimiento);
    }

    public (ReservaPresupuestaria Reserva, MovimientoPresupuestario Movimiento) LiberarReservaPresupuesto(
        Guid referenciaExternaId,
        string motivo,
        Guid usuarioModificacionId,
        DateTimeOffset? fechaLiberacion = null,
        Guid? correlacionId = null)
    {
        if (referenciaExternaId == Guid.Empty)
        {
            throw new ArgumentException("La referencia externa de la reserva es obligatoria.", nameof(referenciaExternaId));
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException("El motivo de liberación de la reserva es obligatorio.", nameof(motivo));
        }

        var reserva = _reservasPresupuestarias.SingleOrDefault(
            item => item.ReferenciaExternaId == referenciaExternaId)
            ?? throw new ReservaPresupuestariaNoEncontradaException(Id, referenciaExternaId);

        var fechaEfectiva = (fechaLiberacion ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var correlacionEfectiva = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaEfectiva);

        reserva.Liberar(motivo, usuarioModificacionId, fechaEfectiva);

        PresupuestoReservado = decimal.Round(
            PresupuestoReservado - reserva.Monto,
            2,
            MidpointRounding.AwayFromZero);
        CuposReservados -= 1;

        var movimiento = MovimientoPresupuestario.Crear(
            Id,
            TipoMovimientoPresupuestario.LiberacionReserva,
            reserva.Monto,
            reserva.PeriodoAcademicoId,
            $"Liberación de reserva '{referenciaExternaId}': {motivo.Trim()}",
            usuarioModificacionId,
            referenciaExternaId,
            fechaEfectiva,
            correlacionEfectiva);

        _movimientosPresupuestarios.Add(movimiento);

        ActualizarAuditoria(usuarioModificacionId, fechaEfectiva);

        RegistrarEventoDominio(new ReservaPresupuestoLiberadaDominioEvent(
            Id,
            reserva.Id,
            reserva.ReferenciaExternaId,
            reserva.PeriodoAcademicoId,
            reserva.Monto,
            reserva.MotivoLiberacion!,
            PresupuestoDisponible,
            CuposDisponibles,
            correlacionEfectiva,
            fechaEfectiva));

        return (reserva, movimiento);
    }

    public MovimientoPresupuestario CerrarPeriodo(
        string periodoAcademicoId,
        string observaciones,
        bool cerrarProgramaDefinitivamente,
        Guid usuarioModificacionId,
        DateTimeOffset? fechaCierre = null,
        Guid? correlacionId = null)
    {
        if (string.IsNullOrWhiteSpace(periodoAcademicoId))
        {
            throw new ArgumentException("El período académico a cerrar es obligatorio.", nameof(periodoAcademicoId));
        }

        if (string.IsNullOrWhiteSpace(observaciones))
        {
            throw new ArgumentException("Las observaciones del cierre de período son obligatorias.", nameof(observaciones));
        }

        if (Estado is not (EstadoProgramaBeca.Activo or EstadoProgramaBeca.Configurado))
        {
            throw new EstadoProgramaInvalidoParaOperacionException(Id, Estado, nameof(CerrarPeriodo));
        }

        var periodoNormalizado = periodoAcademicoId.Trim();
        var reservasPendientes = cerrarProgramaDefinitivamente
            ? _reservasPresupuestarias.Count(item => item.Estado == EstadoReservaPresupuestaria.Reservada)
            : _reservasPresupuestarias.Count(item =>
                item.Estado == EstadoReservaPresupuestaria.Reservada
                && string.Equals(item.PeriodoAcademicoId, periodoNormalizado, StringComparison.OrdinalIgnoreCase));

        if (reservasPendientes > 0)
        {
            throw new ReservasPendientesEnPeriodoException(Id, periodoNormalizado, reservasPendientes);
        }

        var fechaEfectiva = (fechaCierre ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var correlacionEfectiva = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaEfectiva);

        var saldoNoEjecutadoConsolidado = PresupuestoDisponible;

        var movimiento = MovimientoPresupuestario.Crear(
            Id,
            TipoMovimientoPresupuestario.CierrePeriodo,
            saldoNoEjecutadoConsolidado,
            periodoNormalizado,
            observaciones.Trim(),
            usuarioModificacionId,
            null,
            fechaEfectiva,
            correlacionEfectiva);

        _movimientosPresupuestarios.Add(movimiento);

        if (cerrarProgramaDefinitivamente)
        {
            Estado = EstadoProgramaBeca.Cerrado;
        }

        ActualizarAuditoria(usuarioModificacionId, fechaEfectiva);

        RegistrarEventoDominio(new PeriodoProgramaCerradoDominioEvent(
            Id,
            periodoNormalizado,
            saldoNoEjecutadoConsolidado,
            PresupuestoEjecutado,
            Estado,
            correlacionEfectiva,
            fechaEfectiva));

        return movimiento;
    }
}
