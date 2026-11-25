using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Interfaces;

namespace Ascenso.Postulaciones.Domain.Tests.Interfaces;

public sealed class IExcepcionEvaluadorUnicoRepositoryTests
{
    [Fact]
    public void Debe_ExponerSoloObtenerPorPostulacionYAgregarConCancellationToken_Cuando_SeInspeccionaElPuerto()
    {
        var metodos = typeof(IExcepcionEvaluadorUnicoRepository).GetMethods();
        var nombresEsperados = new[]
        {
            nameof(IExcepcionEvaluadorUnicoRepository.ObtenerPorPostulacionAsync),
            nameof(IExcepcionEvaluadorUnicoRepository.AgregarAsync)
        };

        Assert.Equal(nombresEsperados.Length, metodos.Length);

        foreach (var nombre in nombresEsperados)
        {
            var metodo = Assert.Single(metodos, m => m.Name == nombre);
            var ultimoParametro = metodo.GetParameters().Last();

            Assert.Equal(typeof(CancellationToken), ultimoParametro.ParameterType);
            Assert.True(ultimoParametro.HasDefaultValue);
        }

        var obtener = Assert.Single(metodos, m => m.Name == nameof(IExcepcionEvaluadorUnicoRepository.ObtenerPorPostulacionAsync));
        Assert.Equal(typeof(Task<AutorizacionExcepcionEvaluadorUnico?>), obtener.ReturnType);
        Assert.Equal(typeof(Guid), obtener.GetParameters()[0].ParameterType);

        var agregar = Assert.Single(metodos, m => m.Name == nameof(IExcepcionEvaluadorUnicoRepository.AgregarAsync));
        Assert.Equal(typeof(Task), agregar.ReturnType);
        Assert.Equal(typeof(AutorizacionExcepcionEvaluadorUnico), agregar.GetParameters()[0].ParameterType);
    }
}
