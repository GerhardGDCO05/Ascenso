using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Transiciones;

namespace Ascenso.Becas.Domain.Tests.Transiciones;

public sealed class TransicionesTests
{
    [Fact]
    public void Debe_PermitirSoloLasTransicionesDeLaTabla_Cuando_SeEvaluaBeca()
    {
        var esperadas = new HashSet<(EstadoBeca Origen, EstadoBeca Destino)>
        {
            (EstadoBeca.Otorgada, EstadoBeca.Activa),
            (EstadoBeca.Otorgada, EstadoBeca.Revocada),
            (EstadoBeca.Activa, EstadoBeca.Suspendida),
            (EstadoBeca.Activa, EstadoBeca.Renovada),
            (EstadoBeca.Activa, EstadoBeca.Concluida),
            (EstadoBeca.Activa, EstadoBeca.Revocada),
            (EstadoBeca.Suspendida, EstadoBeca.Activa),
            (EstadoBeca.Suspendida, EstadoBeca.Revocada)
        };

        foreach (var origen in Enum.GetValues<EstadoBeca>())
        {
            foreach (var destino in Enum.GetValues<EstadoBeca>())
            {
                Assert.Equal(
                    esperadas.Contains((origen, destino)),
                    TransicionesBeca.Tabla.EsPermitida(origen, destino));
            }
        }

        Assert.Equal(esperadas.Count, TransicionesBeca.Tabla.Permitidas.Count);
    }

    [Fact]
    public void Debe_ReconocerLosEstadosTerminales_Cuando_SeEvaluaBeca()
    {
        Assert.True(TransicionesBeca.Tabla.EsTerminal(EstadoBeca.Renovada));
        Assert.True(TransicionesBeca.Tabla.EsTerminal(EstadoBeca.Concluida));
        Assert.True(TransicionesBeca.Tabla.EsTerminal(EstadoBeca.Revocada));
        Assert.False(TransicionesBeca.Tabla.EsTerminal(EstadoBeca.Otorgada));
        Assert.False(TransicionesBeca.Tabla.EsTerminal(EstadoBeca.Activa));
        Assert.False(TransicionesBeca.Tabla.EsTerminal(EstadoBeca.Suspendida));
    }

    [Fact]
    public void Debe_LanzarTransicionInvalida_Cuando_LaDuplaNoEstaPermitida()
    {
        Assert.Throws<TransicionEstadoInvalidaException>(
            () => TransicionesBeca.Tabla.Validar(EstadoBeca.Otorgada, EstadoBeca.Suspendida));
        Assert.Throws<TransicionEstadoInvalidaException>(
            () => TransicionesBeca.Tabla.Validar(EstadoBeca.Concluida, EstadoBeca.Activa));
    }

    [Fact]
    public void Debe_PermitirSoloLasRespuestasTerminales_Cuando_SeEvaluaOferta()
    {
        var esperadas = new HashSet<(EstadoOferta Origen, EstadoOferta Destino)>
        {
            (EstadoOferta.PendienteRespuesta, EstadoOferta.Aceptada),
            (EstadoOferta.PendienteRespuesta, EstadoOferta.RechazadaPorPostulante),
            (EstadoOferta.PendienteRespuesta, EstadoOferta.Expirada),
            (EstadoOferta.PendienteRespuesta, EstadoOferta.AnuladaPorCoordinacion)
        };

        foreach (var origen in Enum.GetValues<EstadoOferta>())
        {
            foreach (var destino in Enum.GetValues<EstadoOferta>())
            {
                Assert.Equal(
                    esperadas.Contains((origen, destino)),
                    TransicionesOfertaBeca.Tabla.EsPermitida(origen, destino));
            }
        }

        Assert.All(
            Enum.GetValues<EstadoOferta>().Where(estado => estado != EstadoOferta.PendienteRespuesta),
            estado => Assert.True(TransicionesOfertaBeca.Tabla.EsTerminal(estado)));
    }

    [Fact]
    public void Debe_PermitirLaReSubidaTrasRechazo_Cuando_SeEvaluaCompromiso()
    {
        var esperadas = new HashSet<(EstadoCompromiso Origen, EstadoCompromiso Destino)>
        {
            (EstadoCompromiso.PendienteCarga, EstadoCompromiso.EnRevision),
            (EstadoCompromiso.EnRevision, EstadoCompromiso.Aprobado),
            (EstadoCompromiso.EnRevision, EstadoCompromiso.Rechazado),
            (EstadoCompromiso.Rechazado, EstadoCompromiso.EnRevision)
        };

        foreach (var origen in Enum.GetValues<EstadoCompromiso>())
        {
            foreach (var destino in Enum.GetValues<EstadoCompromiso>())
            {
                Assert.Equal(
                    esperadas.Contains((origen, destino)),
                    TransicionesCompromisoBecario.Tabla.EsPermitida(origen, destino));
            }
        }

        Assert.True(TransicionesCompromisoBecario.Tabla.EsTerminal(EstadoCompromiso.Aprobado));
        Assert.False(TransicionesCompromisoBecario.Tabla.EsTerminal(EstadoCompromiso.Rechazado));
    }

    [Fact]
    public void Debe_RechazarTablasVacias_Cuando_SeConstruyeUnaTablaDeTransiciones()
    {
        Assert.Throws<ArgumentException>(
            () => new TablaTransiciones<EstadoBeca>(
                Array.Empty<(EstadoBeca Origen, EstadoBeca Destino)>(),
                Array.Empty<EstadoBeca>()));
    }
}
