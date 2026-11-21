using System.Security.Cryptography;
using System.Text;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.ValueObjects;
using Ascenso.Becas.Infrastructure.Archivos;
using FluentAssertions;

namespace Ascenso.Becas.IntegrationTests.Archivos;

public sealed class CartaOfertaPdfGeneratorTests
{
    private static readonly DatosCartaOferta _datos = new(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        "Ana Pérez",
        "Ingeniería de Software",
        "2026-1",
        new PoliticaCobertura(TipoCobertura.Parcial, 50m, 0m, "USD"),
        750m,
        new DateOnly(2026, 4, 30));

    [Fact]
    public async Task Debe_GenerarUnPdfConCabeceraValida()
    {
        var bytes = await GenerarAsync(_datos);

        Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
        bytes.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task Debe_GenerarElMismoContenido_Cuando_LosDatosSonIguales()
    {
        var primero = await GenerarAsync(_datos);
        var segundo = await GenerarAsync(_datos);

        SHA256.HashData(primero).Should().Equal(SHA256.HashData(segundo));
        primero.Should().Equal(segundo);
    }

    [Fact]
    public async Task Debe_GenerarUnHashDistinto_Cuando_LosDatosCambian()
    {
        var primero = await GenerarAsync(_datos);
        var segundo = await GenerarAsync(_datos with { MontoOfertaPorPeriodo = 800m });

        SHA256.HashData(primero).Should()
            .NotEqual(SHA256.HashData(segundo));
    }

    [Fact]
    public async Task Debe_NormalizarElPrefijoDeLaFuenteEmbebida()
    {
        var bytes = await GenerarAsync(_datos);

        Encoding.Latin1.GetString(bytes).Should().Contain("AAAAAA+");
    }

    [Fact]
    public async Task Debe_RespetarLaCancelacion()
    {
        var generador = new CartaOfertaPdfGenerator();
        using var cancelacion = new CancellationTokenSource();
        await cancelacion.CancelAsync();

        var accion = () => generador.GenerarAsync(_datos, cancelacion.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();
    }

    private static async Task<byte[]> GenerarAsync(DatosCartaOferta datos)
    {
        var generador = new CartaOfertaPdfGenerator();

        await using var pdf = await generador.GenerarAsync(datos);
        using var memoria = new MemoryStream();
        await pdf.CopyToAsync(memoria);

        return memoria.ToArray();
    }
}
