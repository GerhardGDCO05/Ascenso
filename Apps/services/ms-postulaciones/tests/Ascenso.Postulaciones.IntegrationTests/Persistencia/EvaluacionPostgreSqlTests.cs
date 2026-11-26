using Ascenso.Postulaciones.Application.Consolidacion.Interfaces;
using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.EnviarEvaluacionDefinitiva;
using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.GuardarBorradorEvaluacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Servicios;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.BloquearRubrica;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class EvaluacionPostgreSqlTests
{
    private readonly PostgreSqlFixture _fixture;

    public EvaluacionPostgreSqlTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(
        RubricaEvaluacion Rubrica,
        CriterioEvaluacion Criterio1,
        CriterioEvaluacion Criterio2,
        CriterioEvaluacion Criterio3,
        ComiteEvaluador Comite,
        AsignacionEvaluacion Asignacion1,
        AsignacionEvaluacion Asignacion2,
        Guid Evaluador1Id,
        Guid Evaluador2Id,
        Guid PostulacionId,
        DateTimeOffset FechaBase)> SembrarEscenarioAsync()
    {
        var programaBecaId = Guid.CreateVersion7();
        var convocatoriaId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var evaluador1Id = Guid.CreateVersion7();
        var evaluador2Id = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();
        var fechaBase = new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);

        var rubrica = RubricaEvaluacion.Crear(
            programaBecaId,
            convocatoriaId,
            "Rúbrica Evaluación PostgreSQL 2026",
            fechaBase,
            coordinadorId);
        var criterio1 = rubrica.AgregarCriterio(
            "Promedio académico",
            "Rendimiento histórico acumulado",
            40.00m,
            0.00m,
            20.00m,
            fechaBase.AddMinutes(1),
            coordinadorId);
        var criterio2 = rubrica.AgregarCriterio(
            "Vulnerabilidad socioeconómica",
            "Evaluación socioeconómica verificada",
            30.00m,
            0.00m,
            10.00m,
            fechaBase.AddMinutes(2),
            coordinadorId);
        var criterio3 = rubrica.AgregarCriterio(
            "Liderazgo y compromiso",
            "Participación comunitaria y proyección",
            30.00m,
            1.00m,
            5.00m,
            fechaBase.AddMinutes(3),
            coordinadorId);
        rubrica.Publicar(fechaBase.AddMinutes(4), coordinadorId);

        var comite = ComiteEvaluador.Crear(
            programaBecaId,
            convocatoriaId,
            "Comité Evaluaciones PostgreSQL 2026",
            evaluador1Id,
            fechaBase.AddMinutes(5),
            coordinadorId);
        var miembro1 = comite.Miembros.Single();
        var miembro2 = comite.AgregarMiembro(
            evaluador2Id,
            TipoEvaluador.Externo,
            fechaBase.AddMinutes(6),
            coordinadorId);

        var asignacion1 = AsignacionEvaluacion.Crear(
            comite,
            miembro1.Id,
            postulacionId,
            fechaBase.AddMinutes(10),
            fechaBase.AddDays(7),
            coordinadorId);
        var asignacion2 = AsignacionEvaluacion.Crear(
            comite,
            miembro2.Id,
            postulacionId,
            fechaBase.AddMinutes(11),
            fechaBase.AddDays(7),
            coordinadorId);

        await using var contextoSemilla = _fixture.CrearContexto();
        contextoSemilla.Rubricas.Add(rubrica);
        contextoSemilla.ComitesEvaluadores.Add(comite);
        contextoSemilla.AsignacionesEvaluacion.AddRange(asignacion1, asignacion2);
        await contextoSemilla.GuardarCambiosAsync();

        return (
            rubrica,
            criterio1,
            criterio2,
            criterio3,
            comite,
            asignacion1,
            asignacion2,
            evaluador1Id,
            evaluador2Id,
            postulacionId,
            fechaBase);
    }

    [Fact]
    public async Task Debe_PersistirRecargarActualizarBorradorEnviarDefinitivaYBloquearRubrica_Cuando_SeOperaEnPostgreSqlReal()
    {
        var (
            rubrica,
            criterio1,
            criterio2,
            criterio3,
            _,
            asignacion1,
            asignacion2,
            evaluador1Id,
            evaluador2Id,
            postulacionId,
            fechaBase) = await SembrarEscenarioAsync();

        var instanteBorrador1 = new DateTimeOffset(2026, 11, 25, 15, 30, 0, TimeSpan.FromHours(-4));
        Guid evaluacionId;
        Guid calificacionC2EliminadaId;

        // 1. Guardar primer borrador (C1 y C2) -> bloquea rúbrica PUBLICADA -> BLOQUEADA y pasa asignación a EN_PROGRESO
        await using (var contextoBorrador1 = _fixture.CrearContexto())
        {
            var handler = CrearGuardarBorradorHandler(
                contextoBorrador1,
                evaluador1Id,
                new ProveedorTiempoFijo(instanteBorrador1));

            var resultado = await handler.HandleAsync(new GuardarBorradorEvaluacionCommand(
                asignacion1.Id,
                [
                    new DatosCalificacionCriterioCommand(criterio1.Id, 15.25m, "Promedio inicial"),
                    new DatosCalificacionCriterioCommand(criterio2.Id, 8.50m, null)
                ],
                "Comentarios iniciales del borrador"));

            resultado.EsCreacion.Should().BeTrue();
            resultado.Evaluacion.Estado.Should().Be("BORRADOR");
            resultado.Evaluacion.PuntajeTotalPonderado.Should().BeNull();
            resultado.Evaluacion.FechaEnvio.Should().BeNull();
            evaluacionId = resultado.Evaluacion.Id;
        }

        // 2. Recargar desde BD y verificar tipos timestamptz UTC, numeric(7,2), transición de asignación y bloqueo de rúbrica
        await using (var contextoVerificar1 = _fixture.CrearContexto())
        {
            var repoEval = new EvaluacionRepository(contextoVerificar1);
            var repoAsignacion = new ConsultaAsignacionParaEvaluacionRepository(contextoVerificar1);
            var repoRubrica = new RubricaRepository(contextoVerificar1);

            var evaluacionLeida = await repoEval.ObtenerPorAsignacionAsync(asignacion1.Id);
            evaluacionLeida.Should().NotBeNull();
            evaluacionLeida!.Id.Should().Be(evaluacionId);
            evaluacionLeida.Estado.Should().Be(EstadoEvaluacion.Borrador);
            evaluacionLeida.FechaInicio.Should().Be(instanteBorrador1.ToUniversalTime());
            evaluacionLeida.FechaInicio.Offset.Should().Be(TimeSpan.Zero);
            evaluacionLeida.FechaCreacion.Offset.Should().Be(TimeSpan.Zero);
            evaluacionLeida.FechaUltimaModificacion.Offset.Should().Be(TimeSpan.Zero);
            evaluacionLeida.FechaEnvio.Should().BeNull();
            evaluacionLeida.PuntajeTotalPonderado.Should().BeNull();
            evaluacionLeida.ComentariosGenerales.Should().Be("Comentarios iniciales del borrador");
            evaluacionLeida.Version.Should().Be(1);
            evaluacionLeida.Calificaciones.Should().HaveCount(2);

            var calC1 = evaluacionLeida.Calificaciones.Single(c => c.CriterioEvaluacionId == criterio1.Id);
            calC1.PuntajeAsignado.Should().Be(15.25m);
            calC1.Comentario.Should().Be("Promedio inicial");
            calC1.Version.Should().Be(1);

            var calC2 = evaluacionLeida.Calificaciones.Single(c => c.CriterioEvaluacionId == criterio2.Id);
            calC2.PuntajeAsignado.Should().Be(8.50m);
            calC2.Comentario.Should().BeNull();
            calificacionC2EliminadaId = calC2.Id;

            var asignacionLeida = await repoAsignacion.ObtenerPorIdAsync(asignacion1.Id);
            asignacionLeida.Should().NotBeNull();
            asignacionLeida!.Estado.Should().Be(EstadoAsignacionEvaluacion.EnProgreso);
            asignacionLeida.Version.Should().Be(2);

            var rubricaLeida = await repoRubrica.ObtenerPorIdAsync(rubrica.Id);
            rubricaLeida.Should().NotBeNull();
            rubricaLeida!.Estado.Should().Be(EstadoRubrica.Bloqueada);
            rubricaLeida.MotivoBloqueo.Should().Be(MotivoBloqueoRubrica.PrimeraCalificacion);
            rubricaLeida.FechaBloqueo.Should().Be(instanteBorrador1.ToUniversalTime());
        }

        // 3. Actualizar borrador: editar C1, remover C2 y agregar C3
        var instanteBorrador2 = instanteBorrador1.AddMinutes(20);
        await using (var contextoBorrador2 = _fixture.CrearContexto())
        {
            var handler = CrearGuardarBorradorHandler(
                contextoBorrador2,
                evaluador1Id,
                new ProveedorTiempoFijo(instanteBorrador2));

            var resultado = await handler.HandleAsync(new GuardarBorradorEvaluacionCommand(
                asignacion1.Id,
                [
                    new DatosCalificacionCriterioCommand(criterio1.Id, 18.00m, "Promedio verificado"),
                    new DatosCalificacionCriterioCommand(criterio3.Id, 4.00m, "Liderazgo destacado")
                ],
                "Comentarios actualizados"));

            resultado.EsCreacion.Should().BeFalse();
            resultado.Evaluacion.Version.Should().Be(2);
            resultado.Evaluacion.Calificaciones.Should().HaveCount(2);
        }

        await using (var contextoVerificar2 = _fixture.CrearContexto())
        {
            var repoEval = new EvaluacionRepository(contextoVerificar2);
            var evaluacionLeida = (await repoEval.ObtenerPorAsignacionAsync(asignacion1.Id))!;

            evaluacionLeida.Version.Should().Be(2);
            evaluacionLeida.ComentariosGenerales.Should().Be("Comentarios actualizados");
            evaluacionLeida.Calificaciones.Select(c => c.CriterioEvaluacionId)
                .Should().BeEquivalentTo([criterio1.Id, criterio3.Id]);

            var calC1 = evaluacionLeida.Calificaciones.Single(c => c.CriterioEvaluacionId == criterio1.Id);
            calC1.PuntajeAsignado.Should().Be(18.00m);
            calC1.Comentario.Should().Be("Promedio verificado");
            calC1.Version.Should().Be(2);

            var calC3 = evaluacionLeida.Calificaciones.Single(c => c.CriterioEvaluacionId == criterio3.Id);
            calC3.PuntajeAsignado.Should().Be(4.00m);
            calC3.Comentario.Should().Be("Liderazgo destacado");
            calC3.Version.Should().Be(1);

            var existeC2Eliminado = await contextoVerificar2.CalificacionesCriterio
                .AnyAsync(c => c.Id == calificacionC2EliminadaId);
            existeC2Eliminado.Should().BeFalse();
        }

        // 4. Completar los 3 criterios en borrador y enviar evaluación definitiva (además evaluador2 deja un borrador)
        var instanteBorrador3 = instanteBorrador2.AddMinutes(10);
        await using (var contextoCompletarYEnviar = _fixture.CrearContexto())
        {
            var guardarHandler1 = CrearGuardarBorradorHandler(
                contextoCompletarYEnviar,
                evaluador1Id,
                new ProveedorTiempoFijo(instanteBorrador3));

            await guardarHandler1.HandleAsync(new GuardarBorradorEvaluacionCommand(
                asignacion1.Id,
                [
                    new DatosCalificacionCriterioCommand(criterio1.Id, 18.00m, "Promedio verificado"),
                    new DatosCalificacionCriterioCommand(criterio2.Id, 9.00m, "Vulnerabilidad alta"),
                    new DatosCalificacionCriterioCommand(criterio3.Id, 4.00m, "Liderazgo destacado")
                ],
                "Evaluación completa lista para envío"));

            var guardarHandler2 = CrearGuardarBorradorHandler(
                contextoCompletarYEnviar,
                evaluador2Id,
                new ProveedorTiempoFijo(instanteBorrador3.AddMinutes(1)));

            await guardarHandler2.HandleAsync(new GuardarBorradorEvaluacionCommand(
                asignacion2.Id,
                [
                    new DatosCalificacionCriterioCommand(criterio1.Id, 14.00m, "Borrador evaluador 2")
                ],
                "Borrador en curso evaluador 2"));
        }

        var instanteEnvio = instanteBorrador3.AddMinutes(5);
        await using (var contextoEnviar = _fixture.CrearContexto())
        {
            var enviarHandler = CrearEnviarDefinitivaHandler(
                contextoEnviar,
                evaluador1Id,
                new ProveedorTiempoFijo(instanteEnvio));

            var dtoEnviada = await enviarHandler.HandleAsync(
                new EnviarEvaluacionDefinitivaCommand(asignacion1.Id));

            dtoEnviada.Estado.Should().Be("ENVIADA_DEFINITIVA");
            dtoEnviada.PuntajeTotalPonderado.Should().Be(85.50m);
            dtoEnviada.FechaEnvio.Should().Be(instanteEnvio.ToUniversalTime());
        }

        // 5. Verificar estado final en BD, transición EN_PROGRESO -> COMPLETADA y consultas por evaluador y por postulación
        await using (var contextoLecturaFinal = _fixture.CrearContexto())
        {
            var repoEval = new EvaluacionRepository(contextoLecturaFinal);
            var repoAsignacion = new ConsultaAsignacionParaEvaluacionRepository(contextoLecturaFinal);

            var evaluacionFinal = (await repoEval.ObtenerPorAsignacionAsync(asignacion1.Id))!;
            evaluacionFinal.Estado.Should().Be(EstadoEvaluacion.EnviadaDefinitiva);
            evaluacionFinal.PuntajeTotalPonderado.Should().Be(85.50m);
            evaluacionFinal.FechaEnvio.Should().Be(instanteEnvio.ToUniversalTime());
            evaluacionFinal.FechaEnvio!.Value.Offset.Should().Be(TimeSpan.Zero);
            evaluacionFinal.Version.Should().Be(4);
            evaluacionFinal.Calificaciones.Should().HaveCount(3);

            var asignacionFinal = (await repoAsignacion.ObtenerPorIdAsync(asignacion1.Id))!;
            asignacionFinal.Estado.Should().Be(EstadoAsignacionEvaluacion.Completada);
            asignacionFinal.Version.Should().Be(3);

            var paginaEvaluador1 = await repoEval.ListarPorEvaluadorAsync(
                evaluador1Id,
                EstadoEvaluacion.EnviadaDefinitiva,
                pagina: 1,
                tamano: 10);
            paginaEvaluador1.Total.Should().Be(1);
            paginaEvaluador1.Items.Should().ContainSingle(e => e.Id == evaluacionId);

            // ListarDefinitivasPorPostulacionAsync devuelve solo la definitiva de evaluador1 y excluye el borrador de evaluador2
            var definitivasPostulacion = await repoEval.ListarDefinitivasPorPostulacionAsync(postulacionId);
            definitivasPostulacion.Should().ContainSingle();
            definitivasPostulacion[0].Id.Should().Be(evaluacionId);
            definitivasPostulacion[0].EvaluadorId.Should().Be(evaluador1Id);
            definitivasPostulacion[0].PuntajeTotalPonderado.Should().Be(85.50m);
        }
    }

    [Fact]
    public async Task Debe_RechazarViolacionesDeIndicesUnicosChecksYFksRestrict_Cuando_SeEvaluaEnPostgreSqlReal()
    {
        var (
            rubrica,
            criterio1,
            criterio2,
            _,
            _,
            asignacion1,
            _,
            evaluador1Id,
            _,
            _,
            fechaBase) = await SembrarEscenarioAsync();

        var ahora = fechaBase.AddMinutes(30);
        Guid evaluacionId;
        Guid calificacion1Id;
        Guid calificacion2Id;

        await using (var contextoCrear = _fixture.CrearContexto())
        {
            var evaluacion = Evaluacion.CrearBorrador(
                asignacion1,
                rubrica,
                [
                    new DatosCalificacionCriterio(criterio1.Id, 16.00m, "Comentario C1"),
                    new DatosCalificacionCriterio(criterio2.Id, 8.00m, "Comentario C2")
                ],
                "Comentarios generales",
                ahora,
                evaluador1Id);

            evaluacionId = evaluacion.Id;
            calificacion1Id = evaluacion.Calificaciones.Single(c => c.CriterioEvaluacionId == criterio1.Id).Id;
            calificacion2Id = evaluacion.Calificaciones.Single(c => c.CriterioEvaluacionId == criterio2.Id).Id;

            contextoCrear.Evaluaciones.Add(evaluacion);
            await contextoCrear.GuardarCambiosAsync();
        }

        // 1. ux_evaluaciones_asignacion_evaluacion -> EvaluacionDuplicadaException (409 EVALUACION_DUPLICADA)
        await using (var contextoDuplicado = _fixture.CrearContexto())
        {
            var duplicada = Evaluacion.CrearBorrador(
                asignacion1,
                rubrica,
                [new DatosCalificacionCriterio(criterio1.Id, 12.00m, null)],
                null,
                ahora.AddMinutes(1),
                evaluador1Id);

            contextoDuplicado.Evaluaciones.Add(duplicada);
            var guardarDuplicada = () => contextoDuplicado.GuardarCambiosAsync();

            var exDuplicada = (await guardarDuplicada.Should().ThrowAsync<EvaluacionDuplicadaException>()).Which;
            exDuplicada.Codigo.Should().Be(EvaluacionDuplicadaException.CodigoError);
            exDuplicada.EstadoHttp.Should().Be(409);
        }

        await using var contexto = _fixture.CrearContexto();

        // 2. ux_calificaciones_criterio_evaluacion_criterio
        var criterioDuplicadoSql = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.calificaciones_criterio
            SET criterio_evaluacion_id = {criterio1.Id}
            WHERE id = {calificacion2Id}
            """);
        var exCriterioUnico = (await criterioDuplicadoSql.Should().ThrowAsync<PostgresException>()).Which;
        exCriterioUnico.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        exCriterioUnico.ConstraintName.Should().Be("ux_calificaciones_criterio_evaluacion_criterio");

        // 3. ck_evaluaciones_estado_valido (PostgreSQL evalúa checks en orden alfabético: ck_evaluaciones_envio_coherente o ck_evaluaciones_estado_valido)
        var estadoInvalidoSql = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.evaluaciones
            SET estado = 'ANULADA'
            WHERE id = {evaluacionId}
            """);
        var exEstado = (await estadoInvalidoSql.Should().ThrowAsync<PostgresException>()).Which;
        exEstado.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exEstado.ConstraintName.Should().BeOneOf(
            "ck_evaluaciones_estado_valido",
            "ck_evaluaciones_envio_coherente");

        // 4. ck_evaluaciones_puntaje_total_rango (> 100)
        var puntajeTotalFueraRangoSql = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.evaluaciones
            SET estado = 'ENVIADA_DEFINITIVA',
                fecha_envio = {ahora},
                puntaje_total_ponderado = 100.01
            WHERE id = {evaluacionId}
            """);
        var exPuntajeTotal = (await puntajeTotalFueraRangoSql.Should().ThrowAsync<PostgresException>()).Which;
        exPuntajeTotal.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exPuntajeTotal.ConstraintName.Should().Be("ck_evaluaciones_puntaje_total_rango");

        // 5. ck_evaluaciones_envio_coherente (BORRADOR con puntaje_total_ponderado no nulo)
        var borradorConPuntajeSql = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.evaluaciones
            SET puntaje_total_ponderado = 80.00
            WHERE id = {evaluacionId}
            """);
        var exCoherencia1 = (await borradorConPuntajeSql.Should().ThrowAsync<PostgresException>()).Which;
        exCoherencia1.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exCoherencia1.ConstraintName.Should().Be("ck_evaluaciones_envio_coherente");

        // 6. ck_evaluaciones_envio_coherente (ENVIADA_DEFINITIVA sin fecha_envio)
        var enviadaSinFechaSql = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.evaluaciones
            SET estado = 'ENVIADA_DEFINITIVA',
                fecha_envio = NULL,
                puntaje_total_ponderado = 80.00
            WHERE id = {evaluacionId}
            """);
        var exCoherencia2 = (await enviadaSinFechaSql.Should().ThrowAsync<PostgresException>()).Which;
        exCoherencia2.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exCoherencia2.ConstraintName.Should().Be("ck_evaluaciones_envio_coherente");

        // 7. ck_evaluaciones_envio_coherente (INVALIDADA_POR_CONFLICTO sin motivo)
        var invalidadaSinMotivoSql = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.evaluaciones
            SET estado = 'INVALIDADA_POR_CONFLICTO',
                motivo_invalidacion = '   '
            WHERE id = {evaluacionId}
            """);
        var exCoherencia3 = (await invalidadaSinMotivoSql.Should().ThrowAsync<PostgresException>()).Which;
        exCoherencia3.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exCoherencia3.ConstraintName.Should().Be("ck_evaluaciones_envio_coherente");

        // 8. ck_evaluaciones_comentarios_generales_no_vacios
        var comentariosVaciosSql = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.evaluaciones
            SET comentarios_generales = '   '
            WHERE id = {evaluacionId}
            """);
        var exComentarios = (await comentariosVaciosSql.Should().ThrowAsync<PostgresException>()).Which;
        exComentarios.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exComentarios.ConstraintName.Should().Be("ck_evaluaciones_comentarios_generales_no_vacios");

        // 9. ck_calificaciones_criterio_puntaje_rango (< 0 o > 1000)
        var puntajeCriterioNegativoSql = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.calificaciones_criterio
            SET puntaje_asignado = -0.01
            WHERE id = {calificacion1Id}
            """);
        var exRangoCriterio = (await puntajeCriterioNegativoSql.Should().ThrowAsync<PostgresException>()).Which;
        exRangoCriterio.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exRangoCriterio.ConstraintName.Should().Be("ck_calificaciones_criterio_puntaje_rango");

        // 10. ck_calificaciones_criterio_comentario_no_vacio
        var comentarioCriterioVacioSql = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.calificaciones_criterio
            SET comentario = '   '
            WHERE id = {calificacion1Id}
            """);
        var exComentarioCriterio = (await comentarioCriterioVacioSql.Should().ThrowAsync<PostgresException>()).Which;
        exComentarioCriterio.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exComentarioCriterio.ConstraintName.Should().Be("ck_calificaciones_criterio_comentario_no_vacio");

        // 11. FK RESTRICT: no permitir borrar asignación, rúbrica ni criterio referenciados
        var borrarAsignacionSql = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM postulaciones.asignaciones_evaluacion
            WHERE id = {asignacion1.Id}
            """);
        var exFkAsignacion = (await borrarAsignacionSql.Should().ThrowAsync<PostgresException>()).Which;
        exFkAsignacion.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        exFkAsignacion.ConstraintName.Should().Be("fk_evaluaciones_asignaciones_evaluacion");

        var borrarRubricaSql = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM postulaciones.rubricas
            WHERE id = {rubrica.Id}
            """);
        var exFkRubrica = (await borrarRubricaSql.Should().ThrowAsync<PostgresException>()).Which;
        exFkRubrica.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        exFkRubrica.ConstraintName.Should().Be("fk_evaluaciones_rubricas");

        var borrarCriterioSql = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM postulaciones.criterios_evaluacion
            WHERE id = {criterio1.Id}
            """);
        var exFkCriterio = (await borrarCriterioSql.Should().ThrowAsync<PostgresException>()).Which;
        exFkCriterio.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        exFkCriterio.ConstraintName.Should().Be("fk_calificaciones_criterio_criterios_evaluacion");
    }

    [Fact]
    public async Task Debe_GuardarAmbosBorradoresYBloquearRubricaUnaSolaVez_Cuando_DosEvaluadoresCalificanLaMismaPostulacionEnSimultaneoC5()
    {
        var (
            rubrica,
            criterio1,
            criterio2,
            _,
            _,
            asignacion1,
            asignacion2,
            evaluador1Id,
            evaluador2Id,
            _,
            fechaBase) = await SembrarEscenarioAsync();

        var versionRubricaPublicada = rubrica.Version;
        var instanteE1 = fechaBase.AddHours(1);
        var instanteE2 = fechaBase.AddHours(1).AddSeconds(2);

        var listo1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listo2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var barrera = Task.WhenAll(listo1.Task, listo2.Task);

        await using var contexto1 = _fixture.CrearContexto();
        await using var contexto2 = _fixture.CrearContexto();

        var uow1 = new UnitOfWorkConBarreraPrimeraLlamada(contexto1, listo1, barrera);
        var uow2 = new UnitOfWorkConBarreraPrimeraLlamada(contexto2, listo2, barrera);

        var handler1 = CrearGuardarBorradorHandler(
            contexto1,
            evaluador1Id,
            new ProveedorTiempoFijo(instanteE1),
            uow1);
        var handler2 = CrearGuardarBorradorHandler(
            contexto2,
            evaluador2Id,
            new ProveedorTiempoFijo(instanteE2),
            uow2);

        var resultados = await Task.WhenAll(
            Task.Run(() => handler1.HandleAsync(new GuardarBorradorEvaluacionCommand(
                asignacion1.Id,
                [new DatosCalificacionCriterioCommand(criterio1.Id, 17.50m, "Calificación E1")],
                "Borrador Evaluador 1"))),
            Task.Run(() => handler2.HandleAsync(new GuardarBorradorEvaluacionCommand(
                asignacion2.Id,
                [new DatosCalificacionCriterioCommand(criterio2.Id, 9.25m, "Calificación E2")],
                "Borrador Evaluador 2"))));

        resultados.Should().HaveCount(2);
        resultados[0].EsCreacion.Should().BeTrue();
        resultados[1].EsCreacion.Should().BeTrue();
        resultados[0].Evaluacion.Estado.Should().Be("BORRADOR");
        resultados[1].Evaluacion.Estado.Should().Be("BORRADOR");

        await using var contextoVerificacion = _fixture.CrearContexto();
        var repoEval = new EvaluacionRepository(contextoVerificacion);
        var repoAsignacion = new ConsultaAsignacionParaEvaluacionRepository(contextoVerificacion);
        var repoRubrica = new RubricaRepository(contextoVerificacion);

        var eval1 = await repoEval.ObtenerPorAsignacionAsync(asignacion1.Id);
        var eval2 = await repoEval.ObtenerPorAsignacionAsync(asignacion2.Id);
        eval1.Should().NotBeNull();
        eval2.Should().NotBeNull();
        eval1!.Id.Should().NotBe(eval2!.Id);

        var asig1 = await repoAsignacion.ObtenerPorIdAsync(asignacion1.Id);
        var asig2 = await repoAsignacion.ObtenerPorIdAsync(asignacion2.Id);
        asig1!.Estado.Should().Be(EstadoAsignacionEvaluacion.EnProgreso);
        asig2!.Estado.Should().Be(EstadoAsignacionEvaluacion.EnProgreso);

        var rubricaFinal = await repoRubrica.ObtenerPorIdAsync(rubrica.Id);
        rubricaFinal.Should().NotBeNull();
        rubricaFinal!.Estado.Should().Be(EstadoRubrica.Bloqueada);
        rubricaFinal.MotivoBloqueo.Should().Be(MotivoBloqueoRubrica.PrimeraCalificacion);
        rubricaFinal.Version.Should().Be(versionRubricaPublicada + 1);
    }

    [Fact]
    public async Task Debe_ConfirmarUnBorradorYLanzarEvaluacionDuplicadaEnElOtro_Cuando_DosBorradoresSimultaneosCompitenPorLaMismaAsignacion()
    {
        var (
            rubrica,
            criterio1,
            _,
            _,
            _,
            asignacion1,
            _,
            evaluador1Id,
            _,
            _,
            fechaBase) = await SembrarEscenarioAsync();

        var ahora = fechaBase.AddHours(2);
        var listo1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listo2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var barrera = Task.WhenAll(listo1.Task, listo2.Task);

        async Task EjecutarCreacionConcurrenteAsync(TaskCompletionSource senalPropia)
        {
            await using var contexto = _fixture.CrearContexto();
            var repoEvaluacion = new EvaluacionRepository(contexto);

            var borrador = Evaluacion.CrearBorrador(
                asignacion1,
                rubrica,
                [new DatosCalificacionCriterio(criterio1.Id, 15.00m, "Intento concurrente")],
                "Carrera misma asignación",
                ahora,
                evaluador1Id);

            await repoEvaluacion.AgregarAsync(borrador);

            senalPropia.TrySetResult();
            await barrera.WaitAsync(TimeSpan.FromSeconds(10));

            await contexto.GuardarCambiosAsync();
        }

        var tarea1 = Task.Run(() => EjecutarCreacionConcurrenteAsync(listo1));
        var tarea2 = Task.Run(() => EjecutarCreacionConcurrenteAsync(listo2));

        var tareas = new[] { tarea1, tarea2 };
        Exception? excepcionCapturada = null;
        var exitos = 0;

        foreach (var tarea in tareas)
        {
            try
            {
                await tarea;
                exitos++;
            }
            catch (Exception ex)
            {
                excepcionCapturada = ex;
            }
        }

        exitos.Should().Be(1);
        var duplicada = excepcionCapturada.Should().BeOfType<EvaluacionDuplicadaException>().Subject;
        duplicada.Codigo.Should().Be(EvaluacionDuplicadaException.CodigoError);
        duplicada.EstadoHttp.Should().Be(409);

        await using var contextoFinal = _fixture.CrearContexto();
        var totalPorAsignacion = await contextoFinal.Evaluaciones
            .CountAsync(e => e.AsignacionEvaluacionId == asignacion1.Id);
        totalPorAsignacion.Should().Be(1);
    }

    [Fact]
    public async Task Debe_LanzarExcepcionConflictoConcurrencia409_Cuando_DosActualizacionesSimultaneasModificanElMismoBorrador()
    {
        var (
            rubrica,
            criterio1,
            criterio2,
            _,
            _,
            asignacion1,
            _,
            evaluador1Id,
            _,
            _,
            fechaBase) = await SembrarEscenarioAsync();

        var ahora = fechaBase.AddHours(3);

        await using (var contextoInicial = _fixture.CrearContexto())
        {
            var borradorInicial = Evaluacion.CrearBorrador(
                asignacion1,
                rubrica,
                [new DatosCalificacionCriterio(criterio1.Id, 14.00m, "Versión 1")],
                "Borrador v1",
                ahora,
                evaluador1Id);

            contextoInicial.Evaluaciones.Add(borradorInicial);
            await contextoInicial.GuardarCambiosAsync();
        }

        await using var contextoA = _fixture.CrearContexto();
        await using var contextoB = _fixture.CrearContexto();

        var repoEvalA = new EvaluacionRepository(contextoA);
        var repoEvalB = new EvaluacionRepository(contextoB);
        var repoRubricaA = new ConsultaRubricaParaEvaluacionRepository(contextoA);
        var repoRubricaB = new ConsultaRubricaParaEvaluacionRepository(contextoB);

        var evaluacionA = (await repoEvalA.ObtenerParaActualizarPorAsignacionAsync(asignacion1.Id))!;
        var evaluacionB = (await repoEvalB.ObtenerParaActualizarPorAsignacionAsync(asignacion1.Id))!;
        var rubricaA = (await repoRubricaA.ObtenerPorProgramaYConvocatoriaAsync(
            rubrica.ProgramaBecaId,
            rubrica.ConvocatoriaId))!;
        var rubricaB = (await repoRubricaB.ObtenerPorProgramaYConvocatoriaAsync(
            rubrica.ProgramaBecaId,
            rubrica.ConvocatoriaId))!;

        evaluacionA.ActualizarBorrador(
            rubricaA,
            [new DatosCalificacionCriterio(criterio1.Id, 18.00m, "Actualización A")],
            "Ganadora A",
            asignacion1.FechaLimiteEvaluacion,
            ahora.AddMinutes(5),
            evaluador1Id);

        evaluacionB.ActualizarBorrador(
            rubricaB,
            [
                new DatosCalificacionCriterio(criterio1.Id, 16.00m, "Actualización B"),
                new DatosCalificacionCriterio(criterio2.Id, 7.00m, "Nuevo en B")
            ],
            "Perdedora B",
            asignacion1.FechaLimiteEvaluacion,
            ahora.AddMinutes(6),
            evaluador1Id);

        await contextoA.GuardarCambiosAsync();

        var guardarB = () => contextoB.GuardarCambiosAsync();
        var exConcurrencia = (await guardarB.Should().ThrowAsync<ExcepcionConflictoConcurrencia>()).Which;
        exConcurrencia.EstadoHttp.Should().Be(409);

        await using var contextoVerificar = _fixture.CrearContexto();
        var evaluacionPersistida = (await new EvaluacionRepository(contextoVerificar)
            .ObtenerPorAsignacionAsync(asignacion1.Id))!;
        evaluacionPersistida.Version.Should().Be(2);
        evaluacionPersistida.ComentariosGenerales.Should().Be("Ganadora A");
        evaluacionPersistida.Calificaciones.Should().ContainSingle();
        evaluacionPersistida.Calificaciones.Single().PuntajeAsignado.Should().Be(18.00m);
    }

    private static GuardarBorradorEvaluacionCommandHandler CrearGuardarBorradorHandler(
        PostulacionesDbContext contexto,
        Guid usuarioEvaluadorId,
        TimeProvider timeProvider,
        IUnitOfWork? unitOfWorkPersonalizado = null)
    {
        var uow = unitOfWorkPersonalizado ?? contexto;
        var bloquearRubricaHandler = new BloquearRubricaCommandHandler(
            new RubricaRepository(contexto),
            contexto,
            timeProvider);
        var bloqueadorAdapter = new BloqueadorRubricaEvaluacionAdapter(bloquearRubricaHandler);

        return new GuardarBorradorEvaluacionCommandHandler(
            new UsuarioActualFijo(usuarioEvaluadorId),
            new ConsultaAsignacionParaEvaluacionRepository(contexto),
            new ConvocatoriaEvaluacionAbiertaFija(timeProvider),
            new ComiteEvaluadorRepository(contexto),
            new ConsultaRubricaParaEvaluacionRepository(contexto),
            new EvaluacionRepository(contexto),
            bloqueadorAdapter,
            uow,
            new GuardarBorradorEvaluacionCommandValidator(),
            timeProvider);
    }

    private static EnviarEvaluacionDefinitivaCommandHandler CrearEnviarDefinitivaHandler(
        PostulacionesDbContext contexto,
        Guid usuarioEvaluadorId,
        TimeProvider timeProvider)
    {
        var bloquearRubricaHandler = new BloquearRubricaCommandHandler(
            new RubricaRepository(contexto),
            contexto,
            timeProvider);
        var bloqueadorAdapter = new BloqueadorRubricaEvaluacionAdapter(bloquearRubricaHandler);

        return new EnviarEvaluacionDefinitivaCommandHandler(
            new UsuarioActualFijo(usuarioEvaluadorId),
            new ConsultaAsignacionParaEvaluacionRepository(contexto),
            new ConvocatoriaEvaluacionAbiertaFija(timeProvider),
            new EvaluacionRepository(contexto),
            new ComiteEvaluadorRepository(contexto),
            new ConsultaRubricaParaEvaluacionRepository(contexto),
            bloqueadorAdapter,
            new ConsolidadorPostulacionNoOp(),
            contexto,
            new EnviarEvaluacionDefinitivaCommandValidator(),
            timeProvider);
    }

    private sealed class ConsolidadorPostulacionNoOp : IConsolidadorPostulacionEvaluacion
    {
        public Task IntentarConsolidarAutomaticamenteAsync(
            Guid postulacionId,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
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

    private sealed class UsuarioActualFijo : IUsuarioActual
    {
        public UsuarioActualFijo(Guid usuarioId)
        {
            UsuarioId = usuarioId;
        }

        public Guid UsuarioId { get; }

        public bool EstaAutenticado => true;
    }

    private sealed class ProveedorTiempoFijo : TimeProvider
    {
        private readonly DateTimeOffset _instanteUtc;

        public ProveedorTiempoFijo(DateTimeOffset instanteUtc)
        {
            _instanteUtc = instanteUtc;
        }

        public override DateTimeOffset GetUtcNow() => _instanteUtc;
    }

    private sealed class UnitOfWorkConBarreraPrimeraLlamada : IUnitOfWork
    {
        private readonly IUnitOfWork _interno;
        private readonly TaskCompletionSource _listoPropio;
        private readonly Task _barrera;
        private int _llamadas;

        public UnitOfWorkConBarreraPrimeraLlamada(
            IUnitOfWork interno,
            TaskCompletionSource listoPropio,
            Task barrera)
        {
            _interno = interno;
            _listoPropio = listoPropio;
            _barrera = barrera;
        }

        public async Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _llamadas) == 1)
            {
                _listoPropio.TrySetResult();
                await _barrera.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }

            return await _interno.GuardarCambiosAsync(cancellationToken);
        }
    }
}
