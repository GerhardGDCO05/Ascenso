using System.Globalization;
using Ascenso.Becas.Api.Logging;
using FluentAssertions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Ascenso.Becas.IntegrationTests.Logging;

public sealed class RedaccionLogsTests
{
    private const string ValorSecreto = "valor-secreto-123";

    [Fact]
    public void Debe_Redactar_ElOtp_EnUnaPropiedadEscalar()
    {
        using var logger = CrearLogger(out var sink);

        logger.Information("Verificación {CodigoOtp}", "483920");

        var evento = sink.Unico();
        ValorEscalar(Propiedad(evento, "CodigoOtp"))
            .Should().Be(RedaccionDatosSensibles.Reemplazo);
        Texto(evento).Should().NotContain("483920");
    }

    [Theory]
    [InlineData("otp")]
    [InlineData("CodigoOtp")]
    [InlineData("numero_cuenta")]
    [InlineData("CuentaBancaria")]
    [InlineData("iban")]
    [InlineData("cbu")]
    [InlineData("documento")]
    [InlineData("DocumentoIdentidad")]
    [InlineData("cedula")]
    [InlineData("dni")]
    [InlineData("pasaporte")]
    [InlineData("correo")]
    [InlineData("correoElectronico")]
    [InlineData("email")]
    public void Debe_Redactar_LasClavesEquivalentes(string clave)
    {
        using var logger = CrearLogger(out var sink);

        logger.Information($"Dato {{{clave}}}", ValorSecreto);

        var evento = sink.Unico();
        ValorEscalar(Propiedad(evento, clave))
            .Should().Be(RedaccionDatosSensibles.Reemplazo);
        Texto(evento).Should().NotContain(ValorSecreto);
    }

    [Theory]
    [InlineData("Correo Electrónico")]
    [InlineData("Número-Cuenta")]
    [InlineData("DOCUMENTO IDENTIDAD")]
    [InlineData("Código OTP")]
    public void Debe_ReconocerClavesConAcentosYSeparadores(string clave)
        => RedaccionDatosSensibles.EsClaveSensible(clave).Should().BeTrue();

    [Theory]
    [InlineData("ProgramaId")]
    [InlineData("Monto")]
    [InlineData("Estado")]
    [InlineData("Nombre")]
    [InlineData("")]
    public void Debe_Ignorar_LasClavesNoSensibles(string clave)
        => RedaccionDatosSensibles.EsClaveSensible(clave).Should().BeFalse();

    [Fact]
    public void Debe_Conservar_LosValoresNoSensibles()
    {
        using var logger = CrearLogger(out var sink);
        var programaId = Guid.NewGuid();

        logger.Information(
            "Programa {ProgramaId} por {Monto}",
            programaId,
            1500.50m);

        var evento = sink.Unico();
        ValorEscalar(Propiedad(evento, "ProgramaId")).Should().Be(programaId.ToString());
        ValorEscalar(Propiedad(evento, "Monto")).Should().Be("1500.50");
    }

    [Fact]
    public void Debe_Redactar_LasPropiedadesSensiblesDeUnObjeto()
    {
        using var logger = CrearLogger(out var sink);
        var solicitud = new SolicitudConDatosSensibles(
            "ana@ejemplo.com",
            "12345678",
            "ES9121000418450200051332",
            "Ana Pérez",
            1500m);

        logger.Information("Solicitud {@Solicitud}", solicitud);

        var estructura = (StructureValue)Propiedad(sink.Unico(), "Solicitud");
        ValorEstructura(estructura, "CorreoElectronico")
            .Should().Be(RedaccionDatosSensibles.Reemplazo);
        ValorEstructura(estructura, "DocumentoIdentidad")
            .Should().Be(RedaccionDatosSensibles.Reemplazo);
        ValorEstructura(estructura, "NumeroCuenta")
            .Should().Be(RedaccionDatosSensibles.Reemplazo);
        ValorEstructura(estructura, "Nombre").Should().Be("Ana Pérez");
        ValorEstructura(estructura, "Monto").Should().Be("1500");
    }

    [Fact]
    public void Debe_Omitir_LosValoresOriginales_EnElTextoDelObjeto()
    {
        using var logger = CrearLogger(out var sink);
        var solicitud = new SolicitudConDatosSensibles(
            "ana@ejemplo.com",
            "12345678",
            "ES9121000418450200051332",
            "Ana Pérez",
            1500m);

        logger.Information("Solicitud {@Solicitud}", solicitud);

        var texto = Texto(sink.Unico());
        texto.Should().Contain("Ana Pérez");
        texto.Should().NotContain("ana@ejemplo.com");
        texto.Should().NotContain("12345678");
        texto.Should().NotContain("ES9121000418450200051332");
    }

    [Fact]
    public void Debe_Redactar_PropiedadesSensiblesAnidadas()
    {
        using var logger = CrearLogger(out var sink);
        var solicitud = new SolicitudAnidada(
            "Ana Pérez",
            new ContactoPostulante("ana@ejemplo.com", "777-1234"));

        logger.Information("Solicitud {@Solicitud}", solicitud);

        var estructura = (StructureValue)Propiedad(sink.Unico(), "Solicitud");
        var contacto = (StructureValue)estructura.Properties
            .Single(propiedad => propiedad.Name == "Contacto")
            .Value;

        ValorEstructura(contacto, "Correo")
            .Should().Be(RedaccionDatosSensibles.Reemplazo);
        ValorEstructura(contacto, "Telefono").Should().Be("777-1234");
        ValorEstructura(estructura, "Nombre").Should().Be("Ana Pérez");
    }

    [Fact]
    public void Debe_Conservar_LosObjetosSinPropiedadesSensibles()
    {
        using var logger = CrearLogger(out var sink);

        logger.Information(
            "Programa {@Programa}",
            new ProgramaResumen("Beca Excelencia", 12));

        var estructura = (StructureValue)Propiedad(sink.Unico(), "Programa");
        ValorEstructura(estructura, "Nombre").Should().Be("Beca Excelencia");
        ValorEstructura(estructura, "DuracionPeriodos").Should().Be("12");
    }

    [Fact]
    public void Debe_Redactar_ElObjeto_CompletoEnLaSalida()
    {
        using var logger = CrearLogger(out var sink);

        logger.Information(
            "Solicitud {@Solicitud}",
            new SolicitudConDatosSensibles(
                "ana@ejemplo.com",
                "12345678",
                "ES9121000418450200051332",
                "Ana Pérez",
                1500m));

        var texto = Texto(sink.Unico());

        texto.Should().Contain(RedaccionDatosSensibles.Reemplazo);
        texto.Should().NotContain("ana@ejemplo.com");
        texto.Should().NotContain("12345678");
        texto.Should().NotContain("ES9121000418450200051332");
    }

    private static Logger CrearLogger(out SinkEnMemoria sink)
    {
        sink = new SinkEnMemoria();

        return new LoggerConfiguration()
            .Enrich.With<RedaccionLogEventEnricher>()
            .Destructure.With<RedaccionDestructuringPolicy>()
            .WriteTo.Sink(sink)
            .CreateLogger();
    }

    private static LogEventPropertyValue Propiedad(LogEvent evento, string nombre)
        => evento.Properties[nombre];

    private static string? ValorEscalar(LogEventPropertyValue valor)
        => (valor as ScalarValue)?.Value is { } escalar
            ? Convert.ToString(escalar, CultureInfo.InvariantCulture)
            : null;

    private static string? ValorEstructura(StructureValue estructura, string nombre)
        => ValorEscalar(estructura.Properties
            .Single(propiedad => propiedad.Name == nombre)
            .Value);

    private static string Texto(LogEvent evento)
    {
        using var escritor = new StringWriter(CultureInfo.InvariantCulture);

        new MessageTemplateTextFormatter(
                "{Message:lj} {Properties:j}",
                CultureInfo.InvariantCulture)
            .Format(evento, escritor);

        return escritor.ToString();
    }

    private sealed record SolicitudConDatosSensibles(
        string CorreoElectronico,
        string DocumentoIdentidad,
        string NumeroCuenta,
        string Nombre,
        decimal Monto);

    private sealed record ContactoPostulante(string Correo, string Telefono);

    private sealed record SolicitudAnidada(
        string Nombre,
        ContactoPostulante Contacto);

    private sealed record ProgramaResumen(string Nombre, int DuracionPeriodos);

    private sealed class SinkEnMemoria : ILogEventSink
    {
        private readonly List<LogEvent> _eventos = [];

        public void Emit(LogEvent logEvent) => _eventos.Add(logEvent);

        public LogEvent Unico() => _eventos.Single();
    }
}
