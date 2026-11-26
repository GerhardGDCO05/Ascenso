using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;

namespace Ascenso.Postulaciones.Domain.Tests.Interfaces;

public sealed class IAsignacionEvaluacionRepositoryTests
{
    [Fact]
    public void Debe_ExponerTodosLosMetodosAsincronosConCancellationToken_Cuando_SeInspeccionaElPuerto()
    {
        var metodos = typeof(IAsignacionEvaluacionRepository).GetMethods();
        var nombresEsperados = new[]
        {
            nameof(IAsignacionEvaluacionRepository.AgregarRangoAsync),
            nameof(IAsignacionEvaluacionRepository.ListarVigentesPorPostulacionAsync),
            nameof(IAsignacionEvaluacionRepository.ListarPorPostulacionAsync),
            nameof(IAsignacionEvaluacionRepository.ObtenerCargaPorMiembroAsync),
            nameof(IAsignacionEvaluacionRepository.ListarPorEvaluadorAsync)
        };

        Assert.Equal(nombresEsperados.Length, metodos.Length);

        foreach (var nombre in nombresEsperados)
        {
            var metodo = Assert.Single(metodos, m => m.Name == nombre);
            var ultimoParametro = metodo.GetParameters().Last();

            Assert.Equal(typeof(CancellationToken), ultimoParametro.ParameterType);
            Assert.True(ultimoParametro.HasDefaultValue);
        }
    }

    [Fact]
    public void Debe_PreservarItemsYTotal_Cuando_SeCreaPaginaResultado()
    {
        var items = new[] { "A", "B" };

        var pagina = new PaginaResultado<string>(items, 10);

        Assert.Equal(items, pagina.Items);
        Assert.Equal(10, pagina.Total);
    }
}
