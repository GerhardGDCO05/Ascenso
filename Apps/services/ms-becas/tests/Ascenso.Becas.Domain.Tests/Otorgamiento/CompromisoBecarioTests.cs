using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;

namespace Ascenso.Becas.Domain.Tests.Otorgamiento;

public sealed class CompromisoBecarioTests
{
    [Fact]
    public void Debe_CargarElCompromiso_Cuando_ElBecarioAdjuntaUnPdfValido()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva(
            PruebasOtorgamiento.DatosBeca(requiereCompromiso: true));
        var contexto = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(2),
            "Carga inicial del compromiso firmado.");

        beca.CargarCompromiso(
            "https://storage.example/becas/compromiso-1.pdf",
            PruebasOtorgamiento.Hash('b'),
            "190.85.12.34",
            contexto);

        var compromiso = beca.Compromiso!;
        Assert.Equal(EstadoCompromiso.EnRevision, compromiso.Estado);
        Assert.Equal(1, compromiso.NumeroIntentoCarga);
        Assert.Equal("https://storage.example/becas/compromiso-1.pdf", compromiso.UrlPdfFirmadoFirebase);
        Assert.Equal(PruebasOtorgamiento.Hash('b'), compromiso.HashDocumentoSha256);
        Assert.Equal("190.85.12.34", compromiso.IpCargaFirma);
        Assert.Equal(contexto.Fecha, compromiso.FechaCargaFirma);
        Assert.Null(compromiso.RevisadoPorCoordinadorId);
        Assert.Null(compromiso.FechaRevision);
        Assert.False(beca.CompromisoFirmadoAprobado);
        Assert.False(beca.HabilitadaParaDesembolso);
    }

    [Fact]
    public void Debe_AprobarElCompromisoYHabilitarDesembolsos_Cuando_LaRevisionEsFavorable()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva(
            PruebasOtorgamiento.DatosBeca(requiereCompromiso: true));
        CargarCompromiso(beca, 'c');
        var revision = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(3),
            "Documento revisado y aprobado por coordinación.");

        beca.AprobarCompromiso("Documento conforme.", revision);

        Assert.Equal(EstadoCompromiso.Aprobado, beca.Compromiso!.Estado);
        Assert.Equal(revision.UsuarioAutorId, beca.Compromiso.RevisadoPorCoordinadorId);
        Assert.Equal(revision.Fecha, beca.Compromiso.FechaRevision);
        Assert.Equal("Documento conforme.", beca.Compromiso.ObservacionesRevision);
        Assert.True(beca.CompromisoFirmadoAprobado);
        Assert.True(beca.HabilitadaParaDesembolso);
    }

    [Fact]
    public void Debe_PermitirUnaNuevaCarga_Cuando_ElIntentoAnteriorFueRechazado()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva(
            PruebasOtorgamiento.DatosBeca(requiereCompromiso: true));
        CargarCompromiso(beca, 'd');
        var rechazo = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(3),
            "El documento no cumple los requisitos.");

        beca.RechazarCompromiso("Falta la firma del representante legal.", rechazo);

        Assert.Equal(EstadoCompromiso.Rechazado, beca.Compromiso!.Estado);
        Assert.Equal("Falta la firma del representante legal.", beca.Compromiso.ObservacionesRevision);
        Assert.False(beca.CompromisoFirmadoAprobado);
        Assert.False(beca.HabilitadaParaDesembolso);

        var nuevaCarga = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(4),
            "Nuevo intento de carga del compromiso.");
        beca.CargarCompromiso(
            "https://storage.example/becas/compromiso-2.pdf",
            PruebasOtorgamiento.Hash('e'),
            "190.85.12.35",
            nuevaCarga);

        Assert.Equal(EstadoCompromiso.EnRevision, beca.Compromiso.Estado);
        Assert.Equal(2, beca.Compromiso.NumeroIntentoCarga);
        Assert.Null(beca.Compromiso.RevisadoPorCoordinadorId);
        Assert.Null(beca.Compromiso.FechaRevision);

        var aprobacion = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(5),
            "Segundo intento aprobado.");
        beca.AprobarCompromiso(null, aprobacion);

        Assert.True(beca.HabilitadaParaDesembolso);
    }

    [Fact]
    public void Debe_RechazarLaRecarga_Cuando_SeReutilizaElHashDelIntentoRechazado()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva(
            PruebasOtorgamiento.DatosBeca(requiereCompromiso: true));
        CargarCompromiso(beca, 'f');
        beca.RechazarCompromiso(
            "Documento ilegible.",
            PruebasOtorgamiento.Contexto(
                PruebasOtorgamiento.FechaBase.AddDays(3),
                "Rechazo del primer intento."));

        Assert.Throws<ArgumentException>(
            () => beca.CargarCompromiso(
                "https://storage.example/becas/compromiso-3.pdf",
                PruebasOtorgamiento.Hash('f'),
                "190.85.12.36",
                PruebasOtorgamiento.Contexto(PruebasOtorgamiento.FechaBase.AddDays(4))));

        Assert.Equal(EstadoCompromiso.Rechazado, beca.Compromiso!.Estado);
        Assert.Equal(1, beca.Compromiso.NumeroIntentoCarga);
    }

    [Fact]
    public void Debe_RechazarNuevasTransiciones_Cuando_ElCompromisoEstaAprobado()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva(
            PruebasOtorgamiento.DatosBeca(requiereCompromiso: true));
        CargarCompromiso(beca, 'g');
        beca.AprobarCompromiso(null, PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(3),
            "Aprobación del compromiso."));
        var contexto = PruebasOtorgamiento.Contexto(PruebasOtorgamiento.FechaBase.AddDays(4));

        Assert.Throws<TransicionEstadoInvalidaException>(
            () => beca.RechazarCompromiso("Observación tardía.", contexto));
        Assert.Throws<TransicionEstadoInvalidaException>(
            () => beca.CargarCompromiso(
                "https://storage.example/becas/compromiso-4.pdf",
                PruebasOtorgamiento.Hash('h'),
                "190.85.12.37",
                contexto));

        Assert.Equal(EstadoCompromiso.Aprobado, beca.Compromiso!.Estado);
    }

    [Fact]
    public void Debe_RechazarLaCarga_Cuando_ElProgramaNoExigeCompromiso()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();

        Assert.Throws<InvalidOperationException>(
            () => beca.CargarCompromiso(
                "https://storage.example/becas/compromiso.pdf",
                PruebasOtorgamiento.Hash('i'),
                "190.85.12.38",
                PruebasOtorgamiento.Contexto()));
    }

    [Fact]
    public void Debe_RechazarUnaIpInvalida_Cuando_SeCargaElCompromiso()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva(
            PruebasOtorgamiento.DatosBeca(requiereCompromiso: true));

        Assert.Throws<ArgumentException>(
            () => beca.CargarCompromiso(
                "https://storage.example/becas/compromiso.pdf",
                PruebasOtorgamiento.Hash('j'),
                "no-es-una-ip",
                PruebasOtorgamiento.Contexto()));
    }

    private static void CargarCompromiso(Ascenso.Becas.Domain.Otorgamiento.Agregados.Beca beca, char hash)
        => beca.CargarCompromiso(
            "https://storage.example/becas/compromiso.pdf",
            PruebasOtorgamiento.Hash(hash),
            "190.85.12.34",
            PruebasOtorgamiento.Contexto(
                PruebasOtorgamiento.FechaBase.AddDays(2),
                "Carga del compromiso firmado."));
}
