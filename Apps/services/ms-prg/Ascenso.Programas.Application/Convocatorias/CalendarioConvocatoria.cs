namespace Ascenso.Programas.Application.Convocatorias;

/// <summary>
/// Normaliza fechas de calendario a instantes UTC usando la zona horaria de Caracas.
/// </summary>
public static class CalendarioConvocatoria
{
    private static readonly Lazy<TimeZoneInfo> _zonaHoraria = new(ResolverZonaHoraria);

    public static DateTimeOffset NormalizarInicioDelDia(DateOnly fecha)
        => Convertir(fecha, new TimeOnly(0, 0, 0));

    public static DateTimeOffset NormalizarFinDelDia(DateOnly fecha)
        => Convertir(fecha, new TimeOnly(23, 59, 59));

    public static DateOnly FechaCaracas(DateTimeOffset instante)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instante, _zonaHoraria.Value).DateTime);

    public static DateTimeOffset Convertir(DateOnly fecha, TimeOnly hora)
    {
        var fechaHoraLocal = DateTime.SpecifyKind(fecha.ToDateTime(hora), DateTimeKind.Unspecified);
        var fechaHoraUtc = TimeZoneInfo.ConvertTimeToUtc(fechaHoraLocal, _zonaHoraria.Value);

        return new DateTimeOffset(fechaHoraUtc, TimeSpan.Zero);
    }

    private static TimeZoneInfo ResolverZonaHoraria()
    {
        foreach (var identificador in new[] { "America/Caracas", "Venezuela Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(identificador);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        // Venezuela no aplica horario de verano; UTC-4 es un respaldo estable.
        return TimeZoneInfo.CreateCustomTimeZone(
            "America/Caracas-Fallback",
            TimeSpan.FromHours(-4),
            "America/Caracas",
            "America/Caracas");
    }
}
