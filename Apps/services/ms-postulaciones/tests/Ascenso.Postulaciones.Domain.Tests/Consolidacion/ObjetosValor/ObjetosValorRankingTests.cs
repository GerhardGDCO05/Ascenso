using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Consolidacion.ObjetosValor;

public sealed class ObjetosValorRankingTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000003001");
    private static readonly Guid _otraConvocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000003002");
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000003003");
    private static readonly Guid _resultado1Id = Guid.Parse("01968000-0000-7000-8000-000000003004");
    private static readonly Guid _resultado2Id = Guid.Parse("01968000-0000-7000-8000-000000003005");
    private static readonly Guid _postulacion1Id = Guid.Parse("01968000-0000-7000-8000-000000003006");
    private static readonly Guid _postulacion2Id = Guid.Parse("01968000-0000-7000-8000-000000003007");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 26, 10, 0, 0, TimeSpan.FromHours(-4));

    [Fact]
    public void Debe_CalcularCuposYPresupuestoEfectivoYNormalizarMoneda_Cuando_DisponibilidadEsValida()
    {
        var disponibilidad = new DisponibilidadProgramaRanking(
            _convocatoriaId,
            _programaBecaId,
            cuposAsignadosConvocatoria: 5,
            cuposDisponiblesPrograma: 3,
            presupuestoDisponibleBruto: 10000.50m,
            presupuestoComprometidoRenovaciones: 2500.25m,
            moneda: "  usd  ");

        Assert.IsAssignableFrom<ValueObject>(disponibilidad);
        Assert.Equal(_convocatoriaId, disponibilidad.ConvocatoriaId);
        Assert.Equal(_programaBecaId, disponibilidad.ProgramaBecaId);
        Assert.Equal(5, disponibilidad.CuposAsignadosConvocatoria);
        Assert.Equal(3, disponibilidad.CuposDisponiblesPrograma);
        Assert.Equal(3, disponibilidad.CuposDisponiblesEfectivos);
        Assert.Equal(10000.50m, disponibilidad.PresupuestoDisponibleBruto);
        Assert.Equal(2500.25m, disponibilidad.PresupuestoComprometidoRenovaciones);
        Assert.Equal(7500.25m, disponibilidad.PresupuestoDisponibleEfectivo);
        Assert.Equal("USD", disponibilidad.Moneda);
    }

    [Fact]
    public void Debe_AplicarPisoCeroAlPresupuestoEfectivoYMinimoEnCupos_Cuando_RenovacionesExcedenBruto()
    {
        var disponibilidad = new DisponibilidadProgramaRanking(
            _convocatoriaId,
            _programaBecaId,
            cuposAsignadosConvocatoria: 2,
            cuposDisponiblesPrograma: 10,
            presupuestoDisponibleBruto: 1500.00m,
            presupuestoComprometidoRenovaciones: 2200.00m,
            moneda: "bob");

        Assert.Equal(2, disponibilidad.CuposDisponiblesEfectivos);
        Assert.Equal(0.00m, disponibilidad.PresupuestoDisponibleEfectivo);
        Assert.Equal("BOB", disponibilidad.Moneda);
    }

    [Fact]
    public void Debe_PermitirDisponibilidadCero_Cuando_NoHayCuposNiPresupuesto()
    {
        var disponibilidad = new DisponibilidadProgramaRanking(
            _convocatoriaId,
            _programaBecaId,
            cuposAsignadosConvocatoria: 0,
            cuposDisponiblesPrograma: 0,
            presupuestoDisponibleBruto: 0.00m,
            presupuestoComprometidoRenovaciones: 0.00m,
            moneda: "USD");

        Assert.Equal(0, disponibilidad.CuposDisponiblesEfectivos);
        Assert.Equal(0.00m, disponibilidad.PresupuestoDisponibleEfectivo);
    }

    [Theory]
    [InlineData(-1, 2, 1000, 0, "USD")]
    [InlineData(2, -1, 1000, 0, "USD")]
    [InlineData(2, 2, -0.01, 0, "USD")]
    [InlineData(2, 2, 1000, -0.01, "USD")]
    [InlineData(2, 2, 1000.123, 0, "USD")]
    [InlineData(2, 2, 1000, 10.999, "USD")]
    [InlineData(2, 2, 1000, 0, "")]
    [InlineData(2, 2, 1000, 0, "   ")]
    [InlineData(2, 2, 1000, 0, "MONEDALARGA")]
    public void Debe_RechazarDisponibilidadInvalida_Cuando_IncumpleInvariantes(
        int cuposAsignados,
        int cuposPrograma,
        decimal presupuestoBruto,
        decimal comprometidoRenovaciones,
        string moneda)
    {
        var excepcion = Assert.Throws<RankingInvalidoException>(
            () => new DisponibilidadProgramaRanking(
                _convocatoriaId,
                _programaBecaId,
                cuposAsignados,
                cuposPrograma,
                presupuestoBruto,
                comprometidoRenovaciones,
                moneda));

        Assert.Equal(RankingInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarDisponibilidad_Cuando_IdentificadoresSonVacios()
    {
        Assert.Throws<RankingInvalidoException>(
            () => new DisponibilidadProgramaRanking(
                Guid.Empty,
                _programaBecaId,
                2,
                2,
                1000.00m,
                0.00m,
                "USD"));

        Assert.Throws<RankingInvalidoException>(
            () => new DisponibilidadProgramaRanking(
                _convocatoriaId,
                Guid.Empty,
                2,
                2,
                1000.00m,
                0.00m,
                "USD"));
    }

    [Fact]
    public void Debe_CrearElementoRankingEvaluadoConFechaUtc_Cuando_LosDatosSonValidos()
    {
        var elemento = new ElementoRankingEvaluado(
            _resultado1Id,
            _postulacion1Id,
            posicionRanking: 1,
            puntajeConsolidado: 92.50m,
            montoEstimadoBeca: 1500.00m,
            clasificacion: ClasificacionRanking.DentroDeCupo,
            empatadoEnLineaDeCorte: false,
            alertaDiscrepancia: true,
            discrepanciaRevisada: false,
            afectaAdvertenciaDiscrepanciaEnCorte: true,
            excepcionEvaluadorUnico: false,
            fechaConsolidacion: _fechaBase);

        Assert.IsAssignableFrom<ValueObject>(elemento);
        Assert.Equal(_resultado1Id, elemento.ResultadoConsolidadoId);
        Assert.Equal(_postulacion1Id, elemento.PostulacionId);
        Assert.Equal(1, elemento.PosicionRanking);
        Assert.Equal(92.50m, elemento.PuntajeConsolidado);
        Assert.Equal(1500.00m, elemento.MontoEstimadoBeca);
        Assert.Equal(ClasificacionRanking.DentroDeCupo, elemento.Clasificacion);
        Assert.False(elemento.EmpatadoEnLineaDeCorte);
        Assert.True(elemento.AlertaDiscrepancia);
        Assert.False(elemento.DiscrepanciaRevisada);
        Assert.True(elemento.AfectaAdvertenciaDiscrepanciaEnCorte);
        Assert.False(elemento.ExcepcionEvaluadorUnico);
        Assert.Equal(_fechaBase.ToUniversalTime(), elemento.FechaConsolidacion);
        Assert.Equal(TimeSpan.Zero, elemento.FechaConsolidacion.Offset);
    }

    [Fact]
    public void Debe_RechazarElementoRankingEvaluado_Cuando_IncumpleInvariantes()
    {
        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            Guid.Empty, _postulacion1Id, 1, 90.00m, 1000.00m, ClasificacionRanking.DentroDeCupo,
            false, false, false, false, false, _fechaBase));

        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id, Guid.Empty, 1, 90.00m, 1000.00m, ClasificacionRanking.DentroDeCupo,
            false, false, false, false, false, _fechaBase));

        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id, _postulacion1Id, 0, 90.00m, 1000.00m, ClasificacionRanking.DentroDeCupo,
            false, false, false, false, false, _fechaBase));

        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id, _postulacion1Id, 1, 100.01m, 1000.00m, ClasificacionRanking.DentroDeCupo,
            false, false, false, false, false, _fechaBase));

        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id, _postulacion1Id, 1, 90.123m, 1000.00m, ClasificacionRanking.DentroDeCupo,
            false, false, false, false, false, _fechaBase));

        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id, _postulacion1Id, 1, 90.00m, 0.00m, ClasificacionRanking.DentroDeCupo,
            false, false, false, false, false, _fechaBase));

        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id, _postulacion1Id, 1, 90.00m, 1000.123m, ClasificacionRanking.DentroDeCupo,
            false, false, false, false, false, _fechaBase));

        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id, _postulacion1Id, 1, 90.00m, 1000.00m, (ClasificacionRanking)99,
            false, false, false, false, false, _fechaBase));

        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id, _postulacion1Id, 1, 90.00m, 1000.00m, ClasificacionRanking.DentroDeCupo,
            false, alertaDiscrepancia: false, discrepanciaRevisada: true, false, false, _fechaBase));

        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id, _postulacion1Id, 1, 90.00m, 1000.00m, ClasificacionRanking.DentroDeCupo,
            false, alertaDiscrepancia: true, discrepanciaRevisada: true, afectaAdvertenciaDiscrepanciaEnCorte: true, false, _fechaBase));

        Assert.Throws<RankingInvalidoException>(() => new ElementoRankingEvaluado(
            _resultado1Id, _postulacion1Id, 1, 90.00m, 1000.00m, ClasificacionRanking.DentroDeCupo,
            false, false, false, false, false, default));
    }

    [Fact]
    public void Debe_ConstruirCorteRankingConvocatoriaConTotalesEIndicadoresCoherentes_Cuando_LosDatosSonValidos()
    {
        var disponibilidad = new DisponibilidadProgramaRanking(
            _convocatoriaId,
            _programaBecaId,
            cuposAsignadosConvocatoria: 2,
            cuposDisponiblesPrograma: 2,
            presupuestoDisponibleBruto: 5000.00m,
            presupuestoComprometidoRenovaciones: 1000.00m,
            moneda: "USD");

        var elem1 = new ElementoRankingEvaluado(
            _resultado1Id,
            _postulacion1Id,
            1,
            90.00m,
            1500.00m,
            ClasificacionRanking.DentroDeCupo,
            empatadoEnLineaDeCorte: true,
            alertaDiscrepancia: true,
            discrepanciaRevisada: false,
            afectaAdvertenciaDiscrepanciaEnCorte: true,
            excepcionEvaluadorUnico: false,
            _fechaBase);

        var elem2 = new ElementoRankingEvaluado(
            _resultado2Id,
            _postulacion2Id,
            2,
            90.00m,
            1200.00m,
            ClasificacionRanking.ListaDeEspera,
            empatadoEnLineaDeCorte: true,
            alertaDiscrepancia: false,
            discrepanciaRevisada: false,
            afectaAdvertenciaDiscrepanciaEnCorte: false,
            excepcionEvaluadorUnico: false,
            _fechaBase.AddMinutes(5));

        var corte = new CorteRankingConvocatoria(_convocatoriaId, disponibilidad, [elem1, elem2]);

        Assert.IsAssignableFrom<ValueObject>(corte);
        Assert.Equal(_convocatoriaId, corte.ConvocatoriaId);
        Assert.Equal(disponibilidad, corte.Disponibilidad);
        Assert.Equal(2, corte.TotalPostulantes);
        Assert.Equal(1, corte.TotalDentroDeCupo);
        Assert.Equal(1, corte.TotalListaDeEspera);
        Assert.Equal(1500.00m, corte.MontoTotalDentroDeCupo);
        Assert.True(corte.TieneEmpateEnLineaDeCorte);
        Assert.Equal(90.00m, corte.PuntajeEmpateEnLineaDeCorte);
        Assert.Equal(new[] { _postulacion1Id, _postulacion2Id }, corte.PostulacionesEmpatadasEnCorte);
        Assert.True(corte.TieneAdvertenciaDiscrepanciasEnCorte);
        Assert.Equal(1, corte.CantidadDiscrepanciasSinRevisarEnCorte);
        Assert.Equal(new[] { _postulacion1Id }, corte.PostulacionesConDiscrepanciaSinRevisarEnCorte);
        Assert.Equal(new[] { elem1, elem2 }, corte.Elementos);

        var corteEquivalente = new CorteRankingConvocatoria(_convocatoriaId, disponibilidad, [elem1, elem2]);
        Assert.Equal(corte, corteEquivalente);
        Assert.Equal(corte.GetHashCode(), corteEquivalente.GetHashCode());
    }

    [Fact]
    public void Debe_RechazarCorteRankingConvocatoria_Cuando_HayConvocatoriaDiscordanteOPosicionesInvalidas()
    {
        var disponibilidadOtra = new DisponibilidadProgramaRanking(
            _otraConvocatoriaId,
            _programaBecaId,
            2,
            2,
            5000.00m,
            0.00m,
            "USD");

        Assert.Throws<RankingInvalidoException>(
            () => new CorteRankingConvocatoria(_convocatoriaId, disponibilidadOtra, []));

        var disponibilidad = new DisponibilidadProgramaRanking(
            _convocatoriaId,
            _programaBecaId,
            2,
            2,
            5000.00m,
            0.00m,
            "USD");

        Assert.Throws<RankingInvalidoException>(
            () => new CorteRankingConvocatoria(Guid.Empty, disponibilidad, []));

        Assert.Throws<ArgumentNullException>(
            () => new CorteRankingConvocatoria(_convocatoriaId, null!, []));

        Assert.Throws<ArgumentNullException>(
            () => new CorteRankingConvocatoria(_convocatoriaId, disponibilidad, null!));

        var elemPosicionIncorrecta = new ElementoRankingEvaluado(
            _resultado1Id,
            _postulacion1Id,
            2,
            90.00m,
            1000.00m,
            ClasificacionRanking.DentroDeCupo,
            false,
            false,
            false,
            false,
            false,
            _fechaBase);

        Assert.Throws<RankingInvalidoException>(
            () => new CorteRankingConvocatoria(_convocatoriaId, disponibilidad, [elemPosicionIncorrecta]));

        var elem1 = new ElementoRankingEvaluado(
            _resultado1Id, _postulacion1Id, 1, 90.00m, 1000.00m, ClasificacionRanking.DentroDeCupo,
            false, false, false, false, false, _fechaBase);
        var elemDuplicado = new ElementoRankingEvaluado(
            _resultado2Id, _postulacion1Id, 2, 85.00m, 1000.00m, ClasificacionRanking.DentroDeCupo,
            false, false, false, false, false, _fechaBase);

        Assert.Throws<RankingInvalidoException>(
            () => new CorteRankingConvocatoria(_convocatoriaId, disponibilidad, [elem1, elemDuplicado]));
    }
}
