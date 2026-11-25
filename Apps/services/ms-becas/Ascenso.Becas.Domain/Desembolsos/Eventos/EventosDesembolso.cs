using Ascenso.Becas.Domain.Comun;

namespace Ascenso.Becas.Domain.Desembolsos.Eventos;

public abstract record class EventoDesembolso : DomainEvent
{
    protected EventoDesembolso(Guid becaId, Guid calendarioId, Guid desembolsoId,
        Guid correlationId, DateTimeOffset occurredAt)
        : base(correlationId, occurredAt)
    {
        BecaId = becaId;
        CalendarioId = calendarioId;
        DesembolsoId = desembolsoId;
    }

    public Guid BecaId { get; }
    public Guid CalendarioId { get; }
    public Guid DesembolsoId { get; }
}

public sealed record class DesembolsoProgramadoEvento(Guid Beca, Guid Calendario,
    Guid Desembolso, Guid Correlacion, DateTimeOffset Fecha)
    : EventoDesembolso(Beca, Calendario, Desembolso, Correlacion, Fecha);

public sealed record class DesembolsoConfirmadoConOtpEvento(Guid Beca, Guid Calendario,
    Guid Desembolso, Guid Correlacion, DateTimeOffset Fecha)
    : EventoDesembolso(Beca, Calendario, Desembolso, Correlacion, Fecha);

public sealed record class DesembolsoFallidoEvento(Guid Beca, Guid Calendario,
    Guid Desembolso, Guid Correlacion, DateTimeOffset Fecha)
    : EventoDesembolso(Beca, Calendario, Desembolso, Correlacion, Fecha);

public sealed record class DesembolsoDevueltoEvento(Guid Beca, Guid Calendario,
    Guid Desembolso, Guid Correlacion, DateTimeOffset Fecha)
    : EventoDesembolso(Beca, Calendario, Desembolso, Correlacion, Fecha);

public sealed record class DesembolsoReintentadoEvento(Guid Beca, Guid Calendario,
    Guid Desembolso, Guid Correlacion, DateTimeOffset Fecha)
    : EventoDesembolso(Beca, Calendario, Desembolso, Correlacion, Fecha);

public sealed record class DesembolsoCanceladoEvento(Guid Beca, Guid Calendario,
    Guid Desembolso, Guid Correlacion, DateTimeOffset Fecha)
    : EventoDesembolso(Beca, Calendario, Desembolso, Correlacion, Fecha);
