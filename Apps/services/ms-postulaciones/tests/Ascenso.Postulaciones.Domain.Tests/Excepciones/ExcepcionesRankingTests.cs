using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Excepciones;

public sealed class ExcepcionesRankingTests
{
    [Fact]
    public void Debe_ExponerHerenciaCodigoEstadoYMensajePorDefecto_Cuando_SeCreanLasExcepcionesDeRanking()
    {
        var noEncontradas = new (ExcepcionEntidadNoEncontrada Excepcion, string CodigoEsperado)[]
        {
            (new ProgramaBecaNoEncontradoException(), ProgramaBecaNoEncontradoException.CodigoError)
        };

        var reglasNegocio = new (ExcepcionReglaNegocio Excepcion, string CodigoEsperado, int EstadoHttpEsperado)[]
        {
            (new EmpatesSinResolverEnCorteException(), EmpatesSinResolverEnCorteException.CodigoError, 422),
            (new DiscrepanciasSinRevisarEnCorteException(), DiscrepanciasSinRevisarEnCorteException.CodigoError, 422),
            (new RankingSinResultadosConsolidadosException(), RankingSinResultadosConsolidadosException.CodigoError, 422),
            (new RankingInvalidoException(), RankingInvalidoException.CodigoError, 422)
        };

        Assert.Equal("PROGRAMA_NO_ENCONTRADO", ProgramaBecaNoEncontradoException.CodigoError);
        Assert.Equal("EMPATES_SIN_RESOLVER", EmpatesSinResolverEnCorteException.CodigoError);
        Assert.Equal("DISCREPANCIAS_SIN_REVISAR_EN_CORTE", DiscrepanciasSinRevisarEnCorteException.CodigoError);
        Assert.Equal("RANKING_SIN_RESULTADOS", RankingSinResultadosConsolidadosException.CodigoError);
        Assert.Equal("RANKING_INVALIDO", RankingInvalidoException.CodigoError);

        foreach (var (excepcion, codigoEsperado) in noEncontradas)
        {
            Assert.Equal(codigoEsperado, excepcion.Codigo);
            Assert.Equal(404, excepcion.EstadoHttp);
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }

        foreach (var (excepcion, codigoEsperado, estadoHttpEsperado) in reglasNegocio)
        {
            Assert.Equal(codigoEsperado, excepcion.Codigo);
            Assert.Equal(estadoHttpEsperado, excepcion.EstadoHttp);
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }

    [Fact]
    public void Debe_PreservarMensajePersonalizado_Cuando_SeEspecificaUnMensajeNoVacio()
    {
        const string mensajePersonalizado = "Detalle personalizado de la regla de ranking.";

        var excepciones = new DomainException[]
        {
            new ProgramaBecaNoEncontradoException(mensajePersonalizado),
            new EmpatesSinResolverEnCorteException(mensajePersonalizado),
            new DiscrepanciasSinRevisarEnCorteException(mensajePersonalizado),
            new RankingSinResultadosConsolidadosException(mensajePersonalizado),
            new RankingInvalidoException(mensajePersonalizado)
        };

        foreach (var excepcion in excepciones)
        {
            Assert.Equal(mensajePersonalizado, excepcion.Message);
        }
    }

    [Fact]
    public void Debe_UsarMensajePorDefecto_Cuando_ElMensajePersonalizadoEsSoloEspacios()
    {
        var excepciones = new DomainException[]
        {
            new ProgramaBecaNoEncontradoException("   "),
            new EmpatesSinResolverEnCorteException("   "),
            new DiscrepanciasSinRevisarEnCorteException("   "),
            new RankingSinResultadosConsolidadosException("   "),
            new RankingInvalidoException("   ")
        };

        foreach (var excepcion in excepciones)
        {
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }
}
