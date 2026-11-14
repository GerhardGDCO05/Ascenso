using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Donantes.Eventos;
using Ascenso.Programas.Domain.Donantes.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using FluentAssertions;

namespace Ascenso.Programas.Domain.Tests.Donantes;

public sealed class DonanteTests
{
    private static readonly Guid _usuarioPruebaId = Guid.Parse("01966000-0000-7000-8000-000000000001");

    private static Donante CrearDonantePrueba(TipoDonante tipo = TipoDonante.Empresa)
        => Donante.Crear(
            tipo,
            "Fundación Empresarial Horizonte",
            " j-30123456-7 ",
            "donaciones@horizonte.org",
            "+58 212 4001020",
            "Av. Principal de Los Cortijos, Edif. Horizonte, Caracas",
            new ContactoDonante(
                "Carlos Mendoza",
                "Director Ejecutivo",
                "cmendoza@horizonte.org",
                "+58 414 1234567"),
            _usuarioPruebaId,
            new DateTimeOffset(2026, 11, 13, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Debe_CrearDonanteActivoConSaldosEnCeroYEventoDominio_Cuando_DatosSonValidos_HU13_1()
    {
        var donante = CrearDonantePrueba();

        UuidV7.EsVersion7(donante.Id).Should().BeTrue();
        donante.IdentificacionFiscal.Should().Be("J-30123456-7");
        donante.EstadoDonante.Should().Be(EstadoDonante.Activo);
        donante.Moneda.Should().Be("USD");
        donante.SaldoTotalHistorico.Should().Be(0.00m);
        donante.SaldoDisponibleAsignacion.Should().Be(0.00m);
        donante.Version.Should().Be(1);
        donante.EventosDominio.Should().ContainSingle(e => e is DonanteRegistradoDominioEvent);
    }

    [Fact]
    public void Debe_ActualizarContactoYReactivarIncrementandoVersion_Cuando_DatosSonValidos_HU13_1()
    {
        var donante = CrearDonantePrueba(TipoDonante.Organizacion);
        var nuevoContacto = new ContactoDonante(
            "Laura Gómez",
            "Coordinadora de Alianzas",
            "lgomez@horizonte.org",
            "+58 412 7654321");

        donante.Desactivar(tieneFondosComprometidosEnProgramaVigente: false, _usuarioPruebaId);
        donante.EstadoDonante.Should().Be(EstadoDonante.Inactivo);

        donante.ActualizarContacto(
            "alianzas@horizonte.org",
            "+58 212 4009999",
            "Torre Empresarial, Piso 4, Caracas",
            nuevoContacto,
            _usuarioPruebaId);
        donante.Reactivar(_usuarioPruebaId);

        donante.EstadoDonante.Should().Be(EstadoDonante.Activo);
        donante.Email.Should().Be("alianzas@horizonte.org");
        donante.ContactoPrincipal.Should().Be(nuevoContacto);
        donante.Version.Should().Be(4);
    }

    [Fact]
    public void Debe_BloquearDesactivacionConDonanteConFondos_Cuando_TieneFondosComprometidos_RNPRG04_HU13_1()
    {
        var donante = CrearDonantePrueba();

        var accion = () => donante.Desactivar(tieneFondosComprometidosEnProgramaVigente: true, _usuarioPruebaId);

        accion.Should()
            .Throw<DonanteConFondosComprometidosException>()
            .Where(ex => ex.Codigo == "DONANTE_CON_FONDOS" && ex.EstadoHttp == 409);
        donante.EstadoDonante.Should().Be(EstadoDonante.Activo);
    }

    [Fact]
    public void Debe_RegistrarAporteConfirmadoEIncrementarSaldosAtomicamente_Cuando_DatosSonValidos_HU13_2()
    {
        var donante = CrearDonantePrueba();
        var rutaFirebase = $"/donantes/{donante.Id:D}/aportes/{UuidV7.Nuevo():D}.pdf";

        var aporte = donante.RegistrarAporte(
            15000.50m,
            "USD",
            "2026-1",
            new DateTimeOffset(2026, 11, 13, 10, 30, 0, TimeSpan.Zero),
            "Banesco Banco Universal",
            " ref-00012345 ",
            rutaFirebase,
            _usuarioPruebaId,
            "Transferencia institucional primer semestre");

        UuidV7.EsVersion7(aporte.Id).Should().BeTrue();
        aporte.EstadoAporte.Should().Be(EstadoAporte.Confirmado);
        aporte.NumeroReferenciaBancaria.Should().Be("REF-00012345");
        aporte.UrlComprobanteFirebase.Should().Be(rutaFirebase);
        donante.Aportes.Should().ContainSingle().Which.Id.Should().Be(aporte.Id);
        donante.SaldoTotalHistorico.Should().Be(15000.50m);
        donante.SaldoDisponibleAsignacion.Should().Be(15000.50m);
        donante.EventosDominio.Should().Contain(e => e is AporteDonanteRegistradoDominioEvent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/otros/comprobante.pdf")]
    public void Debe_RechazarAportePorComprobanteFaltanteOInvalido_Cuando_UrlNoCorresponde_RNPRG04_HU13_2(string urlInvalida)
    {
        var donante = CrearDonantePrueba();

        var accion = () => donante.RegistrarAporte(
            5000.00m,
            "USD",
            "2026-1",
            DateTimeOffset.UtcNow,
            "Banco Mercantil",
            "REF-999111",
            urlInvalida,
            _usuarioPruebaId);

        accion.Should()
            .Throw<ComprobanteAporteRequeridoException>()
            .Where(ex => ex.Codigo == "COMPROBANTE_REQUERIDO" && ex.EstadoHttp == 400);
        donante.Aportes.Should().BeEmpty();
        donante.SaldoTotalHistorico.Should().Be(0.00m);
        donante.SaldoDisponibleAsignacion.Should().Be(0.00m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-250.00)]
    [InlineData(100.123)]
    public void Debe_RechazarAportePorMontoInvalido_Cuando_MontoNoEsPositivoODecimalesExcedenDos_HU13_2(decimal montoInvalido)
    {
        var donante = CrearDonantePrueba();
        var rutaFirebase = $"/donantes/{donante.Id:D}/aportes/{UuidV7.Nuevo():D}.pdf";

        var accion = () => donante.RegistrarAporte(
            montoInvalido,
            "USD",
            "2026-1",
            DateTimeOffset.UtcNow,
            "Banco Provincial",
            "REF-888222",
            rutaFirebase,
            _usuarioPruebaId);

        accion.Should()
            .Throw<MontoAporteInvalidoException>()
            .Where(ex => ex.Codigo == "MONTO_APORTE_INVALIDO" && ex.EstadoHttp == 400);
        donante.SaldoTotalHistorico.Should().Be(0.00m);
    }

    [Fact]
    public void Debe_RechazarAportePorReferenciaDuplicadaEnAgregadoSinAlterarSaldos_RNPRG04_HU13_2()
    {
        var donante = CrearDonantePrueba();
        var ruta1 = $"/donantes/{donante.Id:D}/aportes/{UuidV7.Nuevo():D}.pdf";
        var ruta2 = $"/donantes/{donante.Id:D}/aportes/{UuidV7.Nuevo():D}.pdf";

        donante.RegistrarAporte(
            1000.00m,
            "USD",
            "2026-1",
            DateTimeOffset.UtcNow,
            "Banesco",
            "REF-DUP-001",
            ruta1,
            _usuarioPruebaId);

        var accion = () => donante.RegistrarAporte(
            2000.00m,
            "USD",
            "2026-1",
            DateTimeOffset.UtcNow,
            "Banesco",
            "ref-dup-001",
            ruta2,
            _usuarioPruebaId);

        accion.Should()
            .Throw<ReferenciaBancariaDuplicadaException>()
            .Where(ex => ex.Codigo == "REFERENCIA_DUPLICADA" && ex.EstadoHttp == 409);
        donante.SaldoTotalHistorico.Should().Be(1000.00m);
        donante.SaldoDisponibleAsignacion.Should().Be(1000.00m);
    }

    [Fact]
    public void Debe_RechazarAporte_Cuando_DonanteEstaInactivo_RNPRG02_HU13_2()
    {
        var donante = CrearDonantePrueba(TipoDonante.EgresadoPersona);
        donante.Desactivar(tieneFondosComprometidosEnProgramaVigente: false, _usuarioPruebaId);
        var rutaFirebase = $"/donantes/{donante.Id:D}/aportes/{UuidV7.Nuevo():D}.pdf";

        var accion = () => donante.RegistrarAporte(
            500.00m,
            "USD",
            "2026-1",
            DateTimeOffset.UtcNow,
            "Banesco",
            "REF-INACTIVO-1",
            rutaFirebase,
            _usuarioPruebaId);

        accion.Should()
            .Throw<DonanteInactivoException>()
            .Where(ex => ex.Codigo == "DONANTE_INACTIVO" && ex.EstadoHttp == 409);
    }

    [Fact]
    public void Debe_ConvertirEnumeracionesDeDonantesACodigosEnMayusculasYRevertirCorrectamente()
    {
        TipoDonante.EgresadoPersona.ACodigo().Should().Be("EGRESADO_PERSONA");
        EstadoDonante.Activo.ACodigo().Should().Be("ACTIVO");
        EstadoAporte.PendienteVerificacion.ACodigo().Should().Be("PENDIENTE_VERIFICACION");

        CodigoEnum.DesdeCodigo<TipoDonante>("EGRESADO_PERSONA").Should().Be(TipoDonante.EgresadoPersona);
        CodigoEnum.DesdeCodigo<EstadoDonante>("INACTIVO").Should().Be(EstadoDonante.Inactivo);
        CodigoEnum.DesdeCodigo<EstadoAporte>("CONFIRMADO").Should().Be(EstadoAporte.Confirmado);
    }

    [Fact]
    public void Debe_RegistrarMultiplesAportesConsecutivos_AcumulandoSaldosYVersion_HU13_2()
    {
        var donante = CrearDonantePrueba(TipoDonante.Empresa);
        donante.Version.Should().Be(1);

        var aporte1 = donante.RegistrarAporte(
            1200.50m,
            "USD",
            "2026-1",
            DateTimeOffset.UtcNow,
            "Banesco",
            "REF-CONSEC-001",
            $"/donantes/{donante.Id:D}/aportes/{UuidV7.Nuevo():D}.pdf",
            _usuarioPruebaId);

        var aporte2 = donante.RegistrarAporte(
            799.50m,
            "USD",
            "2026-1",
            DateTimeOffset.UtcNow,
            "Mercantil",
            "REF-CONSEC-002",
            $"/donantes/{donante.Id:D}/aportes/{UuidV7.Nuevo():D}.pdf",
            _usuarioPruebaId);

        aporte1.Id.Should().NotBe(Guid.Empty);
        aporte2.Id.Should().NotBe(Guid.Empty);
        aporte1.Id.Should().NotBe(aporte2.Id);

        donante.Aportes.Should().HaveCount(2);
        donante.SaldoTotalHistorico.Should().Be(2000.00m);
        donante.SaldoDisponibleAsignacion.Should().Be(2000.00m);
        donante.Version.Should().Be(3);
    }
}

