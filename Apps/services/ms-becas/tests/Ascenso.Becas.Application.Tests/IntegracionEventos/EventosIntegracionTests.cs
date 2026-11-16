using System.Text.Json;
using Ascenso.Becas.Application.IntegracionEventos;
using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;
using FluentAssertions;

namespace Ascenso.Becas.Application.Tests.IntegracionEventos;

public sealed class EventosIntegracionTests
{
    private static readonly Guid _becaId = Guid.CreateVersion7();
    private static readonly Guid _ofertaId = Guid.CreateVersion7();
    private static readonly Guid _postulacionId = Guid.CreateVersion7();
    private static readonly Guid _programaBecaId = Guid.CreateVersion7();
    private static readonly Guid _periodoAcademicoId = Guid.CreateVersion7();
    private static readonly Guid _compromisoId = Guid.CreateVersion7();
    private static readonly Guid _correlacionId = Guid.CreateVersion7();
    private static readonly DateTimeOffset _ocurridoEn =
        new(2026, 11, 14, 15, 30, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions _opcionesJson =
        new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("OfertaBecaEmitida", "OfertaBecaEmitida")]
    [InlineData("BecaActivada", "BecaActivada")]
    [InlineData("OfertaBecaRechazada", "OfertaBecaRechazada")]
    [InlineData("OfertaBecaAnulada", "OfertaBecaAnulada")]
    [InlineData("OfertaBecaExpirada", "OfertaBecaExpirada")]
    [InlineData("CompromisoBecarioCargado", "CompromisoBecarioCargado")]
    [InlineData("CompromisoBecarioAprobado", "CompromisoBecarioAprobado")]
    [InlineData("CompromisoBecarioRechazado", "CompromisoBecarioRechazado")]
    public void Debe_EnvolverElEvento_Cuando_SeMapeaUnEventoConocido(
        string tipoEvento,
        string nombrePayload)
    {
        var evento = CrearEvento(tipoEvento);
        var mapeado = MapeadorEventosIntegracion.Mapear(evento, aggregateVersion: 7);

        mapeado.GetType().IsGenericType.Should().BeTrue();
        mapeado.GetType().GetGenericTypeDefinition().Should()
            .Be(typeof(EventoIntegracion<>));
        mapeado.GetType().GetGenericArguments().Single().Name.Should().Be(nombrePayload);

        using var documento = JsonDocument.Parse(Serializar(mapeado));
        var raiz = documento.RootElement;

        raiz.GetProperty("eventId").GetGuid().Should().Be(evento.EventId);
        raiz.GetProperty("occurredAt").GetDateTimeOffset().Should().Be(evento.OccurredAt);
        raiz.GetProperty("correlationId").GetGuid().Should().Be(evento.CorrelationId);
        raiz.GetProperty("producer").GetString().Should().Be("ms-bec");
        raiz.GetProperty("aggregateId").GetGuid().Should().Be(_becaId);
        raiz.GetProperty("aggregateVersion").GetInt32().Should().Be(7);
        raiz.GetProperty("payload").ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Theory]
    [InlineData("OfertaBecaEmitida")]
    [InlineData("BecaActivada")]
    [InlineData("OfertaBecaRechazada")]
    [InlineData("OfertaBecaAnulada")]
    [InlineData("OfertaBecaExpirada")]
    [InlineData("CompromisoBecarioCargado")]
    [InlineData("CompromisoBecarioAprobado")]
    [InlineData("CompromisoBecarioRechazado")]
    public void Debe_SerializarSinPii_Cuando_SePublicaUnEventoDeSalida(
        string tipoEvento)
    {
        var evento = CrearEvento(tipoEvento);
        var mapeado = MapeadorEventosIntegracion.Mapear(evento);
        var json = Serializar(mapeado);

        json.Should().NotContainAny(
            "nombreEstudiante",
            "numeroDocumento",
            "correo",
            "cuentaBancaria",
            "huellaPayload",
            "codigoOtp",
            "montoSocioeconomico");
    }

    [Fact]
    public void Debe_RechazarElMapeo_Cuando_ElEventoNoTieneContratoDeIntegracion()
    {
        var accion = () => MapeadorEventosIntegracion.Mapear(
            new EventoDesconocido(_correlacionId, _ocurridoEn));

        accion.Should().Throw<InvalidOperationException>()
            .WithMessage("*EventoDesconocido*");
    }

    [Fact]
    public void Debe_ConservarElEventId_Cuando_SeMapeaUnaOfertaEmitida()
    {
        var evento = CrearOfertaEmitida();

        var mapeado = MapeadorEventosIntegracion.Mapear(evento, 3)
            .Should().BeOfType<EventoIntegracion<OfertaBecaEmitida>>().Subject;

        mapeado.EventId.Should().Be(evento.EventId);
        mapeado.CorrelationId.Should().Be(evento.CorrelationId);
        mapeado.AggregateId.Should().Be(evento.BecaId);
        mapeado.AggregateVersion.Should().Be(3);
        mapeado.Payload.OfertaId.Should().Be(evento.OfertaId);
        mapeado.Payload.EstadoOferta.Should().Be("PENDIENTE_RESPUESTA");
    }

    private static string Serializar(object valor)
        => JsonSerializer.Serialize(valor, _opcionesJson);

    private static IDomainEvent CrearEvento(string tipoEvento)
        => tipoEvento switch
        {
            "OfertaBecaEmitida" => CrearOfertaEmitida(),
            "BecaActivada" => CrearActivada(),
            "OfertaBecaRechazada" => CrearRechazada(),
            "OfertaBecaAnulada" => CrearAnulada(),
            "OfertaBecaExpirada" => CrearExpirada(),
            "CompromisoBecarioCargado" => CrearCargado(),
            "CompromisoBecarioAprobado" => CrearAprobado(),
            "CompromisoBecarioRechazado" => CrearRechazado(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(tipoEvento),
                tipoEvento,
                "El evento de prueba no está definido.")
        };

    private static OfertaBecaEmitidaEvento CrearOfertaEmitida()
        => new(
            _becaId,
            _ofertaId,
            _postulacionId,
            _programaBecaId,
            _periodoAcademicoId,
            _ocurridoEn.AddDays(15),
            _correlacionId,
            _ocurridoEn);

    private static BecaActivadaEvento CrearActivada()
        => new(
            _becaId,
            _ofertaId,
            _postulacionId,
            _programaBecaId,
            _periodoAcademicoId,
            _correlacionId,
            _ocurridoEn);

    private static OfertaBecaRechazadaEvento CrearRechazada()
        => new(
            _becaId,
            _ofertaId,
            _postulacionId,
            _programaBecaId,
            _periodoAcademicoId,
            _correlacionId,
            _ocurridoEn);

    private static OfertaBecaAnuladaEvento CrearAnulada()
        => new(
            _becaId,
            _ofertaId,
            _postulacionId,
            _programaBecaId,
            _periodoAcademicoId,
            _correlacionId,
            _ocurridoEn);

    private static OfertaBecaExpiradaEvento CrearExpirada()
        => new(
            _becaId,
            _ofertaId,
            _postulacionId,
            _programaBecaId,
            _periodoAcademicoId,
            _correlacionId,
            _ocurridoEn);

    private static CompromisoBecarioCargadoEvento CrearCargado()
        => new(_becaId, _compromisoId, 2, _correlacionId, _ocurridoEn);

    private static CompromisoBecarioAprobadoEvento CrearAprobado()
        => new(_becaId, _compromisoId, _correlacionId, _ocurridoEn);

    private static CompromisoBecarioRechazadoEvento CrearRechazado()
        => new(_becaId, _compromisoId, _correlacionId, _ocurridoEn);

    private sealed record EventoDesconocido(
        Guid CorrelationId,
        DateTimeOffset OccurredAt)
        : DomainEvent(CorrelationId, OccurredAt);
}
