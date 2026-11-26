using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;
using Ascenso.Becas.Domain.ValueObjects;
using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Ascenso.Becas.Infrastructure.Trabajos;
using FluentAssertions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Ascenso.Becas.IntegrationTests.EndToEnd;

[Collection(ColeccionExtremoAExtremo.Nombre)]
public sealed class OtorgamientoExtremoAExtremoTests
{
    private const string EncabezadoIdempotencia = "Idempotency-Key";
    private const string EncabezadoRoles = "X-Test-Roles";
    private const string EncabezadoPostulante = "X-Test-Postulante";
    private const string EncabezadoProgramas = "X-Test-Programas";
    private const string EncabezadoAcr = "X-Test-Acr";
    private const string EncabezadoCliente = "X-Test-Cliente";
    private const string EncabezadoScopes = "X-Test-Scopes";

    private static readonly JsonSerializerOptions _opcionesJson =
        new(JsonSerializerDefaults.Web);

    private readonly ExtremoAExtremoFixture _fixture;

    public OtorgamientoExtremoAExtremoTests(ExtremoAExtremoFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_EmitirAceptarYConsultarLaBecaActiva_ConLosEventosEsperados()
    {
        var escenario = NuevoEscenario();
        await SembrarProyeccionesAsync(escenario);
        ConfigurarStubs(escenario);

        using var fabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var coordinador = CrearCoordinador(fabrica, escenario.ProgramaId);

        var oferta = await EmitirAsync(coordinador, escenario.PostulacionId);

        oferta.Estado.Should().Be("PENDIENTE_RESPUESTA");

        await using (var contexto = _fixture.CrearContexto())
        {
            var beca = await contexto.Becas
                .Include(registro => registro.Oferta)
                .SingleAsync(registro => registro.Id == oferta.BecaId);

            beca.Estado.Should().Be(EstadoBeca.Otorgada);
            beca.Oferta!.Estado.Should().Be(EstadoOferta.PendienteRespuesta);
        }

        fabrica.Outbox.Eventos.OfType<OfertaBecaEmitidaEvento>()
            .Should().ContainSingle(evento =>
                evento.BecaId == oferta.BecaId
                && evento.OfertaId == oferta.OfertaId
                && evento.PostulacionId == escenario.PostulacionId);

        using var postulante = CrearPostulante(fabrica, escenario.EstudianteId);
        using var respuestaAceptar = await AceptarAsync(
            postulante,
            oferta.OfertaId,
            $"aceptar-{Guid.NewGuid():N}");

        respuestaAceptar.StatusCode.Should().Be(HttpStatusCode.OK);

        var respondida = await respuestaAceptar.Content
            .ReadFromJsonAsync<OfertaRespondidaDto>(_opcionesJson);

        respondida.Should().NotBeNull();
        respondida!.EstadoOferta.Should().Be("ACEPTADA");
        respondida.EstadoBeca.Should().Be("ACTIVA");

        await using (var contexto = _fixture.CrearContexto())
        {
            var beca = await contexto.Becas
                .Include(registro => registro.Oferta)
                .SingleAsync(registro => registro.Id == oferta.BecaId);

            beca.Estado.Should().Be(EstadoBeca.Activa);
            beca.Oferta!.Estado.Should().Be(EstadoOferta.Aceptada);
            beca.FechaActivacion.Should().NotBeNull();
        }

        fabrica.Outbox.Eventos.OfType<BecaActivadaEvento>()
            .Should().ContainSingle(evento =>
                evento.BecaId == oferta.BecaId
                && evento.OfertaId == oferta.OfertaId);

        using var servicio = CrearCuentaServicio(fabrica);
        var activas = await servicio.GetFromJsonAsync<List<BecaActivaDto>>(
            $"/internal/v1/becas/activas?postulanteId={escenario.EstudianteId}"
            + $"&periodoId={escenario.PeriodoId}",
            _opcionesJson);

        activas.Should().NotBeNull();
        activas!.Should().ContainSingle(beca =>
            beca.BecaId == oferta.BecaId
            && beca.Estado == "ACTIVA"
            && beca.ProgramaBecaId == escenario.ProgramaId);
    }

    [Fact]
    public async Task Debe_RechazarLaOferta_Y_CerrarLaBecaConElEventoDeLiberacion()
    {
        var escenario = NuevoEscenario();
        await SembrarProyeccionesAsync(escenario);
        ConfigurarStubs(escenario);

        using var fabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var coordinador = CrearCoordinador(fabrica, escenario.ProgramaId);
        var oferta = await EmitirAsync(coordinador, escenario.PostulacionId);

        using var postulante = CrearPostulante(fabrica, escenario.EstudianteId);
        using var solicitud = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/becas/ofertas/{oferta.OfertaId}/rechazar")
        {
            Content = JsonContent.Create(new
            {
                motivo = "El postulante decidió no continuar con el programa."
            })
        };
        solicitud.Headers.Add(EncabezadoIdempotencia, $"rechazar-{Guid.NewGuid():N}");

        using var respuesta = await postulante.SendAsync(solicitud);
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await VerificarCierreAsync(
            oferta.BecaId,
            EstadoOferta.RechazadaPorPostulante,
            TipoCierreBeca.OfertaRechazada);

        fabrica.Outbox.Eventos.OfType<OfertaBecaRechazadaEvento>()
            .Should().ContainSingle(evento =>
                evento.BecaId == oferta.BecaId
                && evento.OfertaId == oferta.OfertaId);
    }

    [Fact]
    public async Task Debe_AnularLaOferta_Y_CerrarLaBecaConElEventoDeLiberacion()
    {
        var escenario = NuevoEscenario();
        await SembrarProyeccionesAsync(escenario);
        ConfigurarStubs(escenario);

        using var fabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var coordinador = CrearCoordinador(fabrica, escenario.ProgramaId);
        var oferta = await EmitirAsync(coordinador, escenario.PostulacionId);

        using var solicitud = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/becas/ofertas/{oferta.OfertaId}/anular")
        {
            Content = JsonContent.Create(new
            {
                justificacion = "Incumplimiento de los requisitos del programa."
            })
        };
        solicitud.Headers.Add(EncabezadoIdempotencia, $"anular-{Guid.NewGuid():N}");

        using var respuesta = await coordinador.SendAsync(solicitud);
        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await VerificarCierreAsync(
            oferta.BecaId,
            EstadoOferta.AnuladaPorCoordinacion,
            TipoCierreBeca.OfertaAnulada);

        fabrica.Outbox.Eventos.OfType<OfertaBecaAnuladaEvento>()
            .Should().ContainSingle(evento =>
                evento.BecaId == oferta.BecaId
                && evento.OfertaId == oferta.OfertaId);
    }

    [Fact]
    public async Task Debe_ExpirarLaOfertaConHangfire_Y_CerrarLaBecaConElEventoDeLiberacion()
    {
        var escenario = NuevoEscenario();
        await SembrarProyeccionesAsync(escenario);
        ConfigurarStubs(escenario);

        using var fabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var coordinador = CrearCoordinador(fabrica, escenario.ProgramaId);
        var oferta = await EmitirAsync(coordinador, escenario.PostulacionId);
        var fechaEmisionSimulada = DateTimeOffset.UtcNow.AddDays(-16);
        var fechaVencimientoSimulada = DateTimeOffset.UtcNow.AddDays(-1);

        await using (var contexto = _fixture.CrearContexto())
        {
            await contexto.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE becas.oferta_beca
                SET fecha_emision = {fechaEmisionSimulada},
                    fecha_vencimiento_aceptacion = {fechaVencimientoSimulada}
                WHERE id = {oferta.OfertaId}
                """);
        }

        var clienteHangfire = fabrica.Services.GetRequiredService<IBackgroundJobClient>();
        var trabajoId = clienteHangfire.Enqueue<ExpirarOfertaBecaJob>(
            trabajo => trabajo.Ejecutar(oferta.OfertaId, CancellationToken.None));

        trabajoId.Should().NotBeNullOrWhiteSpace();

        var cerrada = await EsperarAsync(async () =>
        {
            await using var contexto = _fixture.CrearContexto();
            var beca = await contexto.Becas
                .AsNoTracking()
                .Include(registro => registro.Oferta)
                .SingleAsync(registro => registro.Id == oferta.BecaId);

            return beca.Estado == EstadoBeca.Revocada
                && beca.TipoCierre == TipoCierreBeca.OfertaExpirada
                && beca.Oferta!.Estado == EstadoOferta.Expirada;
        });

        cerrada.Should().BeTrue(
            "Hangfire debe ejecutar el job de expiración y cerrar la oferta");

        var eventoPublicado = await EsperarAsync(() => Task.FromResult(
            fabrica.Outbox.Eventos.OfType<OfertaBecaExpiradaEvento>()
                .Any(evento =>
                    evento.BecaId == oferta.BecaId
                    && evento.OfertaId == oferta.OfertaId)));

        eventoPublicado.Should().BeTrue(
            "la expiración debe publicar el evento de liberación correspondiente");
    }

    [Fact]
    public async Task Debe_PermitirSoloUnaAceptacion_Cuando_DosOfertasDelMismoEstudianteSeAceptanEnParalelo()
    {
        var primero = NuevoEscenario();
        var segundo = primero with
        {
            PostulacionId = Guid.CreateVersion7(),
            ConvocatoriaId = Guid.CreateVersion7()
        };

        await SembrarProyeccionesAsync(primero);
        ConfigurarStubs(primero);
        ConfigurarStubs(segundo);

        using var fabrica = new FabricaApiExtremoAExtremo(_fixture);
        using var coordinadorPrimero = CrearCoordinador(fabrica, primero.ProgramaId);
        using var coordinadorSegundo = CrearCoordinador(fabrica, segundo.ProgramaId);

        var ofertaPrimera = await EmitirAsync(
            coordinadorPrimero,
            primero.PostulacionId);
        var ofertaSegunda = await EmitirAsync(
            coordinadorSegundo,
            segundo.PostulacionId);

        using var postulantePrimero = CrearPostulante(fabrica, primero.EstudianteId);
        using var postulanteSegundo = CrearPostulante(fabrica, segundo.EstudianteId);

        var aceptaciones = await Task.WhenAll(
            AceptarAsync(
                postulantePrimero,
                ofertaPrimera.OfertaId,
                $"aceptar-{Guid.NewGuid():N}"),
            AceptarAsync(
                postulanteSegundo,
                ofertaSegunda.OfertaId,
                $"aceptar-{Guid.NewGuid():N}"));

        var exitosas = aceptaciones
            .Where(respuesta => respuesta.StatusCode == HttpStatusCode.OK)
            .ToArray();
        var conflictivas = aceptaciones
            .Where(respuesta => respuesta.StatusCode == HttpStatusCode.Conflict)
            .ToArray();

        exitosas.Should().ContainSingle();
        conflictivas.Should().ContainSingle();

        var cuerpoConflicto = await conflictivas[0].Content
            .ReadFromJsonAsync<JsonElement>(_opcionesJson);
        cuerpoConflicto.GetProperty("code").GetString()
            .Should().Be("DOBLE_BENEFICIO");

        await using var contexto = _fixture.CrearContexto();
        var activas = await contexto.Becas
            .AsNoTracking()
            .CountAsync(beca =>
                beca.PostulanteEstudianteId == primero.EstudianteId
                && beca.Estado == EstadoBeca.Activa);

        activas.Should().Be(1);

        foreach (var respuesta in aceptaciones)
        {
            respuesta.Dispose();
        }
    }

    private async Task VerificarCierreAsync(
        Guid becaId,
        EstadoOferta estadoOferta,
        TipoCierreBeca tipoCierre)
    {
        await using var contexto = _fixture.CrearContexto();
        var beca = await contexto.Becas
            .AsNoTracking()
            .Include(registro => registro.Oferta)
            .SingleAsync(registro => registro.Id == becaId);

        beca.Estado.Should().Be(EstadoBeca.Revocada);
        beca.TipoCierre.Should().Be(tipoCierre);
        beca.Oferta!.Estado.Should().Be(estadoOferta);
    }

    private async Task SembrarProyeccionesAsync(EscenarioE2E escenario)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var ahora = DateTimeOffset.UtcNow;

        await using var contexto = _fixture.CrearContexto();

        contexto.ProgramasBecaRef.Add(new ProgramaBecaRef
        {
            ProgramaBecaId = escenario.ProgramaId,
            Codigo = $"PROG-{Guid.NewGuid():N}"[..20],
            Nombre = "Programa de becas E2E",
            PoliticaCobertura = new PoliticaCobertura(
                TipoCobertura.Total,
                100m,
                0m,
                "USD"),
            CondicionesPermanencia = new CondicionesPermanencia(
                13m,
                4,
                1,
                40,
                ConsecuenciaPermanencia.LlamadoAtencion),
            PermiteAcumulacion = true,
            RequiereCompromisoFirmado = false,
            DuracionPeriodos = 2,
            Estado = "ACTIVO",
            UltimoEventoId = Guid.CreateVersion7(),
            OccurredAt = ahora,
            FechaUltimaActualizacion = ahora
        });

        contexto.CostosMatriculaRef.Add(new CostoMatriculaRef
        {
            CostoMatriculaReferenciaId = Guid.CreateVersion7(),
            CarreraId = escenario.CarreraId,
            PeriodoAcademicoId = escenario.PeriodoId,
            MontoReferenciaMatricula = 1500m,
            Moneda = "USD",
            NumeroVersion = 1,
            FechaVigenciaDesde = hoy.AddMonths(-1),
            FechaVigenciaHasta = null,
            Estado = "VIGENTE",
            UltimoEventoId = Guid.CreateVersion7(),
            OccurredAt = ahora,
            FechaUltimaActualizacion = ahora
        });

        contexto.ConveniosRef.Add(new ConvenioRef
        {
            UniversidadAliadaId = escenario.UniversidadId,
            ConvenioId = Guid.CreateVersion7(),
            Estado = "VIGENTE",
            FechaFin = hoy.AddYears(1),
            UltimoEventoId = Guid.CreateVersion7(),
            OccurredAt = ahora,
            FechaUltimaActualizacion = ahora
        });

        await contexto.SaveChangesAsync();
    }

    private void ConfigurarStubs(EscenarioE2E escenario)
    {
        _fixture.WireMock
            .Given(Request.Create()
                .WithPath(
                    $"/internal/v1/postulaciones/{escenario.PostulacionId}/adjudicable")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    postulacionId = escenario.PostulacionId,
                    adjudicable = true,
                    programaBecaId = escenario.ProgramaId,
                    periodoAcademicoId = escenario.PeriodoId,
                    periodoCodigo = "2026-1",
                    universidadAliadaId = escenario.UniversidadId,
                    carreraId = escenario.CarreraId,
                    postulanteEstudianteId = escenario.EstudianteId,
                    nombreEstudiante = "Ana Pérez",
                    numeroDocumentoEstudiante = "12345678",
                    convocatoriaOrigenId = escenario.ConvocatoriaId
                })));

        _fixture.WireMock
            .Given(Request.Create()
                .WithPath(
                    $"/internal/v1/postulaciones/{escenario.PostulacionId}/otorgar")
                .UsingPost())
            .RespondWith(Response.Create().WithStatusCode(204));

        _fixture.WireMock
            .Given(Request.Create()
                .WithPath("/internal/v1/periodos-academicos/actual")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    periodoAcademicoId = escenario.PeriodoId,
                    codigo = "2026-1",
                    nombre = "Periodo académico E2E",
                    ordenSecuencia = 1,
                    fechaInicio = new DateOnly(2026, 1, 1),
                    fechaFin = new DateOnly(2026, 6, 30),
                    estado = "ACTIVO",
                    esActual = true
                })));

        _fixture.WireMock
            .Given(Request.Create()
                .WithPath(
                    $"/internal/v1/programas/{escenario.ProgramaId}/condiciones")
                .WithParam("carreraId", escenario.CarreraId.ToString())
                .WithParam("periodoId", escenario.PeriodoId.ToString())
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    programaBecaId = escenario.ProgramaId,
                    carreraId = escenario.CarreraId,
                    periodoAcademicoId = escenario.PeriodoId,
                    universidadAliadaId = escenario.UniversidadId,
                    politicaCobertura = new
                    {
                        modalidad = "TOTAL",
                        porcentajeCobertura = 100m,
                        montoFijoPeriodo = 0m,
                        moneda = "USD"
                    },
                    condicionesPermanencia = new
                    {
                        promedioMinimoRenovacion = 13m,
                        materiasMinimasAprobadas = 4,
                        maximoMateriasReprobadas = 1,
                        horasServicioPorPeriodo = 40,
                        consecuenciaIncumplimiento = "LLAMADO_ATENCION"
                    },
                    costoMatricula = new
                    {
                        costoMatriculaReferenciaId = Guid.CreateVersion7(),
                        carreraId = escenario.CarreraId,
                        periodoAcademicoId = escenario.PeriodoId,
                        montoReferenciaMatricula = 1500m,
                        moneda = "USD",
                        numeroVersion = 1,
                        fechaVigenciaDesde = DateOnly.FromDateTime(DateTime.UtcNow)
                            .AddMonths(-1),
                        fechaVigenciaHasta = (DateOnly?)null,
                        estado = "VIGENTE"
                    },
                    convenio = new
                    {
                        universidadAliadaId = escenario.UniversidadId,
                        convenioId = Guid.CreateVersion7(),
                        estado = "VIGENTE",
                        fechaFin = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1)
                    }
                })));

        _fixture.WireMock
            .Given(Request.Create()
                .WithPath(
                    $"/internal/v1/programas/{escenario.ProgramaId}/reservas")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    reservaId = Guid.CreateVersion7(),
                    estado = "PENDIENTE",
                    monto = 3000m,
                    cupos = 1,
                    expiraEn = DateTimeOffset.UtcNow.AddMinutes(30)
                })));
    }

    private static HttpClient CrearCoordinador(
        FabricaApiExtremoAExtremo fabrica,
        Guid programaId)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(
            EncabezadoRoles,
            "COORDINADOR_PROGRAMA");
        cliente.DefaultRequestHeaders.Add(
            EncabezadoProgramas,
            programaId.ToString());
        cliente.DefaultRequestHeaders.Add(
            EncabezadoAcr,
            "urn:ascenso:loa:2fa");
        return cliente;
    }

    private static HttpClient CrearPostulante(
        FabricaApiExtremoAExtremo fabrica,
        Guid estudianteId)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(EncabezadoRoles, "POSTULANTE");
        cliente.DefaultRequestHeaders.Add(
            EncabezadoPostulante,
            estudianteId.ToString());
        return cliente;
    }

    private static HttpClient CrearCuentaServicio(
        FabricaApiExtremoAExtremo fabrica)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(
            EncabezadoCliente,
            "ms-bec-internal");
        cliente.DefaultRequestHeaders.Add(
            EncabezadoScopes,
            "becas.internal.read");
        return cliente;
    }

    private static async Task<OfertaEmitidaDto> EmitirAsync(
        HttpClient cliente,
        Guid postulacionId)
    {
        cliente.DefaultRequestHeaders.Remove(EncabezadoIdempotencia);
        cliente.DefaultRequestHeaders.Add(
            EncabezadoIdempotencia,
            $"emitir-{Guid.NewGuid():N}");

        using var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/becas/ofertas",
            new { postulacionId });

        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        respuesta.StatusCode.Should().Be(
            HttpStatusCode.Created,
            "la emisión debe completarse: {0}",
            cuerpo);

        var resultado = JsonSerializer.Deserialize<OfertaEmitidaDto>(
            cuerpo,
            _opcionesJson);

        resultado.Should().NotBeNull();
        return resultado!;
    }

    private static async Task<HttpResponseMessage> AceptarAsync(
        HttpClient cliente,
        Guid ofertaId,
        string clave)
    {
        using var solicitud = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/becas/ofertas/{ofertaId}/aceptar");
        solicitud.Headers.Add(EncabezadoIdempotencia, clave);

        return await cliente.SendAsync(solicitud);
    }

    private static async Task<bool> EsperarAsync(
        Func<Task<bool>> condicion,
        TimeSpan? limite = null)
    {
        var hasta = DateTimeOffset.UtcNow + (limite ?? TimeSpan.FromSeconds(20));

        while (DateTimeOffset.UtcNow < hasta)
        {
            if (await condicion())
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        return false;
    }

    private static EscenarioE2E NuevoEscenario()
        => new(
            ProgramaId: Guid.CreateVersion7(),
            PeriodoId: Guid.CreateVersion7(),
            UniversidadId: Guid.CreateVersion7(),
            CarreraId: Guid.CreateVersion7(),
            EstudianteId: Guid.CreateVersion7(),
            PostulacionId: Guid.CreateVersion7(),
            ConvocatoriaId: Guid.CreateVersion7());

    private sealed record EscenarioE2E(
        Guid ProgramaId,
        Guid PeriodoId,
        Guid UniversidadId,
        Guid CarreraId,
        Guid EstudianteId,
        Guid PostulacionId,
        Guid ConvocatoriaId);
}
