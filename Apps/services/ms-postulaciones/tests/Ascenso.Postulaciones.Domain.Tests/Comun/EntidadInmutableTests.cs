using Ascenso.Postulaciones.Domain.Comun;

namespace Ascenso.Postulaciones.Domain.Tests.Comun;

public sealed class EntidadInmutableTests
{
    private sealed class RegistroPrueba : EntidadInmutable
    {
        public RegistroPrueba(Guid id, DateTimeOffset fechaCreacion, Guid usuarioAutorId, Guid correlacionId)
            : base(id, fechaCreacion, usuarioAutorId, correlacionId)
        {
        }
    }

    [Fact]
    public void Debe_InicializarPropiedadesEnUtc_Cuando_SeCreaElRegistroInmutable()
    {
        var id = UuidV7.Nuevo();
        var fecha = new DateTimeOffset(2026, 11, 13, 14, 0, 0, TimeSpan.FromHours(-4));
        var autor = Guid.NewGuid();
        var correlacion = Guid.NewGuid();

        var registro = new RegistroPrueba(id, fecha, autor, correlacion);

        Assert.Equal(id, registro.Id);
        Assert.Equal(fecha.ToUniversalTime(), registro.FechaCreacion);
        Assert.Equal(autor, registro.UsuarioAutorId);
        Assert.Equal(correlacion, registro.CorrelacionId);
    }

    [Fact]
    public void Debe_NoExponerSettersPublicos_Cuando_SeInspeccionaEntidadInmutable()
    {
        var propiedadesConSetterPublico = typeof(EntidadInmutable)
            .GetProperties()
            .Where(p => p.SetMethod?.IsPublic == true);

        Assert.Empty(propiedadesConSetterPublico);
    }

    [Fact]
    public void Debe_RechazarParametrosVacios_Cuando_SeCreaElRegistroInmutable()
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

