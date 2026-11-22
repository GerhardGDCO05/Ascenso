using System.Security.Cryptography;
using System.Text;
using Ascenso.Becas.Infrastructure.Archivos;
using FluentAssertions;

namespace Ascenso.Becas.IntegrationTests.Archivos;

public sealed class AlmacenamientoFirebaseArchivosTests
{
    private const string Bucket = "bucket-pruebas";

    [Fact]
    public async Task Debe_SubirElObjeto_CalcularHashYTamanioYDevolverUnaReferenciaEstable()
    {
        var objetos = new AlmacenObjetosNubeFalso();
        var almacenamiento = CrearAlmacenamiento(objetos);
        var contenido = Encoding.UTF8.GetBytes("%PDF-1.7 carta de otorgamiento");

        var resultado = await almacenamiento.SubirAsync(
            new MemoryStream(contenido),
            "application/pdf",
            "becas/beca-1/ofertas/oferta-1/carta-otorgamiento.pdf");

        var hashEsperado = Convert.ToHexString(SHA256.HashData(contenido))
            .ToLowerInvariant();

        resultado.HashSha256.Should().Be(hashEsperado);
        resultado.TamanioBytes.Should().Be(contenido.Length);
        resultado.Url.Should().Be(
            $"gs://{Bucket}/becas/beca-1/ofertas/oferta-1/carta-otorgamiento.pdf");

        var subida = objetos.Subidas.Should().ContainSingle().Subject;
        subida.Bucket.Should().Be(Bucket);
        subida.Objeto.Should().Be(
            "becas/beca-1/ofertas/oferta-1/carta-otorgamiento.pdf");
        subida.TipoMime.Should().Be("application/pdf");
        subida.Contenido.Should().Equal(contenido);
    }

    [Fact]
    public async Task Debe_EscaparLosSegmentosDeLaRutaEnLaUrl()
    {
        var almacenamiento = CrearAlmacenamiento(new AlmacenObjetosNubeFalso());

        var resultado = await almacenamiento.SubirAsync(
            new MemoryStream(Encoding.UTF8.GetBytes("carta")),
            "application/pdf",
            "becas/beca 1/carta.pdf");

        resultado.Url.Should().Be($"gs://{Bucket}/becas/beca%201/carta.pdf");
    }

    [Fact]
    public async Task Debe_FirmarElObjetoPorQuinceMinutos_SinCambiarLaReferenciaPersistible()
    {
        var objetos = new AlmacenObjetosNubeFalso();
        var almacenamiento = CrearAlmacenamiento(objetos);

        var archivo = await almacenamiento.SubirAsync(
            new MemoryStream(Encoding.UTF8.GetBytes("carta")),
            "application/pdf",
            "becas/beca-1/carta.pdf");

        var urlFirmada = await almacenamiento.ResolverUrlLecturaAsync(archivo.Url);

        urlFirmada.Should().Be("https://firmada.example/bucket-pruebas/becas%2Fbeca-1%2Fcarta.pdf");
        objetos.Firmas.Should().ContainSingle().Which.Should().Be(
            new Firma(Bucket, "becas/beca-1/carta.pdf", TimeSpan.FromMinutes(15)));
        archivo.Url.Should().Be($"gs://{Bucket}/becas/beca-1/carta.pdf");
    }

    [Theory]
    [InlineData(
        "https://storage.googleapis.com/bucket-pruebas/becas/beca-1/carta.pdf")]
    [InlineData("gs://bucket-pruebas/becas/beca-1/carta.pdf")]
    [InlineData(
        "https://firebasestorage.googleapis.com/v0/b/bucket-pruebas/o/becas%2Fbeca-1%2Fcarta.pdf?alt=media")]
    public async Task Debe_EliminarElObjeto_Cuando_LaUrlPerteneceAlBucket(string url)
    {
        var objetos = new AlmacenObjetosNubeFalso();
        var almacenamiento = CrearAlmacenamiento(objetos);

        await almacenamiento.EliminarAsync(url);

        var eliminacion = objetos.Eliminaciones.Should().ContainSingle().Subject;
        eliminacion.Bucket.Should().Be(Bucket);
        eliminacion.Objeto.Should().Be("becas/beca-1/carta.pdf");
    }

    [Theory]
    [InlineData("https://storage.googleapis.com/otro-bucket/carta.pdf")]
    [InlineData("gs://otro-bucket/carta.pdf")]
    [InlineData("https://example.com/carta.pdf")]
    public async Task Debe_RechazarLaEliminacion_Cuando_LaUrlEsAjenaAlBucket(
        string url)
    {
        var almacenamiento = CrearAlmacenamiento(new AlmacenObjetosNubeFalso());

        var accion = () => almacenamiento.EliminarAsync(url);

        await accion.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("../fuera.pdf")]
    [InlineData("becas/../../fuera.pdf")]
    [InlineData("/etc/passwd")]
    public async Task Debe_RechazarLaSubida_Cuando_LaRutaEscapaDeLaRaiz(string ruta)
    {
        var objetos = new AlmacenObjetosNubeFalso();
        var almacenamiento = CrearAlmacenamiento(objetos);

        var accion = () => almacenamiento.SubirAsync(
            new MemoryStream(Encoding.UTF8.GetBytes("carta")),
            "application/pdf",
            ruta);

        await accion.Should().ThrowAsync<ArgumentException>();
        objetos.Subidas.Should().BeEmpty();
    }

    [Fact]
    public void Debe_RechazarLaConfiguracion_Cuando_ElBucketEstaVacio()
    {
        var accion = () => new AlmacenamientoFirebaseArchivos(
            new AlmacenObjetosNubeFalso(),
            new OpcionesAlmacenamiento
            {
                Firebase = new OpcionesAlmacenamientoFirebase { Bucket = " " }
            });

        accion.Should().Throw<InvalidOperationException>();
    }

    private static AlmacenamientoFirebaseArchivos CrearAlmacenamiento(
        IAlmacenObjetosNube objetos,
        string? urlBase = null)
        => new(
            objetos,
            new OpcionesAlmacenamiento
            {
                Firebase = new OpcionesAlmacenamientoFirebase
                {
                    Bucket = Bucket,
                    UrlBase = urlBase
                }
            });

    private sealed class AlmacenObjetosNubeFalso : IAlmacenObjetosNube
    {
        public List<Subida> Subidas { get; } = [];

        public List<Eliminacion> Eliminaciones { get; } = [];

        public List<Firma> Firmas { get; } = [];

        public Task SubirAsync(
            string bucket,
            string objeto,
            string tipoMime,
            Stream contenido,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(contenido);
            cancellationToken.ThrowIfCancellationRequested();

            using var memoria = new MemoryStream();
            contenido.CopyTo(memoria);

            Subidas.Add(new Subida(
                bucket,
                objeto,
                tipoMime,
                memoria.ToArray()));

            return Task.CompletedTask;
        }

        public Task EliminarAsync(
            string bucket,
            string objeto,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Eliminaciones.Add(new Eliminacion(bucket, objeto));
            return Task.CompletedTask;
        }

        public Task<string> CrearUrlFirmadaLecturaAsync(
            string bucket,
            string objeto,
            TimeSpan vigencia,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Firmas.Add(new Firma(bucket, objeto, vigencia));
            return Task.FromResult(
                $"https://firmada.example/{bucket}/{Uri.EscapeDataString(objeto)}");
        }
    }

    private sealed record Subida(
        string Bucket,
        string Objeto,
        string TipoMime,
        byte[] Contenido);

    private sealed record Eliminacion(string Bucket, string Objeto);

    private sealed record Firma(string Bucket, string Objeto, TimeSpan Vigencia);
}
