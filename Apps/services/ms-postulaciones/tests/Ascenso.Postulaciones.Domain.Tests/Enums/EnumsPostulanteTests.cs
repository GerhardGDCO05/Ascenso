using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Tests.Enums;

public sealed class EnumsPostulanteTests
{
    [Theory]
    [InlineData(CondicionAcademicaIngreso.Admitido, "ADMITIDO")]
    [InlineData(CondicionAcademicaIngreso.Matriculado, "MATRICULADO")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaLaCondicionAcademica(
        CondicionAcademicaIngreso valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Theory]
    [InlineData(EstadoPerfil.Incompleto, "INCOMPLETO")]
    [InlineData(EstadoPerfil.Completo, "COMPLETO")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaElEstadoDelPerfil(
        EstadoPerfil valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeCondicionAcademica()
    {
        foreach (var valor in Enum.GetValues<CondicionAcademicaIngreso>())
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<CondicionAcademicaIngreso>(valor.ACodigo()));
        }
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeEstadoPerfil()
    {
        foreach (var valor in Enum.GetValues<EstadoPerfil>())
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<EstadoPerfil>(valor.ACodigo()));
        }
    }

    [Fact]
    public void Debe_RechazarCodigosInvalidos_Cuando_NoCorrespondenALosEnumsDelPostulante()
    {
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<CondicionAcademicaIngreso>("EGRESADO"));
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<EstadoPerfil>("PENDIENTE"));
    }
}
