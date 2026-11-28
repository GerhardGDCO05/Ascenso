using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Application.ListaEspera.DTOs;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.ListaEspera.DTOs;

public sealed class ListaEsperaMapeadorTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01969000-0000-7000-8000-000000001001");
    private static readonly Guid _programaBecaId = Guid.Parse("01969000-0000-7000-8000-000000001002");
    private static readonly Guid _coordinadorId = Guid.Parse("01969000-0000-7000-8000-000000001003");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_MapearEntradaListaEsperaConEstadoEnMayusculasYMetadatos()
    {
        var postulacionId = Guid.NewGuid();
        var entrada = EntradaListaEspera.Crear(
            _convocatoriaId,
            _programaBecaId,
            postulacionId,
            posicionOrden: 1,
            montoEstimadoBecaSnapshot: 1500.00m,
            _fechaBase,
            _coordinadorId);

        entrada.MarcarElegiblePorVacante(_fechaBase.AddHours(1), UsuarioSistema.Id);
        entrada.Promover(_fechaBase.AddHours(2), _coordinadorId, "Reasignación por vacante liberada");

        var dto = entrada.ADto();

        dto.Id.Should().Be(entrada.Id);
        dto.ConvocatoriaId.Should().Be(_convocatoriaId);
        dto.ProgramaBecaId.Should().Be(_programaBecaId);
        dto.PostulacionId.Should().Be(postulacionId);
        dto.PosicionOrden.Should().Be(1);
        dto.MontoEstimadoBecaSnapshot.Should().Be(1500.00m);
        dto.Estado.Should().Be("PROMOVIDA");
        dto.FechaIngreso.Should().Be(_fechaBase);
        dto.FechaElegibilidad.Should().Be(_fechaBase.AddHours(1));
        dto.FechaPromocion.Should().Be(_fechaBase.AddHours(2));
        dto.PromovidoPorUsuarioId.Should().Be(_coordinadorId);
        dto.MotivoPromocion.Should().Be("Reasignación por vacante liberada");
        dto.Version.Should().Be(3);
    }

    [Fact]
    public void Debe_MapearListaEsperaConvocatoriaConContadoresGlobalesOrdenFifoFiltroYPaginacion()
    {
        var e1 = EntradaListaEspera.Crear(_convocatoriaId, _programaBecaId, Guid.NewGuid(), 1, 1500.00m, _fechaBase, _coordinadorId);
        var e2 = EntradaListaEspera.Crear(_convocatoriaId, _programaBecaId, Guid.NewGuid(), 2, 1400.00m, _fechaBase, _coordinadorId);
        var e3 = EntradaListaEspera.Crear(_convocatoriaId, _programaBecaId, Guid.NewGuid(), 3, 1300.00m, _fechaBase, _coordinadorId);

        e1.Promover(_fechaBase.AddHours(1), _coordinadorId);
        e2.MarcarElegiblePorVacante(_fechaBase.AddHours(2), UsuarioSistema.Id);

        var dtoSinFiltro = ListaEsperaMapeador.AListaEsperaConvocatoriaDto(
            _convocatoriaId,
            [e3, e1, e2],
            filtroEstado: null,
            pagina: 1,
            tamano: 2);

        dtoSinFiltro.ConvocatoriaId.Should().Be(_convocatoriaId);
        dtoSinFiltro.TotalEnListaEspera.Should().Be(3);
        dtoSinFiltro.TotalEnEspera.Should().Be(1);
        dtoSinFiltro.TotalElegiblesPorVacante.Should().Be(1);
        dtoSinFiltro.TotalPromovidas.Should().Be(1);
        dtoSinFiltro.Entradas.Total.Should().Be(3);
        dtoSinFiltro.Entradas.Pagina.Should().Be(1);
        dtoSinFiltro.Entradas.Tamano.Should().Be(2);
        dtoSinFiltro.Entradas.Items.Select(i => i.PosicionOrden).Should().Equal(1, 2);

        var dtoFiltrado = ListaEsperaMapeador.AListaEsperaConvocatoriaDto(
            _convocatoriaId,
            [e3, e1, e2],
            filtroEstado: EstadoListaEspera.ElegiblePorVacante,
            pagina: 1,
            tamano: 20);

        dtoFiltrado.TotalEnListaEspera.Should().Be(3);
        dtoFiltrado.TotalEnEspera.Should().Be(1);
        dtoFiltrado.TotalElegiblesPorVacante.Should().Be(1);
        dtoFiltrado.TotalPromovidas.Should().Be(1);
        dtoFiltrado.Entradas.Total.Should().Be(1);
        dtoFiltrado.Entradas.Items.Should().ContainSingle(i => i.Id == e2.Id && i.Estado == "ELEGIBLE_POR_VACANTE");
    }

    [Fact]
    public void Debe_MapearAdjudicabilidadParaDentroDeCupoListaDeEsperaSinPromoverYPromovida()
    {
        // 1. CONFIRMADO + DENTRO_DE_CUPO -> adjudicable = true, promovidaDeListaEspera = false
        var postDentro = Guid.NewGuid();
        var resultadoDentro = CrearResultadoConfirmado(postDentro, 1, 1500.00m, ClasificacionRanking.DentroDeCupo);

        var dtoDentro = resultadoDentro.AAdjudicabilidadDto();
        dtoDentro.Adjudicable.Should().BeTrue();
        dtoDentro.MotivoNoAdjudicable.Should().BeNull();
        dtoDentro.PromovidaDeListaEspera.Should().BeFalse();
        dtoDentro.EstadoListaEspera.Should().BeNull();
        dtoDentro.PosicionListaEspera.Should().BeNull();
        dtoDentro.FechaPromocionListaEspera.Should().BeNull();
        dtoDentro.ProgramaBecaId.Should().BeNull();

        // 2. CONFIRMADO + LISTA_DE_ESPERA en EN_ESPERA -> adjudicable = false
        var postEspera = Guid.NewGuid();
        var resultadoEspera = CrearResultadoConfirmado(postEspera, 2, 1400.00m, ClasificacionRanking.ListaDeEspera);
        var entradaEnEspera = EntradaListaEspera.Crear(
            _convocatoriaId, _programaBecaId, postEspera, 1, 1400.00m, _fechaBase, _coordinadorId);

        var dtoEnEspera = resultadoEspera.AAdjudicabilidadDto(entradaEnEspera);
        dtoEnEspera.Adjudicable.Should().BeFalse();
        dtoEnEspera.MotivoNoAdjudicable.Should().Be(RankingMapeador.MotivoFueraDeCupoEnListaDeEspera);
        dtoEnEspera.PromovidaDeListaEspera.Should().BeFalse();
        dtoEnEspera.EstadoListaEspera.Should().Be("EN_ESPERA");
        dtoEnEspera.PosicionListaEspera.Should().Be(1);
        dtoEnEspera.FechaPromocionListaEspera.Should().BeNull();
        dtoEnEspera.ProgramaBecaId.Should().Be(_programaBecaId);

        // 3. CONFIRMADO + LISTA_DE_ESPERA en ELEGIBLE_POR_VACANTE -> adjudicable = false
        entradaEnEspera.MarcarElegiblePorVacante(_fechaBase.AddHours(1), UsuarioSistema.Id);
        var dtoElegible = resultadoEspera.AAdjudicabilidadDto(entradaEnEspera);
        dtoElegible.Adjudicable.Should().BeFalse();
        dtoElegible.MotivoNoAdjudicable.Should().Be(RankingMapeador.MotivoFueraDeCupoEnListaDeEspera);
        dtoElegible.PromovidaDeListaEspera.Should().BeFalse();
        dtoElegible.EstadoListaEspera.Should().Be("ELEGIBLE_POR_VACANTE");
        dtoElegible.PosicionListaEspera.Should().Be(1);
        dtoElegible.FechaPromocionListaEspera.Should().BeNull();
        dtoElegible.ProgramaBecaId.Should().Be(_programaBecaId);

        // 4. CONFIRMADO + LISTA_DE_ESPERA en PROMOVIDA -> adjudicable = true
        var fechaPromocion = _fechaBase.AddHours(2);
        entradaEnEspera.Promover(fechaPromocion, _coordinadorId, "Vacante confirmada");
        var dtoPromovida = resultadoEspera.AAdjudicabilidadDto(entradaEnEspera);
        dtoPromovida.Adjudicable.Should().BeTrue();
        dtoPromovida.MotivoNoAdjudicable.Should().BeNull();
        dtoPromovida.PromovidaDeListaEspera.Should().BeTrue();
        dtoPromovida.EstadoListaEspera.Should().Be("PROMOVIDA");
        dtoPromovida.PosicionListaEspera.Should().Be(1);
        dtoPromovida.FechaPromocionListaEspera.Should().Be(fechaPromocion);
        dtoPromovida.ProgramaBecaId.Should().Be(_programaBecaId);
        dtoPromovida.EstadoConfirmacionRanking.Should().Be("CONFIRMADO");
        dtoPromovida.Clasificacion.Should().Be("LISTA_DE_ESPERA");
        dtoPromovida.PosicionRanking.Should().Be(2);
        dtoPromovida.MontoEstimadoBeca.Should().Be(1400.00m);
    }

    private static ResultadoConsolidadoRanking CrearResultadoConfirmado(
        Guid postulacionId,
        int posicion,
        decimal monto,
        ClasificacionRanking clasificacion)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: 90.00m,
            diferenciaEntreEvaluadores: 2.00m,
            umbralDiscrepanciaAplicado: 15.00m,
            alertaDiscrepancia: false);

        var resultado = ResultadoConsolidadoRanking.Crear(calculo, _fechaBase, UsuarioSistema.Id);
        resultado.ConfirmarRanking(posicion, monto, clasificacion, _fechaBase, _coordinadorId);
        return resultado;
    }
}
