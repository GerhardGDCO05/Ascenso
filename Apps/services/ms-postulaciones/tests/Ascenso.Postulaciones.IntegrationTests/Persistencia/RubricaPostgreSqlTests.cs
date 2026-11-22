using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
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
}
