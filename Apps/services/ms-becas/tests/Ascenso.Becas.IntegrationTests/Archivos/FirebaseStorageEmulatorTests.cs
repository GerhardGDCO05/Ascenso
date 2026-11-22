using System.Text;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Archivos;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.IntegrationTests.Archivos;

public sealed class FirebaseStorageEmulatorTests
{
    private const string Bucket = "demo-ascenso.firebasestorage.app";
    private static readonly string _urlBase =
        $"http://{Environment.GetEnvironmentVariable("STORAGE_EMULATOR_HOST") ?? "127.0.0.1:9199"}";

    [Theory]
    [InlineData("becas/beca-1/ofertas/oferta-1/carta-otorgamiento.pdf")]
    [InlineData("becas/beca-1/compromisos/compromiso-1/compromiso-firmado.pdf")]
    public async Task Debe_SubirYLeerElArchivo_UsandoElEmulador(
        string rutaLogica)
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Firebase",
                ["Storage:Firebase:Bucket"] = Bucket,
                ["Storage:Firebase:UsaEmulador"] = "true",
                ["Storage:Firebase:UrlBase"] = _urlBase
            })
            .Build();
        var servicios = new ServiceCollection();
        servicios.AgregarArchivos(configuracion);

        using var proveedor = servicios.BuildServiceProvider();
        var almacenamiento = proveedor.GetRequiredService<IAlmacenamientoArchivos>();
        var contenido = Encoding.UTF8.GetBytes("%PDF-1.7 archivo de prueba");

        var almacenado = await almacenamiento.SubirAsync(
            new MemoryStream(contenido),
            "application/pdf",
            rutaLogica);
        var urlLectura = await almacenamiento.ResolverUrlLecturaAsync(almacenado.Url);

        using var clienteHttp = new HttpClient();
        var contenidoLeido = await clienteHttp.GetByteArrayAsync(urlLectura);

        contenidoLeido.Should().Equal(contenido);
        almacenado.Url.Should().Be($"gs://{Bucket}/{rutaLogica}");
        urlLectura.Should().Contain("/download/storage/v1/b/");

        await almacenamiento.EliminarAsync(almacenado.Url);
        var respuestaLuegoDeEliminar = await clienteHttp.GetAsync(urlLectura);
        respuestaLuegoDeEliminar.StatusCode.Should().Be(
            System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Debe_DenegarLecturaYEscrituraAnonimas_SegunLasReglasDeStorage()
    {
        using var clienteHttp = new HttpClient();

        var lectura = await clienteHttp.GetAsync(
            $"{_urlBase}/v0/b/{Bucket}/o");

        using var contenido = new ByteArrayContent("%PDF-1.7 carga anónima"u8.ToArray());
        contenido.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/pdf");
        var escritura = await clienteHttp.PostAsync(
            $"{_urlBase}/v0/b/{Bucket}/o?uploadType=media&name=pruebas/anonimo.pdf",
            contenido);

        lectura.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
        escritura.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }
}
