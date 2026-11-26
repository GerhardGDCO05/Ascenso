using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Datos;
using Ascenso.Becas.Domain.Tests.Otorgamiento;

namespace Ascenso.Becas.Domain.Tests.Renovacion;

public sealed class BecaSucesoraTests
{
    [Fact]
    public void CreaSucesoraActivaConCadenaYFotografiasDelNuevoPeriodo()
    {
        var anterior = PruebasOtorgamiento.CrearBecaActiva();
        var contexto = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(10),
            "Renovación aprobada por coordinación académica.");
        anterior.IniciarRenovacion(contexto);
        var datos = DatosSucesora();

        var sucesora = anterior.CrearSucesoraPorRenovacion(datos, contexto);
        anterior.Renovar(contexto);

        Assert.Equal(EstadoBeca.Renovada, anterior.Estado);
        Assert.Equal(EstadoBeca.Activa, sucesora.Estado);
        Assert.Equal(anterior.CadenaBecaId, sucesora.CadenaBecaId);
        Assert.Equal(anterior.Id, sucesora.BecaAnteriorId);
        Assert.Equal(anterior.PeriodosCursados + 1, sucesora.PeriodosCursados);
        Assert.Equal(datos.PeriodoSolicitadoId, sucesora.PeriodoVigenteActualId);
        Assert.Equal(datos.CostoMatriculaReferenciaSnapshotId,
            sucesora.CostoMatriculaReferenciaSnapshotId);
        Assert.Equal(datos.CondicionesSnapshot,
            sucesora.CondicionesPermanenciaSnapshot);
        Assert.Equal(datos.CoberturaAplicada, sucesora.CoberturaAplicada);
        Assert.Equal(datos.MontoComprometidoPeriodo,
            sucesora.MontoComprometidoPeriodoActual);
        Assert.True(sucesora.HabilitadaParaDesembolso);
        Assert.Null(sucesora.Oferta);
        Assert.False(sucesora.TieneRenovacionEnCurso);
        Assert.Contains(sucesora.Historial, valor =>
            valor.EstadoBecaAnterior is null
            && valor.EstadoBecaNuevo == EstadoBeca.Activa);
    }

    [Fact]
    public void HeredaCompromisoAprobadoSoloSiNoSeExigeNuevaFirma()
    {
        var anterior = PruebasOtorgamiento.CrearBecaActiva(
            PruebasOtorgamiento.DatosBeca(requiereCompromiso: true));
        anterior.CargarCompromiso(
            "https://storage.example/becas/compromiso.pdf",
            PruebasOtorgamiento.Hash('c'), 1024, "application/pdf",
            "190.85.12.34", PruebasOtorgamiento.Contexto(
                PruebasOtorgamiento.FechaBase.AddDays(2),
                "Carga de compromiso firmado."));
        anterior.AprobarCompromiso("Documento conforme.",
            PruebasOtorgamiento.Contexto(
                PruebasOtorgamiento.FechaBase.AddDays(3),
                "Aprobación de compromiso firmado."));
        var contexto = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(10),
            "Renovación aprobada por coordinación.");
        anterior.IniciarRenovacion(contexto);

        var heredada = anterior.CrearSucesoraPorRenovacion(
            DatosSucesora() with { RequiereCompromisoFirmado = true }, contexto);
        var nuevaFirma = anterior.CrearSucesoraPorRenovacion(
            DatosSucesora() with
            {
                RequiereCompromisoFirmado = true,
                RequiereNuevaFirma = true,
                UrlPlantillaCompromiso = "https://storage.example/becas/nueva-plantilla.pdf"
            }, contexto);

        Assert.True(heredada.CompromisoFirmadoAprobado);
        Assert.True(heredada.HabilitadaParaDesembolso);
        Assert.Null(heredada.Compromiso);
        Assert.False(nuevaFirma.CompromisoFirmadoAprobado);
        Assert.False(nuevaFirma.HabilitadaParaDesembolso);
        Assert.Equal(EstadoCompromiso.PendienteCarga, nuevaFirma.Compromiso?.Estado);
    }

    [Fact]
    public void RechazaSucesoraSinRenovacionReservaOCostoConfirmados()
    {
        var anterior = PruebasOtorgamiento.CrearBecaActiva();
        var contexto = PruebasOtorgamiento.Contexto();
        var datos = DatosSucesora();
        Assert.Throws<InvalidOperationException>(() =>
            anterior.CrearSucesoraPorRenovacion(datos, contexto));

        anterior.IniciarRenovacion(contexto);
        Assert.Throws<ArgumentException>(() => anterior.CrearSucesoraPorRenovacion(
            datos with { ReservaPresupuestoConfirmadaId = Guid.Empty }, contexto));
        Assert.Throws<ArgumentException>(() => anterior.CrearSucesoraPorRenovacion(
            datos with { CostoMatriculaReferenciaSnapshotId = Guid.Empty }, contexto));
        Assert.Throws<ArgumentException>(() => anterior.CrearSucesoraPorRenovacion(
            datos with { RequiereCompromisoFirmado = true, RequiereNuevaFirma = true }, contexto));
        Assert.Equal(EstadoBeca.Activa, anterior.Estado);
    }

    private static DatosBecaSucesora DatosSucesora()
        => new(Guid.CreateVersion7(), Guid.CreateVersion7(), 1500m,
            Guid.CreateVersion7(), PruebasOtorgamiento.Cobertura(
                porcentaje: 60m), PruebasOtorgamiento.Condiciones(),
            false, false, $"BECA-{Guid.NewGuid():N}"[..20], null);
}
