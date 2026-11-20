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

    [Fact]
    public void Debe_RegistrarElPerfilSocioeconomico_Cuando_LaListaFamiliarEstaVacia()
    {
        var postulante = CrearValido();
        var fechaModificacion = _fechaRegistro.AddHours(1);

        postulante.RegistrarPerfilSocioeconomico(
            EstadoCivil.Soltero,
            17.50m,
            trabaja: true,
            Ocupacion.Empleado,
            1500.75m,
            [],
            _usuarioId,
            fechaModificacion);

        Assert.Equal(EstadoCivil.Soltero, postulante.EstadoCivil);
        Assert.Equal(17.50m, postulante.PromedioActual);
        Assert.True(postulante.Trabaja);
        Assert.Equal(Ocupacion.Empleado, postulante.Ocupacion);
        Assert.Equal(1500.75m, postulante.IngresoMensualPostulante);
        Assert.Equal(1, postulante.NumeroIntegrantesFamilia);
        Assert.Equal(1500.75m, postulante.IngresoFamiliarMensualTotal);
        Assert.Empty(postulante.MiembrosFamiliares);
        Assert.Equal(EstadoPerfil.Completo, postulante.EstadoPerfil);
        Assert.Equal(_usuarioId, postulante.UsuarioModificacionId);
        Assert.Equal(fechaModificacion, postulante.FechaUltimaModificacion);
        Assert.Equal(2, postulante.Version);
    }

    [Fact]
    public void Debe_CalcularLosTotales_Cuando_SeRegistranFamiliares()
    {
        var postulante = CrearValido();
        var familiares = new DatosMiembroFamiliar[]
        {
            new(Parentesco.Madre, 45, Ocupacion.LaboresHogar, 0m),
            new(Parentesco.Hermano, 16, Ocupacion.Estudiante, 250.25m)
        };

        postulante.RegistrarPerfilSocioeconomico(
            EstadoCivil.Casado,
            15.25m,
            trabaja: true,
            Ocupacion.Independiente,
            1000.75m,
            familiares,
            _usuarioId,
            _fechaRegistro.AddHours(1));

        Assert.Equal(3, postulante.NumeroIntegrantesFamilia);
        Assert.Equal(1251.00m, postulante.IngresoFamiliarMensualTotal);
        Assert.Equal(2, postulante.MiembrosFamiliares.Count);
        Assert.All(
            postulante.MiembrosFamiliares,
            familiar => Assert.Equal(postulante.Id, familiar.PostulanteId));
    }

    [Fact]
    public void Debe_ReemplazarLaComposicionFamiliar_Cuando_SeActualizaElPerfil()
    {
        var postulante = CrearValido();
        var fechaModificacion = _fechaRegistro.AddHours(1);

        postulante.RegistrarPerfilSocioeconomico(
            EstadoCivil.Casado,
            15m,
            trabaja: true,
            Ocupacion.Empleado,
            1000m,
            [
                new(Parentesco.Pareja, 40, Ocupacion.Empleado, 800m),
                new(Parentesco.Hijo, 10, Ocupacion.Estudiante, 0m)
            ],
            _usuarioId,
            fechaModificacion);

        postulante.RegistrarPerfilSocioeconomico(
            EstadoCivil.Soltero,
            16m,
            trabaja: true,
            Ocupacion.Empleado,
            1200m,
            [new(Parentesco.Hijo, 10, Ocupacion.Estudiante, 300m)],
            _usuarioId,
            fechaModificacion.AddHours(1));

        Assert.Single(postulante.MiembrosFamiliares);
        Assert.Equal(2, postulante.NumeroIntegrantesFamilia);
        Assert.Equal(1500m, postulante.IngresoFamiliarMensualTotal);
        Assert.Equal(EstadoPerfil.Completo, postulante.EstadoPerfil);
        Assert.Equal(3, postulante.Version);
    }

    [Fact]
    public void Debe_AceptarUnaPension_Cuando_ElPostulanteNoTrabaja()
    {
        var postulante = CrearValido();

        postulante.RegistrarPerfilSocioeconomico(
            EstadoCivil.Viudo,
            12m,
            trabaja: false,
            Ocupacion.Jubilado,
            800m,
            [],
            _usuarioId,
            _fechaRegistro.AddHours(1));

        Assert.False(postulante.Trabaja);
        Assert.Equal(Ocupacion.Jubilado, postulante.Ocupacion);
        Assert.Equal(800m, postulante.IngresoFamiliarMensualTotal);
    }

    [Theory]
    [InlineData(true, Ocupacion.Estudiante)]
    [InlineData(true, Ocupacion.LaboresHogar)]
    [InlineData(false, Ocupacion.Empleado)]
    [InlineData(false, Ocupacion.Independiente)]
    public void Debe_RechazarLaOcupacion_Cuando_NoEsCoherenteConLaCondicionLaboral(
        bool trabaja,
        Ocupacion ocupacion)
    {
        var postulante = CrearValido();

        var excepcion = Assert.Throws<CondicionLaboralIncoherenteException>(
            () => RegistrarPerfilSocioeconomico(postulante, trabaja: trabaja, ocupacion: ocupacion));

        Assert.Equal(CondicionLaboralIncoherenteException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarElPromedio_Cuando_EstaFueraDeRangoOTieneDemasiadosDecimales()
    {
        var postulante = CrearValido();

        Assert.Throws<DatosSocioeconomicosInvalidosException>(
            () => RegistrarPerfilSocioeconomico(postulante, promedioActual: -0.01m));
        Assert.Throws<DatosSocioeconomicosInvalidosException>(
            () => RegistrarPerfilSocioeconomico(postulante, promedioActual: 20.01m));
        Assert.Throws<DatosSocioeconomicosInvalidosException>(
            () => RegistrarPerfilSocioeconomico(postulante, promedioActual: 10.001m));
    }

    [Fact]
    public void Debe_RechazarElIngresoDelPostulante_Cuando_NoEsValido()
    {
        var postulante = CrearValido();

        Assert.Throws<DatosSocioeconomicosInvalidosException>(
            () => RegistrarPerfilSocioeconomico(postulante, ingresoMensualPostulante: -0.01m));
        Assert.Throws<DatosSocioeconomicosInvalidosException>(
            () => RegistrarPerfilSocioeconomico(postulante, ingresoMensualPostulante: 100.001m));
    }

    [Fact]
    public void Debe_RechazarElPerfil_Cuando_LosCatalogosNoSonValidos()
    {
        var postulante = CrearValido();

        Assert.Throws<CatalogoInvalidoException>(
            () => RegistrarPerfilSocioeconomico(postulante, estadoCivil: (EstadoCivil)99));
        Assert.Throws<CatalogoInvalidoException>(
            () => RegistrarPerfilSocioeconomico(postulante, ocupacion: (Ocupacion)99));
    }

    [Fact]
    public void Debe_RechazarElPerfil_Cuando_FaltaLaColeccionFamiliar()
    {
        var postulante = CrearValido();

        var excepcion = Assert.Throws<MiembroFamiliarInvalidoException>(
            () => postulante.RegistrarPerfilSocioeconomico(
                EstadoCivil.Soltero,
                15m,
                trabaja: true,
                Ocupacion.Empleado,
                1000m,
                null!,
                _usuarioId,
                _fechaRegistro.AddHours(1)));

        Assert.Equal(MiembroFamiliarInvalidoException.CodigoError, excepcion.Codigo);
    }

    [Fact]
    public void Debe_RechazarElPerfil_Cuando_UnFamiliarEsNulo()
    {
        var postulante = CrearValido();

        Assert.Throws<MiembroFamiliarInvalidoException>(
            () => postulante.RegistrarPerfilSocioeconomico(
                EstadoCivil.Soltero,
                15m,
                trabaja: true,
                Ocupacion.Empleado,
                1000m,
                [null!],
                _usuarioId,
                _fechaRegistro.AddHours(1)));
    }

    [Fact]
    public void Debe_ConservarElPerfilIncompleto_Cuando_LaSeccionSocioeconomicaEsInvalida()
    {
        var postulante = CrearValido();

        Assert.Throws<CondicionLaboralIncoherenteException>(
            () => RegistrarPerfilSocioeconomico(
                postulante,
                trabaja: false,
                ocupacion: Ocupacion.Empleado));

        Assert.Equal(EstadoPerfil.Incompleto, postulante.EstadoPerfil);
        Assert.Null(postulante.EstadoCivil);
        Assert.Null(postulante.PromedioActual);
        Assert.Null(postulante.Trabaja);
        Assert.Null(postulante.Ocupacion);
        Assert.Null(postulante.IngresoMensualPostulante);
        Assert.Null(postulante.NumeroIntegrantesFamilia);
        Assert.Null(postulante.IngresoFamiliarMensualTotal);
        Assert.Empty(postulante.MiembrosFamiliares);
        Assert.Equal(1, postulante.Version);
    }

    [Fact]
    public void Debe_ExponerSoloLectura_Cuando_SeInspeccionanLosCamposSocioeconomicos()
    {
        var nombres = new[]
        {
            nameof(Postulante.EstadoCivil),
            nameof(Postulante.PromedioActual),
            nameof(Postulante.Trabaja),
            nameof(Postulante.Ocupacion),
            nameof(Postulante.IngresoMensualPostulante),
            nameof(Postulante.NumeroIntegrantesFamilia),
            nameof(Postulante.IngresoFamiliarMensualTotal),
            nameof(Postulante.MiembrosFamiliares)
        };

        var propiedades = typeof(Postulante).GetProperties()
            .Where(propiedad => nombres.Contains(propiedad.Name))
            .ToArray();

        Assert.Equal(nombres.Length, propiedades.Length);
        Assert.All(
            propiedades,
            propiedad => Assert.False(propiedad.SetMethod?.IsPublic ?? false));
    }

    private static void RegistrarPerfilSocioeconomico(
        Postulante postulante,
        EstadoCivil estadoCivil = EstadoCivil.Soltero,
        decimal promedioActual = 16m,
        bool trabaja = true,
        Ocupacion ocupacion = Ocupacion.Empleado,
        decimal ingresoMensualPostulante = 1000m,
        IReadOnlyCollection<DatosMiembroFamiliar>? miembrosFamiliares = null)
        => postulante.RegistrarPerfilSocioeconomico(
            estadoCivil,
            promedioActual,
            trabaja,
            ocupacion,
            ingresoMensualPostulante,
            miembrosFamiliares ?? [],
            _usuarioId,
            _fechaRegistro.AddHours(1));

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
