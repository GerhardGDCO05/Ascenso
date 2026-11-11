using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Datos;
using Ascenso.Becas.Domain.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Transiciones;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Domain.Otorgamiento.Entidades;

/// <summary>
/// Oferta emitida al postulante. Su ciclo se modifica únicamente a través
/// de la raíz <c>Beca</c> para mantener consistencia con el agregado.
/// </summary>
public sealed class OfertaBeca : EntidadAuditable
{
    private OfertaBeca()
    {
    }

    private OfertaBeca(
        Guid id,
        Guid becaId,
        DatosOfertaBeca datos,
        DateTimeOffset fechaEmision,
        DateTimeOffset fechaVencimientoAceptacion,
        Guid emitidaPorUsuarioId)
        : base(id, fechaEmision, emitidaPorUsuarioId)
    {
        BecaId = becaId;
        PostulacionId = datos.PostulacionId;
        ReservaPresupuestoId = datos.ReservaPresupuestoId;
        MontoOfertaPorPeriodo = datos.MontoOfertaPorPeriodo;
        MontoTotalEstimadoDuracion = datos.MontoTotalEstimadoDuracion;
        DuracionPeriodos = datos.DuracionPeriodos;
        CondicionesSnapshot = datos.CondicionesSnapshot;
        UrlCartaPdfFirebase = datos.UrlCartaPdfFirebase;
        HashCartaSha256 = datos.HashCartaSha256;
        FechaEmision = fechaEmision.ToUniversalTime();
        FechaVencimientoAceptacion = fechaVencimientoAceptacion.ToUniversalTime();
        Estado = EstadoOferta.PendienteRespuesta;
        ReferenciaJobExpiracionHangfireId = datos.ReferenciaJobExpiracionHangfireId;
        EmitidaPorUsuarioId = emitidaPorUsuarioId;
        AutorizacionOtpEmisionId = datos.AutorizacionOtpEmisionId;
    }

    public Guid BecaId { get; private set; }

    public Guid PostulacionId { get; private set; }

    public Guid ReservaPresupuestoId { get; private set; }

    public decimal MontoOfertaPorPeriodo { get; private set; }

    public decimal MontoTotalEstimadoDuracion { get; private set; }

    public int DuracionPeriodos { get; private set; }

    public CondicionesPermanencia CondicionesSnapshot { get; private set; } = null!;

    public string UrlCartaPdfFirebase { get; private set; } = string.Empty;

    public string HashCartaSha256 { get; private set; } = string.Empty;

    public DateTimeOffset FechaEmision { get; private set; }

    public DateTimeOffset FechaVencimientoAceptacion { get; private set; }

    public EstadoOferta Estado { get; private set; }

    public string? MotivoRechazoOAnulacion { get; private set; }

    public DateTimeOffset? FechaRespuesta { get; private set; }

    public string ReferenciaJobExpiracionHangfireId { get; private set; } = string.Empty;

    public Guid EmitidaPorUsuarioId { get; private set; }

    public Guid? AutorizacionOtpEmisionId { get; private set; }

    public Guid? AutorizacionOtpAceptacionId { get; private set; }

    internal static OfertaBeca Crear(
        Guid becaId,
        DatosOfertaBeca datos,
        ContextoOperacion contexto,
        DateTimeOffset fechaVencimientoAceptacion,
        Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(datos);
        ArgumentNullException.ThrowIfNull(contexto);

        if (becaId == Guid.Empty)
        {
            throw new ArgumentException("La beca de la oferta es obligatoria.", nameof(becaId));
        }

        if (datos.PostulacionId == Guid.Empty)
        {
            throw new ArgumentException("La postulación de la oferta es obligatoria.", nameof(datos));
        }

        if (datos.ReservaPresupuestoId == Guid.Empty)
        {
            throw new ArgumentException("La reserva de presupuesto de la oferta es obligatoria.", nameof(datos));
        }

        if (datos.DuracionPeriodos <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(datos), "La duración de la oferta debe ser mayor que cero.");
        }

        ArgumentNullException.ThrowIfNull(datos.CondicionesSnapshot);

        ValidarMonto(datos.MontoOfertaPorPeriodo, "MontoOfertaPorPeriodo");
        ValidarMonto(datos.MontoTotalEstimadoDuracion, "MontoTotalEstimadoDuracion");

        var totalEsperado = CalculadoraMontoOferta.CalcularMontoTotalEstimado(
            datos.MontoOfertaPorPeriodo,
            datos.DuracionPeriodos);

        if (datos.MontoTotalEstimadoDuracion != totalEsperado)
        {
            throw new ArgumentException(
                "El monto total estimado no coincide con el monto por periodo multiplicado por la duración.",
                nameof(datos));
        }

        if (string.IsNullOrWhiteSpace(datos.UrlCartaPdfFirebase))
        {
            throw new ArgumentException("La URL de la carta de la oferta es obligatoria.", nameof(datos));
        }

        if (!EsHashSha256Valido(datos.HashCartaSha256))
        {
            throw new ArgumentException(
                "El hash de la carta debe ser un SHA-256 hexadecimal de 64 caracteres.",
                nameof(datos));
        }

        if (string.IsNullOrWhiteSpace(datos.ReferenciaJobExpiracionHangfireId))
        {
            throw new ArgumentException("La referencia del job de expiración es obligatoria.", nameof(datos));
        }

        if (fechaVencimientoAceptacion <= contexto.Fecha)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fechaVencimientoAceptacion),
                "La fecha de vencimiento de la oferta debe ser posterior a su emisión.");
        }

        var ofertaId = id ?? UuidV7.Nuevo();

        if (!UuidV7.EsVersion7(ofertaId))
        {
            throw new ArgumentException("El identificador de la oferta debe ser UUIDv7.", nameof(id));
        }

        return new OfertaBeca(
            ofertaId,
            becaId,
            datos,
            contexto.Fecha,
            fechaVencimientoAceptacion,
            contexto.UsuarioAutorId);
    }

    internal void Aceptar(ContextoOperacion contexto, Guid? autorizacionOtpAceptacionId = null)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ValidarTransicion(EstadoOferta.Aceptada);

        if (autorizacionOtpAceptacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "La autorización OTP de aceptación, cuando se informa, debe tener un identificador válido.",
                nameof(autorizacionOtpAceptacionId));
        }

        if (contexto.Fecha > FechaVencimientoAceptacion)
        {
            throw new Excepciones.OfertaVencidaException(Id, FechaVencimientoAceptacion, contexto.Fecha);
        }

        Estado = EstadoOferta.Aceptada;
        FechaRespuesta = contexto.Fecha;
        AutorizacionOtpAceptacionId = autorizacionOtpAceptacionId;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    internal void Rechazar(string? motivo, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ValidarTransicion(EstadoOferta.RechazadaPorPostulante);

        Estado = EstadoOferta.RechazadaPorPostulante;
        MotivoRechazoOAnulacion = NormalizarMotivo(motivo);
        FechaRespuesta = contexto.Fecha;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    internal void Anular(string justificacion, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ValidarTransicion(EstadoOferta.AnuladaPorCoordinacion);

        if (string.IsNullOrWhiteSpace(justificacion) || justificacion.Trim().Length < 10)
        {
            throw new Excepciones.JustificacionRequeridaException("anular la oferta de beca");
        }

        Estado = EstadoOferta.AnuladaPorCoordinacion;
        MotivoRechazoOAnulacion = justificacion.Trim();
        FechaRespuesta = contexto.Fecha;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    internal void Expirar(ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ValidarTransicion(EstadoOferta.Expirada);

        Estado = EstadoOferta.Expirada;
        MotivoRechazoOAnulacion = "Expiración automática de la oferta de beca.";
        FechaRespuesta = contexto.Fecha;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    internal bool EsTerminal()
        => TransicionesOfertaBeca.Tabla.EsTerminal(Estado);

    private void ValidarTransicion(EstadoOferta destino)
        => TransicionesOfertaBeca.Tabla.Validar(Estado, destino);

    private static string? NormalizarMotivo(string? motivo)
        => string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();

    private static void ValidarMonto(decimal monto, string nombre)
    {
        if (monto < 0)
        {
            throw new ArgumentOutOfRangeException(nombre, "El monto de la oferta no puede ser negativo.");
        }

        if (CalculadoraMontoOferta.Redondear(monto) != monto)
        {
            throw new ArgumentException("El monto de la oferta admite como máximo dos decimales.", nombre);
        }
    }

    private static bool EsHashSha256Valido(string hash)
    {
        if (hash is null || hash.Length != 64)
        {
            return false;
        }

        foreach (var caracter in hash)
        {
            if (!Uri.IsHexDigit(caracter))
            {
                return false;
            }
        }

        return true;
    }
}
