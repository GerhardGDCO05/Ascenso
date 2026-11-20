using System.Globalization;
using Ascenso.Becas.Domain.Ports;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace Ascenso.Becas.Infrastructure.Archivos;

/// <summary>
/// Genera la carta de otorgamiento en PDF con PdfSharpCore. El documento usa
/// una fuente embebida y metadatos deterministas para que el mismo conjunto de
/// datos produzca siempre el mismo hash.
/// </summary>
public sealed class CartaOfertaPdfGenerator : ICartaOfertaPdfGenerator
{
    private const string TituloDocumento = "Carta de otorgamiento de beca";

    public Task<Stream> GenerarAsync(
        DatosCartaOferta datos,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datos);
        cancellationToken.ThrowIfCancellationRequested();

        ResolutorFuenteCarta.AsegurarRegistrado();

        using var documento = new PdfDocument();
        documento.Info.Title = TituloDocumento;
        documento.Info.Author = DocumentoPdfComun.Fundacion;
        documento.Info.Subject = $"Oferta de beca {datos.OfertaId}";
        documento.Info.Creator = "MS-BEC";

        var fechaCreacion = datos.FechaVencimientoAceptacion
            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            .AddDays(-15);
        documento.Info.CreationDate = fechaCreacion;
        documento.Info.ModificationDate = fechaCreacion;

        var pagina = documento.AddPage();
        pagina.Size = PageSize.A4;

        using (var grafico = XGraphics.FromPdfPage(pagina))
        {
            DibujarCarta(grafico, datos);
        }

        using var memoria = new MemoryStream();
        documento.Save(memoria, false);

        var pdf = DocumentoPdfComun.Normalizar(memoria.ToArray());
        return Task.FromResult<Stream>(new MemoryStream(pdf, writable: false));
    }

    private static void DibujarCarta(XGraphics grafico, DatosCartaOferta datos)
    {
        var titulo = new XFont(ResolutorFuenteCarta.Familia, 18, XFontStyle.Bold);
        var subtitulo = new XFont(ResolutorFuenteCarta.Familia, 12, XFontStyle.Bold);
        var cuerpo = new XFont(ResolutorFuenteCarta.Familia, 11, XFontStyle.Regular);
        var pie = new XFont(ResolutorFuenteCarta.Familia, 8, XFontStyle.Regular);

        var y = 58d;

        grafico.DrawString(
            DocumentoPdfComun.Fundacion,
            titulo,
            XBrushes.Black,
            new XRect(
                DocumentoPdfComun.Margen,
                y,
                DocumentoPdfComun.AnchoContenido,
                26),
            XStringFormats.TopLeft);

        y += 32;
        grafico.DrawString(
            "CARTA DE OTORGAMIENTO DE BECA",
            subtitulo,
            XBrushes.Black,
            new XRect(
                DocumentoPdfComun.Margen,
                y,
                DocumentoPdfComun.AnchoContenido,
                20),
            XStringFormats.TopLeft);

        y += 36;
        var lineas = new[]
        {
            $"Estudiante: {datos.NombreEstudiante}",
            $"Programa: {datos.NombrePrograma}",
            $"Periodo académico: {datos.PeriodoCodigo}",
            $"Cobertura: {DocumentoPdfComun.DescribirCobertura(datos.Cobertura)}",
            $"Monto por periodo: USD {datos.MontoOfertaPorPeriodo.ToString("N2", CultureInfo.InvariantCulture)}",
            $"Vigencia de la oferta: hasta el {datos.FechaVencimientoAceptacion.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}"
        };

        foreach (var linea in lineas)
        {
            grafico.DrawString(
                linea,
                cuerpo,
                XBrushes.Black,
                new XRect(
                    DocumentoPdfComun.Margen,
                    y,
                    DocumentoPdfComun.AnchoContenido,
                    18),
                XStringFormats.TopLeft);
            y += 24;
        }

        y += 10;
        grafico.DrawString(
            "La Fundación Ascenso otorga la beca descrita para el periodo indicado,",
            cuerpo,
            XBrushes.Black,
            new XRect(
                DocumentoPdfComun.Margen,
                y,
                DocumentoPdfComun.AnchoContenido,
                18),
            XStringFormats.TopLeft);
        y += 20;
        grafico.DrawString(
            "sujeta a las condiciones de permanencia y a la aceptación dentro del plazo.",
            cuerpo,
            XBrushes.Black,
            new XRect(
                DocumentoPdfComun.Margen,
                y,
                DocumentoPdfComun.AnchoContenido,
                18),
            XStringFormats.TopLeft);

        grafico.DrawString(
            $"Beca: {datos.BecaId}  |  Oferta: {datos.OfertaId}",
            pie,
            XBrushes.Black,
            new XRect(
                DocumentoPdfComun.Margen,
                780,
                DocumentoPdfComun.AnchoContenido,
                14),
            XStringFormats.TopLeft);
    }
}
