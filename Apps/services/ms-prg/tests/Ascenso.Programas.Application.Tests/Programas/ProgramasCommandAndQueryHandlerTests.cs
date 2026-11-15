using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Application.Programas.Comandos.AsignarPresupuestoDonantes;
using Ascenso.Programas.Application.Programas.Comandos.CerrarPeriodoPrograma;
using Ascenso.Programas.Application.Programas.Comandos.ConfigurarCriteriosElegibilidad;
using Ascenso.Programas.Application.Programas.Comandos.ConfirmarReservaPresupuesto;
using Ascenso.Programas.Application.Programas.Comandos.CrearProgramaBeca;
using Ascenso.Programas.Application.Programas.Comandos.LiberarReservaPresupuesto;
using Ascenso.Programas.Application.Programas.Comandos.ReajustarPresupuestoDonante;
using Ascenso.Programas.Application.Programas.Comandos.ReservarPresupuestoPrograma;
using Ascenso.Programas.Application.Programas.Consultas.ConsultarMovimientosPresupuestarios;
using Ascenso.Programas.Application.Programas.Consultas.ConsultarProgramas;
using Ascenso.Programas.Application.Programas.Consultas.ObtenerBalancePrograma;
using Ascenso.Programas.Application.Programas.Consultas.ObtenerProgramaPorId;
using Ascenso.Programas.Application.Programas.DTOs;
using Ascenso.Programas.Application.Programas.EventosIntegracion;
using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Donantes.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using Ascenso.Programas.Domain.Programas.Agregados;
using Ascenso.Programas.Domain.Programas.ObjetosValor;
using Ascenso.Programas.Domain.Universidades.Agregados;
using Ascenso.Programas.Domain.Universidades.ObjetosValor;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Programas.Application.Tests.Programas;

public sealed class ProgramasCommandAndQueryHandlerTests
{
    private static readonly Guid _actorId = Guid.Parse("01966000-0000-7000-8000-000000000501");

    private readonly IProgramaBecaRepository _programaRepository = Substitute.For<IProgramaBecaRepository>();
    private readonly IDonanteRepository _donanteRepository = Substitute.For<IDonanteRepository>();
    private readonly IUniversidadAliadaRepository _universidadRepository = Substitute.For<IUniversidadAliadaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IIntegrationEventPublisher _eventPublisher = Substitute.For<IIntegrationEventPublisher>();

    private static CrearProgramaBecaCommand CrearComandoValido(string codigo = "BECA-2026-A")
        => new(
            codigo,
            "Beca Talento Nacional",
            "Programa de apoyo universitario completo.",
            new PoliticaCoberturaDto("TOTAL", 100.00m, 0.00m, "USD"),
            20,
            10,
            "2026-1",
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 12, 20),
            false,
            true,
            "Compromiso de rendimiento académico.",
            _actorId);

    private static ProgramaBeca CrearProgramaConSaldo(decimal saldo = 20000.00m)
    {
        var programa = ProgramaBeca.Crear(
            "BECA-TEST",
            "Programa Prueba",
            "Descripción prueba",
            new PoliticaCobertura(ModalidadCobertura.Total, 100.00m, 0.00m, "USD"),
            15,
            8,
            "2026-1",
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 12, 20),
            false,
            true,
            null,
            _actorId);

        if (saldo > 0m)
        {
            programa.AsignarFondoDonante(Guid.NewGuid(), "2026-1", saldo, _actorId);
        }

        return programa;
    }

    private static Donante CrearDonanteConSaldo(decimal saldo = 30000.00m)
    {
        var donante = Donante.Crear(
            TipoDonante.Empresa,
            "Corporación Impulso S.A.",
            "J-30998877-1",
            "rse@impulso.com",
            "+58 212 8889900",
            "Caracas",
            new ContactoDonante("Ana Pérez", "Gerente", "ana@impulso.com", "+58 412 8889900"),
            _actorId);

        donante.RegistrarAporte(
            saldo,
            "USD",
            "2026-1",
            DateTimeOffset.UtcNow,
            "Mercantil",
            $"REF-{Guid.NewGuid():N}"[..16],
            $"/donantes/{donante.Id:D}/aportes/{Guid.CreateVersion7():D}.pdf",
            _actorId);

        return donante;
    }

    [Fact]
    public async Task Debe_CrearProgramaEnBorradorYPublicarEvento_Cuando_CodigoEsUnico_HU11_1()
    {
        _programaRepository.ExisteCodigoAsync("BECA-2026-A", Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new CrearProgramaBecaCommandHandler(
            _programaRepository,
            _unitOfWork,
            _eventPublisher,
            new CrearProgramaBecaCommandValidator());

        var resultado = await handler.HandleAsync(CrearComandoValido());

        resultado.Codigo.Should().Be("BECA-2026-A");
        resultado.Estado.Should().Be("BORRADOR");
        resultado.PresupuestoTotalAsignado.Should().Be(0.00m);
        resultado.CuposOcupados.Should().Be(0);

        await _programaRepository.Received(1)
            .AgregarAsync(Arg.Any<ProgramaBeca>(), Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1)
            .PublicarAsync(Arg.Any<ProgramaBecaCreado>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1)
            .GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarCreacionDePrograma_Cuando_CodigoEstaDuplicado_HU11_1()
    {
        _programaRepository.ExisteCodigoAsync("BECA-DUP", Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new CrearProgramaBecaCommandHandler(
            _programaRepository,
            _unitOfWork,
            _eventPublisher,
            new CrearProgramaBecaCommandValidator());

        var accion = () => handler.HandleAsync(CrearComandoValido("beca-dup"));

        var ex = (await accion.Should().ThrowAsync<CodigoProgramaDuplicadoException>()).Which;
        ex.EstadoHttp.Should().Be(409);
        await _unitOfWork.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_AsignarYReajustarPresupuestoConDonante_EnMismaTransaccion_HU11_2()
    {
        var programa = CrearProgramaConSaldo(0.00m);
        var donante = CrearDonanteConSaldo(40000.00m);

        _programaRepository.ObtenerPorIdAsync(programa.Id, Arg.Any<CancellationToken>())
            .Returns(programa);
        _donanteRepository.ObtenerPorIdAsync(donante.Id, Arg.Any<CancellationToken>())
            .Returns(donante);

        var asignarHandler = new AsignarPresupuestoDonantesCommandHandler(
            _programaRepository,
            _donanteRepository,
            _unitOfWork,
            _eventPublisher,
            new AsignarPresupuestoDonantesCommandValidator());

        var detalleAsignado = await asignarHandler.HandleAsync(new AsignarPresupuestoDonantesCommand(
            programa.Id,
            [new ItemAsignacionDonanteCommand(donante.Id, "2026-1", 12000.00m)],
            _actorId));

        detalleAsignado.PresupuestoTotalAsignado.Should().Be(12000.00m);
        donante.SaldoDisponibleAsignacion.Should().Be(28000.00m);
        await _eventPublisher.Received(1)
            .PublicarAsync(Arg.Any<PresupuestoProgramaAsignado>(), Arg.Any<CancellationToken>());

        var asignacionId = detalleAsignado.AsignacionesFondo.Single().Id;

        var reajustarHandler = new ReajustarPresupuestoDonanteCommandHandler(
            _programaRepository,
            _donanteRepository,
            _unitOfWork,
            new ReajustarPresupuestoDonanteCommandValidator());

        var detalleReajustado = await reajustarHandler.HandleAsync(new ReajustarPresupuestoDonanteCommand(
            programa.Id,
            asignacionId,
            4000.00m,
            _actorId));

        detalleReajustado.PresupuestoTotalAsignado.Should().Be(8000.00m);
        donante.SaldoDisponibleAsignacion.Should().Be(32000.00m);
    }

    [Fact]
    public async Task Debe_ConfigurarElegibilidadYTransicionarAConfigurado_Cuando_UniversidadTieneConvenioVigente_HU11_3()
    {
        var programa = CrearProgramaConSaldo(25000.00m);
        var universidad = UniversidadAliada.CrearConConvenioInicial(
            "UCAB",
            "Universidad Católica Andrés Bello",
            "Caracas",
            new ContactoEnlace("Laura Ríos", "lrios@ucab.edu.ve", "+58 212 4074111"),
            null,
            null,
            "CONV-2026",
            "Convenio vigente.",
            new DateOnly(2026, 1, 1),
            new DateOnly(2030, 12, 31),
            "https://storage.googleapis.com/ascenso/convenios/ucab.pdf",
            true,
            _actorId);
        var carrera = universidad.AgregarCarreraElegible("ING-INF", "Ingeniería Informática", 10, _actorId);

        _programaRepository.ObtenerPorIdAsync(programa.Id, Arg.Any<CancellationToken>())
            .Returns(programa);
        _universidadRepository.ObtenerPorIdAsync(universidad.Id, Arg.Any<CancellationToken>())
            .Returns(universidad);

        var handler = new ConfigurarCriteriosElegibilidadCommandHandler(
            _programaRepository,
            _universidadRepository,
            _unitOfWork,
            _eventPublisher,
            new ConfigurarCriteriosElegibilidadCommandValidator());

        var resultado = await handler.HandleAsync(new ConfigurarCriteriosElegibilidadCommand(
            programa.Id,
            new CriteriosElegibilidadProgramaDto(15.50m, 17, 24, 1500.00m, [universidad.Id], [carrera.Id]),
            new CondicionesPermanenciaDto(14.00m, 4, 1, 15, "SEMESTRAL", "SUSPENSION", 0.50m, 2, true),
            _actorId));

        resultado.Estado.Should().Be("CONFIGURADO");
        resultado.CriteriosElegibilidad.Should().NotBeNull();
        resultado.CondicionesPermanencia.Should().NotBeNull();
        await _eventPublisher.Received(1)
            .PublicarAsync(Arg.Any<ProgramaBecaConfigurado>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_RechazarConfiguracion_Cuando_UniversidadNoTieneConvenioVigente_HU11_3()
    {
        var programa = CrearProgramaConSaldo(25000.00m);
        var universidad = UniversidadAliada.CrearConConvenioInicial(
            "USB",
            "Universidad Simón Bolívar",
            "Sartenejas",
            new ContactoEnlace("Carlos", "c@usb.ve", "+58 212 9063111"),
            null,
            null,
            "CONV-USB",
            "Convenio vencido.",
            new DateOnly(2024, 1, 1),
            new DateOnly(2025, 1, 1),
            "https://storage.googleapis.com/ascenso/convenios/usb.pdf",
            false,
            _actorId);
        var carrera = universidad.AgregarCarreraElegible("ING-COMP", "Ingeniería de Computación", 10, _actorId);
        universidad.MarcarConvenioVencido(universidad.Convenios.Single().Id, _actorId);

        _programaRepository.ObtenerPorIdAsync(programa.Id, Arg.Any<CancellationToken>())
            .Returns(programa);
        _universidadRepository.ObtenerPorIdAsync(universidad.Id, Arg.Any<CancellationToken>())
            .Returns(universidad);

        var handler = new ConfigurarCriteriosElegibilidadCommandHandler(
            _programaRepository,
            _universidadRepository,
            _unitOfWork,
            _eventPublisher,
            new ConfigurarCriteriosElegibilidadCommandValidator());

        var accion = () => handler.HandleAsync(new ConfigurarCriteriosElegibilidadCommand(
            programa.Id,
            new CriteriosElegibilidadProgramaDto(15.00m, 17, 25, 1000.00m, [universidad.Id], [carrera.Id]),
            new CondicionesPermanenciaDto(14.00m, 4, 1, 10, "SEMESTRAL", "LLAMADO_ATENCION", 0.50m, 1, false),
            _actorId));

        var ex = (await accion.Should().ThrowAsync<ProgramaNoConfigurableException>()).Which;
        ex.EstadoHttp.Should().Be(422);
        ex.Codigo.Should().Be(ProgramaNoConfigurableException.CodigoError);
    }

    [Fact]
    public async Task Debe_ConsultarProgramasPaginadosYObtenerDetallePorId_HU11_3()
    {
        var programa = CrearProgramaConSaldo(10000.00m);

        _programaRepository.ConsultarPaginadoAsync(
                1,
                10,
                EstadoProgramaBeca.Borrador,
                "2026-1",
                ModalidadCobertura.Total,
                null,
                Arg.Any<CancellationToken>())
            .Returns(([programa], 1));

        _programaRepository.ObtenerPorIdAsync(programa.Id, Arg.Any<CancellationToken>())
            .Returns(programa);

        var consultaHandler = new ConsultarProgramasQueryHandler(_programaRepository);
        var paginado = await consultaHandler.HandleAsync(new ConsultarProgramasQuery(
            1,
            10,
            "BORRADOR",
            "2026-1",
            "TOTAL",
            null));

        paginado.TotalRegistros.Should().Be(1);
        paginado.Items.Should().ContainSingle(item => item.Id == programa.Id);

        var detalleHandler = new ObtenerProgramaPorIdQueryHandler(_programaRepository);
        var detalle = await detalleHandler.HandleAsync(new ObtenerProgramaPorIdQuery(programa.Id));

        detalle.Id.Should().Be(programa.Id);
        detalle.AsignacionesFondo.Should().ContainSingle();
    }

    [Fact]
    public async Task Debe_ObtenerBalanceYMovimientosPaginados_YEjecutarCicloDeReservasYCierre_HU12_1_HU12_2()
    {
        var programa = CrearProgramaConSaldo(20000.00m);
        programa.ConfigurarElegibilidadYPermanencia(
            new CriteriosElegibilidadPrograma(15.00m, 17, 25, 1200.00m, [Guid.NewGuid()], [Guid.NewGuid()]),
            new CondicionesPermanencia(14.00m, 4, 1, 10, FrecuenciaReporteAcademico.Semestral, ConsecuenciaIncumplimiento.Suspension, 0.50m, 2, true),
            todasLasUniversidadesTienenConvenioVigente: true,
            todasLasCarrerasSonElegibles: true,
            _actorId);
        programa.Activar(_actorId);

        var refConfirmar = Guid.Parse("01966000-0000-7000-8000-000000000801");
        var refLiberar = Guid.Parse("01966000-0000-7000-8000-000000000802");

        var (reserva1, mov1, _) = programa.ReservarPresupuesto(refConfirmar, "2026-1", 4000.00m, "Reserva 1", _actorId);
        var (reserva2, mov2, _) = programa.ReservarPresupuesto(refLiberar, "2026-1", 2000.00m, "Reserva 2", _actorId);

        _programaRepository.ReservarPresupuestoAtomicoAsync(
                programa.Id,
                refConfirmar,
                "2026-1",
                4000.00m,
                "Reserva 1",
                _actorId,
                Arg.Any<CancellationToken>())
            .Returns((programa, reserva1, mov1, false));

        _programaRepository.ObtenerPorIdAsync(programa.Id, Arg.Any<CancellationToken>())
            .Returns(programa);

        var reservarHandler = new ReservarPresupuestoProgramaCommandHandler(
            _programaRepository,
            _unitOfWork,
            _eventPublisher,
            new ReservarPresupuestoProgramaCommandValidator());

        var respuestaReserva = await reservarHandler.HandleAsync(new ReservarPresupuestoProgramaCommand(
            programa.Id,
            refConfirmar,
            "2026-1",
            4000.00m,
            "Reserva 1",
            _actorId));

        respuestaReserva.YaExistia.Should().BeFalse();
        respuestaReserva.BalanceResultante.PresupuestoReservado.Should().Be(6000.00m);
        await _eventPublisher.Received(1)
            .PublicarAsync(Arg.Any<PresupuestoProgramaReservado>(), Arg.Any<CancellationToken>());

        var confirmarHandler = new ConfirmarReservaPresupuestoCommandHandler(
            _programaRepository,
            _unitOfWork,
            _eventPublisher,
            new ConfirmarReservaPresupuestoCommandValidator());

        var respuestaConfirmada = await confirmarHandler.HandleAsync(new ConfirmarReservaPresupuestoCommand(
            programa.Id,
            refConfirmar,
            "Desembolso confirmado",
            _actorId));

        respuestaConfirmada.Estado.Should().Be("CONFIRMADA");
        respuestaConfirmada.BalanceResultante.PresupuestoEjecutado.Should().Be(4000.00m);
        respuestaConfirmada.BalanceResultante.CuposOcupados.Should().Be(1);
        await _eventPublisher.Received(1)
            .PublicarAsync(Arg.Any<PresupuestoProgramaConfirmado>(), Arg.Any<CancellationToken>());

        var liberarHandler = new LiberarReservaPresupuestoCommandHandler(
            _programaRepository,
            _unitOfWork,
            _eventPublisher,
            new LiberarReservaPresupuestoCommandValidator());

        var respuestaLiberada = await liberarHandler.HandleAsync(new LiberarReservaPresupuestoCommand(
            programa.Id,
            refLiberar,
            "Postulación retirada",
            _actorId));

        respuestaLiberada.Estado.Should().Be("LIBERADA");
        respuestaLiberada.BalanceResultante.PresupuestoReservado.Should().Be(0.00m);
        respuestaLiberada.BalanceResultante.PresupuestoDisponible.Should().Be(16000.00m);
        await _eventPublisher.Received(1)
            .PublicarAsync(Arg.Any<ReservaPresupuestoLiberada>(), Arg.Any<CancellationToken>());

        var balanceHandler = new ObtenerBalanceProgramaQueryHandler(_programaRepository);
        var balance = await balanceHandler.HandleAsync(new ObtenerBalanceProgramaQuery(programa.Id));
        balance.PresupuestoTotalAsignado.Should().Be(20000.00m);
        balance.PresupuestoEjecutado.Should().Be(4000.00m);
        balance.PresupuestoDisponible.Should().Be(16000.00m);

        var cerrarHandler = new CerrarPeriodoProgramaCommandHandler(
            _programaRepository,
            _unitOfWork,
            _eventPublisher,
            new CerrarPeriodoProgramaCommandValidator());

        var cierre = await cerrarHandler.HandleAsync(new CerrarPeriodoProgramaCommand(
            programa.Id,
            "2026-1",
            "Cierre contable del período",
            true,
            _actorId));

        cierre.EstadoProgramaResultante.Should().Be("CERRADO");
        cierre.SaldoNoEjecutadoConsolidado.Should().Be(16000.00m);
        await _eventPublisher.Received(1)
            .PublicarAsync(Arg.Any<PeriodoProgramaCerrado>(), Arg.Any<CancellationToken>());

        _programaRepository.ConsultarMovimientosPaginadosAsync(
                programa.Id,
                1,
                20,
                TipoMovimientoPresupuestario.CierrePeriodo,
                "2026-1",
                null,
                Arg.Any<CancellationToken>())
            .Returns(([programa.MovimientosPresupuestarios.Last()], 1));

        var movimientosHandler = new ConsultarMovimientosPresupuestariosQueryHandler(_programaRepository);
        var movimientos = await movimientosHandler.HandleAsync(new ConsultarMovimientosPresupuestariosQuery(
            programa.Id,
            1,
            20,
            "CIERRE_PERIODO",
            "2026-1",
            null));

        movimientos.TotalRegistros.Should().Be(1);
        movimientos.Items.Single().TipoMovimiento.Should().Be("CIERRE_PERIODO");
    }
}
