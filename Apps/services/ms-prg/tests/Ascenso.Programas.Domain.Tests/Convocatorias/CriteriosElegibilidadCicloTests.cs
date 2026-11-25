using Ascenso.Programas.Domain.Convocatorias.ObjetosValor;
using Ascenso.Programas.Domain.Excepciones;
using FluentAssertions;

namespace Ascenso.Programas.Domain.Tests.Convocatorias;

public sealed class CriteriosElegibilidadCicloTests
{
    private static readonly Guid _universidadId = Guid.Parse("01966000-0000-7000-8000-000000000301");
    private static readonly Guid _carreraId = Guid.Parse("01966000-0000-7000-8000-000000000401");

    [Fact]
    public void Debe_AceptarLosLimitesIncluidos_Cuando_LosCriteriosSonValidos()
    {
        var criterios = CrearCriterios(0.00m, 0, 120, 0.00m);

        criterios.PromedioMinimo.Should().Be(0.00m);
        criterios.EdadMinima.Should().Be(0);
        criterios.EdadMaxima.Should().Be(120);
        criterios.TopeIngresoFamiliar.Should().Be(0.00m);
        criterios.UniversidadesPermitidasIds.Should().ContainSingle().Which.Should().Be(_universidadId);
        criterios.CarrerasPermitidasIds.Should().ContainSingle().Which.Should().Be(_carreraId);
    }

    [Fact]
    public void Debe_AceptarElPromedioMaximo_Cuando_EsVeinte()
    {
        var criterios = CrearCriterios(20.00m);

        criterios.PromedioMinimo.Should().Be(20.00m);
    }

    [Fact]
    public void Debe_RechazarElPromedioFueraDelRango_Cuando_EsNegativoOMayorQueVeinte()
    {
        foreach (var promedio in new[] { -0.001m, 20.001m })
        {
            var accion = () => CrearCriterios(promedio);

            accion.Should().Throw<CriteriosConvocatoriaInvalidosException>();
        }
    }

    [Fact]
    public void Debe_RechazarEdadesFueraDelRango_Cuando_SonMenoresQueCeroOMayoresQueCientoVeinte()
    {
        var edadMinimaNegativa = () => CrearCriterios(edadMinima: -1);
        var edadMaximaExcesiva = () => CrearCriterios(edadMaxima: 121);

        edadMinimaNegativa.Should().Throw<CriteriosConvocatoriaInvalidosException>();
        edadMaximaExcesiva.Should().Throw<CriteriosConvocatoriaInvalidosException>();
    }

    [Fact]
    public void Debe_RechazarEdadesInvertidas_Cuando_LaEdadMinimaSuperaLaMaxima()
    {
        var accion = () => CrearCriterios(edadMinima: 21, edadMaxima: 20);

        accion.Should().Throw<CriteriosConvocatoriaInvalidosException>();
    }

    [Fact]
    public void Debe_RechazarElTopeDeIngresoNegativo_Cuando_ElValorEsMenorQueCero()
    {
        var accion = () => CrearCriterios(topeIngresoFamiliar: -0.001m);

        accion.Should().Throw<CriteriosConvocatoriaInvalidosException>();
    }

    [Fact]
    public void Debe_RechazarListasVacias_Cuando_NoHayUniversidadesOCarrerasPermitidas()
    {
        var sinUniversidades = () => CrearCriterios(universidades: []);
        var sinCarreras = () => CrearCriterios(carreras: []);

        sinUniversidades.Should().Throw<CriteriosConvocatoriaInvalidosException>();
        sinCarreras.Should().Throw<CriteriosConvocatoriaInvalidosException>();
    }

    [Fact]
    public void Debe_CopiarYProtegerLasListas_Cuando_SeConstruyenLosCriterios()
    {
        var universidades = new List<Guid> { _universidadId };
        var carreras = new List<Guid> { _carreraId };
        var criterios = CrearCriterios(universidades: universidades, carreras: carreras);

        universidades.Clear();
        carreras.Clear();

        criterios.UniversidadesPermitidasIds.Should().ContainSingle().Which.Should().Be(_universidadId);
        criterios.CarrerasPermitidasIds.Should().ContainSingle().Which.Should().Be(_carreraId);
        ((IList<Guid>)criterios.UniversidadesPermitidasIds)
            .Invoking(lista => lista.Add(Guid.NewGuid()))
            .Should()
            .Throw<NotSupportedException>();
    }

    private static CriteriosElegibilidadCiclo CrearCriterios(
        decimal promedioMinimo = 15.00m,
        int edadMinima = 18,
        int edadMaxima = 35,
        decimal topeIngresoFamiliar = 1200.00m,
        IReadOnlyCollection<Guid>? universidades = null,
        IReadOnlyCollection<Guid>? carreras = null)
        => new(
            promedioMinimo,
            edadMinima,
            edadMaxima,
            topeIngresoFamiliar,
            universidades ?? [_universidadId],
            carreras ?? [_carreraId]);
}
