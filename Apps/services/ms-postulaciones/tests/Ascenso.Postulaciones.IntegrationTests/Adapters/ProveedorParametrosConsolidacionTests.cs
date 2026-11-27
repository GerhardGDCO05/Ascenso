using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Infrastructure.Adapters;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Ascenso.Postulaciones.IntegrationTests.Adapters;

public sealed class ProveedorParametrosConsolidacionTests
{
    [Fact]
    public async Task Debe_RetornarParametrosPorDefecto_Cuando_ConsolidacionOptionsUsaValoresPorDefecto()
    {
        var opciones = Options.Create(new ConsolidacionOptions());
        var proveedor = new ProveedorParametrosConsolidacionConfigurado(opciones);

        var parametros = await proveedor.ObtenerParametrosAsync(Guid.NewGuid(), Guid.NewGuid());

        parametros.EvaluadoresMinimosRequeridos.Should().Be(ParametrosConsolidacion.EvaluadoresMinimosPorDefecto);
        parametros.UmbralDiscrepanciaPuntos.Should().Be(ParametrosConsolidacion.UmbralDiscrepanciaPorDefecto);
    }

    [Fact]
    public async Task Debe_RetornarParametrosConfigurados_Cuando_ConsolidacionOptionsEspecificaValoresPersonalizados()
    {
        var opciones = Options.Create(new ConsolidacionOptions
        {
            EvaluadoresMinimosRequeridos = 3,
            UmbralDiscrepanciaPuntos = 15.50m
        });
        var proveedor = new ProveedorParametrosConsolidacionConfigurado(opciones);

        var parametros = await proveedor.ObtenerParametrosAsync(Guid.NewGuid(), Guid.NewGuid());

        parametros.EvaluadoresMinimosRequeridos.Should().Be(3);
        parametros.UmbralDiscrepanciaPuntos.Should().Be(15.50m);
    }

    [Fact]
    public async Task Debe_LanzarExcepcion_Cuando_OpcionesOIdentificadoresSonInvalidos()
    {
        var ctorNulo = () => new ProveedorParametrosConsolidacionConfigurado(null!);
        ctorNulo.Should().Throw<ArgumentNullException>();

        var proveedorValido = new ProveedorParametrosConsolidacionConfigurado(
            Options.Create(new ConsolidacionOptions()));

        var convocatoriaVacia = () => proveedorValido.ObtenerParametrosAsync(Guid.Empty, Guid.NewGuid());
        var rubricaVacia = () => proveedorValido.ObtenerParametrosAsync(Guid.NewGuid(), Guid.Empty);

        await convocatoriaVacia.Should().ThrowAsync<ArgumentException>();
        await rubricaVacia.Should().ThrowAsync<ArgumentException>();

        var proveedorInvalido = new ProveedorParametrosConsolidacionConfigurado(
            Options.Create(new ConsolidacionOptions
            {
                EvaluadoresMinimosRequeridos = 1,
                UmbralDiscrepanciaPuntos = 20.00m
            }));

        var obtenerInvalidos = () => proveedorInvalido.ObtenerParametrosAsync(Guid.NewGuid(), Guid.NewGuid());
        await obtenerInvalidos.Should().ThrowAsync<ConsolidacionInvalidaException>();
    }
}
