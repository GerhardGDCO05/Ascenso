using System.Security.Cryptography;
using System.Text;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Comandos.CargarCompromisoBecario;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;
using Ascenso.Becas.Domain.Ports;
using FluentAssertions;

namespace Ascenso.Becas.Application.Tests.Otorgamiento;

public sealed class CargarCompromisoBecarioHandlerTests
{
    private static readonly byte[] _pdfValido =
        Encoding.UTF8.GetBytes("%PDF-1.7 contenido del compromiso firmado");

    [Fact]
    public async Task Debe_CargarElCompromiso_Cuando_ElPdfEsValido()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaConCompromiso(escenario);

        var resultado = await escenario.CrearCargarCompromiso().HandleAsync(
            CrearComando(beca, _pdfValido));

        resultado.CompromisoId.Should().Be(beca.Compromiso!.Id);
        resultado.NumeroIntentoCarga.Should().Be(1);
        resultado.Estado.Should().Be("EN_REVISION");
        beca.Compromiso.Estado.Should().Be(EstadoCompromiso.EnRevision);
        beca.Compromiso.NumeroIntentoCarga.Should().Be(1);
        beca.Compromiso.IpCargaFirma.Should().Be("192.168.1.10");
        beca.HabilitadaParaDesembolso.Should().BeFalse();

        var hashEsperado = Convert.ToHexString(SHA256.HashData(_pdfValido)).ToLowerInvariant();
        beca.Compromiso.HashDocumentoSha256.Should().Be(hashEsperado);
        beca.Compromiso.TipoMimeDocumento.Should().Be("application/pdf");
        beca.Compromiso.TamanioDocumentoBytes.Should().Be(_pdfValido.Length);
        escenario.Almacenamiento.RutasSubidas.Should().ContainSingle()
            .Which.Should().Be(
                $"becas/{beca.Id}/compromisos/{beca.Compromiso.Id}/intento-1.pdf");
        escenario.Almacenamiento.UrlsEliminadas.Should().BeEmpty();
        escenario.Unidad.Confirmaciones.Should().Be(1);
        escenario.Outbox.Confirmados.Should()
            .ContainSingle(evento => evento is CompromisoBecarioCargadoEvento);
    }

    [Fact]
    public async Task Debe_RegistrarUnNuevoIntento_Cuando_ElAnteriorFueRechazado()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaConCompromiso(escenario);
        var contexto = DatosAplicacion.Contexto(DatosAplicacion.FechaBase.AddHours(1));
        beca.CargarCompromiso(
            "https://storage.example/compromiso-1.pdf",
            new string('b', 64),
            _pdfValido.Length,
            "application/pdf",
            "10.0.0.1",
            contexto);
        beca.RechazarCompromiso("Documento ilegible.", contexto);

        var nuevoContenido = Encoding.UTF8.GetBytes("%PDF-1.7 segundo intento firmado");
        var resultado = await escenario.CrearCargarCompromiso().HandleAsync(
            CrearComando(beca, nuevoContenido));

        resultado.NumeroIntentoCarga.Should().Be(2);
        beca.Compromiso!.Estado.Should().Be(EstadoCompromiso.EnRevision);
        beca.Compromiso.NumeroIntentoCarga.Should().Be(2);
        escenario.Almacenamiento.RutasSubidas.Should().ContainSingle()
            .Which.Should().EndWith("/intento-2.pdf");
    }

    [Fact]
    public async Task Debe_RechazarElArchivo_Cuando_NoTieneFirmaPdf()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaConCompromiso(escenario);
        var contenido = Encoding.UTF8.GetBytes("esto no es un pdf");

        var accion = () => escenario.CrearCargarCompromiso().HandleAsync(
            CrearComando(beca, contenido));

        var excepcion = await accion.Should().ThrowAsync<ArchivoCompromisoInvalidoException>();

        excepcion.Which.Codigo.Should().Be("ARCHIVO_COMPROMISO_INVALIDO");
        escenario.Almacenamiento.RutasSubidas.Should().BeEmpty();
        beca.Compromiso!.NumeroIntentoCarga.Should().Be(0);
    }

    [Theory]
    [InlineData("text/plain", "compromiso.pdf")]
    [InlineData("application/pdf", "compromiso.txt")]
    [InlineData("image/png", "compromiso.png")]
    public async Task Debe_RechazarElArchivo_Cuando_ElFormatoNoEsPdf(
        string tipoMime,
        string nombreArchivo)
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaConCompromiso(escenario);

        var accion = () => escenario.CrearCargarCompromiso().HandleAsync(
            new CargarCompromisoBecarioCommand(beca.Id, nombreArchivo, tipoMime, _pdfValido));

        await accion.Should().ThrowAsync<ArchivoCompromisoInvalidoException>();
        escenario.Almacenamiento.RutasSubidas.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_RechazarElArchivo_Cuando_SuperaElTamanioMaximo()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaConCompromiso(escenario);
        var contenido = new byte[CargarCompromisoBecarioValidator.TamanioMaximoBytes + 1];

        var accion = () => escenario.CrearCargarCompromiso().HandleAsync(
            CrearComando(beca, contenido));

        var excepcion = await accion.Should().ThrowAsync<ArchivoCompromisoInvalidoException>();

        excepcion.Which.Message.Should().Contain("5 MB");
        escenario.Almacenamiento.RutasSubidas.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_RechazarElArchivo_Cuando_EstaVacio()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaConCompromiso(escenario);

        var accion = () => escenario.CrearCargarCompromiso().HandleAsync(
            CrearComando(beca, []));

        await accion.Should().ThrowAsync<ArchivoCompromisoInvalidoException>();
    }

    [Fact]
    public async Task Debe_RechazarLaCarga_Cuando_LaBecaNoTieneCompromiso()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = DatosAplicacion.CrearBecaActiva(
            escenario.PostulacionId,
            escenario.ProgramaBecaId,
            escenario.PeriodoAcademicoId,
            escenario.EstudianteId,
            requiereCompromisoFirmado: false);
        escenario.Repositorio.Becas.Add(beca);

        var accion = () => escenario.CrearCargarCompromiso().HandleAsync(
            CrearComando(beca, _pdfValido));

        var excepcion = await accion.Should().ThrowAsync<ConflictoOperacionException>();

        excepcion.Which.Codigo.Should().Be("CONFLICTO_OPERACION");
        escenario.Almacenamiento.RutasSubidas.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_RechazarLaCarga_Cuando_LaBecaNoExiste()
    {
        var escenario = new EscenarioOtorgamiento();

        var accion = () => escenario.CrearCargarCompromiso().HandleAsync(
            new CargarCompromisoBecarioCommand(
                Guid.CreateVersion7(),
                "compromiso.pdf",
                "application/pdf",
                _pdfValido));

        var excepcion = await accion.Should().ThrowAsync<RecursoNoEncontradoException>();

        excepcion.Which.Recurso.Should().Be("beca");
    }

    [Fact]
    public async Task Debe_RechazarLaCarga_Cuando_LaDireccionIpNoEsValida()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaConCompromiso(escenario);
        escenario.Contexto.DireccionIp = "no-es-una-ip";

        var accion = () => escenario.CrearCargarCompromiso().HandleAsync(
            CrearComando(beca, _pdfValido));

        await accion.Should().ThrowAsync<SolicitudInvalidaException>();
        escenario.Almacenamiento.RutasSubidas.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_EliminarElArchivo_Cuando_LaPersistenciaFalla()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaConCompromiso(escenario);
        escenario.Unidad.ErrorGuardado = new InvalidOperationException("fallo al guardar");

        var accion = () => escenario.CrearCargarCompromiso().HandleAsync(
            CrearComando(beca, _pdfValido));

        await accion.Should().ThrowAsync<InvalidOperationException>();

        escenario.Almacenamiento.UrlsEliminadas.Should().ContainSingle()
            .Which.Should().Be(escenario.Almacenamiento.Resultado.Url);
        escenario.Unidad.Confirmaciones.Should().Be(0);
        escenario.Outbox.Descartado.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_EliminarElArchivo_Cuando_ElHashDevueltoNoCoincide()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaConCompromiso(escenario);
        escenario.Almacenamiento.CalcularHash = false;
        escenario.Almacenamiento.Resultado = new ArchivoAlmacenado(
            "https://storage.example/compromiso.pdf",
            new string('c', 64),
            10);

        var accion = () => escenario.CrearCargarCompromiso().HandleAsync(
            CrearComando(beca, _pdfValido));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("almacenamiento");
        escenario.Almacenamiento.UrlsEliminadas.Should().ContainSingle()
            .Which.Should().Be("https://storage.example/compromiso.pdf");
        beca.Compromiso!.NumeroIntentoCarga.Should().Be(0);
    }

    [Fact]
    public async Task Debe_EliminarElArchivo_Cuando_ElTamanioDevueltoNoCoincide()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaConCompromiso(escenario);
        var hash = Convert.ToHexString(SHA256.HashData(_pdfValido)).ToLowerInvariant();
        escenario.Almacenamiento.CalcularHash = false;
        escenario.Almacenamiento.Resultado = new ArchivoAlmacenado(
            "https://storage.example/compromiso.pdf",
            hash,
            _pdfValido.Length + 1);

        var accion = () => escenario.CrearCargarCompromiso().HandleAsync(
            CrearComando(beca, _pdfValido));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("almacenamiento");
        excepcion.Which.InnerException.Should()
            .BeOfType<InvalidOperationException>()
            .Which.Message.Should().Contain("tamaño");
        escenario.Almacenamiento.UrlsEliminadas.Should().ContainSingle()
            .Which.Should().Be("https://storage.example/compromiso.pdf");
        beca.Compromiso!.NumeroIntentoCarga.Should().Be(0);
        escenario.Unidad.Confirmaciones.Should().Be(0);
    }

    [Fact]
    public async Task Debe_Fallar_Cuando_ElAlmacenamientoNoResponde()
    {
        var escenario = new EscenarioOtorgamiento();
        var beca = CrearBecaConCompromiso(escenario);
        escenario.Almacenamiento.ErrorSubida = new IOException("sin almacenamiento");

        var accion = () => escenario.CrearCargarCompromiso().HandleAsync(
            CrearComando(beca, _pdfValido));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("almacenamiento");
        escenario.Unidad.NivelesSolicitados.Should().BeEmpty();
    }

    private static CargarCompromisoBecarioCommand CrearComando(
        Beca beca,
        byte[] contenido)
        => new(
            beca.Id,
            "compromiso-firmado.pdf",
            "application/pdf",
            contenido);

    private static Beca CrearBecaConCompromiso(EscenarioOtorgamiento escenario)
    {
        var beca = DatosAplicacion.CrearBecaActiva(
            escenario.PostulacionId,
            escenario.ProgramaBecaId,
            escenario.PeriodoAcademicoId,
            escenario.EstudianteId,
            requiereCompromisoFirmado: true);
        escenario.Repositorio.Becas.Add(beca);
        return beca;
    }
}
