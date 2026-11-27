using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Consolidacion.Agregados;

public sealed class ResultadoConsolidadoRankingCasoLimiteTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000007001");
    private static readonly Guid _postulacionId = Guid.Parse("01968000-0000-7000-8000-000000007002");
    private static readonly Guid _decisorId = Guid.Parse("01968000-0000-7000-8000-000000007003");
    private static readonly Guid _otroDecisorId = Guid.Parse("01968000-0000-7000-8000-000000007004");
    private static readonly DateTimeOffset _fechaConsolidacion = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_RegistrarDecisionCasoLimiteNormalizadaYActualizarAuditoria_Cuando_EstaEnProvisional()
    {
        var resultado = CrearResultado(puntaje: 88.00m);
        var versionInicial = resultado.Version;
        var fechaDecisionConOffset = new DateTimeOffset(2026, 11, 26, 14, 30, 0, TimeSpan.FromHours(-4));

        Assert.Equal(10, ResultadoConsolidadoRanking.LongitudMinimaJustificacionCasoLimite);
        Assert.Equal(1000, ResultadoConsolidadoRanking.LongitudMaximaJustificacionCasoLimite);
        Assert.False(resultado.TieneDecisionCasoLimite);
        Assert.Null(resultado.TipoCasoLimite);
        Assert.Null(resultado.SeleccionadoEnCasoLimite);
        Assert.Null(resultado.JustificacionDesempateCasoLimite);
        Assert.Null(resultado.DecididoPorUsuarioId);
        Assert.Null(resultado.FechaDecisionCasoLimite);

        resultado.RegistrarDecisionCasoLimite(
            TipoCasoLimite.EmpatePorCupo,
            seleccionado: true,
            justificacion: "   Priorización por mayor vulnerabilidad socioeconómica acreditada.   ",
            fechaDecision: fechaDecisionConOffset,
            usuarioDecisorId: _decisorId);

        Assert.True(resultado.TieneDecisionCasoLimite);
        Assert.Equal(TipoCasoLimite.EmpatePorCupo, resultado.TipoCasoLimite);
        Assert.True(resultado.SeleccionadoEnCasoLimite);
        Assert.Equal(
            "Priorización por mayor vulnerabilidad socioeconómica acreditada.",
            resultado.JustificacionDesempateCasoLimite);
        Assert.Equal(_decisorId, resultado.DecididoPorUsuarioId);
        Assert.Equal(fechaDecisionConOffset.ToUniversalTime(), resultado.FechaDecisionCasoLimite);
        Assert.Equal(TimeSpan.Zero, resultado.FechaDecisionCasoLimite!.Value.Offset);
        Assert.Equal(versionInicial + 1, resultado.Version);
        Assert.Equal(_decisorId, resultado.UsuarioModificacionId);
        Assert.Equal(fechaDecisionConOffset.ToUniversalTime(), resultado.FechaUltimaModificacion);
        Assert.Equal(EstadoRanking.Provisional, resultado.EstadoConfirmacionRanking);
    }

    [Theory]
    [InlineData("")]
    [InlineData("         ")]
    [InlineData("123456789")]
    [InlineData("   123456789   ")]
    public void Debe_RechazarJustificacionInvalida_Cuando_EstaVaciaOEsMenorAlMinimo(string justificacionInvalida)
    {
        var resultado = CrearResultado(puntaje: 88.00m);

        var excepcion = Assert.Throws<JustificacionCasoLimiteInvalidaException>(() =>
            resultado.RegistrarDecisionCasoLimite(
                TipoCasoLimite.EmpatePorCupo,
                seleccionado: true,
                justificacion: justificacionInvalida,
                fechaDecision: _fechaConsolidacion.AddHours(1),
                usuarioDecisorId: _decisorId));

        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.Equal("JUSTIFICACION_CASO_LIMITE_REQUERIDA", excepcion.Codigo);
        Assert.False(resultado.TieneDecisionCasoLimite);
    }

    [Fact]
    public void Debe_RechazarJustificacionInvalida_Cuando_SuperaElMaximoPermitido()
    {
        var resultado = CrearResultado(puntaje: 88.00m);
        var justificacionExcesiva = new string('a', 1001);

        var excepcion = Assert.Throws<JustificacionCasoLimiteInvalidaException>(() =>
            resultado.RegistrarDecisionCasoLimite(
                TipoCasoLimite.CasoLimitePresupuestario,
                seleccionado: true,
                justificacion: justificacionExcesiva,
                fechaDecision: _fechaConsolidacion.AddHours(1),
                usuarioDecisorId: _decisorId));

        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.Equal("JUSTIFICACION_CASO_LIMITE_REQUERIDA", excepcion.Codigo);
        Assert.False(resultado.TieneDecisionCasoLimite);
    }

    [Fact]
    public void Debe_AceptarJustificacionEnLosLimitesExactos_De10Y1000Caracteres()
    {
        var rMin = CrearResultado(puntaje: 88.00m);
        rMin.RegistrarDecisionCasoLimite(
            TipoCasoLimite.EmpatePorCupo,
            true,
            new string('x', 10),
            _fechaConsolidacion.AddHours(1),
            _decisorId);
        Assert.Equal(10, rMin.JustificacionDesempateCasoLimite!.Length);

        var rMax = CrearResultado(puntaje: 88.00m);
        rMax.RegistrarDecisionCasoLimite(
            TipoCasoLimite.EmpatePorPresupuesto,
            false,
            new string('y', 1000),
            _fechaConsolidacion.AddHours(1),
            _decisorId);
        Assert.Equal(1000, rMax.JustificacionDesempateCasoLimite!.Length);
    }

    [Fact]
    public void Debe_RechazarTipoCasoLimiteFueraDeCatalogoYArgumentosTemporalesOUsuarioVacios()
    {
        var resultado = CrearResultado(puntaje: 88.00m);

        var exTipo = Assert.Throws<CasoLimiteInvalidoException>(() =>
            resultado.RegistrarDecisionCasoLimite(
                (TipoCasoLimite)99,
                seleccionado: true,
                justificacion: "Justificación suficientemente extensa para la decisión.",
                fechaDecision: _fechaConsolidacion.AddHours(1),
                usuarioDecisorId: _decisorId));

        Assert.Equal(422, exTipo.EstadoHttp);
        Assert.Equal("CASO_LIMITE_INVALIDO", exTipo.Codigo);

        Assert.Throws<ArgumentException>(() =>
            resultado.RegistrarDecisionCasoLimite(
                TipoCasoLimite.EmpatePorCupo,
                true,
                "Justificación suficientemente extensa para la decisión.",
                default,
                _decisorId));

        Assert.Throws<ArgumentException>(() =>
            resultado.RegistrarDecisionCasoLimite(
                TipoCasoLimite.EmpatePorCupo,
                true,
                "Justificación suficientemente extensa para la decisión.",
                _fechaConsolidacion.AddHours(1),
                Guid.Empty));
    }

    [Fact]
    public void Debe_RechazarSobrescritura_Cuando_DecisionCasoLimiteYaFueRegistrada()
    {
        var resultado = CrearResultado(puntaje: 88.00m);
        var fechaOriginal = _fechaConsolidacion.AddHours(1);
        const string justificacionOriginal = "Decisión inicial auditada por el coordinador.";

        resultado.RegistrarDecisionCasoLimite(
            TipoCasoLimite.EmpatePorCupo,
            seleccionado: true,
            justificacion: justificacionOriginal,
            fechaDecision: fechaOriginal,
            usuarioDecisorId: _decisorId);

        var versionTrasDecision = resultado.Version;

        var excepcion = Assert.Throws<CasoLimiteYaResueltoException>(() =>
            resultado.RegistrarDecisionCasoLimite(
                TipoCasoLimite.EmpatePorCupo,
                seleccionado: false,
                justificacion: "Intento indebido de modificar la decisión ya persistida.",
                fechaDecision: _fechaConsolidacion.AddHours(2),
                usuarioDecisorId: _otroDecisorId));

        Assert.Equal(409, excepcion.EstadoHttp);
        Assert.Equal("CASO_LIMITE_YA_RESUELTO", excepcion.Codigo);
        Assert.True(resultado.SeleccionadoEnCasoLimite);
        Assert.Equal(justificacionOriginal, resultado.JustificacionDesempateCasoLimite);
        Assert.Equal(_decisorId, resultado.DecididoPorUsuarioId);
        Assert.Equal(fechaOriginal, resultado.FechaDecisionCasoLimite);
        Assert.Equal(versionTrasDecision, resultado.Version);
    }

    [Fact]
    public void Debe_RechazarRecalculoMutanteYSerIdempotenteEnRecalculoIdentico_Cuando_TieneDecisionCasoLimite()
    {
        var calculoInicial = CrearCalculo(puntaje: 88.00m);
        var resultado = ResultadoConsolidadoRanking.Crear(calculoInicial, _fechaConsolidacion, UsuarioSistema.Id);

        resultado.RegistrarDecisionCasoLimite(
            TipoCasoLimite.EmpatePorPresupuesto,
            seleccionado: true,
            justificacion: "Criterio de desempate presupuestario aprobado en comité.",
            fechaDecision: _fechaConsolidacion.AddHours(1),
            usuarioDecisorId: _decisorId);

        var versionAntes = resultado.Version;

        // Idempotente cuando el cálculo coincide exactamente
        var modificado = resultado.Recalcular(calculoInicial, _fechaConsolidacion.AddHours(2), _otroDecisorId);
        Assert.False(modificado);
        Assert.Equal(versionAntes, resultado.Version);

        // Rechazo 409 CASO_LIMITE_YA_RESUELTO cuando el recálculo cambia el puntaje
        var calculoDistinto = CrearCalculo(puntaje: 91.00m);
        var excepcion = Assert.Throws<CasoLimiteYaResueltoException>(() =>
            resultado.Recalcular(calculoDistinto, _fechaConsolidacion.AddHours(3), _otroDecisorId));

        Assert.Equal(409, excepcion.EstadoHttp);
        Assert.Equal("CASO_LIMITE_YA_RESUELTO", excepcion.Codigo);
        Assert.Equal(88.00m, resultado.PuntajeConsolidado);
        Assert.Equal(versionAntes, resultado.Version);
    }

    [Fact]
    public void Debe_PermitirRevisarDiscrepanciaYPreservarDecisionTrasConfirmar_YRechazarRegistroEnConfirmado()
    {
        var resultado = CrearResultado(puntaje: 88.00m, alerta: true);
        var fechaDecision = _fechaConsolidacion.AddHours(1);
        const string justificacion = "Decisión documentada para desempate de frontera.";

        resultado.RegistrarDecisionCasoLimite(
            TipoCasoLimite.EmpatePorCupo,
            seleccionado: true,
            justificacion: justificacion,
            fechaDecision: fechaDecision,
            usuarioDecisorId: _decisorId);

        // Revisión de discrepancia sigue permitida en PROVISIONAL sin alterar la decisión
        resultado.MarcarDiscrepanciaRevisada(
            "Revisión formal de la discrepancia entre evaluadores.",
            _fechaConsolidacion.AddHours(2),
            _decisorId);

        Assert.True(resultado.DiscrepanciaRevisada);
        Assert.True(resultado.TieneDecisionCasoLimite);
        Assert.Equal(justificacion, resultado.JustificacionDesempateCasoLimite);

        // ConfirmarRanking preserva los 5 campos de decisión
        resultado.ConfirmarRanking(
            posicionRanking: 2,
            montoEstimadoBeca: 1500.00m,
            clasificacion: ClasificacionRanking.DentroDeCupo,
            fechaConfirmacion: _fechaConsolidacion.AddHours(3),
            usuarioConfirmadorId: _decisorId);

        Assert.Equal(EstadoRanking.Confirmado, resultado.EstadoConfirmacionRanking);
        Assert.True(resultado.TieneDecisionCasoLimite);
        Assert.Equal(TipoCasoLimite.EmpatePorCupo, resultado.TipoCasoLimite);
        Assert.True(resultado.SeleccionadoEnCasoLimite);
        Assert.Equal(justificacion, resultado.JustificacionDesempateCasoLimite);
        Assert.Equal(_decisorId, resultado.DecididoPorUsuarioId);
        Assert.Equal(fechaDecision, resultado.FechaDecisionCasoLimite);

        // Registrar decisión en estado CONFIRMADO lanza 409 RANKING_YA_CONFIRMADO
        var resultadoConfirmadoSinDecision = CrearResultado(puntaje: 95.00m);
        resultadoConfirmadoSinDecision.ConfirmarRanking(
            1,
            1500.00m,
            ClasificacionRanking.DentroDeCupo,
            _fechaConsolidacion.AddHours(3),
            _decisorId);

        var exConfirmado = Assert.Throws<RankingYaConfirmadoException>(() =>
            resultadoConfirmadoSinDecision.RegistrarDecisionCasoLimite(
                TipoCasoLimite.EmpatePorCupo,
                true,
                "Intento tardío sobre ranking ya confirmado.",
                _fechaConsolidacion.AddHours(4),
                _decisorId));

        Assert.Equal(409, exConfirmado.EstadoHttp);
        Assert.Equal("RANKING_YA_CONFIRMADO", exConfirmado.Codigo);
    }

    private static ResultadoCalculoConsolidacion CrearCalculo(decimal puntaje, bool alerta = false)
        => new(
            _convocatoriaId,
            _postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: alerta ? 24.00m : 4.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: alerta);

    private static ResultadoConsolidadoRanking CrearResultado(decimal puntaje, bool alerta = false)
        => ResultadoConsolidadoRanking.Crear(CrearCalculo(puntaje, alerta), _fechaConsolidacion, UsuarioSistema.Id);
}
