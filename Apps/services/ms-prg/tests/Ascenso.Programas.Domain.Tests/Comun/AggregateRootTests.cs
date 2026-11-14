using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Tests.Comun;

public sealed class AggregateRootTests
{
    private sealed class AgregadoPrueba : AggregateRoot<Guid>
    {
        public AgregadoPrueba(Guid id, DateTimeOffset fechaCreacion, Guid usuarioCreacionId)
            : base(id, fechaCreacion, usuarioCreacionId)
        {
        }

        public void Registrar(IDomainEvent evento) => RegistrarEventoDominio(evento);

        public void Limpiar() => LimpiarEventosDominio();
    }

    private sealed record EventoPrueba(
        Guid EventId,
        DateTimeOffset OccurredAt,
        Guid CorrelationId) : IDomainEvent;

    [Fact]
    public void Debe_AcumularEventosDeDominio_Cuando_SeRegistran()
    {
        var agregado = new AgregadoPrueba(UuidV7.Nuevo(), DateTimeOffset.UtcNow, Guid.NewGuid());
        var evento = new EventoPrueba(UuidV7.Nuevo(), DateTimeOffset.UtcNow, Guid.NewGuid());

        agregado.Registrar(evento);

        var eventos = Assert.Single(agregado.EventosDominio);
        Assert.Same(evento, eventos);
    }

    [Fact]
    public void Debe_LimpiarLosEventos_Cuando_SeSolicita()
    {
        var agregado = new AgregadoPrueba(UuidV7.Nuevo(), DateTimeOffset.UtcNow, Guid.NewGuid());
        agregado.Registrar(new EventoPrueba(UuidV7.Nuevo(), DateTimeOffset.UtcNow, Guid.NewGuid()));

        agregado.Limpiar();

        Assert.Empty(agregado.EventosDominio);
    }

    [Fact]
    public void Debe_RechazarEventosNulos_Cuando_SeRegistran()
    {
        var agregado = new AgregadoPrueba(UuidV7.Nuevo(), DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.Throws<ArgumentNullException>(() => agregado.Registrar(null!));
    }
}

