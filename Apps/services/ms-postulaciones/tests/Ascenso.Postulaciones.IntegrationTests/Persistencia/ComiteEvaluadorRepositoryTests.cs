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

public sealed class ComiteEvaluadorRepositoryTests
{
    private static PostulacionesDbContext CrearContexto()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    [Fact]
    public async Task Debe_RastrearComiteYMiembrosComoAdded_Cuando_SeAgregaUnComite()
    {
        await using var contexto = CrearContexto();
        var repositorio = new ComiteEvaluadorRepository(contexto);
        var ahora = DateTimeOffset.UtcNow;
        var usuarioId = Guid.NewGuid();
        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Beca Excelencia 2026",
            Guid.NewGuid(),
            ahora,
            usuarioId);
        var miembroEvaluador = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            ahora.AddMinutes(1),
            usuarioId);

        await repositorio.AgregarAsync(comite);

        contexto.Entry(comite).State.Should().Be(EntityState.Added);
        contexto.Entry(miembroEvaluador).State.Should().Be(EntityState.Added);
        foreach (var miembro in comite.Miembros)
        {
            contexto.Entry(miembro).State.Should().Be(EntityState.Added);
        }
    }

    [Fact]
    public void Debe_TratarNuevoMiembroComoAdded_Cuando_ElComiteYaEstaRastreadoComoUnchanged()
    {
        using var contexto = CrearContexto();
        var ahora = DateTimeOffset.UtcNow;
        var usuarioId = Guid.NewGuid();
        var comite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Comité Beca Excelencia 2026",
            Guid.NewGuid(),
            ahora,
            usuarioId);

        contexto.Attach(comite);
        contexto.Entry(comite).State.Should().Be(EntityState.Unchanged);

        var nuevoMiembro = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            ahora.AddMinutes(5),
            usuarioId);

        contexto.ChangeTracker.DetectChanges();

        contexto.Entry(comite).State.Should().Be(EntityState.Modified);
        contexto.Entry(nuevoMiembro).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public async Task Debe_LanzarArgumentException_Cuando_LosParametrosDeConsultaSonInvalidos()
    {
        await using var contexto = CrearContexto();
        var repositorio = new ComiteEvaluadorRepository(contexto);

        var porId = () => repositorio.ObtenerPorIdAsync(Guid.Empty);
        var paraActualizar = () => repositorio.ObtenerParaActualizarPorIdAsync(Guid.Empty);
        var porProgramaSinPrograma = () => repositorio.ObtenerPorProgramaYConvocatoriaAsync(Guid.Empty, Guid.NewGuid());
        var porProgramaSinConvocatoria = () => repositorio.ObtenerPorProgramaYConvocatoriaAsync(Guid.NewGuid(), Guid.Empty);
        var existeSinPrograma = () => repositorio.ExistePorProgramaYConvocatoriaAsync(Guid.Empty, Guid.NewGuid());
        var existeSinConvocatoria = () => repositorio.ExistePorProgramaYConvocatoriaAsync(Guid.NewGuid(), Guid.Empty);
        var agregarNulo = () => repositorio.AgregarAsync(null!);

        await porId.Should().ThrowAsync<ArgumentException>();
        await paraActualizar.Should().ThrowAsync<ArgumentException>();
        await porProgramaSinPrograma.Should().ThrowAsync<ArgumentException>();
        await porProgramaSinConvocatoria.Should().ThrowAsync<ArgumentException>();
        await existeSinPrograma.Should().ThrowAsync<ArgumentException>();
        await existeSinConvocatoria.Should().ThrowAsync<ArgumentException>();
        await agregarNulo.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Debe_IncluirMiembrosOrdenadosYUsarTrackingSoloEnActualizacion_Cuando_SeInspeccionanLasConsultas()
    {
        using var contexto = CrearContexto();
        var repositorio = new ComiteEvaluadorRepository(contexto);
        var id = Guid.NewGuid();
        var programaId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();

        var porId = repositorio.ConsultarPorId(id);
        var paraActualizar = repositorio.ConsultarParaActualizarPorId(id);
        var porPrograma = repositorio.ConsultarPorProgramaYConvocatoria(programaId, convocatoriaId);

        var sqlPorId = porId.ToQueryString();
        sqlPorId.Should().Contain("postulaciones.comites_evaluadores");
        sqlPorId.Should().Contain("miembros_comite");
        sqlPorId.Should().Contain("LEFT JOIN");
        sqlPorId.Should().Contain("fecha_asignacion");

        var sqlParaActualizar = paraActualizar.ToQueryString();
        sqlParaActualizar.Should().Contain("postulaciones.comites_evaluadores");
        sqlParaActualizar.Should().Contain("miembros_comite");
        sqlParaActualizar.Should().Contain("LEFT JOIN");
        sqlParaActualizar.Should().Contain("fecha_asignacion");

        var sqlPorPrograma = porPrograma.ToQueryString();
        sqlPorPrograma.Should().Contain("postulaciones.comites_evaluadores");
        sqlPorPrograma.Should().Contain("miembros_comite");
        sqlPorPrograma.Should().Contain("programa_beca_id");
        sqlPorPrograma.Should().Contain("convocatoria_id");
        sqlPorPrograma.Should().Contain("fecha_asignacion");

        porId.Expression.ToString().Should().Contain("AsNoTracking");
        porPrograma.Expression.ToString().Should().Contain("AsNoTracking");
        paraActualizar.Expression.ToString().Should().NotContain("AsNoTracking");
    }

    [Fact]
    public void Debe_RegistrarComiteEvaluadorRepository_Cuando_SeConfiguraInfraestructura()
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
            d.ServiceType == typeof(IComiteEvaluadorRepository)
            && d.ImplementationType == typeof(ComiteEvaluadorRepository)
            && d.Lifetime == ServiceLifetime.Scoped);
    }
}
