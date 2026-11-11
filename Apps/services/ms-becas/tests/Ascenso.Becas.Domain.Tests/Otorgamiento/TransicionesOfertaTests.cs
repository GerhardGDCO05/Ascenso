using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;

namespace Ascenso.Becas.Domain.Tests.Otorgamiento;

public sealed class TransicionesOfertaTests
{
    private const string Justificacion = "Justificación suficiente para la operación.";

    [Fact]
    public void Debe_AceptarLaOferta_Cuando_EstaPendiente()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var contexto = PruebasOtorgamiento.Contexto(PruebasOtorgamiento.FechaBase.AddDays(1));
        var otp = PruebasOtorgamiento.NuevoId();

        beca.AceptarOferta(beca.Oferta!.Id, contexto, otp);

        Assert.Equal(EstadoOferta.Aceptada, beca.Oferta.Estado);
        Assert.Equal(contexto.Fecha, beca.Oferta.FechaRespuesta);
        Assert.Equal(otp, beca.Oferta.AutorizacionOtpAceptacionId);
        Assert.Equal(2, beca.Oferta.Version);
        Assert.Equal(EstadoBeca.Activa, beca.Estado);

        var historialOferta = beca.Historial.Last(registro =>
            registro.TipoEntidad == TipoEntidadHistorial.OfertaBeca);
        Assert.Equal(EstadoOferta.Aceptada, historialOferta.EstadoOfertaNuevo);
        Assert.True(historialOferta.EstadoTerminalIrreversible);
        Assert.Equal(otp, historialOferta.AutorizacionOtpId);
    }

    [Fact]
    public void Debe_RechazarLaOferta_Cuando_EstaPendiente()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var contexto = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddHours(4),
            "El postulante declinó la oferta.");

        beca.RechazarOferta(beca.Oferta!.Id, contexto);

        Assert.Equal(EstadoOferta.RechazadaPorPostulante, beca.Oferta.Estado);
        Assert.Equal(contexto.Justificacion, beca.Oferta.MotivoRechazoOAnulacion);
        Assert.Equal(EstadoBeca.Revocada, beca.Estado);
        Assert.Equal(TipoCierreBeca.OfertaRechazada, beca.TipoCierre);
        Assert.All(
            beca.Historial.Where(registro => registro.EstadoTerminalIrreversible),
            registro => Assert.True(registro.EstadoTerminalIrreversible));
    }

    [Fact]
    public void Debe_AnularLaOferta_Cuando_LaJustificacionEsValida()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var contexto = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddHours(5),
            "La coordinación anuló la oferta por error de cupo.",
            PruebasOtorgamiento.NuevoId());

        beca.AnularOferta(beca.Oferta!.Id, contexto);

        Assert.Equal(EstadoOferta.AnuladaPorCoordinacion, beca.Oferta.Estado);
        Assert.Equal(contexto.Justificacion, beca.Oferta.MotivoRechazoOAnulacion);
        Assert.Equal(EstadoBeca.Revocada, beca.Estado);
        Assert.Equal(TipoCierreBeca.OfertaAnulada, beca.TipoCierre);

        var historialBeca = Assert.Single(
            beca.Historial,
            registro => registro.TipoEntidad == TipoEntidadHistorial.Beca
                && registro.EstadoBecaNuevo == EstadoBeca.Revocada);
        Assert.Equal(contexto.Justificacion, historialBeca.JustificacionDetallada);
        Assert.Equal(contexto.AutorizacionOtpId, historialBeca.AutorizacionOtpId);
        Assert.True(historialBeca.EstadoTerminalIrreversible);
    }

    [Fact]
    public void Debe_ExpirarLaOferta_Cuando_EstaPendiente()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var contexto = PruebasOtorgamiento.Contexto(
            beca.Oferta!.FechaVencimientoAceptacion.AddSeconds(1));

        beca.ExpirarOferta(beca.Oferta.Id, contexto);

        Assert.Equal(EstadoOferta.Expirada, beca.Oferta.Estado);
        Assert.Equal(EstadoBeca.Revocada, beca.Estado);
        Assert.Equal(TipoCierreBeca.OfertaExpirada, beca.TipoCierre);
    }

    [Fact]
    public void Debe_RechazarLaAnulacion_Cuando_LaJustificacionEsCorta()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var historialAnterior = beca.Historial.Count;
        var eventosAnteriores = beca.EventosDominio.Count;

        Assert.Throws<JustificacionRequeridaException>(
            () => beca.AnularOferta(
                beca.Oferta!.Id,
                PruebasOtorgamiento.Contexto(justificacion: "corta")));

        Assert.Equal(EstadoBeca.Otorgada, beca.Estado);
        Assert.Equal(EstadoOferta.PendienteRespuesta, beca.Oferta!.Estado);
        Assert.Equal(historialAnterior, beca.Historial.Count);
        Assert.Equal(eventosAnteriores, beca.EventosDominio.Count);
    }

    [Theory]
    [InlineData(EstadoOferta.Aceptada)]
    [InlineData(EstadoOferta.RechazadaPorPostulante)]
    [InlineData(EstadoOferta.Expirada)]
    [InlineData(EstadoOferta.AnuladaPorCoordinacion)]
    public void Debe_RechazarNuevasTransiciones_Cuando_LaOfertaEsTerminal(EstadoOferta estadoTerminal)
    {
        var beca = CrearOfertaEnEstado(estadoTerminal);
        var oferta = beca.Oferta!;
        var contexto = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(2),
            Justificacion);
        var historialAnterior = beca.Historial.Count;
        var eventosAnteriores = beca.EventosDominio.Count;

        Assert.Throws<TransicionEstadoInvalidaException>(
            () => beca.AceptarOferta(oferta.Id, contexto));
        Assert.Throws<TransicionEstadoInvalidaException>(
            () => beca.RechazarOferta(oferta.Id, contexto));
        Assert.Throws<TransicionEstadoInvalidaException>(
            () => beca.AnularOferta(oferta.Id, contexto));
        Assert.Throws<TransicionEstadoInvalidaException>(
            () => beca.ExpirarOferta(oferta.Id, contexto));

        Assert.Equal(estadoTerminal, oferta.Estado);
        Assert.Equal(historialAnterior, beca.Historial.Count);
        Assert.Equal(eventosAnteriores, beca.EventosDominio.Count);
    }

    private static Beca CrearOfertaEnEstado(EstadoOferta estado)
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var contexto = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(1),
            Justificacion);

        switch (estado)
        {
            case EstadoOferta.Aceptada:
                beca.AceptarOferta(beca.Oferta!.Id, contexto);
                break;
            case EstadoOferta.RechazadaPorPostulante:
                beca.RechazarOferta(beca.Oferta!.Id, contexto);
                break;
            case EstadoOferta.Expirada:
                beca.ExpirarOferta(
                    beca.Oferta!.Id,
                    PruebasOtorgamiento.Contexto(beca.Oferta!.FechaVencimientoAceptacion.AddSeconds(1)));
                break;
            case EstadoOferta.AnuladaPorCoordinacion:
                beca.AnularOferta(beca.Oferta!.Id, contexto);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(estado));
        }

        return beca;
    }
}
