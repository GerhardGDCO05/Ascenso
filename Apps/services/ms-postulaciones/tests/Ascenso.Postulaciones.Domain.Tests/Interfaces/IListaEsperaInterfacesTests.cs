using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;

namespace Ascenso.Postulaciones.Domain.Tests.Interfaces;

public sealed class IListaEsperaInterfacesTests
{
    [Fact]
    public void Debe_ExponerMetodosAsincronosConCancellationToken_Cuando_SeInspeccionaIEntradaListaEsperaRepository()
    {
        var tipo = typeof(IEntradaListaEsperaRepository);
        Assert.True(tipo.IsInterface);

        var metodos = tipo.GetMethods();
        Assert.Equal(5, metodos.Length);

        var agregarRango = Assert.Single(metodos, m => m.Name == nameof(IEntradaListaEsperaRepository.AgregarRangoAsync));
        Assert.Equal(typeof(Task), agregarRango.ReturnType);
        var paramsAgregar = agregarRango.GetParameters();
        Assert.Equal(2, paramsAgregar.Length);
        Assert.Equal(typeof(IReadOnlyCollection<EntradaListaEspera>), paramsAgregar[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), paramsAgregar[1].ParameterType);
        Assert.True(paramsAgregar[1].HasDefaultValue);

        var obtenerPorId = Assert.Single(metodos, m => m.Name == nameof(IEntradaListaEsperaRepository.ObtenerPorIdAsync));
        Assert.Equal(typeof(Task<EntradaListaEspera?>), obtenerPorId.ReturnType);
        var paramsPorId = obtenerPorId.GetParameters();
        Assert.Equal(2, paramsPorId.Length);
        Assert.Equal(typeof(Guid), paramsPorId[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), paramsPorId[1].ParameterType);
        Assert.True(paramsPorId[1].HasDefaultValue);

        var obtenerPorPostulacion = Assert.Single(
            metodos,
            m => m.Name == nameof(IEntradaListaEsperaRepository.ObtenerPorPostulacionAsync));
        Assert.Equal(typeof(Task<EntradaListaEspera?>), obtenerPorPostulacion.ReturnType);
        var paramsPorPostulacion = obtenerPorPostulacion.GetParameters();
        Assert.Equal(2, paramsPorPostulacion.Length);
        Assert.Equal(typeof(Guid), paramsPorPostulacion[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), paramsPorPostulacion[1].ParameterType);
        Assert.True(paramsPorPostulacion[1].HasDefaultValue);

        var listarPorConvocatoria = Assert.Single(
            metodos,
            m => m.Name == nameof(IEntradaListaEsperaRepository.ListarPorConvocatoriaAsync));
        Assert.Equal(typeof(Task<IReadOnlyList<EntradaListaEspera>>), listarPorConvocatoria.ReturnType);
        var paramsListar = listarPorConvocatoria.GetParameters();
        Assert.Equal(3, paramsListar.Length);
        Assert.Equal(typeof(Guid), paramsListar[0].ParameterType);
        Assert.Equal(typeof(EstadoListaEspera?), paramsListar[1].ParameterType);
        Assert.True(paramsListar[1].HasDefaultValue);
        Assert.Equal(typeof(CancellationToken), paramsListar[2].ParameterType);
        Assert.True(paramsListar[2].HasDefaultValue);

        var obtenerSiguiente = Assert.Single(
            metodos,
            m => m.Name == nameof(IEntradaListaEsperaRepository.ObtenerSiguientePendientePorConvocatoriaAsync));
        Assert.Equal(typeof(Task<EntradaListaEspera?>), obtenerSiguiente.ReturnType);
        var paramsSiguiente = obtenerSiguiente.GetParameters();
        Assert.Equal(2, paramsSiguiente.Length);
        Assert.Equal(typeof(Guid), paramsSiguiente[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), paramsSiguiente[1].ParameterType);
        Assert.True(paramsSiguiente[1].HasDefaultValue);

        foreach (var metodo in metodos)
        {
            Assert.EndsWith("Async", metodo.Name, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Debe_ExponerMetodoAsincronoGenericoConCancellationToken_Cuando_SeInspeccionaIGestorConcurrenciaListaEspera()
    {
        var tipo = typeof(IGestorConcurrenciaListaEspera);
        Assert.True(tipo.IsInterface);

        var metodos = tipo.GetMethods();
        var metodo = Assert.Single(
            metodos,
            m => m.Name == nameof(IGestorConcurrenciaListaEspera.EjecutarConBloqueoConvocatoriaAsync));

        Assert.EndsWith("Async", metodo.Name, StringComparison.Ordinal);
        Assert.True(metodo.IsGenericMethodDefinition);
        var argumentosGenericos = metodo.GetGenericArguments();
        Assert.Single(argumentosGenericos);

        var tipoRetorno = metodo.ReturnType;
        Assert.True(tipoRetorno.IsGenericType);
        Assert.Equal(typeof(Task<>), tipoRetorno.GetGenericTypeDefinition());

        var parametros = metodo.GetParameters();
        Assert.Equal(3, parametros.Length);
        Assert.Equal(typeof(Guid), parametros[0].ParameterType);
        Assert.True(parametros[1].ParameterType.IsGenericType);
        Assert.Equal(typeof(Func<,,>), parametros[1].ParameterType.GetGenericTypeDefinition());
        Assert.Equal(typeof(CancellationToken), parametros[2].ParameterType);
        Assert.True(parametros[2].HasDefaultValue);
    }
}
