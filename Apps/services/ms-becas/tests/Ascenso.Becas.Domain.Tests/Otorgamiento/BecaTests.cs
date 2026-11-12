using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;

namespace Ascenso.Becas.Domain.Tests.Otorgamiento;

public sealed class BecaTests
{
    [Fact]
    public void Debe_CrearUnaBecaOtorgada_ConLosCamposIniciales()
    {
        var datos = PruebasOtorgamiento.DatosBeca(
            cobertura: PruebasOtorgamiento.Cobertura(TipoCobertura.MontoFijo, 0m, 750m),
            montoComprometido: 750m);
        var contexto = PruebasOtorgamiento.Contexto();

        var beca = Beca.CrearOtorgada(datos, contexto);

        Assert.NotEqual(Guid.Empty, beca.Id);
        Assert.Equal(7, beca.Id.Version);
        Assert.Equal(beca.Id, beca.CadenaBecaId);
        Assert.Null(beca.BecaAnteriorId);
        Assert.Equal(EstadoBeca.Otorgada, beca.Estado);
        Assert.Null(beca.TipoCierre);
        Assert.Equal(0, beca.PeriodosCursados);
        Assert.Equal(0m, beca.MontoDesembolsadoPeriodoActual);
        Assert.False(beca.TieneRenovacionEnCurso);
        Assert.False(beca.HabilitadaParaDesembolso);
        Assert.False(beca.CompromisoFirmadoAprobado);
        Assert.Equal(datos.CostoMatriculaReferenciaSnapshotId, beca.CostoMatriculaReferenciaSnapshotId);
        Assert.Equal(datos.MontoComprometidoPeriodoActual, beca.MontoComprometidoPeriodoActual);
        Assert.Equal(datos.CoberturaAplicada, beca.CoberturaAplicada);
        Assert.Equal(datos.DuracionTotalPeriodos, beca.DuracionTotalPeriodos);
        Assert.Equal(datos.PeriodoOtorgamientoId, beca.PeriodoVigenteActualId);
        Assert.Equal(DateOnly.FromDateTime(PruebasOtorgamiento.FechaBase.UtcDateTime), beca.FechaOtorgamiento);
        Assert.Null(beca.FechaActivacion);
        Assert.Null(beca.FechaConclusionORevocacion);
        Assert.Null(beca.Oferta);
        Assert.Null(beca.Compromiso);
        Assert.Equal(1, beca.Version);
        Assert.Single(beca.Historial);

        var historial = Assert.Single(beca.Historial);
        Assert.Equal(TipoEntidadHistorial.Beca, historial.TipoEntidad);
        Assert.Null(historial.EstadoBecaAnterior);
        Assert.Equal(EstadoBeca.Otorgada, historial.EstadoBecaNuevo);
        Assert.Equal(contexto.UsuarioAutorId, historial.UsuarioAutorId);
        Assert.False(historial.EstadoTerminalIrreversible);
    }

    [Fact]
    public void Debe_InicializarElCompromisoPendiente_Cuando_ElProgramaLoExige()
    {
        var beca = PruebasOtorgamiento.CrearBeca(
            PruebasOtorgamiento.DatosBeca(requiereCompromiso: true));

        Assert.NotNull(beca.Compromiso);
        Assert.Equal(beca.Id, beca.Compromiso.BecaId);
        Assert.Equal(EstadoCompromiso.PendienteCarga, beca.Compromiso.Estado);
        Assert.Equal(0, beca.Compromiso.NumeroIntentoCarga);
        Assert.Null(beca.Compromiso.UrlPdfFirmadoFirebase);
        Assert.Null(beca.Compromiso.HashDocumentoSha256);
    }

    [Fact]
    public void Debe_RechazarUnaDuracionNoPositiva_Cuando_SeCreaLaBeca()
    {
        var datos = PruebasOtorgamiento.DatosBeca(duracion: 0);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Beca.CrearOtorgada(datos, PruebasOtorgamiento.Contexto()));
    }

    [Fact]
    public void Debe_RechazarUnMontoComprometidoNegativo_Cuando_SeCreaLaBeca()
    {
        var datos = PruebasOtorgamiento.DatosBeca(montoComprometido: -0.01m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Beca.CrearOtorgada(datos, PruebasOtorgamiento.Contexto()));
    }

    [Fact]
    public void Debe_RechazarUnIdentificadorQueNoSeaUuidV7_Cuando_SeCreaLaBeca()
    {
        var datos = PruebasOtorgamiento.DatosBeca();

        Assert.Throws<ArgumentException>(
            () => Beca.CrearOtorgada(datos, PruebasOtorgamiento.Contexto(), Guid.NewGuid()));
    }

    [Fact]
    public void Debe_ConservarLasJustificacionesYElActor_Cuando_SeRegistraElHistorial()
    {
        var contexto = PruebasOtorgamiento.Contexto(justificacion: "  Justificación válida  ");
        var beca = PruebasOtorgamiento.CrearBecaConOferta(contexto: contexto);
        var aceptacion = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(2),
            "Aceptación registrada por el postulante");

        beca.AceptarOferta(beca.Oferta!.Id, aceptacion);

        var ultimo = beca.Historial.Last();
        Assert.Equal(EstadoBeca.Activa, ultimo.EstadoBecaNuevo);
        Assert.Equal("Aceptación registrada por el postulante", ultimo.JustificacionDetallada);
        Assert.Equal(aceptacion.UsuarioAutorId, ultimo.UsuarioAutorId);
        Assert.Equal(aceptacion.CorrelacionId, ultimo.CorrelacionId);
    }
}
