using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using Ascenso.Postulaciones.Domain.ListaEspera.Servicios;

namespace Ascenso.Postulaciones.Domain.Tests.ListaEspera.Servicios;

public sealed class ServiciosListaEsperaTests
{
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 27, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_ConstruirColaSecuencialRespetandoPosicionRankingYDecisionesDeCasoLimite_Cuando_HayResultadosEnListaDeEspera()
    {
        var convocatoriaId = Guid.NewGuid();
        var programaBecaId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var post1 = Guid.NewGuid();
        var post2 = Guid.NewGuid();
        var post3 = Guid.NewGuid();
        var post4 = Guid.NewGuid();

        // Caso límite presupuestario HU 10.1: pos 1 y pos 3 dentro de cupo; pos 2 y pos 4 en lista de espera.
        var r4 = CrearResultadoConfirmado(convocatoriaId, post4, 70.00m, 4, 1200.00m, ClasificacionRanking.ListaDeEspera);
        var r2 = CrearResultadoConfirmado(convocatoriaId, post2, 85.00m, 2, 1800.00m, ClasificacionRanking.ListaDeEspera);
        var r1 = CrearResultadoConfirmado(convocatoriaId, post1, 95.00m, 1, 1500.00m, ClasificacionRanking.DentroDeCupo);
        var r3 = CrearResultadoConfirmado(convocatoriaId, post3, 80.00m, 3, 900.00m, ClasificacionRanking.DentroDeCupo);

        var entradas = ServicioConstruccionListaEspera.ConstruirDesdeResultadosConfirmados(
            convocatoriaId,
            programaBecaId,
            [r4, r1, r2, r3],
            _fechaBase,
            actorId);

        Assert.Equal(2, entradas.Count);

        Assert.Equal(1, entradas[0].PosicionOrden);
        Assert.Equal(post2, entradas[0].PostulacionId);
        Assert.Equal(1800.00m, entradas[0].MontoEstimadoBecaSnapshot);
        Assert.Equal(programaBecaId, entradas[0].ProgramaBecaId);
        Assert.Equal(EstadoListaEspera.EnEspera, entradas[0].Estado);
        Assert.Equal(_fechaBase, entradas[0].FechaIngreso);

        Assert.Equal(2, entradas[1].PosicionOrden);
        Assert.Equal(post4, entradas[1].PostulacionId);
        Assert.Equal(1200.00m, entradas[1].MontoEstimadoBecaSnapshot);
        Assert.Equal(EstadoListaEspera.EnEspera, entradas[1].Estado);
    }

    [Fact]
    public void Debe_RetornarListaVacia_Cuando_TodosLosResultadosConfirmadosEstanDentroDeCupo()
    {
        var convocatoriaId = Guid.NewGuid();
        var programaBecaId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var r1 = CrearResultadoConfirmado(convocatoriaId, Guid.NewGuid(), 95.00m, 1, 1500.00m, ClasificacionRanking.DentroDeCupo);
        var r2 = CrearResultadoConfirmado(convocatoriaId, Guid.NewGuid(), 90.00m, 2, 1400.00m, ClasificacionRanking.DentroDeCupo);

        var entradas = ServicioConstruccionListaEspera.ConstruirDesdeResultadosConfirmados(
            convocatoriaId,
            programaBecaId,
            [r1, r2],
            _fechaBase,
            actorId);

        Assert.Empty(entradas);
    }

    [Fact]
    public void Debe_RechazarConstruccionConListaEsperaInvalida_Cuando_HayResultadosNoConfirmadosODeOtraConvocatoria()
    {
        var convocatoriaId = Guid.NewGuid();
        var otraConvocatoriaId = Guid.NewGuid();
        var programaBecaId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var provisional = CrearResultadoProvisional(convocatoriaId, Guid.NewGuid(), 85.00m);
        var deOtraConvocatoria = CrearResultadoConfirmado(otraConvocatoriaId, Guid.NewGuid(), 80.00m, 1, 1000.00m, ClasificacionRanking.ListaDeEspera);

        Assert.Throws<ListaEsperaInvalidaException>(() =>
            ServicioConstruccionListaEspera.ConstruirDesdeResultadosConfirmados(
                Guid.Empty, programaBecaId, [], _fechaBase, actorId));

        Assert.Throws<ListaEsperaInvalidaException>(() =>
            ServicioConstruccionListaEspera.ConstruirDesdeResultadosConfirmados(
                convocatoriaId, Guid.Empty, [], _fechaBase, actorId));

        Assert.Throws<ArgumentNullException>(() =>
            ServicioConstruccionListaEspera.ConstruirDesdeResultadosConfirmados(
                convocatoriaId, programaBecaId, null!, _fechaBase, actorId));

        Assert.Throws<ListaEsperaInvalidaException>(() =>
            ServicioConstruccionListaEspera.ConstruirDesdeResultadosConfirmados(
                convocatoriaId, programaBecaId, [provisional], _fechaBase, actorId));

        Assert.Throws<ListaEsperaInvalidaException>(() =>
            ServicioConstruccionListaEspera.ConstruirDesdeResultadosConfirmados(
                convocatoriaId, programaBecaId, [deOtraConvocatoria], _fechaBase, actorId));
    }

    [Fact]
    public void Debe_SeleccionarMenorPosicionEnEsperaParaVacante_Cuando_ExistenCandidatosEnEspera()
    {
        var convocatoriaId = Guid.NewGuid();
        var e1 = CrearEntrada(convocatoriaId, 1);
        var e2 = CrearEntrada(convocatoriaId, 2);
        var e3 = CrearEntrada(convocatoriaId, 3);

        e1.MarcarElegiblePorVacante(_fechaBase.AddHours(1), Guid.NewGuid());

        // Se pasa desordenado para validar ordenamiento defensivo por PosicionOrden ASC
        var seleccionado = ServicioPromocionListaEspera.SeleccionarSiguienteParaVacante([e3, e1, e2]);

        Assert.Equal(e2.Id, seleccionado.Id);
    }

    [Fact]
    public void Debe_LanzarListaEsperaSinCandidatos_Cuando_NoHayEntradasEnEstadoEnEsperaParaVacante()
    {
        var convocatoriaId = Guid.NewGuid();
        var e1 = CrearEntrada(convocatoriaId, 1);
        var e2 = CrearEntrada(convocatoriaId, 2);
        e1.Promover(_fechaBase.AddHours(1), Guid.NewGuid());
        e2.MarcarElegiblePorVacante(_fechaBase.AddHours(2), Guid.NewGuid());

        Assert.Throws<ListaEsperaSinCandidatosException>(() =>
            ServicioPromocionListaEspera.SeleccionarSiguienteParaVacante([]));

        Assert.Throws<ListaEsperaSinCandidatosException>(() =>
            ServicioPromocionListaEspera.SeleccionarSiguienteParaVacante([e1, e2]));
    }

    [Fact]
    public void Debe_ResolverSiguienteCandidatoEnOrdenFifo_Cuando_SePromuevePorIdOPorSiguienteEnCola()
    {
        var convocatoriaId = Guid.NewGuid();
        var e1 = CrearEntrada(convocatoriaId, 1);
        var e2 = CrearEntrada(convocatoriaId, 2);
        var e3 = CrearEntrada(convocatoriaId, 3);

        e1.Promover(_fechaBase.AddHours(1), Guid.NewGuid());
        e2.MarcarElegiblePorVacante(_fechaBase.AddHours(2), Guid.NewGuid());

        var porSiguiente = ServicioPromocionListaEspera.ResolverCandidatoAPromover([e3, e1, e2], null);
        Assert.Equal(e2.Id, porSiguiente.Id);

        var porId = ServicioPromocionListaEspera.ResolverCandidatoAPromover([e3, e1, e2], e2.Id);
        Assert.Equal(e2.Id, porId.Id);
    }

    [Fact]
    public void Debe_RechazarPromocionConExcepcionApropiada_Cuando_ViolaFifoYaPromovidaNoExisteOSinCandidatos()
    {
        var convocatoriaId = Guid.NewGuid();
        var e1 = CrearEntrada(convocatoriaId, 1);
        var e2 = CrearEntrada(convocatoriaId, 2);
        var e3 = CrearEntrada(convocatoriaId, 3);
        e1.Promover(_fechaBase.AddHours(1), Guid.NewGuid());

        // Intento de saltar e2 (EN_ESPERA) para promover e3
        var exFifo = Assert.Throws<OrdenListaEsperaVioladoException>(() =>
            ServicioPromocionListaEspera.ResolverCandidatoAPromover([e1, e2, e3], e3.Id));
        Assert.Equal(409, exFifo.EstadoHttp);

        // Intento de promover e1 ya promovida
        var exPromovida = Assert.Throws<EntradaListaEsperaYaPromovidaException>(() =>
            ServicioPromocionListaEspera.ResolverCandidatoAPromover([e1, e2, e3], e1.Id));
        Assert.Equal(409, exPromovida.EstadoHttp);

        // Intento de promover ID inexistente
        var exNoEncontrada = Assert.Throws<EntradaListaEsperaNoEncontradaException>(() =>
            ServicioPromocionListaEspera.ResolverCandidatoAPromover([e1, e2, e3], Guid.NewGuid()));
        Assert.Equal(404, exNoEncontrada.EstadoHttp);

        // Todos promovidos y se solicita siguiente (null)
        e2.Promover(_fechaBase.AddHours(2), Guid.NewGuid());
        e3.Promover(_fechaBase.AddHours(3), Guid.NewGuid());
        var exSinCandidatos = Assert.Throws<ListaEsperaSinCandidatosException>(() =>
            ServicioPromocionListaEspera.ResolverCandidatoAPromover([e1, e2, e3], null));
        Assert.Equal(404, exSinCandidatos.EstadoHttp);
    }

    [Fact]
    public void Debe_ValidarCupoYPresupuestoEfectivoParaPromocion_Cuando_SeVerificaDisponibilidadDeMsPrg()
    {
        var convocatoriaId = Guid.NewGuid();
        var programaBecaId = Guid.NewGuid();

        var disponible = new DisponibilidadProgramaRanking(
            convocatoriaId,
            programaBecaId,
            cuposAsignadosConvocatoria: 2,
            cuposDisponiblesPrograma: 1,
            presupuestoDisponibleBruto: 3000.00m,
            presupuestoComprometidoRenovaciones: 1000.00m,
            moneda: "USD");

        // Exacto al presupuesto efectivo (2000.00) y 1 cupo disponible -> válido
        ServicioPromocionListaEspera.ValidarDisponibilidadParaPromocion(disponible, 2000.00m);

        // Presupuesto insuficiente -> 422 PRESUPUESTO_INSUFICIENTE_PROMOCION
        var exPresupuesto = Assert.Throws<PresupuestoInsuficientePromocionException>(() =>
            ServicioPromocionListaEspera.ValidarDisponibilidadParaPromocion(disponible, 2000.01m));
        Assert.Equal(422, exPresupuesto.EstadoHttp);
        Assert.Equal("PRESUPUESTO_INSUFICIENTE_PROMOCION", exPresupuesto.Codigo);

        // Sin cupo efectivo -> 409 SIN_CUPO_DISPONIBLE_PROMOCION (tiene prioridad aunque presupuesto alcance)
        var sinCupo = new DisponibilidadProgramaRanking(
            convocatoriaId,
            programaBecaId,
            cuposAsignadosConvocatoria: 2,
            cuposDisponiblesPrograma: 0,
            presupuestoDisponibleBruto: 5000.00m,
            presupuestoComprometidoRenovaciones: 0.00m,
            moneda: "USD");

        var exCupo = Assert.Throws<SinCupoDisponiblePromocionException>(() =>
            ServicioPromocionListaEspera.ValidarDisponibilidadParaPromocion(sinCupo, 1000.00m));
        Assert.Equal(409, exCupo.EstadoHttp);
        Assert.Equal("SIN_CUPO_DISPONIBLE_PROMOCION", exCupo.Codigo);

        // Monto inválido -> ListaEsperaInvalidaException
        Assert.Throws<ListaEsperaInvalidaException>(() =>
            ServicioPromocionListaEspera.ValidarDisponibilidadParaPromocion(disponible, 0.00m));
    }

    private static ResultadoConsolidadoRanking CrearResultadoProvisional(
        Guid convocatoriaId,
        Guid postulacionId,
        decimal puntaje)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            convocatoriaId,
            postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: 2.00m,
            umbralDiscrepanciaAplicado: 15.00m,
            alertaDiscrepancia: false);

        return ResultadoConsolidadoRanking.Crear(calculo, _fechaBase, Guid.NewGuid());
    }

    private static ResultadoConsolidadoRanking CrearResultadoConfirmado(
        Guid convocatoriaId,
        Guid postulacionId,
        decimal puntaje,
        int posicion,
        decimal monto,
        ClasificacionRanking clasificacion)
    {
        var resultado = CrearResultadoProvisional(convocatoriaId, postulacionId, puntaje);
        resultado.ConfirmarRanking(posicion, monto, clasificacion, _fechaBase, Guid.NewGuid());
        return resultado;
    }

    private static EntradaListaEspera CrearEntrada(Guid convocatoriaId, int posicion)
        => EntradaListaEspera.Crear(
            convocatoriaId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            posicion,
            1500.00m,
            _fechaBase,
            Guid.NewGuid());
}
