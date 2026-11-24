using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class AsignacionPostgreSqlTests
{
    private readonly PostgreSqlFixture _fixture;

    public AsignacionPostgreSqlTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_PersistirRecargarConsultarCargaYPaginarAsignaciones_Cuando_SeOperaEnPostgreSqlReal()
    {
        var coordinadorId = Guid.CreateVersion7();
        var jefaturaUsuarioId = Guid.CreateVersion7();
        var evaluadorInternoId = Guid.CreateVersion7();
        var evaluadorExternoId = Guid.CreateVersion7();
        var postulacion1Id = Guid.CreateVersion7();
        var postulacion2Id = Guid.CreateVersion7();
        var fechaBase = new DateTimeOffset(2026, 11, 24, 14, 0, 0, TimeSpan.Zero);
        var fechaLimite1 = fechaBase.AddDays(5);
        var fechaLimite2 = fechaBase.AddDays(10);

        ComiteEvaluador comite;
        Guid miembroJefaturaId;
        Guid miembroInternoId;
        Guid miembroExternoId;

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            var repoComite = new ComiteEvaluadorRepository(contextoSemilla);
            comite = ComiteEvaluador.Crear(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "Comité Asignaciones PostgreSQL Real",
                jefaturaUsuarioId,
                fechaBase,
                coordinadorId);

            miembroJefaturaId = comite.Miembros.Single().Id;
            miembroInternoId = comite.AgregarMiembro(
                evaluadorInternoId,
                TipoEvaluador.Interno,
                fechaBase.AddMinutes(1),
                coordinadorId).Id;
            miembroExternoId = comite.AgregarMiembro(
                evaluadorExternoId,
                TipoEvaluador.Externo,
                fechaBase.AddMinutes(2),
                coordinadorId).Id;

            await repoComite.AgregarAsync(comite);
            await contextoSemilla.GuardarCambiosAsync();
        }

        Guid asignacion1Id;
        Guid asignacion2Id;
        Guid asignacion3Id;
        Guid asignacion4Id;

        await using (var contextoAsignar = _fixture.CrearContexto())
        {
            var repoComite = new ComiteEvaluadorRepository(contextoAsignar);
            var repoAsignaciones = new AsignacionEvaluacionRepository(contextoAsignar);
            var comiteLeido = (await repoComite.ObtenerPorIdAsync(comite.Id))!;

            var asignacion1 = AsignacionEvaluacion.Crear(
                comiteLeido,
                miembroJefaturaId,
                postulacion1Id,
                fechaBase.AddMinutes(10),
                fechaLimite2,
                coordinadorId);
            var asignacion2 = AsignacionEvaluacion.Crear(
                comiteLeido,
                miembroExternoId,
                postulacion1Id,
                fechaBase.AddMinutes(11),
                fechaLimite2,
                coordinadorId);
            var asignacion3 = AsignacionEvaluacion.Crear(
                comiteLeido,
                miembroJefaturaId,
                postulacion2Id,
                fechaBase.AddMinutes(12),
                fechaLimite1,
                coordinadorId);
            var asignacion4 = AsignacionEvaluacion.Crear(
                comiteLeido,
                miembroInternoId,
                postulacion2Id,
                fechaBase.AddMinutes(13),
                fechaLimite1,
                coordinadorId);

            asignacion1Id = asignacion1.Id;
            asignacion2Id = asignacion2.Id;
            asignacion3Id = asignacion3.Id;
            asignacion4Id = asignacion4.Id;

            await repoAsignaciones.AgregarRangoAsync([asignacion1, asignacion2, asignacion3, asignacion4]);
            await contextoAsignar.GuardarCambiosAsync();
        }

        // Marcar asignacion4 como COMPLETADA en BD para verificar que no cuenta en carga abierta
        await using (var contextoEstado = _fixture.CrearContexto())
        {
            await contextoEstado.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE postulaciones.asignaciones_evaluacion
                SET estado = 'COMPLETADA'
                WHERE id = {asignacion4Id}
                """);
        }

        await using (var contextoLectura = _fixture.CrearContexto())
        {
            var repoAsignaciones = new AsignacionEvaluacionRepository(contextoLectura);

            var porPostulacion1 = await repoAsignaciones.ListarPorPostulacionAsync(postulacion1Id);
            porPostulacion1.Select(a => a.Id).Should().Equal(asignacion1Id, asignacion2Id);
            porPostulacion1[0].ConvocatoriaId.Should().Be(comite.ConvocatoriaId);
            porPostulacion1[0].Estado.Should().Be(EstadoAsignacionEvaluacion.Pendiente);
            porPostulacion1[0].FechaAsignacion.Should().Be(fechaBase.AddMinutes(10));
            porPostulacion1[0].FechaLimiteEvaluacion.Should().Be(fechaLimite2);
            porPostulacion1[0].Version.Should().Be(1);

            var vigentesPostulacion2 = await repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacion2Id);
            vigentesPostulacion2.Select(a => a.Id).Should().BeEquivalentTo([asignacion3Id, asignacion4Id]);

            var carga = await repoAsignaciones.ObtenerCargaPorMiembroAsync(comite.Id);
            carga[miembroJefaturaId].Should().Be(2);
            carga[miembroExternoId].Should().Be(1);
            carga.ContainsKey(miembroInternoId).Should().BeFalse();

            // Paginación y orden por FechaLimiteEvaluacion ascendente para jefaturaUsuarioId
            var pagina1 = await repoAsignaciones.ListarPorEvaluadorAsync(
                jefaturaUsuarioId,
                EstadoAsignacionEvaluacion.Pendiente,
                pagina: 1,
                tamano: 1);
            pagina1.Total.Should().Be(2);
            pagina1.Items.Should().ContainSingle();
            pagina1.Items[0].Id.Should().Be(asignacion3Id);

            var pagina2 = await repoAsignaciones.ListarPorEvaluadorAsync(
                jefaturaUsuarioId,
                EstadoAsignacionEvaluacion.Pendiente,
                pagina: 2,
                tamano: 1);
            pagina2.Total.Should().Be(2);
            pagina2.Items.Should().ContainSingle();
            pagina2.Items[0].Id.Should().Be(asignacion1Id);
        }
    }

    [Fact]
    public async Task Debe_RechazarEvaluadorVigenteDuplicadoYPermitirReinsercionCuandoPreviaEstaReasignadaPorConflicto()
    {
        var coordinadorId = Guid.CreateVersion7();
        var evaluador1Id = Guid.CreateVersion7();
        var evaluador2Id = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();
        var ahora = new DateTimeOffset(2026, 11, 24, 15, 0, 0, TimeSpan.Zero);

        ComiteEvaluador comite;
        Guid miembro1Id;

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            comite = ComiteEvaluador.Crear(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "Comité Índice Único Parcial",
                evaluador1Id,
                ahora,
                coordinadorId);
            miembro1Id = comite.Miembros.Single().Id;
            comite.AgregarMiembro(evaluador2Id, TipoEvaluador.Externo, ahora.AddMinutes(1), coordinadorId);

            contextoSemilla.ComitesEvaluadores.Add(comite);
            await contextoSemilla.GuardarCambiosAsync();
        }

        Guid primeraAsignacionId;

        await using (var contexto1 = _fixture.CrearContexto())
        {
            var comiteLeido = (await new ComiteEvaluadorRepository(contexto1).ObtenerPorIdAsync(comite.Id))!;
            var asignacion = AsignacionEvaluacion.Crear(
                comiteLeido,
                miembro1Id,
                postulacionId,
                ahora.AddMinutes(5),
                ahora.AddDays(5),
                coordinadorId);
            primeraAsignacionId = asignacion.Id;

            contexto1.AsignacionesEvaluacion.Add(asignacion);
            await contexto1.GuardarCambiosAsync();
        }

        // 1. Intento de insertar otra asignación vigente para el mismo evaluador y postulación -> ASIGNACION_DUPLICADA
        await using (var contextoDuplicado = _fixture.CrearContexto())
        {
            var comiteLeido = (await new ComiteEvaluadorRepository(contextoDuplicado).ObtenerPorIdAsync(comite.Id))!;
            var duplicada = AsignacionEvaluacion.Crear(
                comiteLeido,
                miembro1Id,
                postulacionId,
                ahora.AddMinutes(10),
                ahora.AddDays(6),
                coordinadorId);

            contextoDuplicado.AsignacionesEvaluacion.Add(duplicada);
            var guardarDuplicada = () => contextoDuplicado.GuardarCambiosAsync();

            var ex = (await guardarDuplicada.Should().ThrowAsync<AsignacionDuplicadaException>()).Which;
            ex.Codigo.Should().Be(AsignacionDuplicadaException.CodigoError);
            ex.EstadoHttp.Should().Be(409);
        }

        // 2. Pasar la primera asignación a REASIGNADA_POR_CONFLICTO libera el índice único parcial
        await using (var contextoReasignar = _fixture.CrearContexto())
        {
            await contextoReasignar.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE postulaciones.asignaciones_evaluacion
                SET estado = 'REASIGNADA_POR_CONFLICTO'
                WHERE id = {primeraAsignacionId}
                """);
        }

        await using (var contextoReinsertar = _fixture.CrearContexto())
        {
            var repoAsignaciones = new AsignacionEvaluacionRepository(contextoReinsertar);
            var comiteLeido = (await new ComiteEvaluadorRepository(contextoReinsertar).ObtenerPorIdAsync(comite.Id))!;
            var nuevaVigente = AsignacionEvaluacion.Crear(
                comiteLeido,
                miembro1Id,
                postulacionId,
                ahora.AddMinutes(20),
                ahora.AddDays(7),
                coordinadorId);

            await repoAsignaciones.AgregarRangoAsync([nuevaVigente]);
            await contextoReinsertar.GuardarCambiosAsync();

            var vigentes = await repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacionId);
            vigentes.Should().ContainSingle(a => a.Id == nuevaVigente.Id);

            var todas = await repoAsignaciones.ListarPorPostulacionAsync(postulacionId);
            todas.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task Debe_RechazarViolacionesDeChecksYFksRestrictYConservarAsignacionesTrasDesvincularMiembro()
    {
        var coordinadorId = Guid.CreateVersion7();
        var evaluador1Id = Guid.CreateVersion7();
        var evaluador2Id = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();
        var ahora = new DateTimeOffset(2026, 11, 24, 16, 0, 0, TimeSpan.Zero);

        ComiteEvaluador comite;
        Guid miembro2Id;
        Guid asignacionMiembro2Id;

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            comite = ComiteEvaluador.Crear(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "Comité CHECKs y FKs",
                evaluador1Id,
                ahora,
                coordinadorId);
            miembro2Id = comite.AgregarMiembro(
                evaluador2Id,
                TipoEvaluador.Externo,
                ahora.AddMinutes(1),
                coordinadorId).Id;

            contextoSemilla.ComitesEvaluadores.Add(comite);
            await contextoSemilla.GuardarCambiosAsync();

            var asignacion = AsignacionEvaluacion.Crear(
                comite,
                miembro2Id,
                postulacionId,
                ahora.AddMinutes(5),
                ahora.AddDays(5),
                coordinadorId);
            asignacionMiembro2Id = asignacion.Id;

            contextoSemilla.AsignacionesEvaluacion.Add(asignacion);
            await contextoSemilla.GuardarCambiosAsync();
        }

        await using var contexto = _fixture.CrearContexto();

        // 1. ck_asignaciones_estado_valido
        var estadoInvalido = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.asignaciones_evaluacion
            SET estado = 'ANULADA'
            WHERE id = {asignacionMiembro2Id}
            """);
        var exEstado = (await estadoInvalido.Should().ThrowAsync<PostgresException>()).Which;
        exEstado.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exEstado.ConstraintName.Should().Be("ck_asignaciones_estado_valido");

        // 2. ck_asignaciones_fecha_limite_posterior
        var fechaLimiteNoPosterior = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.asignaciones_evaluacion
            SET fecha_limite_evaluacion = fecha_asignacion
            WHERE id = {asignacionMiembro2Id}
            """);
        var exFecha = (await fechaLimiteNoPosterior.Should().ThrowAsync<PostgresException>()).Which;
        exFecha.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exFecha.ConstraintName.Should().Be("ck_asignaciones_fecha_limite_posterior");

        // 3. FK RESTRICT: no borrar comité ni miembro con asignaciones
        var borrarMiembro = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM postulaciones.miembros_comite
            WHERE id = {miembro2Id}
            """);
        var exFkMiembro = (await borrarMiembro.Should().ThrowAsync<PostgresException>()).Which;
        exFkMiembro.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        exFkMiembro.ConstraintName.Should().Be("fk_asignaciones_miembros_comite");

        var borrarComite = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM postulaciones.comites_evaluadores
            WHERE id = {comite.Id}
            """);
        var exFkComite = (await borrarComite.Should().ThrowAsync<PostgresException>()).Which;
        exFkComite.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        exFkComite.ConstraintName.Should().Be("fk_asignaciones_comites");

        // 4. Desvincular lógicamente al miembro conserva sus asignaciones intactas
        await using (var contextoDesvincular = _fixture.CrearContexto())
        {
            var repoComite = new ComiteEvaluadorRepository(contextoDesvincular);
            var comiteParaActualizar = (await repoComite.ObtenerParaActualizarPorIdAsync(comite.Id))!;
            comiteParaActualizar.DesvincularMiembro(miembro2Id, ahora.AddDays(1), coordinadorId)
                .Should().BeTrue();
            await contextoDesvincular.GuardarCambiosAsync();
        }

        await using (var contextoVerificar = _fixture.CrearContexto())
        {
            var repoAsignaciones = new AsignacionEvaluacionRepository(contextoVerificar);
            var porPostulacion = await repoAsignaciones.ListarPorPostulacionAsync(postulacionId);
            porPostulacion.Should().ContainSingle(a =>
                a.Id == asignacionMiembro2Id
                && a.MiembroComiteId == miembro2Id
                && a.UsuarioEvaluadorId == evaluador2Id
                && a.Estado == EstadoAsignacionEvaluacion.Pendiente);

            var porEvaluador = await repoAsignaciones.ListarPorEvaluadorAsync(
                evaluador2Id,
                estado: null,
                pagina: 1,
                tamano: 10);
            porEvaluador.Total.Should().Be(1);
            porEvaluador.Items.Should().ContainSingle(a => a.Id == asignacionMiembro2Id);
        }
    }

    [Fact]
    public async Task Debe_ConfirmarUnaAsignacionYLanzarAsignacionDuplicadaEnLaOtra_Cuando_DosAsignacionesSimultaneasCompitenConBarrera()
    {
        var coordinadorId = Guid.CreateVersion7();
        var evaluadorId = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();
        var ahora = new DateTimeOffset(2026, 11, 24, 17, 0, 0, TimeSpan.Zero);

        Guid comiteId;
        Guid miembroId;

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            var comite = ComiteEvaluador.Crear(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "Comité Concurrencia Asignaciones",
                evaluadorId,
                ahora,
                coordinadorId);
            comiteId = comite.Id;
            miembroId = comite.Miembros.Single().Id;

            contextoSemilla.ComitesEvaluadores.Add(comite);
            await contextoSemilla.GuardarCambiosAsync();
        }

        var listo1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listo2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var barrera = Task.WhenAll(listo1.Task, listo2.Task);

        async Task EjecutarAsignacionConcurrenteAsync(TaskCompletionSource senalPropia)
        {
            await using var contexto = _fixture.CrearContexto();
            var repoComite = new ComiteEvaluadorRepository(contexto);
            var repoAsignaciones = new AsignacionEvaluacionRepository(contexto);

            var comite = (await repoComite.ObtenerPorIdAsync(comiteId))!;
            var asignacion = AsignacionEvaluacion.Crear(
                comite,
                miembroId,
                postulacionId,
                ahora.AddMinutes(5),
                ahora.AddDays(7),
                coordinadorId);

            await repoAsignaciones.AgregarRangoAsync([asignacion]);

            senalPropia.TrySetResult();
            await barrera.WaitAsync(TimeSpan.FromSeconds(10));

            await contexto.GuardarCambiosAsync();
        }

        var tarea1 = Task.Run(() => EjecutarAsignacionConcurrenteAsync(listo1));
        var tarea2 = Task.Run(() => EjecutarAsignacionConcurrenteAsync(listo2));

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
        var duplicada = excepcionCapturada.Should().BeOfType<AsignacionDuplicadaException>().Subject;
        duplicada.Codigo.Should().Be(AsignacionDuplicadaException.CodigoError);
        duplicada.EstadoHttp.Should().Be(409);

        await using var contextoFinal = _fixture.CrearContexto();
        var repoFinal = new AsignacionEvaluacionRepository(contextoFinal);
        var vigentes = await repoFinal.ListarVigentesPorPostulacionAsync(postulacionId);
        vigentes.Should().ContainSingle(a => a.UsuarioEvaluadorId == evaluadorId);
    }
}
