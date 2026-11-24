using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class ModeloPersistenciaAsignacionesTests
{
    private static PostulacionesDbContext CrearContextoModelo()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    [Fact]
    public void Debe_MapearAsignacionEvaluacionConEsquemaColumnasFksIndicesYRestriccionesCheck_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var entidad = modeloDiseno.FindEntityType(typeof(AsignacionEvaluacion));

        entidad.Should().NotBeNull();
        entidad!.GetSchema().Should().Be(PostulacionesDbContext.Esquema);
        entidad.GetTableName().Should().Be("asignaciones_evaluacion");
        entidad.FindPrimaryKey()!.GetName().Should().Be("pk_asignaciones_evaluacion");

        var propiedadId = entidad.FindProperty(nameof(AsignacionEvaluacion.Id))!;
        propiedadId.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadId.GetColumnName().Should().Be("id");
        propiedadId.GetColumnType().Should().Be("uuid");

        var propiedadVersion = entidad.FindProperty(nameof(AsignacionEvaluacion.Version))!;
        propiedadVersion.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadVersion.IsConcurrencyToken.Should().BeTrue();
        propiedadVersion.GetColumnName().Should().Be("version");

        var fechaCreacion = entidad.FindProperty(nameof(AsignacionEvaluacion.FechaCreacion))!;
        fechaCreacion.GetColumnName().Should().Be("fecha_creacion");
        fechaCreacion.GetColumnType().Should().Be("timestamptz");

        var fechaModificacion = entidad.FindProperty(nameof(AsignacionEvaluacion.FechaUltimaModificacion))!;
        fechaModificacion.GetColumnName().Should().Be("fecha_ultima_modificacion");
        fechaModificacion.GetColumnType().Should().Be("timestamptz");

        var comiteEvaluadorId = entidad.FindProperty(nameof(AsignacionEvaluacion.ComiteEvaluadorId))!;
        comiteEvaluadorId.GetColumnName().Should().Be("comite_evaluador_id");
        comiteEvaluadorId.GetColumnType().Should().Be("uuid");
        comiteEvaluadorId.IsNullable.Should().BeFalse();

        var convocatoriaId = entidad.FindProperty(nameof(AsignacionEvaluacion.ConvocatoriaId))!;
        convocatoriaId.GetColumnName().Should().Be("convocatoria_id");
        convocatoriaId.GetColumnType().Should().Be("uuid");
        convocatoriaId.IsNullable.Should().BeFalse();

        var postulacionId = entidad.FindProperty(nameof(AsignacionEvaluacion.PostulacionId))!;
        postulacionId.GetColumnName().Should().Be("postulacion_id");
        postulacionId.GetColumnType().Should().Be("uuid");
        postulacionId.IsNullable.Should().BeFalse();

        var miembroComiteId = entidad.FindProperty(nameof(AsignacionEvaluacion.MiembroComiteId))!;
        miembroComiteId.GetColumnName().Should().Be("miembro_comite_id");
        miembroComiteId.GetColumnType().Should().Be("uuid");
        miembroComiteId.IsNullable.Should().BeFalse();

        var usuarioEvaluadorId = entidad.FindProperty(nameof(AsignacionEvaluacion.UsuarioEvaluadorId))!;
        usuarioEvaluadorId.GetColumnName().Should().Be("usuario_evaluador_id");
        usuarioEvaluadorId.GetColumnType().Should().Be("uuid");
        usuarioEvaluadorId.IsNullable.Should().BeFalse();

        var fechaAsignacion = entidad.FindProperty(nameof(AsignacionEvaluacion.FechaAsignacion))!;
        fechaAsignacion.GetColumnName().Should().Be("fecha_asignacion");
        fechaAsignacion.GetColumnType().Should().Be("timestamptz");
        fechaAsignacion.IsNullable.Should().BeFalse();

        var fechaLimiteEvaluacion = entidad.FindProperty(nameof(AsignacionEvaluacion.FechaLimiteEvaluacion))!;
        fechaLimiteEvaluacion.GetColumnName().Should().Be("fecha_limite_evaluacion");
        fechaLimiteEvaluacion.GetColumnType().Should().Be("timestamptz");
        fechaLimiteEvaluacion.IsNullable.Should().BeFalse();

        var estado = entidad.FindProperty(nameof(AsignacionEvaluacion.Estado))!;
        estado.GetColumnName().Should().Be("estado");
        estado.GetColumnType().Should().Be("varchar");
        estado.GetMaxLength().Should().Be(30);
        estado.IsNullable.Should().BeFalse();

        var convertidorEstado = estado.GetValueConverter();
        convertidorEstado.Should().NotBeNull();
        convertidorEstado!.ConvertToProvider(EstadoAsignacionEvaluacion.Pendiente).Should().Be("PENDIENTE");
        convertidorEstado.ConvertToProvider(EstadoAsignacionEvaluacion.EnProgreso).Should().Be("EN_PROGRESO");
        convertidorEstado.ConvertToProvider(EstadoAsignacionEvaluacion.Completada).Should().Be("COMPLETADA");
        convertidorEstado.ConvertToProvider(EstadoAsignacionEvaluacion.ReasignadaPorConflicto).Should().Be("REASIGNADA_POR_CONFLICTO");
        convertidorEstado.ConvertFromProvider("PENDIENTE").Should().Be(EstadoAsignacionEvaluacion.Pendiente);
        convertidorEstado.ConvertFromProvider("EN_PROGRESO").Should().Be(EstadoAsignacionEvaluacion.EnProgreso);
        convertidorEstado.ConvertFromProvider("COMPLETADA").Should().Be(EstadoAsignacionEvaluacion.Completada);
        convertidorEstado.ConvertFromProvider("REASIGNADA_POR_CONFLICTO").Should().Be(EstadoAsignacionEvaluacion.ReasignadaPorConflicto);

        var fkComite = entidad.GetForeignKeys()
            .Single(clave => clave.PrincipalEntityType.ClrType == typeof(ComiteEvaluador));
        fkComite.GetConstraintName().Should().Be("fk_asignaciones_comites");
        fkComite.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);

        var fkMiembro = entidad.GetForeignKeys()
            .Single(clave => clave.PrincipalEntityType.ClrType == typeof(MiembroComite));
        fkMiembro.GetConstraintName().Should().Be("fk_asignaciones_miembros_comite");
        fkMiembro.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);

        var indiceUnicoVigente = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ux_asignaciones_postulacion_evaluador_vigente");
        indiceUnicoVigente.IsUnique.Should().BeTrue();
        indiceUnicoVigente.Properties.Select(p => p.GetColumnName())
            .Should().Equal("postulacion_id", "usuario_evaluador_id");
        indiceUnicoVigente.GetFilter().Should().Be("estado <> 'REASIGNADA_POR_CONFLICTO'");

        var indiceEvaluadorEstado = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ix_asignaciones_evaluador_estado");
        indiceEvaluadorEstado.IsUnique.Should().BeFalse();
        indiceEvaluadorEstado.Properties.Select(p => p.GetColumnName())
            .Should().Equal("usuario_evaluador_id", "estado", "fecha_limite_evaluacion");

        var indiceComiteEstado = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ix_asignaciones_comite_estado");
        indiceComiteEstado.IsUnique.Should().BeFalse();
        indiceComiteEstado.Properties.Select(p => p.GetColumnName())
            .Should().Equal("comite_evaluador_id", "estado");

        var indicePostulacion = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ix_asignaciones_postulacion");
        indicePostulacion.IsUnique.Should().BeFalse();
        indicePostulacion.Properties.Select(p => p.GetColumnName())
            .Should().Equal("postulacion_id");

        var checks = entidad.GetCheckConstraints()
            .ToDictionary(check => check.Name!, check => check.Sql);
        checks.Keys.Should().Contain([
            "ck_asignaciones_estado_valido",
            "ck_asignaciones_fecha_limite_posterior"
        ]);

        checks["ck_asignaciones_estado_valido"].Should().Contain("'PENDIENTE'");
        checks["ck_asignaciones_estado_valido"].Should().Contain("'EN_PROGRESO'");
        checks["ck_asignaciones_estado_valido"].Should().Contain("'COMPLETADA'");
        checks["ck_asignaciones_estado_valido"].Should().Contain("'REASIGNADA_POR_CONFLICTO'");
        checks["ck_asignaciones_fecha_limite_posterior"].Should().Be("fecha_limite_evaluacion > fecha_asignacion");

        contexto.AsignacionesEvaluacion.Should().NotBeNull();
    }

    [Fact]
    public void Debe_IncluirTablaAsignacionesConRestriccionesEIndices_Cuando_SeGeneraElScriptDeCreacion()
    {
        using var contexto = CrearContextoModelo();

        var script = contexto.Database.GenerateCreateScript();

        script.Should().Contain("CREATE TABLE postulaciones.asignaciones_evaluacion");
        script.Should().Contain("fk_asignaciones_comites");
        script.Should().Contain("fk_asignaciones_miembros_comite");
        script.Should().Contain("ux_asignaciones_postulacion_evaluador_vigente");
        script.Should().Contain("ix_asignaciones_evaluador_estado");
        script.Should().Contain("ix_asignaciones_comite_estado");
        script.Should().Contain("ix_asignaciones_postulacion");
        script.Should().Contain("ck_asignaciones_estado_valido");
        script.Should().Contain("ck_asignaciones_fecha_limite_posterior");
    }
}
