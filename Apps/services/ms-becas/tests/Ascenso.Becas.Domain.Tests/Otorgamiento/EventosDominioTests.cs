using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;

namespace Ascenso.Becas.Domain.Tests.Otorgamiento;

public sealed class EventosDominioTests
{
    private const string Justificacion = "Justificación suficiente para la operación.";

    [Fact]
    public void Debe_EmitirUnSoloEventoDeEmision_Cuando_SeEmiteLaOferta()
    {
        var beca = PruebasOtorgamiento.CrearBeca();
        var contexto = PruebasOtorgamiento.Contexto(PruebasOtorgamiento.FechaBase.AddHours(1));

        var oferta = beca.EmitirOferta(
            PruebasOtorgamiento.DatosOferta(beca.PostulacionOrigenId),
            contexto);

        var evento = Assert.Single(beca.EventosDominio);
        var emision = Assert.IsType<OfertaBecaEmitidaEvento>(evento);
        Assert.Equal(beca.Id, emision.BecaId);
        Assert.Equal(oferta.Id, emision.OfertaId);
        Assert.Equal(beca.PostulacionOrigenId, emision.PostulacionId);
        Assert.Equal(beca.ProgramaBecaId, emision.ProgramaBecaId);
        Assert.Equal(beca.PeriodoVigenteActualId, emision.PeriodoAcademicoId);
        Assert.Equal(oferta.FechaVencimientoAceptacion, emision.FechaVencimientoAceptacion);
        Assert.Equal(contexto.CorrelacionId, emision.CorrelationId);
        Assert.Equal(contexto.Fecha, emision.OccurredAt);
    }

    [Fact]
    public void Debe_EmitirUnSoloEventoDeActivacion_Cuando_SeAceptaLaOferta()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var contexto = PruebasOtorgamiento.Contexto(PruebasOtorgamiento.FechaBase.AddDays(1));

        beca.AceptarOferta(beca.Oferta!.Id, contexto);

        Assert.Equal(2, beca.EventosDominio.Count);
        var evento = Assert.IsType<BecaActivadaEvento>(beca.EventosDominio.Last());
        Assert.Equal(beca.Id, evento.BecaId);
        Assert.Equal(beca.Oferta.Id, evento.OfertaId);
        Assert.Equal(beca.PostulacionOrigenId, evento.PostulacionId);
    }

    [Fact]
    public void Debe_EmitirUnSoloEventoDeRechazo_Cuando_ElPostulanteRechaza()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var contexto = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(1),
            Justificacion);

        beca.RechazarOferta(beca.Oferta!.Id, contexto);

        Assert.Equal(2, beca.EventosDominio.Count);
        var evento = Assert.IsType<OfertaBecaRechazadaEvento>(beca.EventosDominio.Last());
        Assert.Equal(beca.Oferta.Id, evento.OfertaId);
        Assert.Equal(beca.Id, evento.BecaId);
    }

    [Fact]
    public void Debe_EmitirUnSoloEventoDeAnulacion_Cuando_LaCoordinacionAnula()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var contexto = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(1),
            Justificacion);

        beca.AnularOferta(beca.Oferta!.Id, contexto);

        Assert.Equal(2, beca.EventosDominio.Count);
        var evento = Assert.IsType<OfertaBecaAnuladaEvento>(beca.EventosDominio.Last());
        Assert.Equal(beca.Oferta.Id, evento.OfertaId);
    }

    [Fact]
    public void Debe_EmitirUnSoloEventoDeExpiracion_Cuando_LaOfertaVence()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var contexto = PruebasOtorgamiento.Contexto(
            beca.Oferta!.FechaVencimientoAceptacion.AddSeconds(1));

        beca.ExpirarOferta(beca.Oferta.Id, contexto);

        Assert.Equal(2, beca.EventosDominio.Count);
        var evento = Assert.IsType<OfertaBecaExpiradaEvento>(beca.EventosDominio.Last());
        Assert.Equal(beca.Oferta.Id, evento.OfertaId);
    }

    [Fact]
    public void Debe_EmitirUnEventoPorCadaPasoDelCompromiso()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva(
            PruebasOtorgamiento.DatosBeca(requiereCompromiso: true));
        var eventosIniciales = beca.EventosDominio.Count;

        beca.CargarCompromiso(
            "https://storage.example/becas/compromiso.pdf",
            PruebasOtorgamiento.Hash('k'),
            "190.85.12.34",
            PruebasOtorgamiento.Contexto(
                PruebasOtorgamiento.FechaBase.AddDays(2),
                "Carga del compromiso firmado."));

        Assert.IsType<CompromisoBecarioCargadoEvento>(beca.EventosDominio.Last());
        Assert.Equal(eventosIniciales + 1, beca.EventosDominio.Count);

        beca.AprobarCompromiso(
            null,
            PruebasOtorgamiento.Contexto(
                PruebasOtorgamiento.FechaBase.AddDays(3),
                "Aprobación del compromiso firmado."));

        Assert.IsType<CompromisoBecarioAprobadoEvento>(beca.EventosDominio.Last());
        Assert.Equal(eventosIniciales + 2, beca.EventosDominio.Count);

        var becaParaRechazo = PruebasOtorgamiento.CrearBecaActiva(
            PruebasOtorgamiento.DatosBeca(requiereCompromiso: true));
        var eventosRechazoIniciales = becaParaRechazo.EventosDominio.Count;
        becaParaRechazo.CargarCompromiso(
            "https://storage.example/becas/compromiso-rechazo.pdf",
            PruebasOtorgamiento.Hash('l'),
            "190.85.12.35",
            PruebasOtorgamiento.Contexto(
                PruebasOtorgamiento.FechaBase.AddDays(2),
                "Carga del compromiso para rechazo."));
        becaParaRechazo.RechazarCompromiso(
            "Documento incompleto.",
            PruebasOtorgamiento.Contexto(
                PruebasOtorgamiento.FechaBase.AddDays(4),
                "Rechazo del compromiso firmado."));

        Assert.IsType<CompromisoBecarioRechazadoEvento>(becaParaRechazo.EventosDominio.Last());
        Assert.Equal(eventosRechazoIniciales + 2, becaParaRechazo.EventosDominio.Count);
    }

    [Fact]
    public void Debe_NoEmitirEventos_Cuando_UnaTransicionFalla()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var eventosAnteriores = beca.EventosDominio.Count;
        var contexto = PruebasOtorgamiento.Contexto(justificacion: "corta");

        Assert.Throws<JustificacionRequeridaException>(
            () => beca.AnularOferta(beca.Oferta!.Id, contexto));
        Assert.Throws<TransicionEstadoInvalidaException>(
            () => beca.Suspender(PruebasOtorgamiento.Contexto(
                PruebasOtorgamiento.FechaBase.AddDays(1),
                Justificacion)));

        Assert.Equal(eventosAnteriores, beca.EventosDominio.Count);
    }

    [Fact]
    public void Debe_NoEmitirEventos_Cuando_LaAceptacionEstaVencida()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var eventosAnteriores = beca.EventosDominio.Count;

        Assert.Throws<OfertaVencidaException>(
            () => beca.AceptarOferta(
                beca.Oferta!.Id,
                PruebasOtorgamiento.Contexto(
                    beca.Oferta.FechaVencimientoAceptacion.AddSeconds(1))));

        Assert.Equal(eventosAnteriores, beca.EventosDominio.Count);
    }
}
