using Ascenso.Programas.Application.Donantes.Comandos.ActualizarContactoDonante;
using Ascenso.Programas.Application.Donantes.Comandos.DesactivarDonante;
using Ascenso.Programas.Application.Donantes.Comandos.ReactivarDonante;
using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Donantes.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Programas.Application.Tests.Donantes;

public sealed class GestionEstadoYContactoDonanteCommandHandlerTests
{
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000020");
    private readonly IDonanteRepository _donanteRepository = Substitute.For<IDonanteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Debe_ActualizarContactoDonanteYConfirmarCambios_Cuando_DonanteExiste_HU13_1()
    {
        var donante = CrearDonante();
        _donanteRepository.ObtenerPorIdAsync(donante.Id, Arg.Any<CancellationToken>()).Returns(donante);

        var handler = new ActualizarContactoDonanteCommandHandler(
            _donanteRepository,
            _unitOfWork,
            new ActualizarContactoDonanteCommandValidator());

        var resultado = await handler.HandleAsync(new ActualizarContactoDonanteCommand(
            donante.Id,
            "nuevo@organizacion.org",
            "+58 212 8000000",
            "Nueva Dirección Fiscal, Chacao",
            new ContactoDonanteDto("Pedro Ruiz", "Presidente", "pruiz@organizacion.org", "+58 412 1112233"),
            _usuarioId));

        resultado.Email.Should().Be("nuevo@organizacion.org");
        resultado.ContactoPrincipal.NombreRepresentante.Should().Be("Pedro Ruiz");
        resultado.Version.Should().Be(2);
        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_BloquearDesactivacionConDonanteConFondos_Cuando_TieneFondosComprometidos_RNPRG04_HU13_1()
    {
        var donante = CrearDonante();
        _donanteRepository.ObtenerPorIdAsync(donante.Id, Arg.Any<CancellationToken>()).Returns(donante);
        _donanteRepository.TieneFondosComprometidosEnProgramaVigenteAsync(donante.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new DesactivarDonanteCommandHandler(_donanteRepository, _unitOfWork);

        var accion = () => handler.HandleAsync(new DesactivarDonanteCommand(donante.Id, _usuarioId));

        await accion.Should()
            .ThrowAsync<DonanteConFondosComprometidosException>()
            .Where(ex => ex.Codigo == "DONANTE_CON_FONDOS" && ex.EstadoHttp == 409);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_DesactivarYReactivarDonante_Cuando_NoTieneFondosComprometidos_HU13_1()
    {
        var donante = CrearDonante();
        _donanteRepository.ObtenerPorIdAsync(donante.Id, Arg.Any<CancellationToken>()).Returns(donante);
        _donanteRepository.TieneFondosComprometidosEnProgramaVigenteAsync(donante.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        var desactivarHandler = new DesactivarDonanteCommandHandler(_donanteRepository, _unitOfWork);
        var reactivarHandler = new ReactivarDonanteCommandHandler(_donanteRepository, _unitOfWork);

        var desactivado = await desactivarHandler.HandleAsync(new DesactivarDonanteCommand(donante.Id, _usuarioId));
        desactivado.EstadoDonante.Should().Be("INACTIVO");

        var reactivado = await reactivarHandler.HandleAsync(new ReactivarDonanteCommand(donante.Id, _usuarioId));
        reactivado.EstadoDonante.Should().Be("ACTIVO");

        await _unitOfWork.Received(2).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    private static Donante CrearDonante()
        => Donante.Crear(
            TipoDonante.Organizacion,
            "Fundación Futuro",
            "J-30888777-2",
            "info@futuro.org",
            "+58 212 3334455",
            "Av. Universidad, Caracas",
            new ContactoDonante("Elena Díaz", "Directora", "ediaz@futuro.org", "+58 416 9998877"),
            _usuarioId);
}

