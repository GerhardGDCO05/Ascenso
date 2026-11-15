using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Comandos.AceptarOfertaBeca;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;
using Ascenso.Becas.Domain.Ports;
using FluentAssertions;

namespace Ascenso.Becas.Application.Tests.Otorgamiento;

public sealed class AceptarOfertaBecaHandlerTests
{
    [Fact]
    public async Task Debe_AceptarLaOferta_Cuando_EstaPendienteYVigente()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearOfertaPendiente(escenario);

        var resultado = await escenario.CrearAceptar().HandleAsync(
            new AceptarOfertaBecaCommand(beca.Oferta!.Id));

        resultado.OfertaId.Should().Be(beca.Oferta.Id);
        resultado.BecaId.Should().Be(beca.Id);
        resultado.EstadoOferta.Should().Be("ACEPTADA");
        resultado.EstadoBeca.Should().Be("ACTIVA");

        beca.Estado.Should().Be(EstadoBeca.Activa);
        beca.Oferta.Estado.Should().Be(EstadoOferta.Aceptada);
        beca.FechaActivacion.Should().Be(DateOnly.FromDateTime(DatosAplicacion.FechaBase.UtcDateTime));
        beca.HabilitadaParaDesembolso.Should().BeTrue();

        escenario.Repositorio.EstudiantesBloqueados.Should().ContainSingle()
            .Which.Should().Be(escenario.EstudianteId);
        escenario.Unidad.NivelesSolicitados.Should().ContainSingle()
            .Which.Should().Be(NivelAislamientoTransaccion.Serializable);
        escenario.Unidad.Confirmaciones.Should().Be(1);
        escenario.Outbox.Confirmados.Should()
            .ContainSingle(evento => evento is BecaActivadaEvento);
    }

    [Fact]
    public async Task Debe_RechazarLaAceptacion_Cuando_LaOfertaEstaVencida()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearOfertaPendiente(escenario);
        escenario.Reloj.UtcNow = DatosAplicacion.FechaBase.AddDays(16);

        var accion = () => escenario.CrearAceptar().HandleAsync(
            new AceptarOfertaBecaCommand(beca.Oferta!.Id));

        var excepcion = await accion.Should().ThrowAsync<OfertaVencidaException>();

        excepcion.Which.Codigo.Should().Be("OFERTA_VENCIDA");
        beca.Estado.Should().Be(EstadoBeca.Otorgada);
        beca.Oferta!.Estado.Should().Be(EstadoOferta.PendienteRespuesta);
        escenario.Unidad.Confirmaciones.Should().Be(0);
        escenario.Unidad.Reversiones.Should().Be(1);
        escenario.Outbox.Descartado.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_RechazarLaAceptacion_Cuando_ExisteDobleBeneficio()
    {
        var escenario = new EscenarioOtorgamiento();
        var candidata = CrearOfertaPendiente(escenario);
        var otraActiva = DatosAplicacion.CrearBecaActiva(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            escenario.PeriodoAcademicoId,
            escenario.EstudianteId,
            permiteAcumulacion: false);
        escenario.Repositorio.Becas.Add(otraActiva);

        var accion = () => escenario.CrearAceptar().HandleAsync(
            new AceptarOfertaBecaCommand(candidata.Oferta!.Id));

        var excepcion = await accion.Should().ThrowAsync<DobleBeneficioException>();

        excepcion.Which.Codigo.Should().Be("DOBLE_BENEFICIO");
        candidata.Estado.Should().Be(EstadoBeca.Otorgada);
        escenario.Unidad.Confirmaciones.Should().Be(0);
        escenario.Outbox.Descartado.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_PermitirLaAceptacion_Cuando_AmbasBecasPermitenAcumulacion()
    {
        var escenario = new EscenarioOtorgamiento();
        var candidata = DatosAplicacion.CrearBecaConOferta(
            escenario.PostulacionId,
            escenario.ProgramaBecaId,
            escenario.PeriodoAcademicoId,
            escenario.EstudianteId,
            permiteAcumulacion: true);
        escenario.Repositorio.Becas.Add(candidata);

        var otraActiva = DatosAplicacion.CrearBecaActiva(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            escenario.PeriodoAcademicoId,
            escenario.EstudianteId,
            permiteAcumulacion: true);
        escenario.Repositorio.Becas.Add(otraActiva);

        var resultado = await escenario.CrearAceptar().HandleAsync(
            new AceptarOfertaBecaCommand(candidata.Oferta!.Id));

        resultado.EstadoBeca.Should().Be("ACTIVA");
        candidata.Estado.Should().Be(EstadoBeca.Activa);
    }

    [Fact]
    public async Task Debe_RetornarNoEncontrado_Cuando_LaOfertaNoExiste()
    {
        var escenario = new EscenarioOtorgamiento();

        var accion = () => escenario.CrearAceptar().HandleAsync(
            new AceptarOfertaBecaCommand(Guid.CreateVersion7()));

        var excepcion = await accion.Should().ThrowAsync<RecursoNoEncontradoException>();

        excepcion.Which.Recurso.Should().Be("oferta");
        escenario.Unidad.NivelesSolicitados.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_RechazarLaAceptacion_Cuando_LaOfertaYaFueRespondida()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearOfertaPendiente(escenario);
        beca.AceptarOferta(beca.Oferta!.Id, DatosAplicacion.Contexto());

        var accion = () => escenario.CrearAceptar().HandleAsync(
            new AceptarOfertaBecaCommand(beca.Oferta.Id));

        await accion.Should().ThrowAsync<TransicionEstadoInvalidaException>();
        escenario.Unidad.Confirmaciones.Should().Be(0);
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
