using System.Reflection;
using Ascenso.Postulaciones.Application.Evaluaciones.DTOs;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Evaluaciones.DTOs;

public sealed class EvaluacionMapeadorTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968100-0000-7000-8000-000000000101");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968100-0000-7000-8000-000000000102");
    private static readonly Guid _jefaturaId = Guid.Parse("01968100-0000-7000-8000-000000000103");
    private static readonly Guid _coordinadorId = Guid.Parse("01968100-0000-7000-8000-000000000104");
    private static readonly Guid _evaluadorId = Guid.Parse("01968100-0000-7000-8000-000000000105");
    private static readonly Guid _postulacionId = Guid.Parse("01968100-0000-7000-8000-000000000106");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_MapearEvaluacionBorradorConCalificacionesOrdenadasYVistaCiega_Cuando_SeInvocaADto()
    {
        var (asignacion, rubrica, c1, c2) = CrearContexto();
        var criterioMayor = c1.Id.CompareTo(c2.Id) > 0 ? c1 : c2;
        var criterioMenor = c1.Id.CompareTo(c2.Id) > 0 ? c2 : c1;

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [
                new DatosCalificacionCriterio(criterioMayor.Id, 8.50m, "Mayor"),
                new DatosCalificacionCriterio(criterioMenor.Id, 9.00m, "Menor")
            ],
            "Comentarios borrador",
            _fechaBase.AddDays(3),
            _evaluadorId);

        var dto = evaluacion.ADto();

        dto.Id.Should().Be(evaluacion.Id);
        dto.AsignacionEvaluacionId.Should().Be(asignacion.Id);
        dto.PostulacionId.Should().Be(_postulacionId);
        dto.ConvocatoriaId.Should().Be(_convocatoriaId);
        dto.RubricaEvaluacionId.Should().Be(rubrica.Id);
        dto.Estado.Should().Be("BORRADOR");
        dto.FechaInicio.Should().Be(evaluacion.FechaInicio);
        dto.FechaEnvio.Should().BeNull();
        dto.PuntajeTotalPonderado.Should().BeNull();
        dto.ComentariosGenerales.Should().Be("Comentarios borrador");
        dto.Version.Should().Be(1);
        dto.Calificaciones.Select(c => c.CriterioEvaluacionId)
            .Should().ContainInOrder(criterioMenor.Id, criterioMayor.Id);

        // Separación de vista de evaluador: EvaluacionDto no expone EvaluadorId ni MotivoInvalidacion.
        var propiedadesEvaluador = typeof(EvaluacionDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name);
        propiedadesEvaluador.Should().NotContain("EvaluadorId");
        propiedadesEvaluador.Should().NotContain("MotivoInvalidacion");
    }

    [Fact]
    public void Debe_MapearEvaluacionEnviadaConPuntajeYVistaSupervision_Cuando_SeInvocanMapeadores()
    {
        var (asignacion, rubrica, c1, c2) = CrearContexto();
        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [
                new DatosCalificacionCriterio(c1.Id, 10.00m, "C1"),
                new DatosCalificacionCriterio(c2.Id, 5.00m, "C2")
            ],
            "Observación final",
            _fechaBase.AddDays(3),
            _evaluadorId);
        evaluacion.EnviarDefinitiva(rubrica, asignacion.FechaLimiteEvaluacion, _fechaBase.AddDays(4), _evaluadorId);

        var dto = evaluacion.ADto();
        var resumen = evaluacion.AResumenMiEvaluacionDto();
        var supervision = evaluacion.ASupervisionDto();
        var resultadoBorrador = new ResultadoGuardarBorradorEvaluacionDto(dto, true);
        var listadoPostulacion = new EvaluacionesPostulacionDto(_postulacionId, [supervision]);

        dto.Estado.Should().Be("ENVIADA_DEFINITIVA");
        dto.PuntajeTotalPonderado.Should().Be(75.00m);
        dto.FechaEnvio.Should().Be(evaluacion.FechaEnvio);

        resumen.EvaluacionId.Should().Be(evaluacion.Id);
        resumen.AsignacionEvaluacionId.Should().Be(asignacion.Id);
        resumen.PostulacionId.Should().Be(_postulacionId);
        resumen.ConvocatoriaId.Should().Be(_convocatoriaId);
        resumen.RubricaEvaluacionId.Should().Be(rubrica.Id);
        resumen.Estado.Should().Be("ENVIADA_DEFINITIVA");
        resumen.FechaInicio.Should().Be(evaluacion.FechaInicio);
        resumen.FechaEnvio.Should().Be(evaluacion.FechaEnvio);
        resumen.PuntajeTotalPonderado.Should().Be(75.00m);
        resumen.CantidadCriteriosCalificados.Should().Be(2);
        resumen.Version.Should().Be(2);

        supervision.Id.Should().Be(evaluacion.Id);
        supervision.EvaluadorId.Should().Be(_evaluadorId);
        supervision.Estado.Should().Be("ENVIADA_DEFINITIVA");
        supervision.PuntajeTotalPonderado.Should().Be(75.00m);
        supervision.MotivoInvalidacion.Should().BeNull();
        supervision.Calificaciones.Should().HaveCount(2);

        resultadoBorrador.EsCreacion.Should().BeTrue();
        resultadoBorrador.Evaluacion.Should().Be(dto);
        listadoPostulacion.PostulacionId.Should().Be(_postulacionId);
        listadoPostulacion.Evaluaciones.Should().ContainSingle().Which.Should().Be(supervision);
    }

    private static (
        AsignacionEvaluacion Asignacion,
        RubricaEvaluacion Rubrica,
        CriterioEvaluacion C1,
        CriterioEvaluacion C2) CrearContexto()
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Excelencia",
            _jefaturaId,
            _fechaBase,
            _coordinadorId);
        var miembro = comite.AgregarMiembro(
            _evaluadorId,
            TipoEvaluador.Externo,
            _fechaBase.AddDays(1),
            _coordinadorId);
        var asignacion = AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            _fechaBase.AddDays(2),
            _fechaBase.AddDays(15),
            _coordinadorId);

        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Excelencia",
            _fechaBase,
            _coordinadorId);
        var c1 = rubrica.AgregarCriterio("C1", "Desc 1", 50.00m, 0.00m, 10.00m, _fechaBase.AddMinutes(1), _coordinadorId);
        var c2 = rubrica.AgregarCriterio("C2", "Desc 2", 50.00m, 0.00m, 10.00m, _fechaBase.AddMinutes(2), _coordinadorId);
        rubrica.Publicar(_fechaBase.AddMinutes(3), _coordinadorId);

        return (asignacion, rubrica, c1, c2);
    }
}
