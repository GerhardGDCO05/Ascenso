using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;

namespace Ascenso.Programas.Domain.Donantes.Entidades;

/// <summary>
/// Registro contable inmutable (append-only) de un aporte financiero recibido de un donante.
/// </summary>
public sealed class AporteDonante : EntidadInmutable
{
    private AporteDonante()
    {
    }

    private AporteDonante(
        Guid id,
        Guid donanteId,
        decimal montoAporte,
        string moneda,
        string periodoAcademicoId,
        DateTimeOffset fechaAporte,
        string bancoEmisor,
        string numeroReferenciaBancaria,
        string urlComprobanteFirebase,
        EstadoAporte estadoAporte,
        string? observaciones,
        DateTimeOffset fechaCreacion,
        Guid registradoPorUsuarioId,
        Guid correlacionId)
        : base(id, fechaCreacion, registradoPorUsuarioId, correlacionId)
    {
        DonanteId = donanteId;
        MontoAporte = montoAporte;
        Moneda = moneda;
        PeriodoAcademicoId = periodoAcademicoId;
        FechaAporte = fechaAporte.ToUniversalTime();
        BancoEmisor = bancoEmisor;
        NumeroReferenciaBancaria = numeroReferenciaBancaria;
        UrlComprobanteFirebase = urlComprobanteFirebase;
        EstadoAporte = estadoAporte;
        Observaciones = observaciones;
    }

    public Guid DonanteId { get; private set; }

    public decimal MontoAporte { get; private set; }

    public string Moneda { get; private set; } = string.Empty;

    public string PeriodoAcademicoId { get; private set; } = string.Empty;

    public DateTimeOffset FechaAporte { get; private set; }

    public string BancoEmisor { get; private set; } = string.Empty;

    public string NumeroReferenciaBancaria { get; private set; } = string.Empty;

    public string UrlComprobanteFirebase { get; private set; } = string.Empty;

    public EstadoAporte EstadoAporte { get; private set; }

    public string? Observaciones { get; private set; }

    public Guid RegistradoPorUsuarioId => UsuarioAutorId;

    internal static AporteDonante CrearConfirmado(
        Guid donanteId,
        decimal montoAporte,
        string moneda,
        string periodoAcademicoId,
        DateTimeOffset fechaAporte,
        string bancoEmisor,
        string numeroReferenciaBancaria,
        string urlComprobanteFirebase,
        Guid registradoPorUsuarioId,
        string? observaciones,
        DateTimeOffset fechaRegistro,
        Guid correlacionId)
    {
        if (donanteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del donante es obligatorio.", nameof(donanteId));
        }

        if (montoAporte <= 0 || decimal.Round(montoAporte, 2) != montoAporte)
        {
            throw new MontoAporteInvalidoException(montoAporte);
        }

        if (!string.Equals(moneda?.Trim(), "USD", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("La divisa admitida para los aportes de donantes es USD.", nameof(moneda));
        }

        if (string.IsNullOrWhiteSpace(periodoAcademicoId))
        {
            throw new ArgumentException("El período académico asociado al aporte es obligatorio.", nameof(periodoAcademicoId));
        }

        if (fechaAporte == default)
        {
            throw new ArgumentException("La fecha de transacción del aporte es obligatoria.", nameof(fechaAporte));
        }

        if (string.IsNullOrWhiteSpace(bancoEmisor))
        {
            throw new ArgumentException("El banco emisor es obligatorio.", nameof(bancoEmisor));
        }

        if (string.IsNullOrWhiteSpace(numeroReferenciaBancaria))
        {
            throw new ArgumentException("El número de referencia bancaria es obligatorio.", nameof(numeroReferenciaBancaria));
        }

        if (string.IsNullOrWhiteSpace(urlComprobanteFirebase))
        {
            throw new ComprobanteAporteRequeridoException();
        }

        var urlLimpia = urlComprobanteFirebase.Trim();
        var rutaEsperada = $"donantes/{donanteId:D}/aportes/";
        var rutaDecodificada = Uri.UnescapeDataString(urlLimpia);
        if (!rutaDecodificada.Contains(rutaEsperada, StringComparison.OrdinalIgnoreCase))
        {
            throw new ComprobanteAporteRequeridoException(
                $"El comprobante debe ubicarse bajo la ruta '/{rutaEsperada}{{uuid}}.pdf' en Firebase Storage.");
        }

        var observacionesLimpias = string.IsNullOrWhiteSpace(observaciones)
            ? null
            : observaciones.Trim();

        return new AporteDonante(
            UuidV7.Nuevo(fechaRegistro),
            donanteId,
            montoAporte,
            "USD",
            periodoAcademicoId.Trim(),
            fechaAporte,
            bancoEmisor.Trim(),
            numeroReferenciaBancaria.Trim().ToUpperInvariant(),
            urlLimpia,
            EstadoAporte.Confirmado,
            observacionesLimpias,
            fechaRegistro,
            registradoPorUsuarioId,
            correlacionId);
    }
}

