using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;

namespace Ascenso.Programas.Domain.Programas.ObjetosValor;

/// <summary>
/// Objeto de valor inmutable que encapsula las condiciones académicas y de servicio de permanencia del becario.
/// </summary>
public sealed record CondicionesPermanencia
{
    private CondicionesPermanencia()
    {
    }

    public CondicionesPermanencia(
        decimal promedioMinimoRenovacion,
        int materiasMinimasAprobadas,
        int maximoMateriasReprobadas,
        int horasServicioPorPeriodo,
        FrecuenciaReporteAcademico frecuenciaReportes,
        ConsecuenciaIncumplimiento consecuenciaIncumplimiento,
        decimal margenRiesgoPromedio,
        int duracionMaximaPausaPeriodos,
        bool permiteRenovacionTardiaExcepcional)
    {
        var promedio = decimal.Round(promedioMinimoRenovacion, 2, MidpointRounding.AwayFromZero);
        if (promedio is < 0.00m or > 20.00m)
        {
            throw new PromedioAcademicoInvalidoException(nameof(promedioMinimoRenovacion), promedio);
        }

        if (materiasMinimasAprobadas < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(materiasMinimasAprobadas), "Las materias mínimas aprobadas no pueden ser negativas.");
        }

        if (maximoMateriasReprobadas < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximoMateriasReprobadas), "El máximo de materias reprobadas no puede ser negativo.");
        }

        if (horasServicioPorPeriodo < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(horasServicioPorPeriodo), "Las horas de servicio por período no pueden ser negativas.");
        }

        if (!Enum.IsDefined(frecuenciaReportes))
        {
            throw new ArgumentOutOfRangeException(nameof(frecuenciaReportes), "La frecuencia de reportes académicos no es válida.");
        }

        if (!Enum.IsDefined(consecuenciaIncumplimiento))
        {
            throw new ArgumentOutOfRangeException(nameof(consecuenciaIncumplimiento), "La consecuencia por incumplimiento no es válida.");
        }

        var margen = decimal.Round(margenRiesgoPromedio, 2, MidpointRounding.AwayFromZero);
        if (margen is < 0.00m or > 20.00m)
        {
            throw new ArgumentOutOfRangeException(nameof(margenRiesgoPromedio), "El margen de riesgo de promedio debe estar entre 0.00 y 20.00.");
        }

        if (duracionMaximaPausaPeriodos < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(duracionMaximaPausaPeriodos), "La duración máxima de pausa no puede ser negativa.");
        }

        PromedioMinimoRenovacion = promedio;
        MateriasMinimasAprobadas = materiasMinimasAprobadas;
        MaximoMateriasReprobadas = maximoMateriasReprobadas;
        HorasServicioPorPeriodo = horasServicioPorPeriodo;
        FrecuenciaReportes = frecuenciaReportes;
        ConsecuenciaIncumplimiento = consecuenciaIncumplimiento;
        MargenRiesgoPromedio = margen;
        DuracionMaximaPausaPeriodos = duracionMaximaPausaPeriodos;
        PermiteRenovacionTardiaExcepcional = permiteRenovacionTardiaExcepcional;
    }

    public decimal PromedioMinimoRenovacion { get; }

    public int MateriasMinimasAprobadas { get; }

    public int MaximoMateriasReprobadas { get; }

    public int HorasServicioPorPeriodo { get; }

    public FrecuenciaReporteAcademico FrecuenciaReportes { get; }

    public ConsecuenciaIncumplimiento ConsecuenciaIncumplimiento { get; }

    public decimal MargenRiesgoPromedio { get; }

    public int DuracionMaximaPausaPeriodos { get; }

    public bool PermiteRenovacionTardiaExcepcional { get; }
}
