using Ascenso.Becas.Domain.Comun;

namespace Ascenso.Becas.Domain.Tests.Comun;

public sealed class DomainEventTests
{
    private sealed record EventoPrueba : DomainEvent
    {
        public EventoPrueba(Guid correlationId, DateTimeOffset occurredAt, Guid? eventId = null)
            : base(correlationId, occurredAt, eventId)
        {
        }
    }

    [Fact]
    public void Debe_InicializarElSobreDelEvento_Cuando_SeCrea()
    {
        var correlacion = Guid.NewGuid();
        var ocurrencia = new DateTimeOffset(2026, 11, 7, 12, 0, 0, TimeSpan.FromHours(-4));

        var evento = new EventoPrueba(correlacion, ocurrencia);

        Assert.NotEqual(Guid.Empty, evento.EventId);
        Assert.Equal(7, evento.EventId.Version);
        Assert.Equal(ocurrencia.ToUniversalTime(), evento.OccurredAt);
        Assert.Equal(correlacion, evento.CorrelationId);
    }

    [Fact]
    public void Debe_RespetarElEventId_Cuando_SeProporciona()
    {
        var eventId = UuidV7.Nuevo();
        var evento = new EventoPrueba(Guid.NewGuid(), DateTimeOffset.UtcNow, eventId);

        Assert.Equal(eventId, evento.EventId);
    }

    [Fact]
    public void Debe_RechazarElSobreInvalido_Cuando_FaltanDatosObligatorios()
    {
        Assert.Throws<ArgumentException>(
            () => new EventoPrueba(Guid.Empty, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(
            () => new EventoPrueba(Guid.NewGuid(), default));
    }
}
