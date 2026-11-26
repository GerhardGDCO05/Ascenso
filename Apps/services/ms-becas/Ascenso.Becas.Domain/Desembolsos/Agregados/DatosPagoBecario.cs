using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Domain.Desembolsos.Agregados;

public sealed class DatosPagoBecario : AggregateRoot
{
    private DatosPagoBecario() { }

    private DatosPagoBecario(Guid id, ContextoOperacion contexto)
        : base(id, contexto.Fecha, contexto.UsuarioAutorId) { }

    public Guid PostulanteEstudianteId { get; private set; }
    public Guid BecaId { get; private set; }
    public string NombreBanco { get; private set; } = string.Empty;
    public string CodigoBanco { get; private set; } = string.Empty;
    public TipoCuentaBancaria TipoCuenta { get; private set; }
    public string NumeroCuentaCifradoAesGcm { get; private set; } = string.Empty;
    public string DocumentoTitular { get; private set; } = string.Empty;
    public EstadoDatosPago Estado { get; private set; }
    public Guid ModificadoPorUsuarioId { get; private set; }
    public Guid? AutorizacionOtpModificacionId { get; private set; }

    public static DatosPagoBecario Crear(
        Guid estudianteId, Guid becaId, string nombreBanco, string codigoBanco,
        TipoCuentaBancaria tipoCuenta, string numeroCuenta, string documentoTitular,
        Guid autorizacionOtpId, ICifradorCuentaBancaria cifrador, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(cifrador);
        ArgumentNullException.ThrowIfNull(contexto);
        if (estudianteId == Guid.Empty || becaId == Guid.Empty || autorizacionOtpId == Guid.Empty
            || string.IsNullOrWhiteSpace(nombreBanco) || string.IsNullOrWhiteSpace(codigoBanco)
            || string.IsNullOrWhiteSpace(numeroCuenta) || string.IsNullOrWhiteSpace(documentoTitular)
            || !Enum.IsDefined(tipoCuenta))
        {
            throw new ArgumentException("Los datos de pago y la autorización OTP son obligatorios.");
        }

        var cifrado = cifrador.Cifrar(numeroCuenta.Trim());
        if (string.IsNullOrWhiteSpace(cifrado) || cifrado.Contains(numeroCuenta.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("El cifrador no devolvió un número de cuenta protegido.");
        }

        return new DatosPagoBecario(UuidV7.Nuevo(), contexto)
        {
            PostulanteEstudianteId = estudianteId,
            BecaId = becaId,
            NombreBanco = nombreBanco.Trim(),
            CodigoBanco = codigoBanco.Trim(),
            TipoCuenta = tipoCuenta,
            NumeroCuentaCifradoAesGcm = cifrado,
            DocumentoTitular = documentoTitular.Trim(),
            Estado = EstadoDatosPago.Activo,
            ModificadoPorUsuarioId = contexto.UsuarioAutorId,
            AutorizacionOtpModificacionId = autorizacionOtpId
        };
    }

    public void Reemplazar(ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        if (contexto.AutorizacionOtpId is null)
        {
            throw new InvalidOperationException("La modificación de los datos de pago requiere autorización OTP.");
        }

        if (Estado != EstadoDatosPago.Activo)
        {
            throw new InvalidOperationException("La cuenta ya fue reemplazada.");
        }

        Estado = EstadoDatosPago.Reemplazado;
        ModificadoPorUsuarioId = contexto.UsuarioAutorId;
        AutorizacionOtpModificacionId = contexto.AutorizacionOtpId;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }
}
