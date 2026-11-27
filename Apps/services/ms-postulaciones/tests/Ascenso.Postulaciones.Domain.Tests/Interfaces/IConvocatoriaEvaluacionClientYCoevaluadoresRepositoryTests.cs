using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Interfaces;

namespace Ascenso.Postulaciones.Domain.Tests.Interfaces;

public sealed class IConvocatoriaEvaluacionClientYCoevaluadoresRepositoryTests
{
    [Fact]
    public void Debe_ExponerMetodoAsincronoConCancellationToken_Cuando_SeInspeccionaIConvocatoriaEvaluacionClient()
    {
        var metodos = typeof(IConvocatoriaEvaluacionClient).GetMethods();
        var metodo = Assert.Single(
            metodos,
            m => m.Name == nameof(IConvocatoriaEvaluacionClient.ObtenerVentanaRevisionAsync));

        Assert.Equal(typeof(Task<VentanaRevisionConvocatoria?>), metodo.ReturnType);

        var parametros = metodo.GetParameters();
        Assert.Equal(2, parametros.Length);
        Assert.Equal(typeof(Guid), parametros[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parametros[1].ParameterType);
        Assert.True(parametros[1].HasDefaultValue);
    }

    [Fact]
    public void Debe_ExponerTodosLosMetodosAsincronosConCancellationToken_Cuando_SeInspeccionaIConsultaEvaluacionesCoevaluadoresRepository()
    {
        var metodos = typeof(IConsultaEvaluacionesCoevaluadoresRepository).GetMethods();
        var nombresEsperados = new[]
        {
            nameof(IConsultaEvaluacionesCoevaluadoresRepository.ObtenerAsignacionVigenteEvaluadorAsync),
            nameof(IConsultaEvaluacionesCoevaluadoresRepository.ListarDefinitivasDeCoevaluadoresAsync)
        };

        Assert.Equal(nombresEsperados.Length, metodos.Length);

        foreach (var nombre in nombresEsperados)
        {
            var metodo = Assert.Single(metodos, m => m.Name == nombre);
            Assert.True(typeof(Task).IsAssignableFrom(metodo.ReturnType));

            var parametros = metodo.GetParameters();
            Assert.Equal(3, parametros.Length);
            Assert.Equal(typeof(Guid), parametros[0].ParameterType);
            Assert.Equal(typeof(Guid), parametros[1].ParameterType);
            Assert.Equal(typeof(CancellationToken), parametros[2].ParameterType);
            Assert.True(parametros[2].HasDefaultValue);
        }

        Assert.Equal(
            typeof(Task<AsignacionEvaluacion?>),
            typeof(IConsultaEvaluacionesCoevaluadoresRepository)
                .GetMethod(nameof(IConsultaEvaluacionesCoevaluadoresRepository.ObtenerAsignacionVigenteEvaluadorAsync))!
                .ReturnType);

        Assert.Equal(
            typeof(Task<IReadOnlyList<Evaluacion>>),
            typeof(IConsultaEvaluacionesCoevaluadoresRepository)
                .GetMethod(nameof(IConsultaEvaluacionesCoevaluadoresRepository.ListarDefinitivasDeCoevaluadoresAsync))!
                .ReturnType);
    }
}
