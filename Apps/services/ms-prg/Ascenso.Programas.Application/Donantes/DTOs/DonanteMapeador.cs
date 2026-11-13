using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Donantes.Entidades;
using Ascenso.Programas.Domain.Enums;

namespace Ascenso.Programas.Application.Donantes.DTOs;

internal static class DonanteMapeador
{
    public static DonanteResumenDto AResumenDto(this Donante donante)
        => new(
            donante.Id,
            donante.TipoDonante.ACodigo(),
            donante.RazonSocialNombre,
            donante.IdentificacionFiscal,
            donante.Email,
            donante.EstadoDonante.ACodigo(),
            donante.Moneda,
            donante.SaldoTotalHistorico,
            donante.SaldoDisponibleAsignacion,
            donante.FechaCreacion,
            donante.Version);

    public static DonanteDetalleDto ADetalleDto(this Donante donante)
        => new(
            donante.Id,
            donante.TipoDonante.ACodigo(),
            donante.RazonSocialNombre,
            donante.IdentificacionFiscal,
            donante.Email,
            donante.Telefono,
            donante.DireccionFiscal,
            new ContactoDonanteDto(
                donante.ContactoPrincipal.NombreRepresentante,
                donante.ContactoPrincipal.Cargo,
                donante.ContactoPrincipal.EmailContacto,
                donante.ContactoPrincipal.TelefonoContacto),
            donante.EstadoDonante.ACodigo(),
            donante.Moneda,
            donante.SaldoTotalHistorico,
            donante.SaldoDisponibleAsignacion,
            donante.FechaCreacion,
            donante.UsuarioCreacionId,
            donante.FechaUltimaModificacion,
            donante.UsuarioModificacionId,
            donante.Version);

    public static AporteDonanteDto ADto(this AporteDonante aporte)
        => new(
            aporte.Id,
            aporte.DonanteId,
            aporte.MontoAporte,
            aporte.Moneda,
            aporte.PeriodoAcademicoId,
            aporte.FechaAporte,
            aporte.BancoEmisor,
            aporte.NumeroReferenciaBancaria,
            aporte.UrlComprobanteFirebase,
            aporte.EstadoAporte.ACodigo(),
            aporte.Observaciones,
            aporte.FechaCreacion,
            aporte.RegistradoPorUsuarioId);

    public static bool IntentarConvertirEnum<TEnum>(string? valor, out TEnum resultado)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            resultado = default;
            return false;
        }

        var limpio = valor.Trim();

        if (Enum.TryParse(limpio, ignoreCase: true, out resultado) && Enum.IsDefined(resultado))
        {
            return true;
        }

        try
        {
            resultado = CodigoEnum.DesdeCodigo<TEnum>(limpio);
            return true;
        }
        catch (ArgumentException)
        {
            resultado = default;
            return false;
        }
    }
}
