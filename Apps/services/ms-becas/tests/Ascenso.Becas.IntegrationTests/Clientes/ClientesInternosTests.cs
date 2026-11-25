using System.Text.Json;
using System.Net;
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
    public async Task Debe_OtorgarLaPostulacion_ConIdempotenciaYCorrelacion()
    {
        using var servidor = WireMockServer.Start();
        var postulacionId = Guid.CreateVersion7();
        var ofertaId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/postulaciones/{postulacionId}/otorgar")
                .UsingPost())
            .RespondWith(Response.Create().WithStatusCode(204));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));
        await new PostulacionesClient(cliente, new CorrelacionFija())
            .OtorgarAsync(postulacionId, ofertaId);

        var peticion = servidor.LogEntries.Should().ContainSingle().Subject.RequestMessage!;
        peticion.Headers!["Idempotency-Key"]!.Single().Should().Be($"otorgar-{ofertaId}");
        peticion.Headers[CorrelacionConstantes.Encabezado]!.Single().Should().Be(Correlacion);
    }

    [Theory]
    [InlineData(409, "POSTULACION_NO_TRANSICIONABLE")]
    [InlineData(422, "POSTULACION_NO_OTORGABLE")]
    public async Task Debe_MapearElRechazoDeOtorgamiento_Cuando_MS_POSRespondeError(
        int estadoHttp,
        string codigoEsperado)
    {
        using var servidor = WireMockServer.Start();
        var postulacionId = Guid.CreateVersion7();
        var ofertaId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/postulaciones/{postulacionId}/otorgar")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(estadoHttp)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{}"));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));

        var accion = () => new PostulacionesClient(cliente, new CorrelacionFija())
            .OtorgarAsync(postulacionId, ofertaId);

        var excepcion = await accion.Should()
            .ThrowAsync<PostulacionOtorgamientoRechazadaException>();
        excepcion.Which.Codigo.Should().Be(codigoEsperado);
        excepcion.Which.EstadoHttp.Should().Be(estadoHttp);
    }

    [Fact]
    public async Task Debe_PropagarEl503_Cuando_MS_POSNoPuedeOtorgarLaPostulacion()
    {
        using var servidor = WireMockServer.Start();
        var postulacionId = Guid.CreateVersion7();
        var ofertaId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/postulaciones/{postulacionId}/otorgar")
                .UsingPost())
            .RespondWith(Response.Create().WithStatusCode(503));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));

        var accion = () => new PostulacionesClient(cliente, new CorrelacionFija())
            .OtorgarAsync(postulacionId, ofertaId);

        var excepcion = await accion.Should().ThrowAsync<HttpRequestException>();
        excepcion.Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
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
    public async Task Debe_ObtenerCondicionesVigentes_Cuando_MS_PRGEncuentraLaConfiguracion()
    {
        using var servidor = WireMockServer.Start();
        var programaId = Guid.CreateVersion7();
        var carreraId = Guid.CreateVersion7();
        var periodoId = Guid.CreateVersion7();
        var universidadId = Guid.CreateVersion7();
        var costoId = Guid.CreateVersion7();
        var convenioId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/programas/{programaId}/condiciones")
                .WithParam("carreraId", carreraId.ToString())
                .WithParam("periodoId", periodoId.ToString())
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    programaBecaId = programaId,
                    carreraId,
                    periodoAcademicoId = periodoId,
                    universidadAliadaId = universidadId,
                    politicaCobertura = new
                    {
                        modalidad = "MONTO_FIJO",
                        porcentajeCobertura = 0m,
                        montoFijoPeriodo = 750m,
                        moneda = "USD"
                    },
                    condicionesPermanencia = new
                    {
                        promedioMinimoRenovacion = 14m,
                        materiasMinimasAprobadas = 4,
                        maximoMateriasReprobadas = 1,
                        horasServicioPorPeriodo = 80,
                        consecuenciaIncumplimiento = "SUSPENSION"
                    },
                    costoMatricula = new
                    {
                        costoMatriculaReferenciaId = costoId,
                        carreraId,
                        periodoAcademicoId = periodoId,
                        montoReferenciaMatricula = 1500m,
                        moneda = "USD",
                        numeroVersion = 2,
                        fechaVigenciaDesde = new DateOnly(2026, 1, 1),
                        fechaVigenciaHasta = (DateOnly?)null,
                        estado = "VIGENTE"
                    },
                    convenio = new
                    {
                        universidadAliadaId = universidadId,
                        convenioId,
                        estado = "VIGENTE",
                        fechaFin = new DateOnly(2027, 12, 31)
                    }
                })));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));
        var resultado = await new ProgramasClient(cliente, new CorrelacionFija())
            .ObtenerCondicionesAsync(programaId, carreraId, periodoId, universidadId);

        resultado.Should().NotBeNull();
        resultado!.PoliticaCobertura.Modalidad.Should().Be(TipoCobertura.MontoFijo);
        resultado.PoliticaCobertura.MontoFijoPeriodo.Should().Be(750m);
        resultado.CondicionesPermanencia.ConsecuenciaIncumplimiento
            .Should().Be(ConsecuenciaPermanencia.Suspension);
        resultado.CostoMatricula.CostoMatriculaReferenciaId.Should().Be(costoId);
        resultado.CostoMatricula.MontoReferenciaMatricula.Should().Be(1500m);
        resultado.Convenio.ConvenioId.Should().Be(convenioId);

        var peticion = servidor.LogEntries.Should().ContainSingle().Subject.RequestMessage!;
        peticion.Headers![CorrelacionConstantes.Encabezado]!.Single().Should().Be(Correlacion);
    }

    [Fact]
    public async Task Debe_DevolverCondicionesNulas_Cuando_MS_PRGNoEncuentraLaConfiguracion()
    {
        using var servidor = WireMockServer.Start();
        var programaId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/programas/{programaId}/condiciones")
                .UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));
        var resultado = await new ProgramasClient(cliente, new CorrelacionFija())
            .ObtenerCondicionesAsync(
                programaId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7());

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task Debe_ResponderConfiguracionNoDisponible_Cuando_MS_PRGDevuelveCondicionesIncompletas()
    {
        using var servidor = WireMockServer.Start();
        var programaId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/programas/{programaId}/condiciones")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"politicaCobertura":null,"condicionesPermanencia":null}"""));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));

        var accion = () => new ProgramasClient(cliente, new CorrelacionFija())
            .ObtenerCondicionesAsync(
                programaId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7());

        var excepcion = await accion.Should()
            .ThrowAsync<ConfiguracionProgramaNoDisponibleException>();
        excepcion.Which.Codigo.Should().Be(
            ConfiguracionProgramaNoDisponibleException.CodigoError);
        excepcion.Which.EstadoHttp.Should().Be(422);
    }

    [Fact]
    public async Task Debe_MapearConvenioVencido_Cuando_MS_PRGRechazaLasCondiciones()
    {
        using var servidor = WireMockServer.Start();
        var programaId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/programas/{programaId}/condiciones")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(422)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"codigo":"CONVENIO_VENCIDO"}"""));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));

        var accion = () => new ProgramasClient(cliente, new CorrelacionFija())
            .ObtenerCondicionesAsync(
                programaId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7());

        var excepcion = await accion.Should()
            .ThrowAsync<ReservaPresupuestoRechazadaException>();
        excepcion.Which.Codigo.Should().Be(
            ReservaPresupuestoRechazadaException.CodigoConvenioVencido);
    }

    [Theory]
    [InlineData("confirmar")]
    [InlineData("activar")]
    [InlineData("liberar")]
    public async Task Debe_CambiarEstadoDeReserva_ConClaveEstable(
        string operacion)
    {
        using var servidor = WireMockServer.Start();
        var programaId = Guid.CreateVersion7();
        var reservaId = Guid.CreateVersion7();
        var ofertaId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath($"/internal/v1/programas/{programaId}/reservas/{reservaId}/{operacion}")
                .UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));
        var programas = new ProgramasClient(cliente, new CorrelacionFija());

        for (var intento = 0; intento < 2; intento++)
        {
            switch (operacion)
            {
                case "confirmar":
                    await programas.ConfirmarReservaAsync(programaId, reservaId, ofertaId);
                    break;
                case "activar":
                    await programas.ActivarReservaAsync(programaId, reservaId, ofertaId);
                    break;
                default:
                    await programas.LiberarReservaAsync(programaId, reservaId, ofertaId);
                    break;
            }
        }

        servidor.LogEntries.Should().HaveCount(2);
        foreach (var entrada in servidor.LogEntries)
        {
            entrada.RequestMessage!.Headers![ProgramasClient.EncabezadoIdempotencia]!
                .Single().Should().Be($"{operacion}-{ofertaId}");
            entrada.RequestMessage.Headers[CorrelacionConstantes.Encabezado]!
                .Single().Should().Be(Correlacion);
        }
    }

    [Fact]
    public async Task Debe_ObtenerElPeriodoActual_Cuando_MS_PLTRespondeConUnPeriodoActivo()
    {
        using var servidor = WireMockServer.Start();
        var periodoId = Guid.CreateVersion7();

        servidor
            .Given(Request.Create()
                .WithPath("/internal/v1/periodos-academicos/actual")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    periodoAcademicoId = periodoId,
                    codigo = "2026-2",
                    nombre = "Segundo periodo 2026",
                    ordenSecuencia = 2,
                    fechaInicio = new DateOnly(2026, 7, 1),
                    fechaFin = new DateOnly(2026, 12, 31),
                    estado = "ACTIVO",
                    esActual = true
                })));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));
        var resultado = await new PeriodosAcademicosClient(cliente, new CorrelacionFija())
            .ObtenerActualAsync();

        resultado.PeriodoAcademicoId.Should().Be(periodoId);
        resultado.Codigo.Should().Be("2026-2");
        resultado.EsActual.Should().BeTrue();

        var peticion = servidor.LogEntries.Should().ContainSingle().Subject.RequestMessage!;
        peticion.Headers![CorrelacionConstantes.Encabezado]!.Single().Should().Be(Correlacion);
    }

    [Fact]
    public async Task Debe_FallarCerrado_Cuando_MS_PLTResponde503AlConsultarPeriodoActual()
    {
        using var servidor = WireMockServer.Start();

        servidor
            .Given(Request.Create()
                .WithPath("/internal/v1/periodos-academicos/actual")
                .UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        using var cliente = CrearCliente(servidor.Url, TimeSpan.FromSeconds(3));

        var accion = () => new PeriodosAcademicosClient(cliente, new CorrelacionFija())
            .ObtenerActualAsync();

        var excepcion = await accion.Should().ThrowAsync<HttpRequestException>();
        excepcion.Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
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
