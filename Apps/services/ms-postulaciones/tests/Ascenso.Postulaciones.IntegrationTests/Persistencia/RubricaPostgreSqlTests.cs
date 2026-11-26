using Ascenso.Postulaciones.Application.Rubricas.Comandos.BloquearRubrica;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class RubricaPostgreSqlTests
{
    private readonly PostgreSqlFixture _fixture;

    public RubricaPostgreSqlTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_EjecutarFlujoCompletoDeRubrica_Cuando_SePersisteEnPostgreSqlReal()
    {
        var programaBecaId = Guid.CreateVersion7();
        var convocatoriaId = Guid.CreateVersion7();
        var usuarioId = Guid.CreateVersion7();
        var fechaCreacion = DateTimeOffset.UtcNow;

        Guid rubricaId;

        // 1. Crear rúbrica y persistirla sin criterios
        await using (var contextoCreacion = _fixture.CrearContexto())
        {
            var repositorioCreacion = new RubricaRepository(contextoCreacion);
            var rubrica = RubricaEvaluacion.Crear(
                programaBecaId,
                convocatoriaId,
                "Rúbrica Excelencia Académica 2026-II",
                fechaCreacion,
                usuarioId);

            rubricaId = rubrica.Id;

            await repositorioCreacion.AgregarAsync(rubrica);
            await contextoCreacion.GuardarCambiosAsync();

            (await repositorioCreacion.ExistePorProgramaYConvocatoriaAsync(programaBecaId, convocatoriaId))
                .Should().BeTrue();
        }

        Guid criterio1Id;
        Guid criterio2Id;
        Guid criterio3Id;

        // 2. Recargar la rúbrica ya persistida y agregar 3 criterios
        await using (var contextoAgregar = _fixture.CrearContexto())
        {
            var repositorioAgregar = new RubricaRepository(contextoAgregar);
            var rubrica = await repositorioAgregar.ObtenerParaActualizarPorIdAsync(rubricaId);

            rubrica.Should().NotBeNull();
            rubrica!.Estado.Should().Be(EstadoRubrica.Borrador);
            rubrica.Criterios.Should().BeEmpty();

            var fechaAgregar = fechaCreacion.AddMinutes(5);
            var criterio1 = rubrica.AgregarCriterio(
                "Promedio académico",
                "Calificación del promedio histórico acumulado",
                40.00m,
                0.00m,
                20.00m,
                fechaAgregar,
                usuarioId);
            var criterio2 = rubrica.AgregarCriterio(
                "Criterio temporal",
                "Criterio que será eliminado",
                20.00m,
                0.00m,
                10.00m,
                fechaAgregar.AddSeconds(1),
                usuarioId);
            var criterio3 = rubrica.AgregarCriterio(
                "Entrevista personal",
                "Evaluación del comité de selección",
                40.00m,
                0.00m,
                100.00m,
                fechaAgregar.AddSeconds(2),
                usuarioId);

            criterio1Id = criterio1.Id;
            criterio2Id = criterio2.Id;
            criterio3Id = criterio3.Id;

            await contextoAgregar.GuardarCambiosAsync();
        }

        // 3. Recargar, editar criterio 1, eliminar criterio 2 y editar criterio 3 para sumar 100.00
        await using (var contextoEdicion = _fixture.CrearContexto())
        {
            var repositorioEdicion = new RubricaRepository(contextoEdicion);
            var rubrica = await repositorioEdicion.ObtenerParaActualizarPorIdAsync(rubricaId);

            rubrica.Should().NotBeNull();
            rubrica!.Criterios.Should().HaveCount(3);

            var fechaEdicion = fechaCreacion.AddMinutes(10);
            rubrica.EditarCriterio(
                criterio1Id,
                "Promedio académico ponderado",
                "Evaluación del promedio ponderado verificado",
                45.50m,
                10.00m,
                20.00m,
                fechaEdicion,
                usuarioId);

            rubrica.EliminarCriterio(
                criterio2Id,
                fechaEdicion.AddSeconds(1),
                usuarioId);

            rubrica.EditarCriterio(
                criterio3Id,
                "Entrevista y ensayo",
                "Evaluación integral de entrevista y ensayo",
                54.50m,
                0.00m,
                1000.00m,
                fechaEdicion.AddSeconds(2),
                usuarioId);

            await contextoEdicion.GuardarCambiosAsync();
        }

        // 4. Recargar, publicar y guardar
        var fechaPublicacion = fechaCreacion.AddMinutes(15);
        await using (var contextoPublicar = _fixture.CrearContexto())
        {
            var repositorioPublicar = new RubricaRepository(contextoPublicar);
            var rubrica = await repositorioPublicar.ObtenerParaActualizarPorIdAsync(rubricaId);

            rubrica.Should().NotBeNull();
            rubrica!.Criterios.Should().HaveCount(2);
            rubrica.SumaPesos.Should().Be(100.00m);
            rubrica.DiferenciaPara100.Should().Be(0.00m);

            rubrica.Publicar(fechaPublicacion, usuarioId);

            await contextoPublicar.GuardarCambiosAsync();
        }

        // 5. Recargar en modo lectura y verificar estado final y criterios
        await using (var contextoLectura = _fixture.CrearContexto())
        {
            var repositorioLectura = new RubricaRepository(contextoLectura);
            var rubrica = await repositorioLectura.ObtenerPorIdAsync(rubricaId);

            rubrica.Should().NotBeNull();
            rubrica!.Id.Should().Be(rubricaId);
            rubrica.ProgramaBecaId.Should().Be(programaBecaId);
            rubrica.ConvocatoriaId.Should().Be(convocatoriaId);
            rubrica.Estado.Should().Be(EstadoRubrica.Publicada);
            rubrica.FechaPublicacion.Should().NotBeNull();
            rubrica.SumaPesos.Should().Be(100.00m);
            rubrica.DiferenciaPara100.Should().Be(0.00m);
            rubrica.Criterios.Should().HaveCount(2);

            var criteriosOrdenados = rubrica.Criterios.OrderBy(c => c.Orden).ToList();
            criteriosOrdenados[0].Id.Should().Be(criterio1Id);
            criteriosOrdenados[0].Nombre.Should().Be("Promedio académico ponderado");
            criteriosOrdenados[0].Descripcion.Should().Be("Evaluación del promedio ponderado verificado");
            criteriosOrdenados[0].PesoPorcentual.Should().Be(45.50m);
            criteriosOrdenados[0].PuntajeMinimo.Should().Be(10.00m);
            criteriosOrdenados[0].PuntajeMaximo.Should().Be(20.00m);
            criteriosOrdenados[0].Orden.Should().Be(1);

            criteriosOrdenados[1].Id.Should().Be(criterio3Id);
            criteriosOrdenados[1].Nombre.Should().Be("Entrevista y ensayo");
            criteriosOrdenados[1].Descripcion.Should().Be("Evaluación integral de entrevista y ensayo");
            criteriosOrdenados[1].PesoPorcentual.Should().Be(54.50m);
            criteriosOrdenados[1].PuntajeMinimo.Should().Be(0.00m);
            criteriosOrdenados[1].PuntajeMaximo.Should().Be(1000.00m);
            criteriosOrdenados[1].Orden.Should().Be(3);

            var criterioEliminadoEnBd = await contextoLectura.CriteriosEvaluacion
                .AnyAsync(c => c.Id == criterio2Id);
            criterioEliminadoEnBd.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Debe_RechazarRubricaDuplicadaPorProgramaYConvocatoria_Cuando_SeViolaIndiceUnico()
    {
        var programaBecaId = Guid.CreateVersion7();
        var convocatoriaId = Guid.CreateVersion7();
        var usuarioId = Guid.CreateVersion7();
        var ahora = DateTimeOffset.UtcNow;

        await using (var contexto1 = _fixture.CrearContexto())
        {
            contexto1.Rubricas.Add(RubricaEvaluacion.Crear(
                programaBecaId,
                convocatoriaId,
                "Rúbrica Original",
                ahora,
                usuarioId));
            await contexto1.GuardarCambiosAsync();
        }

        await using (var contexto2 = _fixture.CrearContexto())
        {
            contexto2.Rubricas.Add(RubricaEvaluacion.Crear(
                programaBecaId,
                convocatoriaId,
                "Rúbrica Duplicada",
                ahora.AddMinutes(1),
                usuarioId));

            var accion = () => contexto2.GuardarCambiosAsync();

            var excepcion = (await accion.Should()
                .ThrowAsync<RubricaDuplicadaException>()).Which;
            excepcion.Codigo.Should().Be(RubricaDuplicadaException.CodigoError);
            excepcion.EstadoHttp.Should().Be(409);
        }
    }

    [Fact]
    public async Task Debe_RechazarOrdenDuplicadoEnLaMismaRubrica_Cuando_SeViolaIndiceUnicoDeCriterios()
    {
        var usuarioId = Guid.CreateVersion7();
        var ahora = DateTimeOffset.UtcNow;
        var rubrica = RubricaEvaluacion.Crear(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Rúbrica Orden Único",
            ahora,
            usuarioId);
        var criterio1 = rubrica.AgregarCriterio(
            "Criterio 1",
            "Descripción 1",
            50.00m,
            0.00m,
            10.00m,
            ahora,
            usuarioId);
        var criterio2 = rubrica.AgregarCriterio(
            "Criterio 2",
            "Descripción 2",
            50.00m,
            0.00m,
            10.00m,
            ahora,
            usuarioId);

        await using var contexto = _fixture.CrearContexto();
        contexto.Rubricas.Add(rubrica);
        await contexto.GuardarCambiosAsync();

        var accionSql = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.criterios_evaluacion
            SET orden = {criterio1.Orden}
            WHERE id = {criterio2.Id}
            """);

        var pgExcepcion = (await accionSql.Should()
            .ThrowAsync<PostgresException>()).Which;

        pgExcepcion.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        pgExcepcion.ConstraintName.Should().Be("ux_criterios_evaluacion_rubrica_orden");
    }

    [Fact]
    public async Task Debe_PersistirLimitesExactosYRechazarExcesosDeLongitud_Cuando_SeEvaluaEnPostgreSql()
    {
        var usuarioId = Guid.CreateVersion7();
        var ahora = DateTimeOffset.UtcNow;
        var nombreRubrica200 = new string('R', 200);
        var nombreCriterio200 = new string('C', 200);
        var descripcionCriterio1000 = new string('D', 1000);

        var rubrica = RubricaEvaluacion.Crear(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            nombreRubrica200,
            ahora,
            usuarioId);
        var criterio = rubrica.AgregarCriterio(
            nombreCriterio200,
            descripcionCriterio1000,
            100.00m,
            0.00m,
            1000.00m,
            ahora,
            usuarioId);

        await using var contexto = _fixture.CrearContexto();
        contexto.Rubricas.Add(rubrica);
        await contexto.GuardarCambiosAsync();

        var excesoNombreRubrica = new string('R', 201);
        var accionNombreRubrica = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.rubricas
            SET nombre = {excesoNombreRubrica}
            WHERE id = {rubrica.Id}
            """);

        (await accionNombreRubrica.Should().ThrowAsync<PostgresException>()).Which
            .SqlState.Should().Be(PostgresErrorCodes.StringDataRightTruncation);

        var excesoNombreCriterio = new string('C', 201);
        var accionNombreCriterio = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.criterios_evaluacion
            SET nombre = {excesoNombreCriterio}
            WHERE id = {criterio.Id}
            """);

        (await accionNombreCriterio.Should().ThrowAsync<PostgresException>()).Which
            .SqlState.Should().Be(PostgresErrorCodes.StringDataRightTruncation);

        var excesoDescripcionCriterio = new string('D', 1001);
        var accionDescripcionCriterio = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.criterios_evaluacion
            SET descripcion = {excesoDescripcionCriterio}
            WHERE id = {criterio.Id}
            """);

        (await accionDescripcionCriterio.Should().ThrowAsync<PostgresException>()).Which
            .SqlState.Should().Be(PostgresErrorCodes.StringDataRightTruncation);
    }

    [Fact]
    public async Task Debe_PersistirYRecargarBloqueoDeRubrica_Cuando_SeBloqueaEnPostgreSqlReal()
    {
        var usuarioId = Guid.CreateVersion7();
        var ahora = new DateTimeOffset(2026, 11, 22, 14, 0, 0, TimeSpan.Zero);
        var fechaPublicacion = ahora.AddMinutes(5);
        var fechaBloqueo = ahora.AddMinutes(10);
        Guid rubricaId;

        await using (var contextoCrear = _fixture.CrearContexto())
        {
            var repoCrear = new RubricaRepository(contextoCrear);
            var rubrica = RubricaEvaluacion.Crear(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "Rúbrica para bloqueo persistido",
                ahora,
                usuarioId);
            rubrica.AgregarCriterio(
                "Mérito académico",
                "Promedio ponderado",
                100.00m,
                0.00m,
                20.00m,
                ahora.AddMinutes(1),
                usuarioId);
            rubrica.Publicar(fechaPublicacion, usuarioId);
            rubricaId = rubrica.Id;

            await repoCrear.AgregarAsync(rubrica);
            await contextoCrear.GuardarCambiosAsync();
        }

        await using (var contextoBloquear = _fixture.CrearContexto())
        {
            var repoBloquear = new RubricaRepository(contextoBloquear);
            var rubrica = await repoBloquear.ObtenerParaActualizarPorIdAsync(rubricaId);
            rubrica.Should().NotBeNull();
            var versionAntes = rubrica!.Version;

            rubrica.Bloquear(MotivoBloqueoRubrica.PrimeraCalificacion, fechaBloqueo, UsuarioSistema.Id)
                .Should().BeTrue();
            rubrica.Version.Should().Be(versionAntes + 1);

            await contextoBloquear.GuardarCambiosAsync();
        }

        await using (var contextoLectura = _fixture.CrearContexto())
        {
            var repoLectura = new RubricaRepository(contextoLectura);
            var rubrica = await repoLectura.ObtenerPorIdAsync(rubricaId);

            rubrica.Should().NotBeNull();
            rubrica!.Estado.Should().Be(EstadoRubrica.Bloqueada);
            rubrica.MotivoBloqueo.Should().Be(MotivoBloqueoRubrica.PrimeraCalificacion);
            rubrica.FechaBloqueo.Should().Be(fechaBloqueo);
            rubrica.UsuarioModificacionId.Should().Be(UsuarioSistema.Id);
        }
    }

    [Fact]
    public async Task Debe_RechazarViolacionesDeChecksDeBloqueo_Cuando_SeEvaluaEnPostgreSqlReal()
    {
        var usuarioId = Guid.CreateVersion7();
        var ahora = DateTimeOffset.UtcNow;
        var rubrica = RubricaEvaluacion.Crear(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Rúbrica Validación CHECKs Bloqueo",
            ahora,
            usuarioId);
        rubrica.AgregarCriterio(
            "Criterio único",
            "Descripción",
            100.00m,
            0.00m,
            20.00m,
            ahora.AddMinutes(1),
            usuarioId);

        await using var contexto = _fixture.CrearContexto();
        contexto.Rubricas.Add(rubrica);
        await contexto.GuardarCambiosAsync();

        // 1. BLOQUEADA sin motivo ni fecha -> viola ck_rubricas_bloqueo_coherente
        var bloqueadaSinDatos = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.rubricas
            SET estado = 'BLOQUEADA', motivo_bloqueo = NULL, fecha_bloqueo = NULL
            WHERE id = {rubrica.Id}
            """);
        var ex1 = (await bloqueadaSinDatos.Should().ThrowAsync<PostgresException>()).Which;
        ex1.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        ex1.ConstraintName.Should().Be("ck_rubricas_bloqueo_coherente");

        // 2. BLOQUEADA con motivo pero sin fecha -> viola ck_rubricas_bloqueo_coherente
        var bloqueadaSinFecha = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.rubricas
            SET estado = 'BLOQUEADA', motivo_bloqueo = 'INICIO_EVALUACION', fecha_bloqueo = NULL
            WHERE id = {rubrica.Id}
            """);
        var ex2 = (await bloqueadaSinFecha.Should().ThrowAsync<PostgresException>()).Which;
        ex2.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        ex2.ConstraintName.Should().Be("ck_rubricas_bloqueo_coherente");

        // 3. BLOQUEADA con fecha pero sin motivo -> viola ck_rubricas_bloqueo_coherente
        var bloqueadaSinMotivo = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.rubricas
            SET estado = 'BLOQUEADA', motivo_bloqueo = NULL, fecha_bloqueo = {ahora}
            WHERE id = {rubrica.Id}
            """);
        var ex3 = (await bloqueadaSinMotivo.Should().ThrowAsync<PostgresException>()).Which;
        ex3.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        ex3.ConstraintName.Should().Be("ck_rubricas_bloqueo_coherente");

        // 4. BORRADOR con motivo y fecha -> viola ck_rubricas_bloqueo_coherente
        var borradorConDatos = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.rubricas
            SET estado = 'BORRADOR', motivo_bloqueo = 'INICIO_EVALUACION', fecha_bloqueo = {ahora}
            WHERE id = {rubrica.Id}
            """);
        var ex4 = (await borradorConDatos.Should().ThrowAsync<PostgresException>()).Which;
        ex4.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        ex4.ConstraintName.Should().Be("ck_rubricas_bloqueo_coherente");

        // 5. PUBLICADA con motivo o fecha -> viola ck_rubricas_bloqueo_coherente
        var publicadaConDatos = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.rubricas
            SET estado = 'PUBLICADA', motivo_bloqueo = 'PRIMERA_CALIFICACION', fecha_bloqueo = {ahora}
            WHERE id = {rubrica.Id}
            """);
        var ex5 = (await publicadaConDatos.Should().ThrowAsync<PostgresException>()).Which;
        ex5.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        ex5.ConstraintName.Should().Be("ck_rubricas_bloqueo_coherente");

        // 6. Motivo inválido en estado BLOQUEADA -> viola ck_rubricas_motivo_bloqueo_valido
        var motivoInvalido = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE postulaciones.rubricas
            SET estado = 'BLOQUEADA', motivo_bloqueo = 'MOTIVO_INVALIDO', fecha_bloqueo = {ahora}
            WHERE id = {rubrica.Id}
            """);
        var ex6 = (await motivoInvalido.Should().ThrowAsync<PostgresException>()).Which;
        ex6.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        ex6.ConstraintName.Should().Be("ck_rubricas_motivo_bloqueo_valido");
    }

    [Fact]
    public async Task Debe_ProducirUnUnicoCambioEfectivoYTerminarSinError_Cuando_DosBloqueosConcurrentesSeEjecutanConBarrera()
    {
        var usuarioId = Guid.CreateVersion7();
        var fechaCreacion = new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);
        var fechaPublicacion = fechaCreacion.AddMinutes(5);
        var fechaHandler1 = new DateTimeOffset(2026, 11, 22, 13, 0, 0, TimeSpan.Zero);
        var fechaHandler2 = new DateTimeOffset(2026, 11, 22, 13, 5, 0, TimeSpan.Zero);
        Guid rubricaId;
        int versionPublicada;

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            var repoSemilla = new RubricaRepository(contextoSemilla);
            var rubrica = RubricaEvaluacion.Crear(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "Rúbrica Concurrencia Bloqueo",
                fechaCreacion,
                usuarioId);
            rubrica.AgregarCriterio(
                "Criterio Único",
                "Descripción",
                100.00m,
                0.00m,
                20.00m,
                fechaCreacion.AddMinutes(1),
                usuarioId);
            rubrica.Publicar(fechaPublicacion, usuarioId);
            rubricaId = rubrica.Id;
            versionPublicada = rubrica.Version;

            await repoSemilla.AgregarAsync(rubrica);
            await contextoSemilla.GuardarCambiosAsync();
        }

        var lecturaCompletada1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lecturaCompletada2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task barreraLecturas = Task.WhenAll(lecturaCompletada1.Task, lecturaCompletada2.Task);

        await using var contexto1 = _fixture.CrearContexto();
        await using var contexto2 = _fixture.CrearContexto();

        var handler1 = new BloquearRubricaCommandHandler(
            new RubricaRepository(contexto1),
            new UnitOfWorkConBarrera(contexto1, lecturaCompletada1, barreraLecturas),
            new ProveedorTiempoFijo(fechaHandler1));

        var handler2 = new BloquearRubricaCommandHandler(
            new RubricaRepository(contexto2),
            new UnitOfWorkConBarrera(contexto2, lecturaCompletada2, barreraLecturas),
            new ProveedorTiempoFijo(fechaHandler2));

        var resultados = await Task.WhenAll(
            Task.Run(() => handler1.HandleAsync(new BloquearRubricaCommand(rubricaId, MotivoBloqueoRubrica.InicioEvaluacion))),
            Task.Run(() => handler2.HandleAsync(new BloquearRubricaCommand(rubricaId, MotivoBloqueoRubrica.PrimeraCalificacion))));

        resultados.Should().HaveCount(2);
        resultados[0].Estado.Should().Be("BLOQUEADA");
        resultados[1].Estado.Should().Be("BLOQUEADA");
        resultados[0].SoloLectura.Should().BeTrue();
        resultados[1].SoloLectura.Should().BeTrue();
        resultados[0].MotivoBloqueo.Should().Be(resultados[1].MotivoBloqueo);
        resultados[0].FechaBloqueo.Should().Be(resultados[1].FechaBloqueo);

        await using var contextoVerificacion = _fixture.CrearContexto();
        var repoVerificacion = new RubricaRepository(contextoVerificacion);
        var rubricaFinal = await repoVerificacion.ObtenerPorIdAsync(rubricaId);

        rubricaFinal.Should().NotBeNull();
        rubricaFinal!.Estado.Should().Be(EstadoRubrica.Bloqueada);
        rubricaFinal.Version.Should().Be(versionPublicada + 1);
        rubricaFinal.MotivoBloqueo.Should().NotBeNull();
        rubricaFinal.FechaBloqueo.Should().NotBeNull();

        if (rubricaFinal.MotivoBloqueo == MotivoBloqueoRubrica.InicioEvaluacion)
        {
            rubricaFinal.FechaBloqueo.Should().Be(fechaHandler1);
        }
        else
        {
            rubricaFinal.MotivoBloqueo.Should().Be(MotivoBloqueoRubrica.PrimeraCalificacion);
            rubricaFinal.FechaBloqueo.Should().Be(fechaHandler2);
        }
    }

    private sealed class UnitOfWorkConBarrera : IUnitOfWork
    {
        private readonly IUnitOfWork _interno;
        private readonly TaskCompletionSource _lecturaPropiaLista;
        private readonly Task _barreraAmbosLeidos;

        public UnitOfWorkConBarrera(
            IUnitOfWork interno,
            TaskCompletionSource lecturaPropiaLista,
            Task barreraAmbosLeidos)
        {
            _interno = interno;
            _lecturaPropiaLista = lecturaPropiaLista;
            _barreraAmbosLeidos = barreraAmbosLeidos;
        }

        public async Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
        {
            _lecturaPropiaLista.TrySetResult();
            await _barreraAmbosLeidos.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            return await _interno.GuardarCambiosAsync(cancellationToken);
        }
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
}
