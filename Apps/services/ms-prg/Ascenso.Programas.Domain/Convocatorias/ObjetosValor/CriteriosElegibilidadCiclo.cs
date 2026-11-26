using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Excepciones;

namespace Ascenso.Programas.Domain.Convocatorias.ObjetosValor;

/// <summary>
/// Objeto de valor inmutable con los criterios de elegibilidad propios de un ciclo de convocatoria.
/// </summary>
public sealed record CriteriosElegibilidadCiclo : ValueObject
{
    private CriteriosElegibilidadCiclo()
    {
        UniversidadesPermitidasIds = Array.Empty<Guid>();
        CarrerasPermitidasIds = Array.Empty<Guid>();
    }

    public CriteriosElegibilidadCiclo(
        decimal promedioMinimo,
        int edadMinima,
        int edadMaxima,
        decimal topeIngresoFamiliar,
        IReadOnlyCollection<Guid> universidadesPermitidasIds,
        IReadOnlyCollection<Guid> carrerasPermitidasIds)
    {
        ArgumentNullException.ThrowIfNull(universidadesPermitidasIds);
        ArgumentNullException.ThrowIfNull(carrerasPermitidasIds);

        if (promedioMinimo is < 0.00m or > 20.00m)
        {
            throw new CriteriosConvocatoriaInvalidosException(
                $"el promedio mínimo debe estar entre 0.00 y 20.00 (valor recibido: {promedioMinimo:0.00}).");
        }

        var promedio = decimal.Round(promedioMinimo, 2, MidpointRounding.AwayFromZero);

        if (edadMinima is < 0 or > 120)
        {
            throw new CriteriosConvocatoriaInvalidosException(
                $"la edad mínima debe estar entre 0 y 120 (valor recibido: {edadMinima}).");
        }

        if (edadMaxima is < 0 or > 120)
        {
            throw new CriteriosConvocatoriaInvalidosException(
                $"la edad máxima debe estar entre 0 y 120 (valor recibido: {edadMaxima}).");
        }

        if (edadMaxima < edadMinima)
        {
            throw new CriteriosConvocatoriaInvalidosException(
                "la edad máxima debe ser mayor o igual a la edad mínima.");
        }

        if (topeIngresoFamiliar < 0.00m)
        {
            throw new CriteriosConvocatoriaInvalidosException(
                $"el tope de ingreso familiar no puede ser negativo (valor recibido: {topeIngresoFamiliar:0.00}).");
        }

        var tope = decimal.Round(topeIngresoFamiliar, 2, MidpointRounding.AwayFromZero);

        var universidadesNormalizadas = universidadesPermitidasIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (universidadesNormalizadas.Length == 0)
        {
            throw new CriteriosConvocatoriaInvalidosException(
                "debe indicar al menos una universidad permitida.");
        }

        var carrerasNormalizadas = carrerasPermitidasIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (carrerasNormalizadas.Length == 0)
        {
            throw new CriteriosConvocatoriaInvalidosException(
                "debe indicar al menos una carrera permitida.");
        }

        PromedioMinimo = promedio;
        EdadMinima = edadMinima;
        EdadMaxima = edadMaxima;
        TopeIngresoFamiliar = tope;
        UniversidadesPermitidasIds = Array.AsReadOnly(universidadesNormalizadas);
        CarrerasPermitidasIds = Array.AsReadOnly(carrerasNormalizadas);
    }

    public decimal PromedioMinimo { get; }

    public int EdadMinima { get; }

    public int EdadMaxima { get; }

    public decimal TopeIngresoFamiliar { get; }

    public IReadOnlyList<Guid> UniversidadesPermitidasIds { get; }

    public IReadOnlyList<Guid> CarrerasPermitidasIds { get; }
}
