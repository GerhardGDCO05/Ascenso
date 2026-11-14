using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Tests.Enums;

public sealed class CodigoEnumTests
{
    public enum EstadoEjemplo
    {
        PendienteVerificacion,
        EnEvaluacion,
        Admitido,
        Matriculado,
        Periodo2026,
        Plan2026Activo
    }

    [Theory]
    [InlineData(EstadoEjemplo.PendienteVerificacion, "PENDIENTE_VERIFICACION")]
    [InlineData(EstadoEjemplo.EnEvaluacion, "EN_EVALUACION")]
    [InlineData(EstadoEjemplo.Admitido, "ADMITIDO")]
    [InlineData(EstadoEjemplo.Matriculado, "MATRICULADO")]
    [InlineData(EstadoEjemplo.Periodo2026, "PERIODO2026")]
    [InlineData(EstadoEjemplo.Plan2026Activo, "PLAN2026_ACTIVO")]
    public void Debe_ConvertirANombreEnMayusculasConGuiones_Cuando_SeSolicitaElCodigo(
        EstadoEjemplo valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Theory]
    [InlineData("PENDIENTE_VERIFICACION", EstadoEjemplo.PendienteVerificacion)]
    [InlineData("en_evaluacion", EstadoEjemplo.EnEvaluacion)]
    [InlineData(" Admitido ", EstadoEjemplo.Admitido)]
    [InlineData("PERIODO2026", EstadoEjemplo.Periodo2026)]
    [InlineData("PLAN2026_ACTIVO", EstadoEjemplo.Plan2026Activo)]
    public void Debe_ConvertirDesdeElCodigo_Cuando_ElCodigoEsValido(string codigo, EstadoEjemplo esperado)
    {
        Assert.Equal(esperado, CodigoEnum.DesdeCodigo<EstadoEjemplo>(codigo));
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorreTodosLosValores()
    {
        foreach (var valor in Enum.GetValues<EstadoEjemplo>())
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<EstadoEjemplo>(valor.ACodigo()));
        }
    }

    [Fact]
    public void Debe_RechazarCodigosInvalidos_Cuando_NoCorrespondenAUnValor()
    {
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<EstadoEjemplo>("NO_EXISTE"));
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<EstadoEjemplo>(" "));
    }

    [Fact]
    public void Debe_RechazarValoresNoDefinidos_Cuando_SeSolicitaElCodigo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((EstadoEjemplo)99).ACodigo());
    }
}
