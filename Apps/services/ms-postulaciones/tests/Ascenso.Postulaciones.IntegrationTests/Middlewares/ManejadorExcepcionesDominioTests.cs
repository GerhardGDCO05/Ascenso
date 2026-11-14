using System.Text.Json;
using Ascenso.Postulaciones.Api.Middlewares;
using Ascenso.Postulaciones.Domain.Excepciones;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace Ascenso.Postulaciones.IntegrationTests.Middlewares;

public sealed class ManejadorExcepcionesDominioTests
{
    [Fact]
    public async Task Debe_EscribirProblemDetailsConCodigo_Cuando_LaExcepcionEsDeDominio()
    {
        var contexto = CrearContexto();
        var manejador = new ManejadorExcepcionesDominio();

        var manejado = await manejador.TryHandleAsync(
            contexto,
            new ExcepcionEntidadNoEncontrada("El postulante no existe."),
            CancellationToken.None);

        manejado.Should().BeTrue();
        contexto.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        using var documento = await LeerCuerpoAsync(contexto);
        documento.RootElement.GetProperty("codigo").GetString()
            .Should().Be(ExcepcionEntidadNoEncontrada.CodigoError);
        documento.RootElement.GetProperty("detail").GetString()
            .Should().Be("El postulante no existe.");
    }

    [Fact]
    public async Task Debe_EscribirValidationProblemDetails_Cuando_HayErroresDeValidacion()
    {
        var contexto = CrearContexto();
        var manejador = new ManejadorExcepcionesDominio();
        var excepcion = new ValidationException(
        [
            new ValidationFailure("correoContacto", "El correo es obligatorio.")
            {
                ErrorCode = "CORREO_REQUERIDO"
            }
        ]);

        var manejado = await manejador.TryHandleAsync(contexto, excepcion, CancellationToken.None);

        manejado.Should().BeTrue();
        contexto.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        using var documento = await LeerCuerpoAsync(contexto);
        documento.RootElement.GetProperty("codigo").GetString().Should().Be("CORREO_REQUERIDO");
        documento.RootElement.GetProperty("errors").GetProperty("correoContacto")
            .EnumerateArray().Should().Contain(elemento => elemento.GetString() == "El correo es obligatorio.");
    }

    [Fact]
    public async Task Debe_EscribirSolicitudInvalida_Cuando_LaExcepcionEsUnArgumentException()
    {
        var contexto = CrearContexto();
        var manejador = new ManejadorExcepcionesDominio();

        var manejado = await manejador.TryHandleAsync(
            contexto,
            new ArgumentException("Dato inválido."),
            CancellationToken.None);

        manejado.Should().BeTrue();
        contexto.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        using var documento = await LeerCuerpoAsync(contexto);
        documento.RootElement.GetProperty("codigo").GetString().Should().Be("SOLICITUD_INVALIDA");
    }

    [Fact]
    public async Task Debe_NoManejarLaExcepcion_Cuando_EsDeOtroTipo()
    {
        var contexto = CrearContexto();
        var manejador = new ManejadorExcepcionesDominio();

        var manejado = await manejador.TryHandleAsync(
            contexto,
            new InvalidOperationException("inesperada"),
            CancellationToken.None);

        manejado.Should().BeFalse();
        contexto.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    private static DefaultHttpContext CrearContexto()
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/api/v1/postulantes";
        contexto.Response.Body = new MemoryStream();
        return contexto;
    }

    private static async Task<JsonDocument> LeerCuerpoAsync(DefaultHttpContext contexto)
    {
        contexto.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonDocument.ParseAsync(contexto.Response.Body);
    }
}
