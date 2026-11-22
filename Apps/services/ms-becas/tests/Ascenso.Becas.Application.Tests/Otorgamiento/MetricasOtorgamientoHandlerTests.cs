using Ascenso.Becas.Application.Otorgamiento.Comandos.AceptarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.EmitirOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.ExpirarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.RechazarOfertaBeca;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using FluentAssertions;

namespace Ascenso.Becas.Application.Tests.Otorgamiento;

public sealed class MetricasOtorgamientoHandlerTests
{
    [Fact]
    public async Task Debe_IncrementarOfertasEmitidas_Cuando_LaEmisionEsExitosa()
    {
        var escenario = new EscenarioOtorgamiento();

        await escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        escenario.Metricas.Emitidas.Should().Be(1);
        escenario.Metricas.Aceptadas.Should().Be(0);
        escenario.Metricas.Rechazadas.Should().Be(0);
        escenario.Metricas.Expiradas.Should().Be(0);
    }

    [Fact]
    public async Task Debe_IncrementarOfertasAceptadas_Cuando_LaAceptacionEsExitosa()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearOfertaPendiente(escenario);

        await escenario.CrearAceptar().HandleAsync(
            new AceptarOfertaBecaCommand(beca.Oferta!.Id));

        escenario.Metricas.Aceptadas.Should().Be(1);
        escenario.Metricas.Emitidas.Should().Be(0);
    }

    [Fact]
    public async Task Debe_IncrementarOfertasRechazadas_Cuando_ElRechazoEsExitoso()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearOfertaPendiente(escenario);

        await escenario.CrearRechazar().HandleAsync(
            new RechazarOfertaBecaCommand(
                beca.Oferta!.Id,
                "El postulante decidió no continuar."));

        escenario.Metricas.Rechazadas.Should().Be(1);
        escenario.Metricas.Emitidas.Should().Be(0);
    }

    [Fact]
    public async Task Debe_IncrementarOfertasExpiradas_Cuando_LaOfertaVence()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearOfertaPendiente(escenario);
        escenario.Reloj.UtcNow = DatosAplicacion.FechaBase.AddDays(16);

        var expirada = await escenario.CrearExpirar().HandleAsync(
            new ExpirarOfertaBecaCommand(beca.Oferta!.Id));

        expirada.Should().BeTrue();
        escenario.Metricas.Expiradas.Should().Be(1);
        escenario.Metricas.Emitidas.Should().Be(0);
    }

    [Fact]
    public async Task Debe_NoIncrementarOfertasEmitidas_Cuando_LaEmisionFalla()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Postulaciones.Resultado = escenario.Postulaciones.Resultado! with
        {
            Adjudicable = false,
            MotivoNoAdjudicable = "sin cupo"
        };

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        await accion.Should().ThrowAsync<Exception>();
        escenario.Metricas.Emitidas.Should().Be(0);
    }

    private static Beca CrearOfertaPendiente(EscenarioOtorgamiento escenario)
    {
        var beca = DatosAplicacion.CrearBecaConOferta(
            escenario.PostulacionId,
            escenario.ProgramaBecaId,
            escenario.PeriodoAcademicoId,
            escenario.EstudianteId);
        escenario.Repositorio.Becas.Add(beca);
        return beca;
    }
}
