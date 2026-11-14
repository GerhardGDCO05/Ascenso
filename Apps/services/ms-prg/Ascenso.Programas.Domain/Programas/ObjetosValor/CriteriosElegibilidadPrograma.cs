using Ascenso.Programas.Domain.Excepciones;

namespace Ascenso.Programas.Domain.Programas.ObjetosValor;

/// <summary>
/// Objeto de valor inmutable que agrupa los criterios de elegibilidad de un programa de becas.
/// </summary>
public sealed record CriteriosElegibilidadPrograma
{
    private CriteriosElegibilidadPrograma()
    {
        UniversidadesElegiblesIds = Array.Empty<Guid>();
        CarrerasElegiblesIds = Array.Empty<Guid>();
    }

    public CriteriosElegibilidadPrograma(
        decimal promedioMinimoIngreso,
        int edadMinima,
        int edadMaxima,
        decimal topeIngresoFamiliarMensual,
        IReadOnlyCollection<Guid> universidadesElegiblesIds,
        IReadOnlyCollection<Guid> carrerasElegiblesIds)
    {
        var promedio = decimal.Round(promedioMinimoIngreso, 2, MidpointRounding.AwayFromZero);
        if (promedio is < 0.00m or > 20.00m)
        {
            throw new PromedioAcademicoInvalidoException(nameof(promedioMinimoIngreso), promedio);
        }

        if (edadMinima <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(edadMinima), "La edad mínima debe ser mayor que cero.");
        }

        if (edadMaxima < edadMinima)
        {
            throw new ArgumentOutOfRangeException(nameof(edadMaxima), "La edad máxima debe ser mayor o igual a la edad mínima.");
        }

        var tope = decimal.Round(topeIngresoFamiliarMensual, 2, MidpointRounding.AwayFromZero);
        if (tope <= 0.00m)
        {
            throw new ArgumentOutOfRangeException(nameof(topeIngresoFamiliarMensual), "El tope de ingreso familiar mensual debe ser mayor que cero.");
        }

        ArgumentNullException.ThrowIfNull(universidadesElegiblesIds);
        ArgumentNullException.ThrowIfNull(carrerasElegiblesIds);

        var universidadesNormalizadas = universidadesElegiblesIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (universidadesNormalizadas.Length == 0)
        {
            throw new ArgumentException("Debe especificar al menos una universidad elegible válida.", nameof(universidadesElegiblesIds));
        }

        var carrerasNormalizadas = carrerasElegiblesIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (carrerasNormalizadas.Length == 0)
        {
            throw new ArgumentException("Debe especificar al menos una carrera elegible válida.", nameof(carrerasElegiblesIds));
        }

        PromedioMinimoIngreso = promedio;
        EdadMinima = edadMinima;
        EdadMaxima = edadMaxima;
        TopeIngresoFamiliarMensual = tope;
        UniversidadesElegiblesIds = universidadesNormalizadas;
        CarrerasElegiblesIds = carrerasNormalizadas;
    }

    public decimal PromedioMinimoIngreso { get; }

    public int EdadMinima { get; }

    public int EdadMaxima { get; }

    public decimal TopeIngresoFamiliarMensual { get; }

    public IReadOnlyList<Guid> UniversidadesElegiblesIds { get; }

    public IReadOnlyList<Guid> CarrerasElegiblesIds { get; }
}
