using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
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

    [Fact]
    public void Debe_MapearPostulanteYDatosContactoConValueGeneratedNever_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var entidad = contexto.Model.FindEntityType(typeof(Postulante));

        entidad.Should().NotBeNull();
        entidad!.GetSchema().Should().Be(PostulacionesDbContext.Esquema);
        entidad.GetTableName().Should().Be("postulantes");

        var propiedadId = entidad.FindProperty(nameof(Postulante.Id));
        propiedadId.Should().NotBeNull();
        propiedadId!.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadId.GetColumnName().Should().Be("id");

        var propiedadVersion = entidad.FindProperty(nameof(Postulante.Version));
        propiedadVersion.Should().NotBeNull();
        propiedadVersion!.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadVersion.IsConcurrencyToken.Should().BeTrue();

        var navegacionContacto = entidad.FindNavigation(nameof(Postulante.DatosContacto));
        navegacionContacto.Should().NotBeNull();
        var tipoContacto = navegacionContacto!.TargetEntityType;
        tipoContacto.IsOwned().Should().BeTrue();
        tipoContacto.FindProperty("CorreoElectronico")!.GetColumnName().Should().Be("correo_electronico");
        tipoContacto.FindProperty("Telefono")!.GetColumnName().Should().Be("telefono");
    }

    [Fact]
    public void Debe_ConfigurarIndicesUnicosYRestriccionesCheck_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var entidad = modeloDiseno.FindEntityType(typeof(Postulante))!;

        var nombresIndices = entidad.GetIndexes()
            .Where(indice => indice.IsUnique)
            .Select(indice => indice.GetDatabaseName())
            .ToList();

        nombresIndices.Should().Contain("ux_postulante_numero_documento_identidad");
        nombresIndices.Should().Contain("ux_postulante_usuario_id");

        var nombresChecks = entidad.GetCheckConstraints()
            .Select(check => check.Name)
            .ToList();

        nombresChecks.Should().Contain("ck_postulante_condicion_academica_valida");
        nombresChecks.Should().Contain("ck_postulante_estado_perfil_valido");
        nombresChecks.Should().Contain("ck_postulante_acepta_tratamiento_datos");
        nombresChecks.Should().Contain("ck_postulante_longitudes_minimas");
    }

    [Fact]
    public void Debe_IncluirTablaPostulantesEIndicesUnicos_Cuando_SeGeneraElScriptDeCreacion()
    {
        using var contexto = CrearContextoModelo();

        var script = contexto.Database.GenerateCreateScript();

        script.Should().Contain("CREATE TABLE postulaciones.postulantes");
        script.Should().Contain("ux_postulante_numero_documento_identidad");
        script.Should().Contain("ux_postulante_usuario_id");
        script.Should().Contain("correo_electronico");
        script.Should().Contain("telefono");
    }
}
