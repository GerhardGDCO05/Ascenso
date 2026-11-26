using System.Text.Json;
using Ascenso.Becas.Application.IntegracionEventos;
using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Desembolsos.Eventos;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;

namespace Ascenso.Becas.Application.Tests.Configuracion;

public sealed class EventosDesembolsoTests
{
    private static readonly JsonSerializerOptions _opciones =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public void SuspensionPublicaIdentificadoresYSaldoSinDatosPersonales()
    {
        var beca = Guid.CreateVersion7();
        var suspension = Guid.CreateVersion7();
        var evento = new DesembolsosBecaSuspendidosEvento(beca, suspension,
            Guid.CreateVersion7(), Guid.CreateVersion7(), 1400m,
            Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(MapeadorEventosIntegracion.Mapear(evento), _opciones);
        using var documento = JsonDocument.Parse(json);
        var payload = documento.RootElement.GetProperty("payload");
        Assert.Equal(beca, payload.GetProperty("becaId").GetGuid());
        Assert.Equal(suspension, payload.GetProperty("suspensionId").GetGuid());
        Assert.Equal(1400m, payload.GetProperty("saldoPendiente").GetDecimal());
        Assert.Equal(5, payload.EnumerateObject().Count());
    }

    [Fact]
    public void ReanudacionPublicaLaSuspensionLevantadaSinDatosPersonales()
    {
        var beca = Guid.CreateVersion7();
        var suspension = Guid.CreateVersion7();
        var evento = new DesembolsosBecaReanudadosEvento(beca, suspension,
            Guid.CreateVersion7(), Guid.CreateVersion7(), 1400m,
            Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(MapeadorEventosIntegracion.Mapear(evento), _opciones);
        using var documento = JsonDocument.Parse(json);
        var payload = documento.RootElement.GetProperty("payload");
        Assert.Equal(beca, payload.GetProperty("becaId").GetGuid());
        Assert.Equal(suspension, payload.GetProperty("suspensionId").GetGuid());
        Assert.Equal(1400m, payload.GetProperty("saldoPendiente").GetDecimal());
        Assert.Equal(5, payload.EnumerateObject().Count());
    }

    [Fact]
    public void RevocacionPublicaElCierreDefinitivoSinDatosPersonales()
    {
        var beca = Guid.CreateVersion7();
        var evento = new BecaRevocadaDefinitivamenteEvento(beca,
            Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(MapeadorEventosIntegracion.Mapear(evento), _opciones);
        using var documento = JsonDocument.Parse(json);
        var payload = documento.RootElement.GetProperty("payload");
        Assert.Equal(beca, payload.GetProperty("becaId").GetGuid());
        Assert.Equal("REVOCADA", payload.GetProperty("estadoBeca").GetString());
        Assert.Equal("REVOCACION_DEFINITIVA", payload.GetProperty("tipoCierre").GetString());
        Assert.Equal(5, payload.EnumerateObject().Count());
    }

    [Fact]
    public void LosSeisContratosUsanElSobreYSoloIdentificadores()
    {
        var beca = Guid.CreateVersion7();
        var calendario = Guid.CreateVersion7();
        var desembolso = Guid.CreateVersion7();
        var correlacion = Guid.CreateVersion7();
        var fecha = DateTimeOffset.UtcNow;
        IDomainEvent[] eventos =
        [
            new DesembolsoProgramadoEvento(beca, calendario, desembolso, correlacion, fecha),
            new DesembolsoConfirmadoConOtpEvento(beca, calendario, desembolso, correlacion, fecha),
            new DesembolsoFallidoEvento(beca, calendario, desembolso, correlacion, fecha),
            new DesembolsoDevueltoEvento(beca, calendario, desembolso, correlacion, fecha),
            new DesembolsoReintentadoEvento(beca, calendario, desembolso, correlacion, fecha),
            new DesembolsoCanceladoEvento(beca, calendario, desembolso, correlacion, fecha)
        ];

        foreach (var evento in eventos)
        {
            var json = JsonSerializer.Serialize(MapeadorEventosIntegracion.Mapear(evento), _opciones);
            using var documento = JsonDocument.Parse(json);
            var sobre = documento.RootElement;
            Assert.Equal(beca, sobre.GetProperty("aggregateId").GetGuid());
            Assert.Equal(correlacion, sobre.GetProperty("correlationId").GetGuid());
            var payload = sobre.GetProperty("payload");
            Assert.Equal(3, payload.EnumerateObject().Count());
            Assert.Equal(beca, payload.GetProperty("becaId").GetGuid());
            Assert.Equal(calendario, payload.GetProperty("calendarioId").GetGuid());
            Assert.Equal(desembolso, payload.GetProperty("desembolsoId").GetGuid());
        }
    }
}
