using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Comandos.AnularOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.ExpirarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.RechazarOfertaBeca;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;
using FluentAssertions;

namespace Ascenso.Becas.Application.Tests.Otorgamiento;

public sealed class RespuestaOfertaBecaHandlerTests
{
    [Fact]
    public async Task Debe_RechazarLaOferta_Cuando_EstaPendiente()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearOfertaPendiente(escenario);

        await escenario.CrearRechazar().HandleAsync(
            new RechazarOfertaBecaCommand(beca.Oferta!.Id, "No puedo continuar con el programa."));

        beca.Estado.Should().Be(EstadoBeca.Revocada);
        beca.TipoCierre.Should().Be(TipoCierreBeca.OfertaRechazada);
        beca.Oferta!.Estado.Should().Be(EstadoOferta.RechazadaPorPostulante);
        beca.Oferta.MotivoRechazoOAnulacion.Should().Be("No puedo continuar con el programa.");
        beca.HabilitadaParaDesembolso.Should().BeFalse();
        escenario.Unidad.Confirmaciones.Should().Be(1);
        escenario.Outbox.Confirmados.Should()
            .ContainSingle(evento => evento is OfertaBecaRechazadaEvento);
    }

    [Fact]
    public async Task Debe_RechazarElRechazo_Cuando_LaOfertaYaEstaCerrada()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearOfertaPendiente(escenario);
        var ofertaId = beca.Oferta!.Id;
        beca.RechazarOferta(ofertaId, DatosAplicacion.Contexto(justificacion: "Rechazo previo del postulante."));

        var accion = () => escenario.CrearRechazar().HandleAsync(
            new RechazarOfertaBecaCommand(ofertaId, "Segundo rechazo."));

        await accion.Should().ThrowAsync<TransicionEstadoInvalidaException>();
        escenario.Unidad.Confirmaciones.Should().Be(0);
    }

    [Fact]
    public async Task Debe_AnularLaOferta_Cuando_LaJustificacionEsSuficiente()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearOfertaPendiente(escenario);
        const string justificacion = "Anulación autorizada por el comité académico.";

        await escenario.CrearAnular().HandleAsync(
            new AnularOfertaBecaCommand(beca.Oferta!.Id, justificacion));

        beca.Estado.Should().Be(EstadoBeca.Revocada);
        beca.TipoCierre.Should().Be(TipoCierreBeca.OfertaAnulada);
        beca.Oferta!.Estado.Should().Be(EstadoOferta.AnuladaPorCoordinacion);
        beca.Oferta.MotivoRechazoOAnulacion.Should().Be(justificacion);
        escenario.Unidad.Confirmaciones.Should().Be(1);
        escenario.Outbox.Confirmados.Should()
            .ContainSingle(evento => evento is OfertaBecaAnuladaEvento);
    }

    [Fact]
    public async Task Debe_RechazarLaAnulacion_Cuando_LaJustificacionEsInsuficiente()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearOfertaPendiente(escenario);

        var accion = () => escenario.CrearAnular().HandleAsync(
            new AnularOfertaBecaCommand(beca.Oferta!.Id, "corta"));

        var excepcion = await accion.Should().ThrowAsync<JustificacionRequeridaException>();

        excepcion.Which.Codigo.Should().Be("JUSTIFICACION_REQUERIDA");
        beca.Estado.Should().Be(EstadoBeca.Otorgada);
        escenario.Unidad.Confirmaciones.Should().Be(0);
        escenario.Outbox.Descartado.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_ExpirarLaOferta_Cuando_EstaPendienteYVencida()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearOfertaPendiente(escenario);
        escenario.Reloj.UtcNow = DatosAplicacion.FechaBase.AddDays(16);

        var cambio = await escenario.CrearExpirar().HandleAsync(
            new ExpirarOfertaBecaCommand(beca.Oferta!.Id));

        cambio.Should().BeTrue();
        beca.Estado.Should().Be(EstadoBeca.Revocada);
        beca.TipoCierre.Should().Be(TipoCierreBeca.OfertaExpirada);
        beca.Oferta!.Estado.Should().Be(EstadoOferta.Expirada);
        escenario.Unidad.Confirmaciones.Should().Be(1);
        escenario.Outbox.Confirmados.Should()
            .ContainSingle(evento => evento is OfertaBecaExpiradaEvento);
    }

    [Fact]
    public async Task Debe_NoExpirarLaOferta_Cuando_AunNoVence()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearOfertaPendiente(escenario);
        escenario.Reloj.UtcNow = DatosAplicacion.FechaBase.AddDays(1);

        var cambio = await escenario.CrearExpirar().HandleAsync(
            new ExpirarOfertaBecaCommand(beca.Oferta!.Id));

        cambio.Should().BeFalse();
        beca.Estado.Should().Be(EstadoBeca.Otorgada);
        beca.Oferta!.Estado.Should().Be(EstadoOferta.PendienteRespuesta);
        escenario.Unidad.Guardados.Should().Be(0);
        escenario.Outbox.Confirmados.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_NoExpirarLaOferta_Cuando_YaFueAceptada()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearOfertaPendiente(escenario);
        beca.AceptarOferta(beca.Oferta!.Id, DatosAplicacion.Contexto());
        escenario.Reloj.UtcNow = DatosAplicacion.FechaBase.AddDays(16);

        var cambio = await escenario.CrearExpirar().HandleAsync(
            new ExpirarOfertaBecaCommand(beca.Oferta.Id));

        cambio.Should().BeFalse();
        beca.Estado.Should().Be(EstadoBeca.Activa);
        beca.Oferta.Estado.Should().Be(EstadoOferta.Aceptada);
        escenario.Unidad.Guardados.Should().Be(0);
    }

    [Fact]
    public async Task Debe_RetornarFalso_Cuando_LaOfertaNoExiste()
    {
        var escenario = new EscenarioOtorgamiento();

        var cambio = await escenario.CrearExpirar().HandleAsync(
            new ExpirarOfertaBecaCommand(Guid.CreateVersion7()));

        cambio.Should().BeFalse();
        escenario.Unidad.NivelesSolicitados.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_RetornarNoEncontrado_Cuando_LaBecaDeLaOfertaNoExiste()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearOfertaPendiente(escenario);
        var ofertaId = beca.Oferta!.Id;
        var becaId = beca.Id;

        escenario.Repositorio.Becas.Clear();

        var accion = () => escenario.CrearRechazar().HandleAsync(
            new RechazarOfertaBecaCommand(ofertaId));

        var excepcion = await accion.Should().ThrowAsync<RecursoNoEncontradoException>();

        excepcion.Which.Recurso.Should().Be("oferta");
        excepcion.Which.Identificador.Should().Be(ofertaId);
        becaId.Should().NotBeEmpty();
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
