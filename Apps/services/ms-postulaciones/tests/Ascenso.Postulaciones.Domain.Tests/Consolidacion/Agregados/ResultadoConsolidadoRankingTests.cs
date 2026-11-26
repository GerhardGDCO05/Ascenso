using System.Reflection;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Consolidacion.Agregados;

public sealed class ResultadoConsolidadoRankingTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000001001");
    private static readonly Guid _otraConvocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000001002");
    private static readonly Guid _postulacionId = Guid.Parse("01968000-0000-7000-8000-000000001003");
    private static readonly Guid _otraPostulacionId = Guid.Parse("01968000-0000-7000-8000-000000001004");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000001005");
    private static readonly Guid _otroCoordinadorId = Guid.Parse("01968000-0000-7000-8000-000000001006");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 26, 10, 0, 0, TimeSpan.FromHours(-4));

    [Fact]
    public void Debe_CrearEnEstadoProvisionalConFechaUtc_Cuando_ElCalculoEsValido()
    {
        var calculo = CrearCalculo(puntaje: 85.00m, diferencia: 10.00m, alerta: false);

        var resultado = ResultadoConsolidadoRanking.Crear(calculo, _fechaBase, UsuarioSistema.Id);

        Assert.IsAssignableFrom<AggregateRoot>(resultado);
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal(_convocatoriaId, resultado.ConvocatoriaId);
        Assert.Equal(_postulacionId, resultado.PostulacionId);
        Assert.False(resultado.ExcepcionEvaluadorUnico);
        Assert.Null(resultado.JustificacionEvaluadorUnico);
        Assert.Null(resultado.AutorizadorExcepcionId);
        Assert.Equal(2, resultado.CantidadEvaluacionesValidas);
        Assert.Equal(85.00m, resultado.PuntajeConsolidado);
        Assert.Equal(10.00m, resultado.DiferenciaEntreEvaluadores);
        Assert.Equal(20.00m, resultado.UmbralDiscrepanciaAplicado);
        Assert.False(resultado.AlertaDiscrepancia);
        Assert.False(resultado.DiscrepanciaRevisada);
        Assert.Null(resultado.ComentarioRevisionDiscrepancia);
        Assert.Null(resultado.RevisadoPorUsuarioId);
        Assert.Null(resultado.FechaRevisionDiscrepancia);
        Assert.Equal(EstadoRanking.Provisional, resultado.EstadoConfirmacionRanking);
        Assert.Equal(_fechaBase.ToUniversalTime(), resultado.FechaConsolidacion);
        Assert.Equal(TimeSpan.Zero, resultado.FechaConsolidacion.Offset);
        Assert.Equal(1, resultado.Version);
        Assert.True(resultado.CoincideConCalculo(calculo));
    }

    [Fact]
    public void Debe_SerIdempotenteSinAlterarVersionNiRevision_Cuando_SeRecalculaConSnapshotIdentico()
    {
        var calculoInicial = CrearCalculo(puntaje: 82.50m, diferencia: 25.00m, alerta: true);
        var resultado = ResultadoConsolidadoRanking.Crear(calculoInicial, _fechaBase, UsuarioSistema.Id);
        var fechaRevision = _fechaBase.AddHours(1);
        resultado.MarcarDiscrepanciaRevisada(
            "Revisión confirmada tras contrastar rúbricas.",
            fechaRevision,
            _coordinadorId);

        var versionAntes = resultado.Version;
        var fechaConsolidacionAntes = resultado.FechaConsolidacion;

        var cambio = resultado.Recalcular(calculoInicial, _fechaBase.AddHours(2), _otroCoordinadorId);

        Assert.False(cambio);
        Assert.Equal(versionAntes, resultado.Version);
        Assert.Equal(fechaConsolidacionAntes, resultado.FechaConsolidacion);
        Assert.True(resultado.DiscrepanciaRevisada);
        Assert.Equal("Revisión confirmada tras contrastar rúbricas.", resultado.ComentarioRevisionDiscrepancia);
        Assert.Equal(_coordinadorId, resultado.RevisadoPorUsuarioId);
        Assert.Equal(fechaRevision.ToUniversalTime(), resultado.FechaRevisionDiscrepancia);
    }

    [Fact]
    public void Debe_ActualizarMetricasYReiniciarRevision_Cuando_SeRecalculaConSnapshotDistinto()
    {
        var calculoInicial = CrearCalculo(puntaje: 82.50m, diferencia: 25.00m, alerta: true);
        var resultado = ResultadoConsolidadoRanking.Crear(calculoInicial, _fechaBase, UsuarioSistema.Id);
        resultado.MarcarDiscrepanciaRevisada(
            "Revisión inicial previa al tercer evaluador.",
            _fechaBase.AddHours(1),
            _coordinadorId);

        var versionAntes = resultado.Version;
        var fechaRecalculo = _fechaBase.AddHours(3);
        var nuevoCalculo = CrearCalculo(puntaje: 83.33m, diferencia: 25.00m, alerta: true, cantidad: 3);

        var cambio = resultado.Recalcular(nuevoCalculo, fechaRecalculo, _otroCoordinadorId);

        Assert.True(cambio);
        Assert.Equal(versionAntes + 1, resultado.Version);
        Assert.Equal(3, resultado.CantidadEvaluacionesValidas);
        Assert.Equal(83.33m, resultado.PuntajeConsolidado);
        Assert.Equal(25.00m, resultado.DiferenciaEntreEvaluadores);
        Assert.True(resultado.AlertaDiscrepancia);
        Assert.False(resultado.DiscrepanciaRevisada);
        Assert.Null(resultado.ComentarioRevisionDiscrepancia);
        Assert.Null(resultado.RevisadoPorUsuarioId);
        Assert.Null(resultado.FechaRevisionDiscrepancia);
        Assert.Equal(fechaRecalculo.ToUniversalTime(), resultado.FechaConsolidacion);
        Assert.Equal(_otroCoordinadorId, resultado.UsuarioModificacionId);
    }

    [Fact]
    public void Debe_DesactivarAlertaYLimpiarRevision_Cuando_RecalculoReduceDiferenciaBajoUmbral()
    {
        var calculoInicial = CrearCalculo(puntaje: 82.50m, diferencia: 25.00m, alerta: true);
        var resultado = ResultadoConsolidadoRanking.Crear(calculoInicial, _fechaBase, UsuarioSistema.Id);
        resultado.MarcarDiscrepanciaRevisada(
            "Revisión inicial antes de reemplazo de evaluador.",
            _fechaBase.AddHours(1),
            _coordinadorId);

        var nuevoCalculo = CrearCalculo(puntaje: 86.00m, diferencia: 12.00m, alerta: false);
        var cambio = resultado.Recalcular(nuevoCalculo, _fechaBase.AddHours(2), UsuarioSistema.Id);

        Assert.True(cambio);
        Assert.False(resultado.AlertaDiscrepancia);
        Assert.False(resultado.DiscrepanciaRevisada);
        Assert.Null(resultado.ComentarioRevisionDiscrepancia);
        Assert.Null(resultado.RevisadoPorUsuarioId);
        Assert.Null(resultado.FechaRevisionDiscrepancia);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(1000)]
    public void Debe_MarcarDiscrepanciaRevisadaNormalizandoComentario_Cuando_LongitudEstaEnLosLimites(
        int longitudExacta)
    {
        var calculo = CrearCalculo(puntaje: 75.00m, diferencia: 25.00m, alerta: true);
        var resultado = ResultadoConsolidadoRanking.Crear(calculo, _fechaBase, UsuarioSistema.Id);
        var comentarioEsperado = new string('R', longitudExacta);
        var fechaRevision = new DateTimeOffset(2026, 11, 26, 15, 30, 0, TimeSpan.FromHours(-4));

        resultado.MarcarDiscrepanciaRevisada($"  {comentarioEsperado}  ", fechaRevision, _coordinadorId);

        Assert.True(resultado.DiscrepanciaRevisada);
        Assert.Equal(comentarioEsperado, resultado.ComentarioRevisionDiscrepancia);
        Assert.Equal(_coordinadorId, resultado.RevisadoPorUsuarioId);
        Assert.Equal(fechaRevision.ToUniversalTime(), resultado.FechaRevisionDiscrepancia);
        Assert.Equal(TimeSpan.Zero, resultado.FechaRevisionDiscrepancia!.Value.Offset);
        Assert.Equal(2, resultado.Version);
        Assert.Equal(_coordinadorId, resultado.UsuarioModificacionId);
    }

    [Fact]
    public void Debe_RechazarMarcarDiscrepanciaRevisada_Cuando_NoHayAlertaActiva()
    {
        var calculo = CrearCalculo(puntaje: 85.00m, diferencia: 10.00m, alerta: false);
        var resultado = ResultadoConsolidadoRanking.Crear(calculo, _fechaBase, UsuarioSistema.Id);

        var excepcion = Assert.Throws<DiscrepanciaNoDetectadaException>(
            () => resultado.MarcarDiscrepanciaRevisada(
                "Intento de revisar sin alerta.",
                _fechaBase.AddHours(1),
                _coordinadorId));

        Assert.Equal(DiscrepanciaNoDetectadaException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.False(resultado.DiscrepanciaRevisada);
        Assert.Equal(1, resultado.Version);
    }

    [Fact]
    public void Debe_RechazarMarcarDiscrepanciaRevisada_Cuando_YaFueRevisadaPreviamente()
    {
        var calculo = CrearCalculo(puntaje: 75.00m, diferencia: 25.00m, alerta: true);
        var resultado = ResultadoConsolidadoRanking.Crear(calculo, _fechaBase, UsuarioSistema.Id);
        resultado.MarcarDiscrepanciaRevisada(
            "Comentario original verificado.",
            _fechaBase.AddHours(1),
            _coordinadorId);

        var excepcion = Assert.Throws<DiscrepanciaYaRevisadaException>(
            () => resultado.MarcarDiscrepanciaRevisada(
                "Intento de sobrescribir la revisión original.",
                _fechaBase.AddHours(2),
                _otroCoordinadorId));

        Assert.Equal(DiscrepanciaYaRevisadaException.CodigoError, excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
        Assert.Equal("Comentario original verificado.", resultado.ComentarioRevisionDiscrepancia);
        Assert.Equal(_coordinadorId, resultado.RevisadoPorUsuarioId);
        Assert.Equal(2, resultado.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("         ")]
    [InlineData("123456789")]
    [InlineData("   123456789   ")]
    public void Debe_RechazarMarcarDiscrepanciaRevisada_Cuando_ElComentarioEsInvalido(
        string? comentarioInvalido)
    {
        var calculo = CrearCalculo(puntaje: 75.00m, diferencia: 25.00m, alerta: true);
        var resultado = ResultadoConsolidadoRanking.Crear(calculo, _fechaBase, UsuarioSistema.Id);

        var excepcion = Assert.Throws<ComentarioRevisionDiscrepanciaInvalidoException>(
            () => resultado.MarcarDiscrepanciaRevisada(
                comentarioInvalido!,
                _fechaBase.AddHours(1),
                _coordinadorId));

        Assert.Equal(ComentarioRevisionDiscrepanciaInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.False(resultado.DiscrepanciaRevisada);
    }

    [Fact]
    public void Debe_RechazarMarcarDiscrepanciaRevisada_Cuando_ElComentarioSuperaMilCaracteres()
    {
        var calculo = CrearCalculo(puntaje: 75.00m, diferencia: 25.00m, alerta: true);
        var resultado = ResultadoConsolidadoRanking.Crear(calculo, _fechaBase, UsuarioSistema.Id);
        var comentarioExcesivo = new string('X', ResultadoConsolidadoRanking.LongitudMaximaComentarioRevision + 1);

        var excepcion = Assert.Throws<ComentarioRevisionDiscrepanciaInvalidoException>(
            () => resultado.MarcarDiscrepanciaRevisada(
                comentarioExcesivo,
                _fechaBase.AddHours(1),
                _coordinadorId));

        Assert.Equal(ComentarioRevisionDiscrepanciaInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarRecalculoYRevision_Cuando_ElEstadoEsConfirmado()
    {
        var calculo = CrearCalculo(puntaje: 75.00m, diferencia: 25.00m, alerta: true);
        var resultado = ResultadoConsolidadoRanking.Crear(calculo, _fechaBase, UsuarioSistema.Id);
        FijarEstadoRanking(resultado, EstadoRanking.Confirmado);

        var exRecalcular = Assert.Throws<RankingYaConfirmadoException>(
            () => resultado.Recalcular(
                CrearCalculo(puntaje: 80.00m, diferencia: 10.00m, alerta: false),
                _fechaBase.AddHours(1),
                _coordinadorId));

        var exRevisar = Assert.Throws<RankingYaConfirmadoException>(
            () => resultado.MarcarDiscrepanciaRevisada(
                "Intento de revisar en estado confirmado.",
                _fechaBase.AddHours(1),
                _coordinadorId));

        Assert.Equal(RankingYaConfirmadoException.CodigoError, exRecalcular.Codigo);
        Assert.Equal(409, exRecalcular.EstadoHttp);
        Assert.Equal(RankingYaConfirmadoException.CodigoError, exRevisar.Codigo);
        Assert.Equal(409, exRevisar.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarArgumentosInvalidosEIdentidadesDiscordantes_Cuando_SeOperaSobreElAgregado()
    {
        var calculo = CrearCalculo(puntaje: 75.00m, diferencia: 25.00m, alerta: true);
        var resultado = ResultadoConsolidadoRanking.Crear(calculo, _fechaBase, UsuarioSistema.Id);

        Assert.Throws<ArgumentNullException>(() => ResultadoConsolidadoRanking.Crear(null!, _fechaBase, UsuarioSistema.Id));
        Assert.Throws<ArgumentException>(() => ResultadoConsolidadoRanking.Crear(calculo, default, UsuarioSistema.Id));
        Assert.Throws<ArgumentException>(() => ResultadoConsolidadoRanking.Crear(calculo, _fechaBase, Guid.Empty));

        Assert.Throws<ArgumentNullException>(() => resultado.CoincideConCalculo(null!));

        Assert.Throws<ArgumentNullException>(() => resultado.Recalcular(null!, _fechaBase, UsuarioSistema.Id));
        Assert.Throws<ArgumentException>(() => resultado.Recalcular(calculo, default, UsuarioSistema.Id));
        Assert.Throws<ArgumentException>(() => resultado.Recalcular(calculo, _fechaBase, Guid.Empty));

        var calculoOtraPostulacion = new ResultadoCalculoConsolidacion(
            _convocatoriaId, _otraPostulacionId, false, null, null, 2, 80.00m, 10.00m, 20.00m, false);
        var calculoOtraConvocatoria = new ResultadoCalculoConsolidacion(
            _otraConvocatoriaId, _postulacionId, false, null, null, 2, 80.00m, 10.00m, 20.00m, false);

        Assert.Throws<ConsolidacionInvalidaException>(() => resultado.Recalcular(calculoOtraPostulacion, _fechaBase, UsuarioSistema.Id));
        Assert.Throws<ConsolidacionInvalidaException>(() => resultado.Recalcular(calculoOtraConvocatoria, _fechaBase, UsuarioSistema.Id));

        Assert.Throws<ArgumentException>(() => resultado.MarcarDiscrepanciaRevisada("Comentario válido suficiente", default, _coordinadorId));
        Assert.Throws<ArgumentException>(() => resultado.MarcarDiscrepanciaRevisada("Comentario válido suficiente", _fechaBase, Guid.Empty));
    }

    private static ResultadoCalculoConsolidacion CrearCalculo(
        decimal puntaje,
        decimal diferencia,
        bool alerta,
        int cantidad = 2)
        => new(
            _convocatoriaId,
            _postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: cantidad,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: diferencia,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: alerta);

    private static void FijarEstadoRanking(ResultadoConsolidadoRanking resultado, EstadoRanking estado)
    {
        var propiedad = typeof(ResultadoConsolidadoRanking)
            .GetProperty(nameof(ResultadoConsolidadoRanking.EstadoConfirmacionRanking), BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(propiedad);
        propiedad.SetValue(resultado, estado);
    }
}
