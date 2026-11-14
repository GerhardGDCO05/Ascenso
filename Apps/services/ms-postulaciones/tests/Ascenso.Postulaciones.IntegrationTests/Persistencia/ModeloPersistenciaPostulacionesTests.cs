using Ascenso.Postulaciones.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class ModeloPersistenciaPostulacionesTests
{
    private static PostulacionesDbContext CrearContextoModelo()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    [Fact]
    public void Debe_UsarElEsquemaPostulaciones_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();

        PostulacionesDbContext.Esquema.Should().Be("postulaciones");
        contexto.Model.GetDefaultSchema().Should().Be(PostulacionesDbContext.Esquema);
    }

    [Fact]
    public void Debe_ConfigurarElHistorialDeMigraciones_Cuando_SeSolicitaElScriptDeCreacion()
    {
        using var contexto = CrearContextoModelo();

        var historial = contexto.GetService<IHistoryRepository>();
        var script = historial.GetCreateIfNotExistsScript();

        script.Should().Contain("__ef_migrations_history");
        script.Should().Contain(PostulacionesDbContext.Esquema);
    }
}
