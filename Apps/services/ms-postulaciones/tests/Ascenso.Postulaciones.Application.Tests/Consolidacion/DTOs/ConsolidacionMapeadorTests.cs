using System.Reflection;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Consolidacion.DTOs;

public sealed class ConsolidacionMapeadorTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000002001");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000002002");
    private static readonly Guid _postulacionId = Guid.Parse("01968000-0000-7000-8000-000000002003");
    private static readonly Guid _jefaturaId = Guid.Parse("01968000-0000-7000-8000-000000002004");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000002005");
    private static readonly Guid _evaluador1Id = Guid.Parse("01968000-0000-7000-8000-000000002006");
    private static readonly Guid _evaluador2Id = Guid.Parse("01968000-0000-7000-8000-000000002007");
    private static readonly Guid _evaluador3Id = Guid.Parse("01968000-0000-7000-8000-000000002008");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaLimite = new(2026, 12, 10, 23, 59, 59, TimeSpan.Zero);

    [Fact]
    public void Debe_MapearResultadoConsolidadoConEstadoEnCodigoEnum_Cuando_SeInvocaADto()
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            _postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: 82.50m,
            diferenciaEntreEvaluadores: 25.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: true);

        var agregado = ResultadoConsolidadoRanking.Crear(calculo, _fechaBase.AddDays(5), UsuarioSistema.Id);
        var fechaRevision = _fechaBase.AddDays(6);
        agregado.MarcarDiscrepanciaRevisada("Discrepancia revisada por el coordinador.", fechaRevision, _coordinadorId);

        var dto = agregado.ADto();

        dto.Id.Should().Be(agregado.Id);
        dto.ConvocatoriaId.Should().Be(_convocatoriaId);
        dto.PostulacionId.Should().Be(_postulacionId);
        dto.ExcepcionEvaluadorUnico.Should().BeFalse();
        dto.JustificacionEvaluadorUnico.Should().BeNull();
        dto.AutorizadorExcepcionId.Should().BeNull();
        dto.CantidadEvaluacionesValidas.Should().Be(2);
        dto.PuntajeConsolidado.Should().Be(82.50m);
        dto.DiferenciaEntreEvaluadores.Should().Be(25.00m);
        dto.UmbralDiscrepanciaAplicado.Should().Be(20.00m);
        dto.AlertaDiscrepancia.Should().BeTrue();
        dto.DiscrepanciaRevisada.Should().BeTrue();
        dto.ComentarioRevisionDiscrepancia.Should().Be("Discrepancia revisada por el coordinador.");
        dto.RevisadoPorUsuarioId.Should().Be(_coordinadorId);
        dto.FechaRevisionDiscrepancia.Should().Be(fechaRevision);
        dto.EstadoConfirmacionRanking.Should().Be("PROVISIONAL");
        dto.FechaConsolidacion.Should().Be(agregado.FechaConsolidacion);
        dto.Version.Should().Be(2);

        var resultadoOperacion = new ResultadoConsolidarPostulacionDto(dto, EsCreacion: true);
        resultadoOperacion.Resultado.Should().Be(dto);
        resultadoOperacion.EsCreacion.Should().BeTrue();
    }

    [Fact]
    public void Debe_MapearDetalleFiltrandoDefensivamenteYOrdenandoPorFechaEnvioYEvaluacionId_Cuando_SeInvocaADetalleDto()
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            _postulacionId,
            excepcionEvaluadorUnico: false,
            justificacionEvaluadorUnico: null,
            autorizadorExcepcionId: null,
            cantidadEvaluacionesValidas: 2,
            puntajeConsolidado: 85.50m,
            diferenciaEntreEvaluadores: 21.00m,
            umbralDiscrepanciaAplicado: 20.00m,
            alertaDiscrepancia: true);

        var agregado = ResultadoConsolidadoRanking.Crear(calculo, _fechaBase.AddDays(7), UsuarioSistema.Id);

        var fechaEnvioTardia = _fechaBase.AddDays(6);
        var fechaEnvioTemprana = _fechaBase.AddDays(5);

        var evalTardia = CrearEvaluacionDefinitiva(_evaluador2Id, 96.00m, fechaEnvioTardia, "Excelente perfil.");
        var evalTemprana = CrearEvaluacionDefinitiva(_evaluador1Id, 75.00m, fechaEnvioTemprana, "Cumple criterios.");
        var borrador = CrearEvaluacionBorrador(_evaluador3Id, 40.00m);
        var invalidada = CrearEvaluacionDefinitiva(_evaluador3Id, 20.00m, fechaEnvioTemprana.AddHours(-1), "Invalidada.");
        FijarEstadoEvaluacion(invalidada, EstadoEvaluacion.InvalidadaPorConflicto);

        var detalle = agregado.ADetalleDto([evalTardia, borrador, invalidada, evalTemprana]);

        detalle.Id.Should().Be(agregado.Id);
        detalle.EstadoConfirmacionRanking.Should().Be("PROVISIONAL");
        detalle.Evaluaciones.Should().HaveCount(2);

        detalle.Evaluaciones[0].EvaluacionId.Should().Be(evalTemprana.Id);
        detalle.Evaluaciones[0].AsignacionEvaluacionId.Should().Be(evalTemprana.AsignacionEvaluacionId);
        detalle.Evaluaciones[0].EvaluadorId.Should().Be(_evaluador1Id);
        detalle.Evaluaciones[0].PuntajeTotalPonderado.Should().Be(75.00m);
        detalle.Evaluaciones[0].FechaEnvio.Should().Be(fechaEnvioTemprana);
        detalle.Evaluaciones[0].ComentariosGenerales.Should().Be("Cumple criterios.");

        detalle.Evaluaciones[1].EvaluacionId.Should().Be(evalTardia.Id);
        detalle.Evaluaciones[1].AsignacionEvaluacionId.Should().Be(evalTardia.AsignacionEvaluacionId);
        detalle.Evaluaciones[1].EvaluadorId.Should().Be(_evaluador2Id);
        detalle.Evaluaciones[1].PuntajeTotalPonderado.Should().Be(96.00m);
        detalle.Evaluaciones[1].FechaEnvio.Should().Be(fechaEnvioTardia);
        detalle.Evaluaciones[1].ComentariosGenerales.Should().Be("Excelente perfil.");
    }

    [Fact]
    public void Debe_DesempatarOrdenPorEvaluacionId_Cuando_DosEvaluacionesTienenMismaFechaEnvio()
    {
        var calculo = new ResultadoCalculoConsolidacion(
            _convocatoriaId,
            _postulacionId,
            false,
            null,
            null,
            2,
            85.00m,
            10.00m,
            20.00m,
            false);
        var agregado = ResultadoConsolidadoRanking.Crear(calculo, _fechaBase.AddDays(7), UsuarioSistema.Id);

        var mismaFecha = _fechaBase.AddDays(5);
        var evalA = CrearEvaluacionDefinitiva(_evaluador1Id, 80.00m, mismaFecha, null);
        var evalB = CrearEvaluacionDefinitiva(_evaluador2Id, 90.00m, mismaFecha, null);

        var esperados = new[] { evalA.Id, evalB.Id }.OrderBy(id => id).ToArray();

        var detalle = agregado.ADetalleDto([evalB, evalA]);

        detalle.Evaluaciones.Select(e => e.EvaluacionId).Should().Equal(esperados);
    }

    [Fact]
    public void Debe_RechazarMapeoDirectoDeNotaIndividual_Cuando_EvaluacionNoEsDefinitiva()
    {
        var borrador = CrearEvaluacionBorrador(_evaluador1Id, 50.00m);

        var accion = () => borrador.ANotaIndividualDto();

        accion.Should().Throw<InvalidOperationException>();
    }

    private static Evaluacion CrearEvaluacionDefinitiva(
        Guid evaluadorId,
        decimal puntaje,
        DateTimeOffset fechaEnvio,
        string? comentarios)
    {
        var (rubrica, criterio) = CrearRubricaEscalaCien();
        var asignacion = CrearAsignacion(evaluadorId);

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(criterio.Id, puntaje, null)],
            comentarios,
            _fechaBase.AddDays(3),
            evaluadorId);

        evaluacion.EnviarDefinitiva(rubrica, asignacion.FechaLimiteEvaluacion, fechaEnvio, evaluadorId);
        return evaluacion;
    }

    private static Evaluacion CrearEvaluacionBorrador(Guid evaluadorId, decimal puntaje)
    {
        var (rubrica, criterio) = CrearRubricaEscalaCien();
        var asignacion = CrearAsignacion(evaluadorId);

        return Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(criterio.Id, puntaje, null)],
            null,
            _fechaBase.AddDays(3),
            evaluadorId);
    }

    private static AsignacionEvaluacion CrearAsignacion(Guid evaluadorId)
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Mapeo",
            _jefaturaId,
            _fechaBase,
            _coordinadorId);

        var miembro = comite.AgregarMiembro(
            evaluadorId,
            TipoEvaluador.Externo,
            _fechaBase.AddDays(1),
            _coordinadorId);

        return AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            _fechaBase.AddDays(2),
            _fechaLimite,
            _coordinadorId);
    }

    private static (RubricaEvaluacion Rubrica, CriterioEvaluacion Criterio) CrearRubricaEscalaCien()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Escala 100",
            _fechaBase,
            _coordinadorId);

        var criterio = rubrica.AgregarCriterio(
            "Global",
            "Puntaje global",
            100.00m,
            0.00m,
            100.00m,
            _fechaBase.AddMinutes(1),
            _coordinadorId);

        rubrica.Publicar(_fechaBase.AddMinutes(2), _coordinadorId);
        return (rubrica, criterio);
    }

    private static void FijarEstadoEvaluacion(Evaluacion evaluacion, EstadoEvaluacion estado)
    {
        var propiedad = typeof(Evaluacion)
            .GetProperty(nameof(Evaluacion.Estado), BindingFlags.Instance | BindingFlags.Public);

        propiedad.Should().NotBeNull();
        propiedad!.SetValue(evaluacion, estado);
    }
}
