using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Consolidacion.Servicios;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Consolidacion.Servicios;

public sealed class ServicioCalculoRankingCorteTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000005001");
    private static readonly Guid _otraConvocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000005002");
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000005003");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000005004");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_OrdenarDescendentementePorPuntajeConDesempateTecnicoDeterministaYCortarPorCupoEfectivo()
    {
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005101"), 82.00m, _fechaBase.AddMinutes(10));
        var r2 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005102"), 95.00m, _fechaBase.AddMinutes(30));
        var r3 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005103"), 75.00m, _fechaBase.AddMinutes(5));
        var r4 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005104"), 88.50m, _fechaBase.AddMinutes(20));

        var disponibilidad = CrearDisponibilidad(
            cuposAsignados: 3,
            cuposPrograma: 2,
            presupuestoBruto: 10000.00m,
            comprometidoRenovaciones: 2000.00m);

        var montos = new Dictionary<Guid, decimal>
        {
            [r1.PostulacionId] = 1000.00m,
            [r2.PostulacionId] = 1000.00m,
            [r3.PostulacionId] = 1000.00m,
            [r4.PostulacionId] = 1000.00m
        };

        var corte = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2, r3, r4], montos, disponibilidad);

        Assert.Equal(4, corte.TotalPostulantes);
        Assert.Equal(2, corte.TotalDentroDeCupo);
        Assert.Equal(2, corte.TotalListaDeEspera);
        Assert.Equal(2000.00m, corte.MontoTotalDentroDeCupo);
        Assert.False(corte.TieneEmpateEnLineaDeCorte);
        Assert.Null(corte.PuntajeEmpateEnLineaDeCorte);
        Assert.Empty(corte.PostulacionesEmpatadasEnCorte);

        Assert.Equal(r2.PostulacionId, corte.Elementos[0].PostulacionId);
        Assert.Equal(1, corte.Elementos[0].PosicionRanking);
        Assert.Equal(ClasificacionRanking.DentroDeCupo, corte.Elementos[0].Clasificacion);

        Assert.Equal(r4.PostulacionId, corte.Elementos[1].PostulacionId);
        Assert.Equal(2, corte.Elementos[1].PosicionRanking);
        Assert.Equal(ClasificacionRanking.DentroDeCupo, corte.Elementos[1].Clasificacion);

        Assert.Equal(r1.PostulacionId, corte.Elementos[2].PostulacionId);
        Assert.Equal(3, corte.Elementos[2].PosicionRanking);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, corte.Elementos[2].Clasificacion);

        Assert.Equal(r3.PostulacionId, corte.Elementos[3].PostulacionId);
        Assert.Equal(4, corte.Elementos[3].PosicionRanking);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, corte.Elementos[3].Clasificacion);
    }

    [Fact]
    public void Debe_DesempatarOrdenDePresentacionPorFechaConsolidacionYId_Cuando_PuntajesSonIgualesDentroDeCupo()
    {
        var rTardio = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005111"), 92.00m, _fechaBase.AddMinutes(20));
        var rTempranoA = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005112"), 92.00m, _fechaBase.AddMinutes(10));
        var rTempranoB = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005113"), 92.00m, _fechaBase.AddMinutes(10));

        var disponibilidad = CrearDisponibilidad(cuposAsignados: 5, cuposPrograma: 5, presupuestoBruto: 10000.00m);
        var montos = new Dictionary<Guid, decimal>
        {
            [rTardio.PostulacionId] = 1000.00m,
            [rTempranoA.PostulacionId] = 1000.00m,
            [rTempranoB.PostulacionId] = 1000.00m
        };

        var corte = ServicioCalculoRankingCorte.Calcular(
            _convocatoriaId,
            [rTardio, rTempranoB, rTempranoA],
            montos,
            disponibilidad);

        var ordenMismaFecha = new[] { rTempranoA, rTempranoB }.OrderBy(r => r.Id).Select(r => r.PostulacionId).ToArray();

        Assert.False(corte.TieneEmpateEnLineaDeCorte);
        Assert.Equal(3, corte.TotalDentroDeCupo);
        Assert.Equal(ordenMismaFecha[0], corte.Elementos[0].PostulacionId);
        Assert.Equal(ordenMismaFecha[1], corte.Elementos[1].PostulacionId);
        Assert.Equal(rTardio.PostulacionId, corte.Elementos[2].PostulacionId);
    }

    [Fact]
    public void Debe_CortarPorPresupuestoDescontandoRenovacionesSinSaltarOrdenDeMerito_Cuando_MontosSonHeterogeneos()
    {
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005121"), 94.00m, _fechaBase);
        var r2 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005122"), 89.00m, _fechaBase.AddMinutes(1));
        var r3 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005123"), 80.00m, _fechaBase.AddMinutes(2));

        // Bruto 5500 - Renovaciones 3000 = 2500 efectivo.
        // r1 ($2000) cabe -> quedan $500.
        // r2 ($2000) no cabe -> corte estricto; r3 ($400) NO se salta aunque quepa en $500.
        var disponibilidad = CrearDisponibilidad(
            cuposAsignados: 5,
            cuposPrograma: 5,
            presupuestoBruto: 5500.00m,
            comprometidoRenovaciones: 3000.00m);

        var montos = new Dictionary<Guid, decimal>
        {
            [r1.PostulacionId] = 2000.00m,
            [r2.PostulacionId] = 2000.00m,
            [r3.PostulacionId] = 400.00m
        };

        var corte = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2, r3], montos, disponibilidad);

        Assert.Equal(1, corte.TotalDentroDeCupo);
        Assert.Equal(2, corte.TotalListaDeEspera);
        Assert.Equal(2000.00m, corte.MontoTotalDentroDeCupo);
        Assert.False(corte.TieneEmpateEnLineaDeCorte);
        Assert.Equal(ClasificacionRanking.DentroDeCupo, corte.Elementos[0].Clasificacion);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, corte.Elementos[1].Clasificacion);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, corte.Elementos[2].Clasificacion);
    }

    [Theory]
    [InlineData(0, 5, 10000.00, 0.00)]
    [InlineData(5, 0, 10000.00, 0.00)]
    [InlineData(5, 5, 0.00, 0.00)]
    [InlineData(5, 5, 1000.00, 1500.00)]
    public void Debe_ClasificarTodosEnListaDeEsperaSinEmpate_Cuando_DisponibilidadEfectivaEsCero(
        int cuposAsignados,
        int cuposPrograma,
        decimal presupuestoBruto,
        decimal comprometidoRenovaciones)
    {
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005131"), 95.00m, _fechaBase);
        var r2 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005132"), 95.00m, _fechaBase.AddMinutes(1));

        var disponibilidad = CrearDisponibilidad(
            cuposAsignados,
            cuposPrograma,
            presupuestoBruto,
            comprometidoRenovaciones);

        var montos = new Dictionary<Guid, decimal>
        {
            [r1.PostulacionId] = 1000.00m,
            [r2.PostulacionId] = 1000.00m
        };

        var corte = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2], montos, disponibilidad);

        Assert.Equal(0, corte.TotalDentroDeCupo);
        Assert.Equal(2, corte.TotalListaDeEspera);
        Assert.Equal(0.00m, corte.MontoTotalDentroDeCupo);
        Assert.False(corte.TieneEmpateEnLineaDeCorte);
        Assert.All(corte.Elementos, e => Assert.Equal(ClasificacionRanking.ListaDeEspera, e.Clasificacion));
    }

    [Fact]
    public void Debe_DetectarEmpatePorCupoEnLaFronteraYMarcarTodosLosEmpatados()
    {
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005141"), 95.00m, _fechaBase);
        var r2 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005142"), 88.00m, _fechaBase.AddMinutes(1));
        var r3 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005143"), 88.00m, _fechaBase.AddMinutes(2));
        var r4 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005144"), 80.00m, _fechaBase.AddMinutes(3));

        var disponibilidad = CrearDisponibilidad(cuposAsignados: 2, cuposPrograma: 2, presupuestoBruto: 10000.00m);
        var montos = new Dictionary<Guid, decimal>
        {
            [r1.PostulacionId] = 1000.00m,
            [r2.PostulacionId] = 1000.00m,
            [r3.PostulacionId] = 1000.00m,
            [r4.PostulacionId] = 1000.00m
        };

        var corte = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2, r3, r4], montos, disponibilidad);

        Assert.True(corte.TieneEmpateEnLineaDeCorte);
        Assert.Equal(88.00m, corte.PuntajeEmpateEnLineaDeCorte);
        Assert.Equal(new[] { r2.PostulacionId, r3.PostulacionId }, corte.PostulacionesEmpatadasEnCorte);
        Assert.False(corte.Elementos[0].EmpatadoEnLineaDeCorte);
        Assert.Equal(ClasificacionRanking.DentroDeCupo, corte.Elementos[0].Clasificacion);
        Assert.True(corte.Elementos[1].EmpatadoEnLineaDeCorte);
        Assert.Equal(ClasificacionRanking.DentroDeCupo, corte.Elementos[1].Clasificacion);
        Assert.True(corte.Elementos[2].EmpatadoEnLineaDeCorte);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, corte.Elementos[2].Clasificacion);
        Assert.False(corte.Elementos[3].EmpatadoEnLineaDeCorte);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, corte.Elementos[3].Clasificacion);
    }

    [Fact]
    public void Debe_DetectarEmpatePorPresupuestoEnLaFronteraYMarcarTodosLosEmpatados()
    {
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005151"), 90.00m, _fechaBase);
        var r2 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005152"), 90.00m, _fechaBase.AddMinutes(1));

        // 5 cupos pero presupuesto efectivo $3000; cada beca cuesta $2000 (1 cabe, 2 no caben).
        var disponibilidad = CrearDisponibilidad(cuposAsignados: 5, cuposPrograma: 5, presupuestoBruto: 3000.00m);
        var montos = new Dictionary<Guid, decimal>
        {
            [r1.PostulacionId] = 2000.00m,
            [r2.PostulacionId] = 2000.00m
        };

        var corte = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2], montos, disponibilidad);

        Assert.True(corte.TieneEmpateEnLineaDeCorte);
        Assert.Equal(90.00m, corte.PuntajeEmpateEnLineaDeCorte);
        Assert.Equal(new[] { r1.PostulacionId, r2.PostulacionId }, corte.PostulacionesEmpatadasEnCorte);
        Assert.Equal(ClasificacionRanking.DentroDeCupo, corte.Elementos[0].Clasificacion);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, corte.Elementos[1].Clasificacion);
        Assert.All(corte.Elementos, e => Assert.True(e.EmpatadoEnLineaDeCorte));
    }

    [Fact]
    public void Debe_NoBloquearPorEmpate_Cuando_GrupoEmpatadoQuedaCompletamenteFueraPorPresupuesto()
    {
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005161"), 96.00m, _fechaBase);
        var r2 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005162"), 85.00m, _fechaBase.AddMinutes(1));
        var r3 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005163"), 85.00m, _fechaBase.AddMinutes(2));

        // Presupuesto 2500: r1 consume 2000, quedan 500. Ni r2 ($1500) ni r3 ($1500) caben individualmente.
        var disponibilidad = CrearDisponibilidad(cuposAsignados: 5, cuposPrograma: 5, presupuestoBruto: 2500.00m);
        var montos = new Dictionary<Guid, decimal>
        {
            [r1.PostulacionId] = 2000.00m,
            [r2.PostulacionId] = 1500.00m,
            [r3.PostulacionId] = 1500.00m
        };

        var corte = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2, r3], montos, disponibilidad);

        Assert.False(corte.TieneEmpateEnLineaDeCorte);
        Assert.Null(corte.PuntajeEmpateEnLineaDeCorte);
        Assert.Empty(corte.PostulacionesEmpatadasEnCorte);
        Assert.Equal(1, corte.TotalDentroDeCupo);
        Assert.Equal(2, corte.TotalListaDeEspera);
    }

    [Fact]
    public void Debe_DetectarAdvertenciasDeDiscrepanciasSinRevisarDentroDeCupoYPrimerGrupoAdyacenteExcluyendoRevisadasYLejanas()
    {
        var r1SinAlerta = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005171"), 95.00m, _fechaBase, alerta: false);
        var r2DentroSinRevisar = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005172"), 90.00m, _fechaBase.AddMinutes(1), alerta: true);
        var r3DentroRevisada = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005173"), 89.00m, _fechaBase.AddMinutes(2), alerta: true, revisada: true);
        var r4Adyacente1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005174"), 85.00m, _fechaBase.AddMinutes(3), alerta: true);
        var r5Adyacente2MismoPuntaje = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005175"), 85.00m, _fechaBase.AddMinutes(4), alerta: true);
        var r6Lejana = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005176"), 70.00m, _fechaBase.AddMinutes(5), alerta: true);

        var disponibilidad = CrearDisponibilidad(cuposAsignados: 3, cuposPrograma: 3, presupuestoBruto: 10000.00m);
        var montos = new[] { r1SinAlerta, r2DentroSinRevisar, r3DentroRevisada, r4Adyacente1, r5Adyacente2MismoPuntaje, r6Lejana }
            .ToDictionary(r => r.PostulacionId, _ => 1000.00m);

        var corte = ServicioCalculoRankingCorte.Calcular(
            _convocatoriaId,
            [r1SinAlerta, r2DentroSinRevisar, r3DentroRevisada, r4Adyacente1, r5Adyacente2MismoPuntaje, r6Lejana],
            montos,
            disponibilidad);

        Assert.True(corte.TieneAdvertenciaDiscrepanciasEnCorte);
        Assert.Equal(3, corte.CantidadDiscrepanciasSinRevisarEnCorte);
        Assert.Equal(
            new[] { r2DentroSinRevisar.PostulacionId, r4Adyacente1.PostulacionId, r5Adyacente2MismoPuntaje.PostulacionId },
            corte.PostulacionesConDiscrepanciaSinRevisarEnCorte);
        Assert.False(corte.Elementos[2].AfectaAdvertenciaDiscrepanciaEnCorte); // revisada
        Assert.False(corte.Elementos[5].AfectaAdvertenciaDiscrepanciaEnCorte); // lejana al corte
    }

    [Fact]
    public void Debe_IncluirEmpatadosEnFronteraEnAdvertenciaDeDiscrepancias_Cuando_TienenDiscrepanciaSinRevisar()
    {
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005181"), 95.00m, _fechaBase, alerta: false);
        var r2 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005182"), 88.00m, _fechaBase.AddMinutes(1), alerta: false);
        var r3 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005183"), 88.00m, _fechaBase.AddMinutes(2), alerta: true);

        var disponibilidad = CrearDisponibilidad(cuposAsignados: 2, cuposPrograma: 2, presupuestoBruto: 10000.00m);
        var montos = new Dictionary<Guid, decimal>
        {
            [r1.PostulacionId] = 1000.00m,
            [r2.PostulacionId] = 1000.00m,
            [r3.PostulacionId] = 1000.00m
        };

        var corte = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2, r3], montos, disponibilidad);

        Assert.True(corte.TieneEmpateEnLineaDeCorte);
        Assert.True(corte.TieneAdvertenciaDiscrepanciasEnCorte);
        Assert.Equal(new[] { r3.PostulacionId }, corte.PostulacionesConDiscrepanciaSinRevisarEnCorte);
    }

    [Fact]
    public void Debe_RetornarCorteVacio_Cuando_NoExistenResultadosConsolidados()
    {
        var disponibilidad = CrearDisponibilidad(cuposAsignados: 3, cuposPrograma: 3, presupuestoBruto: 5000.00m);

        var corte = ServicioCalculoRankingCorte.Calcular(
            _convocatoriaId,
            [],
            new Dictionary<Guid, decimal>(),
            disponibilidad);

        Assert.Equal(0, corte.TotalPostulantes);
        Assert.Equal(0, corte.TotalDentroDeCupo);
        Assert.Equal(0, corte.TotalListaDeEspera);
        Assert.Equal(0.00m, corte.MontoTotalDentroDeCupo);
        Assert.False(corte.TieneEmpateEnLineaDeCorte);
        Assert.False(corte.TieneAdvertenciaDiscrepanciasEnCorte);
        Assert.Empty(corte.Elementos);
    }

    [Fact]
    public void Debe_RechazarEntradasInvalidas_Cuando_IncumplenPrecondicionesDeCalculo()
    {
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000005191"), 90.00m, _fechaBase);
        var rOtraConv = CrearResultado(
            Guid.Parse("01968000-0000-7000-8000-000000005192"),
            90.00m,
            _fechaBase,
            convocatoriaId: _otraConvocatoriaId);
        var rDuplicada = CrearResultado(r1.PostulacionId, 88.00m, _fechaBase.AddMinutes(1));

        var disponibilidad = CrearDisponibilidad(cuposAsignados: 2, cuposPrograma: 2, presupuestoBruto: 5000.00m);
        var disponibilidadOtra = CrearDisponibilidad(
            cuposAsignados: 2,
            cuposPrograma: 2,
            presupuestoBruto: 5000.00m,
            convocatoriaId: _otraConvocatoriaId);
        var montosValidos = new Dictionary<Guid, decimal> { [r1.PostulacionId] = 1000.00m };

        Assert.Throws<ArgumentNullException>(() => ServicioCalculoRankingCorte.Calcular(_convocatoriaId, null!, montosValidos, disponibilidad));
        Assert.Throws<ArgumentNullException>(() => ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1], null!, disponibilidad));
        Assert.Throws<ArgumentNullException>(() => ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1], montosValidos, null!));

        Assert.Throws<RankingInvalidoException>(() => ServicioCalculoRankingCorte.Calcular(Guid.Empty, [r1], montosValidos, disponibilidad));
        Assert.Throws<RankingInvalidoException>(() => ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1], montosValidos, disponibilidadOtra));
        Assert.Throws<RankingInvalidoException>(() => ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [null!], montosValidos, disponibilidad));
        Assert.Throws<RankingInvalidoException>(() => ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [rOtraConv], new Dictionary<Guid, decimal> { [rOtraConv.PostulacionId] = 1000.00m }, disponibilidad));
        Assert.Throws<RankingInvalidoException>(() => ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, rDuplicada], montosValidos, disponibilidad));
        Assert.Throws<RankingInvalidoException>(() => ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1], new Dictionary<Guid, decimal>(), disponibilidad));
        Assert.Throws<RankingInvalidoException>(() => ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1], new Dictionary<Guid, decimal> { [r1.PostulacionId] = 0.00m }, disponibilidad));
        Assert.Throws<RankingInvalidoException>(() => ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1], new Dictionary<Guid, decimal> { [r1.PostulacionId] = 1000.123m }, disponibilidad));
    }

    private static DisponibilidadProgramaRanking CrearDisponibilidad(
        int cuposAsignados,
        int cuposPrograma,
        decimal presupuestoBruto,
        decimal comprometidoRenovaciones = 0.00m,
        Guid? convocatoriaId = null)
        => new(
            convocatoriaId ?? _convocatoriaId,
            _programaBecaId,
            cuposAsignados,
            cuposPrograma,
            presupuestoBruto,
            comprometidoRenovaciones,
            "USD");

    private static ResultadoConsolidadoRanking CrearResultado(
        Guid postulacionId,
        decimal puntaje,
        DateTimeOffset fechaConsolidacion,
        bool alerta = false,
        bool revisada = false,
        Guid? convocatoriaId = null)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            convocatoriaId ?? _convocatoriaId,
            postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: alerta ? 25.00m : 5.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: alerta);

        var resultado = ResultadoConsolidadoRanking.Crear(calculo, fechaConsolidacion, UsuarioSistema.Id);

        if (revisada)
        {
            resultado.MarcarDiscrepanciaRevisada(
                "Discrepancia revisada por el coordinador antes del corte.",
                fechaConsolidacion.AddMinutes(30),
                _coordinadorId);
        }

        return resultado;
    }
}
