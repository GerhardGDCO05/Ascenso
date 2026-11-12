using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Datos;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

/// <summary>
/// Datos válidos y únicos para las pruebas de persistencia.
/// </summary>
internal static class DatosPrueba
{
    public static readonly DateTimeOffset FechaBase =
        new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    public static ContextoOperacion CrearContextoOperacion(DateTimeOffset? fecha = null)
        => new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            fecha ?? FechaBase);

    public static DatosCreacionBeca CrearDatosBeca(
        Guid? estudianteId = null,
        string? codigoBeca = null,
        Guid? periodoId = null,
        TipoCobertura modalidad = TipoCobertura.Parcial,
        bool permiteAcumulacion = true,
        bool requiereCompromisoFirmado = false,
        int duracionPeriodos = 2)
    {
        var porcentaje = modalidad == TipoCobertura.Total ? 100m : 50m;

        return new DatosCreacionBeca(
            estudianteId ?? Guid.CreateVersion7(),
            "12345678",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            periodoId ?? Guid.CreateVersion7(),
            duracionPeriodos,
            new PoliticaCobertura(modalidad, porcentaje, 0m, "USD"),
            Guid.CreateVersion7(),
            1500m,
            permiteAcumulacion,
            requiereCompromisoFirmado,
            codigoBeca ?? NuevoCodigoBeca());
    }

    public static DatosOfertaBeca CrearDatosOferta(DatosCreacionBeca datosBeca)
        => new(
            datosBeca.PostulacionOrigenId,
            Guid.CreateVersion7(),
            1500m,
            1500m * datosBeca.DuracionTotalPeriodos,
            datosBeca.DuracionTotalPeriodos,
            new CondicionesPermanencia(
                13m,
                4,
                1,
                40,
                ConsecuenciaPermanencia.LlamadoAtencion),
            "https://storage.example/carta-otorgamiento.pdf",
            new string('a', 64),
            $"job-{Guid.NewGuid():N}");

    public static Beca CrearBecaActiva(
        DatosCreacionBeca datosBeca,
        ContextoOperacion contexto)
    {
        var beca = Beca.CrearOtorgada(datosBeca, contexto);
        var oferta = beca.EmitirOferta(CrearDatosOferta(datosBeca), contexto);
        beca.AceptarOferta(oferta.Id, contexto);
        return beca;
    }

    public static string NuevoCodigoBeca()
        => $"BECA-{Guid.NewGuid():N}"[..20];
}
