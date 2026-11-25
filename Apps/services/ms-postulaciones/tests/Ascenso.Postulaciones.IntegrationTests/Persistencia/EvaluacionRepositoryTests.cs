using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using Ascenso.Postulaciones.Infrastructure;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class EvaluacionRepositoryTests
{
    private static PostulacionesDbContext CrearContexto()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    private static (
        RubricaEvaluacion Rubrica,
        CriterioEvaluacion Criterio1,
        CriterioEvaluacion Criterio2,
        CriterioEvaluacion Criterio3,
        AsignacionEvaluacion Asignacion,
        Guid UsuarioEvaluadorId,
        DateTimeOffset Ahora) CrearEscenarioBase()
    {
        var ahora = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
        var programaBecaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var coordinadorId = Guid.NewGuid();
        var evaluadorId = Guid.NewGuid();

        var rubrica = RubricaEvaluacion.Crear(
            programaBecaId,
            convocatoriaId,
            "Rúbrica Evaluaciones 2026",
            ahora,
            coordinadorId);
        var criterio1 = rubrica.AgregarCriterio(
            "Promedio académico",
            "Rendimiento académico",
            40.00m,
            0.00m,
            20.00m,
            ahora.AddMinutes(1),
            coordinadorId);
        var criterio2 = rubrica.AgregarCriterio(
            "Situación socioeconómica",
            "Vulnerabilidad económica",
            30.00m,
            0.00m,
            10.00m,
            ahora.AddMinutes(2),
            coordinadorId);
        var criterio3 = rubrica.AgregarCriterio(
            "Proyección social",
            "Participación comunitaria",
            30.00m,
            1.00m,
            5.00m,
            ahora.AddMinutes(3),
            coordinadorId);
        rubrica.Publicar(ahora.AddMinutes(4), coordinadorId);

        var comite = ComiteEvaluador.Crear(
            programaBecaId,
            convocatoriaId,
            "Comité Evaluador 2026",
            evaluadorId,
            ahora.AddMinutes(5),
            coordinadorId);
        comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            ahora.AddMinutes(6),
            coordinadorId);

        var miembroEvaluador = comite.Miembros.First(m => m.UsuarioEvaluadorId == evaluadorId);
        var asignacion = AsignacionEvaluacion.Crear(
            comite,
            miembroEvaluador.Id,
            Guid.NewGuid(),
            ahora.AddMinutes(7),
            ahora.AddDays(7),
            coordinadorId);

        return (rubrica, criterio1, criterio2, criterio3, asignacion, evaluadorId, ahora.AddMinutes(10));
    }

    [Fact]
    public async Task Debe_RastrearEvaluacionYCalificacionesComoAdded_Cuando_SeAgregaUnaEvaluacion()
    {
        await using var contexto = CrearContexto();
        var repositorio = new EvaluacionRepository(contexto);
        var (rubrica, criterio1, criterio2, _, asignacion, evaluadorId, ahora) = CrearEscenarioBase();

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [
                new DatosCalificacionCriterio(criterio1.Id, 18.00m, "Muy buen promedio"),
                new DatosCalificacionCriterio(criterio2.Id, 8.50m, null)
            ],
            "Borrador inicial",
            ahora,
            evaluadorId);

        await repositorio.AgregarAsync(evaluacion);

        contexto.Entry(evaluacion).State.Should().Be(EntityState.Added);
        foreach (var calificacion in evaluacion.Calificaciones)
        {
            contexto.Entry(calificacion).State.Should().Be(EntityState.Added);
        }
    }

    [Fact]
    public void Debe_DetectarAltasActualizacionesYEliminacionesDeCalificacionesConValueGeneratedNever_Cuando_SeActualizaBorradorRastreado()
    {
        using var contexto = CrearContexto();
        var (rubrica, criterio1, criterio2, criterio3, asignacion, evaluadorId, ahora) = CrearEscenarioBase();

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [
                new DatosCalificacionCriterio(criterio1.Id, 15.00m, "Inicial C1"),
                new DatosCalificacionCriterio(criterio2.Id, 7.00m, "Inicial C2")
            ],
            "Comentarios iniciales",
            ahora,
            evaluadorId);

        var calificacionCriterio1Existente = evaluacion.Calificaciones.Single(c => c.CriterioEvaluacionId == criterio1.Id);
        var calificacionCriterio2Removida = evaluacion.Calificaciones.Single(c => c.CriterioEvaluacionId == criterio2.Id);

        contexto.Attach(evaluacion);
        contexto.Entry(evaluacion).State.Should().Be(EntityState.Unchanged);
        contexto.Entry(calificacionCriterio1Existente).State.Should().Be(EntityState.Unchanged);
        contexto.Entry(calificacionCriterio2Removida).State.Should().Be(EntityState.Unchanged);

        evaluacion.ActualizarBorrador(
            rubrica,
            [
                new DatosCalificacionCriterio(criterio1.Id, 19.50m, "Actualizado C1"),
                new DatosCalificacionCriterio(criterio3.Id, 4.50m, "Nuevo C3")
            ],
            "Comentarios actualizados",
            asignacion.FechaLimiteEvaluacion,
            ahora.AddMinutes(15),
            evaluadorId);

        contexto.ChangeTracker.DetectChanges();

        var calificacionCriterio3Nueva = evaluacion.Calificaciones.Single(c => c.CriterioEvaluacionId == criterio3.Id);

        contexto.Entry(evaluacion).State.Should().Be(EntityState.Modified);
        contexto.Entry(calificacionCriterio1Existente).State.Should().Be(EntityState.Modified);
        contexto.Entry(calificacionCriterio3Nueva).State.Should().Be(EntityState.Added);
        contexto.Entry(calificacionCriterio2Removida).State.Should().Be(EntityState.Deleted);
    }

    [Fact]
    public async Task Debe_LanzarExcepcion_Cuando_LosParametrosDeLosRepositoriosSonInvalidos()
    {
        var ctorEvaluacionNulo = () => new EvaluacionRepository(null!);
        var ctorAsignacionNulo = () => new ConsultaAsignacionParaEvaluacionRepository(null!);
        var ctorRubricaNulo = () => new ConsultaRubricaParaEvaluacionRepository(null!);

        ctorEvaluacionNulo.Should().Throw<ArgumentNullException>();
        ctorAsignacionNulo.Should().Throw<ArgumentNullException>();
        ctorRubricaNulo.Should().Throw<ArgumentNullException>();

        await using var contexto = CrearContexto();
        var repoEvaluacion = new EvaluacionRepository(contexto);
        var repoAsignacion = new ConsultaAsignacionParaEvaluacionRepository(contexto);
        var repoRubrica = new ConsultaRubricaParaEvaluacionRepository(contexto);

        var obtenerAsignacionVacia = () => repoEvaluacion.ObtenerPorAsignacionAsync(Guid.Empty);
        var obtenerParaActualizarVacia = () => repoEvaluacion.ObtenerParaActualizarPorAsignacionAsync(Guid.Empty);
        var listarEvaluadorVacio = () => repoEvaluacion.ListarPorEvaluadorAsync(Guid.Empty, null, 1, 10);
        var listarEstadoIndefinido = () => repoEvaluacion.ListarPorEvaluadorAsync(
            Guid.NewGuid(),
            (EstadoEvaluacion)999,
            1,
            10);
        var listarPaginaCero = () => repoEvaluacion.ListarPorEvaluadorAsync(Guid.NewGuid(), null, 0, 10);
        var listarTamanoCero = () => repoEvaluacion.ListarPorEvaluadorAsync(Guid.NewGuid(), null, 1, 0);
        var listarTamanoExcesivo = () => repoEvaluacion.ListarPorEvaluadorAsync(Guid.NewGuid(), null, 1, 101);
        var definitivasPostulacionVacia = () => repoEvaluacion.ListarDefinitivasPorPostulacionAsync(Guid.Empty);
        var agregarNulo = () => repoEvaluacion.AgregarAsync(null!);

        var asignacionPorIdVacio = () => repoAsignacion.ObtenerPorIdAsync(Guid.Empty);
        var asignacionParaActualizarVacio = () => repoAsignacion.ObtenerParaActualizarPorIdAsync(Guid.Empty);

        var rubricaSinPrograma = () => repoRubrica.ObtenerPorProgramaYConvocatoriaAsync(Guid.Empty, Guid.NewGuid());
        var rubricaSinConvocatoria = () => repoRubrica.ObtenerPorProgramaYConvocatoriaAsync(Guid.NewGuid(), Guid.Empty);

        await obtenerAsignacionVacia.Should().ThrowAsync<ArgumentException>();
        await obtenerParaActualizarVacia.Should().ThrowAsync<ArgumentException>();
        await listarEvaluadorVacio.Should().ThrowAsync<ArgumentException>();
        await listarEstadoIndefinido.Should().ThrowAsync<ArgumentException>();
        await listarPaginaCero.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await listarTamanoCero.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await listarTamanoExcesivo.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await definitivasPostulacionVacia.Should().ThrowAsync<ArgumentException>();
        await agregarNulo.Should().ThrowAsync<ArgumentNullException>();

        await asignacionPorIdVacio.Should().ThrowAsync<ArgumentException>();
        await asignacionParaActualizarVacio.Should().ThrowAsync<ArgumentException>();

        await rubricaSinPrograma.Should().ThrowAsync<ArgumentException>();
        await rubricaSinConvocatoria.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Debe_ConstruirConsultasConIncludeFiltrosOrdenYTrackingAdecuado_Cuando_SeInspeccionanLasConsultas()
    {
        using var contexto = CrearContexto();
        var repoEvaluacion = new EvaluacionRepository(contexto);
        var repoAsignacion = new ConsultaAsignacionParaEvaluacionRepository(contexto);
        var repoRubrica = new ConsultaRubricaParaEvaluacionRepository(contexto);

        var asignacionId = Guid.NewGuid();
        var evaluadorId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();

        var porAsignacion = repoEvaluacion.ConsultarPorAsignacion(asignacionId);
        var paraActualizarPorAsignacion = repoEvaluacion.ConsultarParaActualizarPorAsignacion(asignacionId);
        var porEvaluadorSinEstado = repoEvaluacion.ConsultarPorEvaluador(evaluadorId, null);
        var porEvaluadorConEstado = repoEvaluacion.ConsultarPorEvaluador(evaluadorId, EstadoEvaluacion.Borrador);
        var paginaPorEvaluador = repoEvaluacion.ConsultarPaginaPorEvaluador(
            evaluadorId,
            EstadoEvaluacion.EnviadaDefinitiva,
            pagina: 2,
            tamano: 10);
        var definitivasPorPostulacion = repoEvaluacion.ConsultarDefinitivasPorPostulacion(postulacionId);

        var sqlPorAsignacion = porAsignacion.ToQueryString();
        sqlPorAsignacion.Should().Contain("postulaciones.evaluaciones");
        sqlPorAsignacion.Should().Contain("calificaciones_criterio");
        sqlPorAsignacion.Should().Contain("LEFT JOIN");
        porAsignacion.Expression.ToString().Should().Contain("AsNoTracking");

        var sqlParaActualizar = paraActualizarPorAsignacion.ToQueryString();
        sqlParaActualizar.Should().Contain("postulaciones.evaluaciones");
        sqlParaActualizar.Should().Contain("calificaciones_criterio");
        sqlParaActualizar.Should().Contain("LEFT JOIN");
        paraActualizarPorAsignacion.Expression.ToString().Should().NotContain("AsNoTracking");

        var sqlEvaluadorSinEstado = porEvaluadorSinEstado.ToQueryString();
        sqlEvaluadorSinEstado.Should().Contain("evaluador_id");
        porEvaluadorSinEstado.Expression.ToString().Should().Contain("AsNoTracking");

        var sqlEvaluadorConEstado = porEvaluadorConEstado.ToQueryString();
        sqlEvaluadorConEstado.Should().Contain("evaluador_id");
        sqlEvaluadorConEstado.Should().Contain("BORRADOR");

        var sqlPagina = paginaPorEvaluador.ToQueryString();
        sqlPagina.Should().Contain("calificaciones_criterio");
        sqlPagina.Should().Contain("ORDER BY");
        sqlPagina.Should().Contain("fecha_ultima_modificacion");
        sqlPagina.Should().Contain("LIMIT");
        sqlPagina.Should().Contain("OFFSET");
        paginaPorEvaluador.Expression.ToString().Should().Contain("AsNoTracking");

        var sqlDefinitivas = definitivasPorPostulacion.ToQueryString();
        sqlDefinitivas.Should().Contain("postulacion_id");
        sqlDefinitivas.Should().Contain("BORRADOR");
        sqlDefinitivas.Should().Contain("calificaciones_criterio");
        sqlDefinitivas.Should().Contain("ORDER BY");
        sqlDefinitivas.Should().Contain("fecha_envio");
        definitivasPorPostulacion.Expression.ToString().Should().Contain("AsNoTracking");

        var asignacionLectura = repoAsignacion.ConsultarPorId(asignacionId);
        var asignacionActualizacion = repoAsignacion.ConsultarParaActualizarPorId(asignacionId);
        asignacionLectura.ToQueryString().Should().Contain("postulaciones.asignaciones_evaluacion");
        asignacionLectura.Expression.ToString().Should().Contain("AsNoTracking");
        asignacionActualizacion.ToQueryString().Should().Contain("postulaciones.asignaciones_evaluacion");
        asignacionActualizacion.Expression.ToString().Should().NotContain("AsNoTracking");

        var rubricaPorProgramaConvocatoria = repoRubrica.ConsultarPorProgramaYConvocatoria(programaId, convocatoriaId);
        var sqlRubrica = rubricaPorProgramaConvocatoria.ToQueryString();
        sqlRubrica.Should().Contain("postulaciones.rubricas");
        sqlRubrica.Should().Contain("criterios_evaluacion");
        sqlRubrica.Should().Contain("LEFT JOIN");
        rubricaPorProgramaConvocatoria.Expression.ToString().Should().Contain("AsNoTracking");
    }

    [Fact]
    public void Debe_RegistrarRepositoriosDeEvaluacion_Cuando_SeConfiguraInfraestructura()
    {
        var servicios = new ServiceCollection();
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiciosExternos:Plataforma:UrlBase"] = "http://localhost:5001",
                ["ServiciosExternos:Programas:UrlBase"] = "http://localhost:5002",
                ["ServiciosExternos:TimeoutSegundos"] = "3"
            })
            .Build();

        servicios.AgregarInfraestructuraPostulaciones(
            PostulacionesDbContextFactory.CadenaConexionPorDefecto,
            configuracion);

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IEvaluacionRepository)
            && d.ImplementationType == typeof(EvaluacionRepository)
            && d.Lifetime == ServiceLifetime.Scoped);

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IConsultaAsignacionParaEvaluacionRepository)
            && d.ImplementationType == typeof(ConsultaAsignacionParaEvaluacionRepository)
            && d.Lifetime == ServiceLifetime.Scoped);

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IConsultaRubricaParaEvaluacionRepository)
            && d.ImplementationType == typeof(ConsultaRubricaParaEvaluacionRepository)
            && d.Lifetime == ServiceLifetime.Scoped);
    }
}
