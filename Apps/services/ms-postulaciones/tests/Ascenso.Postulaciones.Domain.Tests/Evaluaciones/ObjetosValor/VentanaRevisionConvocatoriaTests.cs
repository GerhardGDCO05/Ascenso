using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Evaluaciones.ObjetosValor;

public sealed class VentanaRevisionConvocatoriaTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000000820");
    private static readonly DateTimeOffset _inicioUtc = new(2026, 11, 20, 4, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _cierreUtc = new(2026, 11, 30, 3, 59, 59, TimeSpan.Zero);

    [Fact]
    public void Debe_CrearObjetoDeValorYNormalizarFechasAUtc_Cuando_LosArgumentosSonValidos()
    {
        var inicioCaracas = new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.FromHours(-4));
        var cierreMadrid = new DateTimeOffset(2026, 11, 30, 5, 59, 59, TimeSpan.FromHours(2));

        var ventana = new VentanaRevisionConvocatoria(
            _convocatoriaId,
            EstadoConvocatoriaExterna.Publicada,
            inicioCaracas,
            cierreMadrid);

        Assert.IsAssignableFrom<ValueObject>(ventana);
        Assert.Equal(_convocatoriaId, ventana.ConvocatoriaId);
        Assert.Equal(EstadoConvocatoriaExterna.Publicada, ventana.EstadoConvocatoria);
        Assert.Equal(_inicioUtc, ventana.FechaInicioEvaluacion);
        Assert.Equal(_cierreUtc, ventana.FechaCierreEvaluacion);
        Assert.Equal(TimeSpan.Zero, ventana.FechaInicioEvaluacion.Offset);
        Assert.Equal(TimeSpan.Zero, ventana.FechaCierreEvaluacion.Offset);
    }

    [Fact]
    public void Debe_AplicarIgualdadEstructuralSinSettersPublicos_Cuando_DosInstanciasRepresentanLaMismaVentana()
    {
        var inicioCaracas = new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.FromHours(-4));
        var cierreCaracas = new DateTimeOffset(2026, 11, 29, 23, 59, 59, TimeSpan.FromHours(-4));

        var primera = new VentanaRevisionConvocatoria(
            _convocatoriaId,
            EstadoConvocatoriaExterna.Cerrada,
            _inicioUtc,
            _cierreUtc);
        var segunda = new VentanaRevisionConvocatoria(
            _convocatoriaId,
            EstadoConvocatoriaExterna.Cerrada,
            inicioCaracas,
            cierreCaracas);

        Assert.Equal(primera, segunda);
        Assert.True(primera == segunda);

        var propiedadesConSetter = typeof(VentanaRevisionConvocatoria)
            .GetProperties()
            .Where(p => p.SetMethod is not null);
        Assert.Empty(propiedadesConSetter);
    }

    [Fact]
    public void Debe_RechazarConvocatoriaIdVacio_Cuando_SeConstruyeLaVentana()
    {
        Assert.Throws<ArgumentException>(() => new VentanaRevisionConvocatoria(
            Guid.Empty,
            EstadoConvocatoriaExterna.Publicada,
            _inicioUtc,
            _cierreUtc));
    }

    [Fact]
    public void Debe_RechazarEstadoConvocatoriaIndefinido_Cuando_SeConstruyeLaVentana()
    {
        Assert.Throws<ArgumentException>(() => new VentanaRevisionConvocatoria(
            _convocatoriaId,
            (EstadoConvocatoriaExterna)999,
            _inicioUtc,
            _cierreUtc));
    }

    [Fact]
    public void Debe_RechazarFechasPorDefecto_Cuando_SeConstruyeLaVentana()
    {
        Assert.Throws<ArgumentException>(() => new VentanaRevisionConvocatoria(
            _convocatoriaId,
            EstadoConvocatoriaExterna.Publicada,
            default,
            _cierreUtc));

        Assert.Throws<ArgumentException>(() => new VentanaRevisionConvocatoria(
            _convocatoriaId,
            EstadoConvocatoriaExterna.Publicada,
            _inicioUtc,
            default));
    }

    [Fact]
    public void Debe_RechazarCierreIgualOAnteriorAlInicio_Cuando_SeConstruyeLaVentana()
    {
        Assert.Throws<ArgumentException>(() => new VentanaRevisionConvocatoria(
            _convocatoriaId,
            EstadoConvocatoriaExterna.Publicada,
            _inicioUtc,
            _inicioUtc));

        Assert.Throws<ArgumentException>(() => new VentanaRevisionConvocatoria(
            _convocatoriaId,
            EstadoConvocatoriaExterna.Publicada,
            _cierreUtc,
            _inicioUtc));

        var mismoInstanteCaracas = _inicioUtc.ToOffset(TimeSpan.FromHours(-4));
        Assert.Throws<ArgumentException>(() => new VentanaRevisionConvocatoria(
            _convocatoriaId,
            EstadoConvocatoriaExterna.Publicada,
            _inicioUtc,
            mismoInstanteCaracas));
    }

    [Fact]
    public void Debe_RechazarAhoraPorDefecto_Cuando_SeEvaluaLaVentana()
    {
        var ventana = new VentanaRevisionConvocatoria(
            _convocatoriaId,
            EstadoConvocatoriaExterna.Publicada,
            _inicioUtc,
            _cierreUtc);

        Assert.Throws<ArgumentException>(() => ventana.ObtenerEstadoPeriodoRevision(default));
        Assert.Throws<ArgumentException>(() => ventana.AsegurarPeriodoAbiertoParaEvaluacion(default));
        Assert.Throws<ArgumentException>(() => ventana.AsegurarPeriodoCerradoParaVisibilidadCoevaluadores(default));
    }

    [Theory]
    [InlineData(EstadoConvocatoriaExterna.Borrador, -1, EstadoPeriodoRevision.NoIniciado)]
    [InlineData(EstadoConvocatoriaExterna.Borrador, 0, EstadoPeriodoRevision.NoIniciado)]
    [InlineData(EstadoConvocatoriaExterna.Borrador, 1, EstadoPeriodoRevision.NoIniciado)]
    [InlineData(EstadoConvocatoriaExterna.Borrador, 2, EstadoPeriodoRevision.NoIniciado)]
    [InlineData(EstadoConvocatoriaExterna.Borrador, 3, EstadoPeriodoRevision.NoIniciado)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, -1, EstadoPeriodoRevision.NoIniciado)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, 0, EstadoPeriodoRevision.Abierto)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, 1, EstadoPeriodoRevision.Abierto)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, 2, EstadoPeriodoRevision.Abierto)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, 3, EstadoPeriodoRevision.Cerrado)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, -1, EstadoPeriodoRevision.NoIniciado)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, 0, EstadoPeriodoRevision.Abierto)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, 1, EstadoPeriodoRevision.Abierto)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, 2, EstadoPeriodoRevision.Abierto)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, 3, EstadoPeriodoRevision.Cerrado)]
    [InlineData(EstadoConvocatoriaExterna.Cancelada, -1, EstadoPeriodoRevision.Cancelado)]
    [InlineData(EstadoConvocatoriaExterna.Cancelada, 0, EstadoPeriodoRevision.Cancelado)]
    [InlineData(EstadoConvocatoriaExterna.Cancelada, 1, EstadoPeriodoRevision.Cancelado)]
    [InlineData(EstadoConvocatoriaExterna.Cancelada, 2, EstadoPeriodoRevision.Cancelado)]
    [InlineData(EstadoConvocatoriaExterna.Cancelada, 3, EstadoPeriodoRevision.Cancelado)]
    public void Debe_DerivarEstadoPeriodoRevisionSegunTabla_Cuando_SeEvaluaCadaCombinacion(
        EstadoConvocatoriaExterna estadoConvocatoria,
        int puntoTemporal,
        EstadoPeriodoRevision estadoEsperado)
    {
        var ventana = new VentanaRevisionConvocatoria(
            _convocatoriaId,
            estadoConvocatoria,
            _inicioUtc,
            _cierreUtc);
        var ahora = ResolverPuntoTemporal(puntoTemporal);

        var estado = ventana.ObtenerEstadoPeriodoRevision(ahora);

        Assert.Equal(estadoEsperado, estado);
    }

    [Fact]
    public void Debe_NormalizarAhoraAUtc_Cuando_SeEvaluaConOffsetDistintoDeCero()
    {
        var ventana = new VentanaRevisionConvocatoria(
            _convocatoriaId,
            EstadoConvocatoriaExterna.Cerrada,
            _inicioUtc,
            _cierreUtc);

        var inicioConOffset = _inicioUtc.ToOffset(TimeSpan.FromHours(-4));
        var antesDeInicioConOffset = _inicioUtc.AddTicks(-1).ToOffset(TimeSpan.FromHours(-4));
        var cierreConOffset = _cierreUtc.ToOffset(TimeSpan.FromHours(-4));
        var despuesDeCierreConOffset = _cierreUtc.AddTicks(1).ToOffset(TimeSpan.FromHours(-4));

        Assert.Equal(EstadoPeriodoRevision.NoIniciado, ventana.ObtenerEstadoPeriodoRevision(antesDeInicioConOffset));
        Assert.Equal(EstadoPeriodoRevision.Abierto, ventana.ObtenerEstadoPeriodoRevision(inicioConOffset));
        Assert.Equal(EstadoPeriodoRevision.Abierto, ventana.ObtenerEstadoPeriodoRevision(cierreConOffset));
        Assert.Equal(EstadoPeriodoRevision.Cerrado, ventana.ObtenerEstadoPeriodoRevision(despuesDeCierreConOffset));
    }

    [Theory]
    [InlineData(EstadoConvocatoriaExterna.Publicada, 0)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, 1)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, 2)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, 0)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, 1)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, 2)]
    public void Debe_PermitirEvaluacion_Cuando_ElPeriodoEstaAbiertoIncluyendoLimitesExactos(
        EstadoConvocatoriaExterna estadoConvocatoria,
        int puntoTemporal)
    {
        var ventana = new VentanaRevisionConvocatoria(
            _convocatoriaId,
            estadoConvocatoria,
            _inicioUtc,
            _cierreUtc);

        var excepcion = Record.Exception(() => ventana.AsegurarPeriodoAbiertoParaEvaluacion(ResolverPuntoTemporal(puntoTemporal)));

        Assert.Null(excepcion);
    }

    [Theory]
    [InlineData(EstadoConvocatoriaExterna.Borrador, -1)]
    [InlineData(EstadoConvocatoriaExterna.Borrador, 1)]
    [InlineData(EstadoConvocatoriaExterna.Borrador, 3)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, -1)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, -1)]
    public void Debe_LanzarPeriodoRevisionNoIniciado_Cuando_SeIntentaEvaluarAntesDelInicioOEnBorrador(
        EstadoConvocatoriaExterna estadoConvocatoria,
        int puntoTemporal)
    {
        var ventana = new VentanaRevisionConvocatoria(
            _convocatoriaId,
            estadoConvocatoria,
            _inicioUtc,
            _cierreUtc);

        var excepcion = Assert.Throws<PeriodoRevisionNoIniciadoException>(
            () => ventana.AsegurarPeriodoAbiertoParaEvaluacion(ResolverPuntoTemporal(puntoTemporal)));

        Assert.Equal(PeriodoRevisionNoIniciadoException.CodigoError, excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
    }

    [Theory]
    [InlineData(EstadoConvocatoriaExterna.Publicada)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada)]
    public void Debe_LanzarPeriodoRevisionCerrado_Cuando_SeIntentaEvaluarUnTickDespuesDelCierre(
        EstadoConvocatoriaExterna estadoConvocatoria)
    {
        var ventana = new VentanaRevisionConvocatoria(
            _convocatoriaId,
            estadoConvocatoria,
            _inicioUtc,
            _cierreUtc);

        var excepcion = Assert.Throws<PeriodoRevisionCerradoException>(
            () => ventana.AsegurarPeriodoAbiertoParaEvaluacion(_cierreUtc.AddTicks(1)));

        Assert.Equal(PeriodoRevisionCerradoException.CodigoError, excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Debe_LanzarConvocatoriaCancelada_Cuando_SeIntentaEvaluarOConsultarCoevaluadoresEnConvocatoriaCancelada(
        int puntoTemporal)
    {
        var ventana = new VentanaRevisionConvocatoria(
            _convocatoriaId,
            EstadoConvocatoriaExterna.Cancelada,
            _inicioUtc,
            _cierreUtc);
        var ahora = ResolverPuntoTemporal(puntoTemporal);

        var exEscritura = Assert.Throws<ConvocatoriaCanceladaException>(
            () => ventana.AsegurarPeriodoAbiertoParaEvaluacion(ahora));
        var exConsulta = Assert.Throws<ConvocatoriaCanceladaException>(
            () => ventana.AsegurarPeriodoCerradoParaVisibilidadCoevaluadores(ahora));

        Assert.Equal(ConvocatoriaCanceladaException.CodigoError, exEscritura.Codigo);
        Assert.Equal(409, exEscritura.EstadoHttp);
        Assert.Equal(ConvocatoriaCanceladaException.CodigoError, exConsulta.Codigo);
        Assert.Equal(409, exConsulta.EstadoHttp);
    }

    [Theory]
    [InlineData(EstadoConvocatoriaExterna.Publicada)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada)]
    public void Debe_PermitirVisibilidadCoevaluadores_Cuando_HaTranscurridoUnTickDespuesDelCierre(
        EstadoConvocatoriaExterna estadoConvocatoria)
    {
        var ventana = new VentanaRevisionConvocatoria(
            _convocatoriaId,
            estadoConvocatoria,
            _inicioUtc,
            _cierreUtc);

        var excepcion = Record.Exception(
            () => ventana.AsegurarPeriodoCerradoParaVisibilidadCoevaluadores(_cierreUtc.AddTicks(1)));

        Assert.Null(excepcion);
    }

    [Theory]
    [InlineData(EstadoConvocatoriaExterna.Borrador, -1)]
    [InlineData(EstadoConvocatoriaExterna.Borrador, 1)]
    [InlineData(EstadoConvocatoriaExterna.Borrador, 3)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, -1)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, 0)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, 1)]
    [InlineData(EstadoConvocatoriaExterna.Publicada, 2)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, -1)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, 0)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, 1)]
    [InlineData(EstadoConvocatoriaExterna.Cerrada, 2)]
    public void Debe_LanzarPeriodoRevisionAbierto_Cuando_SeConsultaVisibilidadCoevaluadoresAntesDelCierre(
        EstadoConvocatoriaExterna estadoConvocatoria,
        int puntoTemporal)
    {
        var ventana = new VentanaRevisionConvocatoria(
            _convocatoriaId,
            estadoConvocatoria,
            _inicioUtc,
            _cierreUtc);

        var excepcion = Assert.Throws<PeriodoRevisionAbiertoException>(
            () => ventana.AsegurarPeriodoCerradoParaVisibilidadCoevaluadores(ResolverPuntoTemporal(puntoTemporal)));

        Assert.Equal(PeriodoRevisionAbiertoException.CodigoError, excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
    }

    private static DateTimeOffset ResolverPuntoTemporal(int puntoTemporal) => puntoTemporal switch
    {
        -1 => _inicioUtc.AddTicks(-1),
        0 => _inicioUtc,
        1 => _inicioUtc.AddDays(5),
        2 => _cierreUtc,
        3 => _cierreUtc.AddTicks(1),
        _ => throw new ArgumentOutOfRangeException(nameof(puntoTemporal))
    };
}
