using System.Text.Json;
using Ascenso.Becas.Api.Errores;
using Ascenso.Becas.Domain.Excepciones;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ascenso.Becas.IntegrationTests.Errores;

public sealed class ManejoErroresTests
{
    public static IEnumerable<object[]> ExcepcionesDeDominio()
    {
        yield return
        [
            new TransicionEstadoInvalidaException("ACTIVA", "CERRADA"),
            409,
            "TRANSICION_INVALIDA"
        ];
        yield return
        [
            new OfertaVencidaException(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow),
            409,
            "OFERTA_VENCIDA"
        ];
        yield return
        [
            new DobleBeneficioException(Guid.NewGuid(), "ya tiene una beca activa"),
            409,
            "DOBLE_BENEFICIO"
        ];
        yield return
        [
            new JustificacionRequeridaException("anular oferta"),
            400,
            "JUSTIFICACION_REQUERIDA"
        ];
        yield return
        [
            new PostulacionNoAdjudicableException(Guid.NewGuid()),
            422,
            "POSTULACION_NO_ADJUDICABLE"
        ];
        yield return
        [
            new CompromisoNoFirmadoException(Guid.NewGuid()),
            412,
            "COMPROMISO_NO_FIRMADO"
        ];
    }

    [Theory]
    [MemberData(nameof(ExcepcionesDeDominio))]
    public async Task Debe_MapearCadaExcepcionDeDominio(
        Exception excepcion,
        int estadoEsperado,
        string codigoEsperado)
    {
        var respuesta = await EjecutarAsync(excepcion);

        respuesta.Estado.Should().Be(estadoEsperado);
        respuesta.Codigo.Should().Be(codigoEsperado);
        respuesta.Detalle.Should().Be(excepcion.Message);
    }

    [Fact]
    public async Task Debe_MapearElConflictoDeConcurrencia()
    {
        var respuesta = await EjecutarAsync(
            new DbUpdateConcurrencyException("conflicto"));

        respuesta.Estado.Should().Be(409);
        respuesta.Codigo.Should().Be("CONCURRENCY_CONFLICT");
    }

    [Fact]
    public async Task Debe_MapearElErrorDePersistencia()
    {
        var respuesta = await EjecutarAsync(new DbUpdateException("fallo"));

        respuesta.Estado.Should().Be(409);
        respuesta.Codigo.Should().Be("CONFLICTO_PERSISTENCIA");
    }

    [Fact]
    public async Task Debe_MapearLaSolicitudHttpInvalida()
    {
        var respuesta = await EjecutarAsync(
            new BadHttpRequestException("json inválido", 400));

        respuesta.Estado.Should().Be(400);
        respuesta.Codigo.Should().Be("SOLICITUD_INVALIDA");
    }

    [Fact]
    public async Task Debe_MapearLaOperacionCancelada()
    {
        var respuesta = await EjecutarAsync(new OperationCanceledException());

        respuesta.Estado.Should().Be(499);
        respuesta.Codigo.Should().Be("OPERACION_CANCELADA");
    }

    [Fact]
    public async Task Debe_OcultarElDetalle_Cuando_ElErrorEsNoControlado()
    {
        var respuesta = await EjecutarAsync(
            new InvalidOperationException("detalle-interno-secreto"));

        respuesta.Estado.Should().Be(500);
        respuesta.Codigo.Should().Be("ERROR_INTERNO");
        respuesta.Detalle.Should().NotContain("detalle-interno-secreto");
    }

    [Fact]
    public async Task Debe_IncluirElIdentificadorDeCorrelacion()
    {
        var respuesta = await EjecutarAsync(
            new JustificacionRequeridaException("anular oferta"));

        respuesta.Correlacion.Should().Be("correlacion-de-prueba");
    }

    private static async Task<RespuestaError> EjecutarAsync(Exception excepcion)
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/api/v1/becas/ofertas";
        contexto.TraceIdentifier = "correlacion-de-prueba";
        contexto.Response.Body = new MemoryStream();

        var manejador = new ManejadorExcepcionesApi(
            NullLogger<ManejadorExcepcionesApi>.Instance);

        var manejado = await manejador.TryHandleAsync(
            contexto,
            excepcion,
            CancellationToken.None);

        manejado.Should().BeTrue();
        contexto.Response.ContentType.Should().Be("application/problem+json");
        contexto.Response.Body.Position = 0;

        using var documento = await JsonDocument.ParseAsync(contexto.Response.Body);
        var raiz = documento.RootElement;

        return new RespuestaError(
            contexto.Response.StatusCode,
            raiz.GetProperty("code").GetString()!,
            raiz.GetProperty("detail").GetString()!,
            raiz.GetProperty("correlationId").GetString()!);
    }

    private sealed record RespuestaError(
        int Estado,
        string Codigo,
        string Detalle,
        string Correlacion);
}
