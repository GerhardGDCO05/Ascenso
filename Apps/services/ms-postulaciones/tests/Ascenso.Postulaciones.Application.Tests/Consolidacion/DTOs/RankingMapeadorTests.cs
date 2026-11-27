using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Consolidacion.Servicios;
using Ascenso.Postulaciones.Domain.Enums;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Consolidacion.DTOs;

public sealed class RankingMapeadorTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000006001");
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000006002");
    private static readonly Guid _postulacion1Id = Guid.Parse("01968000-0000-7000-8000-000000006003");
    private static readonly Guid _postulacion2Id = Guid.Parse("01968000-0000-7000-8000-000000006004");
    private static readonly Guid _postulacion3Id = Guid.Parse("01968000-0000-7000-8000-000000006005");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000006006");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_MapearCamposDeConfirmacionEnResultadoConsolidadoDtoYDetalleDto_Cuando_ResultadoEstaConfirmado()
    {
        var resultado = CrearResultado(_postulacion1Id, 94.50m, _fechaBase);
        var fechaConfirmacion = _fechaBase.AddHours(4);
        resultado.ConfirmarRanking(
            posicionRanking: 1,
            montoEstimadoBeca: 1500.00m,
            clasificacion: ClasificacionRanking.DentroDeCupo,
            fechaConfirmacion: fechaConfirmacion,
            usuarioConfirmadorId: _coordinadorId);

        var dto = resultado.ADto();
        var detalle = resultado.ADetalleDto([]);

        dto.EstadoConfirmacionRanking.Should().Be("CONFIRMADO");
        dto.PosicionRanking.Should().Be(1);
        dto.MontoEstimadoBeca.Should().Be(1500.00m);
        dto.Clasificacion.Should().Be("DENTRO_DE_CUPO");
        dto.FechaConfirmacionRanking.Should().Be(fechaConfirmacion);
        dto.ConfirmadoPorUsuarioId.Should().Be(_coordinadorId);

        detalle.EstadoConfirmacionRanking.Should().Be("CONFIRMADO");
        detalle.PosicionRanking.Should().Be(1);
        detalle.MontoEstimadoBeca.Should().Be(1500.00m);
        detalle.Clasificacion.Should().Be("DENTRO_DE_CUPO");
        detalle.FechaConfirmacionRanking.Should().Be(fechaConfirmacion);
        detalle.ConfirmadoPorUsuarioId.Should().Be(_coordinadorId);
    }

    [Fact]
    public void Debe_MantenerCamposDeConfirmacionNulosEnDtosDeConsolidacion_Cuando_ResultadoEsProvisional()
    {
        var resultado = CrearResultado(_postulacion1Id, 88.00m, _fechaBase);

        var dto = resultado.ADto();
        var detalle = resultado.ADetalleDto([]);

        dto.EstadoConfirmacionRanking.Should().Be("PROVISIONAL");
        dto.PosicionRanking.Should().BeNull();
        dto.MontoEstimadoBeca.Should().BeNull();
        dto.Clasificacion.Should().BeNull();
        dto.FechaConfirmacionRanking.Should().BeNull();
        dto.ConfirmadoPorUsuarioId.Should().BeNull();

        detalle.PosicionRanking.Should().BeNull();
        detalle.MontoEstimadoBeca.Should().BeNull();
        detalle.Clasificacion.Should().BeNull();
        detalle.FechaConfirmacionRanking.Should().BeNull();
        detalle.ConfirmadoPorUsuarioId.Should().BeNull();
    }

    [Fact]
    public void Debe_MapearCorteProvisionalConFiltroYPaginacionEnCodigoEnum()
    {
        var r1 = CrearResultado(_postulacion1Id, 95.00m, _fechaBase);
        var r2 = CrearResultado(_postulacion2Id, 88.00m, _fechaBase.AddMinutes(1), alerta: true);
        var r3 = CrearResultado(_postulacion3Id, 75.00m, _fechaBase.AddMinutes(2));

        var disponibilidad = new DisponibilidadProgramaRanking(
            _convocatoriaId,
            _programaBecaId,
            cuposAsignadosConvocatoria: 3,
            cuposDisponiblesPrograma: 1,
            presupuestoDisponibleBruto: 5000.00m,
            presupuestoComprometidoRenovaciones: 1000.00m,
            moneda: "usd");

        var montos = new Dictionary<Guid, decimal>
        {
            [_postulacion1Id] = 1500.00m,
            [_postulacion2Id] = 1500.00m,
            [_postulacion3Id] = 1500.00m
        };

        var corte = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2, r3], montos, disponibilidad);

        var dto = corte.ARankingProvisionalDto(
            filtroClasificacion: ClasificacionRanking.ListaDeEspera,
            pagina: 1,
            tamano: 1);

        dto.ConvocatoriaId.Should().Be(_convocatoriaId);
        dto.EstadoRanking.Should().Be("PROVISIONAL");
        dto.FechaConfirmacionRanking.Should().BeNull();
        dto.ConfirmadoPorUsuarioId.Should().BeNull();
        dto.Disponibilidad.Should().NotBeNull();
        dto.Disponibilidad!.ProgramaBecaId.Should().Be(_programaBecaId);
        dto.Disponibilidad.CuposDisponiblesEfectivos.Should().Be(1);
        dto.Disponibilidad.PresupuestoDisponibleEfectivo.Should().Be(4000.00m);
        dto.Disponibilidad.Moneda.Should().Be("USD");
        dto.TotalPostulantes.Should().Be(3);
        dto.TotalDentroDeCupo.Should().Be(1);
        dto.TotalListaDeEspera.Should().Be(2);
        dto.MontoTotalDentroDeCupo.Should().Be(1500.00m);
        dto.TieneEmpateEnLineaDeCorte.Should().BeFalse();
        dto.TieneAdvertenciaDiscrepanciasEnCorte.Should().BeTrue();
        dto.CantidadDiscrepanciasSinRevisarEnCorte.Should().Be(1);
        dto.PostulacionesConDiscrepanciaSinRevisarEnCorte.Should().Equal(_postulacion2Id);

        dto.Elementos.Total.Should().Be(2);
        dto.Elementos.Pagina.Should().Be(1);
        dto.Elementos.Tamano.Should().Be(1);
        dto.Elementos.Items.Should().ContainSingle();
        dto.Elementos.Items[0].PostulacionId.Should().Be(_postulacion2Id);
        dto.Elementos.Items[0].PosicionRanking.Should().Be(2);
        dto.Elementos.Items[0].Clasificacion.Should().Be("LISTA_DE_ESPERA");
        dto.Elementos.Items[0].AfectaAdvertenciaDiscrepanciaEnCorte.Should().BeTrue();
    }

    [Fact]
    public void Debe_MapearRankingConfirmadoOrdenadoPorPosicionSinDependenciaDeDisponibilidad()
    {
        var fechaConfirmacion = _fechaBase.AddHours(5);
        var r2 = CrearResultado(_postulacion2Id, 82.00m, _fechaBase.AddMinutes(1));
        r2.ConfirmarRanking(2, 1200.00m, ClasificacionRanking.ListaDeEspera, fechaConfirmacion, _coordinadorId);

        var r1 = CrearResultado(_postulacion1Id, 95.00m, _fechaBase);
        r1.ConfirmarRanking(1, 1500.00m, ClasificacionRanking.DentroDeCupo, fechaConfirmacion, _coordinadorId);

        var dto = RankingMapeador.ARankingConfirmadoDto(
            _convocatoriaId,
            [r2, r1],
            disponibilidad: null,
            filtroClasificacion: null,
            pagina: 1,
            tamano: 20);

        dto.EstadoRanking.Should().Be("CONFIRMADO");
        dto.FechaConfirmacionRanking.Should().Be(fechaConfirmacion);
        dto.ConfirmadoPorUsuarioId.Should().Be(_coordinadorId);
        dto.Disponibilidad.Should().BeNull();
        dto.TotalPostulantes.Should().Be(2);
        dto.TotalDentroDeCupo.Should().Be(1);
        dto.TotalListaDeEspera.Should().Be(1);
        dto.MontoTotalDentroDeCupo.Should().Be(1500.00m);
        dto.TieneEmpateEnLineaDeCorte.Should().BeFalse();
        dto.Elementos.Items.Select(e => e.PosicionRanking).Should().Equal(1, 2);
        dto.Elementos.Items[0].Clasificacion.Should().Be("DENTRO_DE_CUPO");
        dto.Elementos.Items[1].Clasificacion.Should().Be("LISTA_DE_ESPERA");
    }

    [Fact]
    public void Debe_MapearAdjudicabilidadConMotivoCorrecto_SegunEstadoYClasificacion()
    {
        var provisional = CrearResultado(_postulacion1Id, 90.00m, _fechaBase);
        var dtoProvisional = provisional.AAdjudicabilidadDto();

        dtoProvisional.PostulacionId.Should().Be(_postulacion1Id);
        dtoProvisional.ConvocatoriaOrigenId.Should().Be(_convocatoriaId);
        dtoProvisional.Adjudicable.Should().BeFalse();
        dtoProvisional.MotivoNoAdjudicable.Should().Be(RankingMapeador.MotivoRankingNoConfirmado);
        dtoProvisional.EstadoConfirmacionRanking.Should().Be("PROVISIONAL");
        dtoProvisional.Clasificacion.Should().BeNull();
        dtoProvisional.PosicionRanking.Should().BeNull();
        dtoProvisional.MontoEstimadoBeca.Should().BeNull();
        dtoProvisional.FechaConfirmacionRanking.Should().BeNull();

        var dentro = CrearResultado(_postulacion2Id, 95.00m, _fechaBase);
        var fechaConf = _fechaBase.AddHours(2);
        dentro.ConfirmarRanking(1, 1500.00m, ClasificacionRanking.DentroDeCupo, fechaConf, _coordinadorId);
        var dtoDentro = dentro.AAdjudicabilidadDto();

        dtoDentro.Adjudicable.Should().BeTrue();
        dtoDentro.MotivoNoAdjudicable.Should().BeNull();
        dtoDentro.EstadoConfirmacionRanking.Should().Be("CONFIRMADO");
        dtoDentro.Clasificacion.Should().Be("DENTRO_DE_CUPO");
        dtoDentro.PosicionRanking.Should().Be(1);
        dtoDentro.MontoEstimadoBeca.Should().Be(1500.00m);
        dtoDentro.FechaConfirmacionRanking.Should().Be(fechaConf);

        var espera = CrearResultado(_postulacion3Id, 78.00m, _fechaBase);
        espera.ConfirmarRanking(2, 1500.00m, ClasificacionRanking.ListaDeEspera, fechaConf, _coordinadorId);
        var dtoEspera = espera.AAdjudicabilidadDto();

        dtoEspera.Adjudicable.Should().BeFalse();
        dtoEspera.MotivoNoAdjudicable.Should().Be(RankingMapeador.MotivoFueraDeCupoEnListaDeEspera);
        dtoEspera.EstadoConfirmacionRanking.Should().Be("CONFIRMADO");
        dtoEspera.Clasificacion.Should().Be("LISTA_DE_ESPERA");
        dtoEspera.PosicionRanking.Should().Be(2);
    }

    private static ResultadoConsolidadoRanking CrearResultado(
        Guid postulacionId,
        decimal puntaje,
        DateTimeOffset fechaConsolidacion,
        bool alerta = false)
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: puntaje,
            diferenciaEntreEvaluadores: alerta ? 24.00m : 6.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: alerta);

        return ResultadoConsolidadoRanking.Crear(calculo, fechaConsolidacion, UsuarioSistema.Id);
    }
}
