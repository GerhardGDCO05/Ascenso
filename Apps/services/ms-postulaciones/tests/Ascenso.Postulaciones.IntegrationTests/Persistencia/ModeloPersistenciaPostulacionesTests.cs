using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Domain.Postulantes.Entidades;
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
    }
}
