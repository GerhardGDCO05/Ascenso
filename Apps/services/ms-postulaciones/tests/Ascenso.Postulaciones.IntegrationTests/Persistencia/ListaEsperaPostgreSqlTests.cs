using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConfirmarRankingConvocatoria;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ResolverCasoLimiteRanking;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.ListaEspera.Comandos.PromoverCandidatoListaEspera;
using Ascenso.Postulaciones.Application.ListaEspera.Comandos.RegistrarVacanteLiberada;
using Ascenso.Postulaciones.Application.ListaEspera.Consultas.ObtenerListaEsperaConvocatoria;
using Ascenso.Postulaciones.Application.ListaEspera.Consultas.ObtenerSiguienteCandidatoListaEspera;
using Ascenso.Postulaciones.Application.ListaEspera.DTOs;
using Ascenso.Postulaciones.Application.ListaEspera.Servicios;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using Ascenso.Postulaciones.Infrastructure.Adapters;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NSubstitute;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class ListaEsperaPostgreSqlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string EsquemaPrueba = "TestJwtPg";
    private const string HeaderScope = "X-Test-Scope";

    private readonly PostgreSqlFixture _fixture;
    private readonly WebApplicationFactory<Program> _fabricaBase;

    public ListaEsperaPostgreSqlTests(
        PostgreSqlFixture fixture,
        WebApplicationFactory<Program> fabricaBase)
    {
        _fixture = fixture;
        _fabricaBase = fabricaBase;
    }

    private static ResultadoConsolidadoRanking CrearResultadoProvisional(
        Guid convocatoriaId,
        Guid postulacionId,
        decimal puntajeConsolidado,
        DateTimeOffset fechaConsolidacion,
        Guid actorId)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            convocatoriaId: convocatoriaId,
            postulacionId: postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntajeConsolidado,
            diferenciaEntreEvaluadores: 4.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: false);

        return ResultadoConsolidadoRanking.Crear(calculo, fechaConsolidacion, actorId);
    }

    [Fact]
    public async Task Debe_GenerarAutomaticamenteYAtomicamenteEntradasListaEsperaPreservandoOrdenDeHU10_1YCeroFilasSinFueraDeCupo_Cuando_SeConfirmaRankingEnPostgreSqlReal()
    {
        var fechaBase = new DateTimeOffset(2026, 11, 27, 9, 0, 0, TimeSpan.Zero);
        var coordinadorId = Guid.CreateVersion7();

        // Escenario (a.1): Caso límite presupuestario de HU 10.1 -> p1 (pos 1) y p3 (pos 3) dentro de cupo;
        // p2 (pos 2, monto 1800) y p4 (pos 4, monto 900) quedan en LISTA_DE_ESPERA -> posicionOrden 1 y 2.
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var p1 = Guid.CreateVersion7();
        var p2 = Guid.CreateVersion7();
        var p3 = Guid.CreateVersion7();
        var p4 = Guid.CreateVersion7();

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.ResultadosConsolidadosRanking.AddRange(
                CrearResultadoProvisional(convocatoriaId, p1, 95.00m, fechaBase.AddMinutes(1), coordinadorId),
                CrearResultadoProvisional(convocatoriaId, p2, 88.00m, fechaBase.AddMinutes(2), coordinadorId),
                CrearResultadoProvisional(convocatoriaId, p3, 82.00m, fechaBase.AddMinutes(3), coordinadorId),
                CrearResultadoProvisional(convocatoriaId, p4, 75.00m, fechaBase.AddMinutes(4), coordinadorId));
            await contextoSemilla.GuardarCambiosAsync();
        }

        var disponibilidad = CrearDisponibilidadClient(
            convocatoriaId,
            programaId,
            cupos: 3,
            presupuestoEfectivo: 2600.00m);
        var montos = CrearProveedorMonto(
            convocatoriaId,
            programaId,
            new Dictionary<Guid, decimal>
            {
                [p1] = 1500.00m,
                [p2] = 1800.00m,
                [p3] = 1000.00m,
                [p4] = 900.00m
            });

        await using (var contextoResolver = _fixture.CrearContexto())
        {
            var timeResolver = new ProveedorTiempoFijo(fechaBase.AddMinutes(20));
            var resolverHandler = new ResolverCasoLimiteRankingCommandHandler(
                new UsuarioActualFijo(coordinadorId),
                new RolesUsuarioFijo("COORDINADOR_PROGRAMA"),
                new ConsultaJefaturaComiteRepository(contextoResolver),
                new VerificadorConflictoInteresDecisorSinOperacion(),
                new RankingConvocatoriaRepository(contextoResolver),
                new ConsultaEstadoEvaluacionesConvocatoriaRepository(contextoResolver),
                new ConvocatoriaEvaluacionCerradaFija(timeResolver),
                disponibilidad,
                montos,
                contextoResolver,
                new ResolverCasoLimiteRankingCommandValidator(),
                timeResolver);

            await resolverHandler.HandleAsync(new ResolverCasoLimiteRankingCommand(
                convocatoriaId,
                [p3],
                "Se selecciona p3 por ajustarse al presupuesto remanente de 1100.00 USD."));
        }

        var fechaConfirmacion = fechaBase.AddHours(1);
        await using (var contextoConfirmar = _fixture.CrearContexto())
        {
            var confirmarHandler = CrearConfirmarHandlerReal(
                contextoConfirmar,
                coordinadorId,
                fechaConfirmacion,
                disponibilidad,
                montos);

            var rankingConfirmado = await confirmarHandler.HandleAsync(
                new ConfirmarRankingConvocatoriaCommand(convocatoriaId));
            rankingConfirmado.EstadoRanking.Should().Be("CONFIRMADO");
        }

        await using (var contextoVerificar = _fixture.CrearContexto())
        {
            var entradas = await new EntradaListaEsperaRepository(contextoVerificar)
                .ListarPorConvocatoriaAsync(convocatoriaId);

            entradas.Should().HaveCount(2);
            entradas[0].PostulacionId.Should().Be(p2);
            entradas[0].ProgramaBecaId.Should().Be(programaId);
            entradas[0].PosicionOrden.Should().Be(1);
            entradas[0].MontoEstimadoBecaSnapshot.Should().Be(1800.00m);
            entradas[0].Estado.Should().Be(EstadoListaEspera.EnEspera);
            entradas[0].FechaIngreso.Should().Be(fechaConfirmacion);
            entradas[0].FechaElegibilidad.Should().BeNull();
            entradas[0].FechaPromocion.Should().BeNull();
            entradas[0].PromovidoPorUsuarioId.Should().BeNull();
            entradas[0].MotivoPromocion.Should().BeNull();
            entradas[0].Version.Should().Be(1);

            entradas[1].PostulacionId.Should().Be(p4);
            entradas[1].ProgramaBecaId.Should().Be(programaId);
            entradas[1].PosicionOrden.Should().Be(2);
            entradas[1].MontoEstimadoBecaSnapshot.Should().Be(900.00m);
            entradas[1].Estado.Should().Be(EstadoListaEspera.EnEspera);
            entradas[1].FechaIngreso.Should().Be(fechaConfirmacion);
        }

        // Escenario (a.2): Convocatoria donde el 100% queda DENTRO_DE_CUPO -> cero filas en entradas_lista_espera.
        var convocatoriaSinEsperaId = Guid.CreateVersion7();
        var q1 = Guid.CreateVersion7();
        var q2 = Guid.CreateVersion7();

        await using (var contextoSemilla2 = _fixture.CrearContexto())
        {
            contextoSemilla2.ResultadosConsolidadosRanking.AddRange(
                CrearResultadoProvisional(convocatoriaSinEsperaId, q1, 94.00m, fechaBase.AddMinutes(1), coordinadorId),
                CrearResultadoProvisional(convocatoriaSinEsperaId, q2, 91.00m, fechaBase.AddMinutes(2), coordinadorId));
            await contextoSemilla2.GuardarCambiosAsync();
        }

        var dispSinEspera = CrearDisponibilidadClient(convocatoriaSinEsperaId, programaId, cupos: 5, presupuestoEfectivo: 10000.00m);
        var montosSinEspera = CrearProveedorMonto(
            convocatoriaSinEsperaId,
            programaId,
            new Dictionary<Guid, decimal>
            {
                [q1] = 1500.00m,
                [q2] = 1500.00m
            });

        await using (var contextoConfirmar2 = _fixture.CrearContexto())
        {
            var confirmarHandler2 = CrearConfirmarHandlerReal(
                contextoConfirmar2,
                coordinadorId,
                fechaConfirmacion.AddMinutes(5),
                dispSinEspera,
                montosSinEspera);

            var ranking2 = await confirmarHandler2.HandleAsync(
                new ConfirmarRankingConvocatoriaCommand(convocatoriaSinEsperaId));
            ranking2.EstadoRanking.Should().Be("CONFIRMADO");
        }

        await using (var contextoVerificar2 = _fixture.CrearContexto())
        {
            var entradasVacias = await new EntradaListaEsperaRepository(contextoVerificar2)
                .ListarPorConvocatoriaAsync(convocatoriaSinEsperaId);
            entradasVacias.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Debe_RegistrarVacantePromoverManualmenteValidandoMsPrgYActualizarAdjudicableReal_Cuando_SeOperaFlujoCompletoEnPostgreSqlReal()
    {
        var fechaBase = new DateTimeOffset(2026, 11, 27, 10, 0, 0, TimeSpan.Zero);
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var p1 = Guid.CreateVersion7();
        var p2 = Guid.CreateVersion7();
        var p3 = Guid.CreateVersion7();

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.ResultadosConsolidadosRanking.AddRange(
                CrearResultadoProvisional(convocatoriaId, p1, 95.00m, fechaBase.AddMinutes(1), coordinadorId),
                CrearResultadoProvisional(convocatoriaId, p2, 86.00m, fechaBase.AddMinutes(2), coordinadorId),
                CrearResultadoProvisional(convocatoriaId, p3, 80.00m, fechaBase.AddMinutes(3), coordinadorId));
            await contextoSemilla.GuardarCambiosAsync();
        }

        var dispConfirmar = CrearDisponibilidadClient(convocatoriaId, programaId, cupos: 1, presupuestoEfectivo: 5000.00m);
        var montos = CrearProveedorMonto(
            convocatoriaId,
            programaId,
            new Dictionary<Guid, decimal>
            {
                [p1] = 1500.00m,
                [p2] = 1500.00m,
                [p3] = 1200.00m
            });

        await using (var contextoConfirmar = _fixture.CrearContexto())
        {
            var confirmar = CrearConfirmarHandlerReal(contextoConfirmar, coordinadorId, fechaBase.AddMinutes(10), dispConfirmar, montos);
            await confirmar.HandleAsync(new ConfirmarRankingConvocatoriaCommand(convocatoriaId));
        }

        // (b) Registrar vacante liberada -> marca p2 (posicionOrden = 1) como ELEGIBLE_POR_VACANTE sin promover.
        var fechaVacante = fechaBase.AddMinutes(20);
        EntradaListaEsperaDto entradaElegibleDto;
        await using (var contextoVacante = _fixture.CrearContexto())
        {
            var vacanteHandler = new RegistrarVacanteLiberadaCommandHandler(
                new UsuarioActualFijo(coordinadorId),
                new GestorConcurrenciaListaEsperaPostgreSql(contextoVacante),
                contextoVacante,
                new RegistrarVacanteLiberadaCommandValidator(),
                new ProveedorTiempoFijo(fechaVacante));

            entradaElegibleDto = await vacanteHandler.HandleAsync(
                new RegistrarVacanteLiberadaCommand(convocatoriaId, "Rechazo de oferta inicial."));
        }

        entradaElegibleDto.PostulacionId.Should().Be(p2);
        entradaElegibleDto.PosicionOrden.Should().Be(1);
        entradaElegibleDto.Estado.Should().Be("ELEGIBLE_POR_VACANTE");
        entradaElegibleDto.FechaElegibilidad.Should().Be(fechaVacante);
        entradaElegibleDto.FechaPromocion.Should().BeNull();
        entradaElegibleDto.Version.Should().Be(2);

        // Verificar que /adjudicable sigue respondiendo adjudicable = false mientras es ELEGIBLE_POR_VACANTE
        using var fabricaApi = CrearFabricaConPostgreSqlReal();
        using var clienteHttp = fabricaApi.CreateClient();
        clienteHttp.DefaultRequestHeaders.Add(HeaderScope, "postulaciones.internal.read");

        var respAntes = await clienteHttp.GetAsync($"/internal/v1/postulaciones/{p2}/adjudicable");
        respAntes.StatusCode.Should().Be(HttpStatusCode.OK);
        var adjAntes = await respAntes.Content.ReadFromJsonAsync<AdjudicabilidadPostulacionDto>();
        adjAntes!.Adjudicable.Should().BeFalse();
        adjAntes.PromovidaDeListaEspera.Should().BeFalse();
        adjAntes.EstadoListaEspera.Should().Be("ELEGIBLE_POR_VACANTE");
        adjAntes.MotivoNoAdjudicable.Should().Be("FUERA_DE_CUPO_EN_LISTA_DE_ESPERA");

        // Obtener ID de entrada 2 (p3) para probar rechazo FIFO
        Guid entrada2Id;
        await using (var contextoConsulta = _fixture.CrearContexto())
        {
            var lista = await new EntradaListaEsperaRepository(contextoConsulta).ListarPorConvocatoriaAsync(convocatoriaId);
            entrada2Id = lista.Single(e => e.PostulacionId == p3).Id;
        }

        // (c.1) Rechazo 409 ORDEN_LISTA_ESPERA_VIOLADO al intentar saltar a posicionOrden = 2
        var dispValida = CrearDisponibilidadClient(convocatoriaId, programaId, cupos: 1, presupuestoEfectivo: 2000.00m);
        await using (var contextoFifo = _fixture.CrearContexto())
        {
            var promoverFifo = CrearPromoverHandlerReal(contextoFifo, coordinadorId, fechaBase.AddMinutes(30), dispValida);
            var accionFifo = () => promoverFifo.HandleAsync(new PromoverCandidatoListaEsperaCommand(convocatoriaId, entrada2Id));
            var exFifo = (await accionFifo.Should().ThrowAsync<OrdenListaEsperaVioladoException>()).Which;
            exFifo.Codigo.Should().Be(OrdenListaEsperaVioladoException.CodigoError);
            exFifo.EstadoHttp.Should().Be(409);
        }

        // (c.2) Rechazo 409 SIN_CUPO_DISPONIBLE_PROMOCION
        var dispSinCupo = CrearDisponibilidadClient(convocatoriaId, programaId, cupos: 0, presupuestoEfectivo: 2000.00m);
        await using (var contextoSinCupo = _fixture.CrearContexto())
        {
            var promoverSinCupo = CrearPromoverHandlerReal(contextoSinCupo, coordinadorId, fechaBase.AddMinutes(31), dispSinCupo);
            var accionSinCupo = () => promoverSinCupo.HandleAsync(new PromoverCandidatoListaEsperaCommand(convocatoriaId, entradaElegibleDto.Id));
            var exSinCupo = (await accionSinCupo.Should().ThrowAsync<SinCupoDisponiblePromocionException>()).Which;
            exSinCupo.Codigo.Should().Be(SinCupoDisponiblePromocionException.CodigoError);
            exSinCupo.EstadoHttp.Should().Be(409);
        }

        // (c.3) Rechazo 422 PRESUPUESTO_INSUFICIENTE_PROMOCION
        var dispSinPresupuesto = CrearDisponibilidadClient(convocatoriaId, programaId, cupos: 1, presupuestoEfectivo: 1000.00m);
        await using (var contextoSinPresupuesto = _fixture.CrearContexto())
        {
            var promoverSinPresupuesto = CrearPromoverHandlerReal(contextoSinPresupuesto, coordinadorId, fechaBase.AddMinutes(32), dispSinPresupuesto);
            var accionSinPresupuesto = () => promoverSinPresupuesto.HandleAsync(new PromoverCandidatoListaEsperaCommand(convocatoriaId, entradaElegibleDto.Id));
            var exSinPresupuesto = (await accionSinPresupuesto.Should().ThrowAsync<PresupuestoInsuficientePromocionException>()).Which;
            exSinPresupuesto.Codigo.Should().Be(PresupuestoInsuficientePromocionException.CodigoError);
            exSinPresupuesto.EstadoHttp.Should().Be(422);
        }

        // (c.4) Promoción manual asistida exitosa -> PROMOVIDA
        var fechaPromocion = fechaBase.AddMinutes(40);
        PromocionListaEsperaDto promocionDto;
        await using (var contextoPromover = _fixture.CrearContexto())
        {
            var promoverHandler = CrearPromoverHandlerReal(contextoPromover, coordinadorId, fechaPromocion, dispValida);
            promocionDto = await promoverHandler.HandleAsync(new PromoverCandidatoListaEsperaCommand(
                convocatoriaId,
                entradaElegibleDto.Id,
                "Promoción confirmada tras validar disponibilidad real en MS-PRG."));
        }

        promocionDto.HabilitadaParaAdjudicacion.Should().BeTrue();
        promocionDto.Entrada.Estado.Should().Be("PROMOVIDA");
        promocionDto.Entrada.PromovidoPorUsuarioId.Should().Be(coordinadorId);
        promocionDto.Entrada.FechaPromocion.Should().Be(fechaPromocion);
        promocionDto.Entrada.FechaElegibilidad.Should().Be(fechaVacante);
        promocionDto.Entrada.Version.Should().Be(3);

        // (d) Verificar GET /internal/v1/postulaciones/{postulacionId}/adjudicable real contra PostgreSQL -> 200 OK adjudicable = true
        var respDespues = await clienteHttp.GetAsync($"/internal/v1/postulaciones/{p2}/adjudicable");
        respDespues.StatusCode.Should().Be(HttpStatusCode.OK);
        var adjDespues = await respDespues.Content.ReadFromJsonAsync<AdjudicabilidadPostulacionDto>();
        adjDespues.Should().NotBeNull();
        adjDespues!.PostulacionId.Should().Be(p2);
        adjDespues.Adjudicable.Should().BeTrue();
        adjDespues.MotivoNoAdjudicable.Should().BeNull();
        adjDespues.PromovidaDeListaEspera.Should().BeTrue();
        adjDespues.EstadoListaEspera.Should().Be("PROMOVIDA");
        adjDespues.PosicionListaEspera.Should().Be(1);
        adjDespues.FechaPromocionListaEspera.Should().Be(fechaPromocion);
        adjDespues.ProgramaBecaId.Should().Be(programaId);

        // Verificar además consultas CQRS reales (listado paginado y siguiente candidato = p3)
        await using (var contextoLectura = _fixture.CrearContexto())
        {
            var repoLista = new EntradaListaEsperaRepository(contextoLectura);
            var listaDto = await new ObtenerListaEsperaConvocatoriaQueryHandler(
                repoLista,
                new ObtenerListaEsperaConvocatoriaQueryValidator())
                .HandleAsync(new ObtenerListaEsperaConvocatoriaQuery(convocatoriaId));

            listaDto.TotalEnListaEspera.Should().Be(2);
            listaDto.TotalPromovidas.Should().Be(1);
            listaDto.TotalEnEspera.Should().Be(1);
            listaDto.TotalElegiblesPorVacante.Should().Be(0);

            var siguienteDto = await new ObtenerSiguienteCandidatoListaEsperaQueryHandler(
                repoLista,
                new ObtenerSiguienteCandidatoListaEsperaQueryValidator())
                .HandleAsync(new ObtenerSiguienteCandidatoListaEsperaQuery(convocatoriaId));

            siguienteDto.PostulacionId.Should().Be(p3);
            siguienteDto.PosicionOrden.Should().Be(2);
        }
    }

    [Fact]
    public async Task Debe_RechazarViolacionesDeIndicesUnicosYCuatroRestriccionesCheck_Cuando_SeOperaEnPostgreSqlReal()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var fecha = new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

        // Semilla válida
        await using (var ctxSemilla = _fixture.CrearContexto())
        {
            var e1 = EntradaListaEspera.Crear(convocatoriaId, programaId, postulacionId, 1, 1500.00m, fecha, actorId);
            ctxSemilla.EntradasListaEspera.Add(e1);
            await ctxSemilla.GuardarCambiosAsync();
        }

        // 1. Índice único ux_entradas_lista_espera_postulacion -> EntradaListaEsperaDuplicadaException (409)
        await using (var ctxDupPost = _fixture.CrearContexto())
        {
            var dupPost = EntradaListaEspera.Crear(convocatoriaId, programaId, postulacionId, 2, 1500.00m, fecha, actorId);
            ctxDupPost.EntradasListaEspera.Add(dupPost);

            var accion = () => ctxDupPost.GuardarCambiosAsync();
            var ex = (await accion.Should().ThrowAsync<EntradaListaEsperaDuplicadaException>()).Which;
            ex.Codigo.Should().Be(EntradaListaEsperaDuplicadaException.CodigoError);
            ex.EstadoHttp.Should().Be(409);
        }

        // 2. Índice único ux_entradas_lista_espera_convocatoria_posicion -> ExcepcionConflictoConcurrencia (409 RESTRICCION_UNICIDAD)
        await using (var ctxDupPos = _fixture.CrearContexto())
        {
            var dupPos = EntradaListaEspera.Crear(convocatoriaId, programaId, Guid.CreateVersion7(), 1, 1500.00m, fecha, actorId);
            ctxDupPos.EntradasListaEspera.Add(dupPos);

            var accion = () => ctxDupPos.GuardarCambiosAsync();
            var ex = (await accion.Should().ThrowAsync<ExcepcionConflictoConcurrencia>()).Which;
            ex.Codigo.Should().Be(ExcepcionConflictoConcurrencia.CodigoRestriccionUnicidad);
            ex.EstadoHttp.Should().Be(409);
        }

        // 3. CHECK ck_entradas_lista_espera_estado_valido (con guarda alfabética en ck_entradas_lista_espera_estado_coherente)
        await VerificarCheckViolationAsync(
            convocatoriaId,
            programaId,
            posicionOrden: 10,
            monto: 1500.00m,
            estado: "ESTADO_INVALIDO",
            fechaElegibilidad: null,
            fechaPromocion: null,
            promovidoPor: null,
            motivoPromocion: null,
            checkEsperada: "ck_entradas_lista_espera_estado_valido");

        // 4. CHECK ck_entradas_lista_espera_estado_coherente (EN_ESPERA con fecha_elegibilidad, ELEGIBLE sin fecha, PROMOVIDA sin actor)
        await VerificarCheckViolationAsync(
            convocatoriaId,
            programaId,
            posicionOrden: 11,
            monto: 1500.00m,
            estado: "EN_ESPERA",
            fechaElegibilidad: fecha,
            fechaPromocion: null,
            promovidoPor: null,
            motivoPromocion: null,
            checkEsperada: "ck_entradas_lista_espera_estado_coherente");

        await VerificarCheckViolationAsync(
            convocatoriaId,
            programaId,
            posicionOrden: 12,
            monto: 1500.00m,
            estado: "ELEGIBLE_POR_VACANTE",
            fechaElegibilidad: null,
            fechaPromocion: null,
            promovidoPor: null,
            motivoPromocion: null,
            checkEsperada: "ck_entradas_lista_espera_estado_coherente");

        await VerificarCheckViolationAsync(
            convocatoriaId,
            programaId,
            posicionOrden: 13,
            monto: 1500.00m,
            estado: "PROMOVIDA",
            fechaElegibilidad: null,
            fechaPromocion: fecha,
            promovidoPor: null,
            motivoPromocion: null,
            checkEsperada: "ck_entradas_lista_espera_estado_coherente");

        // 5. CHECK ck_entradas_lista_espera_posicion_positiva
        await VerificarCheckViolationAsync(
            convocatoriaId,
            programaId,
            posicionOrden: 0,
            monto: 1500.00m,
            estado: "EN_ESPERA",
            fechaElegibilidad: null,
            fechaPromocion: null,
            promovidoPor: null,
            motivoPromocion: null,
            checkEsperada: "ck_entradas_lista_espera_posicion_positiva");

        // 6. CHECK ck_entradas_lista_espera_monto_positivo
        await VerificarCheckViolationAsync(
            convocatoriaId,
            programaId,
            posicionOrden: 14,
            monto: 0.00m,
            estado: "EN_ESPERA",
            fechaElegibilidad: null,
            fechaPromocion: null,
            promovidoPor: null,
            motivoPromocion: null,
            checkEsperada: "ck_entradas_lista_espera_monto_positivo");
    }

    [Fact]
    public async Task Debe_SerializarPromocionSimultaneaSinDoblePromocion_Cuando_DosCoordinadoresPromuevenAlMismoCandidatoEnParaleloConDosDbContext()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();
        var coordinadorA = Guid.CreateVersion7();
        var coordinadorB = Guid.CreateVersion7();
        var fechaBase = new DateTimeOffset(2026, 11, 27, 14, 0, 0, TimeSpan.Zero);

        var resultado = CrearResultadoProvisional(convocatoriaId, postulacionId, 86.00m, fechaBase.AddMinutes(-10), coordinadorA);
        resultado.ConfirmarRanking(2, 1500.00m, ClasificacionRanking.ListaDeEspera, fechaBase.AddMinutes(-5), coordinadorA);

        var entrada = EntradaListaEspera.Crear(convocatoriaId, programaId, postulacionId, 1, 1500.00m, fechaBase.AddMinutes(-5), coordinadorA);
        entrada.MarcarElegiblePorVacante(fechaBase.AddMinutes(-1), coordinadorA);

        await using (var ctxSemilla = _fixture.CrearContexto())
        {
            ctxSemilla.ResultadosConsolidadosRanking.Add(resultado);
            ctxSemilla.EntradasListaEspera.Add(entrada);
            await ctxSemilla.GuardarCambiosAsync();
        }

        // El primer coordinador entra al bloqueo C6 y señala que ya retiene el lock; luego espera brevemente
        // mientras el segundo coordinador intenta adquirir el mismo bloqueo C6 en otro DbContext.
        var primeroDentroDelLock = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var liberarPrimero = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var disponibilidadA = Substitute.For<IDisponibilidadRankingClient>();
        disponibilidadA.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                primeroDentroDelLock.TrySetResult();
                await liberarPrimero.Task;
                return (DisponibilidadProgramaRanking?)new DisponibilidadProgramaRanking(
                    convocatoriaId,
                    programaId,
                    3,
                    1,
                    5000.00m,
                    0.00m,
                    "USD");
            });

        var disponibilidadB = CrearDisponibilidadClient(convocatoriaId, programaId, cupos: 1, presupuestoEfectivo: 5000.00m);

        var tareaA = Task.Run(async () =>
        {
            await using var contextoA = _fixture.CrearContexto();
            var handlerA = CrearPromoverHandlerReal(contextoA, coordinadorA, fechaBase.AddMinutes(1), disponibilidadA);
            return await handlerA.HandleAsync(new PromoverCandidatoListaEsperaCommand(convocatoriaId, entrada.Id, "Promovido por A"));
        });

        await primeroDentroDelLock.Task;

        var tareaB = Task.Run(async () =>
        {
            await using var contextoB = _fixture.CrearContexto();
            var handlerB = CrearPromoverHandlerReal(contextoB, coordinadorB, fechaBase.AddMinutes(2), disponibilidadB);
            return await handlerB.HandleAsync(new PromoverCandidatoListaEsperaCommand(convocatoriaId, entrada.Id, "Promovido por B"));
        });

        // Dar margen breve para que B quede bloqueado esperando pg_advisory_xact_lock en PostgreSQL
        await Task.Delay(150);
        tareaB.IsCompleted.Should().BeFalse("la segunda transacción debe esperar el bloqueo C6 de la convocatoria");

        liberarPrimero.TrySetResult();

        var dtoGanador = await tareaA;
        dtoGanador.Entrada.Estado.Should().Be("PROMOVIDA");
        dtoGanador.Entrada.PromovidoPorUsuarioId.Should().Be(coordinadorA);

        var esperarB = () => tareaB;
        var exPerdedor = (await esperarB.Should().ThrowAsync<EntradaListaEsperaYaPromovidaException>()).Which;
        exPerdedor.Codigo.Should().Be(EntradaListaEsperaYaPromovidaException.CodigoError);
        exPerdedor.EstadoHttp.Should().Be(409);

        await using var ctxVerificar = _fixture.CrearContexto();
        var persistida = await new EntradaListaEsperaRepository(ctxVerificar).ObtenerPorIdAsync(entrada.Id);
        persistida.Should().NotBeNull();
        persistida!.Estado.Should().Be(EstadoListaEspera.Promovida);
        persistida.PromovidoPorUsuarioId.Should().Be(coordinadorA);
        persistida.FechaPromocion.Should().Be(fechaBase.AddMinutes(1));
        persistida.MotivoPromocion.Should().Be("Promovido por A");
        persistida.Version.Should().Be(3);
    }

    [Fact]
    public async Task Debe_SerializarDosRegistrosParalelosDeVacanteLiberadaMarcandoPosicion1YPosicion2SinColision_Cuando_SeEjecutanConDosDbContext()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var fechaBase = new DateTimeOffset(2026, 11, 27, 15, 0, 0, TimeSpan.Zero);

        var e1 = EntradaListaEspera.Crear(convocatoriaId, programaId, Guid.CreateVersion7(), 1, 1500.00m, fechaBase, actorId);
        var e2 = EntradaListaEspera.Crear(convocatoriaId, programaId, Guid.CreateVersion7(), 2, 1400.00m, fechaBase, actorId);
        var e3 = EntradaListaEspera.Crear(convocatoriaId, programaId, Guid.CreateVersion7(), 3, 1300.00m, fechaBase, actorId);

        await using (var ctxSemilla = _fixture.CrearContexto())
        {
            ctxSemilla.EntradasListaEspera.AddRange(e1, e2, e3);
            await ctxSemilla.GuardarCambiosAsync();
        }

        var disparo = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<EntradaListaEsperaDto> EjecutarVacanteAsync(DateTimeOffset instante)
        {
            await disparo.Task;
            await using var contexto = _fixture.CrearContexto();
            var handler = new RegistrarVacanteLiberadaCommandHandler(
                new UsuarioActualFijo(actorId),
                new GestorConcurrenciaListaEsperaPostgreSql(contexto),
                contexto,
                new RegistrarVacanteLiberadaCommandValidator(),
                new ProveedorTiempoFijo(instante));

            return await handler.HandleAsync(new RegistrarVacanteLiberadaCommand(convocatoriaId, "Vacante paralela"));
        }

        var tarea1 = Task.Run(() => EjecutarVacanteAsync(fechaBase.AddMinutes(5)));
        var tarea2 = Task.Run(() => EjecutarVacanteAsync(fechaBase.AddMinutes(6)));

        disparo.TrySetResult();

        var resultados = await Task.WhenAll(tarea1, tarea2);

        resultados.Select(r => r.PosicionOrden).Should().BeEquivalentTo([1, 2]);
        resultados.Should().OnlyContain(r => r.Estado == "ELEGIBLE_POR_VACANTE" && r.Version == 2);

        await using var ctxVerificar = _fixture.CrearContexto();
        var persistidas = await new EntradaListaEsperaRepository(ctxVerificar).ListarPorConvocatoriaAsync(convocatoriaId);
        persistidas.Should().HaveCount(3);
        persistidas[0].PosicionOrden.Should().Be(1);
        persistidas[0].Estado.Should().Be(EstadoListaEspera.ElegiblePorVacante);
        persistidas[0].Version.Should().Be(2);
        persistidas[1].PosicionOrden.Should().Be(2);
        persistidas[1].Estado.Should().Be(EstadoListaEspera.ElegiblePorVacante);
        persistidas[1].Version.Should().Be(2);
        persistidas[2].PosicionOrden.Should().Be(3);
        persistidas[2].Estado.Should().Be(EstadoListaEspera.EnEspera);
        persistidas[2].Version.Should().Be(1);
    }

    private async Task VerificarCheckViolationAsync(
        Guid convocatoriaId,
        Guid programaId,
        int posicionOrden,
        decimal monto,
        string estado,
        DateTimeOffset? fechaElegibilidad,
        DateTimeOffset? fechaPromocion,
        Guid? promovidoPor,
        string? motivoPromocion,
        string checkEsperada)
    {
        await using var contexto = _fixture.CrearContexto();
        var id = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var ahora = new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

        var ejecutar = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO postulaciones.entradas_lista_espera (
                id, convocatoria_id, programa_beca_id, postulacion_id, posicion_orden,
                monto_estimado_beca_snapshot, estado, fecha_ingreso, fecha_elegibilidad,
                fecha_promocion, promovido_por_usuario_id, motivo_promocion,
                fecha_creacion, usuario_creacion_id, fecha_ultima_modificacion,
                usuario_modificacion_id, version)
            VALUES (
                {id}, {convocatoriaId}, {programaId}, {postulacionId}, {posicionOrden},
                {monto}, {estado}, {ahora}, {fechaElegibilidad},
                {fechaPromocion}, {promovidoPor}, {motivoPromocion},
                {ahora}, {actorId}, {ahora},
                {actorId}, 1)
            """);

        var pg = (await ejecutar.Should().ThrowAsync<PostgresException>()).Which;
        pg.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        pg.ConstraintName.Should().Be(checkEsperada);
    }

    private WebApplicationFactory<Program> CrearFabricaConPostgreSqlReal()
    {
        return _fabricaBase.WithWebHostBuilder(constructor =>
        {
            constructor.UseSetting("ConnectionStrings:Postulaciones", _fixture.CadenaConexion);
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.AddAuthentication(opciones =>
                    {
                        opciones.DefaultAuthenticateScheme = EsquemaPrueba;
                        opciones.DefaultChallengeScheme = EsquemaPrueba;
                        opciones.DefaultScheme = EsquemaPrueba;
                    })
                    .AddScheme<AuthenticationSchemeOptions, ManejadorAutenticacionPrueba>(
                        EsquemaPrueba,
                        _ => { });
            });
        });
    }

    private static ConfirmarRankingConvocatoriaCommandHandler CrearConfirmarHandlerReal(
        PostulacionesDbContext contexto,
        Guid coordinadorId,
        DateTimeOffset instanteConfirmacion,
        IDisponibilidadRankingClient disponibilidadClient,
        IProveedorMontoEstimadoBeca proveedorMonto)
    {
        var timeProvider = new ProveedorTiempoFijo(instanteConfirmacion);
        var generadorReal = new GeneradorListaEsperaRankingAdapter(new EntradaListaEsperaRepository(contexto));

        return new ConfirmarRankingConvocatoriaCommandHandler(
            new UsuarioActualFijo(coordinadorId),
            new RankingConvocatoriaRepository(contexto),
            new ConsultaEstadoEvaluacionesConvocatoriaRepository(contexto),
            new ConvocatoriaEvaluacionCerradaFija(timeProvider),
            disponibilidadClient,
            proveedorMonto,
            generadorReal,
            contexto,
            new ConfirmarRankingConvocatoriaCommandValidator(),
            timeProvider);
    }

    private static PromoverCandidatoListaEsperaCommandHandler CrearPromoverHandlerReal(
        PostulacionesDbContext contexto,
        Guid coordinadorId,
        DateTimeOffset instantePromocion,
        IDisponibilidadRankingClient disponibilidadClient)
    {
        return new PromoverCandidatoListaEsperaCommandHandler(
            new UsuarioActualFijo(coordinadorId),
            new GestorConcurrenciaListaEsperaPostgreSql(contexto),
            new ResultadoConsolidadoRepository(contexto),
            disponibilidadClient,
            contexto,
            new PromoverCandidatoListaEsperaCommandValidator(),
            new ProveedorTiempoFijo(instantePromocion));
    }

    private static IDisponibilidadRankingClient CrearDisponibilidadClient(
        Guid convocatoriaId,
        Guid programaId,
        int cupos,
        decimal presupuestoEfectivo)
    {
        var client = Substitute.For<IDisponibilidadRankingClient>();
        client.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new DisponibilidadProgramaRanking(
                convocatoriaId,
                programaId,
                cuposAsignadosConvocatoria: cupos,
                cuposDisponiblesPrograma: cupos,
                presupuestoDisponibleBruto: presupuestoEfectivo,
                presupuestoComprometidoRenovaciones: 0.00m,
                moneda: "USD"));
        return client;
    }

    private static IProveedorMontoEstimadoBeca CrearProveedorMonto(
        Guid convocatoriaId,
        Guid programaId,
        IReadOnlyDictionary<Guid, decimal> montos)
    {
        var proveedor = Substitute.For<IProveedorMontoEstimadoBeca>();
        proveedor.ObtenerMontosEstimadosPorPostulacionAsync(
                convocatoriaId,
                programaId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(montos);
        return proveedor;
    }

    private sealed class ConvocatoriaEvaluacionCerradaFija(TimeProvider timeProvider)
        : IConvocatoriaEvaluacionClient
    {
        public Task<VentanaRevisionConvocatoria?> ObtenerVentanaRevisionAsync(
            Guid convocatoriaId,
            CancellationToken cancellationToken = default)
        {
            var ahora = timeProvider.GetUtcNow();
            return Task.FromResult<VentanaRevisionConvocatoria?>(
                new VentanaRevisionConvocatoria(
                    convocatoriaId,
                    EstadoConvocatoriaExterna.Cerrada,
                    ahora.AddDays(-15),
                    ahora.AddMinutes(-5)));
        }
    }

    private sealed class UsuarioActualFijo(Guid usuarioId) : IUsuarioActual
    {
        public Guid UsuarioId { get; } = usuarioId;

        public bool EstaAutenticado => true;
    }

    private sealed class RolesUsuarioFijo(params string[] roles) : IRolesUsuarioActual
    {
        public IReadOnlyCollection<string> Roles { get; } = roles;

        public bool TieneRol(string rol) => Roles.Contains(rol, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class ProveedorTiempoFijo(DateTimeOffset instanteUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instanteUtc;
    }

    private sealed class ManejadorAutenticacionPrueba : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public ManejadorAutenticacionPrueba(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderScope, out var valoresScope))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = valoresScope
                .Where(valor => !string.IsNullOrWhiteSpace(valor))
                .Select(valor => new Claim("scope", valor!))
                .ToList();

            var identidad = new ClaimsIdentity(claims, EsquemaPrueba, "sub", "roles");
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identidad), EsquemaPrueba)));
        }
    }
}
