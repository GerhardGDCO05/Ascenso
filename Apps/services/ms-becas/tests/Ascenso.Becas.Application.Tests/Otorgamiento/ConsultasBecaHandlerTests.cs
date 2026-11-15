using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ConsultarBecasActivas;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerBeca;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerOferta;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using FluentAssertions;

namespace Ascenso.Becas.Application.Tests.Otorgamiento;

public sealed class ConsultasBecaHandlerTests
{
    [Fact]
    public async Task Debe_ObtenerLaOferta_Cuando_ExisteEnElModeloDeLectura()
    {
        var escenario = new EscenarioOtorgamiento();
        var oferta = CrearOfertaDto(Guid.CreateVersion7());
        escenario.Consultas.Oferta = oferta;

        var resultado = await escenario.CrearObtenerOferta().HandleAsync(
            new ObtenerOfertaQuery(oferta.OfertaId));

        resultado.Should().BeSameAs(oferta);
        escenario.Consultas.OfertaSolicitada.Should().Be(oferta.OfertaId);
    }

    [Fact]
    public async Task Debe_RetornarNoEncontrado_Cuando_LaOfertaNoExiste()
    {
        var escenario = new EscenarioOtorgamiento();

        var accion = () => escenario.CrearObtenerOferta().HandleAsync(
            new ObtenerOfertaQuery(Guid.CreateVersion7()));

        var excepcion = await accion.Should().ThrowAsync<RecursoNoEncontradoException>();

        excepcion.Which.Recurso.Should().Be("oferta");
    }

    [Fact]
    public async Task Debe_ObtenerLaBeca_Cuando_ExisteEnElModeloDeLectura()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaDto(Guid.CreateVersion7(), CrearOfertaDto(Guid.CreateVersion7()));
        escenario.Consultas.Beca = beca;

        var resultado = await escenario.CrearObtenerBeca().HandleAsync(
            new ObtenerBecaQuery(beca.BecaId));

        resultado.Should().BeSameAs(beca);
        escenario.Consultas.BecaSolicitada.Should().Be(beca.BecaId);
    }

    [Fact]
    public async Task Debe_RetornarNoEncontrado_Cuando_LaBecaNoExiste()
    {
        var escenario = new EscenarioOtorgamiento();

        var accion = () => escenario.CrearObtenerBeca().HandleAsync(
            new ObtenerBecaQuery(Guid.CreateVersion7()));

        var excepcion = await accion.Should().ThrowAsync<RecursoNoEncontradoException>();

        excepcion.Which.Recurso.Should().Be("beca");
    }

    [Fact]
    public async Task Debe_ConsultarLasBecasActivas_Cuando_SeInformanLosFiltros()
    {
        var escenario = new EscenarioOtorgamiento();
        var estudianteId = Guid.CreateVersion7();
        var periodoId = Guid.CreateVersion7();
        escenario.Consultas.Activas =
        [
            new BecaActivaDto(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                periodoId,
                "ACTIVA",
                "PARCIAL",
                50m,
                0m,
                "USD",
                false)
        ];

        var resultado = await escenario.CrearConsultarActivas().HandleAsync(
            new ConsultarBecasActivasQuery(estudianteId, periodoId));

        resultado.Should().ContainSingle();
        escenario.Consultas.EstudianteSolicitado.Should().Be(estudianteId);
        escenario.Consultas.PeriodoSolicitado.Should().Be(periodoId);
    }

    [Fact]
    public async Task Debe_DevolverListaVacia_Cuando_NoHayBecasActivas()
    {
        var escenario = new EscenarioOtorgamiento();

        var resultado = await escenario.CrearConsultarActivas().HandleAsync(
            new ConsultarBecasActivasQuery(Guid.CreateVersion7(), Guid.CreateVersion7()));

        resultado.Should().BeEmpty();
    }

    [Fact]
    public void Debe_NoExponerDatosPersonalesNiBancarios_EnLaConsultaInterna()
    {
        var propiedades = typeof(BecaActivaDto)
            .GetProperties()
            .Select(propiedad => propiedad.Name)
            .ToArray();

        propiedades.Should().NotContain(nombre =>
            nombre.Contains("Nombre", StringComparison.OrdinalIgnoreCase)
            || nombre.Contains("Documento", StringComparison.OrdinalIgnoreCase)
            || nombre.Contains("Cuenta", StringComparison.OrdinalIgnoreCase)
            || nombre.Contains("Banco", StringComparison.OrdinalIgnoreCase)
            || nombre.Contains("Correo", StringComparison.OrdinalIgnoreCase)
            || nombre.Contains("Telefono", StringComparison.OrdinalIgnoreCase));
    }

    private static OfertaBecaDto CrearOfertaDto(Guid ofertaId)
        => new(
            ofertaId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Programa de becas de prueba",
            Guid.CreateVersion7(),
            "PENDIENTE_RESPUESTA",
            500m,
            2000m,
            4,
            new CondicionesPermanenciaDto(14m, 4, 1, 80, "SUSPENSION"),
            "https://storage.example/carta.pdf",
            DatosAplicacion.FechaBase,
            DatosAplicacion.FechaBase.AddDays(15),
            null,
            null);

    private static BecaDto CrearBecaDto(Guid becaId, OfertaBecaDto? oferta)
        => new(
            becaId,
            "BECA-2026-0001",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Programa de becas de prueba",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            0,
            4,
            "ACTIVA",
            null,
            new PoliticaCoberturaDto("PARCIAL", 50m, 0m, "USD"),
            500m,
            0m,
            false,
            false,
            false,
            true,
            DateOnly.FromDateTime(DatosAplicacion.FechaBase.UtcDateTime),
            DateOnly.FromDateTime(DatosAplicacion.FechaBase.UtcDateTime),
            null,
            oferta,
            null,
            []);
}
