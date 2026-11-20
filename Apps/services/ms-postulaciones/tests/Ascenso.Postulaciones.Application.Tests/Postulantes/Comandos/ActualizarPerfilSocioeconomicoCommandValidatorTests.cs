using Ascenso.Postulaciones.Application.Postulantes.Comandos.ActualizarPerfilSocioeconomico;
using FluentAssertions;
using FluentValidation.Results;

namespace Ascenso.Postulaciones.Application.Tests.Postulantes.Comandos;

public sealed class ActualizarPerfilSocioeconomicoCommandValidatorTests
{
    private readonly ActualizarPerfilSocioeconomicoCommandValidator _validator = new();

    [Fact]
    public void Debe_AceptarComando_Cuando_LaListaFamiliarEstaVacia()
    {
        var resultado = _validator.Validate(CrearComandoValido());

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Debe_AceptarComando_Cuando_LosFamiliaresSonValidos()
    {
        var comando = CrearComandoValido(
            miembros: [CrearFamiliarValido()]);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Debe_AceptarComando_Cuando_NoTrabajaConJubiladoYIngresoDeclarado()
    {
        var comando = CrearComandoValido(
            trabaja: false,
            ocupacion: "JUBILADO",
            ingresoMensualPostulante: 500m);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    public void Debe_AceptarComando_Cuando_ElPromedioEstaEnLosLimites(decimal promedio)
    {
        var comando = CrearComandoValido(promedioActual: promedio);

        var resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("CONVIVIENTE")]
    public void Debe_RechazarComando_Cuando_ElEstadoCivilEsInvalido(string estadoCivil)
    {
        var comando = CrearComandoValido(estadoCivil: estadoCivil);

        DebeSerInvalido(comando, nameof(comando.EstadoCivil));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ElPromedioEsNulo()
    {
        var comando = CrearComandoValido(promedioActual: null);

        DebeSerInvalido(comando, nameof(comando.PromedioActual));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(20.01)]
    public void Debe_RechazarComando_Cuando_ElPromedioEstaFueraDeRango(decimal promedio)
    {
        var comando = CrearComandoValido(promedioActual: promedio);

        DebeSerInvalido(comando, nameof(comando.PromedioActual));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ElPromedioTieneMasDeDosDecimales()
    {
        var comando = CrearComandoValido(promedioActual: 15.555m);

        DebeSerInvalido(comando, nameof(comando.PromedioActual));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_TrabajaEsNulo()
    {
        var comando = CrearComandoValido(trabaja: null);

        DebeSerInvalido(comando, nameof(comando.Trabaja));
    }

    [Theory]
    [InlineData("")]
    [InlineData("DESEMPLEADO")]
    public void Debe_RechazarComando_Cuando_LaOcupacionEsInvalida(string ocupacion)
    {
        var comando = CrearComandoValido(ocupacion: ocupacion);

        DebeSerInvalido(comando, nameof(comando.Ocupacion));
    }

    [Theory]
    [InlineData(true, "ESTUDIANTE")]
    [InlineData(false, "EMPLEADO")]
    public void Debe_RechazarComando_Cuando_LaCondicionLaboralEsIncoherente(
        bool trabaja,
        string ocupacion)
    {
        var comando = CrearComandoValido(trabaja: trabaja, ocupacion: ocupacion);

        DebeSerInvalido(comando, nameof(comando.Ocupacion));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ElIngresoMensualEsNulo()
    {
        var comando = CrearComandoValido(ingresoMensualPostulante: null);

        DebeSerInvalido(comando, nameof(comando.IngresoMensualPostulante));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ElIngresoMensualEsNegativo()
    {
        var comando = CrearComandoValido(ingresoMensualPostulante: -0.01m);

        DebeSerInvalido(comando, nameof(comando.IngresoMensualPostulante));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_ElIngresoMensualTieneMasDeDosDecimales()
    {
        var comando = CrearComandoValido(ingresoMensualPostulante: 1000.001m);

        DebeSerInvalido(comando, nameof(comando.IngresoMensualPostulante));
    }

    [Fact]
    public void Debe_RechazarComando_Cuando_LaListaDeFamiliaresEsNula()
    {
        var comando = CrearComandoValido() with { MiembrosFamiliares = null! };

        DebeSerInvalido(comando, nameof(comando.MiembrosFamiliares));
    }

    [Theory]
    [InlineData("")]
    [InlineData("TIO")]
    public void Debe_RechazarFamiliar_Cuando_ElParentescoEsInvalido(string parentesco)
    {
        var comando = CrearComandoValido(
            miembros: [CrearFamiliarValido(parentesco: parentesco)]);

        DebeSerInvalido(
            comando,
            "MiembrosFamiliares[0].Parentesco");
    }

    [Fact]
    public void Debe_RechazarFamiliar_Cuando_LaEdadEsNula()
    {
        var comando = CrearComandoValido(
            miembros: [CrearFamiliarValido(edad: null)]);

        DebeSerInvalido(
            comando,
            "MiembrosFamiliares[0].Edad");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(121)]
    public void Debe_RechazarFamiliar_Cuando_LaEdadEstaFueraDeRango(int edad)
    {
        var comando = CrearComandoValido(
            miembros: [CrearFamiliarValido(edad: edad)]);

        DebeSerInvalido(
            comando,
            "MiembrosFamiliares[0].Edad");
    }

    [Theory]
    [InlineData("")]
    [InlineData("DESEMPLEADO")]
    public void Debe_RechazarFamiliar_Cuando_LaOcupacionEsInvalida(string ocupacion)
    {
        var comando = CrearComandoValido(
            miembros: [CrearFamiliarValido(ocupacion: ocupacion)]);

        DebeSerInvalido(
            comando,
            "MiembrosFamiliares[0].Ocupacion");
    }

    [Fact]
    public void Debe_RechazarFamiliar_Cuando_ElIngresoMensualEsNulo()
    {
        var comando = CrearComandoValido(
            miembros: [CrearFamiliarValido(ingresoMensual: null)]);

        DebeSerInvalido(
            comando,
            "MiembrosFamiliares[0].IngresoMensual");
    }

    [Fact]
    public void Debe_RechazarFamiliar_Cuando_ElIngresoMensualEsNegativo()
    {
        var comando = CrearComandoValido(
            miembros: [CrearFamiliarValido(ingresoMensual: -0.01m)]);

        DebeSerInvalido(
            comando,
            "MiembrosFamiliares[0].IngresoMensual");
    }

    [Fact]
    public void Debe_RechazarFamiliar_Cuando_ElIngresoMensualTieneMasDeDosDecimales()
    {
        var comando = CrearComandoValido(
            miembros: [CrearFamiliarValido(ingresoMensual: 500.001m)]);

        DebeSerInvalido(
            comando,
            "MiembrosFamiliares[0].IngresoMensual");
    }

    private void DebeSerInvalido(
        ActualizarPerfilSocioeconomicoCommand comando,
        string propiedad)
    {
        ValidationResult resultado = _validator.Validate(comando);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.PropertyName == propiedad);
    }

    private static ActualizarPerfilSocioeconomicoCommand CrearComandoValido(
        string estadoCivil = "SOLTERO",
        decimal? promedioActual = 15.50m,
        bool? trabaja = true,
        string ocupacion = "EMPLEADO",
        decimal? ingresoMensualPostulante = 1200m,
        IReadOnlyList<DatosMiembroFamiliarCommand>? miembros = null)
        => new(
            estadoCivil,
            promedioActual,
            trabaja,
            ocupacion,
            ingresoMensualPostulante,
            miembros ?? []);

    private static DatosMiembroFamiliarCommand CrearFamiliarValido(
        string parentesco = "MADRE",
        int? edad = 45,
        string ocupacion = "LABORES_HOGAR",
        decimal? ingresoMensual = 0m)
        => new(parentesco, edad, ocupacion, ingresoMensual);
}
