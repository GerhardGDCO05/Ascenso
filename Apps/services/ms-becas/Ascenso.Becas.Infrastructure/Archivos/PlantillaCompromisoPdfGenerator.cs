using System.Globalization;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace Ascenso.Becas.Infrastructure.Archivos;

/// <summary>
/// Genera la plantilla del compromiso del becario con PdfSharpCore. El
/// documento incluye las condiciones del programa y de la beca, usa una fuente
/// embebida y normaliza sus metadatos para que el mismo conjunto de datos
/// produzca siempre el mismo hash.
/// </summary>
public sealed class PlantillaCompromisoPdfGenerator : IPlantillaCompromisoPdfGenerator
{
    private const string TituloDocumento = "Plantilla de compromiso del becario";

    public Task<Stream> GenerarAsync(
        DatosPlantillaCompromiso datos,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datos);
        cancellationToken.ThrowIfCancellationRequested();

        ResolutorFuenteCarta.AsegurarRegistrado();

        using var documento = new PdfDocument();
        documento.Info.Title = TituloDocumento;
        documento.Info.Author = DocumentoPdfComun.Fundacion;
        documento.Info.Subject = $"Compromiso del becario para la beca {datos.BecaId}";
        documento.Info.Creator = "MS-BEC";

        var fechaCreacion = datos.FechaEmision
            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        documento.Info.CreationDate = fechaCreacion;
        documento.Info.ModificationDate = fechaCreacion;

        var pagina = documento.AddPage();
        pagina.Size = PageSize.A4;

        using (var grafico = XGraphics.FromPdfPage(pagina))
        {
            DibujarPlantilla(grafico, datos);
        }

        using var memoria = new MemoryStream();
        documento.Save(memoria, false);

        var pdf = DocumentoPdfComun.Normalizar(memoria.ToArray());
        return Task.FromResult<Stream>(new MemoryStream(pdf, writable: false));
    }

    private static void DibujarPlantilla(
        XGraphics grafico,
        DatosPlantillaCompromiso datos)
    {
        var titulo = new XFont(ResolutorFuenteCarta.Familia, 18, XFontStyle.Bold);
        var seccion = new XFont(ResolutorFuenteCarta.Familia, 12, XFontStyle.Bold);
        var cuerpo = new XFont(ResolutorFuenteCarta.Familia, 10.5, XFontStyle.Regular);
        var pie = new XFont(ResolutorFuenteCarta.Familia, 8, XFontStyle.Regular);

        var y = 58d;

        y = Escribir(grafico, DocumentoPdfComun.Fundacion, titulo, y);
        y = Escribir(grafico, "COMPROMISO DEL BECARIO", seccion, y + 4);

        y += 12;
        y = Escribir(grafico, $"Código de beca: {datos.CodigoBeca}", cuerpo, y);
        y = Escribir(
            grafico,
            $"Fecha de emisión: {datos.FechaEmision.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}",
            cuerpo,
            y);

        y += 10;
        y = Escribir(grafico, "DATOS DEL BECARIO", seccion, y);
        y = Escribir(grafico, $"Estudiante: {datos.NombreEstudiante}", cuerpo, y);
        y = Escribir(grafico, $"Documento: {datos.NumeroDocumentoEstudiante}", cuerpo, y);
        y = Escribir(grafico, $"Programa: {datos.NombrePrograma}", cuerpo, y);
        y = Escribir(grafico, $"Periodo académico: {datos.PeriodoCodigo}", cuerpo, y);

        y += 10;
        y = Escribir(grafico, "CONDICIONES DE LA BECA", seccion, y);
        y = Escribir(
            grafico,
            $"Cobertura: {DocumentoPdfComun.DescribirCobertura(datos.Cobertura)}",
            cuerpo,
            y);
        y = Escribir(
            grafico,
            $"Monto por periodo: USD {datos.MontoPorPeriodo.ToString("N2", CultureInfo.InvariantCulture)}",
            cuerpo,
            y);
        y = Escribir(
            grafico,
            $"Duración: {datos.DuracionPeriodos} periodos académicos",
            cuerpo,
            y);

        y += 10;
        y = Escribir(grafico, "CONDICIONES DE PERMANENCIA", seccion, y);
        y = Escribir(
            grafico,
            $"Promedio mínimo de renovación: {datos.Condiciones.PromedioMinimoRenovacion.ToString("0.00", CultureInfo.InvariantCulture)} / 20",
            cuerpo,
            y);
        y = Escribir(
            grafico,
            $"Materias mínimas aprobadas: {datos.Condiciones.MateriasMinimasAprobadas}",
            cuerpo,
            y);
        y = Escribir(
            grafico,
            $"Máximo de materias reprobadas: {datos.Condiciones.MaximoMateriasReprobadas}",
            cuerpo,
            y);
        y = Escribir(
            grafico,
            $"Horas de servicio por periodo: {datos.Condiciones.HorasServicioPorPeriodo}",
            cuerpo,
            y);
        y = Escribir(
            grafico,
            $"Consecuencia del incumplimiento: {DescribirConsecuencia(datos.Condiciones.ConsecuenciaIncumplimiento)}",
            cuerpo,
            y);

        y += 10;
        y = Escribir(grafico, "OBLIGACIONES DEL BECARIO", seccion, y);
        y = Escribir(grafico, "El becario se compromete a:", cuerpo, y);
        y = Escribir(
            grafico,
            "- Mantener el rendimiento académico y las condiciones de permanencia.",
            cuerpo,
            y);
        y = Escribir(
            grafico,
            "- Cumplir las horas de servicio definidas por el programa.",
            cuerpo,
            y);
        y = Escribir(
            grafico,
            "- Informar cualquier cambio en su situación académica o personal.",
            cuerpo,
            y);
        y = Escribir(
            grafico,
            "- Presentar la documentación de seguimiento en los plazos establecidos.",
            cuerpo,
            y);

        y += 20;
        y = Escribir(grafico, "Firma del becario: ______________________________", cuerpo, y);
        y = Escribir(grafico, $"Nombre: {datos.NombreEstudiante}", cuerpo, y);
        y = Escribir(grafico, $"Documento: {datos.NumeroDocumentoEstudiante}", cuerpo, y);

        grafico.DrawString(
            $"Beca: {datos.BecaId}",
            pie,
            XBrushes.Black,
            new XRect(DocumentoPdfComun.Margen, 800, DocumentoPdfComun.AnchoContenido, 14),
            XStringFormats.TopLeft);
    }

    private static double Escribir(
        XGraphics grafico,
        string texto,
        XFont fuente,
        double y)
    {
        grafico.DrawString(
            texto,
            fuente,
            XBrushes.Black,
            new XRect(DocumentoPdfComun.Margen, y, DocumentoPdfComun.AnchoContenido, 18),
            XStringFormats.TopLeft);

        return y + 18;
    }

    private static string DescribirConsecuencia(ConsecuenciaPermanencia consecuencia)
        => consecuencia switch
        {
            ConsecuenciaPermanencia.LlamadoAtencion => "Llamado de atención",
            ConsecuenciaPermanencia.Suspension => "Suspensión de la beca",
            ConsecuenciaPermanencia.Revocacion => "Revocación de la beca",
            _ => consecuencia.ToString()
        };
}
