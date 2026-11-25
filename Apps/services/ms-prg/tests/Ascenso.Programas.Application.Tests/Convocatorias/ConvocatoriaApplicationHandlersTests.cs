using Ascenso.Programas.Application.Convocatorias.Abstracciones;
using Ascenso.Programas.Application.Convocatorias.Comandos.AgregarDocumentoRequerido;
using Ascenso.Programas.Application.Convocatorias.Comandos.EditarConvocatoriaBorrador;
using Ascenso.Programas.Application.Convocatorias.Comandos.EliminarDocumentoRequerido;
using Ascenso.Programas.Application.Convocatorias.Comandos.PublicarConvocatoria;
using Ascenso.Programas.Application.Convocatorias.Consultas.ConsultarConvocatorias;
using Ascenso.Programas.Application.Convocatorias.Consultas.ConsultarConvocatoriasAbiertas;
using Ascenso.Programas.Application.Convocatorias.Consultas.ObtenerConvocatoriaPorId;
using Ascenso.Programas.Application.Convocatorias.DTOs;
using Ascenso.Programas.Application.Convocatorias.EventosIntegracion;
using Ascenso.Programas.Application.Convocatorias.Servicios;
using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Domain.Convocatorias.Agregados;
using Ascenso.Programas.Domain.Convocatorias.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Programas.Application.Tests.Convocatorias;

public sealed class ConvocatoriaApplicationHandlersTests
{
    private static readonly Guid _programaId = Guid.Parse("01966000-0000-7000-8000-000000000101");
    private static readonly Guid _universidadId = Guid.Parse("01966000-0000-7000-8000-000000000301");
    private static readonly Guid _carreraId = Guid.Parse("01966000-0000-7000-8000-000000000401");
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000501");
    private static readonly Guid _periodoGuid = Guid.Parse("01966000-0000-7000-8000-000000000601");
    private static readonly string _periodoId = _periodoGuid.ToString("D");
    private static readonly DateTimeOffset _ahora = new(2026, 11, 25, 16, 0, 0, TimeSpan.Zero);

    private readonly IConvocatoriaRepository _convocatoriaRepository = Substitute.For<IConvocatoriaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICatalogoConvocatoriaReader _catalogoReader = Substitute.For<ICatalogoConvocatoriaReader>();
    private readonly IPeriodoAcademicoClient _periodoClient = Substitute.For<IPeriodoAcademicoClient>();
    private readonly IRelojConvocatorias _reloj = Substitute.For<IRelojConvocatorias>();
    private readonly IIntegrationEventPublisher _eventPublisher = Substitute.For<IIntegrationEventPublisher>();

    public ConvocatoriaApplicationHandlersTests()
    {
        _reloj.Ahora.Returns(_ahora);
        _catalogoReader.ObtenerProgramaAsync(_programaId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(ProgramaValido());
        _periodoClient.ObtenerPorIdAsync(_periodoId, Arg.Any<CancellationToken>())
            .Returns(new PeriodoAcademicoInformacion(_periodoGuid, "2026-II", "VIGENTE"));
    }

    [Fact]
    public async Task Debe_EditarUnBorradorYGuardarLosCambios()
    {
        var convocatoria = CrearConvocatoria();
        _convocatoriaRepository.ObtenerPorIdAsync(convocatoria.Id, Arg.Any<CancellationToken>())
            .Returns(convocatoria);

        var resultado = await CrearHandlerEdicion().HandleAsync(ComandoEdicion(convocatoria.Id));

        resultado.Nombre.Should().Be("Convocatoria actualizada");
        resultado.CuposAsignados.Should().Be(4);
        convocatoria.PeriodoAcademicoId.Should().Be(_periodoId);
        _convocatoriaRepository.Received(1).Actualizar(convocatoria);
        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarEdicion_CuandoLaConvocatoriaNoEstaEnBorrador()
    {
        var convocatoria = CrearConvocatoria(publicada: true);
        _convocatoriaRepository.ObtenerPorIdAsync(convocatoria.Id, Arg.Any<CancellationToken>())
            .Returns(convocatoria);

        await CrearHandlerEdicion().Invoking(handler => handler.HandleAsync(ComandoEdicion(convocatoria.Id)))
            .Should().ThrowAsync<ConvocatoriaNoEditableException>();

        _convocatoriaRepository.DidNotReceive().Actualizar(Arg.Any<Convocatoria>());
        await _unitOfWork.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_PublicarConvocatoriaYEmitirEvento_CuandoRevalidaDisponibilidad()
    {
        var convocatoria = CrearConvocatoria(conDocumento: true);
        _convocatoriaRepository.ObtenerPorIdAsync(convocatoria.Id, Arg.Any<CancellationToken>())
            .Returns(convocatoria);

        var resultado = await CrearHandlerPublicacion().HandleAsync(
            new PublicarConvocatoriaCommand(convocatoria.Id, _usuarioId));

        resultado.Estado.Should().Be("PUBLICADA");
        resultado.FechaPublicacion.Should().Be(_ahora);
        await _catalogoReader.Received(1).ObtenerProgramaAsync(
            _programaId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
        await _periodoClient.Received(1).ObtenerPorIdAsync(
            _periodoId, Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublicarAsync(
            Arg.Any<ConvocatoriaPublicada>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarPublicacion_CuandoFaltanDocumentos()
    {
        var convocatoria = CrearConvocatoria();
        _convocatoriaRepository.ObtenerPorIdAsync(convocatoria.Id, Arg.Any<CancellationToken>())
            .Returns(convocatoria);

        await CrearHandlerPublicacion().Invoking(handler => handler.HandleAsync(
                new PublicarConvocatoriaCommand(convocatoria.Id, _usuarioId)))
            .Should().ThrowAsync<ConvocatoriaSinDocumentosException>();

        convocatoria.Estado.Should().Be(EstadoConvocatoria.Borrador);
        await _unitOfWork.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RevalidarCuposAlPublicar()
    {
        var convocatoria = CrearConvocatoria(conDocumento: true);
        _convocatoriaRepository.ObtenerPorIdAsync(convocatoria.Id, Arg.Any<CancellationToken>())
            .Returns(convocatoria);
        _catalogoReader.ObtenerProgramaAsync(_programaId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(ProgramaValido(cuposDisponibles: 2));

        await CrearHandlerPublicacion().Invoking(handler => handler.HandleAsync(
                new PublicarConvocatoriaCommand(convocatoria.Id, _usuarioId)))
            .Should().ThrowAsync<CuposInsuficientesException>();

        convocatoria.Estado.Should().Be(EstadoConvocatoria.Borrador);
        await _unitOfWork.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RevalidarProgramaYPeriodoAlPublicar()
    {
        var convocatoria = CrearConvocatoria(conDocumento: true);
        _convocatoriaRepository.ObtenerPorIdAsync(convocatoria.Id, Arg.Any<CancellationToken>())
            .Returns(convocatoria);
        _catalogoReader.ObtenerProgramaAsync(_programaId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((ProgramaParaConvocatoria?)null);

        await CrearHandlerPublicacion().Invoking(handler => handler.HandleAsync(
                new PublicarConvocatoriaCommand(convocatoria.Id, _usuarioId)))
            .Should().ThrowAsync<ProgramaNoHabilitadoException>();

        convocatoria.Estado.Should().Be(EstadoConvocatoria.Borrador);
        await _unitOfWork.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());

        _catalogoReader.ObtenerProgramaAsync(_programaId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(ProgramaValido());
        _periodoClient.ObtenerPorIdAsync(_periodoId, Arg.Any<CancellationToken>())
            .Returns(new PeriodoAcademicoInformacion(_periodoGuid, "2026-II", "CERRADO"));

        await CrearHandlerPublicacion().Invoking(handler => handler.HandleAsync(
                new PublicarConvocatoriaCommand(convocatoria.Id, _usuarioId)))
            .Should().ThrowAsync<PeriodoAcademicoNoVigenteException>();

        convocatoria.Estado.Should().Be(EstadoConvocatoria.Borrador);
        await _unitOfWork.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarPublicacion_CuandoLaFechaDeCierreYaVencio()
    {
        var convocatoria = Convocatoria.CrearBorrador(
            _programaId,
            _periodoId,
            "RF21-2026",
            "Convocatoria vencida",
            new DateTimeOffset(2026, 10, 1, 4, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 21, 3, 59, 59, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 23, 3, 59, 59, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 23, 4, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 31, 3, 59, 59, TimeSpan.Zero),
            true,
            3,
            new CriteriosElegibilidadCiclo(15.00m, 17, 25, 1200.00m, [_universidadId], [_carreraId]),
            _usuarioId,
            _ahora.AddDays(-60));
        convocatoria.AgregarDocumento(
            "IDENTIDAD", "Documento de identidad", null, true, ["application/pdf"], 5, _usuarioId, _ahora);
        _convocatoriaRepository.ObtenerPorIdAsync(convocatoria.Id, Arg.Any<CancellationToken>())
            .Returns(convocatoria);

        await CrearHandlerPublicacion().Invoking(handler => handler.HandleAsync(
                new PublicarConvocatoriaCommand(convocatoria.Id, _usuarioId)))
            .Should().ThrowAsync<ConvocatoriaFechaCierreVencidaException>();

        convocatoria.Estado.Should().Be(EstadoConvocatoria.Borrador);
        await _unitOfWork.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_AgregarYQuitarDocumentoRequeridoEnBorrador()
    {
        var convocatoria = CrearConvocatoria();
        _convocatoriaRepository.ObtenerPorIdAsync(convocatoria.Id, Arg.Any<CancellationToken>())
            .Returns(convocatoria);
        var agregar = new AgregarDocumentoRequeridoCommand(
            convocatoria.Id,
            "IDENTIDAD",
            "Documento de identidad",
            null,
            true,
            ["application/pdf"],
            5,
            _usuarioId);
        var documento = await CrearHandlerAgregarDocumento().HandleAsync(agregar);

        documento.Nombre.Should().Be("Documento de identidad");
        convocatoria.DocumentosRequeridos.Should().ContainSingle();
        await _unitOfWork.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());

        var eliminar = new EliminarDocumentoRequeridoCommand(convocatoria.Id, documento.Id, _usuarioId);
        var resultado = await CrearHandlerEliminarDocumento().HandleAsync(eliminar);

        resultado.DocumentosRequeridos.Should().BeEmpty();
        await _unitOfWork.Received(2).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarGestionDeDocumentos_CuandoLaConvocatoriaEstaPublicada()
    {
        var convocatoria = CrearConvocatoria(publicada: true);
        _convocatoriaRepository.ObtenerPorIdAsync(convocatoria.Id, Arg.Any<CancellationToken>())
            .Returns(convocatoria);
        var agregar = new AgregarDocumentoRequeridoCommand(
            convocatoria.Id, "IDENTIDAD", "Documento", null, true, ["application/pdf"], 5, _usuarioId);

        await CrearHandlerAgregarDocumento().Invoking(handler => handler.HandleAsync(agregar))
            .Should().ThrowAsync<ConvocatoriaNoEditableException>();

        var documentoId = convocatoria.DocumentosRequeridos.Single().Id;
        var eliminar = new EliminarDocumentoRequeridoCommand(convocatoria.Id, documentoId, _usuarioId);
        await CrearHandlerEliminarDocumento().Invoking(handler => handler.HandleAsync(eliminar))
            .Should().ThrowAsync<ConvocatoriaNoEditableException>();

        await _unitOfWork.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_ConsultarConFiltrosYPaginacion()
    {
        var convocatoria = CrearConvocatoria();
        _convocatoriaRepository.ConsultarPaginadoAsync(
                2, 1, EstadoConvocatoria.Publicada, _programaId, "talento", Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Convocatoria>)[convocatoria], 4));

        var respuesta = await new ConsultarConvocatoriasQueryHandler(_convocatoriaRepository).HandleAsync(
            new ConsultarConvocatoriasQuery(2, 1, "PUBLICADA", _programaId, "talento"));

        respuesta.Items.Should().ContainSingle();
        respuesta.Pagina.Should().Be(2);
        respuesta.TamanoPagina.Should().Be(1);
        respuesta.TotalRegistros.Should().Be(4);
        respuesta.Items.Single().Id.Should().Be(convocatoria.Id);
    }

    [Fact]
    public async Task Debe_RechazarPaginacionInvalida()
    {
        var handler = new ConsultarConvocatoriasQueryHandler(_convocatoriaRepository);

        await handler.Invoking(item => item.HandleAsync(
                new ConsultarConvocatoriasQuery(0, 10, null, null, null)))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();

        await _convocatoriaRepository.DidNotReceive().ConsultarPaginadoAsync(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EstadoConvocatoria?>(), Arg.Any<Guid?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_ObtenerDetalleYRechazarUnIdInexistente()
    {
        var convocatoria = CrearConvocatoria(conDocumento: true);
        _convocatoriaRepository.ObtenerPorIdAsync(convocatoria.Id, Arg.Any<CancellationToken>())
            .Returns(convocatoria);
        var handler = new ObtenerConvocatoriaPorIdQueryHandler(_convocatoriaRepository);

        var detalle = await handler.HandleAsync(new ObtenerConvocatoriaPorIdQuery(convocatoria.Id));
        detalle.Id.Should().Be(convocatoria.Id);
        detalle.DocumentosRequeridos.Should().ContainSingle();

        var idInexistente = Guid.NewGuid();
        _convocatoriaRepository.ObtenerPorIdAsync(idInexistente, Arg.Any<CancellationToken>())
            .Returns((Convocatoria?)null);
        await handler.Invoking(item => item.HandleAsync(new ObtenerConvocatoriaPorIdQuery(idInexistente)))
            .Should().ThrowAsync<ConvocatoriaNoEncontradaException>();
    }

    [Fact]
    public async Task Debe_ConsultarConvocatoriasAbiertasConElRelojActual()
    {
        var convocatoria = CrearConvocatoria();
        _convocatoriaRepository.ConsultarAbiertasAsync(_ahora, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Convocatoria>)[convocatoria]);

        var respuesta = await new ConsultarConvocatoriasAbiertasQueryHandler(
            _convocatoriaRepository, _reloj)
            .HandleAsync(new ConsultarConvocatoriasAbiertasQuery());

        respuesta.Should().ContainSingle();
        respuesta.Single().Id.Should().Be(convocatoria.Id);
    }

    private EditarConvocatoriaBorradorCommandHandler CrearHandlerEdicion()
        => new(
            _convocatoriaRepository,
            _unitOfWork,
            CrearValidadorDisponibilidad(),
            new ConvocatoriaTransactionExecutorFalso(),
            new EditarConvocatoriaBorradorCommandValidator());

    private PublicarConvocatoriaCommandHandler CrearHandlerPublicacion()
        => new(
            _convocatoriaRepository,
            _unitOfWork,
            CrearValidadorDisponibilidad(),
            new ConvocatoriaTransactionExecutorFalso(),
            _reloj,
            _eventPublisher);

    private AgregarDocumentoRequeridoCommandHandler CrearHandlerAgregarDocumento()
        => new(
            _convocatoriaRepository,
            _unitOfWork,
            new AgregarDocumentoRequeridoCommandValidator());

    private EliminarDocumentoRequeridoCommandHandler CrearHandlerEliminarDocumento()
        => new(_convocatoriaRepository, _unitOfWork);

    private ValidadorDisponibilidadConvocatoria CrearValidadorDisponibilidad()
        => new(_catalogoReader, _periodoClient, _reloj);

    private EditarConvocatoriaBorradorCommand ComandoEdicion(Guid convocatoriaId)
        => new(
            convocatoriaId,
            _periodoId,
            "Convocatoria actualizada",
            new DateOnly(2026, 12, 1),
            new DateOnly(2026, 12, 20),
            new DateOnly(2026, 12, 22),
            new DateOnly(2026, 12, 22),
            new DateOnly(2026, 12, 31),
            true,
            4,
            new CriteriosElegibilidadCicloDto(
                15.00m, 17, 25, 1200.00m, [_universidadId], [_carreraId]),
            _usuarioId);

    private static Convocatoria CrearConvocatoria(bool conDocumento = false, bool publicada = false)
    {
        var convocatoria = Convocatoria.CrearBorrador(
            _programaId,
            _periodoId,
            "RF21-2026",
            "Convocatoria de prueba",
            new DateTimeOffset(2026, 12, 1, 4, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 21, 3, 59, 59, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 23, 3, 59, 59, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 23, 4, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 31, 3, 59, 59, TimeSpan.Zero),
            true,
            3,
            new CriteriosElegibilidadCiclo(15.00m, 17, 25, 1200.00m, [_universidadId], [_carreraId]),
            _usuarioId,
            _ahora);

        if (conDocumento || publicada)
        {
            convocatoria.AgregarDocumento(
                "IDENTIDAD", "Documento de identidad", null, true, ["application/pdf"], 5, _usuarioId, _ahora);
        }

        if (publicada)
        {
            convocatoria.Publicar(_ahora, _usuarioId);
        }

        return convocatoria;
    }

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
