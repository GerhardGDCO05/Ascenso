using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConfirmarRankingConvocatoria;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.RevisarDiscrepanciaConsolidacion;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ConsultarAdjudicabilidadPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerRankingConvocatoria;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.ListaEspera.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NSubstitute;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class RankingPostgreSqlTests
{
    private readonly PostgreSqlFixture _fixture;

    public RankingPostgreSqlTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private static ResultadoConsolidadoRanking CrearResultadoProvisional(
        Guid convocatoriaId,
        Guid postulacionId,
        decimal puntajeConsolidado,
        DateTimeOffset fechaConsolidacion,
        Guid actorId,
        bool alertaDiscrepancia = false,
        bool discrepanciaRevisada = false)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            convocatoriaId: convocatoriaId,
            postulacionId: postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntajeConsolidado,
            diferenciaEntreEvaluadores: alertaDiscrepancia ? 28.00m : 6.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: alertaDiscrepancia);

        var resultado = ResultadoConsolidadoRanking.Crear(calculo, fechaConsolidacion, actorId);

        if (alertaDiscrepancia && discrepanciaRevisada)
        {
            resultado.MarcarDiscrepanciaRevisada(
                "Discrepancia revisada y validada antes de confirmar el ranking.",
                fechaConsolidacion.AddMinutes(5),
                actorId);
        }

        return resultado;
    }

    [Fact]
    public async Task Debe_ConsultarProvisionalConCortePorCupoYPresupuestoDescontandoRenovaciones_Cuando_SeOperaEnPostgreSqlReal()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var fechaBase = new DateTimeOffset(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

        var p1 = Guid.CreateVersion7();
        var p2 = Guid.CreateVersion7();
        var p3 = Guid.CreateVersion7();
        var p4 = Guid.CreateVersion7();

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.ResultadosConsolidadosRanking.AddRange(
                CrearResultadoProvisional(convocatoriaId, p1, 95.00m, fechaBase.AddMinutes(1), actorId),
                CrearResultadoProvisional(convocatoriaId, p2, 89.00m, fechaBase.AddMinutes(2), actorId),
                CrearResultadoProvisional(convocatoriaId, p3, 82.00m, fechaBase.AddMinutes(3), actorId),
                CrearResultadoProvisional(convocatoriaId, p4, 75.00m, fechaBase.AddMinutes(4), actorId));
            await contextoSemilla.GuardarCambiosAsync();
        }

        var disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
        disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new DisponibilidadProgramaRanking(
                convocatoriaId,
                programaId,
                cuposAsignadosConvocatoria: 4,
                cuposDisponiblesPrograma: 3, // Cupos efectivos = 3
                presupuestoDisponibleBruto: 6000.00m,
                presupuestoComprometidoRenovaciones: 2500.00m, // Presupuesto efectivo = 3500.00 (solo alcanzan 2 becas de 1500.00)
                moneda: "USD"));

        var proveedorMonto = Substitute.For<IProveedorMontoEstimadoBeca>();
        proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                convocatoriaId,
                programaId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>
            {
                [p1] = 1500.00m,
                [p2] = 1500.00m,
                [p3] = 1500.00m,
                [p4] = 1500.00m
            });

        await using var contextoConsulta = _fixture.CrearContexto();
        var handler = new ObtenerRankingConvocatoriaQueryHandler(
            new RankingConvocatoriaRepository(contextoConsulta),
            disponibilidadClient,
            proveedorMonto,
            new ObtenerRankingConvocatoriaQueryValidator());

        var ranking = await handler.HandleAsync(
            new ObtenerRankingConvocatoriaQuery(convocatoriaId, Pagina: 1, Tamano: 20));

        ranking.EstadoRanking.Should().Be("PROVISIONAL");
        ranking.TotalPostulantes.Should().Be(4);
        ranking.TotalDentroDeCupo.Should().Be(2);
        ranking.TotalListaDeEspera.Should().Be(2);
        ranking.MontoTotalDentroDeCupo.Should().Be(3000.00m);
        ranking.Disponibilidad.Should().NotBeNull();
        ranking.Disponibilidad!.CuposDisponiblesEfectivos.Should().Be(3);
        ranking.Disponibilidad.PresupuestoDisponibleEfectivo.Should().Be(3500.00m);
        ranking.Elementos.Items.Select(e => (e.PostulacionId, e.PosicionRanking, e.Clasificacion))
            .Should().Equal(
                (p1, 1, "DENTRO_DE_CUPO"),
                (p2, 2, "DENTRO_DE_CUPO"),
                (p3, 3, "LISTA_DE_ESPERA"),
                (p4, 4, "LISTA_DE_ESPERA"));
    }

    [Fact]
    public async Task Debe_BloquearConfirmacionPorEmpateEnLineaDeCorteSinModificarBaseDeDatos_Cuando_SeOperaEnPostgreSqlReal()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var fechaBase = new DateTimeOffset(2026, 11, 26, 11, 0, 0, TimeSpan.Zero);

        var p1 = Guid.CreateVersion7();
        var p2 = Guid.CreateVersion7();
        var p3 = Guid.CreateVersion7();

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.ResultadosConsolidadosRanking.AddRange(
                CrearResultadoProvisional(convocatoriaId, p1, 95.00m, fechaBase.AddMinutes(1), coordinadorId),
                CrearResultadoProvisional(convocatoriaId, p2, 88.00m, fechaBase.AddMinutes(2), coordinadorId),
                CrearResultadoProvisional(convocatoriaId, p3, 88.00m, fechaBase.AddMinutes(3), coordinadorId));
            await contextoSemilla.GuardarCambiosAsync();
        }

        var disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
        disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new DisponibilidadProgramaRanking(
                convocatoriaId,
                programaId,
                cuposAsignadosConvocatoria: 2,
                cuposDisponiblesPrograma: 2,
                presupuestoDisponibleBruto: 10000.00m,
                presupuestoComprometidoRenovaciones: 0.00m,
                moneda: "USD"));

        var proveedorMonto = Substitute.For<IProveedorMontoEstimadoBeca>();
        proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                convocatoriaId,
                programaId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>
            {
                [p1] = 1000.00m,
                [p2] = 1000.00m,
                [p3] = 1000.00m
            });

        await using (var contextoConfirmar = _fixture.CrearContexto())
        {
            var handler = CrearConfirmarHandler(
                contextoConfirmar,
                coordinadorId,
                fechaBase.AddHours(1),
                disponibilidadClient,
                proveedorMonto);

            var accion = () => handler.HandleAsync(new ConfirmarRankingConvocatoriaCommand(convocatoriaId));
            var ex = (await accion.Should().ThrowAsync<EmpatesSinResolverEnCorteException>()).Which;
            ex.Codigo.Should().Be(EmpatesSinResolverEnCorteException.CodigoError);
            ex.EstadoHttp.Should().Be(422);
        }

        await using var contextoVerificar = _fixture.CrearContexto();
        var filas = await contextoVerificar.ResultadosConsolidadosRanking
            .AsNoTracking()
            .Where(r => r.ConvocatoriaId == convocatoriaId)
            .ToListAsync();

        filas.Should().HaveCount(3);
        filas.Should().OnlyContain(r =>
            r.EstadoConfirmacionRanking == EstadoRanking.Provisional
            && r.PosicionRanking == null
            && r.MontoEstimadoBeca == null
            && r.Clasificacion == null
            && r.FechaConfirmacionRanking == null
            && r.ConfirmadoPorUsuarioId == null
            && r.Version == 1);
    }

    [Fact]
    public async Task Debe_BloquearConfirmacionPorEvaluacionesPendientesOPostulacionSinConsolidarEIgnorarReasignadasPorConflicto_Cuando_SeOperaEnPostgreSqlReal()
    {
        var programaId = Guid.CreateVersion7();
        var convocatoriaId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var evaluador1Id = Guid.CreateVersion7();
        var evaluador2Id = Guid.CreateVersion7();
        var evaluadorConflictoId = Guid.CreateVersion7();
        var postulacionConsolidadaId = Guid.CreateVersion7();
        var postulacionSinConsolidarId = Guid.CreateVersion7();
        var fechaBase = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

        var comite = ComiteEvaluador.Crear(
            programaId,
            convocatoriaId,
            "Comité Ranking PostgreSQL",
            evaluador1Id,
            fechaBase,
            coordinadorId);
        var miembro1 = comite.Miembros.Single();
        var miembro2 = comite.AgregarMiembro(evaluador2Id, TipoEvaluador.Externo, fechaBase.AddMinutes(1), coordinadorId);
        var miembroConflicto = comite.AgregarMiembro(evaluadorConflictoId, TipoEvaluador.Interno, fechaBase.AddMinutes(2), coordinadorId);

        var asig1 = AsignacionEvaluacion.Crear(comite, miembro1.Id, postulacionConsolidadaId, fechaBase.AddMinutes(3), fechaBase.AddDays(5), coordinadorId);
        asig1.IniciarEvaluacion(fechaBase.AddMinutes(4), evaluador1Id);
        asig1.CompletarEvaluacion(fechaBase.AddMinutes(5), evaluador1Id);

        var asig2 = AsignacionEvaluacion.Crear(comite, miembro2.Id, postulacionConsolidadaId, fechaBase.AddMinutes(3), fechaBase.AddDays(5), coordinadorId);
        asig2.IniciarEvaluacion(fechaBase.AddMinutes(4), evaluador2Id);
        asig2.CompletarEvaluacion(fechaBase.AddMinutes(5), evaluador2Id);

        var asigConflicto = AsignacionEvaluacion.Crear(comite, miembroConflicto.Id, postulacionConsolidadaId, fechaBase.AddMinutes(3), fechaBase.AddDays(5), coordinadorId);
        var asigSinConsolidar = AsignacionEvaluacion.Crear(comite, miembro1.Id, postulacionSinConsolidarId, fechaBase.AddMinutes(6), fechaBase.AddDays(5), coordinadorId);

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.ComitesEvaluadores.Add(comite);
            contextoSemilla.AsignacionesEvaluacion.AddRange(asig1, asig2, asigConflicto, asigSinConsolidar);
            contextoSemilla.ResultadosConsolidadosRanking.Add(
                CrearResultadoProvisional(convocatoriaId, postulacionConsolidadaId, 90.00m, fechaBase.AddMinutes(10), coordinadorId));
            await contextoSemilla.GuardarCambiosAsync();

            await contextoSemilla.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE postulaciones.asignaciones_evaluacion
                SET estado = 'REASIGNADA_POR_CONFLICTO'
                WHERE id = {asigConflicto.Id}
                """);
        }

        var disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
        var proveedorMonto = Substitute.For<IProveedorMontoEstimadoBeca>();

        // 1. Bloqueo porque asigSinConsolidar está en PENDIENTE
        await using (var contextoPendiente = _fixture.CrearContexto())
        {
            var handler = CrearConfirmarHandler(
                contextoPendiente,
                coordinadorId,
                fechaBase.AddHours(1),
                disponibilidadClient,
                proveedorMonto);

            var accion = () => handler.HandleAsync(new ConfirmarRankingConvocatoriaCommand(convocatoriaId));
            var ex = (await accion.Should().ThrowAsync<EvaluacionesPendientesParaConsolidarException>()).Which;
            ex.Codigo.Should().Be(EvaluacionesPendientesParaConsolidarException.CodigoError);
            ex.EstadoHttp.Should().Be(409);
        }

        // 2. Completar asigSinConsolidar pero sin insertar ResultadoConsolidadoRanking -> sigue bloqueado por postulación sin consolidar
        await using (var contextoCompletarAsignacion = _fixture.CrearContexto())
        {
            await contextoCompletarAsignacion.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE postulaciones.asignaciones_evaluacion
                SET estado = 'COMPLETADA'
                WHERE id = {asigSinConsolidar.Id}
                """);
        }

        await using (var contextoSinConsolidado = _fixture.CrearContexto())
        {
            var handler = CrearConfirmarHandler(
                contextoSinConsolidado,
                coordinadorId,
                fechaBase.AddHours(1),
                disponibilidadClient,
                proveedorMonto);

            var accion = () => handler.HandleAsync(new ConfirmarRankingConvocatoriaCommand(convocatoriaId));
            (await accion.Should().ThrowAsync<EvaluacionesPendientesParaConsolidarException>())
                .Which.Codigo.Should().Be(EvaluacionesPendientesParaConsolidarException.CodigoError);
        }

        // 3. Si la asignación de la segunda postulación pasa a REASIGNADA_POR_CONFLICTO, ya no bloquea
        await using (var contextoReasignar = _fixture.CrearContexto())
        {
            await contextoReasignar.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE postulaciones.asignaciones_evaluacion
                SET estado = 'REASIGNADA_POR_CONFLICTO'
                WHERE id = {asigSinConsolidar.Id}
                """);

            var repoEstado = new ConsultaEstadoEvaluacionesConvocatoriaRepository(contextoReasignar);
            (await repoEstado.ExistenEvaluacionesPendientesOSinConsolidarAsync(convocatoriaId))
                .Should().BeFalse();
        }
    }

    [Fact]
    public async Task Debe_ExigirAcuseAnteDiscrepanciasEnCorteConfirmarAtomicamenteConsultarInmutableVerificarAdjudicabilidadYBloquearMutacionesPosteriores_Cuando_SeOperaEnPostgreSqlReal()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var fechaBase = new DateTimeOffset(2026, 11, 26, 13, 0, 0, TimeSpan.Zero);
        var fechaConfirmacionLocal = new DateTimeOffset(2026, 11, 26, 10, 30, 0, TimeSpan.FromHours(-4));
        var fechaConfirmacionUtc = fechaConfirmacionLocal.ToUniversalTime();

        var postulacionDentro1 = Guid.CreateVersion7();
        var postulacionDentro2ConAlerta = Guid.CreateVersion7();
        var postulacionEspera1 = Guid.CreateVersion7();

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.ResultadosConsolidadosRanking.AddRange(
                CrearResultadoProvisional(convocatoriaId, postulacionDentro1, 96.00m, fechaBase.AddMinutes(1), coordinadorId),
                CrearResultadoProvisional(convocatoriaId, postulacionDentro2ConAlerta, 90.00m, fechaBase.AddMinutes(2), coordinadorId, alertaDiscrepancia: true, discrepanciaRevisada: false),
                CrearResultadoProvisional(convocatoriaId, postulacionEspera1, 81.50m, fechaBase.AddMinutes(3), coordinadorId));
            await contextoSemilla.GuardarCambiosAsync();
        }

        var disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
        disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(new DisponibilidadProgramaRanking(
                convocatoriaId,
                programaId,
                cuposAsignadosConvocatoria: 2,
                cuposDisponiblesPrograma: 2,
                presupuestoDisponibleBruto: 5000.00m,
                presupuestoComprometidoRenovaciones: 1000.00m,
                moneda: "USD"));

        var proveedorMonto = Substitute.For<IProveedorMontoEstimadoBeca>();
        proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                convocatoriaId,
                programaId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>
            {
                [postulacionDentro1] = 1500.00m,
                [postulacionDentro2ConAlerta] = 1500.00m,
                [postulacionEspera1] = 1500.00m
            });

        // 1. Antes de confirmar: adjudicabilidad es false con RANKING_NO_CONFIRMADO
        await using (var contextoAdjudicablePrevio = _fixture.CrearContexto())
        {
            var adjudicabilidadHandler = new ConsultarAdjudicabilidadPostulacionQueryHandler(
                new ResultadoConsolidadoRepository(contextoAdjudicablePrevio),
                Substitute.For<IEntradaListaEsperaRepository>(),
                new ConsultarAdjudicabilidadPostulacionQueryValidator());

            var adjPrevia = await adjudicabilidadHandler.HandleAsync(
                new ConsultarAdjudicabilidadPostulacionQuery(postulacionDentro1));
            adjPrevia.Adjudicable.Should().BeFalse();
            adjPrevia.MotivoNoAdjudicable.Should().Be("RANKING_NO_CONFIRMADO");
        }

        // 2. Sin acuse de advertencia de discrepancia -> 422 DISCREPANCIAS_SIN_REVISAR_EN_CORTE
        await using (var contextoSinAcuse = _fixture.CrearContexto())
        {
            var handler = CrearConfirmarHandler(
                contextoSinAcuse,
                coordinadorId,
                fechaConfirmacionLocal,
                disponibilidadClient,
                proveedorMonto);

            var accionSinAcuse = () => handler.HandleAsync(
                new ConfirmarRankingConvocatoriaCommand(convocatoriaId, ConfirmarConAdvertenciaDiscrepancias: false));
            var ex = (await accionSinAcuse.Should().ThrowAsync<DiscrepanciasSinRevisarEnCorteException>()).Which;
            ex.Codigo.Should().Be(DiscrepanciasSinRevisarEnCorteException.CodigoError);
            ex.EstadoHttp.Should().Be(422);
        }

        // 3. Con ConfirmarConAdvertenciaDiscrepancias = true -> confirma atómicamente las 3 filas
        await using (var contextoConfirmar = _fixture.CrearContexto())
        {
            var handler = CrearConfirmarHandler(
                contextoConfirmar,
                coordinadorId,
                fechaConfirmacionLocal,
                disponibilidadClient,
                proveedorMonto);

            var confirmadoDto = await handler.HandleAsync(
                new ConfirmarRankingConvocatoriaCommand(convocatoriaId, ConfirmarConAdvertenciaDiscrepancias: true));

            confirmadoDto.EstadoRanking.Should().Be("CONFIRMADO");
            confirmadoDto.FechaConfirmacionRanking.Should().Be(fechaConfirmacionUtc);
            confirmadoDto.ConfirmadoPorUsuarioId.Should().Be(coordinadorId);
            confirmadoDto.TotalPostulantes.Should().Be(3);
            confirmadoDto.TotalDentroDeCupo.Should().Be(2);
            confirmadoDto.TotalListaDeEspera.Should().Be(1);
            confirmadoDto.MontoTotalDentroDeCupo.Should().Be(3000.00m);
        }

        // 4. Verificar persistencia real de las 3 filas en PostgreSQL
        await using (var contextoVerificarBd = _fixture.CrearContexto())
        {
            var filas = await new RankingConvocatoriaRepository(contextoVerificarBd)
                .ListarPorConvocatoriaAsync(convocatoriaId);

            filas.Should().HaveCount(3);
            filas.Select(f => (
                    f.PostulacionId,
                    f.EstadoConfirmacionRanking,
                    f.PosicionRanking,
                    f.MontoEstimadoBeca,
                    f.Clasificacion,
                    f.FechaConfirmacionRanking,
                    f.ConfirmadoPorUsuarioId,
                    f.Version))
                .Should().Equal(
                    (postulacionDentro1, EstadoRanking.Confirmado, 1, 1500.00m, ClasificacionRanking.DentroDeCupo, fechaConfirmacionUtc, coordinadorId, 2),
                    (postulacionDentro2ConAlerta, EstadoRanking.Confirmado, 2, 1500.00m, ClasificacionRanking.DentroDeCupo, fechaConfirmacionUtc, coordinadorId, 2),
                    (postulacionEspera1, EstadoRanking.Confirmado, 3, 1500.00m, ClasificacionRanking.ListaDeEspera, fechaConfirmacionUtc, coordinadorId, 2));
        }

        // 5. Consulta posterior del ranking devuelve la fotografía inmutable sin llamar a MS-PRG
        var disponibilidadFalla = Substitute.For<IDisponibilidadRankingClient>();
        disponibilidadFalla
            .When(c => c.ObtenerDisponibilidadPorConvocatoriaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new DependenciaNoDisponibleException("No debe invocarse en CONFIRMADO"));
        var proveedorMontoFalla = Substitute.For<IProveedorMontoEstimadoBeca>();

        await using (var contextoConsultaConfirmado = _fixture.CrearContexto())
        {
            var obtenerHandler = new ObtenerRankingConvocatoriaQueryHandler(
                new RankingConvocatoriaRepository(contextoConsultaConfirmado),
                disponibilidadFalla,
                proveedorMontoFalla,
                new ObtenerRankingConvocatoriaQueryValidator());

            var fotografia = await obtenerHandler.HandleAsync(
                new ObtenerRankingConvocatoriaQuery(convocatoriaId, Clasificacion: "DENTRO_DE_CUPO", Pagina: 1, Tamano: 10));

            fotografia.EstadoRanking.Should().Be("CONFIRMADO");
            fotografia.Disponibilidad.Should().BeNull();
            fotografia.TotalPostulantes.Should().Be(3);
            fotografia.TotalDentroDeCupo.Should().Be(2);
            fotografia.TotalListaDeEspera.Should().Be(1);
            fotografia.Elementos.Total.Should().Be(2);
            fotografia.Elementos.Items.Select(e => e.PostulacionId)
                .Should().Equal(postulacionDentro1, postulacionDentro2ConAlerta);
        }

        // 6. Verificación real de adjudicabilidad: DENTRO_DE_CUPO -> true, LISTA_DE_ESPERA -> false
        await using (var contextoAdjudicabilidad = _fixture.CrearContexto())
        {
            var adjudicabilidadHandler = new ConsultarAdjudicabilidadPostulacionQueryHandler(
                new ResultadoConsolidadoRepository(contextoAdjudicabilidad),
                Substitute.For<IEntradaListaEsperaRepository>(),
                new ConsultarAdjudicabilidadPostulacionQueryValidator());

            var adjDentro = await adjudicabilidadHandler.HandleAsync(
                new ConsultarAdjudicabilidadPostulacionQuery(postulacionDentro1));
            adjDentro.Adjudicable.Should().BeTrue();
            adjDentro.EstadoConfirmacionRanking.Should().Be("CONFIRMADO");
            adjDentro.Clasificacion.Should().Be("DENTRO_DE_CUPO");
            adjDentro.PosicionRanking.Should().Be(1);
            adjDentro.MontoEstimadoBeca.Should().Be(1500.00m);
            adjDentro.MotivoNoAdjudicable.Should().BeNull();

            var adjEspera = await adjudicabilidadHandler.HandleAsync(
                new ConsultarAdjudicabilidadPostulacionQuery(postulacionEspera1));
            adjEspera.Adjudicable.Should().BeFalse();
            adjEspera.EstadoConfirmacionRanking.Should().Be("CONFIRMADO");
            adjEspera.Clasificacion.Should().Be("LISTA_DE_ESPERA");
            adjEspera.PosicionRanking.Should().Be(3);
            adjEspera.MotivoNoAdjudicable.Should().Be("FUERA_DE_CUPO_EN_LISTA_DE_ESPERA");
        }

        // 7. Bloqueo posterior de reconfirmación, revisión de discrepancia y recálculo (409 RANKING_YA_CONFIRMADO)
        await using (var contextoBloqueos = _fixture.CrearContexto())
        {
            var confirmarHandler = CrearConfirmarHandler(
                contextoBloqueos,
                coordinadorId,
                fechaConfirmacionUtc.AddMinutes(10),
                disponibilidadClient,
                proveedorMonto);

            var reconfirmar = () => confirmarHandler.HandleAsync(
                new ConfirmarRankingConvocatoriaCommand(convocatoriaId, ConfirmarConAdvertenciaDiscrepancias: true));
            (await reconfirmar.Should().ThrowAsync<RankingYaConfirmadoException>())
                .Which.Codigo.Should().Be(RankingYaConfirmadoException.CodigoError);

            var revisarHandler = new RevisarDiscrepanciaConsolidacionCommandHandler(
                new UsuarioActualFijo(coordinadorId),
                new ResultadoConsolidadoRepository(contextoBloqueos),
                contextoBloqueos,
                new RevisarDiscrepanciaConsolidacionCommandValidator(),
                new ProveedorTiempoFijo(fechaConfirmacionUtc.AddMinutes(10)));

            var revisarPosterior = () => revisarHandler.HandleAsync(
                new RevisarDiscrepanciaConsolidacionCommand(
                    postulacionDentro2ConAlerta,
                    "Intento de revisar discrepancia tras confirmación del ranking."));
            (await revisarPosterior.Should().ThrowAsync<RankingYaConfirmadoException>())
                .Which.Codigo.Should().Be(RankingYaConfirmadoException.CodigoError);

            var resultadoConfirmadoRastreado = (await new ResultadoConsolidadoRepository(contextoBloqueos)
                .ObtenerParaActualizarPorPostulacionAsync(postulacionDentro1))!;
            var recalcularPosterior = () => resultadoConfirmadoRastreado.Recalcular(
                new ResultadoCalculoConsolidacion(
                    convocatoriaId,
                    postulacionDentro1,
                    excepcionEvaluadorUnico: false,
                    justificacionEvaluadorUnico: null,
                    autorizadorExcepcionId: null,
                    cantidadEvaluacionesValidas: 2,
                    puntajeConsolidado: 99.00m,
                    diferenciaEntreEvaluadores: 2.00m,
                    umbralDiscrepanciaAplicado: 20.00m,
                    alertaDiscrepancia: false),
                fechaConfirmacionUtc.AddMinutes(10),
                coordinadorId);
            recalcularPosterior.Should().Throw<RankingYaConfirmadoException>()
                .Which.Codigo.Should().Be(RankingYaConfirmadoException.CodigoError);
        }
    }

    [Fact]
    public async Task Debe_RechazarViolacionesDeLasTresNuevasCheckYDelIndiceUnicoParcialDePosicion_Cuando_SeOperaEnPostgreSqlReal()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var ahora = new DateTimeOffset(2026, 11, 26, 16, 0, 0, TimeSpan.Zero);

        await using var contextoSql = _fixture.CrearContexto();

        async Task InsertarFilaDirectaAsync(
            Guid postulacionId,
            string estado,
            int? posicion,
            decimal? monto,
            string? clasificacion,
            DateTimeOffset? fechaConfirmacion,
            Guid? confirmadoPor)
        {
            await contextoSql.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO postulaciones.resultados_consolidados_ranking
                    (id, convocatoria_id, postulacion_id, excepcion_evaluador_unico, justificacion_evaluador_unico,
                     autorizador_excepcion_id, cantidad_evaluaciones_validas, puntaje_consolidado,
                     diferencia_entre_evaluadores, umbral_discrepancia_aplicado, alerta_discrepancia,
                     discrepancia_revisada, comentario_revision_discrepancia, revisado_por_usuario_id,
                     fecha_revision_discrepancia, estado_confirmacion_ranking, fecha_consolidacion,
                     posicion_ranking, monto_estimado_beca, clasificacion, fecha_confirmacion_ranking,
                     confirmado_por_usuario_id, fecha_creacion, usuario_creacion_id,
                     fecha_ultima_modificacion, usuario_modificacion_id, version)
                VALUES
                    ({Guid.CreateVersion7()}, {convocatoriaId}, {postulacionId}, {false}, {(string?)null},
                     {(Guid?)null}, {2}, {88.00m},
                     {4.00m}, {20.00m}, {false},
                     {false}, {(string?)null}, {(Guid?)null},
                     {(DateTimeOffset?)null}, {estado}, {ahora},
                     {posicion}, {monto}, {clasificacion}, {fechaConfirmacion},
                     {confirmadoPor}, {ahora}, {actorId},
                     {ahora}, {actorId}, {1})
                """);
        }

        // 1. ck_resultados_consolidados_clasificacion_valida
        var accionClasificacionInvalida = () => InsertarFilaDirectaAsync(
            Guid.CreateVersion7(),
            estado: "CONFIRMADO",
            posicion: 1,
            monto: 1000.00m,
            clasificacion: "FUERA_CATALOGO",
            fechaConfirmacion: ahora,
            confirmadoPor: actorId);
        var exClasificacion = (await accionClasificacionInvalida.Should().ThrowAsync<PostgresException>()).Which;
        exClasificacion.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exClasificacion.ConstraintName.Should().Be("ck_resultados_consolidados_clasificacion_valida");

        // 2. ck_resultados_consolidados_ranking_campos_rango (posicion < 1 y monto <= 0)
        var accionPosicionCero = () => InsertarFilaDirectaAsync(
            Guid.CreateVersion7(),
            estado: "CONFIRMADO",
            posicion: 0,
            monto: 1000.00m,
            clasificacion: "DENTRO_DE_CUPO",
            fechaConfirmacion: ahora,
            confirmadoPor: actorId);
        var exPosicion = (await accionPosicionCero.Should().ThrowAsync<PostgresException>()).Which;
        exPosicion.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exPosicion.ConstraintName.Should().Be("ck_resultados_consolidados_ranking_campos_rango");

        var accionMontoCero = () => InsertarFilaDirectaAsync(
            Guid.CreateVersion7(),
            estado: "CONFIRMADO",
            posicion: 1,
            monto: 0.00m,
            clasificacion: "DENTRO_DE_CUPO",
            fechaConfirmacion: ahora,
            confirmadoPor: actorId);
        var exMonto = (await accionMontoCero.Should().ThrowAsync<PostgresException>()).Which;
        exMonto.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exMonto.ConstraintName.Should().Be("ck_resultados_consolidados_ranking_campos_rango");

        // 3. ck_resultados_consolidados_confirmacion_coherente (PROVISIONAL con posición no nula y CONFIRMADO con campos nulos)
        var accionProvisionalIncoherente = () => InsertarFilaDirectaAsync(
            Guid.CreateVersion7(),
            estado: "PROVISIONAL",
            posicion: 1,
            monto: null,
            clasificacion: null,
            fechaConfirmacion: null,
            confirmadoPor: null);
        var exProvisional = (await accionProvisionalIncoherente.Should().ThrowAsync<PostgresException>()).Which;
        exProvisional.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exProvisional.ConstraintName.Should().Be("ck_resultados_consolidados_confirmacion_coherente");

        var accionConfirmadoIncoherente = () => InsertarFilaDirectaAsync(
            Guid.CreateVersion7(),
            estado: "CONFIRMADO",
            posicion: 1,
            monto: 1000.00m,
            clasificacion: "DENTRO_DE_CUPO",
            fechaConfirmacion: null,
            confirmadoPor: actorId);
        var exConfirmado = (await accionConfirmadoIncoherente.Should().ThrowAsync<PostgresException>()).Which;
        exConfirmado.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exConfirmado.ConstraintName.Should().Be("ck_resultados_consolidados_confirmacion_coherente");

        // 4. ux_resultados_consolidados_ranking_convocatoria_posicion (permite múltiples NULL en PROVISIONAL pero rechaza posición duplicada en la misma convocatoria)
        await InsertarFilaDirectaAsync(
            Guid.CreateVersion7(),
            estado: "PROVISIONAL",
            posicion: null,
            monto: null,
            clasificacion: null,
            fechaConfirmacion: null,
            confirmadoPor: null);
        await InsertarFilaDirectaAsync(
            Guid.CreateVersion7(),
            estado: "PROVISIONAL",
            posicion: null,
            monto: null,
            clasificacion: null,
            fechaConfirmacion: null,
            confirmadoPor: null);

        var postulacionPos1A = Guid.CreateVersion7();
        var postulacionPos1B = Guid.CreateVersion7();

        await using (var contextoPos1 = _fixture.CrearContexto())
        {
            var r1 = CrearResultadoProvisional(convocatoriaId, postulacionPos1A, 95.00m, ahora, actorId);
            r1.ConfirmarRanking(1, 1200.00m, ClasificacionRanking.DentroDeCupo, ahora.AddMinutes(5), actorId);
            contextoPos1.ResultadosConsolidadosRanking.Add(r1);
            await contextoPos1.GuardarCambiosAsync();
        }

        await using (var contextoPosDuplicada = _fixture.CrearContexto())
        {
            var rDuplicado = CrearResultadoProvisional(convocatoriaId, postulacionPos1B, 92.00m, ahora, actorId);
            rDuplicado.ConfirmarRanking(1, 1200.00m, ClasificacionRanking.DentroDeCupo, ahora.AddMinutes(6), actorId);
            contextoPosDuplicada.ResultadosConsolidadosRanking.Add(rDuplicado);

            var guardarDuplicado = () => contextoPosDuplicada.GuardarCambiosAsync();
            var exDuplicado = (await guardarDuplicado.Should().ThrowAsync<ExcepcionConflictoConcurrencia>()).Which;
            exDuplicado.Codigo.Should().Be(ExcepcionConflictoConcurrencia.CodigoRestriccionUnicidad);
            exDuplicado.EstadoHttp.Should().Be(409);
        }
    }

    [Fact]
    public async Task Debe_GarantizarConcurrenciaOptimistaConRollbackAtomicoTotal_Cuando_DosCoordinadoresIntentanConfirmarSimultaneamenteDesdeDosDbContext()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var programaId = Guid.CreateVersion7();
        var coordinadorA = Guid.CreateVersion7();
        var coordinadorB = Guid.CreateVersion7();
        var fechaBase = new DateTimeOffset(2026, 11, 26, 17, 0, 0, TimeSpan.Zero);

        var p1 = Guid.CreateVersion7();
        var p2 = Guid.CreateVersion7();

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.ResultadosConsolidadosRanking.AddRange(
                CrearResultadoProvisional(convocatoriaId, p1, 95.00m, fechaBase.AddMinutes(1), coordinadorA),
                CrearResultadoProvisional(convocatoriaId, p2, 85.00m, fechaBase.AddMinutes(2), coordinadorA));
            await contextoSemilla.GuardarCambiosAsync();
        }

        await using var contextoA = _fixture.CrearContexto();
        await using var contextoB = _fixture.CrearContexto();

        var repoA = new RankingConvocatoriaRepository(contextoA);
        var repoB = new RankingConvocatoriaRepository(contextoB);

        // Ambos coordinadores cargan las filas en estado PROVISIONAL (Version = 1) antes de que cualquiera confirme
        var filasA = await repoA.ListarParaConfirmarPorConvocatoriaAsync(convocatoriaId);
        var filasB = await repoB.ListarParaConfirmarPorConvocatoriaAsync(convocatoriaId);

        var fechaConfirmacionA = fechaBase.AddMinutes(10);
        var fechaConfirmacionB = fechaBase.AddMinutes(11);

        filasA[0].ConfirmarRanking(1, 1500.00m, ClasificacionRanking.DentroDeCupo, fechaConfirmacionA, coordinadorA);
        filasA[1].ConfirmarRanking(2, 1500.00m, ClasificacionRanking.ListaDeEspera, fechaConfirmacionA, coordinadorA);

        // Coordinador B intenta confirmar con otros valores sobre Version = 1 obsoleta
        filasB[0].ConfirmarRanking(1, 1800.00m, ClasificacionRanking.DentroDeCupo, fechaConfirmacionB, coordinadorB);
        filasB[1].ConfirmarRanking(2, 1800.00m, ClasificacionRanking.DentroDeCupo, fechaConfirmacionB, coordinadorB);

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
            r.EstadoConfirmacionRanking == EstadoRanking.Confirmado
            && r.ConfirmadoPorUsuarioId == coordinadorA
            && r.MontoEstimadoBeca == 1500.00m
            && r.Version == 2);
        persistidos[0].Clasificacion.Should().Be(ClasificacionRanking.DentroDeCupo);
        persistidos[1].Clasificacion.Should().Be(ClasificacionRanking.ListaDeEspera);
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

    private sealed class ProveedorTiempoFijo(DateTimeOffset instanteUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instanteUtc;
    }
}
