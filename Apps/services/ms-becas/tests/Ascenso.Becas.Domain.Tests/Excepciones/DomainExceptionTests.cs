using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;

namespace Ascenso.Becas.Domain.Tests.Excepciones;

public sealed class DomainExceptionTests
{
    private sealed class ExcepcionPrueba : DomainException
    {
        public ExcepcionPrueba(string codigo, int estadoHttp, string mensaje)
            : base(codigo, estadoHttp, mensaje)
        {
        }
    }

    [Fact]
    public void Debe_ExponerCodigoYEstatus_Cuando_SeLanzaTransicionInvalida()
    {
        var excepcion = new TransicionEstadoInvalidaException(EstadoBeca.Otorgada, EstadoBeca.Suspendida);

        Assert.Equal("TRANSICION_INVALIDA", excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
        Assert.Equal(nameof(EstadoBeca.Otorgada), excepcion.EstadoActual);
        Assert.Equal(nameof(EstadoBeca.Suspendida), excepcion.EstadoNuevo);
        Assert.Contains("OTORGADA", excepcion.Message.ToUpperInvariant());
    }

    [Fact]
    public void Debe_ExponerCodigoYEstatus_Cuando_LaOfertaEstaVencida()
    {
        var ofertaId = Guid.NewGuid();
        var vencimiento = DateTimeOffset.UtcNow.AddDays(-1);

        var excepcion = new OfertaVencidaException(ofertaId, vencimiento, DateTimeOffset.UtcNow);

        Assert.Equal("OFERTA_VENCIDA", excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
        Assert.Equal(ofertaId, excepcion.OfertaId);
        Assert.Equal(vencimiento.ToUniversalTime(), excepcion.FechaVencimiento);
    }

    [Fact]
    public void Debe_ExponerCodigoYEstatus_Cuando_ExisteDobleBeneficio()
    {
        var estudianteId = Guid.NewGuid();

        var excepcion = new DobleBeneficioException(estudianteId, "Ya posee una beca total activa.");

        Assert.Equal("DOBLE_BENEFICIO", excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
        Assert.Equal(estudianteId, excepcion.EstudianteId);
        Assert.Contains("beca total activa", excepcion.Message);
    }

    [Fact]
    public void Debe_ExponerCodigoYEstatus_Cuando_FaltaLaJustificacion()
    {
        var excepcion = new JustificacionRequeridaException("ANULAR_OFERTA", 10);

        Assert.Equal("JUSTIFICACION_REQUERIDA", excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
        Assert.Equal("ANULAR_OFERTA", excepcion.Operacion);
        Assert.Equal(10, excepcion.LongitudMinima);
        Assert.Contains("10", excepcion.Message);
    }

    [Fact]
    public void Debe_ExponerCodigoYEstatus_Cuando_LaPostulacionNoEsAdjudicable()
    {
        var postulacionId = Guid.NewGuid();

        var excepcion = new PostulacionNoAdjudicableException(postulacionId, "Sin cupo disponible.");

        Assert.Equal("POSTULACION_NO_ADJUDICABLE", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.Equal(postulacionId, excepcion.PostulacionId);
        Assert.Equal("Sin cupo disponible.", excepcion.Motivo);
    }

    [Fact]
    public void Debe_ExponerCodigoYEstatus_Cuando_ElCompromisoNoEstaFirmado()
    {
        var becaId = Guid.NewGuid();

        var excepcion = new CompromisoNoFirmadoException(becaId);

        Assert.Equal("COMPROMISO_NO_FIRMADO", excepcion.Codigo);
        Assert.Equal(412, excepcion.EstadoHttp);
        Assert.Equal(becaId, excepcion.BecaId);
    }

    [Fact]
    public void Debe_RechazarParametrosInvalidos_Cuando_SeConstruyeUnaExcepcionDeDominio()
    {
        Assert.Throws<ArgumentException>(() => new ExcepcionPrueba(" ", 400, "mensaje"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExcepcionPrueba("CODIGO", 399, "mensaje"));
    }
}
