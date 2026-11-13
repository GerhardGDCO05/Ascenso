using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Tests.Comun;

public sealed class EntidadAuditableTests
{
    private sealed class EntidadPrueba : EntidadAuditable
    {
        public EntidadPrueba(Guid id, DateTimeOffset fechaCreacion, Guid usuarioCreacionId)
            : base(id, fechaCreacion, usuarioCreacionId)
        {
        }

        public void Modificar(Guid usuarioModificacionId, DateTimeOffset fechaModificacion)
            => ActualizarAuditoria(usuarioModificacionId, fechaModificacion);
    }

    private sealed class OtraEntidadPrueba : EntidadAuditable
    {
        public OtraEntidadPrueba(Guid id, DateTimeOffset fechaCreacion, Guid usuarioCreacionId)
            : base(id, fechaCreacion, usuarioCreacionId)
        {
        }
    }

    [Fact]
    public void Debe_InicializarLaAuditoria_Cuando_SeCreaLaEntidad()
    {
        var id = UuidV7.Nuevo();
        var fecha = new DateTimeOffset(2026, 11, 13, 10, 30, 0, TimeSpan.FromHours(-4));
        var usuario = Guid.NewGuid();

        var entidad = new EntidadPrueba(id, fecha, usuario);

        Assert.Equal(id, entidad.Id);
        Assert.Equal(fecha.ToUniversalTime(), entidad.FechaCreacion);
        Assert.Equal(fecha.ToUniversalTime(), entidad.FechaUltimaModificacion);
        Assert.Equal(usuario, entidad.UsuarioCreacionId);
        Assert.Equal(usuario, entidad.UsuarioModificacionId);
        Assert.Equal(1, entidad.Version);
    }

    [Fact]
    public void Debe_ActualizarLaAuditoriaEIncrementarLaVersion_Cuando_SeModificaLaEntidad()
    {
        var entidad = new EntidadPrueba(
            UuidV7.Nuevo(),
            DateTimeOffset.UtcNow.AddDays(-1),
            Guid.NewGuid());
        var usuarioModificacion = Guid.NewGuid();
        var fechaModificacion = DateTimeOffset.UtcNow;

        entidad.Modificar(usuarioModificacion, fechaModificacion);

        Assert.Equal(usuarioModificacion, entidad.UsuarioModificacionId);
        Assert.Equal(fechaModificacion.ToUniversalTime(), entidad.FechaUltimaModificacion);
        Assert.Equal(2, entidad.Version);
    }

    [Fact]
    public void Debe_SerIgualPorIdentificadorYTipo_Cuando_SeComparanEntidades()
    {
        var id = UuidV7.Nuevo();
        var fecha = DateTimeOffset.UtcNow;
        var usuario = Guid.NewGuid();

        var primera = new EntidadPrueba(id, fecha, usuario);
        var segunda = new EntidadPrueba(id, fecha.AddDays(-3), Guid.NewGuid());

        Assert.Equal(primera, segunda);
        Assert.True(primera == segunda);
        Assert.False(primera != segunda);
        Assert.Equal(primera.GetHashCode(), segunda.GetHashCode());
    }

    [Fact]
    public void Debe_SerDiferente_Cuando_ElTipoConcretoNoCoincide()
    {
        var id = UuidV7.Nuevo();
        var fecha = DateTimeOffset.UtcNow;
        var usuario = Guid.NewGuid();

        var primera = new EntidadPrueba(id, fecha, usuario);
        var segunda = new OtraEntidadPrueba(id, fecha, usuario);

        Assert.NotEqual<EntidadAuditable>(primera, segunda);
    }

    [Fact]
    public void Debe_RechazarParametrosInvalidos_Cuando_SeCreaOActualizaLaEntidad()
    {
        Assert.Throws<ArgumentException>(
            () => new EntidadPrueba(Guid.Empty, DateTimeOffset.UtcNow, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(
            () => new EntidadPrueba(UuidV7.Nuevo(), default, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(
            () => new EntidadPrueba(UuidV7.Nuevo(), DateTimeOffset.UtcNow, Guid.Empty));

        var entidad = new EntidadPrueba(UuidV7.Nuevo(), DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => entidad.Modificar(Guid.Empty, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => entidad.Modificar(Guid.NewGuid(), default));
    }
}

