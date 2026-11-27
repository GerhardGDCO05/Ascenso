using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Consolidacion.Servicios;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Consolidacion.Servicios;

public sealed class ServicioResolucionCasoLimiteTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000008001");
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000008002");
    private static readonly Guid _decisorId = Guid.Parse("01968000-0000-7000-8000-000000008003");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_DetectarYResolverEmpatePorCupo_ReordenandoGanadoresYDesbloqueandoConfirmacion()
    {
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008101"), 95.00m, _fechaBase);
        var rA = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008102"), 88.00m, _fechaBase.AddMinutes(1));
        var rB = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008103"), 88.00m, _fechaBase.AddMinutes(2));
        var r4 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008104"), 80.00m, _fechaBase.AddMinutes(3));

        var disponibilidad = CrearDisponibilidad(cupos: 2, presupuesto: 10000.00m);
        var montos = CrearMontos([r1, rA, rB, r4], 1000.00m);

        var corteInicial = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, rA, rB, r4], montos, disponibilidad);

        Assert.True(corteInicial.TieneEmpateEnLineaDeCorte);
        Assert.True(corteInicial.TieneCasoLimiteActivo);
        Assert.NotNull(corteInicial.CasoLimiteActivo);
        Assert.Equal(TipoCasoLimite.EmpatePorCupo, corteInicial.CasoLimiteActivo!.Tipo);
        Assert.Equal(88.00m, corteInicial.CasoLimiteActivo.PuntajeAfectado);
        Assert.Equal(1, corteInicial.CasoLimiteActivo.CuposDisponibles);
        Assert.Equal(9000.00m, corteInicial.CasoLimiteActivo.PresupuestoDisponible);
        Assert.Equal(new[] { rA.PostulacionId, rB.PostulacionId }, corteInicial.CasoLimiteActivo.PostulacionesAfectadas);

        var fechaDecision = _fechaBase.AddHours(2);
        const string justificacion = "Postulante B acredita mayor índice de vulnerabilidad socioeconómica.";

        var afectados = ServicioResolucionCasoLimite.AplicarResolucion(
            corteInicial,
            [r1, rA, rB, r4],
            [rB.PostulacionId],
            justificacion,
            fechaDecision,
            _decisorId);

        Assert.Equal(2, afectados.Count);
        Assert.False(r1.TieneDecisionCasoLimite);
        Assert.False(r4.TieneDecisionCasoLimite);

        Assert.True(rB.TieneDecisionCasoLimite);
        Assert.Equal(TipoCasoLimite.EmpatePorCupo, rB.TipoCasoLimite);
        Assert.True(rB.SeleccionadoEnCasoLimite);
        Assert.Equal(justificacion, rB.JustificacionDesempateCasoLimite);
        Assert.Equal(_decisorId, rB.DecididoPorUsuarioId);
        Assert.Equal(fechaDecision, rB.FechaDecisionCasoLimite);

        Assert.True(rA.TieneDecisionCasoLimite);
        Assert.Equal(TipoCasoLimite.EmpatePorCupo, rA.TipoCasoLimite);
        Assert.False(rA.SeleccionadoEnCasoLimite);
        Assert.Equal(justificacion, rA.JustificacionDesempateCasoLimite);

        // Recálculo del corte refleja resolución, desbloquea empate y reordena B antes que A
        var corteResuelto = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, rA, rB, r4], montos, disponibilidad);

        Assert.False(corteResuelto.TieneEmpateEnLineaDeCorte);
        Assert.False(corteResuelto.TieneCasoLimiteActivo);
        Assert.True(corteResuelto.TieneCasoLimiteResuelto);
        Assert.False(corteResuelto.TieneDecisionCasoLimiteInvalidaPorDisponibilidad);
        Assert.Equal(2, corteResuelto.TotalDentroDeCupo);
        Assert.Equal(2, corteResuelto.TotalListaDeEspera);

        Assert.Equal(r1.PostulacionId, corteResuelto.Elementos[0].PostulacionId);
        Assert.Equal(1, corteResuelto.Elementos[0].PosicionRanking);
        Assert.Equal(ClasificacionRanking.DentroDeCupo, corteResuelto.Elementos[0].Clasificacion);
        Assert.False(corteResuelto.Elementos[0].CasoLimiteResuelto);

        Assert.Equal(rB.PostulacionId, corteResuelto.Elementos[1].PostulacionId);
        Assert.Equal(2, corteResuelto.Elementos[1].PosicionRanking);
        Assert.Equal(ClasificacionRanking.DentroDeCupo, corteResuelto.Elementos[1].Clasificacion);
        Assert.True(corteResuelto.Elementos[1].CasoLimiteResuelto);
        Assert.True(corteResuelto.Elementos[1].SeleccionadoEnCasoLimite);

        Assert.Equal(rA.PostulacionId, corteResuelto.Elementos[2].PostulacionId);
        Assert.Equal(3, corteResuelto.Elementos[2].PosicionRanking);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, corteResuelto.Elementos[2].Clasificacion);
        Assert.True(corteResuelto.Elementos[2].CasoLimiteResuelto);
        Assert.False(corteResuelto.Elementos[2].SeleccionadoEnCasoLimite);

        Assert.Equal(r4.PostulacionId, corteResuelto.Elementos[3].PostulacionId);
        Assert.Equal(4, corteResuelto.Elementos[3].PosicionRanking);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, corteResuelto.Elementos[3].Clasificacion);
    }

    [Fact]
    public void Debe_DetectarYResolverEmpatePorPresupuesto_Cuando_CuposAlcanzanPeroPresupuestoNo()
    {
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008111"), 90.00m, _fechaBase);
        var r2 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008112"), 90.00m, _fechaBase.AddMinutes(1));

        var disponibilidad = CrearDisponibilidad(cupos: 5, presupuesto: 3000.00m);
        var montos = new Dictionary<Guid, decimal>
        {
            [r1.PostulacionId] = 2000.00m,
            [r2.PostulacionId] = 2000.00m
        };

        var corteInicial = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2], montos, disponibilidad);

        Assert.True(corteInicial.TieneEmpateEnLineaDeCorte);
        Assert.True(corteInicial.TieneCasoLimiteActivo);
        Assert.Equal(TipoCasoLimite.EmpatePorPresupuesto, corteInicial.CasoLimiteActivo!.Tipo);
        Assert.Equal(90.00m, corteInicial.CasoLimiteActivo.PuntajeAfectado);
        Assert.Equal(5, corteInicial.CasoLimiteActivo.CuposDisponibles);
        Assert.Equal(3000.00m, corteInicial.CasoLimiteActivo.PresupuestoDisponible);

        ServicioResolucionCasoLimite.AplicarResolucion(
            corteInicial,
            [r1, r2],
            [r2.PostulacionId],
            "Prioridad presupuestaria asignada conforme acta del comité.",
            _fechaBase.AddHours(1),
            _decisorId);

        var corteResuelto = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2], montos, disponibilidad);

        Assert.False(corteResuelto.TieneEmpateEnLineaDeCorte);
        Assert.True(corteResuelto.TieneCasoLimiteResuelto);
        Assert.Equal(r2.PostulacionId, corteResuelto.Elementos[0].PostulacionId);
        Assert.Equal(ClasificacionRanking.DentroDeCupo, corteResuelto.Elementos[0].Clasificacion);
        Assert.Equal(r1.PostulacionId, corteResuelto.Elementos[1].PostulacionId);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, corteResuelto.Elementos[1].Clasificacion);
    }

    [Fact]
    public void Debe_DetectarYResolverCasoLimitePresupuestarioSinEmpate_PreservandoOrdenDeMeritoEnListaDeEspera()
    {
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008121"), 94.00m, _fechaBase);
        var r2 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008122"), 89.00m, _fechaBase.AddMinutes(1));
        var r3 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008123"), 80.00m, _fechaBase.AddMinutes(2));

        var disponibilidad = CrearDisponibilidad(cupos: 5, presupuesto: 2500.00m);
        var montos = new Dictionary<Guid, decimal>
        {
            [r1.PostulacionId] = 2000.00m,
            [r2.PostulacionId] = 2000.00m,
            [r3.PostulacionId] = 400.00m
        };

        // Sin decisión: mantiene corte estricto por mérito y reporta caso límite presupuestario activo
        var corteInicial = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2, r3], montos, disponibilidad);

        Assert.False(corteInicial.TieneEmpateEnLineaDeCorte);
        Assert.True(corteInicial.TieneCasoLimiteActivo);
        Assert.Equal(TipoCasoLimite.CasoLimitePresupuestario, corteInicial.CasoLimiteActivo!.Tipo);
        Assert.Null(corteInicial.CasoLimiteActivo.PuntajeAfectado);
        Assert.Equal(4, corteInicial.CasoLimiteActivo.CuposDisponibles);
        Assert.Equal(500.00m, corteInicial.CasoLimiteActivo.PresupuestoDisponible);
        Assert.Equal(new[] { r3.PostulacionId }, corteInicial.CasoLimiteActivo.PostulacionesAfectadas);
        Assert.Equal(ClasificacionRanking.DentroDeCupo, corteInicial.Elementos[0].Clasificacion);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, corteInicial.Elementos[1].Clasificacion);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, corteInicial.Elementos[2].Clasificacion);

        // Con resolución seleccionando r3: r3 pasa a DENTRO_DE_CUPO y r2 conserva posición 2 en LISTA_DE_ESPERA
        var afectados = ServicioResolucionCasoLimite.AplicarResolucion(
            corteInicial,
            [r1, r2, r3],
            [r3.PostulacionId],
            "Aprovechamiento del saldo presupuestario restante de $500 según normativa.",
            _fechaBase.AddHours(1),
            _decisorId);

        Assert.Single(afectados);
        Assert.Equal(r3.PostulacionId, afectados[0].PostulacionId);
        Assert.Equal(TipoCasoLimite.CasoLimitePresupuestario, r3.TipoCasoLimite);
        Assert.True(r3.SeleccionadoEnCasoLimite);

        var corteResuelto = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2, r3], montos, disponibilidad);

        Assert.False(corteResuelto.TieneEmpateEnLineaDeCorte);
        Assert.False(corteResuelto.TieneCasoLimiteActivo);
        Assert.True(corteResuelto.TieneCasoLimiteResuelto);
        Assert.False(corteResuelto.TieneDecisionCasoLimiteInvalidaPorDisponibilidad);
        Assert.Equal(2, corteResuelto.TotalDentroDeCupo);
        Assert.Equal(1, corteResuelto.TotalListaDeEspera);
        Assert.Equal(2400.00m, corteResuelto.MontoTotalDentroDeCupo);

        // Orden de mérito preservado: r1 pos 1 (DentroDeCupo), r2 pos 2 (ListaDeEspera), r3 pos 3 (DentroDeCupo)
        Assert.Equal(r1.PostulacionId, corteResuelto.Elementos[0].PostulacionId);
        Assert.Equal(1, corteResuelto.Elementos[0].PosicionRanking);
        Assert.Equal(ClasificacionRanking.DentroDeCupo, corteResuelto.Elementos[0].Clasificacion);

        Assert.Equal(r2.PostulacionId, corteResuelto.Elementos[1].PostulacionId);
        Assert.Equal(2, corteResuelto.Elementos[1].PosicionRanking);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, corteResuelto.Elementos[1].Clasificacion);

        Assert.Equal(r3.PostulacionId, corteResuelto.Elementos[2].PostulacionId);
        Assert.Equal(3, corteResuelto.Elementos[2].PosicionRanking);
        Assert.Equal(ClasificacionRanking.DentroDeCupo, corteResuelto.Elementos[2].Clasificacion);
        Assert.True(corteResuelto.Elementos[2].CasoLimiteResuelto);
    }

    [Fact]
    public void Debe_DetectarInvalidacionDeDecision_Cuando_DisminuyeCupoOPresupuestoTrasResolver()
    {
        // Escenario A: Empate resuelto con 2 cupos restantes seleccionando 2 de 3; luego baja a 1 cupo restante
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008131"), 88.00m, _fechaBase);
        var r2 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008132"), 88.00m, _fechaBase.AddMinutes(1));
        var r3 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008133"), 88.00m, _fechaBase.AddMinutes(2));

        var disponibilidadInicial = CrearDisponibilidad(cupos: 2, presupuesto: 10000.00m);
        var montosEmpate = CrearMontos([r1, r2, r3], 1000.00m);

        var corteInicial = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2, r3], montosEmpate, disponibilidadInicial);
        ServicioResolucionCasoLimite.AplicarResolucion(
            corteInicial,
            [r1, r2, r3],
            [r1.PostulacionId, r2.PostulacionId],
            "Se seleccionan los dos postulantes con mayor vulnerabilidad.",
            _fechaBase.AddHours(1),
            _decisorId);

        // Baja disponibilidad a 1 cupo -> la selección de 2 ya no cabe -> re-bloquea
        var disponibilidadReducida = CrearDisponibilidad(cupos: 1, presupuesto: 10000.00m);
        var corteInvalidadoEmpate = ServicioCalculoRankingCorte.Calcular(
            _convocatoriaId,
            [r1, r2, r3],
            montosEmpate,
            disponibilidadReducida);

        Assert.True(corteInvalidadoEmpate.TieneEmpateEnLineaDeCorte);
        Assert.True(corteInvalidadoEmpate.TieneDecisionCasoLimiteInvalidaPorDisponibilidad);

        // Escenario B: Caso límite presupuestario resuelto ($400); luego presupuesto restante baja a $200
        var p1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008141"), 94.00m, _fechaBase);
        var p2 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008142"), 89.00m, _fechaBase.AddMinutes(1));
        var p3 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008143"), 80.00m, _fechaBase.AddMinutes(2));

        var montosPresupuesto = new Dictionary<Guid, decimal>
        {
            [p1.PostulacionId] = 2000.00m,
            [p2.PostulacionId] = 2000.00m,
            [p3.PostulacionId] = 400.00m
        };

        var cortePresInicial = ServicioCalculoRankingCorte.Calcular(
            _convocatoriaId,
            [p1, p2, p3],
            montosPresupuesto,
            CrearDisponibilidad(cupos: 5, presupuesto: 2500.00m));

        ServicioResolucionCasoLimite.AplicarResolucion(
            cortePresInicial,
            [p1, p2, p3],
            [p3.PostulacionId],
            "Aprovechamiento del presupuesto remanente conforme normativa vigente.",
            _fechaBase.AddHours(1),
            _decisorId);

        var cortePresReducido = ServicioCalculoRankingCorte.Calcular(
            _convocatoriaId,
            [p1, p2, p3],
            montosPresupuesto,
            CrearDisponibilidad(cupos: 5, presupuesto: 2200.00m));

        Assert.False(cortePresReducido.TieneEmpateEnLineaDeCorte);
        Assert.True(cortePresReducido.TieneDecisionCasoLimiteInvalidaPorDisponibilidad);
        Assert.Equal(ClasificacionRanking.ListaDeEspera, cortePresReducido.Elementos[2].Clasificacion);
    }

    [Fact]
    public void Debe_RechazarCasosInvalidos_EnServicioResolucionCasoLimite()
    {
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008151"), 95.00m, _fechaBase);
        var r2 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008152"), 88.00m, _fechaBase.AddMinutes(1));
        var r3 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000008153"), 88.00m, _fechaBase.AddMinutes(2));

        var montos = CrearMontos([r1, r2, r3], 1000.00m);

        // 1. Sin caso límite activo -> 422 CASO_LIMITE_NO_DETECTADO
        var corteSinCasoLimite = ServicioCalculoRankingCorte.Calcular(
            _convocatoriaId,
            [r1, r2, r3],
            montos,
            CrearDisponibilidad(cupos: 5, presupuesto: 10000.00m));

        Assert.Throws<CasoLimiteNoDetectadoException>(() =>
            ServicioResolucionCasoLimite.AplicarResolucion(
                corteSinCasoLimite,
                [r1, r2, r3],
                [r2.PostulacionId],
                "Justificación válida suficientemente extensa.",
                _fechaBase.AddHours(1),
                _decisorId));

        // Corte con empate real entre r2 y r3 (1 cupo disponible)
        var corteConEmpate = ServicioCalculoRankingCorte.Calcular(
            _convocatoriaId,
            [r1, r2, r3],
            montos,
            CrearDisponibilidad(cupos: 2, presupuesto: 10000.00m));

        // 2. Selección vacía, con Guid.Empty, duplicada o ajena al grupo -> 422 SELECCION_CASO_LIMITE_INVALIDA
        Assert.Throws<SeleccionCasoLimiteInvalidaException>(() =>
            ServicioResolucionCasoLimite.AplicarResolucion(
                corteConEmpate,
                [r1, r2, r3],
                [],
                "Justificación válida suficientemente extensa.",
                _fechaBase.AddHours(1),
                _decisorId));

        Assert.Throws<SeleccionCasoLimiteInvalidaException>(() =>
            ServicioResolucionCasoLimite.AplicarResolucion(
                corteConEmpate,
                [r1, r2, r3],
                [Guid.Empty],
                "Justificación válida suficientemente extensa.",
                _fechaBase.AddHours(1),
                _decisorId));

        Assert.Throws<SeleccionCasoLimiteInvalidaException>(() =>
            ServicioResolucionCasoLimite.AplicarResolucion(
                corteConEmpate,
                [r1, r2, r3],
                [r2.PostulacionId, r2.PostulacionId],
                "Justificación válida suficientemente extensa.",
                _fechaBase.AddHours(1),
                _decisorId));

        Assert.Throws<SeleccionCasoLimiteInvalidaException>(() =>
            ServicioResolucionCasoLimite.AplicarResolucion(
                corteConEmpate,
                [r1, r2, r3],
                [r1.PostulacionId],
                "Justificación válida suficientemente extensa.",
                _fechaBase.AddHours(1),
                _decisorId));

        // 3. Excede cupos disponibles -> 422 SELECCION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD
        Assert.Throws<SeleccionCasoLimiteExcedeDisponibilidadException>(() =>
            ServicioResolucionCasoLimite.AplicarResolucion(
                corteConEmpate,
                [r1, r2, r3],
                [r2.PostulacionId, r3.PostulacionId],
                "Justificación válida suficientemente extensa.",
                _fechaBase.AddHours(1),
                _decisorId));

        // 4. Excede presupuesto disponible -> 422 SELECCION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD
        var corteEmpatePresupuesto = ServicioCalculoRankingCorte.Calcular(
            _convocatoriaId,
            [r2, r3],
            new Dictionary<Guid, decimal>
            {
                [r2.PostulacionId] = 2000.00m,
                [r3.PostulacionId] = 2000.00m
            },
            CrearDisponibilidad(cupos: 5, presupuesto: 3000.00m));

        Assert.Throws<SeleccionCasoLimiteExcedeDisponibilidadException>(() =>
            ServicioResolucionCasoLimite.AplicarResolucion(
                corteEmpatePresupuesto,
                [r2, r3],
                [r2.PostulacionId, r3.PostulacionId],
                "Justificación válida suficientemente extensa.",
                _fechaBase.AddHours(1),
                _decisorId));

        // 5. Justificación inválida -> 422 JUSTIFICACION_CASO_LIMITE_REQUERIDA (sin mutar ningún resultado)
        Assert.Throws<JustificacionCasoLimiteInvalidaException>(() =>
            ServicioResolucionCasoLimite.AplicarResolucion(
                corteConEmpate,
                [r1, r2, r3],
                [r2.PostulacionId],
                "corta",
                _fechaBase.AddHours(1),
                _decisorId));
        Assert.False(r2.TieneDecisionCasoLimite);
        Assert.False(r3.TieneDecisionCasoLimite);

        // 6. Intento de re-resolver sobre un agregado que ya tiene decisión -> 409 CASO_LIMITE_YA_RESUELTO
        ServicioResolucionCasoLimite.AplicarResolucion(
            corteConEmpate,
            [r1, r2, r3],
            [r2.PostulacionId],
            "Justificación válida suficientemente extensa.",
            _fechaBase.AddHours(1),
            _decisorId);

        Assert.Throws<CasoLimiteYaResueltoException>(() =>
            ServicioResolucionCasoLimite.AplicarResolucion(
                corteConEmpate,
                [r1, r2, r3],
                [r3.PostulacionId],
                "Intento de sobrescribir decisión inalterable.",
                _fechaBase.AddHours(2),
                _decisorId));
    }

    private static DisponibilidadProgramaRanking CrearDisponibilidad(int cupos, decimal presupuesto)
        => new(
            _convocatoriaId,
            _programaBecaId,
            cuposAsignadosConvocatoria: cupos,
            cuposDisponiblesPrograma: cupos,
            presupuestoDisponibleBruto: presupuesto,
            presupuestoComprometidoRenovaciones: 0.00m,
            moneda: "USD");

    private static Dictionary<Guid, decimal> CrearMontos(
        IEnumerable<ResultadoConsolidadoRanking> resultados,
        decimal monto)
        => resultados.ToDictionary(r => r.PostulacionId, _ => monto);

    private static ResultadoConsolidadoRanking CrearResultado(
        Guid postulacionId,
        decimal puntaje,
        DateTimeOffset fechaConsolidacion)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: 5.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: false);

        return ResultadoConsolidadoRanking.Crear(calculo, fechaConsolidacion, UsuarioSistema.Id);
    }
}
