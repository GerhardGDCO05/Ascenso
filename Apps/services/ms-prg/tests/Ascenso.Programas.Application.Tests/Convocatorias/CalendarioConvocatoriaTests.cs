using System.Globalization;
using Ascenso.Programas.Application.Convocatorias;
using FluentAssertions;

namespace Ascenso.Programas.Application.Tests.Convocatorias;

public sealed class CalendarioConvocatoriaTests
{
    [Fact]
    public void Debe_NormalizarAperturaAlInicioDelDiaEnCaracasYDevolverUtc()
    {
        var resultado = CalendarioConvocatoria.NormalizarInicioDelDia(new DateOnly(2026, 12, 1));

        resultado.Should().Be(new DateTimeOffset(2026, 12, 1, 4, 0, 0, TimeSpan.Zero));
        resultado.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Debe_NormalizarCierreYSubsanacionAlFinalDelDiaEnCaracasYDevolverUtc()
    {
        var fecha = new DateOnly(2026, 12, 20);
        var esperado = new DateTimeOffset(2026, 12, 21, 3, 59, 59, TimeSpan.Zero);

        CalendarioConvocatoria.NormalizarFinDelDia(fecha).Should().Be(esperado);
    }

    [Fact]
    public void Debe_ConvertirCadaLimiteDeFechaConLaHoraDeCaracas()
    {
        var fechas = new DateOnly(2026, 12, 1);

        CalendarioConvocatoria.Convertir(fechas, new TimeOnly(0, 0))
            .Should().Be(new DateTimeOffset(2026, 12, 1, 4, 0, 0, TimeSpan.Zero));
        CalendarioConvocatoria.Convertir(fechas, new TimeOnly(23, 59, 59))
            .Should().Be(new DateTimeOffset(2026, 12, 2, 3, 59, 59, TimeSpan.Zero));
    }

    [Theory]
    [InlineData("2026-02-01T02:00:00+00:00", "2026-01-31")]
    [InlineData("2026-02-01T04:00:00+00:00", "2026-02-01")]
    public void Debe_ResolverLaFechaDeVigenciaEnElCalendarioDeCaracas(
        string instanteUtc,
        string fechaCaracas)
    {
        var instante = DateTimeOffset.Parse(instanteUtc, CultureInfo.InvariantCulture);
        var esperado = DateOnly.Parse(fechaCaracas, CultureInfo.InvariantCulture);

        CalendarioConvocatoria.FechaCaracas(instante).Should().Be(esperado);
    }
}
