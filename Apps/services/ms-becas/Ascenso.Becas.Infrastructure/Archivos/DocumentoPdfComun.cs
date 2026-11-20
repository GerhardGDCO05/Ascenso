using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Infrastructure.Archivos;

/// <summary>
/// Utilidades compartidas por los generadores de PDF: medidas de página,
/// descripción de la cobertura y normalización determinista de los bytes para
/// que el mismo contenido produzca siempre el mismo hash.
/// </summary>
internal static partial class DocumentoPdfComun
{
    internal const string Fundacion = "Fundación Ascenso";
    internal const double Margen = 56;
    internal const double AnchoPagina = 595;
    internal const double AnchoContenido = AnchoPagina - (Margen * 2);

    internal static string DescribirCobertura(PoliticaCobertura cobertura)
    {
        ArgumentNullException.ThrowIfNull(cobertura);

        return cobertura.Modalidad switch
        {
            TipoCobertura.Total =>
                $"Total ({cobertura.PorcentajeCobertura.ToString("0.##", CultureInfo.InvariantCulture)} %)",
            TipoCobertura.Parcial =>
                $"Parcial ({cobertura.PorcentajeCobertura.ToString("0.##", CultureInfo.InvariantCulture)} %)",
            TipoCobertura.MontoFijo =>
                $"Monto fijo de USD {cobertura.MontoFijoPeriodo.ToString("N2", CultureInfo.InvariantCulture)} por periodo",
            _ => cobertura.Modalidad.ToString()
        };
    }

    internal static byte[] Normalizar(byte[] pdf)
    {
        var texto = Encoding.Latin1.GetString(pdf);

        var fuente = PatronFuente().Match(texto);
        if (fuente.Success)
        {
            texto = texto.Replace(
                fuente.Groups[1].Value + "+",
                "AAAAAA+",
                StringComparison.Ordinal);
        }

        texto = PatronIdentificador().Replace(
            texto,
            "/ID [<00000000000000000000000000000000><00000000000000000000000000000000>]");

        var huella = SHA256.HashData(Encoding.Latin1.GetBytes(texto));
        var identificador = Convert.ToHexString(huella)[..32];

        texto = texto.Replace(
            new string('0', 32),
            identificador,
            StringComparison.Ordinal);

        return Encoding.Latin1.GetBytes(texto);
    }

    [GeneratedRegex(@"/FontName /([A-Z]{6})\+", RegexOptions.CultureInvariant)]
    private static partial Regex PatronFuente();

    [GeneratedRegex(
        @"/ID \[<[0-9A-Fa-f]{32}><[0-9A-Fa-f]{32}>\]",
        RegexOptions.CultureInvariant)]
    private static partial Regex PatronIdentificador();
}
