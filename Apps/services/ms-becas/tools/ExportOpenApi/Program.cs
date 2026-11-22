using System.Text.Json;
using Ascenso.Becas.IntegrationTests.Controllers;

if (args.Length != 1)
{
    throw new ArgumentException("Indica la ruta de salida del documento OpenAPI.");
}

using var fabrica = new OfertasBecaApiFactory();
using var cliente = fabrica.CreateClient();
using var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");
respuesta.EnsureSuccessStatusCode();

var contenido = await respuesta.Content.ReadAsStringAsync();
using var documento = JsonDocument.Parse(contenido);
var destino = Path.GetFullPath(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
await File.WriteAllTextAsync(
    destino,
    JsonSerializer.Serialize(
        documento.RootElement,
        new JsonSerializerOptions { WriteIndented = true })
    + Environment.NewLine);
