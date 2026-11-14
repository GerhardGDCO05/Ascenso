using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Tests.Comun;

public sealed class UuidV7Tests
{
    [Fact]
    public void Debe_GenerarIdentificadoresVersion7_Cuando_SeInvocaUuidV7()
    {
        var id = UuidV7.Nuevo();

        Assert.NotEqual(Guid.Empty, id);
        Assert.True(UuidV7.EsVersion7(id));
    }

    [Fact]
    public void Debe_GenerarIdentificadorVersion7ConMarcaTemporal_Cuando_SePasaFecha()
    {
        var id = UuidV7.Nuevo(DateTimeOffset.UtcNow);

        Assert.True(UuidV7.EsVersion7(id));
        Assert.False(UuidV7.EsVersion7(Guid.NewGuid()));
    }
}

