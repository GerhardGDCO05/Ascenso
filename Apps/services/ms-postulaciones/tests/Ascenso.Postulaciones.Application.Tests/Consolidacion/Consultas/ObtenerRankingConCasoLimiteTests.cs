using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerRankingConvocatoria;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Consolidacion.Servicios;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Consolidacion.Consultas;

public sealed class ObtenerRankingConCasoLimiteTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000012001");
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000012002");
    private static readonly Guid _postulacion1Id = Guid.Parse("01968000-0000-7000-8000-000000012003");
    private static readonly Guid _postulacion2Id = Guid.Parse("01968000-0000-7000-8000-000000012004");
    private static readonly Guid _postulacion3Id = Guid.Parse("01968000-0000-7000-8000-000000012005");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000012006");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    private readonly IRankingConvocatoriaRepository _rankingRepository = Substitute.For<IRankingConvocatoriaRepository>();
    private readonly IDisponibilidadRankingClient _disponibilidadClient = Substitute.For<IDisponibilidadRankingClient>();
    private readonly IProveedorMontoEstimadoBeca _proveedorMonto = Substitute.For<IProveedorMontoEstimadoBeca>();
    private readonly ObtenerRankingConvocatoriaQueryValidator _validator = new();

    private ObtenerRankingConvocatoriaQueryHandler CrearHandler()
        => new(_rankingRepository, _disponibilidadClient, _proveedorMonto, _validator);

    [Fact]
    public async Task Debe_ExponerCasoLimitePresupuestarioActivo_EnConsultaProvisionalSinDecision()
    {
        var r1 = CrearResultado(_postulacion1Id, 94.00m);
        var r2 = CrearResultado(_postulacion2Id, 89.00m, minutosDesfase: 1);
        var r3 = CrearResultado(_postulacion3Id, 80.00m, minutosDesfase: 2);
        var disponibilidad = CrearDisponibilidad(cupos: 5, presupuesto: 2500.00m);
        var montos = new Dictionary<Guid, decimal>
        {
            [_postulacion1Id] = 2000.00m,
            [_postulacion2Id] = 2000.00m,
            [_postulacion3Id] = 400.00m
        };

        _rankingRepository.ListarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1, r2, r3]);
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(disponibilidad);
        _proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                _convocatoriaId,
                _programaBecaId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(montos);

        var handler = CrearHandler();
        var dto = await handler.HandleAsync(new ObtenerRankingConvocatoriaQuery(_convocatoriaId));

        dto.EstadoRanking.Should().Be("PROVISIONAL");
        dto.TieneEmpateEnLineaDeCorte.Should().BeFalse();
        dto.TieneCasoLimiteActivo.Should().BeTrue();
        dto.TipoCasoLimiteActivo.Should().Be("CASO_LIMITE_PRESUPUESTARIO");
        dto.PresupuestoDisponibleEnCasoLimite.Should().Be(500.00m);
        dto.PostulacionesEnCasoLimiteActivo.Should().Equal(_postulacion3Id);
        dto.TieneCasoLimiteResuelto.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_ExponerMetadatosDeCasoLimiteResuelto_EnConsultaProvisionalYConfirmada()
    {
        var r1 = CrearResultado(_postulacion1Id, 88.00m);
        var r2 = CrearResultado(_postulacion2Id, 88.00m, minutosDesfase: 1);
        var disponibilidad = CrearDisponibilidad(cupos: 1, presupuesto: 5000.00m);
        var montos = new Dictionary<Guid, decimal>
        {
            [_postulacion1Id] = 1000.00m,
            [_postulacion2Id] = 1000.00m
        };

        var corte = ServicioCalculoRankingCorte.Calcular(_convocatoriaId, [r1, r2], montos, disponibilidad);
        const string justificacion = "Desempate resuelto con criterio documentado en comité.";
        ServicioResolucionCasoLimite.AplicarResolucion(
            corte,
            [r1, r2],
            [_postulacion2Id],
            justificacion,
            _fechaBase.AddHours(1),
            _coordinadorId);

        _rankingRepository.ListarPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns([r1, r2]);
        _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns(disponibilidad);
        _proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
                _convocatoriaId,
                _programaBecaId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(montos);

        var handler = CrearHandler();
        var provisional = await handler.HandleAsync(new ObtenerRankingConvocatoriaQuery(_convocatoriaId));

        provisional.TieneEmpateEnLineaDeCorte.Should().BeFalse();
        provisional.TieneCasoLimiteActivo.Should().BeFalse();
        provisional.TieneCasoLimiteResuelto.Should().BeTrue();
        provisional.Elementos.Items[0].PostulacionId.Should().Be(_postulacion2Id);
        provisional.Elementos.Items[0].TipoCasoLimite.Should().Be("EMPATE_POR_CUPO");
        provisional.Elementos.Items[0].SeleccionadoEnCasoLimite.Should().BeTrue();

        // Confirmado: no vuelve a llamar a MS-PRG y preserva los metadatos auditados
        r2.ConfirmarRanking(1, 1000.00m, ClasificacionRanking.DentroDeCupo, _fechaBase.AddHours(2), _coordinadorId);
        r1.ConfirmarRanking(2, 1000.00m, ClasificacionRanking.ListaDeEspera, _fechaBase.AddHours(2), _coordinadorId);
        _disponibilidadClient.ClearReceivedCalls();

        var confirmado = await handler.HandleAsync(new ObtenerRankingConvocatoriaQuery(_convocatoriaId));

        confirmado.EstadoRanking.Should().Be("CONFIRMADO");
        confirmado.TieneCasoLimiteActivo.Should().BeFalse();
        confirmado.TieneCasoLimiteResuelto.Should().BeTrue();
        confirmado.Elementos.Items[0].PostulacionId.Should().Be(_postulacion2Id);
        confirmado.Elementos.Items[0].CasoLimiteResuelto.Should().BeTrue();
        confirmado.Elementos.Items[0].JustificacionDesempateCasoLimite.Should().Be(justificacion);
        confirmado.Elementos.Items[1].PostulacionId.Should().Be(_postulacion1Id);
        confirmado.Elementos.Items[1].SeleccionadoEnCasoLimite.Should().BeFalse();

        await _disponibilidadClient.DidNotReceiveWithAnyArgs()
            .ObtenerDisponibilidadPorConvocatoriaAsync(default, default);
    }

    private static DisponibilidadProgramaRanking CrearDisponibilidad(int cupos, decimal presupuesto)
        => new(_convocatoriaId, _programaBecaId, cupos, cupos, presupuesto, 0.00m, "USD");

    private static ResultadoConsolidadoRanking CrearResultado(
        Guid postulacionId,
        decimal puntaje,
        int minutosDesfase = 0)
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

        return ResultadoConsolidadoRanking.Crear(
            calculo,
            _fechaBase.AddMinutes(minutosDesfase),
            UsuarioSistema.Id);
    }
}
