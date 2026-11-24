using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Infrastructure;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class ExcepcionEvaluadorUnicoRepositoryTests
{
    private static PostulacionesDbContext CrearContexto()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    private static AutorizacionExcepcionEvaluadorUnico CrearAutorizacionPrueba(Guid? postulacionId = null)
        => AutorizacionExcepcionEvaluadorUnico.Crear(
            postulacionId ?? Guid.NewGuid(),
            "Único especialista disponible en el área de biotecnología avanzada.",
            new DateTimeOffset(2026, 11, 24, 16, 0, 0, TimeSpan.Zero),
            Guid.NewGuid(),
            Guid.NewGuid());

    [Fact]
    public async Task Debe_RastrearAutorizacionComoAdded_Cuando_SeAgrega()
    {
        await using var contexto = CrearContexto();
        var repositorio = new ExcepcionEvaluadorUnicoRepository(contexto);
        var autorizacion = CrearAutorizacionPrueba();

        await repositorio.AgregarAsync(autorizacion);

        contexto.Entry(autorizacion).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public async Task Debe_LanzarExcepcion_Cuando_LosParametrosSonInvalidos()
    {
        var ctorNulo = () => new ExcepcionEvaluadorUnicoRepository(null!);
        ctorNulo.Should().Throw<ArgumentNullException>();

        await using var contexto = CrearContexto();
        var repositorio = new ExcepcionEvaluadorUnicoRepository(contexto);

        var agregarNulo = () => repositorio.AgregarAsync(null!);
        var porPostulacionVacio = () => repositorio.ObtenerPorPostulacionAsync(Guid.Empty);

        await agregarNulo.Should().ThrowAsync<ArgumentNullException>();
        await porPostulacionVacio.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Debe_ConstruirConsultaSinTrackingPorPostulacion_Cuando_SeInspeccionaLaConsulta()
    {
        using var contexto = CrearContexto();
        var repositorio = new ExcepcionEvaluadorUnicoRepository(contexto);
        var postulacionId = Guid.NewGuid();

        var consulta = repositorio.ConsultarPorPostulacion(postulacionId);
        var sql = consulta.ToQueryString();

        sql.Should().Contain("postulaciones.excepciones_evaluador_unico");
        sql.Should().Contain("postulacion_id");
        consulta.Expression.ToString().Should().Contain("AsNoTracking");
    }

    [Fact]
    public void Debe_RegistrarExcepcionEvaluadorUnicoRepository_Cuando_SeConfiguraInfraestructura()
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
            d.ServiceType == typeof(IExcepcionEvaluadorUnicoRepository)
            && d.ImplementationType == typeof(ExcepcionEvaluadorUnicoRepository)
            && d.Lifetime == ServiceLifetime.Scoped);
    }
}
