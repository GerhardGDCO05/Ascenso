using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class ModeloPersistenciaComitesTests
{
    private static PostulacionesDbContext CrearContextoModelo()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    [Fact]
    public void Debe_MapearComiteEvaluadorConEsquemaColumnasNavegacionEIndiceUnico_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var entidad = modeloDiseno.FindEntityType(typeof(ComiteEvaluador));

        entidad.Should().NotBeNull();
        entidad!.GetSchema().Should().Be(PostulacionesDbContext.Esquema);
        entidad.GetTableName().Should().Be("comites_evaluadores");
        entidad.FindPrimaryKey()!.GetName().Should().Be("pk_comites_evaluadores");

        var propiedadId = entidad.FindProperty(nameof(ComiteEvaluador.Id))!;
        propiedadId.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadId.GetColumnName().Should().Be("id");
        propiedadId.GetColumnType().Should().Be("uuid");

        var propiedadVersion = entidad.FindProperty(nameof(ComiteEvaluador.Version))!;
        propiedadVersion.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadVersion.IsConcurrencyToken.Should().BeTrue();
        propiedadVersion.GetColumnName().Should().Be("version");

        var fechaCreacion = entidad.FindProperty(nameof(ComiteEvaluador.FechaCreacion))!;
        fechaCreacion.GetColumnName().Should().Be("fecha_creacion");
        fechaCreacion.GetColumnType().Should().Be("timestamptz");

        var fechaModificacion = entidad.FindProperty(nameof(ComiteEvaluador.FechaUltimaModificacion))!;
        fechaModificacion.GetColumnName().Should().Be("fecha_ultima_modificacion");
        fechaModificacion.GetColumnType().Should().Be("timestamptz");

        var programaBecaId = entidad.FindProperty(nameof(ComiteEvaluador.ProgramaBecaId))!;
        programaBecaId.GetColumnName().Should().Be("programa_beca_id");
        programaBecaId.GetColumnType().Should().Be("uuid");
        programaBecaId.IsNullable.Should().BeFalse();

        var convocatoriaId = entidad.FindProperty(nameof(ComiteEvaluador.ConvocatoriaId))!;
        convocatoriaId.GetColumnName().Should().Be("convocatoria_id");
        convocatoriaId.GetColumnType().Should().Be("uuid");
        convocatoriaId.IsNullable.Should().BeFalse();

        var nombre = entidad.FindProperty(nameof(ComiteEvaluador.Nombre))!;
        nombre.GetColumnName().Should().Be("nombre");
        nombre.IsNullable.Should().BeFalse();
        nombre.GetMaxLength().Should().Be(ComiteEvaluador.LongitudMaximaNombre);

        var jefaturaUsuarioId = entidad.FindProperty(nameof(ComiteEvaluador.JefaturaUsuarioId))!;
        jefaturaUsuarioId.GetColumnName().Should().Be("jefatura_usuario_id");
        jefaturaUsuarioId.GetColumnType().Should().Be("uuid");
        jefaturaUsuarioId.IsNullable.Should().BeFalse();

        var fechaConformacion = entidad.FindProperty(nameof(ComiteEvaluador.FechaConformacion))!;
        fechaConformacion.GetColumnName().Should().Be("fecha_conformacion");
        fechaConformacion.GetColumnType().Should().Be("date");
        fechaConformacion.IsNullable.Should().BeFalse();

        var conformadoPor = entidad.FindProperty(nameof(ComiteEvaluador.ConformadoPorUsuarioId))!;
        conformadoPor.GetColumnName().Should().Be("conformado_por_usuario_id");
        conformadoPor.GetColumnType().Should().Be("uuid");
        conformadoPor.IsNullable.Should().BeFalse();

        var activo = entidad.FindProperty(nameof(ComiteEvaluador.Activo))!;
        activo.GetColumnName().Should().Be("activo");
        activo.GetColumnType().Should().Be("boolean");
        activo.IsNullable.Should().BeFalse();

        var navegacion = entidad.FindNavigation(nameof(ComiteEvaluador.Miembros))!;
        navegacion.IsCollection.Should().BeTrue();
        navegacion.TargetEntityType.ClrType.Should().Be<MiembroComite>();
        navegacion.FieldInfo!.Name.Should().Be("_miembros");
        navegacion.GetPropertyAccessMode().Should().Be(PropertyAccessMode.Field);

        var indiceUnico = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ux_comites_programa_convocatoria");
        indiceUnico.IsUnique.Should().BeTrue();
        indiceUnico.Properties.Select(p => p.GetColumnName())
            .Should().Equal("programa_beca_id", "convocatoria_id");

        contexto.ComitesEvaluadores.Should().NotBeNull();
    }

    [Fact]
    public void Debe_MapearMiembroComiteConFkIndicesParcialesYRestriccionesCheck_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var entidad = modeloDiseno.FindEntityType(typeof(MiembroComite));

        entidad.Should().NotBeNull();
        entidad!.GetSchema().Should().Be(PostulacionesDbContext.Esquema);
        entidad.GetTableName().Should().Be("miembros_comite");
        entidad.FindPrimaryKey()!.GetName().Should().Be("pk_miembros_comite");

        var propiedadId = entidad.FindProperty(nameof(MiembroComite.Id))!;
        propiedadId.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadId.GetColumnName().Should().Be("id");
        propiedadId.GetColumnType().Should().Be("uuid");

        var propiedadVersion = entidad.FindProperty(nameof(MiembroComite.Version))!;
        propiedadVersion.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadVersion.IsConcurrencyToken.Should().BeTrue();
        propiedadVersion.GetColumnName().Should().Be("version");

        var fechaCreacion = entidad.FindProperty(nameof(MiembroComite.FechaCreacion))!;
        fechaCreacion.GetColumnName().Should().Be("fecha_creacion");
        fechaCreacion.GetColumnType().Should().Be("timestamptz");

        var fechaModificacion = entidad.FindProperty(nameof(MiembroComite.FechaUltimaModificacion))!;
        fechaModificacion.GetColumnName().Should().Be("fecha_ultima_modificacion");
        fechaModificacion.GetColumnType().Should().Be("timestamptz");

        var comiteId = entidad.FindProperty(nameof(MiembroComite.ComiteEvaluadorId))!;
        comiteId.GetColumnName().Should().Be("comite_evaluador_id");
        comiteId.GetColumnType().Should().Be("uuid");
        comiteId.IsNullable.Should().BeFalse();

        var usuarioEvaluadorId = entidad.FindProperty(nameof(MiembroComite.UsuarioEvaluadorId))!;
        usuarioEvaluadorId.GetColumnName().Should().Be("usuario_evaluador_id");
        usuarioEvaluadorId.GetColumnType().Should().Be("uuid");
        usuarioEvaluadorId.IsNullable.Should().BeFalse();

        var tipo = entidad.FindProperty(nameof(MiembroComite.Tipo))!;
        tipo.GetColumnName().Should().Be("tipo");
        tipo.GetColumnType().Should().Be("varchar");
        tipo.GetMaxLength().Should().Be(10);
        tipo.IsNullable.Should().BeFalse();

        var convertidorTipo = tipo.GetValueConverter();
        convertidorTipo.Should().NotBeNull();
        convertidorTipo!.ConvertToProvider(TipoEvaluador.Interno).Should().Be("INTERNO");
        convertidorTipo.ConvertToProvider(TipoEvaluador.Externo).Should().Be("EXTERNO");
        convertidorTipo.ConvertFromProvider("INTERNO").Should().Be(TipoEvaluador.Interno);
        convertidorTipo.ConvertFromProvider("EXTERNO").Should().Be(TipoEvaluador.Externo);

        var rol = entidad.FindProperty(nameof(MiembroComite.Rol))!;
        rol.GetColumnName().Should().Be("rol");
        rol.GetColumnType().Should().Be("varchar");
        rol.GetMaxLength().Should().Be(15);
        rol.IsNullable.Should().BeFalse();

        var convertidorRol = rol.GetValueConverter();
        convertidorRol.Should().NotBeNull();
        convertidorRol!.ConvertToProvider(RolMiembroComite.Jefatura).Should().Be("JEFATURA");
        convertidorRol.ConvertToProvider(RolMiembroComite.Evaluador).Should().Be("EVALUADOR");
        convertidorRol.ConvertFromProvider("JEFATURA").Should().Be(RolMiembroComite.Jefatura);
        convertidorRol.ConvertFromProvider("EVALUADOR").Should().Be(RolMiembroComite.Evaluador);

        var fechaAsignacion = entidad.FindProperty(nameof(MiembroComite.FechaAsignacion))!;
        fechaAsignacion.GetColumnName().Should().Be("fecha_asignacion");
        fechaAsignacion.GetColumnType().Should().Be("date");
        fechaAsignacion.IsNullable.Should().BeFalse();

        var fechaDesvinculacion = entidad.FindProperty(nameof(MiembroComite.FechaDesvinculacion))!;
        fechaDesvinculacion.GetColumnName().Should().Be("fecha_desvinculacion");
        fechaDesvinculacion.GetColumnType().Should().Be("date");
        fechaDesvinculacion.IsNullable.Should().BeTrue();

        var activo = entidad.FindProperty(nameof(MiembroComite.Activo))!;
        activo.GetColumnName().Should().Be("activo");
        activo.GetColumnType().Should().Be("boolean");
        activo.IsNullable.Should().BeFalse();

        var claveForanea = entidad.GetForeignKeys()
            .Single(clave => clave.PrincipalEntityType.ClrType == typeof(ComiteEvaluador));
        claveForanea.GetConstraintName().Should().Be("fk_miembros_comite_comites");
        claveForanea.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        var indiceUsuarioActivo = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ux_miembros_comite_usuario_activo");
        indiceUsuarioActivo.IsUnique.Should().BeTrue();
        indiceUsuarioActivo.Properties.Select(p => p.GetColumnName())
            .Should().Equal("comite_evaluador_id", "usuario_evaluador_id");
        indiceUsuarioActivo.GetFilter().Should().Be("activo");

        var indiceJefaturaActiva = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ux_miembros_comite_jefatura_activa");
        indiceJefaturaActiva.IsUnique.Should().BeTrue();
        indiceJefaturaActiva.Properties.Select(p => p.GetColumnName())
            .Should().Equal("comite_evaluador_id");
        indiceJefaturaActiva.GetFilter().Should().Be("activo AND rol = 'JEFATURA'");

        var checks = entidad.GetCheckConstraints()
            .ToDictionary(check => check.Name!, check => check.Sql);
        checks.Keys.Should().Contain([
            "ck_miembros_comite_tipo_valido",
            "ck_miembros_comite_rol_valido",
            "ck_miembros_comite_desvinculacion_coherente",
            "ck_miembros_comite_jefatura_interna"
        ]);

        checks["ck_miembros_comite_tipo_valido"].Should().Contain("'INTERNO'");
        checks["ck_miembros_comite_tipo_valido"].Should().Contain("'EXTERNO'");
        checks["ck_miembros_comite_rol_valido"].Should().Contain("'JEFATURA'");
        checks["ck_miembros_comite_rol_valido"].Should().Contain("'EVALUADOR'");
        checks["ck_miembros_comite_desvinculacion_coherente"].Should().Be(
            "(activo AND fecha_desvinculacion IS NULL) OR (NOT activo AND fecha_desvinculacion IS NOT NULL AND fecha_desvinculacion >= fecha_asignacion)");
        checks["ck_miembros_comite_jefatura_interna"].Should().Be(
            "rol <> 'JEFATURA' OR tipo = 'INTERNO'");

        contexto.MiembrosComite.Should().NotBeNull();
    }

    [Fact]
    public void Debe_IncluirTablasComitesYMiembrosConRestricciones_Cuando_SeGeneraElScriptDeCreacion()
    {
        using var contexto = CrearContextoModelo();

        var script = contexto.Database.GenerateCreateScript();

        script.Should().Contain("CREATE TABLE postulaciones.comites_evaluadores");
        script.Should().Contain("ux_comites_programa_convocatoria");
        script.Should().Contain("CREATE TABLE postulaciones.miembros_comite");
        script.Should().Contain("fk_miembros_comite_comites");
        script.Should().Contain("ux_miembros_comite_usuario_activo");
        script.Should().Contain("ux_miembros_comite_jefatura_activa");
        script.Should().Contain("ck_miembros_comite_tipo_valido");
        script.Should().Contain("ck_miembros_comite_rol_valido");
        script.Should().Contain("ck_miembros_comite_desvinculacion_coherente");
        script.Should().Contain("ck_miembros_comite_jefatura_interna");
    }
}
