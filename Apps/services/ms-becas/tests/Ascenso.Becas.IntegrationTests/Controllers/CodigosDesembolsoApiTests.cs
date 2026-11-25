using System.Text.Json;
using Ascenso.Becas.Api.Controllers.v1;
using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.IntegrationTests.Controllers;

public sealed class CodigosDesembolsoApiTests
{
    private static readonly JsonSerializerOptions _opciones =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public void SolicitudesAceptanCodigosLiteralesYRechazanNumeros()
    {
        const string cuerpo = """
            {"monto":100,"fechaEjecucion":"2026-09-01","destinatario":"INSTITUCION",
             "datosPagoBecarioId":null,"cuentaDestinoCifrada":"cifrado",
             "metodoPago":"TRANSFERENCIA_BANCARIA","referenciaBancaria":"REF"}
            """;
        var solicitud = JsonSerializer.Deserialize<DesembolsosController.RegistrarEjecucionRequest>(
            cuerpo, _opciones);
        Assert.NotNull(solicitud);
        Assert.Equal(DestinatarioPago.Institucion, solicitud.Destinatario);
        Assert.Equal(MetodoPago.TransferenciaBancaria, solicitud.MetodoPago);

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<
            DesembolsosController.RegistrarEjecucionRequest>(
            cuerpo.Replace("\"INSTITUCION\"", "2", StringComparison.Ordinal),
            _opciones));
    }
}
