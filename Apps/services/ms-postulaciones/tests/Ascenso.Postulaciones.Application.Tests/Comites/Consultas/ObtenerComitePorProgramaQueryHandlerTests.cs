using Ascenso.Postulaciones.Application.Comites.Consultas.ObtenerComitePorPrograma;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Comites.Consultas;

public sealed class ObtenerComitePorProgramaQueryHandlerTests
{
    private static readonly Guid _coordinadorId = Guid.Parse("01966000-0000-7000-8000-000000000731");
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000732");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000733");
    private static readonly Guid _jefaturaId = Guid.Parse("01966000-0000-7000-8000-000000000734");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IComiteEvaluadorRepository _comiteRepository = Substitute.For<IComiteEvaluadorRepository>();
    private readonly ObtenerComitePorProgramaQueryHandler _handler;

    public ObtenerComitePorProgramaQueryHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        _comiteRepository
            .ObtenerPorProgramaYConvocatoriaAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(null));

        _handler = new ObtenerComitePorProgramaQueryHandler(_usuarioActual, _comiteRepository);
    }

    [Fact]
    public async Task Debe_DevolverElComitePorProgramaYConvocatoriaSinTracking_Cuando_Existe()
    {
        using var cts = new CancellationTokenSource();
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Beca Socioeconómica",
            _jefaturaId,
            _fechaCreacion,
            _coordinadorId);
        comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            _fechaCreacion.AddDays(1),
            _coordinadorId);

        _comiteRepository
            .ObtenerPorProgramaYConvocatoriaAsync(_programaBecaId, _convocatoriaId, cts.Token)
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        var resultado = await _handler.HandleAsync(
            new ObtenerComitePorProgramaQuery(_programaBecaId, _convocatoriaId),
            cts.Token);

        resultado.Id.Should().Be(comite.Id);
        resultado.ProgramaBecaId.Should().Be(_programaBecaId);
        resultado.ConvocatoriaId.Should().Be(_convocatoriaId);
        resultado.Nombre.Should().Be("Comité Beca Socioeconómica");
        resultado.Miembros.Should().HaveCount(2);

        await _comiteRepository.Received(1)
            .ObtenerPorProgramaYConvocatoriaAsync(_programaBecaId, _convocatoriaId, cts.Token);
        await _comiteRepository.DidNotReceiveWithAnyArgs()
            .ObtenerParaActualizarPorIdAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(
            new ObtenerComitePorProgramaQuery(_programaBecaId, _convocatoriaId),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(excepcion => excepcion.Codigo == "NO_AUTENTICADO" && excepcion.EstadoHttp == 401);

        await _comiteRepository.DidNotReceiveWithAnyArgs()
            .ObtenerPorProgramaYConvocatoriaAsync(default, default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon404_Cuando_NoExisteComiteParaProgramaYConvocatoria()
    {
        var accion = () => _handler.HandleAsync(
            new ObtenerComitePorProgramaQuery(_programaBecaId, _convocatoriaId),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ComiteNoEncontradoException>()
            .Where(excepcion => excepcion.Codigo == "COMITE_NO_ENCONTRADO" && excepcion.EstadoHttp == 404);
    }
}
