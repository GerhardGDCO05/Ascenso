using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Consolidacion.ObjetosValor;

public sealed class ObjetosValorCasoLimiteTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000006001");
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000006002");
    private static readonly Guid _resultado1Id = Guid.Parse("01968000-0000-7000-8000-000000006011");
    private static readonly Guid _resultado2Id = Guid.Parse("01968000-0000-7000-8000-000000006012");
    private static readonly Guid _postulacion1Id = Guid.Parse("01968000-0000-7000-8000-000000006021");
    private static readonly Guid _postulacion2Id = Guid.Parse("01968000-0000-7000-8000-000000006022");
    private static readonly Guid _decisorId = Guid.Parse("01968000-0000-7000-8000-000000006031");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 26, 12, 0, 0, TimeSpan.FromHours(-4));

    [Fact]
    public void Debe_CrearCasoLimiteRankingDetectadoConIgualdadEstructural_Cuando_DatosDeEmpateSonValidos()
    {
        var casoA = new CasoLimiteRankingDetectado(
            TipoCasoLimite.EmpatePorCupo,
            puntajeAfectado: 88.50m,
            cuposDisponibles: 1,
            presupuestoDisponible: 2500.00m,
            postulacionesAfectadas: [_postulacion1Id, _postulacion2Id]);

        var casoB = new CasoLimiteRankingDetectado(
            TipoCasoLimite.EmpatePorCupo,
            puntajeAfectado: 88.50m,
            cuposDisponibles: 1,
            presupuestoDisponible: 2500.00m,
            postulacionesAfectadas: [_postulacion1Id, _postulacion2Id]);

        var casoDistinto = new CasoLimiteRankingDetectado(
            TipoCasoLimite.EmpatePorPresupuesto,
            puntajeAfectado: 88.50m,
            cuposDisponibles: 2,
            presupuestoDisponible: 2500.00m,
            postulacionesAfectadas: [_postulacion1Id, _postulacion2Id]);

        Assert.Equal(TipoCasoLimite.EmpatePorCupo, casoA.Tipo);
        Assert.Equal(88.50m, casoA.PuntajeAfectado);
        Assert.Equal(1, casoA.CuposDisponibles);
        Assert.Equal(2500.00m, casoA.PresupuestoDisponible);
        Assert.Equal(new[] { _postulacion1Id, _postulacion2Id }, casoA.PostulacionesAfectadas);

        Assert.Equal(casoA, casoB);
        Assert.Equal(casoA.GetHashCode(), casoB.GetHashCode());
        Assert.NotEqual(casoA, casoDistinto);
    }

    [Fact]
    public void Debe_CrearCasoLimitePresupuestarioConPuntajeNuloYUnaPostulacionElegible()
    {
        var caso = new CasoLimiteRankingDetectado(
            TipoCasoLimite.CasoLimitePresupuestario,
            puntajeAfectado: null,
            cuposDisponibles: 3,
            presupuestoDisponible: 500.00m,
            postulacionesAfectadas: [_postulacion1Id]);

        Assert.Equal(TipoCasoLimite.CasoLimitePresupuestario, caso.Tipo);
        Assert.Null(caso.PuntajeAfectado);
        Assert.Equal(3, caso.CuposDisponibles);
        Assert.Equal(500.00m, caso.PresupuestoDisponible);
        Assert.Equal(new[] { _postulacion1Id }, caso.PostulacionesAfectadas);
    }

    [Fact]
    public void Debe_RechazarInvariantesInvalidas_Cuando_SeConstruyeCasoLimiteRankingDetectado()
    {
        Assert.Throws<ArgumentNullException>(() => new CasoLimiteRankingDetectado(
            TipoCasoLimite.EmpatePorCupo, 88.00m, 1, 1000.00m, null!));

        Assert.Throws<CasoLimiteInvalidoException>(() => new CasoLimiteRankingDetectado(
            (TipoCasoLimite)99, 88.00m, 1, 1000.00m, [_postulacion1Id, _postulacion2Id]));

        // Empate sin puntaje o fuera de rango/escala
        Assert.Throws<CasoLimiteInvalidoException>(() => new CasoLimiteRankingDetectado(
            TipoCasoLimite.EmpatePorCupo, null, 1, 1000.00m, [_postulacion1Id, _postulacion2Id]));
        Assert.Throws<CasoLimiteInvalidoException>(() => new CasoLimiteRankingDetectado(
            TipoCasoLimite.EmpatePorPresupuesto, 100.01m, 2, 1000.00m, [_postulacion1Id, _postulacion2Id]));
        Assert.Throws<CasoLimiteInvalidoException>(() => new CasoLimiteRankingDetectado(
            TipoCasoLimite.EmpatePorCupo, 88.123m, 1, 1000.00m, [_postulacion1Id, _postulacion2Id]));

        // CasoLimitePresupuestario con puntaje no nulo
        Assert.Throws<CasoLimiteInvalidoException>(() => new CasoLimiteRankingDetectado(
            TipoCasoLimite.CasoLimitePresupuestario, 80.00m, 1, 500.00m, [_postulacion1Id]));

        // Cupos y presupuesto inválidos
        Assert.Throws<CasoLimiteInvalidoException>(() => new CasoLimiteRankingDetectado(
            TipoCasoLimite.EmpatePorCupo, 88.00m, 0, 1000.00m, [_postulacion1Id, _postulacion2Id]));
        Assert.Throws<CasoLimiteInvalidoException>(() => new CasoLimiteRankingDetectado(
            TipoCasoLimite.EmpatePorCupo, 88.00m, 1, 0.00m, [_postulacion1Id, _postulacion2Id]));
        Assert.Throws<CasoLimiteInvalidoException>(() => new CasoLimiteRankingDetectado(
            TipoCasoLimite.EmpatePorCupo, 88.00m, 1, 1000.123m, [_postulacion1Id, _postulacion2Id]));

        // Postulaciones inválidas (cantidad, Guid.Empty, duplicados)
        Assert.Throws<CasoLimiteInvalidoException>(() => new CasoLimiteRankingDetectado(
            TipoCasoLimite.EmpatePorCupo, 88.00m, 1, 1000.00m, [_postulacion1Id]));
        Assert.Throws<CasoLimiteInvalidoException>(() => new CasoLimiteRankingDetectado(
            TipoCasoLimite.CasoLimitePresupuestario, null, 1, 500.00m, []));
        Assert.Throws<CasoLimiteInvalidoException>(() => new CasoLimiteRankingDetectado(
            TipoCasoLimite.EmpatePorCupo, 88.00m, 1, 1000.00m, [_postulacion1Id, Guid.Empty]));
        Assert.Throws<CasoLimiteInvalidoException>(() => new CasoLimiteRankingDetectado(
            TipoCasoLimite.EmpatePorCupo, 88.00m, 1, 1000.00m, [_postulacion1Id, _postulacion1Id]));
    }

    [Fact]
    public void Debe_PreservarRetrocompatibilidadYValidarCoherenciaDeDecision_EnElementoRankingEvaluado()
    {
        // Constructor previo sin parámetros opcionales sigue funcionando
        var elementoBasico = new ElementoRankingEvaluado(
            _resultado1Id,
            _postulacion1Id,
            1,
            90.00m,
            1000.00m,
            ClasificacionRanking.DentroDeCupo,
            empatadoEnLineaDeCorte: false,
            alertaDiscrepancia: false,
            discrepanciaRevisada: false,
            afectaAdvertenciaDiscrepanciaEnCorte: false,
            excepcionEvaluadorUnico: false,
            fechaConsolidacion: _fechaBase);

        Assert.Null(elementoBasico.TipoCasoLimite);
        Assert.Null(elementoBasico.SeleccionadoEnCasoLimite);
        Assert.Null(elementoBasico.JustificacionDesempateCasoLimite);
        Assert.Null(elementoBasico.DecididoPorUsuarioId);
        Assert.Null(elementoBasico.FechaDecisionCasoLimite);
        Assert.False(elementoBasico.CasoLimiteResuelto);

        // Con decisión completa válida
        var elementoConDecision = new ElementoRankingEvaluado(
            _resultado1Id,
            _postulacion1Id,
            1,
            90.00m,
            1000.00m,
            ClasificacionRanking.DentroDeCupo,
            empatadoEnLineaDeCorte: false,
            alertaDiscrepancia: false,
            discrepanciaRevisada: false,
            afectaAdvertenciaDiscrepanciaEnCorte: false,
            excepcionEvaluadorUnico: false,
            fechaConsolidacion: _fechaBase,
            tipoCasoLimite: TipoCasoLimite.EmpatePorCupo,
            seleccionadoEnCasoLimite: true,
            justificacionDesempateCasoLimite: "  Mayor vulnerabilidad socioeconómica acreditada.  ",
            decididoPorUsuarioId: _decisorId,
            fechaDecisionCasoLimite: _fechaBase.AddHours(1),
            casoLimiteResuelto: true);

        Assert.Equal(TipoCasoLimite.EmpatePorCupo, elementoConDecision.TipoCasoLimite);
        Assert.True(elementoConDecision.SeleccionadoEnCasoLimite);
        Assert.Equal("Mayor vulnerabilidad socioeconómica acreditada.", elementoConDecision.JustificacionDesempateCasoLimite);
        Assert.Equal(_decisorId, elementoConDecision.DecididoPorUsuarioId);
        Assert.Equal(_fechaBase.AddHours(1).ToUniversalTime(), elementoConDecision.FechaDecisionCasoLimite);
        Assert.Equal(TimeSpan.Zero, elementoConDecision.FechaDecisionCasoLimite!.Value.Offset);
        Assert.True(elementoConDecision.CasoLimiteResuelto);

        // Incoherencia parcial (solo algunos campos poblados)
        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id,
            _postulacion1Id,
            1,
            90.00m,
            1000.00m,
            ClasificacionRanking.DentroDeCupo,
            false,
            false,
            false,
            false,
            false,
            _fechaBase,
            tipoCasoLimite: TipoCasoLimite.EmpatePorCupo));

        // casoLimiteResuelto = true sin tipoCasoLimite
        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id,
            _postulacion1Id,
            1,
            90.00m,
            1000.00m,
            ClasificacionRanking.DentroDeCupo,
            false,
            false,
            false,
            false,
            false,
            _fechaBase,
            casoLimiteResuelto: true));

        // TipoCasoLimite fuera de catálogo
        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id,
            _postulacion1Id,
            1,
            90.00m,
            1000.00m,
            ClasificacionRanking.DentroDeCupo,
            false,
            false,
            false,
            false,
            false,
            _fechaBase,
            tipoCasoLimite: (TipoCasoLimite)99,
            seleccionadoEnCasoLimite: true,
            justificacionDesempateCasoLimite: "Justificación válida suficientemente extensa.",
            decididoPorUsuarioId: _decisorId,
            fechaDecisionCasoLimite: _fechaBase));

        // DecididoPorUsuarioId vacío
        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id,
            _postulacion1Id,
            1,
            90.00m,
            1000.00m,
            ClasificacionRanking.DentroDeCupo,
            false,
            false,
            false,
            false,
            false,
            _fechaBase,
            tipoCasoLimite: TipoCasoLimite.EmpatePorCupo,
            seleccionadoEnCasoLimite: true,
            justificacionDesempateCasoLimite: "Justificación válida suficientemente extensa.",
            decididoPorUsuarioId: Guid.Empty,
            fechaDecisionCasoLimite: _fechaBase));

        // Justificación menor a 10 caracteres tras Trim
        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id,
            _postulacion1Id,
            1,
            90.00m,
            1000.00m,
            ClasificacionRanking.DentroDeCupo,
            false,
            false,
            false,
            false,
            false,
            _fechaBase,
            tipoCasoLimite: TipoCasoLimite.EmpatePorCupo,
            seleccionadoEnCasoLimite: true,
            justificacionDesempateCasoLimite: "   corta  ",
            decididoPorUsuarioId: _decisorId,
            fechaDecisionCasoLimite: _fechaBase));
    }

    [Fact]
    public void Debe_ExponerPropiedadesYIgualdadEstructuralDeCasoLimite_EnCorteRankingConvocatoria()
    {
        var disponibilidad = new DisponibilidadProgramaRanking(
            _convocatoriaId,
            _programaBecaId,
            cuposAsignadosConvocatoria: 1,
            cuposDisponiblesPrograma: 1,
            presupuestoDisponibleBruto: 3000.00m,
            presupuestoComprometidoRenovaciones: 0.00m,
            moneda: "USD");

        var elem1 = new ElementoRankingEvaluado(
            _resultado1Id,
            _postulacion1Id,
            1,
            88.00m,
            1000.00m,
            ClasificacionRanking.DentroDeCupo,
            empatadoEnLineaDeCorte: true,
            alertaDiscrepancia: false,
            discrepanciaRevisada: false,
            afectaAdvertenciaDiscrepanciaEnCorte: false,
            excepcionEvaluadorUnico: false,
            fechaConsolidacion: _fechaBase);

        var elem2 = new ElementoRankingEvaluado(
            _resultado2Id,
            _postulacion2Id,
            2,
            88.00m,
            1000.00m,
            ClasificacionRanking.ListaDeEspera,
            empatadoEnLineaDeCorte: true,
            alertaDiscrepancia: false,
            discrepanciaRevisada: false,
            afectaAdvertenciaDiscrepanciaEnCorte: false,
            excepcionEvaluadorUnico: false,
            fechaConsolidacion: _fechaBase.AddMinutes(1));

        var casoActivo = new CasoLimiteRankingDetectado(
            TipoCasoLimite.EmpatePorCupo,
            puntajeAfectado: 88.00m,
            cuposDisponibles: 1,
            presupuestoDisponible: 3000.00m,
            postulacionesAfectadas: [_postulacion1Id, _postulacion2Id]);

        var corteA = new CorteRankingConvocatoria(
            _convocatoriaId,
            disponibilidad,
            [elem1, elem2],
            casoLimiteActivo: casoActivo,
            tieneDecisionCasoLimiteInvalidaPorDisponibilidad: false);

        var corteB = new CorteRankingConvocatoria(
            _convocatoriaId,
            disponibilidad,
            [elem1, elem2],
            casoLimiteActivo: casoActivo,
            tieneDecisionCasoLimiteInvalidaPorDisponibilidad: false);

        var corteConDecisionInvalida = new CorteRankingConvocatoria(
            _convocatoriaId,
            disponibilidad,
            [elem1, elem2],
            casoLimiteActivo: casoActivo,
            tieneDecisionCasoLimiteInvalidaPorDisponibilidad: true);

        Assert.True(corteA.TieneCasoLimiteActivo);
        Assert.Equal(casoActivo, corteA.CasoLimiteActivo);
        Assert.False(corteA.TieneCasoLimiteResuelto);
        Assert.False(corteA.TieneDecisionCasoLimiteInvalidaPorDisponibilidad);
        Assert.Equal(corteA, corteB);
        Assert.Equal(corteA.GetHashCode(), corteB.GetHashCode());
        Assert.NotEqual(corteA, corteConDecisionInvalida);

        // Corte con elemento resuelto refleja TieneCasoLimiteResuelto = true
        var elemResuelto = new ElementoRankingEvaluado(
            _resultado1Id,
            _postulacion1Id,
            1,
            88.00m,
            1000.00m,
            ClasificacionRanking.DentroDeCupo,
            empatadoEnLineaDeCorte: false,
            alertaDiscrepancia: false,
            discrepanciaRevisada: false,
            afectaAdvertenciaDiscrepanciaEnCorte: false,
            excepcionEvaluadorUnico: false,
            fechaConsolidacion: _fechaBase,
            tipoCasoLimite: TipoCasoLimite.EmpatePorCupo,
            seleccionadoEnCasoLimite: true,
            justificacionDesempateCasoLimite: "Justificación suficiente para aprobar desempate.",
            decididoPorUsuarioId: _decisorId,
            fechaDecisionCasoLimite: _fechaBase.AddHours(1),
            casoLimiteResuelto: true);

        var corteResuelto = new CorteRankingConvocatoria(_convocatoriaId, disponibilidad, [elemResuelto]);
        Assert.False(corteResuelto.TieneCasoLimiteActivo);
        Assert.Null(corteResuelto.CasoLimiteActivo);
        Assert.True(corteResuelto.TieneCasoLimiteResuelto);
        Assert.False(corteResuelto.TieneDecisionCasoLimiteInvalidaPorDisponibilidad);
    }
}
