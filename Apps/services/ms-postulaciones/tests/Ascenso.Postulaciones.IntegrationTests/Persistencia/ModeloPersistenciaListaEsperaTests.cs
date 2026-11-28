using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class ModeloPersistenciaListaEsperaTests
{
    private static PostulacionesDbContext CrearContextoModelo()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    private static DbUpdateException CrearDbUpdateUnicidad(string constraintName)
    {
        var pgException = new PostgresException(
            messageText: "duplicate key value violates unique constraint",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.UniqueViolation,
            constraintName: constraintName);

        return new DbUpdateException("Error al guardar cambios.", pgException);
    }

    [Fact]
    public void Debe_ExponerDbSetYMapearColumnasIndicesYChecksDeEntradaListaEspera_Cuando_SeConstruyeElModelo()
    {
        using var contexto = CrearContextoModelo();
        contexto.EntradasListaEspera.Should().NotBeNull();

        var modeloDiseno = contexto.GetService<IDesignTimeModel>().Model;
        var entidad = modeloDiseno.FindEntityType(typeof(EntradaListaEspera));

        entidad.Should().NotBeNull();
        entidad!.GetSchema().Should().Be(PostulacionesDbContext.Esquema);
        entidad.GetTableName().Should().Be("entradas_lista_espera");
        entidad.FindPrimaryKey()!.GetName().Should().Be("pk_entradas_lista_espera");

        entidad.FindProperty(nameof(EntradaListaEspera.EventosDominio)).Should().BeNull();
        entidad.FindProperty(nameof(EntradaListaEspera.EstaPromovida)).Should().BeNull();
        entidad.FindProperty(nameof(EntradaListaEspera.EstaPendiente)).Should().BeNull();

        var id = entidad.FindProperty(nameof(EntradaListaEspera.Id))!;
        id.GetColumnName().Should().Be("id");
        id.GetColumnType().Should().Be("uuid");
        id.ValueGenerated.Should().Be(ValueGenerated.Never);

        var version = entidad.FindProperty(nameof(EntradaListaEspera.Version))!;
        version.GetColumnName().Should().Be("version");
        version.IsConcurrencyToken.Should().BeTrue();
        version.ValueGenerated.Should().Be(ValueGenerated.Never);

        var convocatoriaId = entidad.FindProperty(nameof(EntradaListaEspera.ConvocatoriaId))!;
        convocatoriaId.GetColumnName().Should().Be("convocatoria_id");
        convocatoriaId.GetColumnType().Should().Be("uuid");
        convocatoriaId.IsNullable.Should().BeFalse();

        var programaBecaId = entidad.FindProperty(nameof(EntradaListaEspera.ProgramaBecaId))!;
        programaBecaId.GetColumnName().Should().Be("programa_beca_id");
        programaBecaId.GetColumnType().Should().Be("uuid");
        programaBecaId.IsNullable.Should().BeFalse();

        var postulacionId = entidad.FindProperty(nameof(EntradaListaEspera.PostulacionId))!;
        postulacionId.GetColumnName().Should().Be("postulacion_id");
        postulacionId.GetColumnType().Should().Be("uuid");
        postulacionId.IsNullable.Should().BeFalse();

        var posicionOrden = entidad.FindProperty(nameof(EntradaListaEspera.PosicionOrden))!;
        posicionOrden.GetColumnName().Should().Be("posicion_orden");
        posicionOrden.GetColumnType().Should().Be("integer");
        posicionOrden.IsNullable.Should().BeFalse();

        var monto = entidad.FindProperty(nameof(EntradaListaEspera.MontoEstimadoBecaSnapshot))!;
        monto.GetColumnName().Should().Be("monto_estimado_beca_snapshot");
        monto.GetPrecision().Should().Be(12);
        monto.GetScale().Should().Be(2);
        monto.IsNullable.Should().BeFalse();

        var estado = entidad.FindProperty(nameof(EntradaListaEspera.Estado))!;
        estado.GetColumnName().Should().Be("estado");
        estado.GetColumnType().Should().Be("varchar");
        estado.GetMaxLength().Should().Be(30);
        estado.IsNullable.Should().BeFalse();

        var convertidorEstado = estado.GetValueConverter();
        convertidorEstado.Should().NotBeNull();
        convertidorEstado!.ConvertToProvider(EstadoListaEspera.EnEspera).Should().Be("EN_ESPERA");
        convertidorEstado.ConvertToProvider(EstadoListaEspera.ElegiblePorVacante).Should().Be("ELEGIBLE_POR_VACANTE");
        convertidorEstado.ConvertToProvider(EstadoListaEspera.Promovida).Should().Be("PROMOVIDA");
        convertidorEstado.ConvertFromProvider("EN_ESPERA").Should().Be(EstadoListaEspera.EnEspera);
        convertidorEstado.ConvertFromProvider("ELEGIBLE_POR_VACANTE").Should().Be(EstadoListaEspera.ElegiblePorVacante);
        convertidorEstado.ConvertFromProvider("PROMOVIDA").Should().Be(EstadoListaEspera.Promovida);

        var fechaIngreso = entidad.FindProperty(nameof(EntradaListaEspera.FechaIngreso))!;
        fechaIngreso.GetColumnName().Should().Be("fecha_ingreso");
        fechaIngreso.GetColumnType().Should().Be("timestamptz");
        fechaIngreso.IsNullable.Should().BeFalse();

        var fechaElegibilidad = entidad.FindProperty(nameof(EntradaListaEspera.FechaElegibilidad))!;
        fechaElegibilidad.GetColumnName().Should().Be("fecha_elegibilidad");
        fechaElegibilidad.GetColumnType().Should().Be("timestamptz");
        fechaElegibilidad.IsNullable.Should().BeTrue();

        var fechaPromocion = entidad.FindProperty(nameof(EntradaListaEspera.FechaPromocion))!;
        fechaPromocion.GetColumnName().Should().Be("fecha_promocion");
        fechaPromocion.GetColumnType().Should().Be("timestamptz");
        fechaPromocion.IsNullable.Should().BeTrue();

        var promovidoPor = entidad.FindProperty(nameof(EntradaListaEspera.PromovidoPorUsuarioId))!;
        promovidoPor.GetColumnName().Should().Be("promovido_por_usuario_id");
        promovidoPor.GetColumnType().Should().Be("uuid");
        promovidoPor.IsNullable.Should().BeTrue();

        var motivoPromocion = entidad.FindProperty(nameof(EntradaListaEspera.MotivoPromocion))!;
        motivoPromocion.GetColumnName().Should().Be("motivo_promocion");
        motivoPromocion.GetMaxLength().Should().Be(EntradaListaEspera.LongitudMaximaMotivoPromocion);
        motivoPromocion.IsNullable.Should().BeTrue();

        var indicePostulacion = entidad.GetIndexes()
            .SingleOrDefault(indice => indice.GetDatabaseName() == "ux_entradas_lista_espera_postulacion");
        indicePostulacion.Should().NotBeNull();
        indicePostulacion!.IsUnique.Should().BeTrue();
        indicePostulacion.Properties.Select(p => p.GetColumnName()).Should().Equal("postulacion_id");

        var indiceConvocatoriaPosicion = entidad.GetIndexes()
            .SingleOrDefault(indice => indice.GetDatabaseName() == "ux_entradas_lista_espera_convocatoria_posicion");
        indiceConvocatoriaPosicion.Should().NotBeNull();
        indiceConvocatoriaPosicion!.IsUnique.Should().BeTrue();
        indiceConvocatoriaPosicion.Properties.Select(p => p.GetColumnName())
            .Should().Equal("convocatoria_id", "posicion_orden");

        var indiceConvocatoriaEstadoPosicion = entidad.GetIndexes()
            .SingleOrDefault(indice => indice.GetDatabaseName() == "ix_entradas_lista_espera_convocatoria_estado_posicion");
        indiceConvocatoriaEstadoPosicion.Should().NotBeNull();
        indiceConvocatoriaEstadoPosicion!.IsUnique.Should().BeFalse();
        indiceConvocatoriaEstadoPosicion.Properties.Select(p => p.GetColumnName())
            .Should().Equal("convocatoria_id", "estado", "posicion_orden");

        var checks = entidad.GetCheckConstraints()
            .ToDictionary(check => check.Name!, check => check.Sql);
        checks.Keys.Should().BeEquivalentTo([
            "ck_entradas_lista_espera_estado_valido",
            "ck_entradas_lista_espera_posicion_positiva",
            "ck_entradas_lista_espera_monto_positivo",
            "ck_entradas_lista_espera_estado_coherente"
        ]);

        checks["ck_entradas_lista_espera_estado_valido"].Should().Be(
            "estado IN ('EN_ESPERA', 'ELEGIBLE_POR_VACANTE', 'PROMOVIDA')");
        checks["ck_entradas_lista_espera_posicion_positiva"].Should().Be(
            "posicion_orden >= 1");
        checks["ck_entradas_lista_espera_monto_positivo"].Should().Be(
            "monto_estimado_beca_snapshot > 0");
        checks["ck_entradas_lista_espera_estado_coherente"].Should().Be(
            "(estado = 'EN_ESPERA' AND fecha_elegibilidad IS NULL AND fecha_promocion IS NULL AND promovido_por_usuario_id IS NULL AND motivo_promocion IS NULL) OR (estado = 'ELEGIBLE_POR_VACANTE' AND fecha_elegibilidad IS NOT NULL AND fecha_promocion IS NULL AND promovido_por_usuario_id IS NULL AND motivo_promocion IS NULL) OR (estado = 'PROMOVIDA' AND fecha_promocion IS NOT NULL AND promovido_por_usuario_id IS NOT NULL AND (motivo_promocion IS NULL OR length(btrim(motivo_promocion)) > 0)) OR estado NOT IN ('EN_ESPERA', 'ELEGIBLE_POR_VACANTE', 'PROMOVIDA')");
    }

    [Fact]
    public void Debe_IncluirTablaColumnasIndicesYChecksDeListaEspera_Cuando_SeGeneraElScriptDeCreacion()
    {
        using var contexto = CrearContextoModelo();

        var script = contexto.Database.GenerateCreateScript();

        script.Should().Contain("CREATE TABLE postulaciones.entradas_lista_espera");
        script.Should().Contain("monto_estimado_beca_snapshot numeric(12,2)");
        script.Should().Contain("estado varchar");
        script.Should().Contain("fecha_ingreso timestamptz");
        script.Should().Contain("fecha_elegibilidad timestamptz");
        script.Should().Contain("fecha_promocion timestamptz");
        script.Should().Contain("ux_entradas_lista_espera_postulacion");
        script.Should().Contain("ux_entradas_lista_espera_convocatoria_posicion");
        script.Should().Contain("ix_entradas_lista_espera_convocatoria_estado_posicion");
        script.Should().Contain("ck_entradas_lista_espera_estado_valido");
        script.Should().Contain("ck_entradas_lista_espera_posicion_positiva");
        script.Should().Contain("ck_entradas_lista_espera_monto_positivo");
        script.Should().Contain("ck_entradas_lista_espera_estado_coherente");
    }

    [Fact]
    public void Debe_TraducirIndicesUnicosDeListaEspera_Cuando_FallaRestriccionUnicaEnPostgreSql()
    {
        var excepcionPostulacion = CrearDbUpdateUnicidad(
            TraductorExcepcionesPersistencia.IndiceEntradaListaEsperaPostulacionUnico);
        var traducidaPostulacion = TraductorExcepcionesPersistencia.TraducirSiAplica(excepcionPostulacion);

        var duplicada = traducidaPostulacion.Should().BeOfType<EntradaListaEsperaDuplicadaException>().Subject;
        duplicada.Codigo.Should().Be(EntradaListaEsperaDuplicadaException.CodigoError);
        duplicada.EstadoHttp.Should().Be(409);

        var excepcionPosicion = CrearDbUpdateUnicidad(
            TraductorExcepcionesPersistencia.IndiceEntradaListaEsperaConvocatoriaPosicionUnico);
        var traducidaPosicion = TraductorExcepcionesPersistencia.TraducirSiAplica(excepcionPosicion);

        var conflicto = traducidaPosicion.Should().BeOfType<ExcepcionConflictoConcurrencia>().Subject;
        conflicto.Codigo.Should().Be(ExcepcionConflictoConcurrencia.CodigoRestriccionUnicidad);
        conflicto.EstadoHttp.Should().Be(409);
    }
}
