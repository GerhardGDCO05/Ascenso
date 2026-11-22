using Ascenso.Postulaciones.Domain.Comun;

namespace Ascenso.Postulaciones.Domain.Tests.Comun;

public sealed class UsuarioSistemaTests
{
    [Fact]
    public void Debe_ExponerIdentificadorFijoNoVacio_Cuando_SeConsultaUsuarioSistema()
    {
        Assert.NotEqual(Guid.Empty, UsuarioSistema.Id);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), UsuarioSistema.Id);
    }
}
