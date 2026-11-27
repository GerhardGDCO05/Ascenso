using Ascenso.Postulaciones.Application.Asignaciones.Comandos.AsignarEvaluadoresPostulacion;
using Ascenso.Postulaciones.Application.Asignaciones.Comandos.AutorizarExcepcionEvaluadorUnico;
using Ascenso.Postulaciones.Application.Consolidacion.Interfaces;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NSubstitute;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class ExcepcionEvaluadorUnicoPostgreSqlTests
{
    private readonly PostgreSqlFixture _fixture;

    public ExcepcionEvaluadorUnicoPostgreSqlTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_PersistirYRecargarExcepcionEvaluadorUnicoConTodosSusCamposFielmente_Cuando_SeOperaEnPostgreSqlReal()
    {
        var postulacionId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var correlacionId = Guid.CreateVersion7();
        var fechaOffsetLocal = new DateTimeOffset(2026, 11, 24, 15, 30, 0, TimeSpan.FromHours(-4));
        var fechaUtcEsperada = fechaOffsetLocal.ToUniversalTime();
        const string justificacion = "Único especialista certificado disponible en el área de energía nuclear.";

        Guid autorizacionId;

        await using (var contextoEscritura = _fixture.CrearContexto())
        {
            var repositorio = new ExcepcionEvaluadorUnicoRepository(contextoEscritura);
            var autorizacion = AutorizacionExcepcionEvaluadorUnico.Crear(
                postulacionId,
                $"  {justificacion}  ",
                fechaOffsetLocal,
                coordinadorId,
                correlacionId);
            autorizacionId = autorizacion.Id;

            await repositorio.AgregarAsync(autorizacion);
            await contextoEscritura.GuardarCambiosAsync();
        }

        await using (var contextoLectura = _fixture.CrearContexto())
        {
            var repositorio = new ExcepcionEvaluadorUnicoRepository(contextoLectura);

            var recargada = await repositorio.ObtenerPorPostulacionAsync(postulacionId);
            recargada.Should().NotBeNull();
            recargada!.Id.Should().Be(autorizacionId);
            recargada.PostulacionId.Should().Be(postulacionId);
            recargada.Justificacion.Should().Be(justificacion);
            recargada.UsuarioAutorId.Should().Be(coordinadorId);
            recargada.FechaCreacion.Should().Be(fechaUtcEsperada);
            recargada.FechaCreacion.Offset.Should().Be(TimeSpan.Zero);
            recargada.CorrelacionId.Should().Be(correlacionId);

            var inexistente = await repositorio.ObtenerPorPostulacionAsync(Guid.CreateVersion7());
            inexistente.Should().BeNull();
        }
    }

    [Fact]
    public async Task Debe_TraducirViolacionDeUnicidadAExcepcionYaAutorizada_Cuando_SeInsertaSegundaExcepcionParaLaMismaPostulacion()
    {
        var postulacionId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var ahora = new DateTimeOffset(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);

        await using (var contexto1 = _fixture.CrearContexto())
        {
            var repo1 = new ExcepcionEvaluadorUnicoRepository(contexto1);
            var primera = AutorizacionExcepcionEvaluadorUnico.Crear(
                postulacionId,
                "Primera autorización legítima registrada para la postulación.",
                ahora,
                coordinadorId,
                Guid.CreateVersion7());

            await repo1.AgregarAsync(primera);
            await contexto1.GuardarCambiosAsync();
        }

        await using (var contexto2 = _fixture.CrearContexto())
        {
            var repo2 = new ExcepcionEvaluadorUnicoRepository(contexto2);
            var duplicada = AutorizacionExcepcionEvaluadorUnico.Crear(
                postulacionId,
                "Segunda autorización duplicada para la misma postulación.",
                ahora.AddMinutes(5),
                coordinadorId,
                Guid.CreateVersion7());

            await repo2.AgregarAsync(duplicada);
            var guardarDuplicada = () => contexto2.GuardarCambiosAsync();

            var ex = (await guardarDuplicada.Should().ThrowAsync<ExcepcionEvaluadorUnicoYaAutorizadaException>()).Which;
            ex.Codigo.Should().Be(ExcepcionEvaluadorUnicoYaAutorizadaException.CodigoError);
            ex.EstadoHttp.Should().Be(409);
        }
    }

    [Fact]
    public async Task Debe_RechazarJustificacionInvalidaPorCheckYRechazarUpdateYDeletePorTriggerConFilaIntacta()
    {
        var postulacionId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var correlacionId = Guid.CreateVersion7();
        var ahora = new DateTimeOffset(2026, 11, 24, 19, 0, 0, TimeSpan.Zero);
        const string justificacionOriginal = "Justificación inmutable protegida por trigger en PostgreSQL.";

        Guid autorizacionId;

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            var repo = new ExcepcionEvaluadorUnicoRepository(contextoSemilla);
            var autorizacion = AutorizacionExcepcionEvaluadorUnico.Crear(
                postulacionId,
                justificacionOriginal,
                ahora,
                coordinadorId,
                correlacionId);
            autorizacionId = autorizacion.Id;

            await repo.AgregarAsync(autorizacion);
            await contextoSemilla.GuardarCambiosAsync();
        }

        await using var contexto = _fixture.CrearContexto();

        // 1. Violación de ck_excepciones_justificacion_no_vacia (vacía / solo espacios / < 10 tras btrim)
        var insertarJustificacionCorta = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO postulaciones.excepciones_evaluador_unico
                (id, postulacion_id, justificacion, fecha_autorizacion, usuario_autorizador_id, correlacion_id)
            VALUES
                ({Guid.CreateVersion7()}, {Guid.CreateVersion7()}, {"   corta   "}, {ahora}, {coordinadorId}, {Guid.CreateVersion7()})
            """);
        var exCheck = (await insertarJustificacionCorta.Should().ThrowAsync<PostgresException>()).Which;
        exCheck.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        exCheck.ConstraintName.Should().Be("ck_excepciones_justificacion_no_vacia");

        // 2. Intento de UPDATE rechazado por trg_excepciones_evaluador_unico_inmutable
        var intentarUpdate = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.excepciones_evaluador_unico
            SET justificacion = 'Intento de edición silenciosa de la justificación.'
            WHERE id = {autorizacionId}
            """);
        var exUpdate = (await intentarUpdate.Should().ThrowAsync<PostgresException>()).Which;
        exUpdate.MessageText.Should().Contain("inmutables");

        // 3. Intento de DELETE rechazado por trg_excepciones_evaluador_unico_inmutable
        var intentarDelete = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM postulaciones.excepciones_evaluador_unico
            WHERE id = {autorizacionId}
            """);
        var exDelete = (await intentarDelete.Should().ThrowAsync<PostgresException>()).Which;
        exDelete.MessageText.Should().Contain("inmutables");

        // 4. Verificar que la fila permanece intacta
        await using var contextoVerificacion = _fixture.CrearContexto();
        var repoVerificacion = new ExcepcionEvaluadorUnicoRepository(contextoVerificacion);
        var filaIntacta = await repoVerificacion.ObtenerPorPostulacionAsync(postulacionId);

        filaIntacta.Should().NotBeNull();
        filaIntacta!.Id.Should().Be(autorizacionId);
        filaIntacta.PostulacionId.Should().Be(postulacionId);
        filaIntacta.Justificacion.Should().Be(justificacionOriginal);
        filaIntacta.UsuarioAutorId.Should().Be(coordinadorId);
        filaIntacta.FechaCreacion.Should().Be(ahora);
        filaIntacta.CorrelacionId.Should().Be(correlacionId);
    }

    [Fact]
    public async Task Debe_ConfirmarUnaAutorizacionYLanzar409ExcepcionYaAutorizadaEnLaOtra_Cuando_DosAutorizacionesSimultaneasCompitenConBarrera()
    {
        var postulacionId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var ahora = new DateTimeOffset(2026, 11, 24, 20, 0, 0, TimeSpan.Zero);

        var listo1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listo2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var barrera = Task.WhenAll(listo1.Task, listo2.Task);

        async Task EjecutarAutorizacionConcurrenteAsync(TaskCompletionSource senalPropia, string justificacion)
        {
            await using var contexto = _fixture.CrearContexto();
            var repo = new ExcepcionEvaluadorUnicoRepository(contexto);
            var autorizacion = AutorizacionExcepcionEvaluadorUnico.Crear(
                postulacionId,
                justificacion,
                ahora,
                coordinadorId,
                Guid.CreateVersion7());

            await repo.AgregarAsync(autorizacion);

            senalPropia.TrySetResult();
            await barrera.WaitAsync(TimeSpan.FromSeconds(10));

            await contexto.GuardarCambiosAsync();
        }

        var tarea1 = Task.Run(() => EjecutarAutorizacionConcurrenteAsync(
            listo1,
            "Autorización concurrente 1 para excepción de evaluador único."));
        var tarea2 = Task.Run(() => EjecutarAutorizacionConcurrenteAsync(
            listo2,
            "Autorización concurrente 2 para excepción de evaluador único."));

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
        var conflicto = excepcionCapturada.Should().BeOfType<ExcepcionEvaluadorUnicoYaAutorizadaException>().Subject;
        conflicto.Codigo.Should().Be(ExcepcionEvaluadorUnicoYaAutorizadaException.CodigoError);
        conflicto.EstadoHttp.Should().Be(409);

        await using var contextoFinal = _fixture.CrearContexto();
        var totalFilas = await contextoFinal.ExcepcionesEvaluadorUnico
            .CountAsync(e => e.PostulacionId == postulacionId);
        totalFilas.Should().Be(1);
    }

    [Fact]
    public async Task Debe_RechazarAsignacionDeUnEvaluadorSinExcepcionYConfirmarConExcepcionPersistida_Cuando_SeEjecutaFlujoIntegralEnPostgreSqlReal()
    {
        var coordinadorId = Guid.CreateVersion7();
        var evaluadorUnicoId = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();
        var ahora = new DateTimeOffset(2026, 11, 24, 21, 0, 0, TimeSpan.Zero);
        var fechaLimite = ahora.AddDays(7);

        Guid comiteId;

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            var repoComite = new ComiteEvaluadorRepository(contextoSemilla);
            var comite = ComiteEvaluador.Crear(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "Comité Flujo Integral Excepción PostgreSQL",
                evaluadorUnicoId,
                ahora,
                coordinadorId);
            comiteId = comite.Id;

            await repoComite.AgregarAsync(comite);
            await contextoSemilla.GuardarCambiosAsync();
        }

        var usuarioActual = Substitute.For<IUsuarioActual>();
        usuarioActual.EstaAutenticado.Returns(true);
        usuarioActual.UsuarioId.Returns(coordinadorId);

        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(ahora.AddMinutes(5));

        // 1. Sin excepción persistida: asignar 1 evaluador falla con EVALUACIONES_INSUFICIENTES y no persiste nada
        await using (var contextoSinExcepcion = _fixture.CrearContexto())
        {
            var handlerAsignar = new AsignarEvaluadoresPostulacionCommandHandler(
                usuarioActual,
                new ComiteEvaluadorRepository(contextoSinExcepcion),
                new AsignacionEvaluacionRepository(contextoSinExcepcion),
                new ExcepcionEvaluadorUnicoRepository(contextoSinExcepcion),
                contextoSinExcepcion,
                new AsignarEvaluadoresPostulacionCommandValidator(),
                timeProvider);

            var comandoUnEvaluador = new AsignarEvaluadoresPostulacionCommand(
                comiteId,
                postulacionId,
                fechaLimite,
                UsuariosEvaluadoresIds: [evaluadorUnicoId]);

            var intentoSinExcepcion = () => handlerAsignar.HandleAsync(comandoUnEvaluador);

            var ex = (await intentoSinExcepcion.Should().ThrowAsync<EvaluadoresInsuficientesException>()).Which;
            ex.Codigo.Should().Be(EvaluadoresInsuficientesException.CodigoError);
            ex.EstadoHttp.Should().Be(422);
        }

        await using (var contextoVerificacionVacio = _fixture.CrearContexto())
        {
            var repoAsignaciones = new AsignacionEvaluacionRepository(contextoVerificacionVacio);
            var asignacionesPrevias = await repoAsignaciones.ListarPorPostulacionAsync(postulacionId);
            asignacionesPrevias.Should().BeEmpty();
        }

        // 2. Autorizar y persistir la excepción de evaluador único
        await using (var contextoAutorizar = _fixture.CrearContexto())
        {
            var handlerAutorizar = new AutorizarExcepcionEvaluadorUnicoCommandHandler(
                usuarioActual,
                new ExcepcionEvaluadorUnicoRepository(contextoAutorizar),
                new AsignacionEvaluacionRepository(contextoAutorizar),
                Substitute.For<IConsolidadorPostulacionEvaluacion>(),
                contextoAutorizar,
                new AutorizarExcepcionEvaluadorUnicoCommandValidator(),
                timeProvider);

            var dtoExcepcion = await handlerAutorizar.HandleAsync(
                new AutorizarExcepcionEvaluadorUnicoCommand(
                    postulacionId,
                    "Único evaluador disponible en el comité para esta línea temática."));

            dtoExcepcion.PostulacionId.Should().Be(postulacionId);
            dtoExcepcion.UsuarioAutorizadorId.Should().Be(coordinadorId);
        }

        // 3. Con excepción persistida: asignar 1 evaluador se confirma en PostgreSQL
        await using (var contextoConExcepcion = _fixture.CrearContexto())
        {
            var handlerAsignar = new AsignarEvaluadoresPostulacionCommandHandler(
                usuarioActual,
                new ComiteEvaluadorRepository(contextoConExcepcion),
                new AsignacionEvaluacionRepository(contextoConExcepcion),
                new ExcepcionEvaluadorUnicoRepository(contextoConExcepcion),
                contextoConExcepcion,
                new AsignarEvaluadoresPostulacionCommandValidator(),
                timeProvider);

            var resultado = await handlerAsignar.HandleAsync(
                new AsignarEvaluadoresPostulacionCommand(
                    comiteId,
                    postulacionId,
                    fechaLimite,
                    UsuariosEvaluadoresIds: [evaluadorUnicoId]));

            resultado.PostulacionId.Should().Be(postulacionId);
            resultado.Asignaciones.Should().ContainSingle();
            resultado.Asignaciones[0].UsuarioEvaluadorId.Should().Be(evaluadorUnicoId);
            resultado.Asignaciones[0].Estado.Should().Be("PENDIENTE");
        }

        await using (var contextoVerificacionFinal = _fixture.CrearContexto())
        {
            var repoAsignaciones = new AsignacionEvaluacionRepository(contextoVerificacionFinal);
            var vigentes = await repoAsignaciones.ListarVigentesPorPostulacionAsync(postulacionId);
            vigentes.Should().ContainSingle(a => a.UsuarioEvaluadorId == evaluadorUnicoId);
        }
    }
}
