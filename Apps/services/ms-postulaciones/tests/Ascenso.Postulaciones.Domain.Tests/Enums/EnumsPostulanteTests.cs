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

    [Theory]
    [InlineData(EstadoCivil.Soltero, "SOLTERO")]
    [InlineData(EstadoCivil.Casado, "CASADO")]
    [InlineData(EstadoCivil.Divorciado, "DIVORCIADO")]
    [InlineData(EstadoCivil.Viudo, "VIUDO")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaElEstadoCivil(
        EstadoCivil valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Theory]
    [InlineData(Parentesco.Padre, "PADRE")]
    [InlineData(Parentesco.Madre, "MADRE")]
    [InlineData(Parentesco.Hermano, "HERMANO")]
    [InlineData(Parentesco.Hijo, "HIJO")]
    [InlineData(Parentesco.Pareja, "PAREJA")]
    [InlineData(Parentesco.Otro, "OTRO")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaElParentesco(
        Parentesco valor,
        string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, valor.ACodigo());
    }

    [Theory]
    [InlineData(Ocupacion.Empleado, "EMPLEADO")]
    [InlineData(Ocupacion.Independiente, "INDEPENDIENTE")]
    [InlineData(Ocupacion.Estudiante, "ESTUDIANTE")]
    [InlineData(Ocupacion.LaboresHogar, "LABORES_HOGAR")]
    [InlineData(Ocupacion.Jubilado, "JUBILADO")]
    [InlineData(Ocupacion.NoTrabaja, "NO_TRABAJA")]
    [InlineData(Ocupacion.Otro, "OTRO")]
    public void Debe_ConvertirElCodigo_Cuando_SeSolicitaLaOcupacion(
        Ocupacion valor,
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
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeEstadoCivil()
    {
        foreach (var valor in Enum.GetValues<EstadoCivil>())
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<EstadoCivil>(valor.ACodigo()));
        }
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeParentesco()
    {
        foreach (var valor in Enum.GetValues<Parentesco>())
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<Parentesco>(valor.ACodigo()));
        }
    }

    [Fact]
    public void Debe_SerInversa_Cuando_SeRecorrenLosValoresDeOcupacion()
    {
        foreach (var valor in Enum.GetValues<Ocupacion>())
        {
            Assert.Equal(valor, CodigoEnum.DesdeCodigo<Ocupacion>(valor.ACodigo()));
        }
    }

    [Fact]
    public void Debe_RechazarCodigosInvalidos_Cuando_NoCorrespondenALosEnumsDelPostulante()
    {
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<CondicionAcademicaIngreso>("EGRESADO"));
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<EstadoPerfil>("PENDIENTE"));
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<EstadoCivil>("CONVIVIENTE"));
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<Parentesco>("TIO"));
        Assert.Throws<ArgumentException>(() => CodigoEnum.DesdeCodigo<Ocupacion>("DESEMPLEADO"));
    }
}
