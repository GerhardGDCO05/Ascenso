using Ascenso.Programas.Application.Donantes.Comandos.RegistrarDonante;
using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Programas.Application.Tests.Donantes;

public sealed class RegistrarDonanteCommandHandlerTests
{
    private readonly IDonanteRepository _donanteRepository = Substitute.For<IDonanteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegistrarDonanteCommandHandler _handler;

    public RegistrarDonanteCommandHandlerTests()
    {
        _handler = new RegistrarDonanteCommandHandler(
            _donanteRepository,
            _unitOfWork,
            new RegistrarDonanteCommandValidator());
    }

    [Fact]
    public async Task Debe_RegistrarDonanteConSaldosEnCero_Cuando_IdentificacionEsUnica_HU13_1()
    {
        var comando = CrearComandoValido();
        _donanteRepository
            .ExisteIdentificacionFiscalAsync("J-40999888-1", Arg.Any<CancellationToken>())
            .Returns(false);

        var resultado = await _handler.HandleAsync(comando, CancellationToken.None);

        resultado.TipoDonante.Should().Be("EMPRESA");
        resultado.IdentificacionFiscal.Should().Be("J-40999888-1");
        resultado.EstadoDonante.Should().Be("ACTIVO");
        resultado.Moneda.Should().Be("USD");
        resultado.SaldoTotalHistorico.Should().Be(0.00m);
        resultado.SaldoDisponibleAsignacion.Should().Be(0.00m);

        await _donanteRepository.Received(1).AgregarAsync(
            Arg.Is<Donante>(d => d.IdentificacionFiscal == "J-40999888-1"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarCon409IdentificacionFiscalDuplicada_Cuando_RifYaExiste_HU13_1()
    {
        var comando = CrearComandoValido();
        _donanteRepository
            .ExisteIdentificacionFiscalAsync("J-40999888-1", Arg.Any<CancellationToken>())
            .Returns(true);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<IdentificacionFiscalDuplicadaException>()
            .Where(ex => ex.Codigo == "IDENTIFICACION_FISCAL_DUPLICADA" && ex.EstadoHttp == 409);

        await _donanteRepository.DidNotReceiveWithAnyArgs().AgregarAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private static RegistrarDonanteCommand CrearComandoValido()
        => new(
            "EMPRESA",
            "Corporación Aliada AC",
            " j-40999888-1 ",
            "rse@corporacion.org",
            "+58 212 9001122",
            "Av. Francisco de Miranda, Caracas",
            new ContactoDonanteDto(
                "Ana Morales",
                "Gerente RSE",
                "amorales@corporacion.org",
                "+58 414 2223344"),
            Guid.Parse("01966000-0000-7000-8000-000000000010"));
}

