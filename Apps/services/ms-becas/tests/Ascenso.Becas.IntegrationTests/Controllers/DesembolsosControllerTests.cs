using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ascenso.Becas.Api.Contexto;
using Ascenso.Becas.Application;
using Ascenso.Becas.Application.Desembolsos;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Desembolsos.Datos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Ports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Controllers;

public sealed class DesembolsosControllerTests : IClassFixture<OfertasBecaApiFactory>
{
    private readonly OfertasBecaApiFactory _base;

    public DesembolsosControllerTests(OfertasBecaApiFactory fabrica) => _base = fabrica;

    [Fact]
    public void RegistraCasosDeUsoDeDesembolsosEnElDespacho()
    {
        var servicios = new ServiceCollection().AgregarAplicacion();
        VerificarRegistro<ICommandHandler<ProgramarCalendarioDesembolsoCommand, Guid>>(servicios);
        VerificarRegistro<ICommandHandler<ActualizarDatosPagoBecarioCommand, Guid>>(servicios);
        VerificarRegistro<ICommandHandler<RegistrarEjecucionDesembolsoCommand, DesembolsoDto>>(servicios);
        VerificarRegistro<ICommandHandler<ConfirmarDesembolsoCommand, DesembolsoDto>>(servicios);
        VerificarRegistro<ICommandHandler<RegistrarIncidenciaDesembolsoCommand, Guid>>(servicios);
        VerificarRegistro<ICommandHandler<ResolverIncidenciaDesembolsoCommand, DesembolsoDto>>(servicios);
        VerificarRegistro<ICommandHandler<ReintentarDesembolsoCommand, DesembolsoDto>>(servicios);
        VerificarRegistro<IQueryHandler<ObtenerCalendarioDesembolsosQuery,
            IReadOnlyList<CalendarioDesembolsoDto>>>(servicios);
        VerificarRegistro<IQueryHandler<ObtenerDesembolsosQuery,
            IReadOnlyList<DesembolsoDto>>>(servicios);
    }

    [Fact]
    public async Task OpenApiExponeRutasYCodigosDeRespuesta()
    {
        using var cliente = _base.CreateClient();
        using var respuesta = await cliente.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var rutas = documento.RootElement.GetProperty("paths");
        var becas = "/api/v1/becas";
        var cuotas = becas + "/calendarios-desembolso/{calendarioId}/cuotas";

        Verificar(rutas, becas + "/{becaId}/calendarios-desembolso", "post", "201", "400", "403", "409", "412", "422");
        Verificar(rutas, becas + "/{becaId}/calendarios-desembolso", "get", "200", "403");
        Verificar(rutas, becas + "/{becaId}/datos-pago", "post", "201", "400", "403");
        Verificar(rutas, cuotas, "get", "200", "403");
        Verificar(rutas, cuotas + "/{numeroCuota}/ejecucion", "post", "200", "409", "412", "422");
        Verificar(rutas, cuotas + "/{numeroCuota}/incidencias", "post", "201", "409");
        Verificar(rutas, cuotas + "/{numeroCuota}/incidencias/{incidenciaId}/resolver", "post", "200", "409");
        Verificar(rutas, cuotas + "/{numeroCuota}/reintentar", "post", "200", "409");
        Verificar(rutas, cuotas + "/{numeroCuota}/confirmar", "post", "200", "503");
    }

    [Fact]
    public async Task ProgramaYConsultaCalendarioConRol2FaYAlcance()
    {
        var becaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var periodoId = Guid.CreateVersion7();
        var calendarioId = Guid.CreateVersion7();
        var comandos = Substitute.For<ICommandDispatcher>();
        comandos.EnviarAsync<ProgramarCalendarioDesembolsoCommand, Guid>(
            Arg.Any<ProgramarCalendarioDesembolsoCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(calendarioId));
        var consultas = Substitute.For<IQueryDispatcher>();
        IReadOnlyList<CalendarioDesembolsoDto> lista =
        [new(calendarioId, becaId, periodoId, "MENSUAL", "INSTITUCION", 100m, 100m, 1, "PROGRAMADO")];
        consultas.ConsultarAsync<ObtenerCalendarioDesembolsosQuery,
            IReadOnlyList<CalendarioDesembolsoDto>>(
            Arg.Any<ObtenerCalendarioDesembolsosQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(lista));

        using var fabrica = CrearFabrica(becaId, programaId, comandos, consultas);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, programaId, "ADMIN_FINANCIERO");

        using var creada = await cliente.PostAsJsonAsync($"/api/v1/becas/{becaId}/calendarios-desembolso",
            new
            {
                periodoAcademicoId = periodoId,
                frecuencia = "MENSUAL",
                destinatario = "INSTITUCION",
                montoMaximoAprobadoPeriodo = 100m,
                fechaInicio = "2026-09-01",
                fechaFin = "2026-09-01",
                cuotas = new[] { new { numeroCuota = 1, fechaProgramada = "2026-09-01", monto = 100m } }
            });
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        Assert.Equal(calendarioId, await creada.Content.ReadFromJsonAsync<Guid>());

        using var consultada = await cliente.GetAsync($"/api/v1/becas/{becaId}/calendarios-desembolso");
        Assert.Equal(HttpStatusCode.OK, consultada.StatusCode);
        Assert.Single((await consultada.Content.ReadFromJsonAsync<CalendarioDesembolsoDto[]>())!);
        await comandos.Received(1).EnviarAsync<ProgramarCalendarioDesembolsoCommand, Guid>(
            Arg.Is<ProgramarCalendarioDesembolsoCommand>(item =>
                item.BecaId == becaId && item.Frecuencia == FrecuenciaDesembolso.Mensual
                && item.Destinatario == DestinatarioPago.Institucion),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdministradorFinancieroSinSegundoFactorRecibe401()
    {
        var becaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        using var fabrica = CrearFabrica(becaId, programaId);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, programaId, "ADMIN_FINANCIERO", con2Fa: false);

        using var respuesta = await cliente.GetAsync($"/api/v1/becas/{becaId}/calendarios-desembolso");
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task SinAlcanceRecibe403YNoDespacha()
    {
        var becaId = Guid.CreateVersion7();
        var comandos = Substitute.For<ICommandDispatcher>();
        using var fabrica = CrearFabrica(becaId, Guid.CreateVersion7(), comandos);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, Guid.CreateVersion7(), "COORDINADOR_PROGRAMA");

        using var respuesta = await cliente.PostAsJsonAsync($"/api/v1/becas/{becaId}/calendarios-desembolso",
            new
            {
                periodoAcademicoId = Guid.CreateVersion7(),
                frecuencia = "MENSUAL",
                destinatario = "INSTITUCION",
                montoMaximoAprobadoPeriodo = 100m,
                fechaInicio = "2026-09-01",
                fechaFin = "2026-09-01",
                cuotas = new[] { new { numeroCuota = 1, fechaProgramada = "2026-09-01", monto = 100m } }
            });
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        await comandos.DidNotReceive().EnviarAsync<ProgramarCalendarioDesembolsoCommand, Guid>(
            Arg.Any<ProgramarCalendarioDesembolsoCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CodigoEnumNumericoRecibe400()
    {
        var becaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        using var fabrica = CrearFabrica(becaId, programaId);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, programaId, "COORDINADOR_PROGRAMA");

        using var respuesta = await cliente.PostAsJsonAsync($"/api/v1/becas/{becaId}/calendarios-desembolso",
            new
            {
                periodoAcademicoId = Guid.CreateVersion7(),
                frecuencia = 1,
                destinatario = "INSTITUCION",
                montoMaximoAprobadoPeriodo = 100m,
                fechaInicio = "2026-09-01",
                fechaFin = "2026-09-01",
                cuotas = new[] { new { numeroCuota = 1, fechaProgramada = "2026-09-01", monto = 100m } }
            });
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(409)]
    [InlineData(412)]
    [InlineData(422)]
    public async Task ExponeErroresDeNegocio(int estado)
    {
        var becaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var excepcion = estado switch
        {
            409 => (Exception)new ConflictoOperacionException("Calendario duplicado."),
            412 => new CompromisoNoFirmadoException(becaId),
            _ => new ExcedeOfertaException(becaId)
        };
        var comandos = Substitute.For<ICommandDispatcher>();
        comandos.EnviarAsync<ProgramarCalendarioDesembolsoCommand, Guid>(
            Arg.Any<ProgramarCalendarioDesembolsoCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<Guid>(excepcion));
        using var fabrica = CrearFabrica(becaId, programaId, comandos);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, programaId, "COORDINADOR_PROGRAMA");

        using var respuesta = await cliente.PostAsJsonAsync($"/api/v1/becas/{becaId}/calendarios-desembolso",
            new
            {
                periodoAcademicoId = Guid.CreateVersion7(),
                frecuencia = "MENSUAL",
                destinatario = "INSTITUCION",
                montoMaximoAprobadoPeriodo = 100m,
                fechaInicio = "2026-09-01",
                fechaFin = "2026-09-01",
                cuotas = new[] { new { numeroCuota = 1, fechaProgramada = "2026-09-01", monto = 100m } }
            });
        Assert.Equal((HttpStatusCode)estado, respuesta.StatusCode);
    }

    [Fact]
    public async Task DatosPagoExigenIdentificadorOtpYLoEntreganAlContextoHttp()
    {
        var becaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var otpId = Guid.CreateVersion7();
        var comandos = Substitute.For<ICommandDispatcher>();
        comandos.EnviarAsync<ActualizarDatosPagoBecarioCommand, Guid>(
            Arg.Any<ActualizarDatosPagoBecarioCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Guid.CreateVersion7()));
        using var fabrica = CrearFabrica(becaId, programaId, comandos);
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, programaId, "ADMIN_FINANCIERO");
        var ruta = $"/api/v1/becas/{becaId}/datos-pago";
        var cuerpo = new
        {
            nombreBanco = "Banco",
            codigoBanco = "001",
            tipoCuenta = "AHORRO",
            numeroCuenta = "123456",
            documentoTitular = "V123",
            codigoOtp = "123456"
        };

        using var sinOtp = await cliente.PostAsJsonAsync(ruta, cuerpo);
        Assert.Equal(HttpStatusCode.BadRequest, sinOtp.StatusCode);

        cliente.DefaultRequestHeaders.Add(ContextoOperacionHttp.EncabezadoAutorizacionOtp,
            otpId.ToString());
        using var conOtp = await cliente.PostAsJsonAsync(ruta, cuerpo);
        Assert.Equal(HttpStatusCode.Created, conOtp.StatusCode);

        var contexto = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        contexto.Request.Headers[ContextoOperacionHttp.EncabezadoAutorizacionOtp] = otpId.ToString();
        var actual = new ContextoOperacionHttp(new Microsoft.AspNetCore.Http.HttpContextAccessor
        {
            HttpContext = contexto
        });
        Assert.Equal(otpId, actual.AutorizacionOtpId);
    }

    [Fact]
    public async Task ConfirmacionFallaCerradaSinMovimientoPresupuestario()
    {
        var becaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var calendario = CrearCalendario(becaId);
        var comandos = Substitute.For<ICommandDispatcher>();
        comandos.EnviarAsync<ConfirmarDesembolsoCommand, DesembolsoDto>(
            Arg.Any<ConfirmarDesembolsoCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DesembolsoDto>(
                new DependenciaNoDisponibleException("MS-PRG: movimientos-presupuesto")));
        using var fabrica = CrearFabrica(becaId, programaId, comandos,
            calendarios: CrearRepositorioCalendario(calendario));
        using var cliente = fabrica.CreateClient();
        Autenticar(cliente, programaId, "ADMIN_FINANCIERO");
        cliente.DefaultRequestHeaders.Add(ContextoOperacionHttp.EncabezadoAutorizacionOtp,
            Guid.CreateVersion7().ToString());

        using var respuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/becas/calendarios-desembolso/{calendario.Id}/cuotas/1/confirmar",
            new { codigoOtp = "123456" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        using var problema = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("DEPENDENCIA_NO_DISPONIBLE", problema.RootElement.GetProperty("code").GetString());
        await comandos.Received(1).EnviarAsync<ConfirmarDesembolsoCommand, DesembolsoDto>(
            Arg.Is<ConfirmarDesembolsoCommand>(valor =>
                valor.CalendarioId == calendario.Id && valor.NumeroCuota == 1
                && valor.CodigoOtp == "123456"), Arg.Any<CancellationToken>());
    }

    private WebApplicationFactory<Program> CrearFabrica(Guid becaId, Guid programaId,
        ICommandDispatcher? comandos = null, IQueryDispatcher? consultas = null,
        ICalendarioDesembolsoRepository? calendarios = null)
        => _base.WithWebHostBuilder(builder => builder.ConfigureTestServices(servicios =>
        {
            var becas = Substitute.For<IConsultasBeca>();
            becas.ObtenerRecursoBecaAsync(becaId, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<RecursoBecaDto?>(
                    new RecursoBecaDto(becaId, programaId, Guid.CreateVersion7())));
            servicios.RemoveAll<IConsultasBeca>();
            servicios.AddSingleton(becas);
            servicios.RemoveAll<ICommandDispatcher>();
            servicios.AddSingleton(comandos ?? Substitute.For<ICommandDispatcher>());
            servicios.RemoveAll<IQueryDispatcher>();
            servicios.AddSingleton(consultas ?? Substitute.For<IQueryDispatcher>());
            servicios.RemoveAll<ICalendarioDesembolsoRepository>();
            servicios.AddSingleton(calendarios ?? Substitute.For<ICalendarioDesembolsoRepository>());
        }));

    private static ICalendarioDesembolsoRepository CrearRepositorioCalendario(
        CalendarioDesembolso calendario)
    {
        var repositorio = Substitute.For<ICalendarioDesembolsoRepository>();
        repositorio.ObtenerPorIdAsync(calendario.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CalendarioDesembolso?>(calendario));
        return repositorio;
    }

    private static CalendarioDesembolso CrearCalendario(Guid becaId)
    {
        var fecha = new DateOnly(2026, 9, 1);
        return CalendarioDesembolso.Programar(becaId, Guid.CreateVersion7(),
            FrecuenciaDesembolso.Mensual, DestinatarioPago.Institucion, 100m,
            fecha, fecha, [new CuotaProgramada(1, fecha, 100m)],
            new ContextoOperacion(Guid.CreateVersion7(), Guid.CreateVersion7(),
                DateTimeOffset.UtcNow));
    }

    private static void Autenticar(HttpClient cliente, Guid programaId,
        string rol, bool con2Fa = true)
    {
        cliente.DefaultRequestHeaders.Add("X-Test-Roles", rol);
        cliente.DefaultRequestHeaders.Add("X-Test-Programas", programaId.ToString());
        cliente.DefaultRequestHeaders.Add("Idempotency-Key", $"beca-api-{Guid.NewGuid():N}");
        if (con2Fa)
            cliente.DefaultRequestHeaders.Add("X-Test-Acr", "urn:ascenso:loa:2fa");
    }

    private static void Verificar(JsonElement rutas, string ruta, string metodo,
        params string[] codigos)
    {
        Assert.True(rutas.TryGetProperty(ruta, out var operaciones), ruta);
        Assert.True(operaciones.TryGetProperty(metodo, out var operacion), metodo);
        var respuestas = operacion.GetProperty("responses");
        foreach (var codigo in codigos)
            Assert.True(respuestas.TryGetProperty(codigo, out _), $"{metodo} {ruta}: {codigo}");
    }

    private static void VerificarRegistro<T>(IServiceCollection servicios)
        => Assert.Contains(servicios, registro => registro.ServiceType == typeof(T));
}
