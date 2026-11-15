using Ascenso.Becas.Api.Correlacion;
using Ascenso.Becas.Application.Correlacion;
using Ascenso.Becas.Infrastructure.Clientes;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Ascenso.Becas.IntegrationTests.Correlacion;

public sealed class CorrelacionTests
{
    [Fact]
    public async Task Debe_ConservarElIdentificador_Cuando_ElClienteLoEnvia()
    {
        var logger = new LoggerCorrelacion();
        var contexto = new DefaultHttpContext();
        contexto.Request.Headers[CorrelacionConstantes.Encabezado] =
            "correlacion-cliente-123";

        await new CorrelacionMiddleware(
                _ => Task.CompletedTask,
                logger)
            .InvokeAsync(contexto);

        contexto.Response.Headers[CorrelacionConstantes.Encabezado]
            .ToString().Should().Be("correlacion-cliente-123");
        contexto.TraceIdentifier.Should().Be("correlacion-cliente-123");
        contexto.Items[CorrelacionConstantes.ItemHttp]
            .Should().Be("correlacion-cliente-123");
        logger.Correlacion.Should().Be("correlacion-cliente-123");
    }

    [Fact]
    public async Task Debe_GenerarUnIdentificador_Cuando_NoVieneEnLaSolicitud()
    {
        var contexto = new DefaultHttpContext();

        await new CorrelacionMiddleware(
                _ => Task.CompletedTask,
                new LoggerCorrelacion())
            .InvokeAsync(contexto);

        var correlacion = contexto.Response.Headers[CorrelacionConstantes.Encabezado]
            .ToString();

        correlacion.Should().NotBeNullOrWhiteSpace();
        correlacion.Should().HaveLength(32);
        contexto.TraceIdentifier.Should().Be(correlacion);
    }

    [Fact]
    public async Task Debe_ReemplazarElIdentificador_Cuando_EsDemasiadoLargo()
    {
        var contexto = new DefaultHttpContext();
        var invalido = new string('x', 129);
        contexto.Request.Headers[CorrelacionConstantes.Encabezado] = invalido;

        await new CorrelacionMiddleware(
                _ => Task.CompletedTask,
                new LoggerCorrelacion())
            .InvokeAsync(contexto);

        contexto.Response.Headers[CorrelacionConstantes.Encabezado]
            .ToString().Should().NotBe(invalido);
    }

    [Fact]
    public async Task Debe_PropagarElIdentificador_EnLlamadasHttpSalientes()
    {
        var manejadorFinal = new ManejadorFinal();
        var propagador = new CorrelacionDelegatingHandler(
            new ContextoFijo("correlacion-saliente"))
        {
            InnerHandler = manejadorFinal
        };

        using var cliente = new HttpClient(propagador);
        await cliente.GetAsync("https://servicio.test/recurso");

        manejadorFinal.Solicitud.Should().NotBeNull();
        manejadorFinal.Solicitud!.Headers
            .GetValues(CorrelacionConstantes.Encabezado)
            .Single().Should().Be("correlacion-saliente");
    }

    [Fact]
    public void Debe_ResolverElIdentificadorDesdeElContextoHttp()
    {
        var contexto = new DefaultHttpContext();
        contexto.Items[CorrelacionConstantes.ItemHttp] = "correlacion-http";
        var accesor = new HttpContextAccessor { HttpContext = contexto };

        var correlacion = new CorrelacionContexto(accesor).CorrelationId;

        correlacion.Should().Be("correlacion-http");
    }

    private sealed class ContextoFijo : ICorrelacionContexto
    {
        public ContextoFijo(string correlationId)
        {
            CorrelationId = correlationId;
        }

        public string CorrelationId { get; }
    }

    private sealed class ManejadorFinal : HttpMessageHandler
    {
        public HttpRequestMessage? Solicitud { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Solicitud = request;
            return Task.FromResult(new HttpResponseMessage(
                System.Net.HttpStatusCode.OK));
        }
    }

    private sealed class LoggerCorrelacion : ILogger<CorrelacionMiddleware>
    {
        public string? Correlacion { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> valores)
            {
                Correlacion = valores
                    .FirstOrDefault(valor => valor.Key == "CorrelationId")
                    .Value?.ToString();
            }

            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }
}
