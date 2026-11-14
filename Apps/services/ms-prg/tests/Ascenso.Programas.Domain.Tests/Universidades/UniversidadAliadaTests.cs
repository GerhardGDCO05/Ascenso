using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Universidades.Agregados;
using Ascenso.Programas.Domain.Universidades.Eventos;
using Ascenso.Programas.Domain.Universidades.ObjetosValor;
using FluentAssertions;

namespace Ascenso.Programas.Domain.Tests.Universidades;

public sealed class UniversidadAliadaTests
{
    private static readonly Guid _usuarioPruebaId = Guid.Parse("01966000-0000-7000-8000-000000000101");

    private static UniversidadAliada CrearUniversidadPrueba(
        string codigo = " ucab-ccs ",
        DateOnly? fechaInicio = null,
        DateOnly? fechaFin = null,
        string urlPdf = "https://storage.googleapis.com/ascenso/convenios/ucab-2026.pdf")
        => UniversidadAliada.CrearConConvenioInicial(
            codigo,
            "Universidad Católica Andrés Bello",
            "Montalbán, Caracas",
            new ContactoEnlace("Laura Ríos", "lrios@ucab.edu.ve", "+58 212 4074111"),
            new CuentaBancariaInstitucional("Banesco", "Corriente", "ENC-0134-XXXX", "J-00012345-6"),
            null,
            "CONV-2026-001",
            "Convenio marco de cooperación académica y financiamiento de matrículas.",
            fechaInicio ?? new DateOnly(2026, 1, 1),
            fechaFin ?? new DateOnly(2028, 12, 31),
            urlPdf,
            permiteIntercambioAutomaticoDatos: true,
            _usuarioPruebaId);

    [Fact]
    public void Debe_CrearUniversidadConConvenioVigenteYEvento_Cuando_DatosSonValidos_HU14_1()
    {
        var universidad = CrearUniversidadPrueba();

        universidad.Id.Should().NotBe(Guid.Empty);
        universidad.CodigoInstitucional.Should().Be("UCAB-CCS");
        universidad.Estado.Should().Be(EstadoUniversidad.Activa);
        universidad.Convenios.Should().HaveCount(1);
        universidad.TieneConvenioVigente(new DateOnly(2026, 6, 1)).Should().BeTrue();

        var convenio = universidad.Convenios.Single();
        convenio.Estado.Should().Be(EstadoConvenio.Vigente);
        universidad.EventosDominio.Should().ContainSingle(ev => ev is ConvenioUniversidadRegistradoDominioEvent);
    }

    [Fact]
    public void Debe_RechazarConvenio_Cuando_FechaFinNoEsPosteriorAFechaInicio_HU14_1()
    {
        var accion = () => CrearUniversidadPrueba(
            fechaInicio: new DateOnly(2026, 5, 1),
            fechaFin: new DateOnly(2026, 5, 1));

        accion.Should()
            .Throw<FechasConvenioInvalidasException>()
            .Where(ex => ex.Codigo == "FECHAS_CONVENIO_INVALIDAS" && ex.EstadoHttp == 400);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://storage.googleapis.com/ascenso/convenios/ucab-2026.docx")]
    public void Debe_RechazarConvenio_Cuando_DocumentoNoEsPdf_HU14_1(string urlInvalida)
    {
        var accion = () => CrearUniversidadPrueba(urlPdf: urlInvalida);

        accion.Should()
            .Throw<DocumentoConvenioPdfRequeridoException>()
            .Where(ex => ex.Codigo == "DOCUMENTO_CONVENIO_PDF_REQUERIDO" && ex.EstadoHttp == 400);
    }

    [Fact]
    public void Debe_BloquearNuevasPostulacionesCon422_Cuando_ConvenioVence_RNPRG05_HU14_1()
    {
        var universidad = CrearUniversidadPrueba();
        var convenio = universidad.Convenios.Single();

        universidad.MarcarConvenioVencido(convenio.Id, _usuarioPruebaId);

        convenio.Estado.Should().Be(EstadoConvenio.Vencido);
        universidad.TieneConvenioVigente().Should().BeFalse();
        universidad.EventosDominio.Should().Contain(ev => ev is ConvenioUniversidadVencidoDominioEvent);

        var accion = () => universidad.ValidarConvenioVigenteParaNuevasPostulaciones();

        accion.Should()
            .Throw<ConvenioUniversidadVencidoException>()
            .Where(ex => ex.Codigo == "CONVENIO_VENCIDO" && ex.EstadoHttp == 422);
    }

    [Fact]
    public void Debe_RechazarCarreraDuplicadaEnMismaUniversidad_HU14_2()
    {
        var universidad = CrearUniversidadPrueba();
        universidad.AgregarCarreraElegible("ing-inf", "Ingeniería Informática", 10, _usuarioPruebaId);

        var accion = () => universidad.AgregarCarreraElegible("ING-INF", "Ingeniería Informática", 10, _usuarioPruebaId);

        accion.Should()
            .Throw<CodigoCarreraDuplicadoException>()
            .Where(ex => ex.Codigo == "CODIGO_CARRERA_DUPLICADO" && ex.EstadoHttp == 409);
    }

    [Fact]
    public void Debe_VersionarCostosMatriculaConsecutivos_PasandoAnteriorAHistorico_RNPRG05_HU14_2()
    {
        var universidad = CrearUniversidadPrueba();
        var carrera = universidad.AgregarCarreraElegible("ING-INF", "Ingeniería Informática", 10, _usuarioPruebaId);

        var fechaV1 = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var fechaV2 = new DateTimeOffset(2026, 2, 15, 15, 30, 0, TimeSpan.Zero);

        var costoV1 = universidad.ActualizarCostoMatricula(
            carrera.Id,
            "2026-1",
            1800.00m,
            "USD",
            _usuarioPruebaId,
            fechaV1);

        var costoV2 = universidad.ActualizarCostoMatricula(
            carrera.Id,
            "2026-1",
            2150.50m,
            "USD",
            _usuarioPruebaId,
            fechaV2);

        costoV1.NumeroVersion.Should().Be(1);
        costoV1.Estado.Should().Be(EstadoVersionCosto.Historico);
        costoV1.FechaVigenciaHasta.Should().Be(fechaV2);
        costoV1.MontoReferenciaMatricula.Should().Be(1800.00m);

        costoV2.NumeroVersion.Should().Be(2);
        costoV2.Estado.Should().Be(EstadoVersionCosto.Vigente);
        costoV2.FechaVigenciaHasta.Should().BeNull();
        costoV2.MontoReferenciaMatricula.Should().Be(2150.50m);

        carrera.CostosMatricula.Should().HaveCount(2);
        universidad.EventosDominio.OfType<CostoMatriculaActualizadoDominioEvent>().Should().HaveCount(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(100.123)]
    public void Debe_RechazarCostoMatricula_Cuando_MontoEsInvalido_HU14_2(decimal montoInvalido)
    {
        var universidad = CrearUniversidadPrueba();
        var carrera = universidad.AgregarCarreraElegible("DER", "Derecho", 10, _usuarioPruebaId);

        var accion = () => universidad.ActualizarCostoMatricula(
            carrera.Id,
            "2026-1",
            montoInvalido,
            "USD",
            _usuarioPruebaId);

        accion.Should()
            .Throw<MontoMatriculaInvalidoException>()
            .Where(ex => ex.Codigo == "MONTO_MATRICULA_INVALIDO" && ex.EstadoHttp == 400);
    }
}
