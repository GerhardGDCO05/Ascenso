using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Domain.Tests.Enums;

public sealed class EnumeracionesTests
{
    [Fact]
    public void Debe_TenerLosSeisEstadosExactos_Cuando_SeInspeccionaEstadoBeca()
    {
        var esperados = new[]
        {
            "Otorgada", "Activa", "Suspendida", "Renovada", "Concluida", "Revocada"
        };

        Assert.Equal(esperados, Enum.GetNames<EstadoBeca>());
        Assert.Equal(6, Enum.GetValues<EstadoBeca>().Length);
    }

    [Fact]
    public void Debe_TenerLosCincoEstadosExactos_Cuando_SeInspeccionaEstadoOferta()
    {
        var esperados = new[]
        {
            "PendienteRespuesta",
            "Aceptada",
            "RechazadaPorPostulante",
            "Expirada",
            "AnuladaPorCoordinacion"
        };

        Assert.Equal(esperados, Enum.GetNames<EstadoOferta>());
        Assert.Equal(5, Enum.GetValues<EstadoOferta>().Length);
    }

    [Fact]
    public void Debe_TenerLosCuatroEstadosExactos_Cuando_SeInspeccionaEstadoCompromiso()
    {
        var esperados = new[]
        {
            "PendienteCarga", "EnRevision", "Aprobado", "Rechazado"
        };

        Assert.Equal(esperados, Enum.GetNames<EstadoCompromiso>());
        Assert.Equal(4, Enum.GetValues<EstadoCompromiso>().Length);
    }

    [Fact]
    public void Debe_GenerarLosCodigosLiterales_Cuando_SeTraducenLosEstadosDeBeca()
    {
        Assert.Equal("OTORGADA", EstadoBeca.Otorgada.ACodigo());
        Assert.Equal("ACTIVA", EstadoBeca.Activa.ACodigo());
        Assert.Equal("SUSPENDIDA", EstadoBeca.Suspendida.ACodigo());
        Assert.Equal("RENOVADA", EstadoBeca.Renovada.ACodigo());
        Assert.Equal("CONCLUIDA", EstadoBeca.Concluida.ACodigo());
        Assert.Equal("REVOCADA", EstadoBeca.Revocada.ACodigo());
    }

    [Fact]
    public void Debe_GenerarLosCodigosLiterales_Cuando_SeTraducenLasOfertasYCompromisos()
    {
        Assert.Equal("PENDIENTE_RESPUESTA", EstadoOferta.PendienteRespuesta.ACodigo());
        Assert.Equal("ACEPTADA", EstadoOferta.Aceptada.ACodigo());
        Assert.Equal("RECHAZADA_POR_POSTULANTE", EstadoOferta.RechazadaPorPostulante.ACodigo());
        Assert.Equal("EXPIRADA", EstadoOferta.Expirada.ACodigo());
        Assert.Equal("ANULADA_POR_COORDINACION", EstadoOferta.AnuladaPorCoordinacion.ACodigo());

        Assert.Equal("PENDIENTE_CARGA", EstadoCompromiso.PendienteCarga.ACodigo());
        Assert.Equal("EN_REVISION", EstadoCompromiso.EnRevision.ACodigo());
        Assert.Equal("APROBADO", EstadoCompromiso.Aprobado.ACodigo());
        Assert.Equal("RECHAZADO", EstadoCompromiso.Rechazado.ACodigo());
    }

    [Fact]
    public void Debe_GenerarLosCodigosLiterales_Cuando_SeTraducenLosTiposDelDominio()
    {
        Assert.Equal("OFERTA_RECHAZADA", TipoCierreBeca.OfertaRechazada.ACodigo());
        Assert.Equal("OFERTA_EXPIRADA", TipoCierreBeca.OfertaExpirada.ACodigo());
        Assert.Equal("OFERTA_ANULADA", TipoCierreBeca.OfertaAnulada.ACodigo());
        Assert.Equal("REVOCACION_DEFINITIVA", TipoCierreBeca.RevocacionDefinitiva.ACodigo());

        Assert.Equal("TOTAL", TipoCobertura.Total.ACodigo());
        Assert.Equal("PARCIAL", TipoCobertura.Parcial.ACodigo());
        Assert.Equal("MONTO_FIJO", TipoCobertura.MontoFijo.ACodigo());

        Assert.Equal("AHORRO", TipoCuentaBancaria.Ahorro.ACodigo());
        Assert.Equal("CORRIENTE", TipoCuentaBancaria.Corriente.ACodigo());

        Assert.Equal("CONFIRMAR_DESEMBOLSO", TipoOperacionOTP.ConfirmarDesembolso.ACodigo());
        Assert.Equal("REVOCAR_BECA", TipoOperacionOTP.RevocarBeca.ACodigo());
        Assert.Equal("MODIFICAR_DATOS_PAGO", TipoOperacionOTP.ModificarDatosPago.ACodigo());
        Assert.Equal("EMITIR_OFERTA_BECA", TipoOperacionOTP.EmitirOfertaBeca.ACodigo());
        Assert.Equal("ANULAR_OFERTA_BECA", TipoOperacionOTP.AnularOfertaBeca.ACodigo());

        Assert.Equal("LLAMADO_ATENCION", ConsecuenciaPermanencia.LlamadoAtencion.ACodigo());
        Assert.Equal("SUSPENSION", ConsecuenciaPermanencia.Suspension.ACodigo());
        Assert.Equal("REVOCACION", ConsecuenciaPermanencia.Revocacion.ACodigo());
    }

    [Fact]
    public void Debe_RechazarValoresNoDefinidos_Cuando_SeTraduceUnCodigo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((EstadoBeca)999).ACodigo());
    }
}
