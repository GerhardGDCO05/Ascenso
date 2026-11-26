using Ascenso.Postulaciones.Application.Rubricas.DTOs;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Rubricas.DTOs;

public sealed class RubricaMapeadorTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000101");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000102");
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000103");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_MapearRubricaEnBorradorSinCriterios_Cuando_SeConvierteADto()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Beca Excelencia 2026-II",
            _fechaCreacion,
            _usuarioId);

        var dto = rubrica.ADto();

        dto.Id.Should().Be(rubrica.Id);
        dto.ProgramaBecaId.Should().Be(_programaBecaId);
        dto.ConvocatoriaId.Should().Be(_convocatoriaId);
        dto.Nombre.Should().Be("Rúbrica Beca Excelencia 2026-II");
        dto.Estado.Should().Be("BORRADOR");
        dto.FechaPublicacion.Should().BeNull();
        dto.MotivoBloqueo.Should().BeNull();
        dto.FechaBloqueo.Should().BeNull();
        dto.SoloLectura.Should().BeFalse();
        dto.Criterios.Should().BeEmpty();
        dto.SumaPesos.Should().Be(0.00m);
        dto.DiferenciaPara100.Should().Be(100.00m);
    }

    [Fact]
    public void Debe_MapearRubricaConCriteriosOrdenadosSumaYDiferencia_Cuando_TieneCriterios()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Socioeconómica",
            _fechaCreacion,
            _usuarioId);

        var primero = rubrica.AgregarCriterio(
            "Criterio 1",
            "Descripción 1",
            30.00m,
            0.00m,
            10.00m,
            _fechaCreacion.AddMinutes(1),
            _usuarioId);

        var segundo = rubrica.AgregarCriterio(
            "Criterio 2",
            "Descripción 2",
            20.00m,
            1.00m,
            20.00m,
            _fechaCreacion.AddMinutes(2),
            _usuarioId);

        var tercero = rubrica.AgregarCriterio(
            "Criterio 3",
            "Descripción 3",
            40.50m,
            0.00m,
            100.00m,
            _fechaCreacion.AddMinutes(3),
            _usuarioId);

        rubrica.EliminarCriterio(primero.Id, _fechaCreacion.AddMinutes(4), _usuarioId);

        var dto = rubrica.ADto();

        dto.Estado.Should().Be("BORRADOR");
        dto.SumaPesos.Should().Be(60.50m);
        dto.DiferenciaPara100.Should().Be(39.50m);
        dto.Criterios.Should().HaveCount(2);

        dto.Criterios[0].Id.Should().Be(segundo.Id);
        dto.Criterios[0].Nombre.Should().Be("Criterio 2");
        dto.Criterios[0].Descripcion.Should().Be("Descripción 2");
        dto.Criterios[0].PesoPorcentual.Should().Be(20.00m);
        dto.Criterios[0].PuntajeMinimo.Should().Be(1.00m);
        dto.Criterios[0].PuntajeMaximo.Should().Be(20.00m);
        dto.Criterios[0].Orden.Should().Be(2);

        dto.Criterios[1].Id.Should().Be(tercero.Id);
        dto.Criterios[1].Nombre.Should().Be("Criterio 3");
        dto.Criterios[1].Descripcion.Should().Be("Descripción 3");
        dto.Criterios[1].PesoPorcentual.Should().Be(40.50m);
        dto.Criterios[1].PuntajeMinimo.Should().Be(0.00m);
        dto.Criterios[1].PuntajeMaximo.Should().Be(100.00m);
        dto.Criterios[1].Orden.Should().Be(3);
    }

    [Fact]
    public void Debe_MapearEstadoYFechaPublicacion_Cuando_LaRubricaEstaPublicada()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Publicada",
            _fechaCreacion,
            _usuarioId);

        rubrica.AgregarCriterio(
            "Mérito académico",
            "Promedio ponderado acumulado",
            100.00m,
            0.00m,
            20.00m,
            _fechaCreacion.AddMinutes(5),
            _usuarioId);

        var fechaPublicacion = _fechaCreacion.AddHours(1);
        rubrica.Publicar(fechaPublicacion, _usuarioId);

        var dto = rubrica.ADto();

        dto.Estado.Should().Be("PUBLICADA");
        dto.FechaPublicacion.Should().Be(fechaPublicacion);
        dto.MotivoBloqueo.Should().BeNull();
        dto.FechaBloqueo.Should().BeNull();
        dto.SoloLectura.Should().BeTrue();
        dto.SumaPesos.Should().Be(100.00m);
        dto.DiferenciaPara100.Should().Be(0.00m);
    }

    [Theory]
    [InlineData(MotivoBloqueoRubrica.InicioEvaluacion, "INICIO_EVALUACION")]
    [InlineData(MotivoBloqueoRubrica.PrimeraCalificacion, "PRIMERA_CALIFICACION")]
    public void Debe_MapearMotivoFechaBloqueoYSoloLectura_Cuando_LaRubricaEstaBloqueada(
        MotivoBloqueoRubrica motivo,
        string codigoMotivoEsperado)
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Bloqueada",
            _fechaCreacion,
            _usuarioId);

        rubrica.AgregarCriterio(
            "Mérito académico",
            "Promedio ponderado acumulado",
            100.00m,
            0.00m,
            20.00m,
            _fechaCreacion.AddMinutes(5),
            _usuarioId);

        var fechaPublicacion = _fechaCreacion.AddHours(1);
        rubrica.Publicar(fechaPublicacion, _usuarioId);

        var fechaBloqueo = _fechaCreacion.AddHours(2);
        rubrica.Bloquear(motivo, fechaBloqueo, UsuarioSistema.Id);

        var dto = rubrica.ADto();

        dto.Estado.Should().Be("BLOQUEADA");
        dto.FechaPublicacion.Should().Be(fechaPublicacion);
        dto.MotivoBloqueo.Should().Be(codigoMotivoEsperado);
        dto.FechaBloqueo.Should().Be(fechaBloqueo);
        dto.SoloLectura.Should().BeTrue();
        dto.SumaPesos.Should().Be(100.00m);
        dto.DiferenciaPara100.Should().Be(0.00m);
    }
}
