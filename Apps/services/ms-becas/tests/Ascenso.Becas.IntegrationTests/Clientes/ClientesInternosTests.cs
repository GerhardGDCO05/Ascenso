using System.Text.Json;
using Ascenso.Becas.Application.Correlacion;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Clientes;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Ascenso.Becas.IntegrationTests.Clientes;

public sealed class ClientesInternosTests
{
    private const string Correlacion = "correlacion-cliente-123";

    [Fact]
    public async Task Debe_ConsultarLaAdjudicabilidad_Cuando_MS_POSResponde200()
    {
        using var servidor = WireMockServer.Start();
        var postulacionId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var periodoId = Guid.CreateVersion7();
        var universidadId = Guid.CreateVersion7();
        var carreraId = Guid.CreateVersion7();
        var estudianteId = Guid.CreateVersion7();
        var convocatoriaId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/postulaciones/{postulacionId}/adjudicable")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    postulacionId,
                    adjudicable = true,
                    programaBecaId = programaId,
                    periodoAcademicoId = periodoId,
                    periodoCodigo = "2026-1",
                    universidadAliadaId = universidadId,
                    carreraId,
                    postulanteEstudianteId = estudianteId,
                    nombreEstudiante = "Ana Pérez",
                    numeroDocumentoEstudiante = "12345678",
                    convocatoriaOrigenId = convocatoriaId
                })));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));
        var resultado = await new PostulacionesClient(cliente, new CorrelacionFija())
            .ObtenerAdjudicabilidadAsync(postulacionId);

        resultado.Adjudicable.Should().BeTrue();
        resultado.PostulacionId.Should().Be(postulacionId);
        resultado.ProgramaBecaId.Should().Be(programaId);
        resultado.PeriodoAcademicoId.Should().Be(periodoId);
        resultado.PeriodoCodigo.Should().Be("2026-1");
        resultado.UniversidadAliadaId.Should().Be(universidadId);
        resultado.CarreraId.Should().Be(carreraId);
        resultado.PostulanteEstudianteId.Should().Be(estudianteId);
        resultado.NombreEstudiante.Should().Be("Ana Pérez");
        resultado.NumeroDocumentoEstudiante.Should().Be("12345678");
        resultado.ConvocatoriaOrigenId.Should().Be(convocatoriaId);

        var peticion = servidor.LogEntries.Should().ContainSingle().Subject.RequestMessage!;
        peticion.Headers![CorrelacionConstantes.Encabezado]!.Single().Should().Be(Correlacion);
    }

    [Fact]
    public async Task Debe_MapearEl404_Cuando_LaPostulacionNoExisteEnMS_POS()
    {
        using var servidor = WireMockServer.Start();
        var postulacionId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/postulaciones/{postulacionId}/adjudicable")
                .UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));

        var accion = () => new PostulacionesClient(cliente, new CorrelacionFija())
            .ObtenerAdjudicabilidadAsync(postulacionId);

        var excepcion = await accion.Should().ThrowAsync<PostulacionNoEncontradaException>();
        excepcion.Which.Codigo.Should().Be("POSTULACION_NO_ENCONTRADA");
        excepcion.Which.EstadoHttp.Should().Be(422);
        excepcion.Which.PostulacionId.Should().Be(postulacionId);
    }

    [Fact]
    public async Task Debe_TratarComoNoAdjudicable_Cuando_MS_POSRespondeFalse()
    {
        using var servidor = WireMockServer.Start();
        var postulacionId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/postulaciones/{postulacionId}/adjudicable")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    postulacionId,
                    adjudicable = false,
                    motivoNoAdjudicable = "sin cupo disponible"
                })));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));
        var resultado = await new PostulacionesClient(cliente, new CorrelacionFija())
            .ObtenerAdjudicabilidadAsync(postulacionId);

        resultado.Adjudicable.Should().BeFalse();
        resultado.MotivoNoAdjudicable.Should().Be("sin cupo disponible");
    }

    [Fact]
    public async Task Debe_PropagarElTimeout_Cuando_MS_POSNoRespondeATiempo()
    {
        using var servidor = WireMockServer.Start();
        var postulacionId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/postulaciones/{postulacionId}/adjudicable")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(5))
                .WithBody("{}"));

        using var cliente = CrearCliente(
            servidor.Url,
            TimeSpan.FromMilliseconds(200));

        var accion = () => new PostulacionesClient(cliente, new CorrelacionFija())
            .ObtenerAdjudicabilidadAsync(postulacionId);

        await accion.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Debe_ReservarPresupuesto_Cuando_MS_PRGAceptaLaSolicitud()
    {
        using var servidor = WireMockServer.Start();
        var programaId = Guid.CreateVersion7();
        var ofertaId = Guid.CreateVersion7();
        var periodoId = Guid.CreateVersion7();
        var reservaId = Guid.CreateVersion7();
        var expiraEn = DateTimeOffset.UtcNow.AddMinutes(30);

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/programas/{programaId}/reservas")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    reservaId,
                    estado = "PENDIENTE",
                    monto = 2000m,
                    cupos = 1,
                    expiraEn
                })));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));
        var solicitud = new SolicitudReservaPresupuesto(
            "OFERTA_BECA",
            ofertaId,
            periodoId,
            2000m,
            1);

        var resultado = await new ProgramasClient(cliente, new CorrelacionFija())
            .ReservarAsync(programaId, solicitud);

        resultado.ReservaId.Should().Be(reservaId);
        resultado.Estado.Should().Be("PENDIENTE");
        resultado.Monto.Should().Be(2000m);
        resultado.Cupos.Should().Be(1);
        resultado.ExpiraEn.Should().Be(expiraEn);

        var peticion = servidor.LogEntries.Should().ContainSingle().Subject.RequestMessage!;
        peticion.Headers!["Idempotency-Key"]!.Single().Should().Be($"oferta-{ofertaId}");
        peticion.Headers![CorrelacionConstantes.Encabezado]!.Single().Should().Be(Correlacion);

        using var cuerpo = JsonDocument.Parse(peticion.Body!);
        cuerpo.RootElement.GetProperty("referenciaTipo").GetString()
            .Should().Be("OFERTA_BECA");
        cuerpo.RootElement.GetProperty("referenciaId").GetGuid().Should().Be(ofertaId);
        cuerpo.RootElement.GetProperty("periodoAcademicoId").GetGuid().Should().Be(periodoId);
        cuerpo.RootElement.GetProperty("monto").GetDecimal().Should().Be(2000m);
        cuerpo.RootElement.GetProperty("cupos").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Debe_ReutilizarLaMismaClaveDeIdempotencia_Cuando_SeRepiteLaReserva()
    {
        using var servidor = WireMockServer.Start();
        var programaId = Guid.CreateVersion7();
        var ofertaId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/programas/{programaId}/reservas")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    reservaId = Guid.CreateVersion7(),
                    estado = "PENDIENTE",
                    monto = 1000m,
                    cupos = 1,
                    expiraEn = DateTimeOffset.UtcNow.AddMinutes(30)
                })));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));
        var clienteProgramas = new ProgramasClient(cliente, new CorrelacionFija());
        var solicitud = new SolicitudReservaPresupuesto(
            "OFERTA_BECA",
            ofertaId,
            Guid.CreateVersion7(),
            1000m,
            1);

        await clienteProgramas.ReservarAsync(programaId, solicitud);
        await clienteProgramas.ReservarAsync(programaId, solicitud);

        var claves = servidor.LogEntries
            .Select(entrada => entrada.RequestMessage!.Headers!["Idempotency-Key"]!.Single())
            .ToList();

        claves.Should().HaveCount(2);
        claves.Should().OnlyContain(clave => clave == $"oferta-{ofertaId}");
    }

    [Fact]
    public async Task Debe_MapearEl409_Cuando_MS_PRGSinCupoOPresupuesto()
    {
        using var servidor = WireMockServer.Start();
        var programaId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/programas/{programaId}/reservas")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(409)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {"code":"SIN_CUPO_O_PRESUPUESTO","message":"sin cupo"}
                    """));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));

        var accion = () => new ProgramasClient(cliente, new CorrelacionFija())
            .ReservarAsync(
                programaId,
                new SolicitudReservaPresupuesto(
                    "OFERTA_BECA",
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    1000m,
                    1));

        var excepcion = await accion.Should()
            .ThrowAsync<ReservaPresupuestoRechazadaException>();
        excepcion.Which.Codigo.Should().Be(
            ReservaPresupuestoRechazadaException.CodigoSinCupoOPresupuesto);
        excepcion.Which.EstadoHttp.Should().Be(409);
    }

    [Fact]
    public async Task Debe_MapearEl422_Cuando_MS_PRGConvenioVencido()
    {
        using var servidor = WireMockServer.Start();
        var programaId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/programas/{programaId}/reservas")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(422)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {"codigo":"CONVENIO_VENCIDO","mensaje":"convenio vencido"}
                    """));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));

        var accion = () => new ProgramasClient(cliente, new CorrelacionFija())
            .ReservarAsync(
                programaId,
                new SolicitudReservaPresupuesto(
                    "OFERTA_BECA",
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    1000m,
                    1));

        var excepcion = await accion.Should()
            .ThrowAsync<ReservaPresupuestoRechazadaException>();
        excepcion.Which.Codigo.Should().Be(
            ReservaPresupuestoRechazadaException.CodigoConvenioVencido);
        excepcion.Which.EstadoHttp.Should().Be(422);
    }

    [Fact]
    public async Task Debe_ConsumirElOtp_Cuando_MS_PLTConfirmaElConsumo()
    {
        using var servidor = WireMockServer.Start();
        var usuarioId = Guid.CreateVersion7();
        var recursoId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath("/internal/v1/otp/consumir")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"consumido":true}"""));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));
        var resultado = await new OtpClient(cliente, new CorrelacionFija())
            .ConsumirAsync(
                usuarioId,
                TipoOperacionOTP.ConfirmarDesembolso,
                recursoId,
                "huella-payload",
                "123456");

        resultado.Consumido.Should().BeTrue();
        resultado.CodigoError.Should().BeNull();

        var peticion = servidor.LogEntries.Should().ContainSingle().Subject.RequestMessage!;
        peticion.Headers!["Idempotency-Key"]!.Single().Should()
            .Be($"otp-{usuarioId:N}-CONFIRMAR_DESEMBOLSO-{recursoId:N}");

        using var cuerpo = JsonDocument.Parse(peticion.Body!);
        cuerpo.RootElement.GetProperty("usuarioId").GetGuid().Should().Be(usuarioId);
        cuerpo.RootElement.GetProperty("operacion").GetString()
            .Should().Be("CONFIRMAR_DESEMBOLSO");
        cuerpo.RootElement.GetProperty("recursoId").GetGuid().Should().Be(recursoId);
        cuerpo.RootElement.GetProperty("huellaPayload").GetString()
            .Should().Be("huella-payload");
        cuerpo.RootElement.GetProperty("codigo").GetString().Should().Be("123456");
    }

    [Theory]
    [InlineData(403, "OTP_BLOQUEADO")]
    [InlineData(410, "OTP_EXPIRADO")]
    [InlineData(422, "OTP_INVALIDO")]
    public async Task Debe_DevolverElCodigoDeError_Cuando_MS_PLTRechazaElOtp(
        int estado,
        string codigo)
    {
        using var servidor = WireMockServer.Start();

        servidor
            .Given(Request.Create()
                .WithPath("/internal/v1/otp/consumir")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(estado)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { code = codigo })));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));
        var resultado = await new OtpClient(cliente, new CorrelacionFija())
            .ConsumirAsync(
                Guid.CreateVersion7(),
                TipoOperacionOTP.RevocarBeca,
                Guid.CreateVersion7(),
                "huella-payload",
                "123456");

        resultado.Consumido.Should().BeFalse();
        resultado.CodigoError.Should().Be(codigo);
    }

    private static HttpClient CrearCliente(string? url, TimeSpan timeout)
        => new()
        {
            BaseAddress = new Uri(url ?? throw new InvalidOperationException("WireMock no inició.")),
            Timeout = timeout
        };

    private sealed class CorrelacionFija : ICorrelacionContexto
    {
        public string CorrelationId => Correlacion;
    }
}
