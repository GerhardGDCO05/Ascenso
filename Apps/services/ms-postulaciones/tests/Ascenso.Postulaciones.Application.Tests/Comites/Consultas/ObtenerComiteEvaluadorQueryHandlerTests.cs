using Ascenso.Postulaciones.Application.Comites.Consultas.ObtenerComiteEvaluador;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Comites.Consultas;

public sealed class ObtenerComiteEvaluadorQueryHandlerTests
{
    private static readonly Guid _coordinadorId = Guid.Parse("01966000-0000-7000-8000-000000000721");
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000722");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000723");
    private static readonly Guid _jefaturaId = Guid.Parse("01966000-0000-7000-8000-000000000724");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IComiteEvaluadorRepository _comiteRepository = Substitute.For<IComiteEvaluadorRepository>();
    private readonly ObtenerComiteEvaluadorQueryHandler _handler;

    public ObtenerComiteEvaluadorQueryHandlerTests()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        _comiteRepository
            .ObtenerPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ComiteEvaluador?>(null));

        _handler = new ObtenerComiteEvaluadorQueryHandler(_usuarioActual, _comiteRepository);
    }

    [Fact]
    public async Task Debe_DevolverElComiteConMiembrosSinTracking_Cuando_Existe()
    {
        using var cts = new CancellationTokenSource();
        var comite = CrearComiteConHistorial();

        _comiteRepository
            .ObtenerPorIdAsync(comite.Id, cts.Token)
            .Returns(Task.FromResult<ComiteEvaluador?>(comite));

        var resultado = await _handler.HandleAsync(
            new ObtenerComiteEvaluadorQuery(comite.Id),
            cts.Token);

        resultado.Id.Should().Be(comite.Id);
        resultado.ProgramaBecaId.Should().Be(_programaBecaId);
        resultado.ConvocatoriaId.Should().Be(_convocatoriaId);
        resultado.Nombre.Should().Be("Comité Beca Excelencia");
        resultado.JefaturaUsuarioId.Should().Be(_jefaturaId);
        resultado.Miembros.Should().HaveCount(2);

        await _comiteRepository.Received(1).ObtenerPorIdAsync(comite.Id, cts.Token);
        await _comiteRepository.DidNotReceiveWithAnyArgs()
            .ObtenerParaActualizarPorIdAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon401_Cuando_NoHayUsuarioAutenticado()
    {
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var accion = () => _handler.HandleAsync(
            new ObtenerComiteEvaluadorQuery(Guid.NewGuid()),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<NoAutenticadoException>()
            .Where(excepcion => excepcion.Codigo == "NO_AUTENTICADO" && excepcion.EstadoHttp == 401);

        await _comiteRepository.DidNotReceiveWithAnyArgs().ObtenerPorIdAsync(default, default);
    }

    [Fact]
    public async Task Debe_RechazarCon404_Cuando_ElComiteNoExiste()
    {
        var accion = () => _handler.HandleAsync(
            new ObtenerComiteEvaluadorQuery(Guid.NewGuid()),
            CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ComiteNoEncontradoException>()
            .Where(excepcion => excepcion.Codigo == "COMITE_NO_ENCONTRADO" && excepcion.EstadoHttp == 404);
    }

    private static ComiteEvaluador CrearComiteConHistorial()
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Beca Excelencia",
            _jefaturaId,
            _fechaCreacion,
            _coordinadorId);

        comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            _fechaCreacion.AddDays(1),
            _coordinadorId);

        return comite;
    }
}
