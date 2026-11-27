using Ascenso.Postulaciones.Application.ListaEspera.Consultas.ObtenerListaEsperaConvocatoria;
using Ascenso.Postulaciones.Application.ListaEspera.Consultas.ObtenerSiguienteCandidatoListaEspera;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.ListaEspera.Consultas;

public sealed class ConsultasListaEsperaTests
{
    private static readonly Guid _convocatoriaId = Guid.Parse("01969000-0000-7000-8000-000000004001");
    private static readonly Guid _programaBecaId = Guid.Parse("01969000-0000-7000-8000-000000004002");
    private static readonly Guid _coordinadorId = Guid.Parse("01969000-0000-7000-8000-000000004003");
    private static readonly DateTimeOffset _fechaBase = new(2026, 11, 27, 10, 0, 0, TimeSpan.Zero);

    private readonly IEntradaListaEsperaRepository _entradaRepository = Substitute.For<IEntradaListaEsperaRepository>();

    [Fact]
    public async Task Debe_RetornarListaPaginadaConContadoresGlobalesYFiltroPorEstado_Cuando_SeConsultaListaEspera()
    {
        using var cts = new CancellationTokenSource();
        var e1 = CrearEntrada(1);
        var e2 = CrearEntrada(2);
        var e3 = CrearEntrada(3);
        e1.Promover(_fechaBase.AddHours(1), _coordinadorId);
        e2.MarcarElegiblePorVacante(_fechaBase.AddHours(2), UsuarioSistema.Id);

        _entradaRepository
            .ListarPorConvocatoriaAsync(_convocatoriaId, null, cts.Token)
            .Returns([e3, e1, e2]);

        var handler = new ObtenerListaEsperaConvocatoriaQueryHandler(
            _entradaRepository,
            new ObtenerListaEsperaConvocatoriaQueryValidator());

        var dto = await handler.HandleAsync(
            new ObtenerListaEsperaConvocatoriaQuery(
                _convocatoriaId,
                Estado: "elegible_por_vacante",
                Pagina: 1,
                Tamano: 10),
            cts.Token);

        dto.ConvocatoriaId.Should().Be(_convocatoriaId);
        dto.TotalEnListaEspera.Should().Be(3);
        dto.TotalEnEspera.Should().Be(1);
        dto.TotalElegiblesPorVacante.Should().Be(1);
        dto.TotalPromovidas.Should().Be(1);
        dto.Entradas.Total.Should().Be(1);
        dto.Entradas.Items.Should().ContainSingle(i => i.Id == e2.Id && i.PosicionOrden == 2 && i.Estado == "ELEGIBLE_POR_VACANTE");
    }

    [Theory]
    [InlineData(true, null, 1, 20)]
    [InlineData(false, null, 0, 20)]
    [InlineData(false, null, 1, 0)]
    [InlineData(false, null, 1, 101)]
    [InlineData(false, "EXPIRADA", 1, 20)]
    public async Task Debe_RechazarConValidationException_Cuando_ParametrosDeConsultaListaEsperaSonInvalidos(
        bool convocatoriaVacia,
        string? estado,
        int pagina,
        int tamano)
    {
        var handler = new ObtenerListaEsperaConvocatoriaQueryHandler(
            _entradaRepository,
            new ObtenerListaEsperaConvocatoriaQueryValidator());

        var query = new ObtenerListaEsperaConvocatoriaQuery(
            convocatoriaVacia ? Guid.Empty : _convocatoriaId,
            estado,
            pagina,
            tamano);

        var ex = await handler.Invoking(h => h.HandleAsync(query))
            .Should().ThrowAsync<ValidationException>();

        ex.Which.Errors.Should().OnlyContain(e => e.ErrorCode == ListaEsperaInvalidaException.CodigoError);
        await _entradaRepository.DidNotReceiveWithAnyArgs().ListarPorConvocatoriaAsync(default, default, default);
    }

    [Fact]
    public async Task Debe_RetornarSiguienteCandidatoPendiente_Cuando_ExisteEnColaFifo()
    {
        using var cts = new CancellationTokenSource();
        var e2 = CrearEntrada(2);
        e2.MarcarElegiblePorVacante(_fechaBase.AddHours(1), UsuarioSistema.Id);

        _entradaRepository
            .ObtenerSiguientePendientePorConvocatoriaAsync(_convocatoriaId, cts.Token)
            .Returns(e2);

        var handler = new ObtenerSiguienteCandidatoListaEsperaQueryHandler(
            _entradaRepository,
            new ObtenerSiguienteCandidatoListaEsperaQueryValidator());

        var dto = await handler.HandleAsync(
            new ObtenerSiguienteCandidatoListaEsperaQuery(_convocatoriaId),
            cts.Token);

        dto.Id.Should().Be(e2.Id);
        dto.PosicionOrden.Should().Be(2);
        dto.Estado.Should().Be("ELEGIBLE_POR_VACANTE");
    }

    [Fact]
    public async Task Debe_LanzarListaEsperaSinCandidatos404_Cuando_NoHaySiguienteCandidatoPendiente()
    {
        _entradaRepository
            .ObtenerSiguientePendientePorConvocatoriaAsync(_convocatoriaId, Arg.Any<CancellationToken>())
            .Returns((EntradaListaEspera?)null);

        var handler = new ObtenerSiguienteCandidatoListaEsperaQueryHandler(
            _entradaRepository,
            new ObtenerSiguienteCandidatoListaEsperaQueryValidator());

        var ex = await handler
            .Invoking(h => h.HandleAsync(new ObtenerSiguienteCandidatoListaEsperaQuery(_convocatoriaId)))
            .Should().ThrowAsync<ListaEsperaSinCandidatosException>();

        ex.Which.EstadoHttp.Should().Be(404);
        ex.Which.Codigo.Should().Be("LISTA_ESPERA_SIN_CANDIDATOS");
    }

    [Fact]
    public async Task Debe_RechazarConValidationException_Cuando_ConvocatoriaIdEsVacioEnSiguienteCandidato()
    {
        var handler = new ObtenerSiguienteCandidatoListaEsperaQueryHandler(
            _entradaRepository,
            new ObtenerSiguienteCandidatoListaEsperaQueryValidator());

        var ex = await handler
            .Invoking(h => h.HandleAsync(new ObtenerSiguienteCandidatoListaEsperaQuery(Guid.Empty)))
            .Should().ThrowAsync<ValidationException>();

        ex.Which.Errors.Should().OnlyContain(e => e.ErrorCode == ListaEsperaInvalidaException.CodigoError);
        await _entradaRepository.DidNotReceiveWithAnyArgs().ObtenerSiguientePendientePorConvocatoriaAsync(default, default);
    }

    private static EntradaListaEspera CrearEntrada(int posicion)
        => EntradaListaEspera.Crear(
            _convocatoriaId,
            _programaBecaId,
            Guid.NewGuid(),
            posicion,
            1500.00m,
            _fechaBase,
            _coordinadorId);
}
