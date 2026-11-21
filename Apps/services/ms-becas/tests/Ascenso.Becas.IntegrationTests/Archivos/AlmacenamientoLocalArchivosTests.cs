using System.Security.Cryptography;
using System.Text;
using Ascenso.Becas.Infrastructure.Archivos;
using FluentAssertions;

namespace Ascenso.Becas.IntegrationTests.Archivos;

public sealed class AlmacenamientoLocalArchivosTests : IDisposable
{
    private readonly string _raiz;

    public AlmacenamientoLocalArchivosTests()
    {
        _raiz = Path.GetFullPath(
            Path.Combine(
                Path.GetTempPath(),
                "ascenso-becas-archivos",
                Guid.NewGuid().ToString("N")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_raiz))
        {
            Directory.Delete(_raiz, recursive: true);
        }
    }

    [Fact]
    public async Task Debe_SubirElArchivo_CalcularHashYTamanioYDevolverUrlLocal()
    {
        var almacenamiento = CrearAlmacenamiento();
        var contenido = Encoding.UTF8.GetBytes("%PDF-1.7 contenido de la carta");

        var resultado = await almacenamiento.SubirAsync(
            new MemoryStream(contenido),
            "application/pdf",
            "becas/beca-1/ofertas/oferta-1/carta-otorgamiento.pdf");

        var hashEsperado = Convert.ToHexString(SHA256.HashData(contenido))
            .ToLowerInvariant();

        resultado.HashSha256.Should().Be(hashEsperado);
        resultado.TamanioBytes.Should().Be(contenido.Length);

        var ruta = new Uri(resultado.Url).LocalPath;
        ruta.Should().Be(
            Path.Combine(
                _raiz,
                "becas",
                "beca-1",
                "ofertas",
                "oferta-1",
                "carta-otorgamiento.pdf"));
        (await File.ReadAllBytesAsync(ruta)).Should().Equal(contenido);
    }

    [Fact]
    public async Task Debe_DevolverLaUrlBaseConfigurada_Cuando_SeDefine()
    {
        var almacenamiento = CrearAlmacenamiento("https://cdn.example/archivos");
        var contenido = Encoding.UTF8.GetBytes("carta");

        var resultado = await almacenamiento.SubirAsync(
            new MemoryStream(contenido),
            "application/pdf",
            "becas/beca 1/carta.pdf");

        resultado.Url.Should().Be(
            "https://cdn.example/archivos/becas/beca%201/carta.pdf");
        File.Exists(Path.Combine(_raiz, "becas", "beca 1", "carta.pdf"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Debe_EliminarElArchivo_Cuando_LaUrlPerteneceAlAlmacenamiento()
    {
        var almacenamiento = CrearAlmacenamiento();
        var contenido = Encoding.UTF8.GetBytes("carta");

        var resultado = await almacenamiento.SubirAsync(
            new MemoryStream(contenido),
            "application/pdf",
            "becas/beca-1/carta.pdf");

        var ruta = new Uri(resultado.Url).LocalPath;
        File.Exists(ruta).Should().BeTrue();

        await almacenamiento.EliminarAsync(resultado.Url);

        File.Exists(ruta).Should().BeFalse();
    }

    [Fact]
    public async Task Debe_EliminarElArchivo_Cuando_SeUsaLaUrlBaseConfigurada()
    {
        var almacenamiento = CrearAlmacenamiento("https://cdn.example/archivos");
        var contenido = Encoding.UTF8.GetBytes("carta");

        var resultado = await almacenamiento.SubirAsync(
            new MemoryStream(contenido),
            "application/pdf",
            "becas/beca-1/carta.pdf");

        await almacenamiento.EliminarAsync(resultado.Url);

        File.Exists(Path.Combine(_raiz, "becas", "beca-1", "carta.pdf"))
            .Should().BeFalse();
    }

    [Fact]
    public async Task Debe_SerIdempotente_Cuando_ElArchivoYaNoExiste()
    {
        var almacenamiento = CrearAlmacenamiento();

        var accion = () => almacenamiento.EliminarAsync(
            new Uri(Path.Combine(_raiz, "becas", "inexistente.pdf")).AbsoluteUri);

        await accion.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("../fuera.pdf")]
    [InlineData("becas/../../fuera.pdf")]
    [InlineData("./carta.pdf")]
    [InlineData("/etc/passwd")]
    public async Task Debe_RechazarLaSubida_Cuando_LaRutaEscapaDeLaRaiz(string ruta)
    {
        var almacenamiento = CrearAlmacenamiento();

        var accion = () => almacenamiento.SubirAsync(
            new MemoryStream(Encoding.UTF8.GetBytes("carta")),
            "application/pdf",
            ruta);

        await accion.Should().ThrowAsync<ArgumentException>();
        Directory.Exists(_raiz).Should().BeTrue();
        Directory.GetFiles(_raiz).Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_RechazarLaEliminacion_Cuando_LaUrlEsAjena()
    {
        var almacenamiento = CrearAlmacenamiento();

        var accion = () => almacenamiento.EliminarAsync(
            "https://example.com/archivos/carta.pdf");

        await accion.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Debe_RechazarLaEliminacion_Cuando_LaRutaAbsolutaEstaFueraDeLaRaiz()
    {
        var almacenamiento = CrearAlmacenamiento();
        var fuera = Path.GetFullPath(
            Path.Combine(_raiz, "..", "otro-archivo.pdf"));

        var accion = () => almacenamiento.EliminarAsync(
            new Uri(fuera).AbsoluteUri);

        await accion.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Debe_RechazarLaConfiguracion_Cuando_LaRutaBaseEstaVacia()
    {
        var accion = () => new AlmacenamientoLocalArchivos(
            new OpcionesAlmacenamiento
            {
                Local = new OpcionesAlmacenamientoLocal { RutaBase = " " }
            });

        accion.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Debe_RechazarLaConfiguracion_Cuando_LaUrlBaseNoEsHttp()
    {
        var accion = () => new AlmacenamientoLocalArchivos(
            new OpcionesAlmacenamiento
            {
                Local = new OpcionesAlmacenamientoLocal
                {
                    RutaBase = _raiz,
                    UrlBase = "ftp://cdn.example/archivos"
                }
            });

        accion.Should().Throw<InvalidOperationException>();
    }

    private AlmacenamientoLocalArchivos CrearAlmacenamiento(string? urlBase = null)
        => new(
            new OpcionesAlmacenamiento
            {
                Local = new OpcionesAlmacenamientoLocal
                {
                    RutaBase = _raiz,
                    UrlBase = urlBase
                }
            });
}
