using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Infrastructure;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class RubricaRepositoryTests
{
    private static PostulacionesDbContext CrearContexto()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    [Fact]
    public async Task Debe_RastrearRubricaYCriteriosComoAdded_Cuando_SeAgregaUnaRubrica()
    {
        await using var contexto = CrearContexto();
        var repositorio = new RubricaRepository(contexto);
        var ahora = DateTimeOffset.UtcNow;
        var usuarioId = Guid.NewGuid();
        var rubrica = RubricaEvaluacion.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Rúbrica Mérito 2026",
            ahora,
            usuarioId);
        var criterio = rubrica.AgregarCriterio(
            "Promedio académico",
            "Evaluación del promedio ponderado",
            40.00m,
            0.00m,
            20.00m,
            ahora,
            usuarioId);

        await repositorio.AgregarAsync(rubrica);

        contexto.Entry(rubrica).State.Should().Be(EntityState.Added);
        contexto.Entry(criterio).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public void Debe_TratarNuevoCriterioComoAdded_Cuando_LaRubricaYaEstaRastreadaComoUnchanged()
    {
        using var contexto = CrearContexto();
        var ahora = DateTimeOffset.UtcNow;
        var usuarioId = Guid.NewGuid();
        var rubrica = RubricaEvaluacion.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Rúbrica Mérito 2026",
            ahora,
            usuarioId);

        contexto.Attach(rubrica);
        contexto.Entry(rubrica).State.Should().Be(EntityState.Unchanged);

        var nuevoCriterio = rubrica.AgregarCriterio(
            "Entrevista personal",
            "Evaluación de competencias blandas",
            60.00m,
            0.00m,
            100.00m,
            ahora.AddMinutes(5),
            usuarioId);

        contexto.ChangeTracker.DetectChanges();

        contexto.Entry(rubrica).State.Should().Be(EntityState.Modified);
        contexto.Entry(nuevoCriterio).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public async Task Debe_LanzarArgumentException_Cuando_LosParametrosDeConsultaSonInvalidos()
    {
        await using var contexto = CrearContexto();
        var repositorio = new RubricaRepository(contexto);

        var porId = () => repositorio.ObtenerPorIdAsync(Guid.Empty);
        var paraActualizar = () => repositorio.ObtenerParaActualizarPorIdAsync(Guid.Empty);
        var existeSinPrograma = () => repositorio.ExistePorProgramaYConvocatoriaAsync(Guid.Empty, Guid.NewGuid());
        var existeSinConvocatoria = () => repositorio.ExistePorProgramaYConvocatoriaAsync(Guid.NewGuid(), Guid.Empty);
        var agregarNulo = () => repositorio.AgregarAsync(null!);

        await porId.Should().ThrowAsync<ArgumentException>();
        await paraActualizar.Should().ThrowAsync<ArgumentException>();
        await existeSinPrograma.Should().ThrowAsync<ArgumentException>();
        await existeSinConvocatoria.Should().ThrowAsync<ArgumentException>();
        await agregarNulo.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Debe_IncluirCriteriosYUsarTrackingSoloEnActualizacion_Cuando_SeInspeccionanLasConsultas()
    {
        using var contexto = CrearContexto();
        var repositorio = new RubricaRepository(contexto);
        var id = Guid.NewGuid();

        var porId = repositorio.ConsultarPorId(id);
        var paraActualizar = repositorio.ConsultarParaActualizarPorId(id);

        porId.ToQueryString().Should().Contain("postulaciones.rubricas");
        porId.ToQueryString().Should().Contain("criterios_evaluacion");
        porId.ToQueryString().Should().Contain("LEFT JOIN");

        paraActualizar.ToQueryString().Should().Contain("postulaciones.rubricas");
        paraActualizar.ToQueryString().Should().Contain("criterios_evaluacion");
        paraActualizar.ToQueryString().Should().Contain("LEFT JOIN");

        porId.Expression.ToString().Should().Contain("AsNoTracking");
        paraActualizar.Expression.ToString().Should().NotContain("AsNoTracking");
    }

    [Fact]
    public void Debe_RegistrarRubricaRepository_Cuando_SeConfiguraInfraestructura()
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
            d.ServiceType == typeof(IRubricaRepository)
            && d.ImplementationType == typeof(RubricaRepository)
            && d.Lifetime == ServiceLifetime.Scoped);
    }
}
