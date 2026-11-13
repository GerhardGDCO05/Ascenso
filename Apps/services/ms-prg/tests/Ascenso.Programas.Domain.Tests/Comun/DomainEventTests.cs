using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Tests.Comun;

public sealed class DomainEventTests
{
    private sealed record EventoConcreto : EventoDominio
    {
        public EventoConcreto(Guid correlacionId, DateTimeOffset ocurridoEn, Guid? eventoId = null)
            : base(correlacionId, ocurridoEn, eventoId)
        {
        }
    }

    [Fact]
    public void Debe_GenerarUuidV7YConvertirFechaAUtc_Cuando_SeCreaEventoDominio()
    {
        var correlacionId = Guid.NewGuid();
        var fechaLocal = new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.FromHours(-4));

        var evento = new EventoConcreto(correlacionId, fechaLocal);

        Assert.True(UuidV7.EsVersion7(evento.EventId));
        Assert.Equal(fechaLocal.ToUniversalTime(), evento.OccurredAt);
        Assert.Equal(correlacionId, evento.CorrelationId);
    }

    [Fact]
    public void Debe_RechazarParametrosInvalidos_Cuando_SeCreaEventoDominio()
    {
        Assert.Throws<ArgumentException>(() => new EventoConcreto(Guid.Empty, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new EventoConcreto(Guid.NewGuid(), default));
    }
}

