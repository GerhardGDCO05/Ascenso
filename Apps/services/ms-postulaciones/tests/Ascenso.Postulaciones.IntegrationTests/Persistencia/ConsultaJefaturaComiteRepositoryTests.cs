using System.Reflection;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Infrastructure;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class ConsultaJefaturaComiteRepositoryTests
{
    private static PostulacionesDbContext CrearContexto()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    [Fact]
    public async Task Debe_LanzarExcepcion_Cuando_LosArgumentosSonInvalidos()
    {
        var ctorNulo = () => new ConsultaJefaturaComiteRepository(null!);
        ctorNulo.Should().Throw<ArgumentNullException>();

        await using var contexto = CrearContexto();
        var repositorio = new ConsultaJefaturaComiteRepository(contexto);

        var convocatoriaVacia = () => repositorio.EsJefaturaActivaEnConvocatoriaAsync(Guid.Empty, Guid.NewGuid());
        var usuarioVacio = () => repositorio.EsJefaturaActivaEnConvocatoriaAsync(Guid.NewGuid(), Guid.Empty);

        await convocatoriaVacia.Should().ThrowAsync<ArgumentException>();
        await usuarioVacio.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Debe_ConstruirConsultaAsNoTrackingFiltrandoConvocatoriaActivoYJefatura_Cuando_SeInspeccionaLaConsulta()
    {
        using var contexto = CrearContexto();
        var repositorio = new ConsultaJefaturaComiteRepository(contexto);
        var convocatoriaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var consulta = repositorio.ConsultarComiteConJefaturaActiva(convocatoriaId, usuarioId);
        var sql = consulta.ToQueryString();

        sql.Should().Contain("postulaciones.comites_evaluadores");
        sql.Should().Contain("convocatoria_id");
        sql.Should().Contain("activo");
        sql.Should().Contain("jefatura_usuario_id");
        consulta.Expression.ToString().Should().Contain("AsNoTracking");
    }

    [Fact]
    public void Debe_EvaluarJefaturaActivaEnConvocatoria_SegunConvocatoriaJefaturaYEstadoActivo()
    {
        var ahora = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
        var convocatoriaId = Guid.NewGuid();
        var otraConvocatoriaId = Guid.NewGuid();
        var jefaturaUsuarioId = Guid.NewGuid();
        var miembroRegularId = Guid.NewGuid();

        var comiteActivo = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            convocatoriaId,
            "Comité Activo Convocatoria",
            jefaturaUsuarioId,
            ahora,
            jefaturaUsuarioId);
        comiteActivo.AgregarMiembro(
            miembroRegularId,
            TipoEvaluador.Interno,
            ahora.AddMinutes(5),
            jefaturaUsuarioId);

        var comiteOtraConvocatoria = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            otraConvocatoriaId,
            "Comité Otra Convocatoria",
            jefaturaUsuarioId,
            ahora,
            jefaturaUsuarioId);

        var comiteInactivo = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            convocatoriaId,
            "Comité Inactivo",
            Guid.NewGuid(),
            ahora,
            jefaturaUsuarioId);
        typeof(ComiteEvaluador)
            .GetProperty(nameof(ComiteEvaluador.Activo), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(comiteInactivo, false);

        var fuente = new[] { comiteActivo, comiteOtraConvocatoria, comiteInactivo }.AsQueryable();

        ConsultaJefaturaComiteRepository
            .FiltrarComiteConJefaturaActiva(fuente, convocatoriaId, jefaturaUsuarioId)
            .Any()
            .Should()
            .BeTrue();

        ConsultaJefaturaComiteRepository
            .FiltrarComiteConJefaturaActiva(fuente, convocatoriaId, miembroRegularId)
            .Any()
            .Should()
            .BeFalse();

        ConsultaJefaturaComiteRepository
            .FiltrarComiteConJefaturaActiva(new[] { comiteOtraConvocatoria }.AsQueryable(), convocatoriaId, jefaturaUsuarioId)
            .Any()
            .Should()
            .BeFalse();

        ConsultaJefaturaComiteRepository
            .FiltrarComiteConJefaturaActiva(new[] { comiteInactivo }.AsQueryable(), convocatoriaId, comiteInactivo.JefaturaUsuarioId)
            .Any()
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Debe_RegistrarConsultaJefaturaComiteRepositoryComoScoped_Cuando_SeConfiguraInfraestructura()
    {
        var servicios = new ServiceCollection();

        servicios.AgregarInfraestructuraPostulaciones(
            PostulacionesDbContextFactory.CadenaConexionPorDefecto);

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IConsultaJefaturaComiteRepository)
            && d.ImplementationType == typeof(ConsultaJefaturaComiteRepository)
            && d.Lifetime == ServiceLifetime.Scoped);

        using var proveedor = servicios.BuildServiceProvider();
        using var scope = proveedor.CreateScope();

        var repositorio = scope.ServiceProvider.GetRequiredService<IConsultaJefaturaComiteRepository>();
        repositorio.Should().BeOfType<ConsultaJefaturaComiteRepository>();
    }
}
