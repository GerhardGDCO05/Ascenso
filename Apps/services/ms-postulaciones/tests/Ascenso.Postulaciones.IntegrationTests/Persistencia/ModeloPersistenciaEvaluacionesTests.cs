using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.Entidades;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class ModeloPersistenciaEvaluacionesTests
{
    private static PostulacionesDbContext CrearContextoModelo()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    [Fact]
    public void Debe_MapearEvaluacionConEsquemaColumnasNavegacionFksIndicesYRestriccionesCheck_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var entidad = modeloDiseno.FindEntityType(typeof(Evaluacion));

        entidad.Should().NotBeNull();
        entidad!.GetSchema().Should().Be(PostulacionesDbContext.Esquema);
        entidad.GetTableName().Should().Be("evaluaciones");
        entidad.FindPrimaryKey()!.GetName().Should().Be("pk_evaluaciones");

        var propiedadId = entidad.FindProperty(nameof(Evaluacion.Id))!;
        propiedadId.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadId.GetColumnName().Should().Be("id");
        propiedadId.GetColumnType().Should().Be("uuid");

        var propiedadVersion = entidad.FindProperty(nameof(Evaluacion.Version))!;
        propiedadVersion.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadVersion.IsConcurrencyToken.Should().BeTrue();
        propiedadVersion.GetColumnName().Should().Be("version");

        var fechaCreacion = entidad.FindProperty(nameof(Evaluacion.FechaCreacion))!;
        fechaCreacion.GetColumnName().Should().Be("fecha_creacion");
        fechaCreacion.GetColumnType().Should().Be("timestamptz");

        var fechaModificacion = entidad.FindProperty(nameof(Evaluacion.FechaUltimaModificacion))!;
        fechaModificacion.GetColumnName().Should().Be("fecha_ultima_modificacion");
        fechaModificacion.GetColumnType().Should().Be("timestamptz");

        var asignacionEvaluacionId = entidad.FindProperty(nameof(Evaluacion.AsignacionEvaluacionId))!;
        asignacionEvaluacionId.GetColumnName().Should().Be("asignacion_evaluacion_id");
        asignacionEvaluacionId.GetColumnType().Should().Be("uuid");
        asignacionEvaluacionId.IsNullable.Should().BeFalse();

        var postulacionId = entidad.FindProperty(nameof(Evaluacion.PostulacionId))!;
        postulacionId.GetColumnName().Should().Be("postulacion_id");
        postulacionId.GetColumnType().Should().Be("uuid");
        postulacionId.IsNullable.Should().BeFalse();

        var convocatoriaId = entidad.FindProperty(nameof(Evaluacion.ConvocatoriaId))!;
        convocatoriaId.GetColumnName().Should().Be("convocatoria_id");
        convocatoriaId.GetColumnType().Should().Be("uuid");
        convocatoriaId.IsNullable.Should().BeFalse();

        var rubricaEvaluacionId = entidad.FindProperty(nameof(Evaluacion.RubricaEvaluacionId))!;
        rubricaEvaluacionId.GetColumnName().Should().Be("rubrica_evaluacion_id");
        rubricaEvaluacionId.GetColumnType().Should().Be("uuid");
        rubricaEvaluacionId.IsNullable.Should().BeFalse();

        var evaluadorId = entidad.FindProperty(nameof(Evaluacion.EvaluadorId))!;
        evaluadorId.GetColumnName().Should().Be("evaluador_id");
        evaluadorId.GetColumnType().Should().Be("uuid");
        evaluadorId.IsNullable.Should().BeFalse();

        var fechaInicio = entidad.FindProperty(nameof(Evaluacion.FechaInicio))!;
        fechaInicio.GetColumnName().Should().Be("fecha_inicio");
        fechaInicio.GetColumnType().Should().Be("timestamptz");
        fechaInicio.IsNullable.Should().BeFalse();

        var fechaEnvio = entidad.FindProperty(nameof(Evaluacion.FechaEnvio))!;
        fechaEnvio.GetColumnName().Should().Be("fecha_envio");
        fechaEnvio.GetColumnType().Should().Be("timestamptz");
        fechaEnvio.IsNullable.Should().BeTrue();

        var puntajeTotalPonderado = entidad.FindProperty(nameof(Evaluacion.PuntajeTotalPonderado))!;
        puntajeTotalPonderado.GetColumnName().Should().Be("puntaje_total_ponderado");
        puntajeTotalPonderado.GetPrecision().Should().Be(5);
        puntajeTotalPonderado.GetScale().Should().Be(2);
        puntajeTotalPonderado.IsNullable.Should().BeTrue();

        var comentariosGenerales = entidad.FindProperty(nameof(Evaluacion.ComentariosGenerales))!;
        comentariosGenerales.GetColumnName().Should().Be("comentarios_generales");
        comentariosGenerales.GetMaxLength().Should().Be(Evaluacion.LongitudMaximaComentariosGenerales);
        comentariosGenerales.IsNullable.Should().BeTrue();

        var estado = entidad.FindProperty(nameof(Evaluacion.Estado))!;
        estado.GetColumnName().Should().Be("estado");
        estado.GetColumnType().Should().Be("varchar");
        estado.GetMaxLength().Should().Be(30);
        estado.IsNullable.Should().BeFalse();

        var convertidorEstado = estado.GetValueConverter();
        convertidorEstado.Should().NotBeNull();
        convertidorEstado!.ConvertToProvider(EstadoEvaluacion.Borrador).Should().Be("BORRADOR");
        convertidorEstado.ConvertToProvider(EstadoEvaluacion.EnviadaDefinitiva).Should().Be("ENVIADA_DEFINITIVA");
        convertidorEstado.ConvertToProvider(EstadoEvaluacion.InvalidadaPorConflicto).Should().Be("INVALIDADA_POR_CONFLICTO");
        convertidorEstado.ConvertFromProvider("BORRADOR").Should().Be(EstadoEvaluacion.Borrador);
        convertidorEstado.ConvertFromProvider("ENVIADA_DEFINITIVA").Should().Be(EstadoEvaluacion.EnviadaDefinitiva);
        convertidorEstado.ConvertFromProvider("INVALIDADA_POR_CONFLICTO").Should().Be(EstadoEvaluacion.InvalidadaPorConflicto);

        var motivoInvalidacion = entidad.FindProperty(nameof(Evaluacion.MotivoInvalidacion))!;
        motivoInvalidacion.GetColumnName().Should().Be("motivo_invalidacion");
        motivoInvalidacion.GetMaxLength().Should().Be(1000);
        motivoInvalidacion.IsNullable.Should().BeTrue();

        var navegacion = entidad.FindNavigation(nameof(Evaluacion.Calificaciones))!;
        navegacion.IsCollection.Should().BeTrue();
        navegacion.TargetEntityType.ClrType.Should().Be<CalificacionCriterio>();
        navegacion.FieldInfo!.Name.Should().Be("_calificaciones");
        navegacion.GetPropertyAccessMode().Should().Be(PropertyAccessMode.Field);

        var fkAsignacion = entidad.GetForeignKeys()
            .Single(clave => clave.PrincipalEntityType.ClrType == typeof(AsignacionEvaluacion));
        fkAsignacion.GetConstraintName().Should().Be("fk_evaluaciones_asignaciones_evaluacion");
        fkAsignacion.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);

        var fkRubrica = entidad.GetForeignKeys()
            .Single(clave => clave.PrincipalEntityType.ClrType == typeof(RubricaEvaluacion));
        fkRubrica.GetConstraintName().Should().Be("fk_evaluaciones_rubricas");
        fkRubrica.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);

        var indiceAsignacionUnico = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ux_evaluaciones_asignacion_evaluacion");
        indiceAsignacionUnico.IsUnique.Should().BeTrue();
        indiceAsignacionUnico.Properties.Select(p => p.GetColumnName())
            .Should().Equal("asignacion_evaluacion_id");

        var indiceEvaluadorEstado = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ix_evaluaciones_evaluador_estado");
        indiceEvaluadorEstado.IsUnique.Should().BeFalse();
        indiceEvaluadorEstado.Properties.Select(p => p.GetColumnName())
            .Should().Equal("evaluador_id", "estado", "fecha_ultima_modificacion");

        var indicePostulacionEstado = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ix_evaluaciones_postulacion_estado");
        indicePostulacionEstado.IsUnique.Should().BeFalse();
        indicePostulacionEstado.Properties.Select(p => p.GetColumnName())
            .Should().Equal("postulacion_id", "estado");

        var checks = entidad.GetCheckConstraints()
            .ToDictionary(check => check.Name!, check => check.Sql);
        checks.Keys.Should().Contain([
            "ck_evaluaciones_estado_valido",
            "ck_evaluaciones_puntaje_total_rango",
            "ck_evaluaciones_envio_coherente",
            "ck_evaluaciones_comentarios_generales_no_vacios"
        ]);

        checks["ck_evaluaciones_estado_valido"].Should().Contain("'BORRADOR'");
        checks["ck_evaluaciones_estado_valido"].Should().Contain("'ENVIADA_DEFINITIVA'");
        checks["ck_evaluaciones_estado_valido"].Should().Contain("'INVALIDADA_POR_CONFLICTO'");
        checks["ck_evaluaciones_puntaje_total_rango"].Should().Be(
            "puntaje_total_ponderado IS NULL OR (puntaje_total_ponderado >= 0 AND puntaje_total_ponderado <= 100)");
        checks["ck_evaluaciones_envio_coherente"].Should().Be(
            "(estado = 'BORRADOR' AND fecha_envio IS NULL AND puntaje_total_ponderado IS NULL AND motivo_invalidacion IS NULL) OR (estado = 'ENVIADA_DEFINITIVA' AND fecha_envio IS NOT NULL AND puntaje_total_ponderado IS NOT NULL AND motivo_invalidacion IS NULL) OR (estado = 'INVALIDADA_POR_CONFLICTO' AND motivo_invalidacion IS NOT NULL AND length(btrim(motivo_invalidacion)) > 0)");
        checks["ck_evaluaciones_comentarios_generales_no_vacios"].Should().Be(
            "comentarios_generales IS NULL OR length(btrim(comentarios_generales)) > 0");

        contexto.Evaluaciones.Should().NotBeNull();
    }

    [Fact]
    public void Debe_MapearCalificacionCriterioConEsquemaColumnasFksIndiceUnicoYRestriccionesCheck_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var entidad = modeloDiseno.FindEntityType(typeof(CalificacionCriterio));

        entidad.Should().NotBeNull();
        entidad!.GetSchema().Should().Be(PostulacionesDbContext.Esquema);
        entidad.GetTableName().Should().Be("calificaciones_criterio");
        entidad.FindPrimaryKey()!.GetName().Should().Be("pk_calificaciones_criterio");

        var propiedadId = entidad.FindProperty(nameof(CalificacionCriterio.Id))!;
        propiedadId.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadId.GetColumnName().Should().Be("id");
        propiedadId.GetColumnType().Should().Be("uuid");

        var propiedadVersion = entidad.FindProperty(nameof(CalificacionCriterio.Version))!;
        propiedadVersion.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadVersion.IsConcurrencyToken.Should().BeTrue();
        propiedadVersion.GetColumnName().Should().Be("version");

        var fechaCreacion = entidad.FindProperty(nameof(CalificacionCriterio.FechaCreacion))!;
        fechaCreacion.GetColumnName().Should().Be("fecha_creacion");
        fechaCreacion.GetColumnType().Should().Be("timestamptz");

        var fechaModificacion = entidad.FindProperty(nameof(CalificacionCriterio.FechaUltimaModificacion))!;
        fechaModificacion.GetColumnName().Should().Be("fecha_ultima_modificacion");
        fechaModificacion.GetColumnType().Should().Be("timestamptz");

        var evaluacionId = entidad.FindProperty(nameof(CalificacionCriterio.EvaluacionId))!;
        evaluacionId.GetColumnName().Should().Be("evaluacion_id");
        evaluacionId.GetColumnType().Should().Be("uuid");
        evaluacionId.IsNullable.Should().BeFalse();

        var criterioEvaluacionId = entidad.FindProperty(nameof(CalificacionCriterio.CriterioEvaluacionId))!;
        criterioEvaluacionId.GetColumnName().Should().Be("criterio_evaluacion_id");
        criterioEvaluacionId.GetColumnType().Should().Be("uuid");
        criterioEvaluacionId.IsNullable.Should().BeFalse();

        var puntajeAsignado = entidad.FindProperty(nameof(CalificacionCriterio.PuntajeAsignado))!;
        puntajeAsignado.GetColumnName().Should().Be("puntaje_asignado");
        puntajeAsignado.GetPrecision().Should().Be(7);
        puntajeAsignado.GetScale().Should().Be(2);
        puntajeAsignado.IsNullable.Should().BeFalse();

        var comentario = entidad.FindProperty(nameof(CalificacionCriterio.Comentario))!;
        comentario.GetColumnName().Should().Be("comentario");
        comentario.GetMaxLength().Should().Be(DatosCalificacionCriterio.LongitudMaximaComentario);
        comentario.IsNullable.Should().BeTrue();

        var fkEvaluacion = entidad.GetForeignKeys()
            .Single(clave => clave.PrincipalEntityType.ClrType == typeof(Evaluacion));
        fkEvaluacion.GetConstraintName().Should().Be("fk_calificaciones_criterio_evaluaciones");
        fkEvaluacion.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        var fkCriterio = entidad.GetForeignKeys()
            .Single(clave => clave.PrincipalEntityType.ClrType == typeof(CriterioEvaluacion));
        fkCriterio.GetConstraintName().Should().Be("fk_calificaciones_criterio_criterios_evaluacion");
        fkCriterio.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);

        var indiceUnico = entidad.GetIndexes()
            .Single(indice => indice.GetDatabaseName() == "ux_calificaciones_criterio_evaluacion_criterio");
        indiceUnico.IsUnique.Should().BeTrue();
        indiceUnico.Properties.Select(p => p.GetColumnName())
            .Should().Equal("evaluacion_id", "criterio_evaluacion_id");

        var checks = entidad.GetCheckConstraints()
            .ToDictionary(check => check.Name!, check => check.Sql);
        checks.Keys.Should().Contain([
            "ck_calificaciones_criterio_puntaje_rango",
            "ck_calificaciones_criterio_comentario_no_vacio"
        ]);

        checks["ck_calificaciones_criterio_puntaje_rango"].Should().Be(
            "puntaje_asignado >= 0 AND puntaje_asignado <= 1000");
        checks["ck_calificaciones_criterio_comentario_no_vacio"].Should().Be(
            "comentario IS NULL OR length(btrim(comentario)) > 0");

        contexto.CalificacionesCriterio.Should().NotBeNull();
    }

    [Fact]
    public void Debe_IncluirTablasEvaluacionesYCalificacionesCriterioConRestriccionesEIndices_Cuando_SeGeneraElScriptDeCreacion()
    {
        using var contexto = CrearContextoModelo();

        var script = contexto.Database.GenerateCreateScript();

        script.Should().Contain("CREATE TABLE postulaciones.evaluaciones");
        script.Should().Contain("pk_evaluaciones");
        script.Should().Contain("fk_evaluaciones_asignaciones_evaluacion");
        script.Should().Contain("fk_evaluaciones_rubricas");
        script.Should().Contain("ux_evaluaciones_asignacion_evaluacion");
        script.Should().Contain("ix_evaluaciones_evaluador_estado");
        script.Should().Contain("ix_evaluaciones_postulacion_estado");
        script.Should().Contain("ck_evaluaciones_estado_valido");
        script.Should().Contain("ck_evaluaciones_puntaje_total_rango");
        script.Should().Contain("ck_evaluaciones_envio_coherente");
        script.Should().Contain("ck_evaluaciones_comentarios_generales_no_vacios");

        script.Should().Contain("CREATE TABLE postulaciones.calificaciones_criterio");
        script.Should().Contain("pk_calificaciones_criterio");
        script.Should().Contain("fk_calificaciones_criterio_evaluaciones");
        script.Should().Contain("fk_calificaciones_criterio_criterios_evaluacion");
        script.Should().Contain("ux_calificaciones_criterio_evaluacion_criterio");
        script.Should().Contain("ck_calificaciones_criterio_puntaje_rango");
        script.Should().Contain("ck_calificaciones_criterio_comentario_no_vacio");
    }
}
