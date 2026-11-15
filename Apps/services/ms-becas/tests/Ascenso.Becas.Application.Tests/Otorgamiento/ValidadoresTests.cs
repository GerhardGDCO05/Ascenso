using Ascenso.Becas.Application.Otorgamiento.Comandos.AceptarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.AnularOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.CargarCompromisoBecario;
using Ascenso.Becas.Application.Otorgamiento.Comandos.EmitirOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.ExpirarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.RechazarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.RevisarCompromisoBecario;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ConsultarBecasActivas;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerBeca;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerOferta;
using FluentAssertions;
using FluentValidation;

namespace Ascenso.Becas.Application.Tests.Otorgamiento;

public sealed class ValidadoresTests
{
    [Fact]
    public void Debe_ValidarElComandoDeEmision()
    {
        DebeSerInvalido(new EmitirOfertaBecaValidator(), new EmitirOfertaBecaCommand(Guid.Empty), "PostulacionId");
        DebeSerValido(new EmitirOfertaBecaValidator(), new EmitirOfertaBecaCommand(Guid.CreateVersion7()));
    }

    [Fact]
    public void Debe_ValidarElComandoDeAceptacion()
    {
        DebeSerInvalido(new AceptarOfertaBecaValidator(), new AceptarOfertaBecaCommand(Guid.Empty), "OfertaId");
        DebeSerValido(new AceptarOfertaBecaValidator(), new AceptarOfertaBecaCommand(Guid.CreateVersion7()));
    }

    [Fact]
    public void Debe_ValidarElComandoDeRechazo()
    {
        DebeSerInvalido(new RechazarOfertaBecaValidator(), new RechazarOfertaBecaCommand(Guid.Empty), "OfertaId");
        DebeSerInvalido(
            new RechazarOfertaBecaValidator(),
            new RechazarOfertaBecaCommand(Guid.CreateVersion7(), new string('a', 1001)),
            "Motivo");
        DebeSerValido(
            new RechazarOfertaBecaValidator(),
            new RechazarOfertaBecaCommand(Guid.CreateVersion7(), "Motivo válido"));
    }

    [Fact]
    public void Debe_ValidarElComandoDeAnulacion()
    {
        DebeSerInvalido(
            new AnularOfertaBecaValidator(),
            new AnularOfertaBecaCommand(Guid.CreateVersion7(), string.Empty),
            "Justificacion");
        DebeSerInvalido(
            new AnularOfertaBecaValidator(),
            new AnularOfertaBecaCommand(Guid.CreateVersion7(), "corta"),
            "Justificacion");
        DebeSerInvalido(
            new AnularOfertaBecaValidator(),
            new AnularOfertaBecaCommand(Guid.CreateVersion7(), new string('a', 1001)),
            "Justificacion");
        DebeSerValido(
            new AnularOfertaBecaValidator(),
            new AnularOfertaBecaCommand(Guid.CreateVersion7(), "Justificación suficiente."));
    }

    [Fact]
    public void Debe_ValidarElComandoDeExpiracion()
    {
        DebeSerInvalido(new ExpirarOfertaBecaValidator(), new ExpirarOfertaBecaCommand(Guid.Empty), "OfertaId");
        DebeSerValido(new ExpirarOfertaBecaValidator(), new ExpirarOfertaBecaCommand(Guid.CreateVersion7()));
    }

    [Fact]
    public void Debe_ValidarElComandoDeCargaDeCompromiso()
    {
        var validador = new CargarCompromisoBecarioValidator();
        var contenido = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        DebeSerInvalido(
            validador,
            new CargarCompromisoBecarioCommand(Guid.Empty, "compromiso.pdf", "application/pdf", contenido),
            "BecaId");
        DebeSerInvalido(
            validador,
            new CargarCompromisoBecarioCommand(Guid.CreateVersion7(), "compromiso.txt", "application/pdf", contenido),
            "NombreArchivo");
        DebeSerInvalido(
            validador,
            new CargarCompromisoBecarioCommand(Guid.CreateVersion7(), "compromiso.pdf", "text/plain", contenido),
            "TipoMime");
        DebeSerInvalido(
            validador,
            new CargarCompromisoBecarioCommand(Guid.CreateVersion7(), "compromiso.pdf", "application/pdf", []),
            "Contenido");
        DebeSerInvalido(
            validador,
            new CargarCompromisoBecarioCommand(Guid.CreateVersion7(), "compromiso.pdf", "application/pdf", null!),
            "Contenido");
        DebeSerInvalido(
            validador,
            new CargarCompromisoBecarioCommand(
                Guid.CreateVersion7(),
                "compromiso.pdf",
                "application/pdf",
                new byte[CargarCompromisoBecarioValidator.TamanioMaximoBytes + 1]),
            "Contenido");
        DebeSerValido(
            validador,
            new CargarCompromisoBecarioCommand(
                Guid.CreateVersion7(),
                "compromiso.pdf",
                "application/pdf",
                contenido));
    }

    [Fact]
    public void Debe_ValidarElComandoDeRevisionDeCompromiso()
    {
        var validador = new RevisarCompromisoBecarioValidator();

        DebeSerInvalido(
            validador,
            new RevisarCompromisoBecarioCommand(Guid.Empty, Aprobar: true),
            "CompromisoId");
        DebeSerInvalido(
            validador,
            new RevisarCompromisoBecarioCommand(Guid.CreateVersion7(), Aprobar: false),
            "Observaciones");
        DebeSerInvalido(
            validador,
            new RevisarCompromisoBecarioCommand(
                Guid.CreateVersion7(),
                Aprobar: false,
                Observaciones: new string('a', 1001)),
            "Observaciones");
        DebeSerValido(
            validador,
            new RevisarCompromisoBecarioCommand(Guid.CreateVersion7(), Aprobar: true));
        DebeSerValido(
            validador,
            new RevisarCompromisoBecarioCommand(
                Guid.CreateVersion7(),
                Aprobar: false,
                Observaciones: "Documento ilegible."));
    }

    [Fact]
    public void Debe_ValidarLaConsultaDeBecasActivas()
    {
        var validador = new ConsultarBecasActivasValidator();

        DebeSerInvalido(
            validador,
            new ConsultarBecasActivasQuery(Guid.Empty, Guid.CreateVersion7()),
            "PostulanteEstudianteId");
        DebeSerInvalido(
            validador,
            new ConsultarBecasActivasQuery(Guid.CreateVersion7(), Guid.Empty),
            "PeriodoAcademicoId");
        DebeSerValido(
            validador,
            new ConsultarBecasActivasQuery(Guid.CreateVersion7(), Guid.CreateVersion7()));
    }

    [Fact]
    public void Debe_ValidarLasConsultasPorIdentificador()
    {
        DebeSerInvalido(new ObtenerOfertaValidator(), new ObtenerOfertaQuery(Guid.Empty), "OfertaId");
        DebeSerValido(new ObtenerOfertaValidator(), new ObtenerOfertaQuery(Guid.CreateVersion7()));
        DebeSerInvalido(new ObtenerBecaValidator(), new ObtenerBecaQuery(Guid.Empty), "BecaId");
        DebeSerValido(new ObtenerBecaValidator(), new ObtenerBecaQuery(Guid.CreateVersion7()));
    }

    private static void DebeSerValido<T>(IValidator<T> validador, T instancia)
    {
        var resultado = validador.Validate(instancia);
        resultado.IsValid.Should().BeTrue(
            "se esperaba una solicitud válida, pero se obtuvieron: {0}",
            string.Join(" | ", resultado.Errors.Select(error => error.ErrorMessage)));
    }

    private static void DebeSerInvalido<T>(
        IValidator<T> validador,
        T instancia,
        string propiedad)
    {
        var resultado = validador.Validate(instancia);
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == propiedad);
    }
}
