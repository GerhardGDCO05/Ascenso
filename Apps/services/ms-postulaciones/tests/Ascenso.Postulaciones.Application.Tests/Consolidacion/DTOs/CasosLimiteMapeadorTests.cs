using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Consolidacion.Servicios;
using Ascenso.Postulaciones.Domain.Enums;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Consolidacion.DTOs;

public sealed class CasosLimiteMapeadorTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000009001");
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000009002");
    private static readonly Guid _decisorId = Guid.Parse("01968000-0000-7000-8000-000000009003");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_ExponerContratoMinimoIsp_EnIRolesUsuarioActual()
    {
        var tipo = typeof(IRolesUsuarioActual);
        tipo.IsInterface.Should().BeTrue();

        var metodo = Assert.Single(tipo.GetMethods(), m => m.Name == nameof(IRolesUsuarioActual.TieneRol));
        metodo.ReturnType.Should().Be<bool>();

        var parametros = metodo.GetParameters();
        parametros.Should().HaveCount(1);
        parametros[0].ParameterType.Should().Be<string>();
    }

    [Fact]
    public void Debe_MapearCamposDeCasoLimiteActivoEnRankingProvisionalDto()
    {
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000009101"), 95.00m, _fechaBase);
        var r2 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000009102"), 88.00m, _fechaBase.AddMinutes(1));
        var r3 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000009103"), 88.00m, _fechaBase.AddMinutes(2));

        var disponibilidad = CrearDisponibilidad(cupos: 2, presupuesto: 10000.00m);
        var montos = new Dictionary<Guid, decimal>
        {
            [r1.PostulacionId] = 1000.00m,
            [r2.PostulacionId] = 1000.00m,
            [r3.PostulacionId] = 1000.00m
        };

        var corte = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2, r3], montos, disponibilidad);

        var dto = corte.ARankingProvisionalDto(filtroClasificacion: null, pagina: 1, tamano: 10);

        dto.TieneCasoLimiteActivo.Should().BeTrue();
        dto.TipoCasoLimiteActivo.Should().Be("EMPATE_POR_CUPO");
        dto.PostulacionesEnCasoLimiteActivo.Should().Equal(r2.PostulacionId, r3.PostulacionId);
        dto.CuposDisponiblesEnCasoLimite.Should().Be(1);
        dto.PresupuestoDisponibleEnCasoLimite.Should().Be(9000.00m);
        dto.TieneCasoLimiteResuelto.Should().BeFalse();
        dto.TieneDecisionCasoLimiteInvalidaPorDisponibilidad.Should().BeFalse();

        dto.Elementos.Items.Should().OnlyContain(e => !e.CasoLimiteResuelto && e.TipoCasoLimite == null);
    }

    [Fact]
    public void Debe_MapearCamposDeCasoLimiteResuelto_EnConsolidacionProvisionalYConfirmado()
    {
        var r1 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000009111"), 88.00m, _fechaBase);
        var r2 = CrearResultado(Guid.Parse("01968000-0000-7000-8000-000000009112"), 88.00m, _fechaBase.AddMinutes(1));

        var disponibilidad = CrearDisponibilidad(cupos: 1, presupuesto: 5000.00m);
        var montos = new Dictionary<Guid, decimal>
        {
            [r1.PostulacionId] = 1000.00m,
            [r2.PostulacionId] = 1000.00m
        };

        var corteInicial = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2], montos, disponibilidad);
        var fechaDecision = _fechaBase.AddHours(1);
        const string justificacion = "Desempate resuelto por criterio socioeconómico acreditado.";

        ServicioResolucionCasoLimite.AplicarResolucion(
            corteInicial,
            [r1, r2],
            [r2.PostulacionId],
            justificacion,
            fechaDecision,
            _decisorId);

        // ConsolidacionMapeador (ResultadoConsolidadoDto y DetalleResultadoConsolidadoDto)
        var resultadoDto = r2.ADto();
        resultadoDto.TipoCasoLimite.Should().Be("EMPATE_POR_CUPO");
        resultadoDto.SeleccionadoEnCasoLimite.Should().BeTrue();
        resultadoDto.JustificacionDesempateCasoLimite.Should().Be(justificacion);
        resultadoDto.DecididoPorUsuarioId.Should().Be(_decisorId);
        resultadoDto.FechaDecisionCasoLimite.Should().Be(fechaDecision);

        var detalleDto = r1.ADetalleDto([]);
        detalleDto.TipoCasoLimite.Should().Be("EMPATE_POR_CUPO");
        detalleDto.SeleccionadoEnCasoLimite.Should().BeFalse();
        detalleDto.JustificacionDesempateCasoLimite.Should().Be(justificacion);
        detalleDto.DecididoPorUsuarioId.Should().Be(_decisorId);
        detalleDto.FechaDecisionCasoLimite.Should().Be(fechaDecision);

        // RankingMapeador en PROVISIONAL tras resolución
        var corteResuelto = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2], montos, disponibilidad);
        var provisionalDto = corteResuelto.ARankingProvisionalDto(filtroClasificacion: null, pagina: 1, tamano: 10);

        provisionalDto.TieneCasoLimiteActivo.Should().BeFalse();
        provisionalDto.TipoCasoLimiteActivo.Should().BeNull();
        provisionalDto.PostulacionesEnCasoLimiteActivo.Should().BeEmpty();
        provisionalDto.CuposDisponiblesEnCasoLimite.Should().BeNull();
        provisionalDto.PresupuestoDisponibleEnCasoLimite.Should().BeNull();
        provisionalDto.TieneCasoLimiteResuelto.Should().BeTrue();
        provisionalDto.TieneDecisionCasoLimiteInvalidaPorDisponibilidad.Should().BeFalse();

        var ganadorProvisional = provisionalDto.Elementos.Items[0];
        ganadorProvisional.PostulacionId.Should().Be(r2.PostulacionId);
        ganadorProvisional.CasoLimiteResuelto.Should().BeTrue();
        ganadorProvisional.TipoCasoLimite.Should().Be("EMPATE_POR_CUPO");
        ganadorProvisional.SeleccionadoEnCasoLimite.Should().BeTrue();
        ganadorProvisional.JustificacionDesempateCasoLimite.Should().Be(justificacion);
        ganadorProvisional.DecididoPorUsuarioId.Should().Be(_decisorId);
        ganadorProvisional.FechaDecisionCasoLimite.Should().Be(fechaDecision);

        // RankingMapeador en CONFIRMADO preservando metadatos
        var fechaConfirmacion = _fechaBase.AddHours(2);
        r2.ConfirmarRanking(1, 1000.00m, ClasificacionRanking.DentroDeCupo, fechaConfirmacion, _decisorId);
        r1.ConfirmarRanking(2, 1000.00m, ClasificacionRanking.ListaDeEspera, fechaConfirmacion, _decisorId);

        var confirmadoDto = RankingMapeador.ARankingConfirmadoDto(
            _convocatoriaId,
            [r1, r2],
            disponibilidad: disponibilidad,
            filtroClasificacion: null,
            pagina: 1,
            tamano: 10);

        confirmadoDto.TieneCasoLimiteActivo.Should().BeFalse();
        confirmadoDto.TipoCasoLimiteActivo.Should().BeNull();
        confirmadoDto.PostulacionesEnCasoLimiteActivo.Should().BeEmpty();
        confirmadoDto.CuposDisponiblesEnCasoLimite.Should().BeNull();
        confirmadoDto.PresupuestoDisponibleEnCasoLimite.Should().BeNull();
        confirmadoDto.TieneCasoLimiteResuelto.Should().BeTrue();
        confirmadoDto.TieneDecisionCasoLimiteInvalidaPorDisponibilidad.Should().BeFalse();

        confirmadoDto.Elementos.Items[0].PostulacionId.Should().Be(r2.PostulacionId);
        confirmadoDto.Elementos.Items[0].CasoLimiteResuelto.Should().BeTrue();
        confirmadoDto.Elementos.Items[0].TipoCasoLimite.Should().Be("EMPATE_POR_CUPO");
        confirmadoDto.Elementos.Items[0].SeleccionadoEnCasoLimite.Should().BeTrue();
        confirmadoDto.Elementos.Items[0].JustificacionDesempateCasoLimite.Should().Be(justificacion);
        confirmadoDto.Elementos.Items[0].DecididoPorUsuarioId.Should().Be(_decisorId);
        confirmadoDto.Elementos.Items[0].FechaDecisionCasoLimite.Should().Be(fechaDecision);

        confirmadoDto.Elementos.Items[1].PostulacionId.Should().Be(r1.PostulacionId);
        confirmadoDto.Elementos.Items[1].CasoLimiteResuelto.Should().BeTrue();
        confirmadoDto.Elementos.Items[1].SeleccionadoEnCasoLimite.Should().BeFalse();
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
