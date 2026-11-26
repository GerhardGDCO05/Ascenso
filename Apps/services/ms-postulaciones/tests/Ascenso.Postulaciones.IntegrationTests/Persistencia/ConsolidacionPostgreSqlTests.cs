using Ascenso.Postulaciones.Application.Asignaciones.Comandos.AutorizarExcepcionEvaluadorUnico;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConsolidarPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.RevisarDiscrepanciaConsolidacion;
using Ascenso.Postulaciones.Application.Consolidacion.Servicios;
using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.EnviarEvaluacionDefinitiva;
using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.GuardarBorradorEvaluacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Servicios;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.BloquearRubrica;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using Ascenso.Postulaciones.Infrastructure.Adapters;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class ConsolidacionPostgreSqlTests
{
    private readonly PostgreSqlFixture _fixture;

    public ConsolidacionPostgreSqlTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(
        RubricaEvaluacion Rubrica,
        CriterioEvaluacion Criterio,
        ComiteEvaluador Comite,
        AsignacionEvaluacion Asignacion1,
        AsignacionEvaluacion Asignacion2,
        Guid Evaluador1Id,
        Guid Evaluador2Id,
        Guid CoordinadorId,
        Guid PostulacionId,
        DateTimeOffset FechaBase)> SembrarEscenarioDosEvaluadoresAsync()
    {
        var programaBecaId = Guid.CreateVersion7();
        var convocatoriaId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var evaluador1Id = Guid.CreateVersion7();
        var evaluador2Id = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();
        var fechaBase = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

        var rubrica = RubricaEvaluacion.Crear(
            programaBecaId,
            convocatoriaId,
            "Rúbrica Consolidación PostgreSQL 2026",
            fechaBase,
            coordinadorId);
        var criterio = rubrica.AgregarCriterio(
            "Evaluación global",
            "Puntaje en escala 0 a 100",
            100.00m,
            0.00m,
            100.00m,
            fechaBase.AddMinutes(1),
            coordinadorId);
        rubrica.Publicar(fechaBase.AddMinutes(2), coordinadorId);

        var comite = ComiteEvaluador.Crear(
            programaBecaId,
            convocatoriaId,
            "Comité Consolidación PostgreSQL 2026",
            evaluador1Id,
            fechaBase.AddMinutes(3),
            coordinadorId);
        var miembro1 = comite.Miembros.Single();
        var miembro2 = comite.AgregarMiembro(
            evaluador2Id,
            TipoEvaluador.Externo,
            fechaBase.AddMinutes(4),
            coordinadorId);

        var asignacion1 = AsignacionEvaluacion.Crear(
            comite,
            miembro1.Id,
            postulacionId,
            fechaBase.AddMinutes(5),
            fechaBase.AddDays(7),
            coordinadorId);
        var asignacion2 = AsignacionEvaluacion.Crear(
            comite,
            miembro2.Id,
            postulacionId,
            fechaBase.AddMinutes(6),
            fechaBase.AddDays(7),
            coordinadorId);

        await using var contextoSemilla = _fixture.CrearContexto();
        contextoSemilla.Rubricas.Add(rubrica);
        contextoSemilla.ComitesEvaluadores.Add(comite);
        contextoSemilla.AsignacionesEvaluacion.AddRange(asignacion1, asignacion2);
        await contextoSemilla.GuardarCambiosAsync();

        return (
            rubrica,
            criterio,
            comite,
            asignacion1,
            asignacion2,
            evaluador1Id,
            evaluador2Id,
            coordinadorId,
            postulacionId,
            fechaBase);
    }

    [Fact]
    public async Task Debe_PersistirYRecargarResultadoConsolidadoConTiposTimestamptzUtcNumericYEstado_Cuando_SeOperaEnPostgreSqlReal()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();
        var usuarioActorId = Guid.CreateVersion7();
        var fechaLocal = new DateTimeOffset(2026, 11, 26, 14, 15, 0, TimeSpan.FromHours(-4));
        var fechaUtc = fechaLocal.ToUniversalTime();

        var calculo = new ResultadoCalculoConsolidacion(
            convocatoriaId,
            postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 3,
            puntajeConsolidado: 80.01m,
            diferenciaEntreEvaluadores: 20.01m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: true);

        Guid resultadoId;

        await using (var contextoEscritura = _fixture.CrearContexto())
        {
            var repo = new ResultadoConsolidadoRepository(contextoEscritura);
            var resultado = ResultadoConsolidadoRanking.Crear(calculo, fechaLocal, usuarioActorId);
            resultadoId = resultado.Id;

            await repo.AgregarAsync(resultado);
            await contextoEscritura.GuardarCambiosAsync();
        }

        await using (var contextoLectura = _fixture.CrearContexto())
        {
            var repo = new ResultadoConsolidadoRepository(contextoLectura);
            var leido = await repo.ObtenerPorPostulacionAsync(postulacionId);

            leido.Should().NotBeNull();
            leido!.Id.Should().Be(resultadoId);
            leido.ConvocatoriaId.Should().Be(convocatoriaId);
            leido.PostulacionId.Should().Be(postulacionId);
            leido.ExcepcionEvaluadorUnico.Should().BeFalse();
            leido.JustificacionEvaluadorUnico.Should().BeNull();
            leido.AutorizadorExcepcionId.Should().BeNull();
            leido.CantidadEvaluacionesValidas.Should().Be(3);
            leido.PuntajeConsolidado.Should().Be(80.01m);
            leido.DiferenciaEntreEvaluadores.Should().Be(20.01m);
            leido.UmbralDiscrepanciaAplicado.Should().Be(20.00m);
            leido.AlertaDiscrepancia.Should().BeTrue();
            leido.DiscrepanciaRevisada.Should().BeFalse();
            leido.ComentarioRevisionDiscrepancia.Should().BeNull();
            leido.RevisadoPorUsuarioId.Should().BeNull();
            leido.FechaRevisionDiscrepancia.Should().BeNull();
            leido.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Provisional);
            leido.FechaConsolidacion.Should().Be(fechaUtc);
            leido.FechaConsolidacion.Offset.Should().Be(TimeSpan.Zero);
            leido.FechaCreacion.Should().Be(fechaUtc);
            leido.FechaCreacion.Offset.Should().Be(TimeSpan.Zero);
            leido.Version.Should().Be(1);
        }
    }

    [Fact]
    public async Task Debe_ConsolidarAutomaticamenteAlEnviarSegundaEvaluacionDefinitivaYRevisarYListarPorConvocatoria_Cuando_SeOperaEnPostgreSqlReal()
    {
        // Postulación 1: 80.00 y 90.00 -> promedio 85.00, diferencia 10.00, alerta = false
        var (
            _,
            criterio1,
            _,
            asignacion1A,
            asignacion1B,
            evaluador1A,
            evaluador1B,
            coordinadorId,
            postulacion1Id,
            fechaBase) = await SembrarEscenarioDosEvaluadoresAsync();

        await GuardarYEnviarEvaluacionAsync(
            asignacion1A.Id,
            criterio1.Id,
            80.00m,
            evaluador1A,
            fechaBase.AddMinutes(10));

        // Tras la 1.ª evaluación definitiva aún no debe existir resultado consolidado
        await using (var contextoIntermedio = _fixture.CrearContexto())
        {
            var repo = new ResultadoConsolidadoRepository(contextoIntermedio);
            (await repo.ObtenerPorPostulacionAsync(postulacion1Id)).Should().BeNull();
        }

        await GuardarYEnviarEvaluacionAsync(
            asignacion1B.Id,
            criterio1.Id,
            90.00m,
            evaluador1B,
            fechaBase.AddMinutes(15));

        await using (var contextoVerificar1 = _fixture.CrearContexto())
        {
            var repo = new ResultadoConsolidadoRepository(contextoVerificar1);
            var resultado1 = await repo.ObtenerPorPostulacionAsync(postulacion1Id);

            resultado1.Should().NotBeNull();
            resultado1!.CantidadEvaluacionesValidas.Should().Be(2);
            resultado1.PuntajeConsolidado.Should().Be(85.00m);
            resultado1.DiferenciaEntreEvaluadores.Should().Be(10.00m);
            resultado1.AlertaDiscrepancia.Should().BeFalse();
            resultado1.DiscrepanciaRevisada.Should().BeFalse();
            resultado1.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Provisional);
        }

        // Postulación 2 (en la misma convocatoria): 60.00 y 85.00 -> promedio 72.50, diferencia 25.00, alerta = true
        var postulacion2Id = Guid.CreateVersion7();
        Guid asignacion2AId;
        Guid asignacion2BId;
        await using (var contextoSemilla2 = _fixture.CrearContexto())
        {
            var comiteLeido = (await new ComiteEvaluadorRepository(contextoSemilla2)
                .ObtenerPorIdAsync(asignacion1A.ComiteEvaluadorId))!;
            var miembro1 = comiteLeido.Miembros.First(m => m.UsuarioEvaluadorId == evaluador1A);
            var miembro2 = comiteLeido.Miembros.First(m => m.UsuarioEvaluadorId == evaluador1B);

            var asig2A = AsignacionEvaluacion.Crear(
                comiteLeido,
                miembro1.Id,
                postulacion2Id,
                fechaBase.AddMinutes(20),
                fechaBase.AddDays(7),
                coordinadorId);
            var asig2B = AsignacionEvaluacion.Crear(
                comiteLeido,
                miembro2.Id,
                postulacion2Id,
                fechaBase.AddMinutes(21),
                fechaBase.AddDays(7),
                coordinadorId);
            asignacion2AId = asig2A.Id;
            asignacion2BId = asig2B.Id;

            contextoSemilla2.AsignacionesEvaluacion.AddRange(asig2A, asig2B);
            await contextoSemilla2.GuardarCambiosAsync();
        }

        await GuardarYEnviarEvaluacionAsync(
            asignacion2AId,
            criterio1.Id,
            60.00m,
            evaluador1A,
            fechaBase.AddMinutes(25));
        await GuardarYEnviarEvaluacionAsync(
            asignacion2BId,
            criterio1.Id,
            85.00m,
            evaluador1B,
            fechaBase.AddMinutes(30));

        // Verificar listado filtrado por alertaDiscrepancia=true y discrepanciaRevisada=false
        await using (var contextoListarPendientes = _fixture.CrearContexto())
        {
            var repo = new ResultadoConsolidadoRepository(contextoListarPendientes);
            var pendientes = await repo.ListarPorConvocatoriaAsync(
                asignacion1A.ConvocatoriaId,
                alertaDiscrepancia: true,
                discrepanciaRevisada: false,
                pagina: 1,
                tamano: 10);

            pendientes.Total.Should().Be(1);
            pendientes.Items.Should().ContainSingle(r =>
                r.PostulacionId == postulacion2Id
                && r.PuntajeConsolidado == 72.50m
                && r.DiferenciaEntreEvaluadores == 25.00m
                && r.AlertaDiscrepancia
                && !r.DiscrepanciaRevisada);
        }

        // Revisar discrepancia de postulación 2
        var fechaRevision = fechaBase.AddMinutes(40);
        await using (var contextoRevisar = _fixture.CrearContexto())
        {
            var handlerRevisar = new RevisarDiscrepanciaConsolidacionCommandHandler(
                new UsuarioActualFijo(coordinadorId),
                new ResultadoConsolidadoRepository(contextoRevisar),
                contextoRevisar,
                new RevisarDiscrepanciaConsolidacionCommandValidator(),
                new ProveedorTiempoFijo(fechaRevision));

            var dtoRevisado = await handlerRevisar.HandleAsync(
                new RevisarDiscrepanciaConsolidacionCommand(
                    postulacion2Id,
                    "  Se revisaron ambas actas y se valida el promedio de 72.50 puntos.  "));

            dtoRevisado.DiscrepanciaRevisada.Should().BeTrue();
            dtoRevisado.ComentarioRevisionDiscrepancia.Should().Be(
                "Se revisaron ambas actas y se valida el promedio de 72.50 puntos.");
            dtoRevisado.RevisadoPorUsuarioId.Should().Be(coordinadorId);
            dtoRevisado.FechaRevisionDiscrepancia.Should().Be(fechaRevision.ToUniversalTime());
            dtoRevisado.Version.Should().Be(2);
        }

        // Verificar listado general ordenado por PuntajeConsolidado DESC y filtrado por discrepanciaRevisada=true
        await using (var contextoListarFinal = _fixture.CrearContexto())
        {
            var repo = new ResultadoConsolidadoRepository(contextoListarFinal);

            var todos = await repo.ListarPorConvocatoriaAsync(
                asignacion1A.ConvocatoriaId,
                alertaDiscrepancia: null,
                discrepanciaRevisada: null,
                pagina: 1,
                tamano: 10);
            todos.Total.Should().Be(2);
            todos.Items.Select(r => r.PostulacionId)
                .Should().Equal(postulacion1Id, postulacion2Id);

            var revisadas = await repo.ListarPorConvocatoriaAsync(
                asignacion1A.ConvocatoriaId,
                alertaDiscrepancia: true,
                discrepanciaRevisada: true,
                pagina: 1,
                tamano: 10);
            revisadas.Total.Should().Be(1);
            revisadas.Items.Single().PostulacionId.Should().Be(postulacion2Id);
        }
    }

    [Fact]
    public async Task Debe_ConsolidarAutomaticamenteConExcepcionEvaluadorUnicoAutorizadaAntesYDespuesDeLaEvaluacion_Cuando_SeOperaEnPostgreSqlReal()
    {
        var programaBecaId = Guid.CreateVersion7();
        var convocatoriaId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var evaluadorId = Guid.CreateVersion7();
        var postulacionAntesId = Guid.CreateVersion7();
        var postulacionDespuesId = Guid.CreateVersion7();
        var fechaBase = new DateTimeOffset(2026, 11, 26, 15, 0, 0, TimeSpan.Zero);

        var rubrica = RubricaEvaluacion.Crear(
            programaBecaId,
            convocatoriaId,
            "Rúbrica Excepción Consolidación",
            fechaBase,
            coordinadorId);
        var criterio = rubrica.AgregarCriterio(
            "Evaluación única",
            "Puntaje directo",
            100.00m,
            0.00m,
            100.00m,
            fechaBase.AddMinutes(1),
            coordinadorId);
        rubrica.Publicar(fechaBase.AddMinutes(2), coordinadorId);

        var comite = ComiteEvaluador.Crear(
            programaBecaId,
            convocatoriaId,
            "Comité Excepción Consolidación",
            evaluadorId,
            fechaBase.AddMinutes(3),
            coordinadorId);
        var miembro = comite.Miembros.Single();

        var asignacionAntes = AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            postulacionAntesId,
            fechaBase.AddMinutes(4),
            fechaBase.AddDays(7),
            coordinadorId);
        var asignacionDespues = AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            postulacionDespuesId,
            fechaBase.AddMinutes(5),
            fechaBase.AddDays(7),
            coordinadorId);

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.Rubricas.Add(rubrica);
            contextoSemilla.ComitesEvaluadores.Add(comite);
            contextoSemilla.AsignacionesEvaluacion.AddRange(asignacionAntes, asignacionDespues);
            await contextoSemilla.GuardarCambiosAsync();
        }

        // Caso A: excepción autorizada ANTES de que el único evaluador envíe su evaluación definitiva
        await using (var contextoExcepcionAntes = _fixture.CrearContexto())
        {
            var handlerAutorizar = CrearAutorizarExcepcionHandler(
                contextoExcepcionAntes,
                coordinadorId,
                new ProveedorTiempoFijo(fechaBase.AddMinutes(6)));

            await handlerAutorizar.HandleAsync(new AutorizarExcepcionEvaluadorUnicoCommand(
                postulacionAntesId,
                "Único especialista disponible antes de iniciar la evaluación."));
        }

        await GuardarYEnviarEvaluacionAsync(
            asignacionAntes.Id,
            criterio.Id,
            88.50m,
            evaluadorId,
            fechaBase.AddMinutes(10));

        await using (var contextoVerificarAntes = _fixture.CrearContexto())
        {
            var repo = new ResultadoConsolidadoRepository(contextoVerificarAntes);
            var resultadoAntes = await repo.ObtenerPorPostulacionAsync(postulacionAntesId);

            resultadoAntes.Should().NotBeNull();
            resultadoAntes!.ExcepcionEvaluadorUnico.Should().BeTrue();
            resultadoAntes.JustificacionEvaluadorUnico.Should().Be(
                "Único especialista disponible antes de iniciar la evaluación.");
            resultadoAntes.AutorizadorExcepcionId.Should().Be(coordinadorId);
            resultadoAntes.CantidadEvaluacionesValidas.Should().Be(1);
            resultadoAntes.PuntajeConsolidado.Should().Be(88.50m);
            resultadoAntes.DiferenciaEntreEvaluadores.Should().Be(0.00m);
            resultadoAntes.AlertaDiscrepancia.Should().BeFalse();
        }

        // Caso B: el único evaluador envía primero (no consolida por falta de excepción), y luego se autoriza la excepción
        await GuardarYEnviarEvaluacionAsync(
            asignacionDespues.Id,
            criterio.Id,
            76.25m,
            evaluadorId,
            fechaBase.AddMinutes(15));

        await using (var contextoSinExcepcionAun = _fixture.CrearContexto())
        {
            var repo = new ResultadoConsolidadoRepository(contextoSinExcepcionAun);
            (await repo.ObtenerPorPostulacionAsync(postulacionDespuesId)).Should().BeNull();
        }

        await using (var contextoExcepcionDespues = _fixture.CrearContexto())
        {
            var handlerAutorizar = CrearAutorizarExcepcionHandler(
                contextoExcepcionDespues,
                coordinadorId,
                new ProveedorTiempoFijo(fechaBase.AddMinutes(20)));

            await handlerAutorizar.HandleAsync(new AutorizarExcepcionEvaluadorUnicoCommand(
                postulacionDespuesId,
                "Excepción autorizada cuando la única evaluación definitiva ya estaba completada."));
        }

        await using (var contextoVerificarDespues = _fixture.CrearContexto())
        {
            var repo = new ResultadoConsolidadoRepository(contextoVerificarDespues);
            var resultadoDespues = await repo.ObtenerPorPostulacionAsync(postulacionDespuesId);

            resultadoDespues.Should().NotBeNull();
            resultadoDespues!.ExcepcionEvaluadorUnico.Should().BeTrue();
            resultadoDespues.JustificacionEvaluadorUnico.Should().Be(
                "Excepción autorizada cuando la única evaluación definitiva ya estaba completada.");
            resultadoDespues.AutorizadorExcepcionId.Should().Be(coordinadorId);
            resultadoDespues.CantidadEvaluacionesValidas.Should().Be(1);
            resultadoDespues.PuntajeConsolidado.Should().Be(76.25m);
            resultadoDespues.DiferenciaEntreEvaluadores.Should().Be(0.00m);
            resultadoDespues.AlertaDiscrepancia.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Debe_ExcluirEstrictamenteEvaluacionesEnBorradorEInvalidadasPorConflicto_Cuando_SeConsolidaEnPostgreSqlReal()
    {
        var (
            rubrica,
            criterio,
            comite,
            asignacion1,
            asignacion2,
            evaluador1Id,
            evaluador2Id,
            coordinadorId,
            postulacionId,
            fechaBase) = await SembrarEscenarioDosEvaluadoresAsync();

        var evaluadorInvalidadoId = Guid.CreateVersion7();
        var evaluadorBorradorId = Guid.CreateVersion7();
        Guid asignacionInvalidadaId;
        Guid evaluacionInvalidadaId;

        await using (var contextoExtra = _fixture.CrearContexto())
        {
            var comiteRastreado = (await new ComiteEvaluadorRepository(contextoExtra)
                .ObtenerParaActualizarPorIdAsync(comite.Id))!;
            var miembroInvalidado = comiteRastreado.AgregarMiembro(
                evaluadorInvalidadoId,
                TipoEvaluador.Externo,
                fechaBase.AddMinutes(7),
                coordinadorId);
            var miembroBorrador = comiteRastreado.AgregarMiembro(
                evaluadorBorradorId,
                TipoEvaluador.Interno,
                fechaBase.AddMinutes(8),
                coordinadorId);

            var asignacionInvalidada = AsignacionEvaluacion.Crear(
                comiteRastreado,
                miembroInvalidado.Id,
                postulacionId,
                fechaBase.AddMinutes(9),
                fechaBase.AddDays(7),
                coordinadorId);
            asignacionInvalidadaId = asignacionInvalidada.Id;

            var evalInvalidada = Evaluacion.CrearBorrador(
                asignacionInvalidada,
                rubrica,
                [new DatosCalificacionCriterio(criterio.Id, 20.00m, "Sesgada")],
                "Evaluación que será invalidada",
                fechaBase.AddMinutes(10),
                evaluadorInvalidadoId);
            evalInvalidada.EnviarDefinitiva(
                rubrica,
                asignacionInvalidada.FechaLimiteEvaluacion,
                fechaBase.AddMinutes(11),
                evaluadorInvalidadoId);
            evaluacionInvalidadaId = evalInvalidada.Id;

            var asignacionBorrador = AsignacionEvaluacion.Crear(
                comiteRastreado,
                miembroBorrador.Id,
                postulacionId,
                fechaBase.AddMinutes(12),
                fechaBase.AddDays(7),
                coordinadorId);
            var evalBorrador = Evaluacion.CrearBorrador(
                asignacionBorrador,
                rubrica,
                [new DatosCalificacionCriterio(criterio.Id, 10.00m, "Borrador")],
                "Borrador no enviado",
                fechaBase.AddMinutes(13),
                evaluadorBorradorId);

            contextoExtra.AsignacionesEvaluacion.AddRange(asignacionInvalidada, asignacionBorrador);
            contextoExtra.Evaluaciones.AddRange(evalInvalidada, evalBorrador);
            await contextoExtra.GuardarCambiosAsync();

            // Marcar la asignación extra como REASIGNADA_POR_CONFLICTO y la evaluación como INVALIDADA_POR_CONFLICTO
            // y la asignación del borrador también como REASIGNADA_POR_CONFLICTO para que solo queden asignacion1 y asignacion2 vigentes
            await contextoExtra.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE postulaciones.asignaciones_evaluacion
                SET estado = 'REASIGNADA_POR_CONFLICTO'
                WHERE id IN ({asignacionInvalidadaId}, {asignacionBorrador.Id})
                """);
            await contextoExtra.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE postulaciones.evaluaciones
                SET estado = 'INVALIDADA_POR_CONFLICTO',
                    motivo_invalidacion = 'Conflicto de interés declarado por parentesco.'
                WHERE id = {evaluacionInvalidadaId}
                """);
        }

        await GuardarYEnviarEvaluacionAsync(
            asignacion1.Id,
            criterio.Id,
            80.00m,
            evaluador1Id,
            fechaBase.AddMinutes(20));
        await GuardarYEnviarEvaluacionAsync(
            asignacion2.Id,
            criterio.Id,
            90.00m,
            evaluador2Id,
            fechaBase.AddMinutes(25));

        await using var contextoVerificar = _fixture.CrearContexto();
        var repoDatos = new ConsultaDatosConsolidacionRepository(contextoVerificar);
        var definitivas = await repoDatos.ListarEvaluacionesDefinitivasPorPostulacionAsync(postulacionId);
        definitivas.Should().HaveCount(2);
        definitivas.Select(e => e.PuntajeTotalPonderado).Should().Equal(80.00m, 90.00m);

        var repoResultados = new ResultadoConsolidadoRepository(contextoVerificar);
        var consolidado = await repoResultados.ObtenerPorPostulacionAsync(postulacionId);
        consolidado.Should().NotBeNull();
        consolidado!.CantidadEvaluacionesValidas.Should().Be(2);
        consolidado.PuntajeConsolidado.Should().Be(85.00m);
        consolidado.DiferenciaEntreEvaluadores.Should().Be(10.00m);
        consolidado.AlertaDiscrepancia.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_TraducirViolacionDeIndiceUnicoAResultadoConsolidadoDuplicadoYRechazarViolacionesCheck_Cuando_SeOperaEnPostgreSqlReal()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var ahora = new DateTimeOffset(2026, 11, 26, 16, 0, 0, TimeSpan.Zero);

        var calculoBase = new ResultadoCalculoConsolidacion(
            convocatoriaId,
            postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: 80.00m,
            diferenciaEntreEvaluadores: 10.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: false);

        await using (var contexto1 = _fixture.CrearContexto())
        {
            var repo1 = new ResultadoConsolidadoRepository(contexto1);
            await repo1.AgregarAsync(ResultadoConsolidadoRanking.Crear(calculoBase, ahora, actorId));
            await contexto1.GuardarCambiosAsync();
        }

        // 1. Violación de ux_resultados_consolidados_ranking_postulacion traducida a ResultadoConsolidadoDuplicadoException
        await using (var contextoDuplicado = _fixture.CrearContexto())
        {
            var repo2 = new ResultadoConsolidadoRepository(contextoDuplicado);
            await repo2.AgregarAsync(ResultadoConsolidadoRanking.Crear(calculoBase, ahora.AddMinutes(1), actorId));

            var guardarDuplicado = () => contextoDuplicado.GuardarCambiosAsync();
            var exDuplicado = (await guardarDuplicado.Should().ThrowAsync<ResultadoConsolidadoDuplicadoException>()).Which;
            exDuplicado.Codigo.Should().Be(ResultadoConsolidadoDuplicadoException.CodigoError);
            exDuplicado.EstadoHttp.Should().Be(409);
        }

        await using var contextoSql = _fixture.CrearContexto();

        async Task VerificarViolacionCheckAsync(
            string estado,
            bool excepcion,
            string? justificacionExcepcion,
            Guid? autorizadorExcepcionId,
            int cantidadEvaluaciones,
            decimal puntajeConsolidado,
            decimal diferencia,
            decimal umbral,
            bool alerta,
            bool revisada,
            string? comentarioRevision,
            Guid? revisadoPorId,
            DateTimeOffset? fechaRevision,
            string nombreCheckEsperado)
        {
            var accion = () => contextoSql.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO postulaciones.resultados_consolidados_ranking
                    (id, convocatoria_id, postulacion_id, excepcion_evaluador_unico, justificacion_evaluador_unico,
                     autorizador_excepcion_id, cantidad_evaluaciones_validas, puntaje_consolidado,
                     diferencia_entre_evaluadores, umbral_discrepancia_aplicado, alerta_discrepancia,
                     discrepancia_revisada, comentario_revision_discrepancia, revisado_por_usuario_id,
                     fecha_revision_discrepancia, estado_confirmacion_ranking, fecha_consolidacion,
                     fecha_creacion, usuario_creacion_id, fecha_ultima_modificacion, usuario_modificacion_id, version)
                VALUES
                    ({Guid.CreateVersion7()}, {convocatoriaId}, {Guid.CreateVersion7()}, {excepcion}, {justificacionExcepcion},
                     {autorizadorExcepcionId}, {cantidadEvaluaciones}, {puntajeConsolidado},
                     {diferencia}, {umbral}, {alerta},
                     {revisada}, {comentarioRevision}, {revisadoPorId},
                     {fechaRevision}, {estado}, {ahora},
                     {ahora}, {actorId}, {ahora}, {actorId}, {1})
                """);

            var pgEx = (await accion.Should().ThrowAsync<PostgresException>()).Which;
            pgEx.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
            pgEx.ConstraintName.Should().Be(nombreCheckEsperado);
        }

        // 2. ck_resultados_consolidados_estado_valido
        await VerificarViolacionCheckAsync(
            estado: "INVALIDO",
            excepcion: false,
            justificacionExcepcion: null,
            autorizadorExcepcionId: null,
            cantidadEvaluaciones: 2,
            puntajeConsolidado: 80.00m,
            diferencia: 10.00m,
            umbral: 20.00m,
            alerta: false,
            revisada: false,
            comentarioRevision: null,
            revisadoPorId: null,
            fechaRevision: null,
            nombreCheckEsperado: "ck_resultados_consolidados_estado_valido");

        // 3. ck_resultados_consolidados_puntaje_rango
        await VerificarViolacionCheckAsync(
            estado: "PROVISIONAL",
            excepcion: false,
            justificacionExcepcion: null,
            autorizadorExcepcionId: null,
            cantidadEvaluaciones: 2,
            puntajeConsolidado: 105.00m,
            diferencia: 10.00m,
            umbral: 20.00m,
            alerta: false,
            revisada: false,
            comentarioRevision: null,
            revisadoPorId: null,
            fechaRevision: null,
            nombreCheckEsperado: "ck_resultados_consolidados_puntaje_rango");

        // 4. ck_resultados_consolidados_diferencia_rango
        await VerificarViolacionCheckAsync(
            estado: "PROVISIONAL",
            excepcion: false,
            justificacionExcepcion: null,
            autorizadorExcepcionId: null,
            cantidadEvaluaciones: 2,
            puntajeConsolidado: 80.00m,
            diferencia: -1.00m,
            umbral: 20.00m,
            alerta: false,
            revisada: false,
            comentarioRevision: null,
            revisadoPorId: null,
            fechaRevision: null,
            nombreCheckEsperado: "ck_resultados_consolidados_diferencia_rango");

        // 5. ck_resultados_consolidados_umbral_rango
        await VerificarViolacionCheckAsync(
            estado: "PROVISIONAL",
            excepcion: false,
            justificacionExcepcion: null,
            autorizadorExcepcionId: null,
            cantidadEvaluaciones: 2,
            puntajeConsolidado: 80.00m,
            diferencia: 10.00m,
            umbral: 0.00m,
            alerta: true,
            revisada: false,
            comentarioRevision: null,
            revisadoPorId: null,
            fechaRevision: null,
            nombreCheckEsperado: "ck_resultados_consolidados_umbral_rango");

        // 6. ck_resultados_consolidados_excepcion_coherente
        await VerificarViolacionCheckAsync(
            estado: "PROVISIONAL",
            excepcion: true,
            justificacionExcepcion: null,
            autorizadorExcepcionId: actorId,
            cantidadEvaluaciones: 1,
            puntajeConsolidado: 80.00m,
            diferencia: 0.00m,
            umbral: 20.00m,
            alerta: false,
            revisada: false,
            comentarioRevision: null,
            revisadoPorId: null,
            fechaRevision: null,
            nombreCheckEsperado: "ck_resultados_consolidados_excepcion_coherente");

        // 7. ck_resultados_consolidados_alerta_coherente
        await VerificarViolacionCheckAsync(
            estado: "PROVISIONAL",
            excepcion: false,
            justificacionExcepcion: null,
            autorizadorExcepcionId: null,
            cantidadEvaluaciones: 2,
            puntajeConsolidado: 80.00m,
            diferencia: 10.00m,
            umbral: 20.00m,
            alerta: true,
            revisada: false,
            comentarioRevision: null,
            revisadoPorId: null,
            fechaRevision: null,
            nombreCheckEsperado: "ck_resultados_consolidados_alerta_coherente");

        // 8. ck_resultados_consolidados_revision_coherente
        await VerificarViolacionCheckAsync(
            estado: "PROVISIONAL",
            excepcion: false,
            justificacionExcepcion: null,
            autorizadorExcepcionId: null,
            cantidadEvaluaciones: 2,
            puntajeConsolidado: 80.00m,
            diferencia: 10.00m,
            umbral: 20.00m,
            alerta: false,
            revisada: true,
            comentarioRevision: "Revisión incoherente sin alerta activa",
            revisadoPorId: actorId,
            fechaRevision: ahora,
            nombreCheckEsperado: "ck_resultados_consolidados_revision_coherente");
    }

    [Fact]
    public async Task Debe_CompletarAmbosEnviosSinErrorYPersistirExactamenteUnaFilaDeterminista_Cuando_DosEvaluadoresEnvianDefinitivaSimultaneamenteConBarreraC5()
    {
        var (
            _,
            criterio,
            _,
            asignacion1,
            asignacion2,
            evaluador1Id,
            evaluador2Id,
            _,
            postulacionId,
            fechaBase) = await SembrarEscenarioDosEvaluadoresAsync();

        // Guardar primero ambos borradores completos (72.00 y 96.00 -> promedio 84.00, diferencia 24.00, alerta = true)
        await using (var contextoBorrador1 = _fixture.CrearContexto())
        {
            var guardar1 = CrearGuardarBorradorHandler(
                contextoBorrador1,
                evaluador1Id,
                new ProveedorTiempoFijo(fechaBase.AddMinutes(10)));
            await guardar1.HandleAsync(new GuardarBorradorEvaluacionCommand(
                asignacion1.Id,
                [new DatosCalificacionCriterioCommand(criterio.Id, 72.00m, "Criterio E1")],
                "Borrador listo E1"));
        }

        await using (var contextoBorrador2 = _fixture.CrearContexto())
        {
            var guardar2 = CrearGuardarBorradorHandler(
                contextoBorrador2,
                evaluador2Id,
                new ProveedorTiempoFijo(fechaBase.AddMinutes(11)));
            await guardar2.HandleAsync(new GuardarBorradorEvaluacionCommand(
                asignacion2.Id,
                [new DatosCalificacionCriterioCommand(criterio.Id, 96.00m, "Criterio E2")],
                "Borrador listo E2"));
        }

        // Barrera sincronizada justo después de que ambos evaluadores confirmen su evaluación definitiva
        // y antes de que ambos ejecuten la consulta/inserción del ResultadoConsolidadoRanking (concurrencia C5).
        var envioConfirmado1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var envioConfirmado2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var barreraPostEnvio = Task.WhenAll(envioConfirmado1.Task, envioConfirmado2.Task);

        var listoInsertarConsolidado1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listoInsertarConsolidado2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var barreraInsercionConsolidado = Task.WhenAll(listoInsertarConsolidado1.Task, listoInsertarConsolidado2.Task);

        async Task EjecutarEnvioSimultaneoAsync(
            Guid asignacionId,
            Guid evaluadorId,
            TaskCompletionSource senalEnvioPropio,
            TaskCompletionSource senalConsolidadoPropio)
        {
            await using var contexto = _fixture.CrearContexto();
            var timeProvider = new ProveedorTiempoFijo(fechaBase.AddMinutes(20));

            var uowConsolidacionConBarrera = new UnitOfWorkConBarreraPrimeraLlamada(
                contexto,
                senalConsolidadoPropio,
                barreraInsercionConsolidado);

            var consolidarHandler = new ConsolidarPostulacionCommandHandler(
                new UsuarioActualFijo(evaluadorId),
                new ResultadoConsolidadoRepository(contexto),
                new ConsultaDatosConsolidacionRepository(contexto),
                new ProveedorParametrosConsolidacionConfigurado(Options.Create(new ConsolidacionOptions())),
                uowConsolidacionConBarrera,
                new ConsolidarPostulacionCommandValidator(),
                timeProvider);

            var consolidadorConBarreraPostEnvio = new ConsolidadorConBarreraPrevia(
                new ConsolidadorPostulacionEvaluacionAdapter(consolidarHandler),
                senalEnvioPropio,
                barreraPostEnvio);

            var bloquearRubricaHandler = new BloquearRubricaCommandHandler(
                new RubricaRepository(contexto),
                contexto,
                timeProvider);

            var enviarHandler = new EnviarEvaluacionDefinitivaCommandHandler(
                new UsuarioActualFijo(evaluadorId),
                new ConsultaAsignacionParaEvaluacionRepository(contexto),
                new ConvocatoriaEvaluacionAbiertaFija(timeProvider),
                new EvaluacionRepository(contexto),
                new ComiteEvaluadorRepository(contexto),
                new ConsultaRubricaParaEvaluacionRepository(contexto),
                new BloqueadorRubricaEvaluacionAdapter(bloquearRubricaHandler),
                consolidadorConBarreraPostEnvio,
                contexto,
                new EnviarEvaluacionDefinitivaCommandValidator(),
                timeProvider);

            var dto = await enviarHandler.HandleAsync(new EnviarEvaluacionDefinitivaCommand(asignacionId));
            dto.Estado.Should().Be("ENVIADA_DEFINITIVA");
        }

        var tarea1 = Task.Run(() => EjecutarEnvioSimultaneoAsync(
            asignacion1.Id,
            evaluador1Id,
            envioConfirmado1,
            listoInsertarConsolidado1));
        var tarea2 = Task.Run(() => EjecutarEnvioSimultaneoAsync(
            asignacion2.Id,
            evaluador2Id,
            envioConfirmado2,
            listoInsertarConsolidado2));

        // Ambos envíos deben terminar sin lanzar ninguna excepción al cliente
        await Task.WhenAll(tarea1, tarea2);

        await using var contextoVerificacion = _fixture.CrearContexto();
        var filas = await contextoVerificacion.ResultadosConsolidadosRanking
            .AsNoTracking()
            .Where(r => r.PostulacionId == postulacionId)
            .ToListAsync();

        filas.Should().ContainSingle();
        var consolidado = filas[0];
        consolidado.CantidadEvaluacionesValidas.Should().Be(2);
        consolidado.PuntajeConsolidado.Should().Be(84.00m);
        consolidado.DiferenciaEntreEvaluadores.Should().Be(24.00m);
        consolidado.UmbralDiscrepanciaAplicado.Should().Be(20.00m);
        consolidado.AlertaDiscrepancia.Should().BeTrue();
        consolidado.DiscrepanciaRevisada.Should().BeFalse();
        consolidado.ExcepcionEvaluadorUnico.Should().BeFalse();
        consolidado.EstadoConfirmacionRanking.Should().Be(EstadoRanking.Provisional);
    }

    [Fact]
    public async Task Debe_Lanzar409ConflictoConcurrenciaEnSegundaRevisionSimultanea_Cuando_DosCoordinadoresRevisanLaMismaDiscrepancia()
    {
        var convocatoriaId = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();
        var coordinador1Id = Guid.CreateVersion7();
        var coordinador2Id = Guid.CreateVersion7();
        var ahora = new DateTimeOffset(2026, 11, 26, 17, 0, 0, TimeSpan.Zero);

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            var repoSemilla = new ResultadoConsolidadoRepository(contextoSemilla);
            var resultadoInicial = ResultadoConsolidadoRanking.Crear(
                new ResultadoCalculoConsolidacion(
                    convocatoriaId,
                    postulacionId,
                    excepcionEvaluadorUnico: false,
                    justificacionEvaluadorUnico: null,
                    autorizadorExcepcionId: null,
                    cantidadEvaluacionesValidas: 2,
                    puntajeConsolidado: 75.00m,
                    diferenciaEntreEvaluadores: 30.00m,
                    umbralDiscrepanciaAplicado: 20.00m,
                    alertaDiscrepancia: true),
                ahora,
                coordinador1Id);

            await repoSemilla.AgregarAsync(resultadoInicial);
            await contextoSemilla.GuardarCambiosAsync();
        }

        await using var contextoA = _fixture.CrearContexto();
        await using var contextoB = _fixture.CrearContexto();

        var repoA = new ResultadoConsolidadoRepository(contextoA);
        var repoB = new ResultadoConsolidadoRepository(contextoB);

        var resultadoA = (await repoA.ObtenerParaActualizarPorPostulacionAsync(postulacionId))!;
        var resultadoB = (await repoB.ObtenerParaActualizarPorPostulacionAsync(postulacionId))!;

        resultadoA.MarcarDiscrepanciaRevisada(
            "Revisión ganadora del coordinador A confirmada primero.",
            ahora.AddMinutes(5),
            coordinador1Id);

        resultadoB.MarcarDiscrepanciaRevisada(
            "Revisión concurrente del coordinador B con versión obsoleta.",
            ahora.AddMinutes(6),
            coordinador2Id);

        await contextoA.GuardarCambiosAsync();

        var guardarB = () => contextoB.GuardarCambiosAsync();
        var exConcurrencia = (await guardarB.Should().ThrowAsync<ExcepcionConflictoConcurrencia>()).Which;
        exConcurrencia.Codigo.Should().Be(ExcepcionConflictoConcurrencia.CodigoError);
        exConcurrencia.EstadoHttp.Should().Be(409);

        await using var contextoVerificar = _fixture.CrearContexto();
        var persistido = (await new ResultadoConsolidadoRepository(contextoVerificar)
            .ObtenerPorPostulacionAsync(postulacionId))!;

        persistido.Version.Should().Be(2);
        persistido.DiscrepanciaRevisada.Should().BeTrue();
        persistido.ComentarioRevisionDiscrepancia.Should().Be(
            "Revisión ganadora del coordinador A confirmada primero.");
        persistido.RevisadoPorUsuarioId.Should().Be(coordinador1Id);
    }

    private async Task GuardarYEnviarEvaluacionAsync(
        Guid asignacionId,
        Guid criterioId,
        decimal puntaje,
        Guid evaluadorId,
        DateTimeOffset instante)
    {
        await using (var contextoGuardar = _fixture.CrearContexto())
        {
            var guardarHandler = CrearGuardarBorradorHandler(
                contextoGuardar,
                evaluadorId,
                new ProveedorTiempoFijo(instante));

            await guardarHandler.HandleAsync(new GuardarBorradorEvaluacionCommand(
                asignacionId,
                [new DatosCalificacionCriterioCommand(criterioId, puntaje, "Calificación registrada")],
                "Evaluación completa"));
        }

        await using (var contextoEnviar = _fixture.CrearContexto())
        {
            var enviarHandler = CrearEnviarDefinitivaConConsolidacionRealHandler(
                contextoEnviar,
                evaluadorId,
                new ProveedorTiempoFijo(instante.AddMinutes(1)));

            await enviarHandler.HandleAsync(new EnviarEvaluacionDefinitivaCommand(asignacionId));
        }
    }

    private static GuardarBorradorEvaluacionCommandHandler CrearGuardarBorradorHandler(
        PostulacionesDbContext contexto,
        Guid usuarioEvaluadorId,
        TimeProvider timeProvider)
    {
        var bloquearRubricaHandler = new BloquearRubricaCommandHandler(
            new RubricaRepository(contexto),
            contexto,
            timeProvider);

        return new GuardarBorradorEvaluacionCommandHandler(
            new UsuarioActualFijo(usuarioEvaluadorId),
            new ConsultaAsignacionParaEvaluacionRepository(contexto),
            new ConvocatoriaEvaluacionAbiertaFija(timeProvider),
            new ComiteEvaluadorRepository(contexto),
            new ConsultaRubricaParaEvaluacionRepository(contexto),
            new EvaluacionRepository(contexto),
            new BloqueadorRubricaEvaluacionAdapter(bloquearRubricaHandler),
            contexto,
            new GuardarBorradorEvaluacionCommandValidator(),
            timeProvider);
    }

    private static EnviarEvaluacionDefinitivaCommandHandler CrearEnviarDefinitivaConConsolidacionRealHandler(
        PostulacionesDbContext contexto,
        Guid usuarioEvaluadorId,
        TimeProvider timeProvider)
    {
        var bloquearRubricaHandler = new BloquearRubricaCommandHandler(
            new RubricaRepository(contexto),
            contexto,
            timeProvider);

        var consolidarHandler = new ConsolidarPostulacionCommandHandler(
            new UsuarioActualFijo(usuarioEvaluadorId),
            new ResultadoConsolidadoRepository(contexto),
            new ConsultaDatosConsolidacionRepository(contexto),
            new ProveedorParametrosConsolidacionConfigurado(Options.Create(new ConsolidacionOptions())),
            contexto,
            new ConsolidarPostulacionCommandValidator(),
            timeProvider);

        return new EnviarEvaluacionDefinitivaCommandHandler(
            new UsuarioActualFijo(usuarioEvaluadorId),
            new ConsultaAsignacionParaEvaluacionRepository(contexto),
            new ConvocatoriaEvaluacionAbiertaFija(timeProvider),
            new EvaluacionRepository(contexto),
            new ComiteEvaluadorRepository(contexto),
            new ConsultaRubricaParaEvaluacionRepository(contexto),
            new BloqueadorRubricaEvaluacionAdapter(bloquearRubricaHandler),
            new ConsolidadorPostulacionEvaluacionAdapter(consolidarHandler),
            contexto,
            new EnviarEvaluacionDefinitivaCommandValidator(),
            timeProvider);
    }

    private static AutorizarExcepcionEvaluadorUnicoCommandHandler CrearAutorizarExcepcionHandler(
        PostulacionesDbContext contexto,
        Guid coordinadorId,
        TimeProvider timeProvider)
    {
        var consolidarHandler = new ConsolidarPostulacionCommandHandler(
            new UsuarioActualFijo(coordinadorId),
            new ResultadoConsolidadoRepository(contexto),
            new ConsultaDatosConsolidacionRepository(contexto),
            new ProveedorParametrosConsolidacionConfigurado(Options.Create(new ConsolidacionOptions())),
            contexto,
            new ConsolidarPostulacionCommandValidator(),
            timeProvider);

        return new AutorizarExcepcionEvaluadorUnicoCommandHandler(
            new UsuarioActualFijo(coordinadorId),
            new ExcepcionEvaluadorUnicoRepository(contexto),
            new AsignacionEvaluacionRepository(contexto),
            new ConsolidadorPostulacionEvaluacionAdapter(consolidarHandler),
            contexto,
            new AutorizarExcepcionEvaluadorUnicoCommandValidator(),
            timeProvider);
    }

    private sealed class ConsolidadorConBarreraPrevia(
        Application.Consolidacion.Interfaces.IConsolidadorPostulacionEvaluacion interno,
        TaskCompletionSource listoPropio,
        Task barrera)
        : Application.Consolidacion.Interfaces.IConsolidadorPostulacionEvaluacion
    {
        public async Task IntentarConsolidarAutomaticamenteAsync(
            Guid postulacionId,
            CancellationToken cancellationToken = default)
        {
            listoPropio.TrySetResult();
            await barrera.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            await interno.IntentarConsolidarAutomaticamenteAsync(postulacionId, cancellationToken);
        }
    }

    private sealed class UnitOfWorkConBarreraPrimeraLlamada(
        IUnitOfWork interno,
        TaskCompletionSource listoPropio,
        Task barrera) : IUnitOfWork
    {
        private int _llamadas;

        public async Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _llamadas) == 1)
            {
                listoPropio.TrySetResult();
                await barrera.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }

            return await interno.GuardarCambiosAsync(cancellationToken);
        }
    }

    private sealed class ConvocatoriaEvaluacionAbiertaFija(TimeProvider timeProvider)
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
                    EstadoConvocatoriaExterna.Publicada,
                    ahora.AddDays(-10),
                    ahora.AddDays(30)));
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
