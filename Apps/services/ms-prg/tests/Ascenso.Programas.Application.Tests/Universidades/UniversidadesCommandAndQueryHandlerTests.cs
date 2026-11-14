using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Application.Universidades.Comandos.ActualizarCostoMatricula;
using Ascenso.Programas.Application.Universidades.Comandos.AgregarCarreraElegible;
using Ascenso.Programas.Application.Universidades.Comandos.MarcarConvenioVencido;
using Ascenso.Programas.Application.Universidades.Comandos.RegistrarUniversidadConvenio;
using Ascenso.Programas.Application.Universidades.Consultas.ConsultarHistorialCostosCarrera;
using Ascenso.Programas.Application.Universidades.DTOs;
using Ascenso.Programas.Application.Universidades.EventosIntegracion;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using Ascenso.Programas.Domain.Universidades.Agregados;
using Ascenso.Programas.Domain.Universidades.ObjetosValor;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Programas.Application.Tests.Universidades;

public sealed class UniversidadesCommandAndQueryHandlerTests
{
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000201");
    private readonly IUniversidadAliadaRepository _universidadRepository = Substitute.For<IUniversidadAliadaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IIntegrationEventPublisher _eventPublisher = Substitute.For<IIntegrationEventPublisher>();

    [Fact]
    public async Task Debe_RegistrarUniversidadYPublicarEvento_Cuando_CodigoEsUnico_HU14_1()
    {
        _universidadRepository.ExisteCodigoInstitucionalAsync("UCAB-CCS", Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new RegistrarUniversidadConvenioCommandHandler(
            _universidadRepository,
            _unitOfWork,
            _eventPublisher,
            new RegistrarUniversidadConvenioCommandValidator());

        var comando = new RegistrarUniversidadConvenioCommand(
            "UCAB-CCS",
            "Universidad Católica Andrés Bello",
            "Montalbán, Caracas",
            new ContactoEnlaceDto("Laura Ríos", "lrios@ucab.edu.ve", "+58 212 4074111"),
            null,
            null,
            "CONV-2026-001",
            "Convenio vigente para becas de pregrado.",
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            "/universidades/ucab/convenios/conv-2026.pdf",
            true,
            _usuarioId);

        var resultado = await handler.HandleAsync(comando);

        resultado.CodigoInstitucional.Should().Be("UCAB-CCS");
        resultado.TieneConvenioVigente.Should().BeTrue();
        resultado.Convenios.Should().HaveCount(1);

        await _universidadRepository.Received(1)
            .AgregarAsync(Arg.Any<UniversidadAliada>(), Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1)
            .PublicarAsync(Arg.Any<ConvenioUniversidadRegistrado>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1)
            .GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarRegistroUniversidad_Cuando_CodigoInstitucionalYaExiste_HU14_1()
    {
        _universidadRepository.ExisteCodigoInstitucionalAsync("UCAB-CCS", Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new RegistrarUniversidadConvenioCommandHandler(
            _universidadRepository,
            _unitOfWork,
            _eventPublisher,
            new RegistrarUniversidadConvenioCommandValidator());

        var comando = new RegistrarUniversidadConvenioCommand(
            "ucab-ccs",
            "UCAB",
            "Caracas",
            new ContactoEnlaceDto("Laura Ríos", "lrios@ucab.edu.ve", "+58 212 4074111"),
            null,
            null,
            "CONV-2026-001",
            "Términos",
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            "/universidades/ucab/convenios/conv-2026.pdf",
            true,
            _usuarioId);

        var accion = () => handler.HandleAsync(comando);

        await accion.Should().ThrowAsync<CodigoInstitucionalDuplicadoException>();
        await _unitOfWork.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_MarcarConvenioVencidoYPublicarEvento_HU14_1()
    {
        var universidad = CrearUniversidad();
        var convenio = universidad.Convenios.Single();
        _universidadRepository.ObtenerPorIdAsync(universidad.Id, Arg.Any<CancellationToken>())
            .Returns(universidad);

        var handler = new MarcarConvenioVencidoCommandHandler(
            _universidadRepository,
            _unitOfWork,
            _eventPublisher);

        var resultado = await handler.HandleAsync(
            new MarcarConvenioVencidoCommand(universidad.Id, convenio.Id, _usuarioId));

        resultado.Estado.Should().Be("VENCIDO");
        await _eventPublisher.Received(1)
            .PublicarAsync(Arg.Any<ConvenioUniversidadVencido>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_AgregarCarreraYActualizarCostoVersionado_HU14_2()
    {
        var universidad = CrearUniversidad();
        _universidadRepository.ObtenerPorIdAsync(universidad.Id, Arg.Any<CancellationToken>())
            .Returns(universidad);

        var carreraHandler = new AgregarCarreraElegibleCommandHandler(
            _universidadRepository,
            _unitOfWork,
            new AgregarCarreraElegibleCommandValidator());

        var carreraDto = await carreraHandler.HandleAsync(
            new AgregarCarreraElegibleCommand(universidad.Id, "ING-CIV", "Ingeniería Civil", 10, _usuarioId));

        var costoHandler = new ActualizarCostoMatriculaCommandHandler(
            _universidadRepository,
            _unitOfWork,
            _eventPublisher,
            new ActualizarCostoMatriculaCommandValidator());

        var costoV1 = await costoHandler.HandleAsync(
            new ActualizarCostoMatriculaCommand(universidad.Id, carreraDto.Id, "2026-1", 1500m, "USD", _usuarioId));

        var costoV2 = await costoHandler.HandleAsync(
            new ActualizarCostoMatriculaCommand(universidad.Id, carreraDto.Id, "2026-1", 1750m, "USD", _usuarioId));

        costoV1.NumeroVersion.Should().Be(1);
        costoV2.NumeroVersion.Should().Be(2);
        costoV2.Estado.Should().Be("VIGENTE");

        await _eventPublisher.Received(2)
            .PublicarAsync(Arg.Any<CostoMatriculaActualizado>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(3).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_ConsultarHistorialCostosCarrera_Cuando_CarreraExiste_HU14_2()
    {
        var universidad = CrearUniversidad();
        var carrera = universidad.AgregarCarreraElegible("ING-IND", "Ingeniería Industrial", 10, _usuarioId);
        var costo1 = universidad.ActualizarCostoMatricula(carrera.Id, "2026-1", 1400m, "USD", _usuarioId);
        var costo2 = universidad.ActualizarCostoMatricula(carrera.Id, "2026-1", 1650m, "USD", _usuarioId);

        _universidadRepository.ObtenerPorIdAsync(universidad.Id, Arg.Any<CancellationToken>())
            .Returns(universidad);
        _universidadRepository.ConsultarHistorialCostosCarreraAsync(universidad.Id, carrera.Id, "2026-1", Arg.Any<CancellationToken>())
            .Returns([costo2, costo1]);

        var handler = new ConsultarHistorialCostosCarreraQueryHandler(_universidadRepository);

        var resultado = await handler.HandleAsync(
            new ConsultarHistorialCostosCarreraQuery(universidad.Id, carrera.Id, "2026-1"));

        resultado.Should().HaveCount(2);
        resultado[0].NumeroVersion.Should().Be(2);
        resultado[0].Estado.Should().Be("VIGENTE");
        resultado[1].NumeroVersion.Should().Be(1);
        resultado[1].Estado.Should().Be("HISTORICO");
    }

    private static UniversidadAliada CrearUniversidad()
        => UniversidadAliada.CrearConConvenioInicial(
            "UCAB-CCS",
            "Universidad Católica Andrés Bello",
            "Montalbán, Caracas",
            new ContactoEnlace("Laura Ríos", "lrios@ucab.edu.ve", "+58 212 4074111"),
            null,
            null,
            "CONV-2026-001",
            "Convenio vigente",
            new DateOnly(2026, 1, 1),
            new DateOnly(2028, 12, 31),
            "/universidades/ucab/convenios/conv-2026.pdf",
            true,
            _usuarioId);
}
