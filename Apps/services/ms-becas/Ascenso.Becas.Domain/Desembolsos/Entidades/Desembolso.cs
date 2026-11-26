using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Transiciones;

namespace Ascenso.Becas.Domain.Desembolsos.Entidades;

/// <summary>Cuota de un calendario; solo la raíz del calendario debe persistirla.</summary>
public sealed class Desembolso : EntidadAuditable
{
    private readonly List<IncidenciaDesembolso> _incidencias = [];

    private Desembolso()
    {
    }

    private Desembolso(Guid id, ContextoOperacion contexto)
        : base(id, contexto.Fecha, contexto.UsuarioAutorId)
    {
    }

    public Guid CalendarioDesembolsoId { get; private set; }

    public Guid BecaId { get; private set; }

    public int NumeroCuota { get; private set; }

    public decimal MontoProgramado { get; private set; }

    public decimal? MontoEjecutado { get; private set; }

    public DateOnly FechaProgramada { get; private set; }

    public DateOnly? FechaEjecucionEfectiva { get; private set; }

    public DestinatarioPago Destinatario { get; private set; }

    public Guid? DatosPagoBecarioId { get; private set; }

    public string? CuentaBancariaDestinoSnapshot { get; private set; }

    public MetodoPago? MetodoPago { get; private set; }

    public EstadoDesembolso Estado { get; private set; }

    public int IntentosEjecucion { get; private set; }

    public string? ReferenciaBancaria { get; private set; }

    public string? ObservacionIncidenciaBancaria { get; private set; }

    public Guid? RegistradoPorUsuarioId { get; private set; }

    public Guid? ConfirmadoPorUsuarioId { get; private set; }

    public Guid? AutorizacionOtpConfirmacionId { get; private set; }

    public DateTimeOffset? FechaConfirmacion { get; private set; }

    public Guid? SuspensionDesembolsoId { get; private set; }

    public Guid? PausaAplazamientoId { get; private set; }

    public Guid? DesembolsoOrigenReintentoId { get; private set; }

    public bool EsVigente { get; private set; }

    public IReadOnlyCollection<IncidenciaDesembolso> Incidencias => _incidencias.AsReadOnly();

    internal static Desembolso Programar(
        Guid calendarioId,
        Guid becaId,
        int numeroCuota,
        decimal monto,
        DateOnly fecha,
        DestinatarioPago destinatario,
        ContextoOperacion contexto)
        => new(UuidV7.Nuevo(), contexto)
        {
            CalendarioDesembolsoId = calendarioId,
            BecaId = becaId,
            NumeroCuota = numeroCuota,
            MontoProgramado = monto,
            FechaProgramada = fecha,
            Destinatario = destinatario,
            Estado = EstadoDesembolso.Programado,
            EsVigente = true
        };

    internal void RegistrarEjecucion(
        decimal monto,
        DateOnly fechaEjecucion,
        DestinatarioPago destinatario,
        Guid? datosPagoBecarioId,
        string cuentaBancariaDestinoSnapshot,
        MetodoPago metodo,
        string referenciaBancaria,
        ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        TransicionesDesembolso.Tabla.Validar(Estado, EstadoDesembolso.EnProceso);
        if (monto <= 0 || monto > MontoProgramado || decimal.Round(monto, 2) != monto)
        {
            throw new ArgumentOutOfRangeException(nameof(monto));
        }

        if (fechaEjecucion == default || !Enum.IsDefined(destinatario) || !Enum.IsDefined(metodo)
            || datosPagoBecarioId == Guid.Empty
            || (destinatario == DestinatarioPago.Becario && datosPagoBecarioId is null)
            || !EsSnapshotCifrado(cuentaBancariaDestinoSnapshot)
            || string.IsNullOrWhiteSpace(referenciaBancaria))
        {
            throw new ArgumentException("Los datos de ejecución del pago están incompletos.");
        }

        MontoEjecutado = monto;
        FechaEjecucionEfectiva = fechaEjecucion;
        Destinatario = destinatario;
        DatosPagoBecarioId = datosPagoBecarioId;
        CuentaBancariaDestinoSnapshot = cuentaBancariaDestinoSnapshot;
        MetodoPago = metodo;
        ReferenciaBancaria = referenciaBancaria.Trim();
        IntentosEjecucion = Math.Max(1, IntentosEjecucion);
        RegistradoPorUsuarioId = contexto.UsuarioAutorId;
        Estado = EstadoDesembolso.EnProceso;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    internal void Confirmar(Guid autorizacionOtpId, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        TransicionesDesembolso.Tabla.Validar(Estado, EstadoDesembolso.PagadoConfirmado);
        if (autorizacionOtpId == Guid.Empty)
        {
            throw new ArgumentException("La confirmación requiere autorización OTP.", nameof(autorizacionOtpId));
        }

        Estado = EstadoDesembolso.PagadoConfirmado;
        ConfirmadoPorUsuarioId = contexto.UsuarioAutorId;
        AutorizacionOtpConfirmacionId = autorizacionOtpId;
        FechaConfirmacion = contexto.Fecha;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    internal IncidenciaDesembolso ReportarIncidencia(
        EstadoDesembolso estado,
        string motivo,
        string observacion,
        ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        if (estado is not (EstadoDesembolso.Fallido or EstadoDesembolso.Devuelto))
        {
            throw new ArgumentOutOfRangeException(nameof(estado));
        }

        TransicionesDesembolso.Tabla.Validar(Estado, estado);
        if (string.IsNullOrWhiteSpace(observacion))
        {
            throw new ArgumentException("La incidencia requiere observación bancaria.", nameof(observacion));
        }

        var incidencia = IncidenciaDesembolso.Registrar(
            Id,
            estado == EstadoDesembolso.Fallido
                ? TipoIncidenciaDesembolso.FalloBancario
                : TipoIncidenciaDesembolso.DevolucionBancaria,
            motivo,
            observacion,
            contexto);
        Estado = estado;
        ObservacionIncidenciaBancaria = observacion.Trim();
        _incidencias.Add(incidencia);
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
        return incidencia;
    }

    internal void ResolverIncidencia(Guid incidenciaId, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var incidencia = _incidencias.SingleOrDefault(item => item.Id == incidenciaId)
            ?? throw new InvalidOperationException("La incidencia no pertenece al desembolso.");
        incidencia.Resolver(contexto);
    }

    internal Desembolso Reintentar(ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        TransicionesDesembolso.Tabla.Validar(Estado, EstadoDesembolso.Programado);
        if (_incidencias.Count == 0 || _incidencias.Any(item => item.Estado != EstadoIncidencia.Resuelta))
        {
            throw new InvalidOperationException("La incidencia debe resolverse antes del reintento.");
        }

        if (!EsVigente)
        {
            throw new InvalidOperationException("Solo puede reintentarse el desembolso vigente.");
        }

        EsVigente = false;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);

        return new Desembolso(UuidV7.Nuevo(), contexto)
        {
            CalendarioDesembolsoId = CalendarioDesembolsoId,
            BecaId = BecaId,
            NumeroCuota = NumeroCuota,
            MontoProgramado = MontoProgramado,
            FechaProgramada = FechaProgramada,
            Destinatario = Destinatario,
            Estado = EstadoDesembolso.Programado,
            IntentosEjecucion = IntentosEjecucion + 1,
            DesembolsoOrigenReintentoId = Id,
            EsVigente = true
        };
    }

    internal void Suspender(Guid suspensionId, Guid? pausaId, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        TransicionesDesembolso.Tabla.Validar(Estado, EstadoDesembolso.Suspendido);
        if (suspensionId == Guid.Empty)
        {
            throw new ArgumentException("La suspensión es obligatoria.", nameof(suspensionId));
        }

        Estado = EstadoDesembolso.Suspendido;
        SuspensionDesembolsoId = suspensionId;
        PausaAplazamientoId = pausaId;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    internal void Reanudar(ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        TransicionesDesembolso.Tabla.Validar(Estado, EstadoDesembolso.Programado);
        Estado = EstadoDesembolso.Programado;
        SuspensionDesembolsoId = null;
        PausaAplazamientoId = null;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    internal void Cancelar(ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        TransicionesDesembolso.Tabla.Validar(Estado, EstadoDesembolso.Cancelado);
        Estado = EstadoDesembolso.Cancelado;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    private static bool EsSnapshotCifrado(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return false;
        var partes = valor.Split(':');
        if (partes.Length != 4 || partes[0] != "v1") return false;
        try
        {
            return Convert.FromBase64String(partes[1]).Length == 12
                && Convert.FromBase64String(partes[2]).Length == 16
                && Convert.FromBase64String(partes[3]).Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
