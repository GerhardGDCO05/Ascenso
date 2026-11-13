using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Application.Donantes.Comandos.RegistrarAporteDonante;
using Ascenso.Programas.Application.Donantes.EventosIntegracion;
using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Donantes.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Programas.Application.Tests.Donantes;

public sealed class RegistrarAporteDonanteCommandHandlerTests
{
    private static readonly Guid UsuarioId = Guid.Parse("01966000-0000-7000-8000-000000000030");
    private readonly IDonanteRepository _donanteRepository = Substitute.For<IDonanteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IIntegrationEventPublisher _eventPublisher = Substitute.For<IIntegrationEventPublisher>();
    private readonly RegistrarAporteDonanteCommandHandler _handler;

    public RegistrarAporteDonanteCommandHandlerTests()
    {
        _handler = new RegistrarAporteDonanteCommandHandler(
            _donanteRepository,
            _unitOfWork,
            _eventPublisher,
            new RegistrarAporteDonanteCommandValidator());
    }

    [Fact]
    public async Task Debe_RegistrarAporteConfirmadoIncrementarSaldosYPublicarEventoOutbox_Cuando_DatosSonValidos_HU13_2()
    {
        var donante = CrearDonante();
        var rutaFirebase = $"/donantes/{donante.Id:D}/aportes/{UuidV7.Nuevo():D}.pdf";

        _donanteRepository.ObtenerPorIdAsync(donante.Id, Arg.Any<CancellationToken>()).Returns(donante);
        _donanteRepository.ExisteReferenciaBancariaAsync("BNC-2026-0001", Arg.Any<CancellationToken>()).Returns(false);

        var comando = new RegistrarAporteDonanteCommand(
            donante.Id,
            25000.00m,
            "USD",
            "2026-1",
            new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.Zero),
            "Banco Nacional de Crédito",
            "bnc-2026-0001",
            rutaFirebase,
            "Aporte inicial 2026-1",
            UsuarioId);

        var resultado = await _handler.HandleAsync(comando, CancellationToken.None);

        resultado.EstadoAporte.Should().Be("CONFIRMADO");
        resultado.MontoAporte.Should().Be(25000.00m);
        resultado.NumeroReferenciaBancaria.Should().Be("BNC-2026-0001");
        donante.SaldoTotalHistorico.Should().Be(25000.00m);
        donante.SaldoDisponibleAsignacion.Should().Be(25000.00m);

        await _eventPublisher.Received(1).PublicarAsync(
            Arg.Is<AporteDonanteRegistrado>(e =>
                e.DonanteId == donante.Id
                && e.AporteDonanteId == resultado.Id
                && e.MontoAporte == 25000.00m
                && e.SaldoDisponibleAsignacion == 25000.00m),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarCon409ReferenciaDuplicadaSinAlterarSaldos_Cuando_ReferenciaYaExiste_RNPRG04_HU13_2()
    {
        var donante = CrearDonante();
        var rutaFirebase = $"/donantes/{donante.Id:D}/aportes/{UuidV7.Nuevo():D}.pdf";

        _donanteRepository.ObtenerPorIdAsync(donante.Id, Arg.Any<CancellationToken>()).Returns(donante);
        _donanteRepository.ExisteReferenciaBancariaAsync("REF-REPETIDA-1", Arg.Any<CancellationToken>()).Returns(true);

        var comando = new RegistrarAporteDonanteCommand(
            donante.Id,
            1000.00m,
            "USD",
            "2026-1",
            DateTimeOffset.UtcNow,
            "Banesco",
            "ref-repetida-1",
            rutaFirebase,
            null,
            UsuarioId);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ReferenciaBancariaDuplicadaException>()
            .Where(ex => ex.Codigo == "REFERENCIA_DUPLICADA" && ex.EstadoHttp == 409);

        donante.SaldoTotalHistorico.Should().Be(0.00m);
        donante.SaldoDisponibleAsignacion.Should().Be(0.00m);
        await _eventPublisher.DidNotReceiveWithAnyArgs().PublicarAsync<AporteDonanteRegistrado>(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/otro-directorio/archivo.pdf")]
    public async Task Debe_FallarValidacionCon400_Cuando_FaltaComprobanteFirebaseORutaInvalida_RNPRG04_HU13_2(string urlInvalida)
    {
        var donante = CrearDonante();
        var comando = new RegistrarAporteDonanteCommand(
            donante.Id,
            1000.00m,
            "USD",
            "2026-1",
            DateTimeOffset.UtcNow,
            "Banesco",
            "REF-VALID-01",
            urlInvalida,
            null,
            UsuarioId);

        var accion = () => _handler.HandleAsync(comando, CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.ErrorCode == "COMPROBANTE_REQUERIDO"));

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private static Donante CrearDonante()
        => Donante.Crear(
            TipoDonante.Empresa,
            "Industrias Ascenso",
            "J-30111222-3",
            "finanzas@ascenso.org",
            "+58 212 5556677",
            "Zona Industrial, Valencia",
            new ContactoDonante("Roberto Silva", "Tesorero", "rsilva@ascenso.org", "+58 414 5556677"),
            UsuarioId);
}

