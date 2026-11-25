using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerAsignacionesPostulacion;
using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerCargaEvaluadoresComite;
using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerMisAsignaciones;
using Ascenso.Postulaciones.Domain.Enums;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Asignaciones.Consultas;

public sealed class ConsultasAsignacionesValidatorsTests
{
    [Fact]
    public void Debe_ValidarObtenerAsignacionesPostulacionQuery()
    {
        var validator = new ObtenerAsignacionesPostulacionQueryValidator();

        validator.Validate(new ObtenerAsignacionesPostulacionQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ObtenerAsignacionesPostulacionQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Debe_ValidarObtenerCargaEvaluadoresComiteQuery()
    {
        var validator = new ObtenerCargaEvaluadoresComiteQueryValidator();

        validator.Validate(new ObtenerCargaEvaluadoresComiteQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ObtenerCargaEvaluadoresComiteQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Debe_AceptarObtenerMisAsignacionesQueryValida()
    {
        var validator = new ObtenerMisAsignacionesQueryValidator();

        validator.Validate(new ObtenerMisAsignacionesQuery(null, 1, 20)).IsValid.Should().BeTrue();
        validator.Validate(new ObtenerMisAsignacionesQuery(EstadoAsignacionEvaluacion.Pendiente, 2, 100))
            .IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Debe_RechazarPaginaMenorAUno_Cuando_SeValidaObtenerMisAsignacionesQuery(int paginaInvalida)
    {
        var validator = new ObtenerMisAsignacionesQueryValidator();

        validator.Validate(new ObtenerMisAsignacionesQuery(null, paginaInvalida, 20))
            .IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(0)]
    [InlineData(101)]
    public void Debe_RechazarTamanoFueraDeRango_Cuando_SeValidaObtenerMisAsignacionesQuery(int tamanoInvalido)
    {
        var validator = new ObtenerMisAsignacionesQueryValidator();

        validator.Validate(new ObtenerMisAsignacionesQuery(null, 1, tamanoInvalido))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Debe_RechazarEstadoIndefinido_Cuando_SeValidaObtenerMisAsignacionesQuery()
    {
        var validator = new ObtenerMisAsignacionesQueryValidator();

        validator.Validate(new ObtenerMisAsignacionesQuery((EstadoAsignacionEvaluacion)999, 1, 20))
            .IsValid.Should().BeFalse();
    }
}
