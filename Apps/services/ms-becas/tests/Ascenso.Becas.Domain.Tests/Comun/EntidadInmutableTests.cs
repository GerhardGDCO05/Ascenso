using Ascenso.Becas.Domain.Comun;

namespace Ascenso.Becas.Domain.Tests.Comun;

public sealed class EntidadInmutableTests
{
    private sealed class RegistroPrueba : EntidadInmutable
    {
        public RegistroPrueba(
            Guid id,
            DateTimeOffset fechaCreacion,
            Guid usuarioAutorId,
            Guid correlacionId)
            : base(id, fechaCreacion, usuarioAutorId, correlacionId)
        {
        }
    }

    [Fact]
    public void Debe_InicializarLosCamposInmutables_Cuando_SeCreaElRegistro()
    {
        var id = UuidV7.Nuevo();
        var fecha = new DateTimeOffset(2026, 11, 7, 12, 0, 0, TimeSpan.FromHours(-4));
        var usuario = Guid.NewGuid();
        var correlacion = Guid.NewGuid();

        var registro = new RegistroPrueba(id, fecha, usuario, correlacion);

        Assert.Equal(id, registro.Id);
        Assert.Equal(fecha.ToUniversalTime(), registro.FechaCreacion);
        Assert.Equal(usuario, registro.UsuarioAutorId);
        Assert.Equal(correlacion, registro.CorrelacionId);
    }

    [Fact]
    public void Debe_SerIgualPorIdentificadorYCType_Cuando_SeComparanRegistros()
    {
        var id = UuidV7.Nuevo();
        var fecha = DateTimeOffset.UtcNow;

        var primero = new RegistroPrueba(id, fecha, Guid.NewGuid(), Guid.NewGuid());
        var segundo = new RegistroPrueba(id, fecha.AddHours(1), Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(primero, segundo);
        Assert.Equal(primero.GetHashCode(), segundo.GetHashCode());
    }

    [Fact]
    public void Debe_RechazarCamposObligatorios_Cuando_SeCreaElRegistro()
    {
        Assert.Throws<ArgumentException>(
            () => new RegistroPrueba(Guid.Empty, DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Throws<ArgumentException>(
            () => new RegistroPrueba(UuidV7.Nuevo(), default, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Throws<ArgumentException>(
            () => new RegistroPrueba(UuidV7.Nuevo(), DateTimeOffset.UtcNow, Guid.Empty, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(
            () => new RegistroPrueba(UuidV7.Nuevo(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.Empty));
    }
}
