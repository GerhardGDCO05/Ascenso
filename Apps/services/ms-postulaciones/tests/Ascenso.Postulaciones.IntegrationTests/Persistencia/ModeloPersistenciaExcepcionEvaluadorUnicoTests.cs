using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class ModeloPersistenciaExcepcionEvaluadorUnicoTests
{
    private static PostulacionesDbContext CrearContextoModelo()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    [Fact]
    public void Debe_MapearAutorizacionExcepcionEvaluadorUnicoConEsquemaColumnasIndiceYRestriccionCheck_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var entidad = modeloDiseno.FindEntityType(typeof(AutorizacionExcepcionEvaluadorUnico));

        entidad.Should().NotBeNull();
        entidad!.GetSchema().Should().Be(PostulacionesDbContext.Esquema);
        entidad.GetTableName().Should().Be("excepciones_evaluador_unico");
        entidad.FindPrimaryKey()!.GetName().Should().Be("pk_excepciones_evaluador_unico");

        var propiedadId = entidad.FindProperty(nameof(AutorizacionExcepcionEvaluadorUnico.Id))!;
        propiedadId.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadId.GetColumnName().Should().Be("id");
        propiedadId.GetColumnType().Should().Be("uuid");
        propiedadId.IsNullable.Should().BeFalse();

        var postulacionId = entidad.FindProperty(nameof(AutorizacionExcepcionEvaluadorUnico.PostulacionId))!;
        postulacionId.GetColumnName().Should().Be("postulacion_id");
        postulacionId.GetColumnType().Should().Be("uuid");
        postulacionId.IsNullable.Should().BeFalse();

        var justificacion = entidad.FindProperty(nameof(AutorizacionExcepcionEvaluadorUnico.Justificacion))!;
        justificacion.GetColumnName().Should().Be("justificacion");
        justificacion.GetColumnType().Should().Be("varchar(1000)");
        justificacion.GetMaxLength().Should().Be(AutorizacionExcepcionEvaluadorUnico.LongitudMaximaJustificacion);
        justificacion.IsNullable.Should().BeFalse();

        var usuarioAutorizadorId = entidad.FindProperty(nameof(AutorizacionExcepcionEvaluadorUnico.UsuarioAutorId))!;
        usuarioAutorizadorId.GetColumnName().Should().Be("usuario_autorizador_id");
        usuarioAutorizadorId.GetColumnType().Should().Be("uuid");
        usuarioAutorizadorId.IsNullable.Should().BeFalse();

        var fechaAutorizacion = entidad.FindProperty(nameof(AutorizacionExcepcionEvaluadorUnico.FechaCreacion))!;
        fechaAutorizacion.GetColumnName().Should().Be("fecha_autorizacion");
        fechaAutorizacion.GetColumnType().Should().Be("timestamptz");
        fechaAutorizacion.IsNullable.Should().BeFalse();

        var correlacionId = entidad.FindProperty(nameof(AutorizacionExcepcionEvaluadorUnico.CorrelacionId))!;
        correlacionId.GetColumnName().Should().Be("correlacion_id");
        correlacionId.GetColumnType().Should().Be("uuid");
        correlacionId.IsNullable.Should().BeFalse();

        entidad.GetForeignKeys().Should().BeEmpty();

        var indiceUnico = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ux_excepciones_evaluador_unico_postulacion");
        indiceUnico.IsUnique.Should().BeTrue();
        indiceUnico.Properties.Select(p => p.GetColumnName())
            .Should().Equal("postulacion_id");

        var checks = entidad.GetCheckConstraints()
            .ToDictionary(check => check.Name!, check => check.Sql);
        checks.Keys.Should().Contain("ck_excepciones_justificacion_no_vacia");
        checks["ck_excepciones_justificacion_no_vacia"].Should().Be("char_length(btrim(justificacion)) >= 10");

        contexto.ExcepcionesEvaluadorUnico.Should().NotBeNull();
    }

    [Fact]
    public void Debe_IncluirTablaExcepcionesEvaluadorUnicoConRestriccionEIndice_Cuando_SeGeneraElScriptDeCreacion()
    {
        using var contexto = CrearContextoModelo();

        var script = contexto.Database.GenerateCreateScript();

        script.Should().Contain("CREATE TABLE postulaciones.excepciones_evaluador_unico");
        script.Should().Contain("pk_excepciones_evaluador_unico");
        script.Should().Contain("ux_excepciones_evaluador_unico_postulacion");
        script.Should().Contain("ck_excepciones_justificacion_no_vacia");
    }
}
