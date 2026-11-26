using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;

namespace Ascenso.Postulaciones.Domain.Tests.Interfaces;

public sealed class IEvaluacionRepositoryTests
{
    [Fact]
    public void Debe_ExponerTodosLosMetodosAsincronosConCancellationToken_Cuando_SeInspeccionaIEvaluacionRepository()
    {
        var metodos = typeof(IEvaluacionRepository).GetMethods();
        var nombresEsperados = new[]
        {
            nameof(IEvaluacionRepository.ObtenerPorAsignacionAsync),
            nameof(IEvaluacionRepository.ObtenerParaActualizarPorAsignacionAsync),
            nameof(IEvaluacionRepository.ListarPorEvaluadorAsync),
            nameof(IEvaluacionRepository.ListarDefinitivasPorPostulacionAsync),
            nameof(IEvaluacionRepository.AgregarAsync)
        };

        Assert.Equal(nombresEsperados.Length, metodos.Length);

        foreach (var nombre in nombresEsperados)
        {
            var metodo = Assert.Single(metodos, m => m.Name == nombre);
            Assert.True(typeof(Task).IsAssignableFrom(metodo.ReturnType));

            var ultimoParametro = metodo.GetParameters().Last();
            Assert.Equal(typeof(CancellationToken), ultimoParametro.ParameterType);
            Assert.True(ultimoParametro.HasDefaultValue);
        }

        Assert.Equal(
            typeof(Task<Evaluacion?>),
            typeof(IEvaluacionRepository).GetMethod(nameof(IEvaluacionRepository.ObtenerPorAsignacionAsync))!.ReturnType);
        Assert.Equal(
            typeof(Task<Evaluacion?>),
            typeof(IEvaluacionRepository).GetMethod(nameof(IEvaluacionRepository.ObtenerParaActualizarPorAsignacionAsync))!.ReturnType);
        Assert.Equal(
            typeof(Task<PaginaResultado<Evaluacion>>),
            typeof(IEvaluacionRepository).GetMethod(nameof(IEvaluacionRepository.ListarPorEvaluadorAsync))!.ReturnType);
        Assert.Equal(
            typeof(Task<IReadOnlyList<Evaluacion>>),
            typeof(IEvaluacionRepository).GetMethod(nameof(IEvaluacionRepository.ListarDefinitivasPorPostulacionAsync))!.ReturnType);
        Assert.Equal(
            typeof(Task),
            typeof(IEvaluacionRepository).GetMethod(nameof(IEvaluacionRepository.AgregarAsync))!.ReturnType);
    }

    [Fact]
    public void Debe_ExponerTodosLosMetodosAsincronosConCancellationToken_Cuando_SeInspeccionaIConsultaAsignacionParaEvaluacionRepository()
    {
        var metodos = typeof(IConsultaAsignacionParaEvaluacionRepository).GetMethods();
        var nombresEsperados = new[]
        {
            nameof(IConsultaAsignacionParaEvaluacionRepository.ObtenerPorIdAsync),
            nameof(IConsultaAsignacionParaEvaluacionRepository.ObtenerParaActualizarPorIdAsync)
        };

        Assert.Equal(nombresEsperados.Length, metodos.Length);

        foreach (var nombre in nombresEsperados)
        {
            var metodo = Assert.Single(metodos, m => m.Name == nombre);
            Assert.Equal(typeof(Task<AsignacionEvaluacion?>), metodo.ReturnType);

            var ultimoParametro = metodo.GetParameters().Last();
            Assert.Equal(typeof(CancellationToken), ultimoParametro.ParameterType);
            Assert.True(ultimoParametro.HasDefaultValue);
        }
    }

    [Fact]
    public void Debe_ExponerMetodoAsincronoConCancellationToken_Cuando_SeInspeccionaIConsultaRubricaParaEvaluacionRepository()
    {
        var metodos = typeof(IConsultaRubricaParaEvaluacionRepository).GetMethods();
        var metodo = Assert.Single(
            metodos,
            m => m.Name == nameof(IConsultaRubricaParaEvaluacionRepository.ObtenerPorProgramaYConvocatoriaAsync));

        Assert.Equal(typeof(Task<RubricaEvaluacion?>), metodo.ReturnType);

        var ultimoParametro = metodo.GetParameters().Last();
        Assert.Equal(typeof(CancellationToken), ultimoParametro.ParameterType);
        Assert.True(ultimoParametro.HasDefaultValue);
    }
}
