using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class ComitePostgreSqlTests
{
    private readonly PostgreSqlFixture _fixture;

    public ComitePostgreSqlTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_PersistirYRecargarComiteConMiembrosInternosExternosYDesvinculadosYPermitirReincorporacion_Cuando_SeOperaEnPostgreSqlReal()
    {
        var programaBecaId = Guid.CreateVersion7();
        var convocatoriaId = Guid.CreateVersion7();
        var jefaturaUsuarioId = Guid.CreateVersion7();
        var evaluadorInternoId = Guid.CreateVersion7();
        var evaluadorExternoId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var fechaCreacion = new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

        Guid comiteId;
        Guid miembroJefaturaId;
        Guid miembroInternoPrimeraMembresiaId;
        Guid miembroExternoId;

        // 1. Crear comité con su jefatura y dos evaluadores (interno y externo)
        await using (var contextoCrear = _fixture.CrearContexto())
        {
            var repoCrear = new ComiteEvaluadorRepository(contextoCrear);
            var comite = ComiteEvaluador.Crear(
                programaBecaId,
                convocatoriaId,
                "Comité Evaluador Beca Excelencia 2026-II",
                jefaturaUsuarioId,
                fechaCreacion,
                coordinadorId);

            comiteId = comite.Id;
            miembroJefaturaId = comite.Miembros.Single().Id;

            var mInterno = comite.AgregarMiembro(
                evaluadorInternoId,
                TipoEvaluador.Interno,
                fechaCreacion.AddMinutes(5),
                coordinadorId);
            var mExterno = comite.AgregarMiembro(
                evaluadorExternoId,
                TipoEvaluador.Externo,
                fechaCreacion.AddMinutes(10),
                coordinadorId);

            miembroInternoPrimeraMembresiaId = mInterno.Id;
            miembroExternoId = mExterno.Id;

            await repoCrear.AgregarAsync(comite);
            await contextoCrear.GuardarCambiosAsync();

            (await repoCrear.ExistePorProgramaYConvocatoriaAsync(programaBecaId, convocatoriaId))
                .Should().BeTrue();
        }

        // 2. Desvincular al evaluador interno y luego reincorporarlo (nueva membresía activa)
        Guid miembroInternoSegundaMembresiaId;
        var fechaDesvinculacion = fechaCreacion.AddDays(1);
        var fechaReincorporacion = fechaCreacion.AddDays(2);

        await using (var contextoEditar = _fixture.CrearContexto())
        {
            var repoEditar = new ComiteEvaluadorRepository(contextoEditar);
            var comite = await repoEditar.ObtenerParaActualizarPorIdAsync(comiteId);

            comite.Should().NotBeNull();
            comite!.Miembros.Should().HaveCount(3);

            comite.DesvincularMiembro(miembroInternoPrimeraMembresiaId, fechaDesvinculacion, coordinadorId)
                .Should().BeTrue();

            var reincorporado = comite.AgregarMiembro(
                evaluadorInternoId,
                TipoEvaluador.Interno,
                fechaReincorporacion,
                coordinadorId);
            miembroInternoSegundaMembresiaId = reincorporado.Id;

            await contextoEditar.GuardarCambiosAsync();
        }

        // 3. Recargar por Id y por Programa y Convocatoria y verificar estado completo
        await using (var contextoLectura = _fixture.CrearContexto())
        {
            var repoLectura = new ComiteEvaluadorRepository(contextoLectura);
            var comitePorId = await repoLectura.ObtenerPorIdAsync(comiteId);
            var comitePorPrograma = await repoLectura.ObtenerPorProgramaYConvocatoriaAsync(programaBecaId, convocatoriaId);

            comitePorId.Should().NotBeNull();
            comitePorPrograma.Should().NotBeNull();
            comitePorPrograma!.Id.Should().Be(comiteId);

            comitePorId!.ProgramaBecaId.Should().Be(programaBecaId);
            comitePorId.ConvocatoriaId.Should().Be(convocatoriaId);
            comitePorId.Nombre.Should().Be("Comité Evaluador Beca Excelencia 2026-II");
            comitePorId.JefaturaUsuarioId.Should().Be(jefaturaUsuarioId);
            comitePorId.ConformadoPorUsuarioId.Should().Be(coordinadorId);
            comitePorId.FechaConformacion.Should().Be(DateOnly.FromDateTime(fechaCreacion.UtcDateTime));
            comitePorId.Activo.Should().BeTrue();
            comitePorId.Version.Should().Be(5);
            comitePorId.Miembros.Should().HaveCount(4);

            var jefatura = comitePorId.Miembros.Single(m => m.Id == miembroJefaturaId);
            jefatura.UsuarioEvaluadorId.Should().Be(jefaturaUsuarioId);
            jefatura.Tipo.Should().Be(TipoEvaluador.Interno);
            jefatura.Rol.Should().Be(RolMiembroComite.Jefatura);
            jefatura.Activo.Should().BeTrue();
            jefatura.FechaDesvinculacion.Should().BeNull();

            var externo = comitePorId.Miembros.Single(m => m.Id == miembroExternoId);
            externo.UsuarioEvaluadorId.Should().Be(evaluadorExternoId);
            externo.Tipo.Should().Be(TipoEvaluador.Externo);
            externo.Rol.Should().Be(RolMiembroComite.Evaluador);
            externo.Activo.Should().BeTrue();
            externo.FechaDesvinculacion.Should().BeNull();

            var desvinculado = comitePorId.Miembros.Single(m => m.Id == miembroInternoPrimeraMembresiaId);
            desvinculado.UsuarioEvaluadorId.Should().Be(evaluadorInternoId);
            desvinculado.Tipo.Should().Be(TipoEvaluador.Interno);
            desvinculado.Rol.Should().Be(RolMiembroComite.Evaluador);
            desvinculado.Activo.Should().BeFalse();
            desvinculado.FechaDesvinculacion.Should().Be(DateOnly.FromDateTime(fechaDesvinculacion.UtcDateTime));

            var reincorporado = comitePorId.Miembros.Single(m => m.Id == miembroInternoSegundaMembresiaId);
            reincorporado.UsuarioEvaluadorId.Should().Be(evaluadorInternoId);
            reincorporado.Tipo.Should().Be(TipoEvaluador.Interno);
            reincorporado.Rol.Should().Be(RolMiembroComite.Evaluador);
            reincorporado.Activo.Should().BeTrue();
            reincorporado.FechaAsignacion.Should().Be(DateOnly.FromDateTime(fechaReincorporacion.UtcDateTime));
            reincorporado.FechaDesvinculacion.Should().BeNull();
        }
    }

    [Fact]
    public async Task Debe_RechazarComiteDuplicadoPorProgramaYConvocatoria_Cuando_SeViolaIndiceUnico()
    {
        var programaBecaId = Guid.CreateVersion7();
        var convocatoriaId = Guid.CreateVersion7();
        var usuarioId = Guid.CreateVersion7();
        var ahora = DateTimeOffset.UtcNow;

        await using (var contexto1 = _fixture.CrearContexto())
        {
            contexto1.ComitesEvaluadores.Add(ComiteEvaluador.Crear(
                programaBecaId,
                convocatoriaId,
                "Comité Original",
                Guid.CreateVersion7(),
                ahora,
                usuarioId));
            await contexto1.GuardarCambiosAsync();
        }

        await using (var contexto2 = _fixture.CrearContexto())
        {
            contexto2.ComitesEvaluadores.Add(ComiteEvaluador.Crear(
                programaBecaId,
                convocatoriaId,
                "Comité Duplicado",
                Guid.CreateVersion7(),
                ahora.AddMinutes(1),
                usuarioId));

            var accion = () => contexto2.GuardarCambiosAsync();

            var excepcion = (await accion.Should()
                .ThrowAsync<ComiteDuplicadoException>()).Which;
            excepcion.Codigo.Should().Be(ComiteDuplicadoException.CodigoError);
            excepcion.EstadoHttp.Should().Be(409);
        }
    }

    [Fact]
    public async Task Debe_RechazarMiembroActivoDuplicadoYJefaturaActivaDuplicada_Cuando_SeViolanIndicesParciales()
    {
        var usuarioId = Guid.CreateVersion7();
        var jefaturaId = Guid.CreateVersion7();
        var evaluadorId = Guid.CreateVersion7();
        var ahora = DateTimeOffset.UtcNow;

        var comite = ComiteEvaluador.Crear(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Comité Índices Parciales",
            jefaturaId,
            ahora,
            usuarioId);
        var miembroEvaluador = comite.AgregarMiembro(
            evaluadorId,
            TipoEvaluador.Interno,
            ahora.AddMinutes(1),
            usuarioId);

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.ComitesEvaluadores.Add(comite);
            await contextoSemilla.GuardarCambiosAsync();
        }

        // 1. Violación de ux_miembros_comite_usuario_activo vía SQL y traducción en DbContext
        await using (var contextoDuplicadoUsuario = _fixture.CrearContexto())
        {
            var insertarUsuarioDuplicado = () => contextoDuplicadoUsuario.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO postulaciones.miembros_comite (
                    id, comite_evaluador_id, usuario_evaluador_id, tipo, rol,
                    fecha_asignacion, fecha_desvinculacion, activo,
                    fecha_creacion, usuario_creacion_id, fecha_ultima_modificacion, usuario_modificacion_id, version
                ) VALUES (
                    {Guid.CreateVersion7()}, {comite.Id}, {evaluadorId}, 'EXTERNO', 'EVALUADOR',
                    {DateOnly.FromDateTime(ahora.UtcDateTime)}, NULL, TRUE,
                    {ahora}, {usuarioId}, {ahora}, {usuarioId}, 1
                )
                """);

            var pgExUsuario = (await insertarUsuarioDuplicado.Should().ThrowAsync<PostgresException>()).Which;
            pgExUsuario.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
            pgExUsuario.ConstraintName.Should().Be("ux_miembros_comite_usuario_activo");
        }

        // También verificar traducción a MiembroComiteDuplicadoException cuando dos DbContext agregan al mismo usuario simultáneamente
        await using (var contextoA = _fixture.CrearContexto())
        await using (var contextoB = _fixture.CrearContexto())
        {
            var nuevoEvaluadorSimultaneo = Guid.CreateVersion7();
            var fechaHoy = DateOnly.FromDateTime(ahora.UtcDateTime);

            // Insertamos directamente una fila con el usuario para simular carrera sin chocar con Version del comité
            await contextoA.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO postulaciones.miembros_comite (
                    id, comite_evaluador_id, usuario_evaluador_id, tipo, rol,
                    fecha_asignacion, fecha_desvinculacion, activo,
                    fecha_creacion, usuario_creacion_id, fecha_ultima_modificacion, usuario_modificacion_id, version
                ) VALUES (
                    {Guid.CreateVersion7()}, {comite.Id}, {nuevoEvaluadorSimultaneo}, 'INTERNO', 'EVALUADOR',
                    {fechaHoy}, NULL, TRUE,
                    {ahora}, {usuarioId}, {ahora}, {usuarioId}, 1
                )
                """);

            var comiteEnB = await new ComiteEvaluadorRepository(contextoB).ObtenerParaActualizarPorIdAsync(comite.Id);
            // comiteEnB fue cargado después del insert pero filtrando manualmente o agregando una entidad MiembroComite con otro usuario y cambiándolo
            contextoB.MiembrosComite.Add(
                Ascenso.Postulaciones.Domain.Comites.Entidades.MiembroComite.Crear(
                    comite.Id,
                    nuevoEvaluadorSimultaneo,
                    TipoEvaluador.Interno,
                    RolMiembroComite.Evaluador,
                    ahora,
                    usuarioId));

            var guardarDuplicado = () => contextoB.GuardarCambiosAsync();
            var exTraducida = (await guardarDuplicado.Should().ThrowAsync<MiembroComiteDuplicadoException>()).Which;
            exTraducida.Codigo.Should().Be(MiembroComiteDuplicadoException.CodigoError);
            exTraducida.EstadoHttp.Should().Be(409);
        }

        // 2. Violación de ux_miembros_comite_jefatura_activa
        await using (var contextoJefatura = _fixture.CrearContexto())
        {
            var actualizarAJefatura = () => contextoJefatura.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE postulaciones.miembros_comite
                SET rol = 'JEFATURA', tipo = 'INTERNO'
                WHERE id = {miembroEvaluador.Id}
                """);

            var pgExJefatura = (await actualizarAJefatura.Should().ThrowAsync<PostgresException>()).Which;
            pgExJefatura.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
            pgExJefatura.ConstraintName.Should().Be("ux_miembros_comite_jefatura_activa");
        }
    }

    [Fact]
    public async Task Debe_RechazarViolacionesDeTodosLosChecksDeMiembrosComite_Cuando_SeEvaluaEnPostgreSqlReal()
    {
        var usuarioId = Guid.CreateVersion7();
        var ahora = DateTimeOffset.UtcNow;
        var fechaAsignacion = DateOnly.FromDateTime(ahora.UtcDateTime);
        var comite = ComiteEvaluador.Crear(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Comité Validación CHECKs",
            Guid.CreateVersion7(),
            ahora,
            usuarioId);
        var miembro = comite.AgregarMiembro(
            Guid.CreateVersion7(),
            TipoEvaluador.Interno,
            ahora.AddMinutes(1),
            usuarioId);

        await using var contexto = _fixture.CrearContexto();
        contexto.ComitesEvaluadores.Add(comite);
        await contexto.GuardarCambiosAsync();

        // 1. ck_miembros_comite_tipo_valido
        var tipoInvalido = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.miembros_comite
            SET tipo = 'INVITADO'
            WHERE id = {miembro.Id}
            """);
        var exTipo = (await tipoInvalido.Should().ThrowAsync<PostgresException>()).Which;
        exTipo.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exTipo.ConstraintName.Should().Be("ck_miembros_comite_tipo_valido");

        // 2. ck_miembros_comite_rol_valido
        var rolInvalido = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.miembros_comite
            SET rol = 'SECRETARIO'
            WHERE id = {miembro.Id}
            """);
        var exRol = (await rolInvalido.Should().ThrowAsync<PostgresException>()).Which;
        exRol.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exRol.ConstraintName.Should().Be("ck_miembros_comite_rol_valido");

        // 3. ck_miembros_comite_desvinculacion_coherente: activo = false sin fecha_desvinculacion
        var inactivoSinFecha = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.miembros_comite
            SET activo = FALSE, fecha_desvinculacion = NULL
            WHERE id = {miembro.Id}
            """);
        var exInactivoSinFecha = (await inactivoSinFecha.Should().ThrowAsync<PostgresException>()).Which;
        exInactivoSinFecha.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exInactivoSinFecha.ConstraintName.Should().Be("ck_miembros_comite_desvinculacion_coherente");

        // 4. ck_miembros_comite_desvinculacion_coherente: activo = true con fecha_desvinculacion no nula
        var activoConFecha = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.miembros_comite
            SET activo = TRUE, fecha_desvinculacion = {fechaAsignacion}
            WHERE id = {miembro.Id}
            """);
        var exActivoConFecha = (await activoConFecha.Should().ThrowAsync<PostgresException>()).Which;
        exActivoConFecha.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exActivoConFecha.ConstraintName.Should().Be("ck_miembros_comite_desvinculacion_coherente");

        // 5. ck_miembros_comite_desvinculacion_coherente: fecha_desvinculacion < fecha_asignacion
        var fechaAnterior = fechaAsignacion.AddDays(-1);
        var desvinculacionAnterior = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.miembros_comite
            SET activo = FALSE, fecha_desvinculacion = {fechaAnterior}
            WHERE id = {miembro.Id}
            """);
        var exFechaAnterior = (await desvinculacionAnterior.Should().ThrowAsync<PostgresException>()).Which;
        exFechaAnterior.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exFechaAnterior.ConstraintName.Should().Be("ck_miembros_comite_desvinculacion_coherente");

        // 6. ck_miembros_comite_jefatura_interna: rol = 'JEFATURA' con tipo = 'EXTERNO'
        var miembroJefatura = comite.Miembros.Single(m => m.Rol == RolMiembroComite.Jefatura);
        var jefaturaExterna = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.miembros_comite
            SET tipo = 'EXTERNO'
            WHERE id = {miembroJefatura.Id}
            """);
        var exJefaturaExterna = (await jefaturaExterna.Should().ThrowAsync<PostgresException>()).Which;
        exJefaturaExterna.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exJefaturaExterna.ConstraintName.Should().Be("ck_miembros_comite_jefatura_interna");
    }

    [Fact]
    public async Task Debe_ConfirmarUnaAltaYLanzarExcepcionConflictoConcurrenciaEnLaOtra_Cuando_DosAltasSimultaneasCompitenConBarrera()
    {
        var usuarioId = Guid.CreateVersion7();
        var ahora = DateTimeOffset.UtcNow;
        Guid comiteId;

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            var comite = ComiteEvaluador.Crear(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "Comité Concurrencia Altas",
                Guid.CreateVersion7(),
                ahora,
                usuarioId);
            comiteId = comite.Id;
            contextoSemilla.ComitesEvaluadores.Add(comite);
            await contextoSemilla.GuardarCambiosAsync();
        }

        var listo1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listo2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var barrera = Task.WhenAll(listo1.Task, listo2.Task);

        async Task EjecutarAltaConcurrenteAsync(Guid evaluadorId, TipoEvaluador tipo, TaskCompletionSource senalPropia)
        {
            await using var contexto = _fixture.CrearContexto();
            var repo = new ComiteEvaluadorRepository(contexto);
            var comite = await repo.ObtenerParaActualizarPorIdAsync(comiteId);
            comite.Should().NotBeNull();

            comite!.AgregarMiembro(evaluadorId, tipo, ahora.AddMinutes(2), usuarioId);

            senalPropia.TrySetResult();
            await barrera.WaitAsync(TimeSpan.FromSeconds(10));

            await contexto.GuardarCambiosAsync();
        }

        var tarea1 = Task.Run(() => EjecutarAltaConcurrenteAsync(Guid.CreateVersion7(), TipoEvaluador.Interno, listo1));
        var tarea2 = Task.Run(() => EjecutarAltaConcurrenteAsync(Guid.CreateVersion7(), TipoEvaluador.Externo, listo2));

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
        excepcionCapturada.Should().BeOfType<ExcepcionConflictoConcurrencia>();

        await using var contextoFinal = _fixture.CrearContexto();
        var comiteFinal = await new ComiteEvaluadorRepository(contextoFinal).ObtenerPorIdAsync(comiteId);
        comiteFinal.Should().NotBeNull();
        comiteFinal!.Version.Should().Be(2);
        comiteFinal.Miembros.Should().HaveCount(2);
    }
}
