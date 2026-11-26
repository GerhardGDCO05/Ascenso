using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;

namespace Ascenso.Postulaciones.Domain.Tests.Interfaces;

public sealed class IConsolidacionRepositoriesTests
{
    [Fact]
    public void Debe_ExponerTodosLosMetodosAsincronosConCancellationToken_Cuando_SeInspeccionaIResultadoConsolidadoRepository()
    {
        var metodos = typeof(IResultadoConsolidadoRepository).GetMethods();
        var nombresEsperados = new[]
        {
            nameof(IResultadoConsolidadoRepository.ObtenerPorPostulacionAsync),
            nameof(IResultadoConsolidadoRepository.ObtenerParaActualizarPorPostulacionAsync),
            nameof(IResultadoConsolidadoRepository.RecargarParaActualizarPorPostulacionAsync),
            nameof(IResultadoConsolidadoRepository.ListarPorConvocatoriaAsync),
            nameof(IResultadoConsolidadoRepository.AgregarAsync)
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
            typeof(Task<ResultadoConsolidadoRanking?>),
            typeof(IResultadoConsolidadoRepository)
                .GetMethod(nameof(IResultadoConsolidadoRepository.ObtenerPorPostulacionAsync))!
                .ReturnType);
        Assert.Equal(
            typeof(Task<ResultadoConsolidadoRanking?>),
            typeof(IResultadoConsolidadoRepository)
                .GetMethod(nameof(IResultadoConsolidadoRepository.ObtenerParaActualizarPorPostulacionAsync))!
                .ReturnType);
        Assert.Equal(
            typeof(Task<ResultadoConsolidadoRanking?>),
            typeof(IResultadoConsolidadoRepository)
                .GetMethod(nameof(IResultadoConsolidadoRepository.RecargarParaActualizarPorPostulacionAsync))!
                .ReturnType);
        Assert.Equal(
            typeof(Task<PaginaResultado<ResultadoConsolidadoRanking>>),
            typeof(IResultadoConsolidadoRepository)
                .GetMethod(nameof(IResultadoConsolidadoRepository.ListarPorConvocatoriaAsync))!
                .ReturnType);
        Assert.Equal(
            typeof(Task),
            typeof(IResultadoConsolidadoRepository)
                .GetMethod(nameof(IResultadoConsolidadoRepository.AgregarAsync))!
                .ReturnType);

        var parametrosListar = typeof(IResultadoConsolidadoRepository)
            .GetMethod(nameof(IResultadoConsolidadoRepository.ListarPorConvocatoriaAsync))!
            .GetParameters();
        Assert.Equal(6, parametrosListar.Length);
        Assert.Equal(typeof(Guid), parametrosListar[0].ParameterType);
        Assert.Equal(typeof(bool?), parametrosListar[1].ParameterType);
        Assert.Equal(typeof(bool?), parametrosListar[2].ParameterType);
        Assert.Equal(typeof(int), parametrosListar[3].ParameterType);
        Assert.Equal(typeof(int), parametrosListar[4].ParameterType);
        Assert.Equal(typeof(CancellationToken), parametrosListar[5].ParameterType);
    }

    [Fact]
    public void Debe_ExponerTodosLosMetodosAsincronosConCancellationToken_Cuando_SeInspeccionaIConsultaDatosConsolidacionRepository()
    {
        var metodos = typeof(IConsultaDatosConsolidacionRepository).GetMethods();
        var nombresEsperados = new[]
        {
            nameof(IConsultaDatosConsolidacionRepository.ListarAsignacionesVigentesPorPostulacionAsync),
            nameof(IConsultaDatosConsolidacionRepository.ListarEvaluacionesDefinitivasPorPostulacionAsync),
            nameof(IConsultaDatosConsolidacionRepository.ObtenerExcepcionEvaluadorUnicoPorPostulacionAsync)
        };

        Assert.Equal(nombresEsperados.Length, metodos.Length);

        foreach (var nombre in nombresEsperados)
        {
            var metodo = Assert.Single(metodos, m => m.Name == nombre);
            Assert.True(typeof(Task).IsAssignableFrom(metodo.ReturnType));

            var parametros = metodo.GetParameters();
            Assert.Equal(2, parametros.Length);
            Assert.Equal(typeof(Guid), parametros[0].ParameterType);
            Assert.Equal(typeof(CancellationToken), parametros[1].ParameterType);
            Assert.True(parametros[1].HasDefaultValue);
        }

        Assert.Equal(
            typeof(Task<IReadOnlyList<AsignacionEvaluacion>>),
            typeof(IConsultaDatosConsolidacionRepository)
                .GetMethod(nameof(IConsultaDatosConsolidacionRepository.ListarAsignacionesVigentesPorPostulacionAsync))!
                .ReturnType);
        Assert.Equal(
            typeof(Task<IReadOnlyList<Evaluacion>>),
            typeof(IConsultaDatosConsolidacionRepository)
                .GetMethod(nameof(IConsultaDatosConsolidacionRepository.ListarEvaluacionesDefinitivasPorPostulacionAsync))!
                .ReturnType);
        Assert.Equal(
            typeof(Task<AutorizacionExcepcionEvaluadorUnico?>),
            typeof(IConsultaDatosConsolidacionRepository)
                .GetMethod(nameof(IConsultaDatosConsolidacionRepository.ObtenerExcepcionEvaluadorUnicoPorPostulacionAsync))!
                .ReturnType);
    }

    [Fact]
    public void Debe_ExponerMetodoAsincronoConCancellationToken_Cuando_SeInspeccionaIProveedorParametrosConsolidacion()
    {
        var metodos = typeof(IProveedorParametrosConsolidacion).GetMethods();
        var metodo = Assert.Single(
            metodos,
            m => m.Name == nameof(IProveedorParametrosConsolidacion.ObtenerParametrosAsync));

        Assert.Equal(typeof(Task<ParametrosConsolidacion>), metodo.ReturnType);

        var parametros = metodo.GetParameters();
        Assert.Equal(3, parametros.Length);
        Assert.Equal(typeof(Guid), parametros[0].ParameterType);
        Assert.Equal(typeof(Guid), parametros[1].ParameterType);
        Assert.Equal(typeof(CancellationToken), parametros[2].ParameterType);
        Assert.True(parametros[2].HasDefaultValue);
    }
}
