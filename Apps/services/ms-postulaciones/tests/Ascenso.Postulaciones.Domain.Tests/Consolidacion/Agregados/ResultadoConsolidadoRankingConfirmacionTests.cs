using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Consolidacion.Agregados;

public sealed class ResultadoConsolidadoRankingConfirmacionTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000004001");
    private static readonly Guid _postulacionId = Guid.Parse("01968000-0000-7000-8000-000000004002");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000004003");
    private static readonly Guid _otroCoordinadorId = Guid.Parse("01968000-0000-7000-8000-000000004004");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 26, 10, 0, 0, TimeSpan.FromHours(-4));

    [Fact]
    public void Debe_MantenerCamposDeConfirmacionNulosYNoSerAdjudicable_Cuando_EstaEnEstadoProvisional()
    {
        var resultado = CrearResultado(puntaje: 88.50m, diferencia: 10.00m, alerta: false);

        Assert.Equal(EstadoRanking.Provisional, resultado.EstadoConfirmacionRanking);
        Assert.Null(resultado.PosicionRanking);
        Assert.Null(resultado.MontoEstimadoBeca);
        Assert.Null(resultado.Clasificacion);
        Assert.Null(resultado.FechaConfirmacionRanking);
        Assert.Null(resultado.ConfirmadoPorUsuarioId);
        Assert.False(resultado.EsAdjudicable);
    }

    [Fact]
    public void Debe_TransicionarAConfirmadoYSerAdjudicable_Cuando_SeConfirmaDentroDeCupo()
    {
        var resultado = CrearResultado(puntaje: 94.00m, diferencia: 8.00m, alerta: false);
        var fechaConfirmacion = new DateTimeOffset(2026, 11, 26, 18, 30, 0, TimeSpan.FromHours(-4));

        resultado.ConfirmarRanking(
            posicionRanking: 1,
            montoEstimadoBeca: 1500.00m,
            clasificacion: ClasificacionRanking.DentroDeCupo,
            fechaConfirmacion: fechaConfirmacion,
            usuarioConfirmadorId: _coordinadorId);

        Assert.Equal(EstadoRanking.Confirmado, resultado.EstadoConfirmacionRanking);
        Assert.Equal(1, resultado.PosicionRanking);
        Assert.Equal(1500.00m, resultado.MontoEstimadoBeca);
        Assert.Equal(ClasificacionRanking.DentroDeCupo, resultado.Clasificacion);
        Assert.Equal(fechaConfirmacion.ToUniversalTime(), resultado.FechaConfirmacionRanking);
        Assert.Equal(TimeSpan.Zero, resultado.FechaConfirmacionRanking!.Value.Offset);
        Assert.Equal(_coordinadorId, resultado.ConfirmadoPorUsuarioId);
        Assert.True(resultado.EsAdjudicable);
        Assert.Equal(2, resultado.Version);
        Assert.Equal(_coordinadorId, resultado.UsuarioModificacionId);
    }

    [Fact]
    public void Debe_TransicionarAConfirmadoYNoSerAdjudicable_Cuando_SeConfirmaEnListaDeEspera()
    {
        var resultado = CrearResultado(puntaje: 78.00m, diferencia: 6.00m, alerta: false);
        var fechaConfirmacion = _fechaBase.AddHours(3);

        resultado.ConfirmarRanking(
            posicionRanking: 4,
            montoEstimadoBeca: 1200.50m,
            clasificacion: ClasificacionRanking.ListaDeEspera,
            fechaConfirmacion: fechaConfirmacion,
            usuarioConfirmadorId: _coordinadorId);

        Assert.Equal(EstadoRanking.Confirmado, resultado.EstadoConfirmacionRanking);
        Assert.Equal(4, resultado.PosicionRanking);
        Assert.Equal(1200.50m, resultado.MontoEstimadoBeca);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, resultado.Clasificacion);
        Assert.Equal(fechaConfirmacion.ToUniversalTime(), resultado.FechaConfirmacionRanking);
        Assert.Equal(_coordinadorId, resultado.ConfirmadoPorUsuarioId);
        Assert.False(resultado.EsAdjudicable);
        Assert.Equal(2, resultado.Version);
    }

    [Fact]
    public void Debe_BloquearRecalculoRevisionYReconfirmacion_Cuando_YaFueConfirmado()
    {
        var resultado = CrearResultado(puntaje: 84.00m, diferencia: 24.00m, alerta: true);
        resultado.ConfirmarRanking(
            posicionRanking: 2,
            montoEstimadoBeca: 1000.00m,
            clasificacion: ClasificacionRanking.DentroDeCupo,
            fechaConfirmacion: _fechaBase.AddHours(2),
            usuarioConfirmadorId: _coordinadorId);

        var exReconfirmar = Assert.Throws<RankingYaConfirmadoException>(
            () => resultado.ConfirmarRanking(
                posicionRanking: 1,
                montoEstimadoBeca: 1200.00m,
                clasificacion: ClasificacionRanking.DentroDeCupo,
                fechaConfirmacion: _fechaBase.AddHours(3),
                usuarioConfirmadorId: _otroCoordinadorId));

        var exRecalcular = Assert.Throws<RankingYaConfirmadoException>(
            () => resultado.Recalcular(
                CrearCalculo(puntaje: 90.00m, diferencia: 10.00m, alerta: false),
                _fechaBase.AddHours(3),
                _otroCoordinadorId));

        var exRevisar = Assert.Throws<RankingYaConfirmadoException>(
            () => resultado.MarcarDiscrepanciaRevisada(
                "Intento de revisar después de confirmar el ranking.",
                _fechaBase.AddHours(3),
                _otroCoordinadorId));

        Assert.Equal(RankingYaConfirmadoException.CodigoError, exReconfirmar.Codigo);
        Assert.Equal(409, exReconfirmar.EstadoHttp);
        Assert.Equal(RankingYaConfirmadoException.CodigoError, exRecalcular.Codigo);
        Assert.Equal(409, exRecalcular.EstadoHttp);
        Assert.Equal(RankingYaConfirmadoException.CodigoError, exRevisar.Codigo);
        Assert.Equal(409, exRevisar.EstadoHttp);
        Assert.Equal(2, resultado.PosicionRanking);
        Assert.Equal(_coordinadorId, resultado.ConfirmadoPorUsuarioId);
        Assert.Equal(2, resultado.Version);
    }

    [Theory]
    [InlineData(0, 1000.00, ClasificacionRanking.DentroDeCupo)]
    [InlineData(-1, 1000.00, ClasificacionRanking.DentroDeCupo)]
    [InlineData(1, 0.00, ClasificacionRanking.DentroDeCupo)]
    [InlineData(1, -50.00, ClasificacionRanking.DentroDeCupo)]
    [InlineData(1, 1000.123, ClasificacionRanking.DentroDeCupo)]
    [InlineData(1, 1000.00, (ClasificacionRanking)999)]
    public void Debe_RechazarConfirmacionConRankingInvalido_Cuando_ParametrosDeNegocioSonInvalidos(
        int posicion,
        decimal monto,
        ClasificacionRanking clasificacion)
    {
        var resultado = CrearResultado(puntaje: 90.00m, diferencia: 5.00m, alerta: false);

        var excepcion = Assert.Throws<RankingInvalidoException>(
            () => resultado.ConfirmarRanking(
                posicion,
                monto,
                clasificacion,
                _fechaBase.AddHours(1),
                _coordinadorId));

        Assert.Equal(RankingInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.Equal(EstadoRanking.Provisional, resultado.EstadoConfirmacionRanking);
        Assert.Null(resultado.PosicionRanking);
        Assert.Null(resultado.MontoEstimadoBeca);
        Assert.Null(resultado.Clasificacion);
        Assert.Null(resultado.FechaConfirmacionRanking);
        Assert.Null(resultado.ConfirmadoPorUsuarioId);
        Assert.Equal(1, resultado.Version);
    }

    [Fact]
    public void Debe_RechazarArgumentosTemporalesOUsuarioVacios_Cuando_SeConfirmaElRanking()
    {
        var resultado = CrearResultado(puntaje: 90.00m, diferencia: 5.00m, alerta: false);

        Assert.Throws<ArgumentException>(
            () => resultado.ConfirmarRanking(
                1,
                1000.00m,
                ClasificacionRanking.DentroDeCupo,
                default,
                _coordinadorId));

        Assert.Throws<ArgumentException>(
            () => resultado.ConfirmarRanking(
                1,
                1000.00m,
                ClasificacionRanking.DentroDeCupo,
                _fechaBase.AddHours(1),
                Guid.Empty));
    }

    private static ResultadoConsolidadoRanking CrearResultado(
        decimal puntaje,
        decimal diferencia,
        bool alerta)
        => ResultadoConsolidadoRanking.Crear(
            CrearCalculo(puntaje, diferencia, alerta),
            _fechaBase,
            UsuarioSistema.Id);

    private static ResultadoCalculoConsolidacion CrearCalculo(
        decimal puntaje,
        decimal diferencia,
        bool alerta)
        => new(
            _convocatoriaId,
            _postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: diferencia,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: alerta);
}
