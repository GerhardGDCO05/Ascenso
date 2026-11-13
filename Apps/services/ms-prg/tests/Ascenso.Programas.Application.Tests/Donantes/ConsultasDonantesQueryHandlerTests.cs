using Ascenso.Programas.Application.Donantes.Consultas.ConsultarDonantes;
using Ascenso.Programas.Application.Donantes.Consultas.ConsultarHistorialAportes;
using Ascenso.Programas.Application.Donantes.Consultas.ObtenerDonantePorId;
using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Donantes.Entidades;
using Ascenso.Programas.Domain.Donantes.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Programas.Application.Tests.Donantes;

public sealed class ConsultasDonantesQueryHandlerTests
{
    private static readonly Guid UsuarioId = Guid.Parse("01966000-0000-7000-8000-000000000040");
    private readonly IDonanteRepository _donanteRepository = Substitute.For<IDonanteRepository>();

    [Fact]
    public async Task Debe_RetornarPaginadoYDetalleDeDonante_Cuando_SeConsultaPorFiltrosYPorId_HU13_1()
    {
        var donante = CrearDonante();
        _donanteRepository.ConsultarPaginadoAsync(
                1,
                10,
                TipoDonante.Empresa,
                EstadoDonante.Activo,
                "Horizonte",
                Arg.Any<CancellationToken>())
            .Returns(([donante], 1));

        _donanteRepository.ObtenerPorIdAsync(donante.Id, Arg.Any<CancellationToken>())
            .Returns(donante);

        var paginadoHandler = new ConsultarDonantesQueryHandler(_donanteRepository);
        var detalleHandler = new ObtenerDonantePorIdQueryHandler(_donanteRepository);

        var paginado = await paginadoHandler.HandleAsync(new ConsultarDonantesQuery(
            1,
            10,
            "EMPRESA",
            "ACTIVO",
            "Horizonte"));

        var detalle = await detalleHandler.HandleAsync(new ObtenerDonantePorIdQuery(donante.Id));

        paginado.TotalRegistros.Should().Be(1);
        paginado.Items.Should().ContainSingle(d => d.Id == donante.Id);
        detalle.IdentificacionFiscal.Should().Be(donante.IdentificacionFiscal);
        detalle.ContactoPrincipal.EmailContacto.Should().Be("contacto@horizonte.org");
    }

    [Fact]
    public async Task Debe_LanzarDonanteNoEncontradoException_Cuando_DonanteNoExisteEnDetalle_HU13_1()
    {
        var donanteIdInexistente = UuidV7.Nuevo();
        _donanteRepository.ObtenerPorIdAsync(donanteIdInexistente, Arg.Any<CancellationToken>()).Returns((Donante?)null);

        var detalleHandler = new ObtenerDonantePorIdQueryHandler(_donanteRepository);

        var accion = () => detalleHandler.HandleAsync(new ObtenerDonantePorIdQuery(donanteIdInexistente));

        await accion.Should()
            .ThrowAsync<DonanteNoEncontradoException>()
            .Where(ex => ex.EstadoHttp == 404);
    }

    [Fact]
    public async Task Debe_ConsultarHistorialAportesPorPeriodoAcademico_Cuando_DonanteExiste_HU13_2()
    {
        var donante = CrearDonante();
        var rutaFirebase = $"/donantes/{donante.Id:D}/aportes/{UuidV7.Nuevo():D}.pdf";
        var aporte = donante.RegistrarAporte(
            7500.00m,
            "USD",
            "2026-1",
            DateTimeOffset.UtcNow,
            "Banesco",
            "REF-HIST-001",
            rutaFirebase,
            UsuarioId);

        _donanteRepository.ExisteAsync(donante.Id, Arg.Any<CancellationToken>()).Returns(true);
        _donanteRepository.ConsultarHistorialAportesAsync(donante.Id, "2026-1", Arg.Any<CancellationToken>())
            .Returns(new List<AporteDonante> { aporte });

        var handler = new ConsultarHistorialAportesQueryHandler(_donanteRepository);

        var historial = await handler.HandleAsync(new ConsultarHistorialAportesQuery(donante.Id, "2026-1"));

        historial.Should().ContainSingle(a =>
            a.Id == aporte.Id
            && a.PeriodoAcademicoId == "2026-1"
            && a.NumeroReferenciaBancaria == "REF-HIST-001");
    }

    private static Donante CrearDonante()
        => Donante.Crear(
            TipoDonante.Empresa,
            "Horizonte Solidario",
            "J-30999111-0",
            "hola@horizonte.org",
            "+58 212 7008090",
            "Av. Libertador, Caracas",
            new ContactoDonante("Sofía Méndez", "Directora", "contacto@horizonte.org", "+58 414 7008090"),
            UsuarioId);
}

