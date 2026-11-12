using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Ascenso.Becas.Infrastructure.Persistence;
using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class ModeloPersistenciaTests
{
    private readonly PostgreSqlFixture _fixture;

    public ModeloPersistenciaTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData(typeof(Beca), "beca")]
    [InlineData(typeof(OfertaBeca), "oferta_beca")]
    [InlineData(typeof(CompromisoBecario), "compromiso_becario")]
    [InlineData(typeof(HistorialEstadoBeca), "historial_estado_beca")]
    [InlineData(typeof(ProgramaBecaRef), "programa_beca_ref")]
    [InlineData(typeof(CostoMatriculaRef), "costo_matricula_ref")]
    [InlineData(typeof(ConvenioRef), "convenio_ref")]
    [InlineData(typeof(IdempotencyRequest), "idempotency_request")]
    [InlineData(typeof(InboxState), "inbox_state")]
    [InlineData(typeof(OutboxState), "outbox_state")]
    [InlineData(typeof(OutboxMessage), "outbox_message")]
    public void Debe_MapearLasTablasEnSnakeCase_DentroDelEsquemaBecas(
        Type tipo,
        string tablaEsperada)
    {
        using var contexto = _fixture.CrearContexto();
        var entidad = contexto.Model.FindEntityType(tipo);

        Assert.NotNull(entidad);
        Assert.Equal(BecasDbContext.Esquema, entidad.GetSchema());
        Assert.Equal(tablaEsperada, entidad.GetTableName());
    }

    [Theory]
    [InlineData(typeof(Beca), nameof(Beca.PostulanteEstudianteId), "postulante_estudiante_id")]
    [InlineData(typeof(Beca), nameof(Beca.CadenaBecaId), "cadena_beca_id")]
    [InlineData(typeof(Beca), nameof(Beca.MontoComprometidoPeriodoActual), "monto_comprometido_periodo_actual")]
    [InlineData(typeof(OfertaBeca), nameof(OfertaBeca.FechaVencimientoAceptacion), "fecha_vencimiento_aceptacion")]
    [InlineData(typeof(CompromisoBecario), nameof(CompromisoBecario.NumeroIntentoCarga), "numero_intento_carga")]
    [InlineData(typeof(HistorialEstadoBeca), nameof(HistorialEstadoBeca.EstadoBecaNuevo), "estado_beca_nuevo")]
    [InlineData(typeof(ProgramaBecaRef), nameof(ProgramaBecaRef.UltimoEventoId), "ultimo_evento_id")]
    [InlineData(typeof(CostoMatriculaRef), nameof(CostoMatriculaRef.MontoReferenciaMatricula), "monto_referencia_matricula")]
    [InlineData(typeof(ConvenioRef), nameof(ConvenioRef.FechaFin), "fecha_fin")]
    [InlineData(typeof(IdempotencyRequest), nameof(IdempotencyRequest.HashPayload), "hash_payload")]
    [InlineData(typeof(Beca), "CoberturaAplicada.PorcentajeCobertura", "cobertura_porcentaje")]
    [InlineData(typeof(OfertaBeca), "CondicionesSnapshot.PromedioMinimoRenovacion", "condiciones_promedio_minimo_renovacion")]
    [InlineData(typeof(ProgramaBecaRef), "PoliticaCobertura.MontoFijoPeriodo", "politica_cobertura_monto_fijo_periodo")]
    public void Debe_MapearLasColumnasEnSnakeCase(
        Type tipo,
        string rutaPropiedad,
        string columnaEsperada)
    {
        using var contexto = _fixture.CrearContexto();
        var propiedad = ObtenerPropiedad(contexto.Model, tipo, rutaPropiedad);

        Assert.Equal(columnaEsperada, propiedad.GetColumnName());
    }

    [Theory]
    [InlineData(typeof(Beca), nameof(Beca.Id), "uuid")]
    [InlineData(typeof(Beca), nameof(Beca.FechaOtorgamiento), "date")]
    [InlineData(typeof(Beca), nameof(Beca.FechaCreacion), "timestamptz")]
    [InlineData(typeof(Beca), nameof(Beca.MontoComprometidoPeriodoActual), "numeric(18,2)")]
    [InlineData(typeof(Beca), "CoberturaAplicada.PorcentajeCobertura", "numeric(5,2)")]
    [InlineData(typeof(OfertaBeca), nameof(OfertaBeca.FechaVencimientoAceptacion), "timestamptz")]
    [InlineData(typeof(OfertaBeca), "CondicionesSnapshot.PromedioMinimoRenovacion", "numeric(5,2)")]
    [InlineData(typeof(HistorialEstadoBeca), nameof(HistorialEstadoBeca.FechaCambio), "timestamptz")]
    [InlineData(typeof(ProgramaBecaRef), nameof(ProgramaBecaRef.FechaUltimaActualizacion), "timestamptz")]
    [InlineData(typeof(CostoMatriculaRef), nameof(CostoMatriculaRef.FechaVigenciaDesde), "date")]
    public void Debe_ConfigurarLosTiposDeAlmacenamientoDelContrato(
        Type tipo,
        string rutaPropiedad,
        string tipoEsperado)
    {
        using var contexto = _fixture.CrearContexto();
        var propiedad = ObtenerPropiedad(contexto.Model, tipo, rutaPropiedad);

        Assert.Equal(tipoEsperado, propiedad.GetColumnType());
    }

    [Theory]
    [InlineData(typeof(Beca), nameof(Beca.Estado))]
    [InlineData(typeof(Beca), nameof(Beca.TipoCierre))]
    [InlineData(typeof(OfertaBeca), nameof(OfertaBeca.Estado))]
    [InlineData(typeof(CompromisoBecario), nameof(CompromisoBecario.Estado))]
    [InlineData(typeof(HistorialEstadoBeca), nameof(HistorialEstadoBeca.TipoEntidad))]
    [InlineData(typeof(HistorialEstadoBeca), nameof(HistorialEstadoBeca.EstadoBecaAnterior))]
    [InlineData(typeof(HistorialEstadoBeca), nameof(HistorialEstadoBeca.EstadoOfertaNuevo))]
    [InlineData(typeof(OfertaBeca), "CondicionesSnapshot.ConsecuenciaIncumplimiento")]
    [InlineData(typeof(ProgramaBecaRef), "PoliticaCobertura.Modalidad")]
    [InlineData(typeof(ProgramaBecaRef), "CondicionesPermanencia.ConsecuenciaIncumplimiento")]
    public void Debe_PersistirLosEnumsComoVarchar(
        Type tipo,
        string rutaPropiedad)
    {
        using var contexto = _fixture.CrearContexto();
        var propiedad = ObtenerPropiedad(contexto.Model, tipo, rutaPropiedad);

        var converter = propiedad.GetValueConverter();

        Assert.NotNull(converter);
        Assert.Equal(typeof(string), converter.ProviderClrType);
    }

    [Fact]
    public void Debe_MapearLosObjetosDeValorComoTiposPropiosEnLaMismaTabla()
    {
        using var contexto = _fixture.CrearContexto();

        var cobertura = ObtenerNavegacion(contexto.Model, typeof(Beca), "CoberturaAplicada");
        var condicionesOferta = ObtenerNavegacion(contexto.Model, typeof(OfertaBeca), "CondicionesSnapshot");
        var politicaPrograma = ObtenerNavegacion(contexto.Model, typeof(ProgramaBecaRef), "PoliticaCobertura");
        var condicionesPrograma = ObtenerNavegacion(contexto.Model, typeof(ProgramaBecaRef), "CondicionesPermanencia");

        Assert.True(cobertura.TargetEntityType.IsOwned());
        Assert.True(condicionesOferta.TargetEntityType.IsOwned());
        Assert.True(politicaPrograma.TargetEntityType.IsOwned());
        Assert.True(condicionesPrograma.TargetEntityType.IsOwned());
        Assert.Equal("beca", cobertura.TargetEntityType.GetTableName());
        Assert.Equal("oferta_beca", condicionesOferta.TargetEntityType.GetTableName());
        Assert.Equal("programa_beca_ref", politicaPrograma.TargetEntityType.GetTableName());
    }

    [Theory]
    [InlineData(typeof(Beca))]
    [InlineData(typeof(OfertaBeca))]
    [InlineData(typeof(CompromisoBecario))]
    public void Debe_ConfigurarVersionComoTokenDeConcurrencia(Type tipo)
    {
        using var contexto = _fixture.CrearContexto();
        var propiedad = contexto.Model
            .FindEntityType(tipo)!
            .FindProperty(nameof(Beca.Version));

        Assert.NotNull(propiedad);
        Assert.True(propiedad.IsConcurrencyToken);
        Assert.Equal("version", propiedad.GetColumnName());
    }

    [Fact]
    public async Task Debe_RechazarUnaActualizacionConcurrente_Cuando_LaVersionCambio()
    {
        await using var contextoA = _fixture.CrearContexto();
        await using var contextoB = _fixture.CrearContexto();

        var beca = Beca.CrearOtorgada(
            DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());

        contextoA.Becas.Add(beca);
        await contextoA.SaveChangesAsync();

        var copiaA = await contextoA.Becas.SingleAsync(registro => registro.Id == beca.Id);
        var copiaB = await contextoB.Becas.SingleAsync(registro => registro.Id == beca.Id);

        Assert.Equal(1, copiaA.Version);
        Assert.Equal(1, copiaB.Version);

        await contextoA.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE becas.beca
            SET version = version + 1
            WHERE id = {beca.Id}
            """);

        contextoB.Entry(copiaB).Property(registro => registro.Estado).CurrentValue =
            EstadoBeca.Suspendida;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => contextoB.SaveChangesAsync());
    }

    private static IProperty ObtenerPropiedad(
        IModel modelo,
        Type tipo,
        string rutaPropiedad)
    {
        var entidad = modelo.FindEntityType(tipo)
            ?? throw new InvalidOperationException($"No se encontró la entidad {tipo.Name}.");

        var partes = rutaPropiedad.Split('.');
        IProperty? propiedad = null;

        foreach (var parte in partes)
        {
            propiedad = entidad.FindProperty(parte);
            if (propiedad is not null)
            {
                return propiedad;
            }

            entidad = entidad.FindNavigation(parte)?.TargetEntityType
                ?? throw new InvalidOperationException(
                    $"No se encontró la propiedad o navegación {parte} en {tipo.Name}.");
        }

        return propiedad
            ?? throw new InvalidOperationException($"No se encontró la propiedad {rutaPropiedad}.");
    }

    private static INavigation ObtenerNavegacion(
        IModel modelo,
        Type tipo,
        string nombreNavegacion)
        => modelo.FindEntityType(tipo)?.FindNavigation(nombreNavegacion)
            ?? throw new InvalidOperationException(
                $"No se encontró la navegación {nombreNavegacion} en {tipo.Name}.");
}
