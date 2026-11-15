using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;

namespace Ascenso.Postulaciones.Domain.Tests.Postulantes.Agregados;

public sealed class PostulanteTests
{
    private static readonly Guid _usuarioId = Guid.NewGuid();
    private static readonly Guid _universidadId = Guid.NewGuid();
    private static readonly Guid _carreraId = Guid.NewGuid();
    private static readonly Guid _periodoId = Guid.NewGuid();
    private static readonly DateTimeOffset _fechaRegistro =
        new(2026, 11, 14, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_CrearElPerfilEnEstadoIncompleto_Cuando_LosDatosSonValidos()
    {
        var postulante = CrearValido();

        Assert.NotEqual(Guid.Empty, postulante.Id);
        Assert.True(UuidV7.EsVersion7(postulante.Id));
        Assert.Equal(_usuarioId, postulante.UsuarioId);
        Assert.Equal("Ana", postulante.Nombres);
        Assert.Equal("Pérez Gómez", postulante.Apellidos);
        Assert.Equal("Ana Pérez Gómez", postulante.NombreCompleto);
        Assert.Equal(EstadoPerfil.Incompleto, postulante.EstadoPerfil);
        Assert.True(postulante.AceptaTratamientoDatos);
        Assert.Equal(_fechaRegistro, postulante.FechaRegistro);
        Assert.Equal(_fechaRegistro, postulante.FechaCreacion);
        Assert.Equal(_usuarioId, postulante.UsuarioCreacionId);
        Assert.Equal(1, postulante.Version);
        Assert.Empty(postulante.EventosDominio);
    }

    [Fact]
    public void Debe_NormalizarLosCamposDeTexto_Cuando_ContienenEspaciosExtremos()
    {
        var postulante = Postulante.Crear(
            Guid.Empty,
            _usuarioId,
            "  12345678  ",
            "  Ana  ",
            "  Pérez Gómez  ",
            new DatosContacto("ana@example.com", "999888777"),
            _universidadId,
            _carreraId,
            _periodoId,
            CondicionAcademicaIngreso.Matriculado,
            aceptaTratamientoDatos: true,
            _fechaRegistro);

        Assert.Null(postulante.PersonaId);
        Assert.Equal("12345678", postulante.NumeroDocumentoIdentidad);
        Assert.Equal("Ana", postulante.Nombres);
        Assert.Equal("Pérez Gómez", postulante.Apellidos);
        Assert.Equal(CondicionAcademicaIngreso.Matriculado, postulante.CondicionAcademica);
    }

    [Fact]
    public void Debe_ConservarElPersonaId_Cuando_SeProporciona()
    {
        var personaId = Guid.NewGuid();

        var postulante = CrearValido(personaId: personaId);

        Assert.Equal(personaId, postulante.PersonaId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234")]
    public void Debe_RechazarElNumeroDeDocumento_Cuando_NoEsValido(string documento)
    {
        Assert.Throws<DatosPersonalesInvalidosException>(
            () => CrearValido(numeroDocumentoIdentidad: documento));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" A ")]
    [InlineData("Ana 123")]
    public void Debe_RechazarElNombre_Cuando_NoEsValido(string nombres)
    {
        Assert.Throws<DatosPersonalesInvalidosException>(
            () => CrearValido(nombres: nombres));
    }

    [Fact]
    public void Debe_RechazarElRegistro_Cuando_NoSeAceptaElTratamientoDeDatos()
    {
        var excepcion = Assert.Throws<ExcepcionReglaNegocio>(
            () => CrearValido(aceptaTratamientoDatos: false));

        Assert.Equal("TRATAMIENTO_DATOS_NO_ACEPTADO", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarElRegistro_Cuando_LaCondicionAcademicaNoEsValida()
    {
        Assert.Throws<CondicionAcademicaInvalidaException>(
            () => CrearValido(condicionAcademica: (CondicionAcademicaIngreso)99));
    }

    [Fact]
    public void Debe_RechazarElRegistro_Cuando_NoHayUsuarioAutenticado()
    {
        Assert.Throws<NoAutenticadoException>(
            () => CrearValido(usuarioId: Guid.Empty));
    }

    [Fact]
    public void Debe_RechazarElRegistro_Cuando_FaltaUnIdentificadorAcademico()
    {
        Assert.Throws<ArgumentException>(() => CrearValido(universidadAliadaId: Guid.Empty));
        Assert.Throws<ArgumentException>(() => CrearValido(carreraId: Guid.Empty));
        Assert.Throws<ArgumentException>(() => CrearValido(periodoActualId: Guid.Empty));
    }

    [Fact]
    public void Debe_RechazarElRegistro_Cuando_LaFechaDeRegistroEsPredeterminada()
    {
        Assert.Throws<ArgumentException>(() => CrearValido(fechaRegistro: default(DateTimeOffset)));
    }

    [Fact]
    public void Debe_RechazarElRegistro_Cuando_FaltanLosDatosDeContacto()
    {
        Assert.Throws<DatosPersonalesInvalidosException>(
            () => Postulante.Crear(
                null,
                _usuarioId,
                "12345678",
                "Ana",
                "Pérez Gómez",
                null!,
                _universidadId,
                _carreraId,
                _periodoId,
                CondicionAcademicaIngreso.Admitido,
                aceptaTratamientoDatos: true,
                _fechaRegistro));
    }

    private static Postulante CrearValido(
        Guid? personaId = null,
        Guid? usuarioId = null,
        string numeroDocumentoIdentidad = "12345678",
        string nombres = "Ana",
        string apellidos = "Pérez Gómez",
        Guid? universidadAliadaId = null,
        Guid? carreraId = null,
        Guid? periodoActualId = null,
        CondicionAcademicaIngreso condicionAcademica = CondicionAcademicaIngreso.Admitido,
        bool aceptaTratamientoDatos = true,
        DateTimeOffset? fechaRegistro = null)
        => Postulante.Crear(
            personaId,
            usuarioId ?? _usuarioId,
            numeroDocumentoIdentidad,
            nombres,
            apellidos,
            new DatosContacto("ana.perez@example.com", "+51 999 888 777"),
            universidadAliadaId ?? _universidadId,
            carreraId ?? _carreraId,
            periodoActualId ?? _periodoId,
            condicionAcademica,
            aceptaTratamientoDatos,
            fechaRegistro ?? _fechaRegistro);
}
