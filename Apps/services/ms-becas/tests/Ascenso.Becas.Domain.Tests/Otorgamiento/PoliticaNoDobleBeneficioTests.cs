using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Servicios;

namespace Ascenso.Becas.Domain.Tests.Otorgamiento;

public sealed class PoliticaNoDobleBeneficioTests
{
    [Fact]
    public void Debe_Rechazar_Cuando_ElEstudianteYaTieneOtraBecaActivaTotal()
    {
        var estudianteId = PruebasOtorgamiento.NuevoId();
        var anterior = CrearActiva(
            estudianteId,
            PruebasOtorgamiento.NuevoId(),
            PruebasOtorgamiento.Cobertura(TipoCobertura.Total, 100m),
            permiteAcumulacion: false);
        var candidata = PruebasOtorgamiento.CrearBeca(PruebasOtorgamiento.DatosBeca(
            estudianteId: estudianteId,
            cobertura: PruebasOtorgamiento.Cobertura(TipoCobertura.Parcial, 50m)));

        var excepcion = Assert.Throws<DobleBeneficioException>(
            () => PoliticaNoDobleBeneficio.Validar(candidata, [anterior]));

        Assert.Contains("TOTAL", excepcion.Detalle, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Debe_Rechazar_Cuando_CoincideElPeriodoYNoAmbosPermitenAcumulacion()
    {
        var estudianteId = PruebasOtorgamiento.NuevoId();
        var periodoId = PruebasOtorgamiento.NuevoId();
        var anterior = CrearActiva(
            estudianteId,
            periodoId,
            PruebasOtorgamiento.Cobertura(TipoCobertura.Parcial, 50m),
            permiteAcumulacion: true);
        var candidata = PruebasOtorgamiento.CrearBeca(PruebasOtorgamiento.DatosBeca(
            estudianteId: estudianteId,
            periodoId: periodoId,
            cobertura: PruebasOtorgamiento.Cobertura(TipoCobertura.Parcial, 30m),
            permiteAcumulacion: false));

        Assert.Throws<DobleBeneficioException>(
            () => PoliticaNoDobleBeneficio.Validar(candidata, [anterior]));
    }

    [Fact]
    public void Debe_Permitir_Cuando_AmbosProgramasPermitenAcumulacionEnElMismoPeriodo()
    {
        var estudianteId = PruebasOtorgamiento.NuevoId();
        var periodoId = PruebasOtorgamiento.NuevoId();
        var anterior = CrearActiva(
            estudianteId,
            periodoId,
            PruebasOtorgamiento.Cobertura(TipoCobertura.Parcial, 50m),
            permiteAcumulacion: true);
        var candidata = PruebasOtorgamiento.CrearBeca(PruebasOtorgamiento.DatosBeca(
            estudianteId: estudianteId,
            periodoId: periodoId,
            cobertura: PruebasOtorgamiento.Cobertura(TipoCobertura.Parcial, 30m),
            permiteAcumulacion: true));

        PoliticaNoDobleBeneficio.Validar(candidata, [anterior]);

        Assert.Equal(EstadoBeca.Otorgada, candidata.Estado);
    }

    [Fact]
    public void Debe_Permitir_Cuando_LasBecasActivasEstanEnPeriodosDistintos()
    {
        var estudianteId = PruebasOtorgamiento.NuevoId();
        var anterior = CrearActiva(
            estudianteId,
            PruebasOtorgamiento.NuevoId(),
            PruebasOtorgamiento.Cobertura(TipoCobertura.Parcial, 50m),
            permiteAcumulacion: false);
        var candidata = PruebasOtorgamiento.CrearBeca(PruebasOtorgamiento.DatosBeca(
            estudianteId: estudianteId,
            periodoId: PruebasOtorgamiento.NuevoId(),
            cobertura: PruebasOtorgamiento.Cobertura(TipoCobertura.MontoFijo, 0m, 300m)));

        PoliticaNoDobleBeneficio.Validar(candidata, [anterior]);
    }

    [Fact]
    public void Debe_IgnorarLasBecasSuspendidasYDeOtrosEstudiantes()
    {
        var estudianteId = PruebasOtorgamiento.NuevoId();
        var periodoId = PruebasOtorgamiento.NuevoId();
        var suspendida = CrearActiva(
            estudianteId,
            periodoId,
            PruebasOtorgamiento.Cobertura(TipoCobertura.Total, 100m),
            permiteAcumulacion: false);
        suspendida.Suspender(PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(10),
            "Suspensión temporal de la beca."));

        var otroEstudiante = CrearActiva(
            PruebasOtorgamiento.NuevoId(),
            periodoId,
            PruebasOtorgamiento.Cobertura(TipoCobertura.Total, 100m),
            permiteAcumulacion: false);

        var candidata = PruebasOtorgamiento.CrearBeca(PruebasOtorgamiento.DatosBeca(
            estudianteId: estudianteId,
            periodoId: periodoId,
            cobertura: PruebasOtorgamiento.Cobertura(TipoCobertura.Total, 100m)));

        PoliticaNoDobleBeneficio.Validar(candidata, [suspendida, otroEstudiante]);
    }

    [Fact]
    public void Debe_IgnorarLaPropiaCandidata_Cuando_ApareceEnLaLista()
    {
        var candidata = PruebasOtorgamiento.CrearBecaActiva();

        PoliticaNoDobleBeneficio.Validar(candidata, [candidata]);
    }

    private static Beca CrearActiva(
        Guid estudianteId,
        Guid periodoId,
        Ascenso.Becas.Domain.ValueObjects.PoliticaCobertura cobertura,
        bool permiteAcumulacion)
        => PruebasOtorgamiento.CrearBecaActiva(PruebasOtorgamiento.DatosBeca(
            estudianteId: estudianteId,
            periodoId: periodoId,
            cobertura: cobertura,
            permiteAcumulacion: permiteAcumulacion));
}
