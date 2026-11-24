using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Infrastructure;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class AsignacionEvaluacionRepositoryTests
{
    private static PostulacionesDbContext CrearContexto()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    private static (ComiteEvaluador Comite, AsignacionEvaluacion Asignacion1, AsignacionEvaluacion Asignacion2) CrearAsignacionesPrueba()
    {
        var ahora = new DateTimeOffset(2026, 11, 24, 15, 0, 0, TimeSpan.Zero);
        var fechaLimite = ahora.AddDays(7);
        var usuarioAsignaId = Guid.NewGuid();
        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Asignaciones 2026",
            Guid.NewGuid(),
            ahora,
            usuarioAsignaId);
        var miembro2 = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            ahora.AddMinutes(1),
            usuarioAsignaId);
        var postulacionId = Guid.NewGuid();

        var asignacion1 = AsignacionEvaluacion.Crear(
            comite,
            comite.Miembros.First().Id,
            postulacionId,
            ahora.AddMinutes(2),
            fechaLimite,
            usuarioAsignaId);
        var asignacion2 = AsignacionEvaluacion.Crear(
            comite,
            miembro2.Id,
            postulacionId,
            ahora.AddMinutes(3),
            fechaLimite,
            usuarioAsignaId);

        return (comite, asignacion1, asignacion2);
    }

    [Fact]
    public async Task Debe_RastrearAsignacionesComoAdded_Cuando_SeAgregaRango()
    {
        await using var contexto = CrearContexto();
        var repositorio = new AsignacionEvaluacionRepository(contexto);
        var (_, asignacion1, asignacion2) = CrearAsignacionesPrueba();

        await repositorio.AgregarRangoAsync([asignacion1, asignacion2]);

        contexto.Entry(asignacion1).State.Should().Be(EntityState.Added);
        contexto.Entry(asignacion2).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public async Task Debe_LanzarExcepcion_Cuando_LosParametrosSonInvalidos()
    {
        var ctorNulo = () => new AsignacionEvaluacionRepository(null!);
        ctorNulo.Should().Throw<ArgumentNullException>();

        await using var contexto = CrearContexto();
        var repositorio = new AsignacionEvaluacionRepository(contexto);

        var agregarNulo = () => repositorio.AgregarRangoAsync(null!);
        var agregarVacio = () => repositorio.AgregarRangoAsync([]);
        var vigentesVacio = () => repositorio.ListarVigentesPorPostulacionAsync(Guid.Empty);
        var porPostulacionVacio = () => repositorio.ListarPorPostulacionAsync(Guid.Empty);
        var cargaVacia = () => repositorio.ObtenerCargaPorMiembroAsync(Guid.Empty);
        var evaluadorVacio = () => repositorio.ListarPorEvaluadorAsync(Guid.Empty, null, 1, 10);
        var paginaInvalida = () => repositorio.ListarPorEvaluadorAsync(Guid.NewGuid(), null, 0, 10);
        var tamanoCero = () => repositorio.ListarPorEvaluadorAsync(Guid.NewGuid(), null, 1, 0);
        var tamanoExcesivo = () => repositorio.ListarPorEvaluadorAsync(Guid.NewGuid(), null, 1, 101);
        var estadoIndefinido = () => repositorio.ListarPorEvaluadorAsync(
            Guid.NewGuid(),
            (EstadoAsignacionEvaluacion)999,
            1,
            10);

        await agregarNulo.Should().ThrowAsync<ArgumentNullException>();
        await agregarVacio.Should().ThrowAsync<ArgumentException>();
        await vigentesVacio.Should().ThrowAsync<ArgumentException>();
        await porPostulacionVacio.Should().ThrowAsync<ArgumentException>();
        await cargaVacia.Should().ThrowAsync<ArgumentException>();
        await evaluadorVacio.Should().ThrowAsync<ArgumentException>();
        await paginaInvalida.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await tamanoCero.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await tamanoExcesivo.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await estadoIndefinido.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Debe_ConstruirConsultasSinTrackingConFiltrosOrdenYPaginacion_Cuando_SeInspeccionanLasConsultas()
    {
        using var contexto = CrearContexto();
        var repositorio = new AsignacionEvaluacionRepository(contexto);
        var postulacionId = Guid.NewGuid();
        var comiteId = Guid.NewGuid();
        var evaluadorId = Guid.NewGuid();

        var vigentes = repositorio.ConsultarVigentesPorPostulacion(postulacionId);
        var porPostulacion = repositorio.ConsultarPorPostulacion(postulacionId);
        var abiertasPorComite = repositorio.ConsultarAbiertasPorComite(comiteId);
        var porEvaluadorSinEstado = repositorio.ConsultarPorEvaluador(evaluadorId, null);
        var porEvaluadorConEstado = repositorio.ConsultarPorEvaluador(
            evaluadorId,
            EstadoAsignacionEvaluacion.Pendiente);
        var paginaEvaluador = repositorio.ConsultarPaginaPorEvaluador(
            evaluadorId,
            EstadoAsignacionEvaluacion.Pendiente,
            pagina: 2,
            tamano: 15);

        var sqlVigentes = vigentes.ToQueryString();
        sqlVigentes.Should().Contain("postulaciones.asignaciones_evaluacion");
        sqlVigentes.Should().Contain("postulacion_id");
        sqlVigentes.Should().Contain("REASIGNADA_POR_CONFLICTO");
        vigentes.Expression.ToString().Should().Contain("AsNoTracking");

        var sqlPorPostulacion = porPostulacion.ToQueryString();
        sqlPorPostulacion.Should().Contain("postulaciones.asignaciones_evaluacion");
        sqlPorPostulacion.Should().Contain("postulacion_id");
        sqlPorPostulacion.Should().Contain("ORDER BY");
        sqlPorPostulacion.Should().Contain("fecha_asignacion");
        porPostulacion.Expression.ToString().Should().Contain("AsNoTracking");

        var sqlAbiertas = abiertasPorComite.ToQueryString();
        sqlAbiertas.Should().Contain("postulaciones.asignaciones_evaluacion");
        sqlAbiertas.Should().Contain("comite_evaluador_id");
        sqlAbiertas.Should().Contain("PENDIENTE");
        sqlAbiertas.Should().Contain("EN_PROGRESO");
        abiertasPorComite.Expression.ToString().Should().Contain("AsNoTracking");

        var sqlEvaluadorSinEstado = porEvaluadorSinEstado.ToQueryString();
        sqlEvaluadorSinEstado.Should().Contain("usuario_evaluador_id");
        porEvaluadorSinEstado.Expression.ToString().Should().Contain("AsNoTracking");

        var sqlEvaluadorConEstado = porEvaluadorConEstado.ToQueryString();
        sqlEvaluadorConEstado.Should().Contain("usuario_evaluador_id");
        sqlEvaluadorConEstado.Should().Contain("PENDIENTE");

        var sqlPagina = paginaEvaluador.ToQueryString();
        sqlPagina.Should().Contain("ORDER BY");
        sqlPagina.Should().Contain("fecha_limite_evaluacion");
        sqlPagina.Should().Contain("LIMIT");
        sqlPagina.Should().Contain("OFFSET");
    }

    [Fact]
    public void Debe_RegistrarAsignacionEvaluacionRepository_Cuando_SeConfiguraInfraestructura()
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
            d.ServiceType == typeof(IAsignacionEvaluacionRepository)
            && d.ImplementationType == typeof(AsignacionEvaluacionRepository)
            && d.Lifetime == ServiceLifetime.Scoped);
    }
}
