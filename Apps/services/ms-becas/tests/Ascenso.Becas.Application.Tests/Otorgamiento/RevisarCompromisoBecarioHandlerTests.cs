using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Comandos.RevisarCompromisoBecario;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;
using FluentAssertions;

namespace Ascenso.Becas.Application.Tests.Otorgamiento;

public sealed class RevisarCompromisoBecarioHandlerTests
{
    [Fact]
    public async Task Debe_AprobarElCompromiso_Cuando_EstaEnRevision()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaEnRevision(escenario);

        var resultado = await escenario.CrearRevisarCompromiso().HandleAsync(
            new RevisarCompromisoBecarioCommand(
                beca.Compromiso!.Id,
                Aprobar: true,
                Observaciones: "Documento conforme."));

        resultado.CompromisoId.Should().Be(beca.Compromiso.Id);
        resultado.Estado.Should().Be("APROBADO");
        resultado.HabilitadaParaDesembolso.Should().BeTrue();
        beca.CompromisoFirmadoAprobado.Should().BeTrue();
        beca.HabilitadaParaDesembolso.Should().BeTrue();
        beca.Compromiso.Estado.Should().Be(EstadoCompromiso.Aprobado);
        beca.Compromiso.ObservacionesRevision.Should().Be("Documento conforme.");
        escenario.Unidad.Confirmaciones.Should().Be(1);
        escenario.Outbox.Confirmados.Should()
            .ContainSingle(evento => evento is CompromisoBecarioAprobadoEvento);
    }

    [Fact]
    public async Task Debe_RechazarElCompromiso_Cuando_SeInformanObservaciones()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaEnRevision(escenario);

        var resultado = await escenario.CrearRevisarCompromiso().HandleAsync(
            new RevisarCompromisoBecarioCommand(
                beca.Compromiso!.Id,
                Aprobar: false,
                Observaciones: "La firma no coincide con el documento."));

        resultado.Estado.Should().Be("RECHAZADO");
        resultado.HabilitadaParaDesembolso.Should().BeFalse();
        beca.CompromisoFirmadoAprobado.Should().BeFalse();
        beca.HabilitadaParaDesembolso.Should().BeFalse();
        beca.Compromiso.Estado.Should().Be(EstadoCompromiso.Rechazado);
        escenario.Outbox.Confirmados.Should()
            .ContainSingle(evento => evento is CompromisoBecarioRechazadoEvento);
    }

    [Fact]
    public async Task Debe_RechazarLaRevision_Cuando_ElRechazoNoTieneObservaciones()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaEnRevision(escenario);

        var accion = () => escenario.CrearRevisarCompromiso().HandleAsync(
            new RevisarCompromisoBecarioCommand(
                beca.Compromiso!.Id,
                Aprobar: false));

        var excepcion = await accion.Should().ThrowAsync<SolicitudInvalidaException>();

        excepcion.Which.Codigo.Should().Be("SOLICITUD_INVALIDA");
        beca.Compromiso!.Estado.Should().Be(EstadoCompromiso.EnRevision);
        escenario.Unidad.Confirmaciones.Should().Be(0);
    }

    [Fact]
    public async Task Debe_RetornarNoEncontrado_Cuando_ElCompromisoNoExiste()
    {
        var escenario = new EscenarioOtorgamiento();

        var accion = () => escenario.CrearRevisarCompromiso().HandleAsync(
            new RevisarCompromisoBecarioCommand(
                Guid.CreateVersion7(),
                Aprobar: true));

        var excepcion = await accion.Should().ThrowAsync<RecursoNoEncontradoException>();

        excepcion.Which.Recurso.Should().Be("compromiso");
        escenario.Unidad.NivelesSolicitados.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_RechazarLaRevision_Cuando_ElCompromisoNoEstaEnRevision()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = DatosAplicacion.CrearBecaActiva(
            escenario.PostulacionId,
            escenario.ProgramaBecaId,
            escenario.PeriodoAcademicoId,
            escenario.EstudianteId,
            requiereCompromisoFirmado: true);
        escenario.Repositorio.Becas.Add(beca);
        escenario.Compromisos.Compromisos.Add(beca.Compromiso!);

        var accion = () => escenario.CrearRevisarCompromiso().HandleAsync(
            new RevisarCompromisoBecarioCommand(
                beca.Compromiso!.Id,
                Aprobar: true));

        await accion.Should().ThrowAsync<TransicionEstadoInvalidaException>();
        beca.Compromiso!.Estado.Should().Be(EstadoCompromiso.PendienteCarga);
        escenario.Unidad.Confirmaciones.Should().Be(0);
    }

    private static Beca CrearBecaEnRevision(EscenarioOtorgamiento escenario)
    {
        var beca = DatosAplicacion.CrearBecaActivaConCompromisoEnRevision(
            escenario.PostulacionId,
            escenario.ProgramaBecaId,
            escenario.PeriodoAcademicoId,
            escenario.EstudianteId);
        escenario.Repositorio.Becas.Add(beca);
        escenario.Compromisos.Compromisos.Add(beca.Compromiso!);
        return beca;
    }
}
