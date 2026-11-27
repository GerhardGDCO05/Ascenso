using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Infrastructure;
using Ascenso.Postulaciones.Infrastructure.Adapters;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class ResultadoConsolidadoRepositoryTests
{
    private static PostulacionesDbContext CrearContexto()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    private static ResultadoConsolidadoRanking CrearResultadoConAlerta()
    {
        var calculo = new ResultadoCalculoConsolidacion(
            convocatoriaId: Guid.NewGuid(),
            postulacionId: Guid.NewGuid(),
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: 80.00m,
            diferenciaEntreEvaluadores: 25.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: true);

        return ResultadoConsolidadoRanking.Crear(
            calculo,
            new DateTimeOffset(2026, 11, 26, 10, 0, 0, TimeSpan.Zero),
            Guid.NewGuid());
    }

    [Fact]
    public async Task Debe_RastrearResultadoConsolidadoComoAdded_Cuando_SeAgregaUnResultado()
    {
        await using var contexto = CrearContexto();
        var repositorio = new ResultadoConsolidadoRepository(contexto);
        var resultado = CrearResultadoConAlerta();

        await repositorio.AgregarAsync(resultado);

        contexto.Entry(resultado).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public void Debe_DetectarActualizacionesYDescartarEntradasEnRecarga_Cuando_SeModificaResultadoRastreado()
    {
        using var contexto = CrearContexto();
        var repositorio = new ResultadoConsolidadoRepository(contexto);
        var resultado = CrearResultadoConAlerta();

        contexto.Attach(resultado);
        contexto.Entry(resultado).State.Should().Be(EntityState.Unchanged);

        resultado.MarcarDiscrepanciaRevisada(
            "Se verificaron las rúbricas de ambos evaluadores y se confirma la puntuación.",
            new DateTimeOffset(2026, 11, 26, 11, 0, 0, TimeSpan.Zero),
            Guid.NewGuid());

        contexto.ChangeTracker.DetectChanges();
        contexto.Entry(resultado).State.Should().Be(EntityState.Modified);

        repositorio.DescartarRastreoLocalPorPostulacion(resultado.PostulacionId);
        contexto.Entry(resultado).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task Debe_LanzarExcepcion_Cuando_LosParametrosDeLosRepositoriosSonInvalidos()
    {
        var ctorResultadoNulo = () => new ResultadoConsolidadoRepository(null!);
        var ctorDatosNulo = () => new ConsultaDatosConsolidacionRepository(null!);

        ctorResultadoNulo.Should().Throw<ArgumentNullException>();
        ctorDatosNulo.Should().Throw<ArgumentNullException>();

        await using var contexto = CrearContexto();
        var repoResultado = new ResultadoConsolidadoRepository(contexto);
        var repoDatos = new ConsultaDatosConsolidacionRepository(contexto);

        var obtenerVacio = () => repoResultado.ObtenerPorPostulacionAsync(Guid.Empty);
        var obtenerParaActualizarVacio = () => repoResultado.ObtenerParaActualizarPorPostulacionAsync(Guid.Empty);
        var recargarVacio = () => repoResultado.RecargarParaActualizarPorPostulacionAsync(Guid.Empty);
        var listarConvocatoriaVacia = () => repoResultado.ListarPorConvocatoriaAsync(Guid.Empty, null, null, 1, 10);
        var listarPaginaCero = () => repoResultado.ListarPorConvocatoriaAsync(Guid.NewGuid(), null, null, 0, 10);
        var listarTamanoCero = () => repoResultado.ListarPorConvocatoriaAsync(Guid.NewGuid(), null, null, 1, 0);
        var listarTamanoExcesivo = () => repoResultado.ListarPorConvocatoriaAsync(Guid.NewGuid(), null, null, 1, 101);
        var agregarNulo = () => repoResultado.AgregarAsync(null!);

        var asignacionesPostulacionVacia = () => repoDatos.ListarAsignacionesVigentesPorPostulacionAsync(Guid.Empty);
        var evaluacionesPostulacionVacia = () => repoDatos.ListarEvaluacionesDefinitivasPorPostulacionAsync(Guid.Empty);
        var excepcionPostulacionVacia = () => repoDatos.ObtenerExcepcionEvaluadorUnicoPorPostulacionAsync(Guid.Empty);

        await obtenerVacio.Should().ThrowAsync<ArgumentException>();
        await obtenerParaActualizarVacio.Should().ThrowAsync<ArgumentException>();
        await recargarVacio.Should().ThrowAsync<ArgumentException>();
        await listarConvocatoriaVacia.Should().ThrowAsync<ArgumentException>();
        await listarPaginaCero.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await listarTamanoCero.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await listarTamanoExcesivo.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await agregarNulo.Should().ThrowAsync<ArgumentNullException>();

        await asignacionesPostulacionVacia.Should().ThrowAsync<ArgumentException>();
        await evaluacionesPostulacionVacia.Should().ThrowAsync<ArgumentException>();
        await excepcionPostulacionVacia.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Debe_ConstruirConsultasConFiltrosOrdenTrackingYSinJoinInnecesario_Cuando_SeInspeccionanLasConsultas()
    {
        using var contexto = CrearContexto();
        var repoResultado = new ResultadoConsolidadoRepository(contexto);
        var repoDatos = new ConsultaDatosConsolidacionRepository(contexto);

        var postulacionId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();

        var porPostulacion = repoResultado.ConsultarPorPostulacion(postulacionId);
        var paraActualizarPorPostulacion = repoResultado.ConsultarParaActualizarPorPostulacion(postulacionId);
        var porConvocatoriaConFiltros = repoResultado.ConsultarPorConvocatoria(
            convocatoriaId,
            alertaDiscrepancia: true,
            discrepanciaRevisada: false);
        var paginaPorConvocatoria = repoResultado.ConsultarPaginaPorConvocatoria(
            convocatoriaId,
            alertaDiscrepancia: true,
            discrepanciaRevisada: false,
            pagina: 2,
            tamano: 15);

        porPostulacion.ToQueryString().Should().Contain("postulaciones.resultados_consolidados_ranking");
        porPostulacion.Expression.ToString().Should().Contain("AsNoTracking");

        paraActualizarPorPostulacion.ToQueryString().Should().Contain("postulaciones.resultados_consolidados_ranking");
        paraActualizarPorPostulacion.Expression.ToString().Should().NotContain("AsNoTracking");

        var sqlFiltrado = porConvocatoriaConFiltros.ToQueryString();
        sqlFiltrado.Should().Contain("convocatoria_id");
        sqlFiltrado.Should().Contain("alerta_discrepancia");
        sqlFiltrado.Should().Contain("discrepancia_revisada");
        porConvocatoriaConFiltros.Expression.ToString().Should().Contain("AsNoTracking");

        var sqlPagina = paginaPorConvocatoria.ToQueryString();
        sqlPagina.Should().Contain("ORDER BY");
        sqlPagina.Should().Contain("puntaje_consolidado");
        sqlPagina.Should().Contain("fecha_consolidacion");
        sqlPagina.Should().Contain("LIMIT");
        sqlPagina.Should().Contain("OFFSET");
        paginaPorConvocatoria.Expression.ToString().Should().Contain("AsNoTracking");

        var asignacionesVigentes = repoDatos.ConsultarAsignacionesVigentesPorPostulacion(postulacionId);
        var sqlAsignaciones = asignacionesVigentes.ToQueryString();
        sqlAsignaciones.Should().Contain("postulaciones.asignaciones_evaluacion");
        sqlAsignaciones.Should().Contain("REASIGNADA_POR_CONFLICTO");
        asignacionesVigentes.Expression.ToString().Should().Contain("AsNoTracking");

        var evaluacionesDefinitivas = repoDatos.ConsultarEvaluacionesDefinitivasPorPostulacion(postulacionId);
        var sqlEvaluaciones = evaluacionesDefinitivas.ToQueryString();
        sqlEvaluaciones.Should().Contain("postulaciones.evaluaciones");
        sqlEvaluaciones.Should().Contain("ENVIADA_DEFINITIVA");
        sqlEvaluaciones.Should().NotContain("calificaciones_criterio");
        sqlEvaluaciones.Should().Contain("ORDER BY");
        sqlEvaluaciones.Should().Contain("fecha_envio");
        evaluacionesDefinitivas.Expression.ToString().Should().Contain("AsNoTracking");

        var excepcion = repoDatos.ConsultarExcepcionEvaluadorUnicoPorPostulacion(postulacionId);
        excepcion.ToQueryString().Should().Contain("postulaciones.excepciones_evaluador_unico");
        excepcion.Expression.ToString().Should().Contain("AsNoTracking");
    }

    [Fact]
    public void Debe_RegistrarRepositoriosYProveedorDeConsolidacion_Cuando_SeConfiguraInfraestructura()
    {
        var servicios = new ServiceCollection();
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiciosExternos:Plataforma:UrlBase"] = "http://localhost:5001",
                ["ServiciosExternos:Programas:UrlBase"] = "http://localhost:5002",
                ["ServiciosExternos:TimeoutSegundos"] = "3",
                ["Consolidacion:EvaluadoresMinimosRequeridos"] = "3",
                ["Consolidacion:UmbralDiscrepanciaPuntos"] = "18.50"
            })
            .Build();

        servicios.AgregarInfraestructuraPostulaciones(
            PostulacionesDbContextFactory.CadenaConexionPorDefecto,
            configuracion);

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IResultadoConsolidadoRepository)
            && d.ImplementationType == typeof(ResultadoConsolidadoRepository)
            && d.Lifetime == ServiceLifetime.Scoped);

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IConsultaDatosConsolidacionRepository)
            && d.ImplementationType == typeof(ConsultaDatosConsolidacionRepository)
            && d.Lifetime == ServiceLifetime.Scoped);

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IProveedorParametrosConsolidacion)
            && d.ImplementationType == typeof(ProveedorParametrosConsolidacionConfigurado)
            && d.Lifetime == ServiceLifetime.Scoped);

        using var proveedor = servicios.BuildServiceProvider();
        var opciones = proveedor.GetRequiredService<IOptions<ConsolidacionOptions>>().Value;
        opciones.EvaluadoresMinimosRequeridos.Should().Be(3);
        opciones.UmbralDiscrepanciaPuntos.Should().Be(18.50m);
    }
}
