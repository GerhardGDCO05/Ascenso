using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Donantes.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Programas.Agregados;
using Ascenso.Programas.Domain.Programas.Eventos;
using Ascenso.Programas.Domain.Programas.ObjetosValor;
using FluentAssertions;

namespace Ascenso.Programas.Domain.Tests.Programas;

public sealed class ProgramaBecaTests
{
    private static readonly Guid _usuarioPruebaId = Guid.Parse("01966000-0000-7000-8000-000000000201");

    private static ProgramaBeca CrearProgramaPrueba(
        string codigo = " beca-ing-2026 ",
        int cupoTotal = 25,
        DateOnly? vigenciaDesde = null,
        DateOnly? vigenciaHasta = null,
        PoliticaCobertura? politica = null)
        => ProgramaBeca.Crear(
            codigo,
            "Beca Excelencia en Ingeniería",
            "Programa integral de becas para carreras STEM.",
            politica ?? new PoliticaCobertura(ModalidadCobertura.Total, 100.00m, 0.00m, "USD"),
            cupoTotal,
            10,
            "2026-1",
            vigenciaDesde ?? new DateOnly(2026, 1, 15),
            vigenciaHasta ?? new DateOnly(2026, 12, 15),
            permiteAcumulacion: false,
            requiereCompromisoFirmado: true,
            "Mantener promedio mínimo de 15.00 puntos.",
            _usuarioPruebaId);

    private static Donante CrearDonanteConSaldo(decimal montoAporte = 50000.00m)
    {
        var donante = Donante.Crear(
            TipoDonante.Empresa,
            "Fundación Empresas Aliadas",
            "J-30112233-4",
            "rse@empresasaliadas.org",
            "+58 212 5550000",
            "Caracas, Venezuela",
            new ContactoDonante("Carlos Méndez", "Director RSE", "cmendez@empresasaliadas.org", "+58 414 5550000"),
            _usuarioPruebaId);

        if (montoAporte > 0m)
        {
            donante.RegistrarAporte(
                montoAporte,
                "USD",
                "2026-1",
                DateTimeOffset.UtcNow,
                "Banesco",
                $"REF-{Guid.NewGuid():N}"[..16],
                $"/donantes/{donante.Id:D}/aportes/{Guid.CreateVersion7():D}.pdf",
                _usuarioPruebaId);
        }

        return donante;
    }

    private static CriteriosElegibilidadPrograma CrearCriteriosPrueba(
        decimal promedioMinimo = 15.00m)
        => new(
            promedioMinimo,
            17,
            25,
            1200.00m,
            [Guid.Parse("01966000-0000-7000-8000-000000000301")],
            [Guid.Parse("01966000-0000-7000-8000-000000000401")]);

    private static CondicionesPermanencia CrearCondicionesPrueba(
        decimal promedioRenovacion = 14.50m)
        => new(
            promedioRenovacion,
            4,
            1,
            20,
            FrecuenciaReporteAcademico.Semestral,
            ConsecuenciaIncumplimiento.Suspension,
            0.50m,
            2,
            permiteRenovacionTardiaExcepcional: true);

    [Fact]
    public void Debe_CrearProgramaEnBorradorConSaldoYCupoOcupadoEnCero_Cuando_DatosSonValidos_HU11_1()
    {
        var programa = CrearProgramaPrueba();

        programa.Id.Should().NotBe(Guid.Empty);
        programa.Codigo.Should().Be("BECA-ING-2026");
        programa.Estado.Should().Be(EstadoProgramaBeca.Borrador);
        programa.PresupuestoTotalAsignado.Should().Be(0.00m);
        programa.CuposOcupados.Should().Be(0);
        programa.PoliticaCobertura.Modalidad.Should().Be(ModalidadCobertura.Total);
        programa.PoliticaCobertura.PorcentajeCobertura.Should().Be(100.00m);
        programa.EventosDominio.Should().ContainSingle(e => e is ProgramaBecaCreadoDominioEvent);
    }

    [Fact]
    public void Debe_RechazarCreacionDePrograma_Cuando_VigenciaHastaEsMenorOIgualAVigenciaDesde_HU11_1()
    {
        var accion = () => CrearProgramaPrueba(
            vigenciaDesde: new DateOnly(2026, 6, 1),
            vigenciaHasta: new DateOnly(2026, 6, 1));

        var ex = accion.Should().Throw<FechasVigenciaProgramaInvalidasException>().Which;
        ex.EstadoHttp.Should().Be(400);
        ex.Codigo.Should().Be(FechasVigenciaProgramaInvalidasException.CodigoError);
    }

    [Fact]
    public void Debe_RechazarPoliticaCobertura_Cuando_ParametrosSonInconsistentesConModalidad_HU11_1()
    {
        var accion = () => new PoliticaCobertura(ModalidadCobertura.Parcial, 100.00m, 0.00m, "USD");

        var ex = accion.Should().Throw<PoliticaCoberturaInvalidaException>().Which;
        ex.EstadoHttp.Should().Be(400);
        ex.Codigo.Should().Be(PoliticaCoberturaInvalidaException.CodigoError);
    }

    [Fact]
    public void Debe_AsignarPresupuestoYDeducirSaldoDonante_Cuando_DonanteActivoTieneSaldoSuficiente_HU11_2()
    {
        var programa = CrearProgramaPrueba();
        var donante = CrearDonanteConSaldo(50000.00m);

        donante.ComprometerSaldoParaPrograma(15000.00m, _usuarioPruebaId);
        var asignacion = programa.AsignarFondoDonante(donante.Id, "2026-1", 15000.00m, _usuarioPruebaId);

        donante.SaldoDisponibleAsignacion.Should().Be(35000.00m);
        programa.PresupuestoTotalAsignado.Should().Be(15000.00m);
        programa.AsignacionesFondo.Should().ContainSingle();
        asignacion.Estado.Should().Be(EstadoAsignacionFondo.Asignada);
        asignacion.MontoAsignado.Should().Be(15000.00m);
        programa.EventosDominio.Should().Contain(e => e is PresupuestoProgramaAsignadoDominioEvent);
    }

    [Fact]
    public void Debe_RechazarCompromisoSaldo_Cuando_DonanteTieneSaldoInsuficiente_HU11_2()
    {
        var donante = CrearDonanteConSaldo(1000.00m);

        var accion = () => donante.ComprometerSaldoParaPrograma(1500.00m, _usuarioPruebaId);

        var ex = accion.Should().Throw<SaldoDonanteInsuficienteException>().Which;
        ex.EstadoHttp.Should().Be(409);
        ex.Codigo.Should().Be(SaldoDonanteInsuficienteException.CodigoError);
    }

    [Fact]
    public void Debe_ReajustarPresupuestoYRestituirSaldo_EnBorradorOBloquearEnActivo_HU11_2()
    {
        var programa = CrearProgramaPrueba();
        var donante = CrearDonanteConSaldo(30000.00m);

        donante.ComprometerSaldoParaPrograma(20000.00m, _usuarioPruebaId);
        var asignacion = programa.AsignarFondoDonante(donante.Id, "2026-1", 20000.00m, _usuarioPruebaId);

        var (donanteId, montoRestituido) = programa.ReajustarAsignacionFondo(asignacion.Id, 5000.00m, _usuarioPruebaId);
        donante.ReintegrarSaldoDePrograma(montoRestituido, _usuarioPruebaId);

        donanteId.Should().Be(donante.Id);
        montoRestituido.Should().Be(5000.00m);
        programa.PresupuestoTotalAsignado.Should().Be(15000.00m);
        donante.SaldoDisponibleAsignacion.Should().Be(15000.00m);

        programa.ConfigurarElegibilidadYPermanencia(
            CrearCriteriosPrueba(),
            CrearCondicionesPrueba(),
            todasLasUniversidadesTienenConvenioVigente: true,
            todasLasCarrerasSonElegibles: true,
            _usuarioPruebaId);

        programa.Activar(_usuarioPruebaId);

        var accionEnActivo = () => programa.ReajustarAsignacionFondo(asignacion.Id, 5000.00m, _usuarioPruebaId);
        var ex = accionEnActivo.Should().Throw<PresupuestoEnEjecucionException>().Which;
        ex.EstadoHttp.Should().Be(409);
        ex.Codigo.Should().Be(PresupuestoEnEjecucionException.CodigoError);
    }

    [Fact]
    public void Debe_TransicionarAConfiguradoYRegistrarEvento_Cuando_CumpleRN_PRG_01_HU11_3()
    {
        var programa = CrearProgramaPrueba();
        programa.AsignarFondoDonante(Guid.NewGuid(), "2026-1", 25000.00m, _usuarioPruebaId);

        var criterios = CrearCriteriosPrueba(16.00m);
        var condiciones = CrearCondicionesPrueba(15.00m);

        programa.ConfigurarElegibilidadYPermanencia(
            criterios,
            condiciones,
            todasLasUniversidadesTienenConvenioVigente: true,
            todasLasCarrerasSonElegibles: true,
            _usuarioPruebaId);

        programa.Estado.Should().Be(EstadoProgramaBeca.Configurado);
        programa.CriteriosElegibilidad.Should().BeEquivalentTo(criterios);
        programa.CondicionesPermanencia.Should().BeEquivalentTo(condiciones);
        programa.EventosDominio.Should().Contain(e => e is ProgramaBecaConfiguradoDominioEvent);
    }

    [Fact]
    public void Debe_RechazarTransicionAConfigurado_Cuando_FaltaPresupuestoOConvenioVigente_HU11_3()
    {
        var programaSinPresupuesto = CrearProgramaPrueba();

        var accionSinPresupuesto = () => programaSinPresupuesto.ConfigurarElegibilidadYPermanencia(
            CrearCriteriosPrueba(),
            CrearCondicionesPrueba(),
            todasLasUniversidadesTienenConvenioVigente: true,
            todasLasCarrerasSonElegibles: true,
            _usuarioPruebaId);

        var exPresupuesto = accionSinPresupuesto.Should().Throw<ProgramaNoConfigurableException>().Which;
        exPresupuesto.EstadoHttp.Should().Be(422);
        exPresupuesto.Codigo.Should().Be(ProgramaNoConfigurableException.CodigoError);

        programaSinPresupuesto.AsignarFondoDonante(Guid.NewGuid(), "2026-1", 10000.00m, _usuarioPruebaId);

        var accionSinConvenio = () => programaSinPresupuesto.ConfigurarElegibilidadYPermanencia(
            CrearCriteriosPrueba(),
            CrearCondicionesPrueba(),
            todasLasUniversidadesTienenConvenioVigente: false,
            todasLasCarrerasSonElegibles: true,
            _usuarioPruebaId);

        var exConvenio = accionSinConvenio.Should().Throw<ProgramaNoConfigurableException>().Which;
        exConvenio.EstadoHttp.Should().Be(422);
        exConvenio.Codigo.Should().Be(ProgramaNoConfigurableException.CodigoError);
    }

    [Fact]
    public void Debe_RechazarPromedioFueraDeRango_Cuando_SeInstancianCriteriosOCondiciones_HU11_3()
    {
        var accionCriterios = () => CrearCriteriosPrueba(21.00m);
        var ex1 = accionCriterios.Should().Throw<PromedioAcademicoInvalidoException>().Which;
        ex1.EstadoHttp.Should().Be(400);
        ex1.Codigo.Should().Be(PromedioAcademicoInvalidoException.CodigoError);

        var accionCondiciones = () => CrearCondicionesPrueba(-0.50m);
        var ex2 = accionCondiciones.Should().Throw<PromedioAcademicoInvalidoException>().Which;
        ex2.EstadoHttp.Should().Be(400);
        ex2.Codigo.Should().Be(PromedioAcademicoInvalidoException.CodigoError);
    }
}
