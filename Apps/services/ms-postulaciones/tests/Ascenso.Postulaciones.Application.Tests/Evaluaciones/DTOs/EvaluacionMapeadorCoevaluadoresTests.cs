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

public sealed class EvaluacionMapeadorCoevaluadoresTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968200-0000-7000-8000-000000000101");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968200-0000-7000-8000-000000000102");
    private static readonly Guid _jefaturaId = Guid.Parse("01968200-0000-7000-8000-000000000103");
    private static readonly Guid _coordinadorId = Guid.Parse("01968200-0000-7000-8000-000000000104");
    private static readonly Guid _evaluadorId = Guid.Parse("01968200-0000-7000-8000-000000000105");
    private static readonly Guid _postulacionId = Guid.Parse("01968200-0000-7000-8000-000000000106");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_MapearEvaluacionDefinitivaSinIdentidadDeParNiPostulanteYConCalificacionesOrdenadas_Cuando_SeInvocaACoevaluadorDto()
    {
        var (asignacion, rubrica, c1, c2) = CrearContexto();
        var criterioMayor = c1.Id.CompareTo(c2.Id) > 0 ? c1 : c2;
        var criterioMenor = c1.Id.CompareTo(c2.Id) > 0 ? c2 : c1;

        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [
                new DatosCalificacionCriterio(criterioMayor.Id, 10.00m, "Mayor"),
                new DatosCalificacionCriterio(criterioMenor.Id, 6.00m, "Menor")
            ],
            "Evaluación definitiva consistente.",
            _fechaBase.AddDays(3),
            _evaluadorId);
        var fechaEnvio = _fechaBase.AddDays(4);
        evaluacion.EnviarDefinitiva(rubrica, asignacion.FechaLimiteEvaluacion, fechaEnvio, _evaluadorId);

        var dto = evaluacion.ACoevaluadorDto();
        var contenedor = new EvaluacionesCoevaluadoresPostulacionDto(
            _postulacionId,
            _convocatoriaId,
            _fechaBase.AddDays(10),
            [dto]);

        dto.EvaluacionId.Should().Be(evaluacion.Id);
        dto.RubricaEvaluacionId.Should().Be(rubrica.Id);
        dto.Estado.Should().Be("ENVIADA_DEFINITIVA");
        dto.FechaEnvio.Should().Be(fechaEnvio);
        dto.PuntajeTotalPonderado.Should().Be(80.00m);
        dto.ComentariosGenerales.Should().Be("Evaluación definitiva consistente.");
        dto.Calificaciones.Select(c => c.CriterioEvaluacionId)
            .Should().ContainInOrder(criterioMenor.Id, criterioMayor.Id);

        contenedor.PostulacionId.Should().Be(_postulacionId);
        contenedor.ConvocatoriaId.Should().Be(_convocatoriaId);
        contenedor.FechaCierreRevision.Should().Be(_fechaBase.AddDays(10));
        contenedor.Evaluaciones.Should().ContainSingle().Which.Should().Be(dto);

        var propiedades = typeof(EvaluacionCoevaluadorDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        propiedades.Should().BeEquivalentTo(
            [
                nameof(EvaluacionCoevaluadorDto.EvaluacionId),
                nameof(EvaluacionCoevaluadorDto.RubricaEvaluacionId),
                nameof(EvaluacionCoevaluadorDto.Estado),
                nameof(EvaluacionCoevaluadorDto.FechaEnvio),
                nameof(EvaluacionCoevaluadorDto.PuntajeTotalPonderado),
                nameof(EvaluacionCoevaluadorDto.ComentariosGenerales),
                nameof(EvaluacionCoevaluadorDto.Calificaciones)
            ]);
        propiedades.Should().NotContain("EvaluadorId");
        propiedades.Should().NotContain("AsignacionEvaluacionId");
        propiedades.Should().NotContain("MotivoInvalidacion");
        propiedades.Should().NotContain("Version");
        propiedades.Should().NotContain(p => p.Contains("Postulante", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Debe_LanzarArgumentNullException_Cuando_EvaluacionEsNulaAlInvocarACoevaluadorDto()
    {
        Evaluacion evaluacion = null!;

        var accion = () => evaluacion.ACoevaluadorDto();

        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Debe_RechazarEvaluacionEnBorrador_Cuando_SeInvocaACoevaluadorDto()
    {
        var (asignacion, rubrica, c1, _) = CrearContexto();
        var borrador = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [new DatosCalificacionCriterio(c1.Id, 8.00m, "Parcial")],
            "Borrador privado",
            _fechaBase.AddDays(3),
            _evaluadorId);

        var accion = () => borrador.ACoevaluadorDto();

        accion.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Debe_RechazarEvaluacionInvalidadaPorConflicto_Cuando_SeInvocaACoevaluadorDto()
    {
        var (asignacion, rubrica, c1, c2) = CrearContexto();
        var evaluacion = Evaluacion.CrearBorrador(
            asignacion,
            rubrica,
            [
                new DatosCalificacionCriterio(c1.Id, 8.00m, "C1"),
                new DatosCalificacionCriterio(c2.Id, 9.00m, "C2")
            ],
            "Enviada luego invalidada",
            _fechaBase.AddDays(3),
            _evaluadorId);
        evaluacion.EnviarDefinitiva(rubrica, asignacion.FechaLimiteEvaluacion, _fechaBase.AddDays(4), _evaluadorId);
        FijarEstadoEvaluacion(evaluacion, EstadoEvaluacion.InvalidadaPorConflicto);

        var accion = () => evaluacion.ACoevaluadorDto();

        accion.Should().Throw<InvalidOperationException>();
    }

    private static void FijarEstadoEvaluacion(Evaluacion evaluacion, EstadoEvaluacion estado)
    {
        var propiedad = typeof(Evaluacion)
            .GetProperty(nameof(Evaluacion.Estado), BindingFlags.Instance | BindingFlags.Public);

        propiedad.Should().NotBeNull();
        propiedad!.SetValue(evaluacion, estado);
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
