using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Domain.Tests.Otorgamiento;

public sealed class OfertaBecaTests
{
    [Fact]
    public void Debe_CrearLaOfertaPendiente_ConLosCamposYValoresPorDefecto()
    {
        var beca = PruebasOtorgamiento.CrearBeca();
        var contexto = PruebasOtorgamiento.Contexto(PruebasOtorgamiento.FechaBase.AddHours(2));
        var datos = PruebasOtorgamiento.DatosOferta(
            beca.PostulacionOrigenId,
            montoPorPeriodo: 625.50m,
            duracion: beca.DuracionTotalPeriodos);

        var oferta = beca.EmitirOferta(datos, contexto);

        Assert.Equal(7, oferta.Id.Version);
        Assert.Equal(beca.Id, oferta.BecaId);
        Assert.Equal(datos.PostulacionId, oferta.PostulacionId);
        Assert.Equal(datos.ReservaPresupuestoId, oferta.ReservaPresupuestoId);
        Assert.Equal(625.50m, oferta.MontoOfertaPorPeriodo);
        Assert.Equal(2502.00m, oferta.MontoTotalEstimadoDuracion);
        Assert.Equal(beca.DuracionTotalPeriodos, oferta.DuracionPeriodos);
        Assert.Equal(datos.CondicionesSnapshot, oferta.CondicionesSnapshot);
        Assert.Equal(datos.UrlCartaPdfFirebase, oferta.UrlCartaPdfFirebase);
        Assert.Equal(datos.HashCartaSha256, oferta.HashCartaSha256);
        Assert.Equal(contexto.Fecha, oferta.FechaEmision);
        Assert.Equal(contexto.Fecha.AddDays(15), oferta.FechaVencimientoAceptacion);
        Assert.Equal(datos.ReferenciaJobExpiracionHangfireId, oferta.ReferenciaJobExpiracionHangfireId);
        Assert.Equal(contexto.UsuarioAutorId, oferta.EmitidaPorUsuarioId);
        Assert.Equal(EstadoOferta.PendienteRespuesta, oferta.Estado);
        Assert.Null(oferta.MotivoRechazoOAnulacion);
        Assert.Null(oferta.FechaRespuesta);
        Assert.Null(oferta.AutorizacionOtpEmisionId);
        Assert.Null(oferta.AutorizacionOtpAceptacionId);
        Assert.Equal(1, oferta.Version);
        Assert.Same(oferta, beca.Oferta);
        Assert.Equal(2, beca.Version);
        Assert.Equal(2, beca.Historial.Count);
    }

    [Fact]
    public void Debe_RegistrarElHistorialDeEmision_Cuando_SeEmiteLaOferta()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();

        var historialOferta = Assert.Single(
            beca.Historial,
            registro => registro.TipoEntidad == TipoEntidadHistorial.OfertaBeca);

        Assert.Equal(beca.Oferta!.Id, historialOferta.OfertaBecaId);
        Assert.Null(historialOferta.EstadoOfertaAnterior);
        Assert.Equal(EstadoOferta.PendienteRespuesta, historialOferta.EstadoOfertaNuevo);
        Assert.False(historialOferta.EstadoTerminalIrreversible);
    }

    [Fact]
    public void Debe_RechazarUnaSegundaOferta_Cuando_LaBecaYaTieneUna()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();

        Assert.Throws<InvalidOperationException>(
            () => beca.EmitirOferta(
                PruebasOtorgamiento.DatosOferta(beca.PostulacionOrigenId),
                PruebasOtorgamiento.Contexto()));
    }

    [Fact]
    public void Debe_RechazarUnaDuracionDistinta_Cuando_SeEmiteLaOferta()
    {
        var beca = PruebasOtorgamiento.CrearBeca();
        var datos = PruebasOtorgamiento.DatosOferta(beca.PostulacionOrigenId, duracion: 3);

        Assert.Throws<ArgumentException>(
            () => beca.EmitirOferta(datos, PruebasOtorgamiento.Contexto()));
    }

    [Fact]
    public void Debe_RechazarUnTotalInconsistente_Cuando_SeEmiteLaOferta()
    {
        var beca = PruebasOtorgamiento.CrearBeca();
        var datos = PruebasOtorgamiento.DatosOferta(beca.PostulacionOrigenId) with
        {
            MontoTotalEstimadoDuracion = 1m
        };

        Assert.Throws<ArgumentException>(
            () => beca.EmitirOferta(datos, PruebasOtorgamiento.Contexto()));
    }
}
