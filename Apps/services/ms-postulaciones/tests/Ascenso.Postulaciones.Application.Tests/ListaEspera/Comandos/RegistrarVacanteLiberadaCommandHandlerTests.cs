using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.ListaEspera.Comandos.RegistrarVacanteLiberada;
using Ascenso.Postulaciones.Application.ListaEspera.DTOs;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.ListaEspera.Comandos;

public sealed class RegistrarVacanteLiberadaCommandHandlerTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01969000-0000-7000-8000-000000005001");
    private static readonly Guid _programaBecaId = Guid.Parse("01969000-0000-7000-8000-000000005002");
    private static readonly Guid _coordinadorId = Guid.Parse("01969000-0000-7000-8000-000000005003");
    private static readonly DateTimeOffset _fechaIngreso = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahora = new(2026, 11, 27, 15, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IGestorConcurrenciaListaEspera _gestorConcurrencia = Substitute.For<IGestorConcurrenciaListaEspera>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegistrarVacanteLiberadaCommandValidator _validator = new();
    private readonly TimeProvider _timeProvider = new RelojFijo(_ahora);

    private RegistrarVacanteLiberadaCommandHandler CrearHandler()
        => new(_usuarioActual, _gestorConcurrencia, _unitOfWork, _validator, _timeProvider);

    [Fact]
    public async Task Debe_RechazarConValidationException_Cuando_ConvocatoriaEsVaciaOMotivoSupera500Caracteres()
    {
        var handler = CrearHandler();

        var exVacio = await handler
            .Invoking(h => h.HandleAsync(new RegistrarVacanteLiberadaCommand(Guid.Empty)))
            .Should().ThrowAsync<ValidationException>();
        exVacio.Which.Errors.Should().OnlyContain(e => e.ErrorCode == ListaEsperaInvalidaException.CodigoError);

        var motivoLargo = new string('X', RegistrarVacanteLiberadaCommand.LongitudMaximaMotivoVacante + 1);
        var exLargo = await handler
            .Invoking(h => h.HandleAsync(new RegistrarVacanteLiberadaCommand(_convocatoriaId, motivoLargo)))
            .Should().ThrowAsync<ValidationException>();
        exLargo.Which.Errors.Should().OnlyContain(e => e.ErrorCode == ListaEsperaInvalidaException.CodigoError);

        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    [Fact]
    public async Task Debe_MarcarMenorPosicionEnEsperaComoElegibleSinPromover_Cuando_UsuarioEstaAutenticado()
    {
        using var cts = new CancellationTokenSource();
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        var e1 = CrearEntrada(1);
        var e2 = CrearEntrada(2);
        ConfigurarBloqueo([e2, e1]);

        var handler = CrearHandler();
        var dto = await handler.HandleAsync(
            new RegistrarVacanteLiberadaCommand(_convocatoriaId, "Oferta rechazada por postulante"),
            cts.Token);

        dto.Id.Should().Be(e1.Id);
        dto.PosicionOrden.Should().Be(1);
        dto.Estado.Should().Be("ELEGIBLE_POR_VACANTE");
        dto.FechaElegibilidad.Should().Be(_ahora);
        dto.FechaPromocion.Should().BeNull();
        dto.PromovidoPorUsuarioId.Should().BeNull();
        dto.Version.Should().Be(2);

        e1.UsuarioModificacionId.Should().Be(_coordinadorId);
        e2.Estado.Should().Be(EstadoListaEspera.EnEspera);
        e2.Version.Should().Be(1);

        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_UsarUsuarioSistemaComoActor_Cuando_InvocacionEsServicioInternoSinUsuarioAutenticado()
    {
        using var cts = new CancellationTokenSource();
        _usuarioActual.EstaAutenticado.Returns(false);
        _usuarioActual.UsuarioId.Returns(Guid.Empty);

        var e1 = CrearEntrada(1);
        e1.MarcarElegiblePorVacante(_fechaIngreso.AddHours(2), _coordinadorId);
        var e2 = CrearEntrada(2);
        ConfigurarBloqueo([e1, e2]);

        var handler = CrearHandler();
        var dto = await handler.HandleAsync(new RegistrarVacanteLiberadaCommand(_convocatoriaId), cts.Token);

        dto.Id.Should().Be(e2.Id);
        dto.PosicionOrden.Should().Be(2);
        dto.Estado.Should().Be("ELEGIBLE_POR_VACANTE");
        dto.FechaElegibilidad.Should().Be(_ahora);
        e2.UsuarioModificacionId.Should().Be(UsuarioSistema.Id);
        e1.Estado.Should().Be(EstadoListaEspera.ElegiblePorVacante);
        await _unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_LanzarListaEsperaSinCandidatos404SinGuardar_Cuando_NoHayEntradasEnEspera()
    {
        _usuarioActual.EstaAutenticado.Returns(true);
        _usuarioActual.UsuarioId.Returns(_coordinadorId);

        var e1 = CrearEntrada(1);
        e1.MarcarElegiblePorVacante(_fechaIngreso.AddHours(1), _coordinadorId);
        ConfigurarBloqueo([e1]);

        var handler = CrearHandler();
        var ex = await handler
            .Invoking(h => h.HandleAsync(new RegistrarVacanteLiberadaCommand(_convocatoriaId)))
            .Should().ThrowAsync<ListaEsperaSinCandidatosException>();

        ex.Which.EstadoHttp.Should().Be(404);
        ex.Which.Codigo.Should().Be("LISTA_ESPERA_SIN_CANDIDATOS");
        await _unitOfWork.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
    }

    private void ConfigurarBloqueo(IReadOnlyList<EntradaListaEspera> entradas)
    {
        _gestorConcurrencia
            .EjecutarConBloqueoConvocatoriaAsync(
                _convocatoriaId,
                Arg.Any<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<EntradaListaEsperaDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var accion = callInfo.ArgAt<Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<EntradaListaEsperaDto>>>(1);
                var token = callInfo.ArgAt<CancellationToken>(2);
                return accion(entradas, token);
            });
    }

    private static EntradaListaEspera CrearEntrada(int posicion)
        => EntradaListaEspera.Crear(
            _convocatoriaId,
            _programaBecaId,
            Guid.NewGuid(),
            posicion,
            1500.00m,
            _fechaIngreso,
            _coordinadorId);

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
