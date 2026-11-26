using Ascenso.Programas.Application.Convocatorias.Abstracciones;
using Ascenso.Programas.Application.Convocatorias.Comandos.CrearConvocatoriaBorrador;
using Ascenso.Programas.Application.Convocatorias.DTOs;
using Ascenso.Programas.Application.Convocatorias.Servicios;
using Ascenso.Programas.Domain.Convocatorias.Agregados;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Programas.Application.Tests.Convocatorias;

public sealed class CrearConvocatoriaBorradorCommandHandlerTests
{
    private static readonly Guid _programaId = Guid.Parse("01966000-0000-7000-8000-000000000101");
    private static readonly Guid _universidadId = Guid.Parse("01966000-0000-7000-8000-000000000301");
    private static readonly Guid _carreraId = Guid.Parse("01966000-0000-7000-8000-000000000401");
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000501");
    private static readonly string _periodoId = Guid.Parse("01966000-0000-7000-8000-000000000601").ToString("D");

    private readonly IConvocatoriaRepository _convocatoriaRepository = Substitute.For<IConvocatoriaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICatalogoConvocatoriaReader _catalogoReader = Substitute.For<ICatalogoConvocatoriaReader>();
    private readonly IPeriodoAcademicoClient _periodoClient = Substitute.For<IPeriodoAcademicoClient>();
    private readonly IRelojConvocatorias _reloj = Substitute.For<IRelojConvocatorias>();

    public CrearConvocatoriaBorradorCommandHandlerTests()
    {
        _reloj.Ahora.Returns(new DateTimeOffset(2026, 11, 25, 2, 0, 0, TimeSpan.Zero));
        _catalogoReader.ObtenerProgramaAsync(_programaId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(ProgramaValido());
        _periodoClient.ObtenerPorIdAsync(_periodoId, Arg.Any<CancellationToken>())
            .Returns(new PeriodoAcademicoInformacion(Guid.Parse(_periodoId), "2026-II", "VIGENTE"));
        _convocatoriaRepository.ExisteCodigoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    [Fact]
    public async Task Debe_CrearBorradorYGuardar_Cuando_ProgramaPeriodoYCriteriosSonValidos()
    {
        var resultado = await CrearHandler().HandleAsync(ComandoValido());

        resultado.Estado.Should().Be("BORRADOR");
        resultado.Codigo.Should().Be("RF21-2026");
        resultado.FechaApertura.Should().Be(new DateTimeOffset(2026, 12, 1, 4, 0, 0, TimeSpan.Zero));
        resultado.FechaCierre.Should().Be(new DateTimeOffset(2026, 12, 21, 3, 59, 59, TimeSpan.Zero));
        await _convocatoriaRepository.Received(1).AgregarAsync(
            Arg.Is<Convocatoria>(convocatoria => convocatoria.Estado == EstadoConvocatoria.Borrador),
            Arg.Any<CancellationToken>());
        await _catalogoReader.Received(1).ObtenerProgramaAsync(
            _programaId,
            new DateOnly(2026, 11, 24),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarCreacion_Cuando_ProgramaNoExiste()
    {
        _catalogoReader.ObtenerProgramaAsync(_programaId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((ProgramaParaConvocatoria?)null);

        await CrearHandler().Invoking(handler => handler.HandleAsync(ComandoValido()))
            .Should().ThrowAsync<ProgramaNoHabilitadoException>();

        await _convocatoriaRepository.DidNotReceive().AgregarAsync(
            Arg.Any<Convocatoria>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarCreacion_Cuando_PeriodoNoEstaVigente()
    {
        _periodoClient.ObtenerPorIdAsync(_periodoId, Arg.Any<CancellationToken>())
            .Returns(new PeriodoAcademicoInformacion(Guid.Parse(_periodoId), "2026-II", "CERRADO"));

        await CrearHandler().Invoking(handler => handler.HandleAsync(ComandoValido()))
            .Should().ThrowAsync<PeriodoAcademicoNoVigenteException>();

        await _unitOfWork.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarCreacion_Cuando_CuposSuperanLosDisponibles()
    {
        _catalogoReader.ObtenerProgramaAsync(_programaId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(ProgramaValido(cuposDisponibles: 2));

        await CrearHandler().Invoking(handler => handler.HandleAsync(ComandoValido(cupos: 3)))
            .Should().ThrowAsync<CuposInsuficientesException>();
    }

    [Fact]
    public async Task Debe_RechazarCreacion_Cuando_UniversidadOCarreraNoPerteneceAlConvenio()
    {
        var comandoUniversidadInvalida = ComandoValido(criterios: Criterios(universidadId: Guid.NewGuid()));
        await CrearHandler().Invoking(handler => handler.HandleAsync(comandoUniversidadInvalida))
            .Should().ThrowAsync<CriterioUniversidadInvalidaException>();

        var comandoCarreraInvalida = ComandoValido(criterios: Criterios(carreraId: Guid.NewGuid()));
        await CrearHandler().Invoking(handler => handler.HandleAsync(comandoCarreraInvalida))
            .Should().ThrowAsync<CriterioCarreraInvalidaException>();

        await _unitOfWork.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarCreacion_Cuando_CriteriosEstanFueraDeRango()
    {
        var comando = ComandoValido(criterios: Criterios(promedioMinimo: 20.01m));

        await CrearHandler().Invoking(handler => handler.HandleAsync(comando))
            .Should().ThrowAsync<ValidationException>();

        await _catalogoReader.DidNotReceive().ObtenerProgramaAsync(
            Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarCreacion_Cuando_IdentificadorDePeriodoNoEsUuid()
    {
        var comando = ComandoValido() with { PeriodoAcademicoId = "2026-II" };

        await CrearHandler().Invoking(handler => handler.HandleAsync(comando))
            .Should().ThrowAsync<ValidationException>();

        await _periodoClient.DidNotReceive().ObtenerPorIdAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private CrearConvocatoriaBorradorCommandHandler CrearHandler()
        => new(
            _convocatoriaRepository,
            _unitOfWork,
            new ValidadorDisponibilidadConvocatoria(_catalogoReader, _periodoClient, _reloj),
            new ConvocatoriaTransactionExecutorFalso(),
            new CrearConvocatoriaBorradorCommandValidator());

    private static CrearConvocatoriaBorradorCommand ComandoValido(
        int cupos = 3,
        CriteriosElegibilidadCicloDto? criterios = null)
        => new(
            _programaId,
            _periodoId,
            " rf21-2026 ",
            "Convocatoria 2026",
            new DateOnly(2026, 12, 1),
            new DateOnly(2026, 12, 20),
            new DateOnly(2026, 12, 22),
            new DateOnly(2026, 12, 22),
            new DateOnly(2026, 12, 31),
            true,
            cupos,
            criterios ?? Criterios(),
            _usuarioId);

    private static CriteriosElegibilidadCicloDto Criterios(
        decimal promedioMinimo = 15.00m,
        Guid? universidadId = null,
        Guid? carreraId = null)
        => new(
            promedioMinimo,
            17,
            25,
            1200.00m,
            [universidadId ?? _universidadId],
            [carreraId ?? _carreraId]);

    private static ProgramaParaConvocatoria ProgramaValido(int cuposDisponibles = 10)
        => new(
            _programaId,
            "BECA-TEST",
            "Programa de prueba",
            EstadoProgramaBeca.Configurado,
            cuposDisponibles,
            10000.00m,
            [_universidadId],
            [_carreraId]);
}
