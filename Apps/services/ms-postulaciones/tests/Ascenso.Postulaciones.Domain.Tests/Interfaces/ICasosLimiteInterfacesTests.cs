using Ascenso.Postulaciones.Domain.Interfaces;

namespace Ascenso.Postulaciones.Domain.Tests.Interfaces;

public sealed class ICasosLimiteInterfacesTests
{
    [Fact]
    public void Debe_ExponerMetodoAsincronoConCancellationToken_Cuando_SeInspeccionaIConsultaJefaturaComiteRepository()
    {
        var tipo = typeof(IConsultaJefaturaComiteRepository);
        Assert.True(tipo.IsInterface);

        var metodos = tipo.GetMethods();
        var metodo = Assert.Single(
            metodos,
            m => m.Name == nameof(IConsultaJefaturaComiteRepository.EsJefaturaActivaEnConvocatoriaAsync));

        Assert.EndsWith("Async", metodo.Name, StringComparison.Ordinal);
        Assert.Equal(typeof(Task<bool>), metodo.ReturnType);

        var parametros = metodo.GetParameters();
        Assert.Equal(3, parametros.Length);
        Assert.Equal(typeof(Guid), parametros[0].ParameterType);
        Assert.Equal(typeof(Guid), parametros[1].ParameterType);
        Assert.Equal(typeof(CancellationToken), parametros[2].ParameterType);
        Assert.True(parametros[2].HasDefaultValue);
    }

    [Fact]
    public void Debe_ExponerMetodoAsincronoConCancellationToken_Cuando_SeInspeccionaIVerificadorConflictoInteresDecisor()
    {
        var tipo = typeof(IVerificadorConflictoInteresDecisor);
        Assert.True(tipo.IsInterface);

        var metodos = tipo.GetMethods();
        var metodo = Assert.Single(
            metodos,
            m => m.Name == nameof(IVerificadorConflictoInteresDecisor.TieneConflictoEnPostulacionesAsync));

        Assert.EndsWith("Async", metodo.Name, StringComparison.Ordinal);
        Assert.Equal(typeof(Task<bool>), metodo.ReturnType);

        var parametros = metodo.GetParameters();
        Assert.Equal(4, parametros.Length);
        Assert.Equal(typeof(Guid), parametros[0].ParameterType);
        Assert.Equal(typeof(Guid), parametros[1].ParameterType);
        Assert.Equal(typeof(IReadOnlyCollection<Guid>), parametros[2].ParameterType);
        Assert.Equal(typeof(CancellationToken), parametros[3].ParameterType);
        Assert.True(parametros[3].HasDefaultValue);
    }
}
