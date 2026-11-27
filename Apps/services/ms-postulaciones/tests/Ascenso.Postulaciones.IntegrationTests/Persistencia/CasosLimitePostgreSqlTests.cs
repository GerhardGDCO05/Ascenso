using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConfirmarRankingConvocatoria;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ResolverCasoLimiteRanking;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerRankingConvocatoria;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.ListaEspera.Interfaces;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Consolidacion.Servicios;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Infrastructure.Adapters;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NSubstitute;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class CasosLimitePostgreSqlTests
{
    private readonly PostgreSqlFixture _fixture;

    public CasosLimitePostgreSqlTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
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
    public async Task Debe_DetectarYResolverEmpatePorCupoPorCoordinadorYPorJefaturaComiteReal_Cuando_SeOperaEnPostgreSqlReal()
    {
        var fechaBase = new DateTimeOffset(2026, 11, 26, 14, 0, 0, TimeSpan.Zero);

        // Escenario 1: Resolución por COORDINADOR_PROGRAMA
        var convocatoriaCoordId = Guid.CreateVersion7();
        var programaCoordId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var p1Coord = Guid.CreateVersion7();
        var p2Coord = Guid.CreateVersion7();
        var p3Coord = Guid.CreateVersion7();

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.ResultadosConsolidadosRanking.AddRange(
                CrearResultadoProvisional(convocatoriaCoordId, p1Coord, 95.00m, fechaBase.AddMinutes(1), coordinadorId),
                CrearResultadoProvisional(convocatoriaCoordId, p2Coord, 88.00m, fechaBase.AddMinutes(2), coordinadorId),
                CrearResultadoProvisional(convocatoriaCoordId, p3Coord, 88.00m, fechaBase.AddMinutes(3), coordinadorId));
            await contextoSemilla.GuardarCambiosAsync();
        }

        var disponibilidadCoord = CrearDisponibilidadClient(convocatoriaCoordId, programaCoordId, cupos: 2, presupuesto: 10000.00m);
        var montosCoord = CrearProveedorMonto(
            convocatoriaCoordId,
            programaCoordId,
            new Dictionary<Guid, decimal>
            {
                [p1Coord] = 1500.00m,
                [p2Coord] = 1500.00m,
                [p3Coord] = 1500.00m
            });

        var instanteDecisionCoord = fechaBase.AddHours(1);
        await using (var contextoResolverCoord = _fixture.CrearContexto())
        {
            var handler = CrearResolverHandler(
                contextoResolverCoord,
                coordinadorId,
                "COORDINADOR_PROGRAMA",
                instanteDecisionCoord,
                disponibilidadCoord,
                montosCoord);

            var dto = await handler.HandleAsync(new ResolverCasoLimiteRankingCommand(
                convocatoriaCoordId,
                [p3Coord],
                "  Desempate por coordinador basado en mayor producción científica verificada.  "));

            dto.TieneEmpateEnLineaDeCorte.Should().BeFalse();
            dto.TieneCasoLimiteActivo.Should().BeFalse();
            dto.TieneCasoLimiteResuelto.Should().BeTrue();
            dto.TotalDentroDeCupo.Should().Be(2);
            dto.TotalListaDeEspera.Should().Be(1);
            dto.Elementos.Items.Select(e => (e.PostulacionId, e.PosicionRanking, e.Clasificacion, e.SeleccionadoEnCasoLimite))
                .Should().Equal(
                    (p1Coord, 1, "DENTRO_DE_CUPO", null),
                    (p3Coord, 2, "DENTRO_DE_CUPO", true),
                    (p2Coord, 3, "LISTA_DE_ESPERA", false));
        }

        await using (var contextoVerificarCoord = _fixture.CrearContexto())
        {
            var filas = await new RankingConvocatoriaRepository(contextoVerificarCoord)
                .ListarPorConvocatoriaAsync(convocatoriaCoordId);

            var filaGanadora = filas.Single(f => f.PostulacionId == p3Coord);
            filaGanadora.TipoCasoLimite.Should().Be(TipoCasoLimite.EmpatePorCupo);
            filaGanadora.SeleccionadoEnCasoLimite.Should().BeTrue();
            filaGanadora.JustificacionDesempateCasoLimite.Should().Be("Desempate por coordinador basado en mayor producción científica verificada.");
            filaGanadora.DecididoPorUsuarioId.Should().Be(coordinadorId);
            filaGanadora.FechaDecisionCasoLimite.Should().Be(instanteDecisionCoord);
            filaGanadora.Version.Should().Be(2);

            var filaNoSeleccionada = filas.Single(f => f.PostulacionId == p2Coord);
            filaNoSeleccionada.TipoCasoLimite.Should().Be(TipoCasoLimite.EmpatePorCupo);
            filaNoSeleccionada.SeleccionadoEnCasoLimite.Should().BeFalse();
            filaNoSeleccionada.JustificacionDesempateCasoLimite.Should().Be("Desempate por coordinador basado en mayor producción científica verificada.");
            filaNoSeleccionada.DecididoPorUsuarioId.Should().Be(coordinadorId);
            filaNoSeleccionada.FechaDecisionCasoLimite.Should().Be(instanteDecisionCoord);
            filaNoSeleccionada.Version.Should().Be(2);
        }

        // Escenario 2: Resolución por Jefatura del comité (verificando consulta real a postulaciones.comites_evaluadores)
        var convocatoriaComiteId = Guid.CreateVersion7();
        var programaComiteId = Guid.CreateVersion7();
        var jefaturaId = Guid.CreateVersion7();
        var miembroRegularId = Guid.CreateVersion7();
        var p1Comite = Guid.CreateVersion7();
        var p2Comite = Guid.CreateVersion7();

        var comite = ComiteEvaluador.Crear(
            programaComiteId,
            convocatoriaComiteId,
            "Comité Evaluador PostgreSQL Caso Límite",
            jefaturaId,
            fechaBase,
            coordinadorId);
        comite.AgregarMiembro(miembroRegularId, TipoEvaluador.Interno, fechaBase.AddMinutes(5), coordinadorId);

        await using (var contextoSemillaComite = _fixture.CrearContexto())
        {
            contextoSemillaComite.ComitesEvaluadores.Add(comite);
            contextoSemillaComite.ResultadosConsolidadosRanking.AddRange(
                CrearResultadoProvisional(convocatoriaComiteId, p1Comite, 90.00m, fechaBase.AddMinutes(10), coordinadorId),
                CrearResultadoProvisional(convocatoriaComiteId, p2Comite, 90.00m, fechaBase.AddMinutes(11), coordinadorId));
            await contextoSemillaComite.GuardarCambiosAsync();
        }

        var disponibilidadComite = CrearDisponibilidadClient(convocatoriaComiteId, programaComiteId, cupos: 1, presupuesto: 5000.00m);
        var montosComite = CrearProveedorMonto(
            convocatoriaComiteId,
            programaComiteId,
            new Dictionary<Guid, decimal>
            {
                [p1Comite] = 1500.00m,
                [p2Comite] = 1500.00m
            });

        // 2.a Miembro regular (no jefatura) es rechazado con 403 PERMISO_DENEGADO contra postulaciones.comites_evaluadores
        await using (var contextoMiembroRegular = _fixture.CrearContexto())
        {
            var handlerMiembro = CrearResolverHandler(
                contextoMiembroRegular,
                miembroRegularId,
                "MIEMBRO_COMITE",
                fechaBase.AddHours(2),
                disponibilidadComite,
                montosComite);

            var accionMiembro = () => handlerMiembro.HandleAsync(new ResolverCasoLimiteRankingCommand(
                convocatoriaComiteId,
                [p1Comite],
                "Intento de resolución por miembro regular sin jefatura."));

            var exPermiso = (await accionMiembro.Should().ThrowAsync<PermisoDenegadoResolucionCasoLimiteException>()).Which;
            exPermiso.Codigo.Should().Be(PermisoDenegadoResolucionCasoLimiteException.CodigoError);
            exPermiso.EstadoHttp.Should().Be(403);
        }

        // 2.b Jefatura activa del comité resuelve exitosamente
        await using (var contextoJefatura = _fixture.CrearContexto())
        {
            var handlerJefatura = CrearResolverHandler(
                contextoJefatura,
                jefaturaId,
                "MIEMBRO_COMITE",
                fechaBase.AddHours(2),
                disponibilidadComite,
                montosComite);

            var dtoJefatura = await handlerJefatura.HandleAsync(new ResolverCasoLimiteRankingCommand(
                convocatoriaComiteId,
                [p2Comite],
                "Resolución oficial emitida por la jefatura activa del comité evaluador."));

            dtoJefatura.TieneEmpateEnLineaDeCorte.Should().BeFalse();
            dtoJefatura.TieneCasoLimiteResuelto.Should().BeTrue();
            dtoJefatura.Elementos.Items.Single(e => e.PostulacionId == p2Comite).DecididoPorUsuarioId.Should().Be(jefaturaId);
        }
    }

    [Fact]
    public async Task Debe_DetectarYResolverCasoLimitePresupuestarioPreservandoOrdenDeMeritoEnListaDeEspera_Cuando_SeOperaEnPostgreSqlReal()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var fechaBase = new DateTimeOffset(2026, 11, 26, 15, 0, 0, TimeSpan.Zero);

        var p1 = Guid.CreateVersion7(); // 94.00 ($2,000.00) -> DENTRO_DE_CUPO (quedan $500.00)
        var p2 = Guid.CreateVersion7(); // 89.00 ($2,000.00) -> Excede $500.00 restantes -> LISTA_DE_ESPERA (Posición 2)
        var p3 = Guid.CreateVersion7(); // 80.00 ($400.00) -> Cabe en $500.00 restantes -> Candidato en CASO_LIMITE_PRESUPUESTARIO

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.ResultadosConsolidadosRanking.AddRange(
                CrearResultadoProvisional(convocatoriaId, p1, 94.00m, fechaBase.AddMinutes(1), coordinadorId),
                CrearResultadoProvisional(convocatoriaId, p2, 89.00m, fechaBase.AddMinutes(2), coordinadorId),
                CrearResultadoProvisional(convocatoriaId, p3, 80.00m, fechaBase.AddMinutes(3), coordinadorId));
            await contextoSemilla.GuardarCambiosAsync();
        }

        var disponibilidadClient = CrearDisponibilidadClient(convocatoriaId, programaId, cupos: 5, presupuesto: 2500.00m);
        var proveedorMonto = CrearProveedorMonto(
            convocatoriaId,
            programaId,
            new Dictionary<Guid, decimal>
            {
                [p1] = 2000.00m,
                [p2] = 2000.00m,
                [p3] = 400.00m
            });

        // 1. Consulta antes de resolver: corte estricto por mérito pero informa CASO_LIMITE_PRESUPUESTARIO activo
        await using (var contextoConsultaPrevia = _fixture.CrearContexto())
        {
            var obtenerHandler = new ObtenerRankingConvocatoriaQueryHandler(
                new RankingConvocatoriaRepository(contextoConsultaPrevia),
                disponibilidadClient,
                proveedorMonto,
                new ObtenerRankingConvocatoriaQueryValidator());

            var previo = await obtenerHandler.HandleAsync(new ObtenerRankingConvocatoriaQuery(convocatoriaId));
            previo.TieneEmpateEnLineaDeCorte.Should().BeFalse();
            previo.TieneCasoLimiteActivo.Should().BeTrue();
            previo.TipoCasoLimiteActivo.Should().Be("CASO_LIMITE_PRESUPUESTARIO");
            previo.PresupuestoDisponibleEnCasoLimite.Should().Be(500.00m);
            previo.PostulacionesEnCasoLimiteActivo.Should().Equal(p3);
            previo.TotalDentroDeCupo.Should().Be(1);
            previo.TotalListaDeEspera.Should().Be(2);
        }

        // 2. Resolver seleccionando p3
        await using (var contextoResolver = _fixture.CrearContexto())
        {
            var handler = CrearResolverHandler(
                contextoResolver,
                coordinadorId,
                "COORDINADOR_PROGRAMA",
                fechaBase.AddHours(1),
                disponibilidadClient,
                proveedorMonto);

            var resuelto = await handler.HandleAsync(new ResolverCasoLimiteRankingCommand(
                convocatoriaId,
                [p3],
                "Se adjudica saldo presupuestario remanente de $500.00 a la postulación elegible de $400.00."));

            resuelto.TieneCasoLimiteActivo.Should().BeFalse();
            resuelto.TieneCasoLimiteResuelto.Should().BeTrue();
            resuelto.TotalDentroDeCupo.Should().Be(2);
            resuelto.TotalListaDeEspera.Should().Be(1);
            resuelto.MontoTotalDentroDeCupo.Should().Be(2400.00m);

            // p2 (89.00) permanece en PosicionRanking = 2 liderando LISTA_DE_ESPERA para HU 10.2
            resuelto.Elementos.Items.Select(e => (e.PostulacionId, e.PosicionRanking, e.Clasificacion, e.TipoCasoLimite, e.SeleccionadoEnCasoLimite))
                .Should().Equal(
                    (p1, 1, "DENTRO_DE_CUPO", null, null),
                    (p2, 2, "LISTA_DE_ESPERA", null, null),
                    (p3, 3, "DENTRO_DE_CUPO", "CASO_LIMITE_PRESUPUESTARIO", true));
        }
    }

    [Fact]
    public async Task Debe_GarantizarInalterabilidadDesbloqueoDeConfirmacionYRebloqueoSiBajaDisponibilidad_Cuando_SeOperaEnPostgreSqlReal()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var fechaBase = new DateTimeOffset(2026, 11, 26, 16, 0, 0, TimeSpan.Zero);

        var p1 = Guid.CreateVersion7();
        var p2 = Guid.CreateVersion7();
        var p3 = Guid.CreateVersion7();
        var p4 = Guid.CreateVersion7();

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.ResultadosConsolidadosRanking.AddRange(
                CrearResultadoProvisional(convocatoriaId, p1, 95.00m, fechaBase.AddMinutes(1), coordinadorId),
                CrearResultadoProvisional(convocatoriaId, p2, 88.00m, fechaBase.AddMinutes(2), coordinadorId),
                CrearResultadoProvisional(convocatoriaId, p3, 88.00m, fechaBase.AddMinutes(3), coordinadorId),
                CrearResultadoProvisional(convocatoriaId, p4, 88.00m, fechaBase.AddMinutes(4), coordinadorId));
            await contextoSemilla.GuardarCambiosAsync();
        }

        var disponibilidad3Cupos = CrearDisponibilidadClient(convocatoriaId, programaId, cupos: 3, presupuesto: 6000.00m);
        var proveedorMonto = CrearProveedorMonto(
            convocatoriaId,
            programaId,
            new Dictionary<Guid, decimal>
            {
                [p1] = 1500.00m,
                [p2] = 1500.00m,
                [p3] = 1500.00m,
                [p4] = 1500.00m
            });

        // 1. Resolver el empate por cupo seleccionando p2 y p3 (2 cupos disponibles para el grupo de 3 empatados)
        var fechaDecision = fechaBase.AddMinutes(30);
        await using (var contextoResolver = _fixture.CrearContexto())
        {
            var resolverHandler = CrearResolverHandler(
                contextoResolver,
                coordinadorId,
                "COORDINADOR_PROGRAMA",
                fechaDecision,
                disponibilidad3Cupos,
                proveedorMonto);

            await resolverHandler.HandleAsync(new ResolverCasoLimiteRankingCommand(
                convocatoriaId,
                [p2, p3],
                "Decisión formal e inalterable de desempate en línea de corte."));
        }

        // 2. Inalterabilidad (Criterio 4): intento de sobrescribir la decisión -> 409 CASO_LIMITE_YA_RESUELTO
        await using (var contextoSobrescribir = _fixture.CrearContexto())
        {
            var resolverHandler = CrearResolverHandler(
                contextoSobrescribir,
                coordinadorId,
                "COORDINADOR_PROGRAMA",
                fechaDecision.AddMinutes(5),
                disponibilidad3Cupos,
                proveedorMonto);

            var sobrescribir = () => resolverHandler.HandleAsync(new ResolverCasoLimiteRankingCommand(
                convocatoriaId,
                [p3, p4],
                "Intento posterior de cambiar el ganador del desempate."));

            var exYaResuelto = (await sobrescribir.Should().ThrowAsync<CasoLimiteYaResueltoException>()).Which;
            exYaResuelto.Codigo.Should().Be(CasoLimiteYaResueltoException.CodigoError);
            exYaResuelto.EstadoHttp.Should().Be(409);
        }

        // 3. Inalterabilidad ante recálculo mutante sobre entidad cargada desde PostgreSQL -> 409 CASO_LIMITE_YA_RESUELTO
        await using (var contextoRecalculo = _fixture.CrearContexto())
        {
            var resultadoRastreado = (await new ResultadoConsolidadoRepository(contextoRecalculo)
                .ObtenerParaActualizarPorPostulacionAsync(p2))!;

            var recalcularMutante = () => resultadoRastreado.Recalcular(
                new ResultadoCalculoConsolidacion(
                    convocatoriaId,
                    p2,
                    excepcionEvaluadorUnico: false,
                    justificacionEvaluadorUnico: null,
                    autorizadorExcepcionId: null,
                    cantidadEvaluacionesValidas: 2,
                    puntajeConsolidado: 91.00m,
                    diferenciaEntreEvaluadores: 2.00m,
                    umbralDiscrepanciaAplicado: 20.00m,
                    alertaDiscrepancia: false),
                fechaDecision.AddMinutes(10),
                coordinadorId);

            recalcularMutante.Should().Throw<CasoLimiteYaResueltoException>()
                .Which.Codigo.Should().Be(CasoLimiteYaResueltoException.CodigoError);
        }

        // 4.a Re-bloqueo con 422 EMPATES_SIN_RESOLVER si los cupos bajan a 2 (queda 1 cupo para el grupo donde se seleccionaron 2)
        var disponibilidad2Cupos = CrearDisponibilidadClient(convocatoriaId, programaId, cupos: 2, presupuesto: 6000.00m);
        await using (var contextoRebloqueoEmpate = _fixture.CrearContexto())
        {
            var confirmarHandler = CrearConfirmarHandler(
                contextoRebloqueoEmpate,
                coordinadorId,
                fechaDecision.AddMinutes(12),
                disponibilidad2Cupos,
                proveedorMonto);

            var confirmarConMenosCupos = () => confirmarHandler.HandleAsync(new ConfirmarRankingConvocatoriaCommand(convocatoriaId));
            var exEmpate = (await confirmarConMenosCupos.Should().ThrowAsync<EmpatesSinResolverEnCorteException>()).Which;
            exEmpate.Codigo.Should().Be(EmpatesSinResolverEnCorteException.CodigoError);
            exEmpate.EstadoHttp.Should().Be(422);
        }

        // 4.b Re-bloqueo con 422 DECISION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD si el presupuesto baja a $2,000.00 (ninguno del grupo cabe)
        var disponibilidadReducida = CrearDisponibilidadClient(convocatoriaId, programaId, cupos: 3, presupuesto: 2000.00m);
        await using (var contextoRebloqueo = _fixture.CrearContexto())
        {
            var confirmarHandler = CrearConfirmarHandler(
                contextoRebloqueo,
                coordinadorId,
                fechaDecision.AddMinutes(15),
                disponibilidadReducida,
                proveedorMonto);

            var confirmarReducido = () => confirmarHandler.HandleAsync(new ConfirmarRankingConvocatoriaCommand(convocatoriaId));
            var exRebloqueo = (await confirmarReducido.Should().ThrowAsync<DecisionCasoLimiteInvalidaPorDisponibilidadException>()).Which;
            exRebloqueo.Codigo.Should().Be(DecisionCasoLimiteInvalidaPorDisponibilidadException.CodigoError);
            exRebloqueo.EstadoHttp.Should().Be(422);
        }

        // 5. Desbloqueo y confirmación PROVISIONAL -> CONFIRMADO cuando la disponibilidad cubre la decisión
        var fechaConfirmacion = fechaDecision.AddMinutes(20);
        await using (var contextoConfirmar = _fixture.CrearContexto())
        {
            var confirmarHandler = CrearConfirmarHandler(
                contextoConfirmar,
                coordinadorId,
                fechaConfirmacion,
                disponibilidad3Cupos,
                proveedorMonto);

            var confirmado = await confirmarHandler.HandleAsync(new ConfirmarRankingConvocatoriaCommand(convocatoriaId));
            confirmado.EstadoRanking.Should().Be("CONFIRMADO");
            confirmado.TieneCasoLimiteResuelto.Should().BeTrue();
            confirmado.TotalDentroDeCupo.Should().Be(3);
            confirmado.TotalListaDeEspera.Should().Be(1);
        }

        // 6. Verificar en PostgreSQL que la confirmación preservó intactos los 5 campos de caso límite
        await using (var contextoVerificarConfirmado = _fixture.CrearContexto())
        {
            var filas = await new RankingConvocatoriaRepository(contextoVerificarConfirmado)
                .ListarPorConvocatoriaAsync(convocatoriaId);

            var filaGanadora = filas.Single(f => f.PostulacionId == p2);
            filaGanadora.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Confirmado);
            filaGanadora.PosicionRanking.Should().Be(2);
            filaGanadora.Clasificacion.Should().Be(ClasificacionRanking.DentroDeCupo);
            filaGanadora.TipoCasoLimite.Should().Be(TipoCasoLimite.EmpatePorCupo);
            filaGanadora.SeleccionadoEnCasoLimite.Should().BeTrue();
            filaGanadora.JustificacionDesempateCasoLimite.Should().Be("Decisión formal e inalterable de desempate en línea de corte.");
            filaGanadora.DecididoPorUsuarioId.Should().Be(coordinadorId);
            filaGanadora.FechaDecisionCasoLimite.Should().Be(fechaDecision);
            filaGanadora.Version.Should().Be(3);

            var filaNoSeleccionada = filas.Single(f => f.PostulacionId == p4);
            filaNoSeleccionada.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Confirmado);
            filaNoSeleccionada.PosicionRanking.Should().Be(4);
            filaNoSeleccionada.Clasificacion.Should().Be(ClasificacionRanking.ListaDeEspera);
            filaNoSeleccionada.TipoCasoLimite.Should().Be(TipoCasoLimite.EmpatePorCupo);
            filaNoSeleccionada.SeleccionadoEnCasoLimite.Should().BeFalse();
            filaNoSeleccionada.Version.Should().Be(3);
        }
    }

    [Fact]
    public async Task Debe_RechazarViolacionesDeLasDosNuevasCheckEnPostgreSqlSinInterceptarLasDiezCheckPrevias()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var ahora = new DateTimeOffset(2026, 11, 26, 18, 0, 0, TimeSpan.Zero);

        await using var contextoSql = _fixture.CrearContexto();

        async Task InsertarFilaDirectaAsync(
            string estado,
            string? tipoCasoLimite,
            bool? seleccionadoCasoLimite,
            string? justificacionCasoLimite,
            Guid? decididoPor,
            DateTimeOffset? fechaDecision)
        {
            await contextoSql.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO postulaciones.resultados_consolidados_ranking
                    (id, convocatoria_id, postulacion_id, excepcion_evaluador_unico, justificacion_evaluador_unico,
                     autorizador_excepcion_id, cantidad_evaluaciones_validas, puntaje_consolidado,
                     diferencia_entre_evaluadores, umbral_discrepancia_aplicado, alerta_discrepancia,
                     discrepancia_revisada, comentario_revision_discrepancia, revisado_por_usuario_id,
                     fecha_revision_discrepancia, estado_confirmacion_ranking, fecha_consolidacion,
                     posicion_ranking, monto_estimado_beca, clasificacion, fecha_confirmacion_ranking,
                     confirmado_por_usuario_id, tipo_caso_limite, seleccionado_en_caso_limite,
                     justificacion_desempate_caso_limite, decidido_por_usuario_id, fecha_decision_caso_limite,
                     fecha_creacion, usuario_creacion_id, fecha_ultima_modificacion, usuario_modificacion_id, version)
                VALUES
                    ({Guid.CreateVersion7()}, {convocatoriaId}, {Guid.CreateVersion7()}, {false}, {(string?)null},
                     {(Guid?)null}, {2}, {88.00m},
                     {4.00m}, {20.00m}, {false},
                     {false}, {(string?)null}, {(Guid?)null},
                     {(DateTimeOffset?)null}, {estado}, {ahora},
                     {(int?)null}, {(decimal?)null}, {(string?)null}, {(DateTimeOffset?)null},
                     {(Guid?)null}, {tipoCasoLimite}, {seleccionadoCasoLimite},
                     {justificacionCasoLimite}, {decididoPor}, {fechaDecision},
                     {ahora}, {actorId}, {ahora}, {actorId}, {1})
                """);
        }

        // 1. Lógica trivaluada SQL: tipo_caso_limite = NULL con justificacion_desempate_caso_limite poblada
        // DEBE ser rechazado por ck_resultados_consolidados_caso_limite_coherente
        var accionTipoNuloConJustificacion = () => InsertarFilaDirectaAsync(
            estado: "PROVISIONAL",
            tipoCasoLimite: null,
            seleccionadoCasoLimite: null,
            justificacionCasoLimite: "Justificación huérfana sin tipo de caso límite.",
            decididoPor: null,
            fechaDecision: null);
        var exTipoNuloParcial = (await accionTipoNuloConJustificacion.Should().ThrowAsync<PostgresException>()).Which;
        exTipoNuloParcial.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exTipoNuloParcial.ConstraintName.Should().Be("ck_resultados_consolidados_caso_limite_coherente");

        // 2. tipo_caso_limite válido pero campos de decisión incompletos -> ck_resultados_consolidados_caso_limite_coherente
        var accionIncompleta = () => InsertarFilaDirectaAsync(
            estado: "PROVISIONAL",
            tipoCasoLimite: "EMPATE_POR_CUPO",
            seleccionadoCasoLimite: true,
            justificacionCasoLimite: "Justificación válida de más de diez caracteres.",
            decididoPor: null,
            fechaDecision: ahora);
        var exIncompleta = (await accionIncompleta.Should().ThrowAsync<PostgresException>()).Which;
        exIncompleta.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exIncompleta.ConstraintName.Should().Be("ck_resultados_consolidados_caso_limite_coherente");

        // 3. Justificación menor de 10 caracteres tras btrim -> ck_resultados_consolidados_caso_limite_coherente
        var accionJustificacionCorta = () => InsertarFilaDirectaAsync(
            estado: "PROVISIONAL",
            tipoCasoLimite: "EMPATE_POR_PRESUPUESTO",
            seleccionadoCasoLimite: false,
            justificacionCasoLimite: "   123456789   ",
            decididoPor: actorId,
            fechaDecision: ahora);
        var exJustificacionCorta = (await accionJustificacionCorta.Should().ThrowAsync<PostgresException>()).Which;
        exJustificacionCorta.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exJustificacionCorta.ConstraintName.Should().Be("ck_resultados_consolidados_caso_limite_coherente");

        // 4. tipo_caso_limite = 'INVALIDO' delega a ck_resultados_consolidados_tipo_caso_limite_valido
        var accionTipoInvalido = () => InsertarFilaDirectaAsync(
            estado: "PROVISIONAL",
            tipoCasoLimite: "INVALIDO",
            seleccionadoCasoLimite: null,
            justificacionCasoLimite: null,
            decididoPor: null,
            fechaDecision: null);
        var exTipoInvalido = (await accionTipoInvalido.Should().ThrowAsync<PostgresException>()).Which;
        exTipoInvalido.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exTipoInvalido.ConstraintName.Should().Be("ck_resultados_consolidados_tipo_caso_limite_valido");

        // 5. Fila con las 5 columnas en NULL y estado_confirmacion_ranking = 'INVALIDO' falla en ck_resultados_consolidados_estado_valido
        var accionEstadoInvalido = () => InsertarFilaDirectaAsync(
            estado: "INVALIDO",
            tipoCasoLimite: null,
            seleccionadoCasoLimite: null,
            justificacionCasoLimite: null,
            decididoPor: null,
            fechaDecision: null);
        var exEstadoInvalido = (await accionEstadoInvalido.Should().ThrowAsync<PostgresException>()).Which;
        exEstadoInvalido.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exEstadoInvalido.ConstraintName.Should().Be("ck_resultados_consolidados_estado_valido");
    }

    [Fact]
    public async Task Debe_GarantizarConcurrenciaOptimistaC1ConRollbackAtomicoTotal_Cuando_DosDecisoresResuelvenSimultaneamenteDesdeDosDbContext()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var decisorA = Guid.CreateVersion7();
        var decisorB = Guid.CreateVersion7();
        var fechaBase = new DateTimeOffset(2026, 11, 26, 19, 0, 0, TimeSpan.Zero);

        var p1 = Guid.CreateVersion7();
        var p2 = Guid.CreateVersion7();

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.ResultadosConsolidadosRanking.AddRange(
                CrearResultadoProvisional(convocatoriaId, p1, 90.00m, fechaBase.AddMinutes(1), decisorA),
                CrearResultadoProvisional(convocatoriaId, p2, 90.00m, fechaBase.AddMinutes(2), decisorA));
            await contextoSemilla.GuardarCambiosAsync();
        }

        var disponibilidad = new DisponibilidadProgramaRanking(
            convocatoriaId,
            programaId,
            cuposAsignadosConvocatoria: 1,
            cuposDisponiblesPrograma: 1,
            presupuestoDisponibleBruto: 5000.00m,
            presupuestoComprometidoRenovaciones: 0.00m,
            moneda: "USD");
        var montos = new Dictionary<Guid, decimal>
        {
            [p1] = 1500.00m,
            [p2] = 1500.00m
        };

        await using var contextoA = _fixture.CrearContexto();
        await using var contextoB = _fixture.CrearContexto();

        var repoA = new RankingConvocatoriaRepository(contextoA);
        var repoB = new RankingConvocatoriaRepository(contextoB);

        // Ambos decisores cargan las filas en estado PROVISIONAL (Version = 1)
        var filasA = await repoA.ListarParaConfirmarPorConvocatoriaAsync(convocatoriaId);
        var filasB = await repoB.ListarParaConfirmarPorConvocatoriaAsync(convocatoriaId);

        var corteA = ServicioCalculoRankingCorte.Calcular(convocatoriaId, filasA, montos, disponibilidad);
        var corteB = ServicioCalculoRankingCorte.Calcular(convocatoriaId, filasB, montos, disponibilidad);

        ServicioResolucionCasoLimite.AplicarResolucion(
            corteA,
            filasA,
            [p1],
            "Decisión ganadora registrada por el decisor A.",
            fechaBase.AddMinutes(10),
            decisorA);

        ServicioResolucionCasoLimite.AplicarResolucion(
            corteB,
            filasB,
            [p2],
            "Decisión concurrente conflictiva del decisor B.",
            fechaBase.AddMinutes(11),
            decisorB);

        await contextoA.GuardarCambiosAsync();

        var guardarB = () => contextoB.GuardarCambiosAsync();
        var exConcurrencia = (await guardarB.Should().ThrowAsync<ExcepcionConflictoConcurrencia>()).Which;
        exConcurrencia.Codigo.Should().Be(ExcepcionConflictoConcurrencia.CodigoError);
        exConcurrencia.EstadoHttp.Should().Be(409);

        await using var contextoVerificar = _fixture.CrearContexto();
        var persistidos = await new RankingConvocatoriaRepository(contextoVerificar)
            .ListarPorConvocatoriaAsync(convocatoriaId);

        persistidos.Should().HaveCount(2);
        persistidos.Should().OnlyContain(r =>
            r.TipoCasoLimite == TipoCasoLimite.EmpatePorCupo
            && r.DecididoPorUsuarioId == decisorA
            && r.JustificacionDesempateCasoLimite == "Decisión ganadora registrada por el decisor A."
            && r.Version == 2);
        persistidos.Single(r => r.PostulacionId == p1).SeleccionadoEnCasoLimite.Should().BeTrue();
        persistidos.Single(r => r.PostulacionId == p2).SeleccionadoEnCasoLimite.Should().BeFalse();
    }

    private static IDisponibilidadRankingClient CrearDisponibilidadClient(
        Guid convocatoriaId,
        Guid programaId,
        int cupos,
        decimal presupuesto)
    {
        var client = Substitute.For<IDisponibilidadRankingClient>();
        client.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new DisponibilidadProgramaRanking(
                convocatoriaId,
                programaId,
                cuposAsignadosConvocatoria: cupos,
                cuposDisponiblesPrograma: cupos,
                presupuestoDisponibleBruto: presupuesto,
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

    private static ResolverCasoLimiteRankingCommandHandler CrearResolverHandler(
        PostulacionesDbContext contexto,
        Guid usuarioDecisorId,
        string rol,
        DateTimeOffset instanteDecision,
        IDisponibilidadRankingClient disponibilidadClient,
        IProveedorMontoEstimadoBeca proveedorMonto)
    {
        var timeProvider = new ProveedorTiempoFijo(instanteDecision);
        return new ResolverCasoLimiteRankingCommandHandler(
            new UsuarioActualFijo(usuarioDecisorId),
            new RolesUsuarioActualFijo(rol),
            new ConsultaJefaturaComiteRepository(contexto),
            new VerificadorConflictoInteresDecisorSinOperacion(),
            new RankingConvocatoriaRepository(contexto),
            new ConsultaEstadoEvaluacionesConvocatoriaRepository(contexto),
            new ConvocatoriaEvaluacionCerradaFija(timeProvider),
            disponibilidadClient,
            proveedorMonto,
            contexto,
            new ResolverCasoLimiteRankingCommandValidator(),
            timeProvider);
    }

    private static ConfirmarRankingConvocatoriaCommandHandler CrearConfirmarHandler(
        PostulacionesDbContext contexto,
        Guid coordinadorId,
        DateTimeOffset instanteConfirmacion,
        IDisponibilidadRankingClient disponibilidadClient,
        IProveedorMontoEstimadoBeca proveedorMonto)
    {
        var timeProvider = new ProveedorTiempoFijo(instanteConfirmacion);
        return new ConfirmarRankingConvocatoriaCommandHandler(
            new UsuarioActualFijo(coordinadorId),
            new RankingConvocatoriaRepository(contexto),
            new ConsultaEstadoEvaluacionesConvocatoriaRepository(contexto),
            new ConvocatoriaEvaluacionCerradaFija(timeProvider),
            disponibilidadClient,
            proveedorMonto,
            Substitute.For<IGeneradorListaEsperaRanking>(),
            contexto,
            new ConfirmarRankingConvocatoriaCommandValidator(),
            timeProvider);
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

    private sealed class RolesUsuarioActualFijo(string rolActivo) : IRolesUsuarioActual
    {
        public bool TieneRol(string rol)
            => string.Equals(rolActivo, rol, StringComparison.Ordinal);
    }

    private sealed class ProveedorTiempoFijo(DateTimeOffset instanteUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instanteUtc;
    }
}
