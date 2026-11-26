using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Infrastructure;
using Ascenso.Postulaciones.Infrastructure.Adapters;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class ConsultaEvaluacionesCoevaluadoresRepositoryTests
{
    private static PostulacionesDbContext CrearContexto()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    [Fact]
    public async Task Debe_LanzarExcepcion_Cuando_LosParametrosDelRepositorioSonInvalidos()
    {
        var ctorNulo = () => new ConsultaEvaluacionesCoevaluadoresRepository(null!);
        ctorNulo.Should().Throw<ArgumentNullException>();

        await using var contexto = CrearContexto();
        var repositorio = new ConsultaEvaluacionesCoevaluadoresRepository(contexto);

        var obtenerAsignacionPostulacionVacia =
            () => repositorio.ObtenerAsignacionVigenteEvaluadorAsync(Guid.Empty, Guid.NewGuid());
        var obtenerAsignacionEvaluadorVacio =
            () => repositorio.ObtenerAsignacionVigenteEvaluadorAsync(Guid.NewGuid(), Guid.Empty);
        var listarDefinitivasPostulacionVacia =
            () => repositorio.ListarDefinitivasDeCoevaluadoresAsync(Guid.Empty, Guid.NewGuid());
        var listarDefinitivasEvaluadorVacio =
            () => repositorio.ListarDefinitivasDeCoevaluadoresAsync(Guid.NewGuid(), Guid.Empty);

        await obtenerAsignacionPostulacionVacia.Should().ThrowAsync<ArgumentException>();
        await obtenerAsignacionEvaluadorVacio.Should().ThrowAsync<ArgumentException>();
        await listarDefinitivasPostulacionVacia.Should().ThrowAsync<ArgumentException>();
        await listarDefinitivasEvaluadorVacio.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Debe_ConstruirConsultasAsNoTrackingConIncludeFiltrosYOrden_Cuando_SeInspeccionanLasConsultas()
    {
        using var contexto = CrearContexto();
        var repositorio = new ConsultaEvaluacionesCoevaluadoresRepository(contexto);
        var postulacionId = Guid.NewGuid();
        var usuarioEvaluadorId = Guid.NewGuid();

        var consultaAsignacion = repositorio.ConsultarAsignacionVigenteEvaluador(
            postulacionId,
            usuarioEvaluadorId);
        var consultaCoevaluadores = repositorio.ConsultarDefinitivasDeCoevaluadores(
            postulacionId,
            usuarioEvaluadorId);

        var sqlAsignacion = consultaAsignacion.ToQueryString();
        sqlAsignacion.Should().Contain("postulaciones.asignaciones_evaluacion");
        sqlAsignacion.Should().Contain("postulacion_id");
        sqlAsignacion.Should().Contain("usuario_evaluador_id");
        sqlAsignacion.Should().Contain("REASIGNADA_POR_CONFLICTO");
        consultaAsignacion.Expression.ToString().Should().Contain("AsNoTracking");

        var sqlCoevaluadores = consultaCoevaluadores.ToQueryString();
        sqlCoevaluadores.Should().Contain("postulaciones.evaluaciones");
        sqlCoevaluadores.Should().Contain("calificaciones_criterio");
        sqlCoevaluadores.Should().Contain("LEFT JOIN");
        sqlCoevaluadores.Should().Contain("postulacion_id");
        sqlCoevaluadores.Should().Contain("evaluador_id");
        sqlCoevaluadores.Should().Contain("ENVIADA_DEFINITIVA");
        sqlCoevaluadores.Should().Contain("ORDER BY");
        sqlCoevaluadores.Should().Contain("fecha_envio");
        consultaCoevaluadores.Expression.ToString().Should().Contain("AsNoTracking");
    }

    [Fact]
    public void Debe_RegistrarRepositorioYHttpClientDeConvocatoriaConTimeoutConfigurado_Cuando_SeConfiguraInfraestructura()
    {
        var servicios = new ServiceCollection();
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiciosExternos:Plataforma:UrlBase"] = "http://localhost:5001",
                ["ServiciosExternos:Programas:UrlBase"] = "http://localhost:5002",
                ["ServiciosExternos:TimeoutSegundos"] = "5"
            })
            .Build();

        servicios.AgregarInfraestructuraPostulaciones(
            PostulacionesDbContextFactory.CadenaConexionPorDefecto,
            configuracion);

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IConsultaEvaluacionesCoevaluadoresRepository)
            && d.ImplementationType == typeof(ConsultaEvaluacionesCoevaluadoresRepository)
            && d.Lifetime == ServiceLifetime.Scoped);

        using var proveedor = servicios.BuildServiceProvider();
        using var scope = proveedor.CreateScope();

        var repositorio = scope.ServiceProvider.GetRequiredService<IConsultaEvaluacionesCoevaluadoresRepository>();
        repositorio.Should().BeOfType<ConsultaEvaluacionesCoevaluadoresRepository>();

        var clienteConvocatoria = scope.ServiceProvider.GetRequiredService<IConvocatoriaEvaluacionClient>();
        clienteConvocatoria.Should().BeOfType<ConvocatoriaEvaluacionClient>();

        var factory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = factory.CreateClient(nameof(IConvocatoriaEvaluacionClient));
        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Debe_UsarTimeoutPorDefectoDe3SegundosEnConvocatoriaEvaluacionClient_Cuando_TimeoutConfiguradoEsNoPositivo()
    {
        var servicios = new ServiceCollection();
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiciosExternos:Programas:UrlBase"] = "http://localhost:5002",
                ["ServiciosExternos:TimeoutSegundos"] = "0"
            })
            .Build();

        servicios.AgregarInfraestructuraPostulaciones(
            PostulacionesDbContextFactory.CadenaConexionPorDefecto,
            configuracion);

        using var proveedor = servicios.BuildServiceProvider();
        var factory = proveedor.GetRequiredService<IHttpClientFactory>();
        var httpClient = factory.CreateClient(nameof(IConvocatoriaEvaluacionClient));

        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(3));
    }
}
