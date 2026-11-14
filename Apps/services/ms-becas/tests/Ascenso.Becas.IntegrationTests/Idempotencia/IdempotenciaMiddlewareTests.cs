using System.Text;
using Ascenso.Becas.Api.Idempotencia;
using Ascenso.Becas.Application.Idempotencia;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Ascenso.Becas.IntegrationTests.Idempotencia;

public sealed class IdempotenciaMiddlewareTests
{
    private const string Ruta = "/api/v1/becas/ofertas";
    private const string Clave = "clave-idempotente-01";

    [Fact]
    public async Task Debe_ReplicarLaRespuesta_Cuando_SeRepiteLaClaveYElPayload()
    {
        var almacen = new AlmacenIdempotenciaEnMemoria();
        var ejecuciones = 0;
        RequestDelegate siguiente = async contexto =>
        {
            ejecuciones++;
            contexto.Response.StatusCode = StatusCodes.Status201Created;
            await contexto.Response.WriteAsync("""{"id":"1"}""");
        };

        await InvocarAsync(
            CrearContexto("""{"programa":"A","monto":100}"""),
            almacen,
            siguiente);

        var repeticion = CrearContexto("""{ "monto": 100, "programa": "A" }""");
        var cuerpo = await InvocarAsync(repeticion, almacen, siguiente);

        ejecuciones.Should().Be(1);
        repeticion.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        cuerpo.Should().Be("""{"id":"1"}""");
        repeticion.Response.Headers[IdempotenciaConstantes.EncabezadoReplay]
            .ToString().Should().Be("true");
    }

    [Fact]
    public async Task Debe_Rechazar_Cuando_LaClaveSeReutilizaConOtroPayload()
    {
        var almacen = new AlmacenIdempotenciaEnMemoria();
        var ejecuciones = 0;
        RequestDelegate siguiente = async contexto =>
        {
            ejecuciones++;
            contexto.Response.StatusCode = StatusCodes.Status201Created;
            await contexto.Response.WriteAsync("""{"id":"1"}""");
        };

        await InvocarAsync(CrearContexto("""{"monto":100}"""), almacen, siguiente);

        var repeticion = CrearContexto("""{"monto":200}""");
        var cuerpo = await InvocarAsync(repeticion, almacen, siguiente);

        ejecuciones.Should().Be(1);
        repeticion.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        cuerpo.Should().Contain("IDEMPOTENCY_KEY_REUTILIZADA");
    }

    [Fact]
    public async Task Debe_ResponderEnProceso_Cuando_LaSolicitudAunNoTermina()
    {
        var almacen = new AlmacenIdempotenciaEnMemoria();
        var contexto = CrearContexto("""{"monto":100}""");
        var hash = await HashPayload.CalcularAsync(
            contexto.Request,
            CancellationToken.None);
        var ahora = DateTimeOffset.UtcNow;

        almacen.Registros[(Endpoint(), Clave)] = new RegistroIdempotencia(
            Guid.CreateVersion7(),
            Endpoint(),
            Clave,
            hash,
            IdempotenciaConstantes.EstadoEnProceso,
            null,
            null,
            null,
            ahora,
            ahora.AddDays(7));

        var ejecuciones = 0;
        var cuerpo = await InvocarAsync(
            contexto,
            almacen,
            _ =>
            {
                ejecuciones++;
                return Task.CompletedTask;
            });

        ejecuciones.Should().Be(0);
        contexto.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        contexto.Response.Headers.RetryAfter.ToString().Should().Be("1");
        cuerpo.Should().Contain("IDEMPOTENCIA_EN_PROCESO");
    }

    [Fact]
    public async Task Debe_EjecutarDeNuevo_Cuando_ElRegistroEstaExpirado()
    {
        var almacen = new AlmacenIdempotenciaEnMemoria();
        var contexto = CrearContexto("""{"monto":100}""");
        var hash = await HashPayload.CalcularAsync(
            contexto.Request,
            CancellationToken.None);
        var ahora = DateTimeOffset.UtcNow;

        almacen.Registros[(Endpoint(), Clave)] = new RegistroIdempotencia(
            Guid.CreateVersion7(),
            Endpoint(),
            Clave,
            hash,
            IdempotenciaConstantes.EstadoCompletada,
            StatusCodes.Status201Created,
            """{"id":"viejo"}""",
            null,
            ahora.AddDays(-8),
            ahora.AddDays(-1));

        var ejecuciones = 0;
        await InvocarAsync(
            contexto,
            almacen,
            async ctx =>
            {
                ejecuciones++;
                ctx.Response.StatusCode = StatusCodes.Status201Created;
                await ctx.Response.WriteAsync("""{"id":"nuevo"}""");
            });

        ejecuciones.Should().Be(1);
        contexto.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        almacen.Registros[(Endpoint(), Clave)].ResponseBody
            .Should().Be("""{"id":"nuevo"}""");
    }

    [Fact]
    public async Task Debe_Replicar_Cuando_ElMultipartCambiaDeBoundary()
    {
        var almacen = new AlmacenIdempotenciaEnMemoria();
        var ejecuciones = 0;
        RequestDelegate siguiente = async contexto =>
        {
            ejecuciones++;
            contexto.Response.StatusCode = StatusCodes.Status201Created;
            await contexto.Response.WriteAsync("""{"id":"1"}""");
        };

        await InvocarAsync(
            CrearContextoMultipart("----boundary-uno"),
            almacen,
            siguiente);

        var repeticion = CrearContextoMultipart("----boundary-dos");
        await InvocarAsync(repeticion, almacen, siguiente);

        ejecuciones.Should().Be(1);
        repeticion.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        repeticion.Response.Headers[IdempotenciaConstantes.EncabezadoReplay]
            .ToString().Should().Be("true");
    }

    [Fact]
    public async Task Debe_Rechazar_Cuando_FaltaLaClaveDeIdempotencia()
    {
        var almacen = new AlmacenIdempotenciaEnMemoria();
        var contexto = CrearContexto("""{"monto":100}""", clave: null);
        var ejecuciones = 0;

        var cuerpo = await InvocarAsync(
            contexto,
            almacen,
            _ =>
            {
                ejecuciones++;
                return Task.CompletedTask;
            });

        ejecuciones.Should().Be(0);
        contexto.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        cuerpo.Should().Contain("IDEMPOTENCY_KEY_REQUERIDA");
    }

    [Fact]
    public async Task Debe_Rechazar_Cuando_LaClaveEsDemasiadoCorta()
    {
        var almacen = new AlmacenIdempotenciaEnMemoria();
        var contexto = CrearContexto("""{"monto":100}""", clave: "corta");

        var cuerpo = await InvocarAsync(
            contexto,
            almacen,
            _ => Task.CompletedTask);

        contexto.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        cuerpo.Should().Contain("IDEMPOTENCY_KEY_INVALIDA");
    }

    [Fact]
    public async Task Debe_RevertirElRegistro_Cuando_ElCasoDeUsoFalla()
    {
        var almacen = new AlmacenIdempotenciaEnMemoria();
        var contexto = CrearContexto("""{"monto":100}""");

        var accion = () => InvocarAsync(
            contexto,
            almacen,
            _ => throw new InvalidOperationException("fallo simulado"));

        await accion.Should().ThrowAsync<InvalidOperationException>();
        almacen.Registros.Should().BeEmpty();
        almacen.Reversiones.Should().Be(1);
    }

    [Fact]
    public async Task Debe_PermitirLeerElCuerpo_Cuando_ElMiddlewareCalculaElHash()
    {
        var almacen = new AlmacenIdempotenciaEnMemoria();
        var contexto = CrearContexto("""{"monto":100}""");
        string? cuerpoLeido = null;

        await InvocarAsync(
            contexto,
            almacen,
            async ctx =>
            {
                using var lector = new StreamReader(ctx.Request.Body);
                cuerpoLeido = await lector.ReadToEndAsync();
                ctx.Response.StatusCode = StatusCodes.Status204NoContent;
            });

        cuerpoLeido.Should().Be("""{"monto":100}""");
    }

    [Fact]
    public async Task Debe_OmitirLaIdempotencia_Cuando_NoEsUnComandoDeLaApi()
    {
        var almacen = new AlmacenIdempotenciaEnMemoria();
        var contexto = CrearContexto("""{"monto":100}""", clave: null);
        contexto.Request.Method = HttpMethods.Get;
        var ejecuciones = 0;

        await InvocarAsync(
            contexto,
            almacen,
            _ =>
            {
                ejecuciones++;
                return Task.CompletedTask;
            });

        ejecuciones.Should().Be(1);
        almacen.Registros.Should().BeEmpty();
    }

    private static string Endpoint() => $"POST {Ruta}";

    private static DefaultHttpContext CrearContexto(
        string cuerpo,
        string? clave = Clave,
        string? contentType = "application/json")
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Method = HttpMethods.Post;
        contexto.Request.Path = Ruta;
        contexto.Request.ContentType = contentType;
        contexto.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(cuerpo));
        contexto.Response.Body = new MemoryStream();

        if (clave is not null)
        {
            contexto.Request.Headers[IdempotenciaConstantes.Encabezado] = clave;
        }

        return contexto;
    }

    private static DefaultHttpContext CrearContextoMultipart(string boundary)
    {
        var cuerpo = $"""
            --{boundary}
            Content-Disposition: form-data; name="archivo"; filename="archivo.pdf"
            Content-Type: application/pdf

            contenido-del-archivo
            --{boundary}--
            """;

        return CrearContexto(
            cuerpo,
            contentType: $"multipart/form-data; boundary={boundary}");
    }

    private static async Task<string> InvocarAsync(
        DefaultHttpContext contexto,
        IAlmacenIdempotencia almacen,
        RequestDelegate siguiente)
    {
        var middleware = new IdempotenciaMiddleware(siguiente);
        await middleware.InvokeAsync(contexto, almacen);

        contexto.Response.Body.Position = 0;
        return await new StreamReader(contexto.Response.Body).ReadToEndAsync();
    }

    private sealed class AlmacenIdempotenciaEnMemoria : IAlmacenIdempotencia
    {
        private readonly List<(string Endpoint, string Clave)> _pendientes = [];
        private bool _enTransaccion;

        public Dictionary<(string Endpoint, string Clave), RegistroIdempotencia>
            Registros
        { get; } = [];

        public int Reversiones { get; private set; }

        public Task<RegistroIdempotencia?> ObtenerAsync(
            string endpoint,
            string clave,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                Registros.TryGetValue((endpoint, clave), out var registro)
                    ? registro
                    : null);

        public Task IniciarTransaccionAsync(
            CancellationToken cancellationToken = default)
        {
            _enTransaccion = true;
            _pendientes.Clear();
            return Task.CompletedTask;
        }

        public Task<bool> IntentarRegistrarAsync(
            RegistroIdempotencia registro,
            CancellationToken cancellationToken = default)
        {
            var llave = (registro.Endpoint, registro.Clave);

            if (Registros.ContainsKey(llave))
            {
                return Task.FromResult(false);
            }

            Registros[llave] = registro;

            if (_enTransaccion)
            {
                _pendientes.Add(llave);
            }

            return Task.FromResult(true);
        }

        public Task CompletarAsync(
            string endpoint,
            string clave,
            int statusCode,
            string? responseBody,
            Guid? recursoId,
            CancellationToken cancellationToken = default)
        {
            var llave = (endpoint, clave);

            if (Registros.TryGetValue(llave, out var registro))
            {
                Registros[llave] = registro with
                {
                    Estado = IdempotenciaConstantes.EstadoCompletada,
                    StatusCode = statusCode,
                    ResponseBody = responseBody,
                    RecursoId = recursoId
                };
            }

            return Task.CompletedTask;
        }

        public Task EliminarAsync(
            string endpoint,
            string clave,
            CancellationToken cancellationToken = default)
        {
            Registros.Remove((endpoint, clave));
            return Task.CompletedTask;
        }

        public Task ConfirmarAsync(CancellationToken cancellationToken = default)
        {
            _enTransaccion = false;
            _pendientes.Clear();
            return Task.CompletedTask;
        }

        public Task RevertirAsync(CancellationToken cancellationToken = default)
        {
            foreach (var llave in _pendientes)
            {
                Registros.Remove(llave);
            }

            _pendientes.Clear();
            _enTransaccion = false;
            Reversiones++;
            return Task.CompletedTask;
        }
    }
}
