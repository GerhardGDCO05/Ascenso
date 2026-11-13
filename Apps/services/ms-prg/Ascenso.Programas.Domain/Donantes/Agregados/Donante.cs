using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Donantes.Entidades;
using Ascenso.Programas.Domain.Donantes.Eventos;
using Ascenso.Programas.Domain.Donantes.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;

namespace Ascenso.Programas.Domain.Donantes.Agregados;

/// <summary>
/// Raíz del agregado de Donantes y control transaccional de sus aportes y saldos disponibles.
/// </summary>
public sealed class Donante : AggregateRoot
{
    private readonly List<AporteDonante> _aportes = [];

    private Donante()
    {
    }

    private Donante(
        Guid id,
        TipoDonante tipoDonante,
        string razonSocialNombre,
        string identificacionFiscal,
        string email,
        string telefono,
        string direccionFiscal,
        ContactoDonante contactoPrincipal,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
        : base(id, fechaCreacion, usuarioCreacionId)
    {
        TipoDonante = tipoDonante;
        RazonSocialNombre = razonSocialNombre;
        IdentificacionFiscal = identificacionFiscal;
        Email = email;
        Telefono = telefono;
        DireccionFiscal = direccionFiscal;
        ContactoPrincipal = contactoPrincipal;
        EstadoDonante = EstadoDonante.Activo;
        Moneda = "USD";
        SaldoTotalHistorico = 0.00m;
        SaldoDisponibleAsignacion = 0.00m;
    }

    public TipoDonante TipoDonante { get; private set; }

    public string RazonSocialNombre { get; private set; } = string.Empty;

    public string IdentificacionFiscal { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Telefono { get; private set; } = string.Empty;

    public string DireccionFiscal { get; private set; } = string.Empty;

    public ContactoDonante ContactoPrincipal { get; private set; } = null!;

    public EstadoDonante EstadoDonante { get; private set; }

    public string Moneda { get; private set; } = "USD";

    public decimal SaldoTotalHistorico { get; private set; }

    public decimal SaldoDisponibleAsignacion { get; private set; }

    public IReadOnlyCollection<AporteDonante> Aportes => _aportes.AsReadOnly();

    public static Donante Crear(
        TipoDonante tipoDonante,
        string razonSocialNombre,
        string identificacionFiscal,
        string email,
        string telefono,
        string direccionFiscal,
        ContactoDonante contactoPrincipal,
        Guid usuarioCreacionId,
        DateTimeOffset? fechaCreacion = null,
        Guid? correlacionId = null)
    {
        if (!Enum.IsDefined(tipoDonante))
        {
            throw new ArgumentOutOfRangeException(nameof(tipoDonante), "El tipo de donante no es válido.");
        }

        if (string.IsNullOrWhiteSpace(razonSocialNombre))
        {
            throw new ArgumentException("La razón social o nombre completo es obligatorio.", nameof(razonSocialNombre));
        }

        if (string.IsNullOrWhiteSpace(identificacionFiscal))
        {
            throw new ArgumentException("La identificación fiscal (RIF/Cédula) es obligatoria.", nameof(identificacionFiscal));
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("El correo electrónico institucional debe ser válido.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(telefono))
        {
            throw new ArgumentException("El teléfono del donante es obligatorio.", nameof(telefono));
        }

        if (string.IsNullOrWhiteSpace(direccionFiscal))
        {
            throw new ArgumentException("La dirección fiscal es obligatoria.", nameof(direccionFiscal));
        }

        ArgumentNullException.ThrowIfNull(contactoPrincipal);

        var fechaEfectiva = (fechaCreacion ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var correlacionEfectiva = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaEfectiva);

        var donante = new Donante(
            UuidV7.Nuevo(fechaEfectiva),
            tipoDonante,
            razonSocialNombre.Trim(),
            identificacionFiscal.Trim().ToUpperInvariant(),
            email.Trim(),
            telefono.Trim(),
            direccionFiscal.Trim(),
            contactoPrincipal,
            fechaEfectiva,
            usuarioCreacionId);

        donante.RegistrarEventoDominio(new DonanteRegistradoDominioEvent(
            donante.Id,
            donante.TipoDonante,
            donante.RazonSocialNombre,
            donante.IdentificacionFiscal,
            donante.Moneda,
            correlacionEfectiva,
            fechaEfectiva));

        return donante;
    }

    public void ActualizarContacto(
        string email,
        string telefono,
        string direccionFiscal,
        ContactoDonante contactoPrincipal,
        Guid usuarioModificacionId,
        DateTimeOffset? fechaModificacion = null)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("El correo electrónico institucional debe ser válido.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(telefono))
        {
            throw new ArgumentException("El teléfono del donante es obligatorio.", nameof(telefono));
        }

        if (string.IsNullOrWhiteSpace(direccionFiscal))
        {
            throw new ArgumentException("La dirección fiscal es obligatoria.", nameof(direccionFiscal));
        }

        ArgumentNullException.ThrowIfNull(contactoPrincipal);

        Email = email.Trim();
        Telefono = telefono.Trim();
        DireccionFiscal = direccionFiscal.Trim();
        ContactoPrincipal = contactoPrincipal;

        ActualizarAuditoria(usuarioModificacionId, (fechaModificacion ?? DateTimeOffset.UtcNow).ToUniversalTime());
    }

    public void Desactivar(
        bool tieneFondosComprometidosEnProgramaVigente,
        Guid usuarioModificacionId,
        DateTimeOffset? fechaModificacion = null)
    {
        if (tieneFondosComprometidosEnProgramaVigente || SaldoTotalHistorico > SaldoDisponibleAsignacion)
        {
            throw new DonanteConFondosComprometidosException(Id);
        }

        EstadoDonante = EstadoDonante.Inactivo;
        ActualizarAuditoria(usuarioModificacionId, (fechaModificacion ?? DateTimeOffset.UtcNow).ToUniversalTime());
    }

    public void Reactivar(
        Guid usuarioModificacionId,
        DateTimeOffset? fechaModificacion = null)
    {
        EstadoDonante = EstadoDonante.Activo;
        ActualizarAuditoria(usuarioModificacionId, (fechaModificacion ?? DateTimeOffset.UtcNow).ToUniversalTime());
    }

    public AporteDonante RegistrarAporte(
        decimal montoAporte,
        string moneda,
        string periodoAcademicoId,
        DateTimeOffset fechaAporte,
        string bancoEmisor,
        string numeroReferenciaBancaria,
        string urlComprobanteFirebase,
        Guid registradoPorUsuarioId,
        string? observaciones = null,
        DateTimeOffset? fechaRegistro = null,
        Guid? correlacionId = null)
    {
        if (EstadoDonante != EstadoDonante.Activo)
        {
            throw new DonanteInactivoException(Id);
        }

        if (!string.IsNullOrWhiteSpace(numeroReferenciaBancaria))
        {
            var referenciaNormalizada = numeroReferenciaBancaria.Trim().ToUpperInvariant();
            if (_aportes.Any(aporte => string.Equals(aporte.NumeroReferenciaBancaria, referenciaNormalizada, StringComparison.Ordinal)))
            {
                throw new ReferenciaBancariaDuplicadaException(referenciaNormalizada);
            }
        }

        var fechaEfectiva = (fechaRegistro ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var correlacionEfectiva = correlacionId.HasValue && correlacionId.Value != Guid.Empty
            ? correlacionId.Value
            : UuidV7.Nuevo(fechaEfectiva);

        var aporte = AporteDonante.CrearConfirmado(
            Id,
            montoAporte,
            moneda,
            periodoAcademicoId,
            fechaAporte,
            bancoEmisor,
            numeroReferenciaBancaria,
            urlComprobanteFirebase,
            registradoPorUsuarioId,
            observaciones,
            fechaEfectiva,
            correlacionEfectiva);

        _aportes.Add(aporte);
        SaldoTotalHistorico += aporte.MontoAporte;
        SaldoDisponibleAsignacion += aporte.MontoAporte;

        ActualizarAuditoria(registradoPorUsuarioId, fechaEfectiva);

        RegistrarEventoDominio(new AporteDonanteRegistradoDominioEvent(
            aporte.Id,
            Id,
            aporte.MontoAporte,
            aporte.Moneda,
            aporte.PeriodoAcademicoId,
            aporte.FechaAporte,
            aporte.NumeroReferenciaBancaria,
            aporte.UrlComprobanteFirebase,
            SaldoTotalHistorico,
            SaldoDisponibleAsignacion,
            correlacionEfectiva,
            fechaEfectiva));

        return aporte;
    }
}

