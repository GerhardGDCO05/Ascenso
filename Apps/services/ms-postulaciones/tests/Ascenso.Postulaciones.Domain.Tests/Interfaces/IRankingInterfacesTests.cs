using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Interfaces;

namespace Ascenso.Postulaciones.Domain.Tests.Interfaces;

public sealed class IRankingInterfacesTests
{
    [Fact]
    public void Debe_ExponerMetodosAsincronosConCancellationToken_Cuando_SeInspeccionaIRankingConvocatoriaRepository()
    {
        var metodos = typeof(IRankingConvocatoriaRepository).GetMethods();
        var nombresEsperados = new[]
        {
            nameof(IRankingConvocatoriaRepository.ListarPorConvocatoriaAsync),
            nameof(IRankingConvocatoriaRepository.ListarParaConfirmarPorConvocatoriaAsync)
        };

        Assert.Equal(nombresEsperados.Length, metodos.Length);

        foreach (var nombre in nombresEsperados)
        {
            var metodo = Assert.Single(metodos, m => m.Name == nombre);
            Assert.Equal(typeof(Task<IReadOnlyList<ResultadoConsolidadoRanking>>), metodo.ReturnType);

            var parametros = metodo.GetParameters();
            Assert.Equal(2, parametros.Length);
            Assert.Equal(typeof(Guid), parametros[0].ParameterType);
            Assert.Equal(typeof(CancellationToken), parametros[1].ParameterType);
            Assert.True(parametros[1].HasDefaultValue);
        }
    }

    [Fact]
    public void Debe_ExponerMetodoAsincronoConCancellationToken_Cuando_SeInspeccionaIConsultaEstadoEvaluacionesConvocatoriaRepository()
    {
        var metodos = typeof(IConsultaEstadoEvaluacionesConvocatoriaRepository).GetMethods();
        var metodo = Assert.Single(
            metodos,
            m => m.Name == nameof(IConsultaEstadoEvaluacionesConvocatoriaRepository.ExistenEvaluacionesPendientesOSinConsolidarAsync));

        Assert.Equal(typeof(Task<bool>), metodo.ReturnType);

        var parametros = metodo.GetParameters();
        Assert.Equal(2, parametros.Length);
        Assert.Equal(typeof(Guid), parametros[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parametros[1].ParameterType);
        Assert.True(parametros[1].HasDefaultValue);
    }

    [Fact]
    public void Debe_ExponerMetodoAsincronoConCancellationToken_Cuando_SeInspeccionaIDisponibilidadRankingClient()
    {
        var metodos = typeof(IDisponibilidadRankingClient).GetMethods();
        var metodo = Assert.Single(
            metodos,
            m => m.Name == nameof(IDisponibilidadRankingClient.ObtenerDisponibilidadPorConvocatoriaAsync));

        Assert.Equal(typeof(Task<DisponibilidadProgramaRanking?>), metodo.ReturnType);

        var parametros = metodo.GetParameters();
        Assert.Equal(2, parametros.Length);
        Assert.Equal(typeof(Guid), parametros[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parametros[1].ParameterType);
        Assert.True(parametros[1].HasDefaultValue);
    }

    [Fact]
    public void Debe_ExponerMetodoAsincronoConCancellationToken_Cuando_SeInspeccionaIProveedorMontoEstimadoBeca()
    {
        var metodos = typeof(IProveedorMontoEstimadoBeca).GetMethods();
        var metodo = Assert.Single(
            metodos,
            m => m.Name == nameof(IProveedorMontoEstimadoBeca.ObtenerMontosEstimadosPorPostulacionAsync));

        Assert.Equal(typeof(Task<IReadOnlyDictionary<Guid, decimal>>), metodo.ReturnType);

        var parametros = metodo.GetParameters();
        Assert.Equal(4, parametros.Length);
        Assert.Equal(typeof(Guid), parametros[0].ParameterType);
        Assert.Equal(typeof(Guid), parametros[1].ParameterType);
        Assert.Equal(typeof(IReadOnlyCollection<Guid>), parametros[2].ParameterType);
        Assert.Equal(typeof(CancellationToken), parametros[3].ParameterType);
        Assert.True(parametros[3].HasDefaultValue);
    }
}
