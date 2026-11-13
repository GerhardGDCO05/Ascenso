using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Ascenso.Becas.Infrastructure.Persistence;
using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

public sealed class BecasDbContextTests
{
    private const string CadenaConexion =
        "Host=localhost;Port=5432;Database=ascenso_becas;Username=postgres;Password=postgres";

    [Fact]
    public void Debe_ConstruirseConLaCadenaDePostgreSql_Cuando_SeConfiguraElContexto()
    {
        using var contexto = CrearContexto();

        Assert.True(contexto.Database.IsNpgsql());
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", contexto.Database.ProviderName);
        Assert.Equal(CadenaConexion, contexto.Database.GetConnectionString());
    }

    [Fact]
    public void Debe_ExponerLosDbSetsDelAgregadoLasProyeccionesYLaIdempotencia()
    {
        using var contexto = CrearContexto();

        Assert.NotNull(contexto.Becas);
        Assert.NotNull(contexto.OfertasBeca);
        Assert.NotNull(contexto.CompromisosBecarios);
        Assert.NotNull(contexto.HistorialEstadosBeca);
        Assert.NotNull(contexto.ProgramasBecaRef);
        Assert.NotNull(contexto.CostosMatriculaRef);
        Assert.NotNull(contexto.ConveniosRef);
        Assert.NotNull(contexto.SolicitudesIdempotencia);
    }

    [Fact]
    public void Debe_MapearLasEntidadesEnElEsquemaBecas_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContexto();

        var tipos = contexto.Model.GetEntityTypes().Select(entidad => entidad.ClrType).ToArray();

        Assert.Contains(typeof(Beca), tipos);
        Assert.Contains(typeof(OfertaBeca), tipos);
        Assert.Contains(typeof(CompromisoBecario), tipos);
        Assert.Contains(typeof(HistorialEstadoBeca), tipos);
        Assert.Contains(typeof(ProgramaBecaRef), tipos);
        Assert.Contains(typeof(CostoMatriculaRef), tipos);
        Assert.Contains(typeof(ConvenioRef), tipos);
        Assert.Contains(typeof(IdempotencyRequest), tipos);
        Assert.Equal("becas", contexto.Model.GetDefaultSchema());
    }

    private static BecasDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<BecasDbContext>()
            .UseNpgsql(CadenaConexion)
            .Options;

        return new BecasDbContext(opciones);
    }
}
