using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Domain.Postulantes.Entidades;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
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
    public void Debe_MapearLasColumnasSocioeconomicasNulables_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var entidad = contexto.Model.FindEntityType(typeof(Postulante))!;

        var estadoCivil = entidad.FindProperty(nameof(Postulante.EstadoCivil))!;
        estadoCivil.GetColumnName().Should().Be("estado_civil");
        estadoCivil.IsNullable.Should().BeTrue();
        estadoCivil.GetMaxLength().Should().Be(20);

        var promedioActual = entidad.FindProperty(nameof(Postulante.PromedioActual))!;
        promedioActual.GetColumnName().Should().Be("promedio_actual");
        promedioActual.IsNullable.Should().BeTrue();
        promedioActual.GetPrecision().Should().Be(5);
        promedioActual.GetScale().Should().Be(2);

        var trabaja = entidad.FindProperty(nameof(Postulante.Trabaja))!;
        trabaja.GetColumnName().Should().Be("trabaja");
        trabaja.IsNullable.Should().BeTrue();

        var ocupacion = entidad.FindProperty(nameof(Postulante.Ocupacion))!;
        ocupacion.GetColumnName().Should().Be("ocupacion");
        ocupacion.IsNullable.Should().BeTrue();
        ocupacion.GetMaxLength().Should().Be(30);

        var ingresoPostulante = entidad.FindProperty(nameof(Postulante.IngresoMensualPostulante))!;
        ingresoPostulante.GetColumnName().Should().Be("ingreso_mensual_postulante");
        ingresoPostulante.IsNullable.Should().BeTrue();
        ingresoPostulante.GetPrecision().Should().Be(18);
        ingresoPostulante.GetScale().Should().Be(2);

        var numeroIntegrantes = entidad.FindProperty(nameof(Postulante.NumeroIntegrantesFamilia))!;
        numeroIntegrantes.GetColumnName().Should().Be("numero_integrantes_familia");
        numeroIntegrantes.IsNullable.Should().BeTrue();

        var ingresoTotal = entidad.FindProperty(nameof(Postulante.IngresoFamiliarMensualTotal))!;
        ingresoTotal.GetColumnName().Should().Be("ingreso_familiar_mensual_total");
        ingresoTotal.IsNullable.Should().BeTrue();
        ingresoTotal.GetPrecision().Should().Be(18);
        ingresoTotal.GetScale().Should().Be(2);

        var navegacion = entidad.FindNavigation(nameof(Postulante.MiembrosFamiliares))!;
        navegacion.IsCollection.Should().BeTrue();
        navegacion.TargetEntityType.ClrType.Should().Be<MiembroFamiliar>();
        navegacion.FieldInfo!.Name.Should().Be("_miembrosFamiliares");
        navegacion.GetPropertyAccessMode().Should().Be(PropertyAccessMode.Field);
    }

    [Fact]
    public void Debe_MapearMiembroFamiliarConRelacionIndiceYRestricciones_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var entidad = modeloDiseno.FindEntityType(typeof(MiembroFamiliar));

        entidad.Should().NotBeNull();
        entidad!.GetSchema().Should().Be(PostulacionesDbContext.Esquema);
        entidad.GetTableName().Should().Be("miembros_familiares");
        entidad.FindPrimaryKey()!.GetName().Should().Be("pk_miembros_familiares");

        entidad.FindProperty(nameof(MiembroFamiliar.PostulanteId))!
            .GetColumnName().Should().Be("postulante_id");
        entidad.FindProperty(nameof(MiembroFamiliar.Parentesco))!
            .GetColumnName().Should().Be("parentesco");
        entidad.FindProperty(nameof(MiembroFamiliar.Parentesco))!
            .GetMaxLength().Should().Be(20);
        entidad.FindProperty(nameof(MiembroFamiliar.Edad))!
            .GetColumnName().Should().Be("edad");
        entidad.FindProperty(nameof(MiembroFamiliar.Ocupacion))!
            .GetColumnName().Should().Be("ocupacion");
        entidad.FindProperty(nameof(MiembroFamiliar.Ocupacion))!
            .GetMaxLength().Should().Be(30);

        var ingreso = entidad.FindProperty(nameof(MiembroFamiliar.IngresoMensual))!;
        ingreso.GetColumnName().Should().Be("ingreso_mensual");
        ingreso.GetPrecision().Should().Be(18);
        ingreso.GetScale().Should().Be(2);

        var claveForanea = entidad.GetForeignKeys()
            .Single(clave => clave.PrincipalEntityType.ClrType == typeof(Postulante));
        claveForanea.GetConstraintName().Should().Be("fk_miembros_familiares_postulantes");
        claveForanea.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        entidad.GetIndexes()
            .Select(indice => indice.GetDatabaseName())
            .Should().Contain("ix_miembros_familiares_postulante_id");

        var nombresChecks = entidad.GetCheckConstraints()
            .Select(check => check.Name)
            .ToList();

        nombresChecks.Should().Contain("ck_miembro_familiar_parentesco_valido");
        nombresChecks.Should().Contain("ck_miembro_familiar_edad_rango");
        nombresChecks.Should().Contain("ck_miembro_familiar_ocupacion_valida");
        nombresChecks.Should().Contain("ck_miembro_familiar_ingreso_no_negativo");

        contexto.MiembrosFamiliares.Should().NotBeNull();
    }

    [Fact]
    public void Debe_IncluirLaColeccionFamiliar_Cuando_SeGeneraUnaConsultaConLaNavegacion()
    {
        using var contexto = CrearContextoModelo();
        var usuarioId = Guid.NewGuid();

        var sql = contexto.Postulantes
            .AsNoTracking()
            .Include(postulante => postulante.MiembrosFamiliares)
            .Where(postulante => postulante.UsuarioId == usuarioId)
            .ToQueryString();

        sql.Should().Contain("miembros_familiares");
        sql.Should().Contain("LEFT JOIN");
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
        nombresChecks.Should().Contain("ck_postulante_estado_civil_valido");
        nombresChecks.Should().Contain("ck_postulante_ocupacion_valida");
        nombresChecks.Should().Contain("ck_postulante_promedio_actual_rango");
        nombresChecks.Should().Contain("ck_postulante_ingreso_mensual_no_negativo");
        nombresChecks.Should().Contain("ck_postulante_ingreso_familiar_total_no_negativo");
        nombresChecks.Should().Contain("ck_postulante_numero_integrantes_familia");
        nombresChecks.Should().Contain("ck_postulante_perfil_completo_campos_requeridos");
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
        script.Should().Contain("estado_civil");
        script.Should().Contain("promedio_actual");
        script.Should().Contain("numero_integrantes_familia");
        script.Should().Contain("ingreso_familiar_mensual_total");
        script.Should().Contain("CREATE TABLE postulaciones.miembros_familiares");
        script.Should().Contain("fk_miembros_familiares_postulantes");
        script.Should().Contain("ix_miembros_familiares_postulante_id");
        script.Should().Contain("ck_miembro_familiar_edad_rango");
        script.Should().Contain("CREATE TABLE postulaciones.rubricas");
        script.Should().Contain("ux_rubricas_programa_convocatoria");
        script.Should().Contain("ck_rubricas_estado_valido");
        script.Should().Contain("ck_rubricas_motivo_bloqueo_valido");
        script.Should().Contain("ck_rubricas_bloqueo_coherente");
        script.Should().Contain("motivo_bloqueo");
        script.Should().Contain("fecha_bloqueo");
        script.Should().Contain("CREATE TABLE postulaciones.criterios_evaluacion");
        script.Should().Contain("fk_criterios_evaluacion_rubricas");
        script.Should().Contain("ux_criterios_evaluacion_rubrica_orden");
        script.Should().Contain("ck_criterios_evaluacion_peso_rango");
        script.Should().Contain("ck_criterios_evaluacion_puntaje_rango");
        script.Should().Contain("ck_criterios_evaluacion_orden_positivo");
    }

    [Fact]
    public void Debe_MapearRubricaEvaluacionConIndiceUnicoRestriccionYCriterios_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var entidad = modeloDiseno.FindEntityType(typeof(RubricaEvaluacion));

        entidad.Should().NotBeNull();
        entidad!.GetSchema().Should().Be(PostulacionesDbContext.Esquema);
        entidad.GetTableName().Should().Be("rubricas");
        entidad.FindPrimaryKey()!.GetName().Should().Be("pk_rubricas");

        var propiedadId = entidad.FindProperty(nameof(RubricaEvaluacion.Id))!;
        propiedadId.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadId.GetColumnName().Should().Be("id");

        var propiedadVersion = entidad.FindProperty(nameof(RubricaEvaluacion.Version))!;
        propiedadVersion.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadVersion.IsConcurrencyToken.Should().BeTrue();

        var programaBecaId = entidad.FindProperty(nameof(RubricaEvaluacion.ProgramaBecaId))!;
        programaBecaId.GetColumnName().Should().Be("programa_beca_id");
        programaBecaId.IsNullable.Should().BeFalse();

        var convocatoriaId = entidad.FindProperty(nameof(RubricaEvaluacion.ConvocatoriaId))!;
        convocatoriaId.GetColumnName().Should().Be("convocatoria_id");
        convocatoriaId.IsNullable.Should().BeFalse();

        var nombre = entidad.FindProperty(nameof(RubricaEvaluacion.Nombre))!;
        nombre.GetColumnName().Should().Be("nombre");
        nombre.IsNullable.Should().BeFalse();
        nombre.GetMaxLength().Should().Be(200);

        var estado = entidad.FindProperty(nameof(RubricaEvaluacion.Estado))!;
        estado.GetColumnName().Should().Be("estado");
        estado.GetColumnType().Should().Be("varchar");
        estado.IsNullable.Should().BeFalse();
        estado.GetMaxLength().Should().Be(20);

        var fechaPublicacion = entidad.FindProperty(nameof(RubricaEvaluacion.FechaPublicacion))!;
        fechaPublicacion.GetColumnName().Should().Be("fecha_publicacion");
        fechaPublicacion.GetColumnType().Should().Be("timestamptz");
        fechaPublicacion.IsNullable.Should().BeTrue();

        var motivoBloqueo = entidad.FindProperty(nameof(RubricaEvaluacion.MotivoBloqueo))!;
        motivoBloqueo.GetColumnName().Should().Be("motivo_bloqueo");
        motivoBloqueo.GetColumnType().Should().Be("varchar");
        motivoBloqueo.IsNullable.Should().BeTrue();
        motivoBloqueo.GetMaxLength().Should().Be(30);

        var convertidorMotivo = motivoBloqueo.GetValueConverter();
        convertidorMotivo.Should().NotBeNull();
        convertidorMotivo!.ConvertToProvider(MotivoBloqueoRubrica.InicioEvaluacion)
            .Should().Be("INICIO_EVALUACION");
        convertidorMotivo.ConvertToProvider(MotivoBloqueoRubrica.PrimeraCalificacion)
            .Should().Be("PRIMERA_CALIFICACION");
        convertidorMotivo.ConvertFromProvider("INICIO_EVALUACION")
            .Should().Be(MotivoBloqueoRubrica.InicioEvaluacion);
        convertidorMotivo.ConvertFromProvider("PRIMERA_CALIFICACION")
            .Should().Be(MotivoBloqueoRubrica.PrimeraCalificacion);

        var fechaBloqueo = entidad.FindProperty(nameof(RubricaEvaluacion.FechaBloqueo))!;
        fechaBloqueo.GetColumnName().Should().Be("fecha_bloqueo");
        fechaBloqueo.GetColumnType().Should().Be("timestamptz");
        fechaBloqueo.IsNullable.Should().BeTrue();

        entidad.FindProperty(nameof(RubricaEvaluacion.SumaPesos)).Should().BeNull();
        entidad.FindProperty(nameof(RubricaEvaluacion.DiferenciaPara100)).Should().BeNull();

        var navegacion = entidad.FindNavigation(nameof(RubricaEvaluacion.Criterios))!;
        navegacion.IsCollection.Should().BeTrue();
        navegacion.TargetEntityType.ClrType.Should().Be<CriterioEvaluacion>();
        navegacion.FieldInfo!.Name.Should().Be("_criterios");
        navegacion.GetPropertyAccessMode().Should().Be(PropertyAccessMode.Field);

        entidad.GetIndexes()
            .Where(indice => indice.IsUnique)
            .Select(indice => indice.GetDatabaseName())
            .Should().Contain("ux_rubricas_programa_convocatoria");

        var checks = entidad.GetCheckConstraints().ToList();
        checks.Select(check => check.Name)
            .Should().Contain([
                "ck_rubricas_estado_valido",
                "ck_rubricas_motivo_bloqueo_valido",
                "ck_rubricas_bloqueo_coherente"
            ]);

        var checkMotivo = checks.Single(check => check.Name == "ck_rubricas_motivo_bloqueo_valido");
        checkMotivo.Sql.Should().Contain("motivo_bloqueo IS NULL");
        checkMotivo.Sql.Should().Contain("'INICIO_EVALUACION'");
        checkMotivo.Sql.Should().Contain("'PRIMERA_CALIFICACION'");

        var checkCoherencia = checks.Single(check => check.Name == "ck_rubricas_bloqueo_coherente");
        checkCoherencia.Sql.Should().Contain("estado = 'BLOQUEADA'");
        checkCoherencia.Sql.Should().Contain("motivo_bloqueo IS NOT NULL");
        checkCoherencia.Sql.Should().Contain("fecha_bloqueo IS NOT NULL");
        checkCoherencia.Sql.Should().Contain("'BORRADOR'");
        checkCoherencia.Sql.Should().Contain("'PUBLICADA'");
        checkCoherencia.Sql.Should().Contain("motivo_bloqueo IS NULL");
        checkCoherencia.Sql.Should().Contain("fecha_bloqueo IS NULL");

        contexto.Rubricas.Should().NotBeNull();
    }

    [Fact]
    public void Debe_MapearCriterioEvaluacionConRelacionIndiceUnicoYRestricciones_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var entidad = modeloDiseno.FindEntityType(typeof(CriterioEvaluacion));

        entidad.Should().NotBeNull();
        entidad!.GetSchema().Should().Be(PostulacionesDbContext.Esquema);
        entidad.GetTableName().Should().Be("criterios_evaluacion");
        entidad.FindPrimaryKey()!.GetName().Should().Be("pk_criterios_evaluacion");

        var propiedadId = entidad.FindProperty(nameof(CriterioEvaluacion.Id))!;
        propiedadId.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadId.GetColumnName().Should().Be("id");

        var propiedadVersion = entidad.FindProperty(nameof(CriterioEvaluacion.Version))!;
        propiedadVersion.ValueGenerated.Should().Be(ValueGenerated.Never);
        propiedadVersion.IsConcurrencyToken.Should().BeTrue();

        var rubricaId = entidad.FindProperty(nameof(CriterioEvaluacion.RubricaEvaluacionId))!;
        rubricaId.GetColumnName().Should().Be("rubrica_evaluacion_id");
        rubricaId.IsNullable.Should().BeFalse();

        var nombre = entidad.FindProperty(nameof(CriterioEvaluacion.Nombre))!;
        nombre.GetColumnName().Should().Be("nombre");
        nombre.IsNullable.Should().BeFalse();
        nombre.GetMaxLength().Should().Be(200);

        var descripcion = entidad.FindProperty(nameof(CriterioEvaluacion.Descripcion))!;
        descripcion.GetColumnName().Should().Be("descripcion");
        descripcion.IsNullable.Should().BeFalse();
        descripcion.GetMaxLength().Should().Be(1000);

        var peso = entidad.FindProperty(nameof(CriterioEvaluacion.PesoPorcentual))!;
        peso.GetColumnName().Should().Be("peso_porcentual");
        peso.GetPrecision().Should().Be(5);
        peso.GetScale().Should().Be(2);
        peso.IsNullable.Should().BeFalse();

        var puntajeMinimo = entidad.FindProperty(nameof(CriterioEvaluacion.PuntajeMinimo))!;
        puntajeMinimo.GetColumnName().Should().Be("puntaje_minimo");
        puntajeMinimo.GetPrecision().Should().Be(7);
        puntajeMinimo.GetScale().Should().Be(2);
        puntajeMinimo.IsNullable.Should().BeFalse();

        var puntajeMaximo = entidad.FindProperty(nameof(CriterioEvaluacion.PuntajeMaximo))!;
        puntajeMaximo.GetColumnName().Should().Be("puntaje_maximo");
        puntajeMaximo.GetPrecision().Should().Be(7);
        puntajeMaximo.GetScale().Should().Be(2);
        puntajeMaximo.IsNullable.Should().BeFalse();

        var orden = entidad.FindProperty(nameof(CriterioEvaluacion.Orden))!;
        orden.GetColumnName().Should().Be("orden");
        orden.IsNullable.Should().BeFalse();

        var claveForanea = entidad.GetForeignKeys()
            .Single(clave => clave.PrincipalEntityType.ClrType == typeof(RubricaEvaluacion));
        claveForanea.GetConstraintName().Should().Be("fk_criterios_evaluacion_rubricas");
        claveForanea.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        entidad.GetIndexes()
            .Where(indice => indice.IsUnique)
            .Select(indice => indice.GetDatabaseName())
            .Should().Contain("ux_criterios_evaluacion_rubrica_orden");

        var nombresChecks = entidad.GetCheckConstraints()
            .Select(check => check.Name)
            .ToList();

        nombresChecks.Should().Contain("ck_criterios_evaluacion_peso_rango");
        nombresChecks.Should().Contain("ck_criterios_evaluacion_puntaje_rango");
        nombresChecks.Should().Contain("ck_criterios_evaluacion_orden_positivo");

        contexto.CriteriosEvaluacion.Should().NotBeNull();
    }
}
