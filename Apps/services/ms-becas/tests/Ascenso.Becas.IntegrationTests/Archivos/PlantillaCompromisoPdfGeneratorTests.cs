using System.Security.Cryptography;
using System.Text;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.ValueObjects;
using Ascenso.Becas.Infrastructure.Archivos;
using FluentAssertions;

namespace Ascenso.Becas.IntegrationTests.Archivos;

public sealed class PlantillaCompromisoPdfGeneratorTests
{
    private static readonly DatosPlantillaCompromiso _datos = new(
        Guid.CreateVersion7(),
        "BECA-2026-0001",
        "Ana Pérez",
        "12345678",
        "Ingeniería de Software",
        "2026-1",
        new PoliticaCobertura(TipoCobertura.Parcial, 50m, 0m, "USD"),
        750m,
        4,
        new CondicionesPermanencia(
            14m,
            4,
            1,
            80,
            ConsecuenciaPermanencia.Suspension),
        new DateOnly(2026, 11, 20));

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
    public async Task Debe_GenerarUnHashDistinto_Cuando_CambianLasCondiciones()
    {
        var primero = await GenerarAsync(_datos);
        var segundo = await GenerarAsync(_datos with
        {
            Condiciones = new CondicionesPermanencia(
                16m,
                5,
                0,
                40,
                ConsecuenciaPermanencia.Revocacion)
        });

        SHA256.HashData(primero).Should()
            .NotEqual(SHA256.HashData(segundo));
    }

    [Fact]
    public async Task Debe_GenerarUnHashDistinto_Cuando_CambiaLaCobertura()
    {
        var primero = await GenerarAsync(_datos);
        var segundo = await GenerarAsync(_datos with
        {
            Cobertura = new PoliticaCobertura(
                TipoCobertura.MontoFijo,
                0m,
                500m,
                "USD")
        });

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
        var generador = new PlantillaCompromisoPdfGenerator();
        using var cancelacion = new CancellationTokenSource();
        await cancelacion.CancelAsync();

        var accion = () => generador.GenerarAsync(_datos, cancelacion.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();
    }

    private static async Task<byte[]> GenerarAsync(DatosPlantillaCompromiso datos)
    {
        var generador = new PlantillaCompromisoPdfGenerator();

        await using var pdf = await generador.GenerarAsync(datos);
        using var memoria = new MemoryStream();
        await pdf.CopyToAsync(memoria);

        return memoria.ToArray();
    }
}
