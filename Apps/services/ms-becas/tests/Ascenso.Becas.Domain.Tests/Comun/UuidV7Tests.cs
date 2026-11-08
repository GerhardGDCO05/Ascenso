using Ascenso.Becas.Domain.Comun;

namespace Ascenso.Becas.Domain.Tests.Comun;

public sealed class UuidV7Tests
{
    [Fact]
    public void Debe_GenerarUnUuidVersion7_Cuando_SeSolicitaUnNuevoIdentificador()
    {
        var id = UuidV7.Nuevo();

        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(7, id.Version);
        Assert.True(UuidV7.EsVersion7(id));
    }

    [Fact]
    public void Debe_SerOrdenablePorTiempo_Cuando_SeGeneranIdentificadoresConMarcasCrecientes()
    {
        var origen = new DateTimeOffset(2026, 11, 7, 0, 0, 0, TimeSpan.Zero);
        var identificadores = Enumerable.Range(0, 20)
            .Select(indice => UuidV7.Nuevo(origen.AddMilliseconds(indice)))
            .ToArray();

        var representaciones = identificadores
            .Select(id => id.ToString("D"))
            .ToArray();

        Assert.Equal(representaciones.OrderBy(valor => valor, StringComparer.Ordinal).ToArray(), representaciones);
    }

    [Fact]
    public void Debe_ReconocerIdentificadoresQueNoSonVersion7()
    {
        Assert.False(UuidV7.EsVersion7(Guid.NewGuid()));
    }
}
