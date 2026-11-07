using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Domain.ValueObjects;

/// <summary>
/// Condiciones académicas y de servicio que debe mantener el becario
/// para conservar o renovar la beca.
/// </summary>
public sealed record class CondicionesPermanencia : ValueObject
{
    public CondicionesPermanencia(
        decimal promedioMinimoRenovacion,
        int materiasMinimasAprobadas,
        int maximoMateriasReprobadas,
        int horasServicioPorPeriodo,
        ConsecuenciaPermanencia consecuenciaIncumplimiento)
    {
        if (promedioMinimoRenovacion is < 0 or > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(promedioMinimoRenovacion),
                "El promedio mínimo de renovación debe estar entre 0 y 20.");
        }

        if (decimal.Round(promedioMinimoRenovacion, 2) != promedioMinimoRenovacion)
        {
            throw new ArgumentException(
                "El promedio mínimo de renovación admite como máximo dos decimales.",
                nameof(promedioMinimoRenovacion));
        }

        if (materiasMinimasAprobadas < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(materiasMinimasAprobadas),
                "Las materias mínimas aprobadas no pueden ser negativas.");
        }

        if (maximoMateriasReprobadas < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximoMateriasReprobadas),
                "El máximo de materias reprobadas no puede ser negativo.");
        }

        if (horasServicioPorPeriodo < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(horasServicioPorPeriodo),
                "Las horas de servicio por periodo no pueden ser negativas.");
        }

        if (!Enum.IsDefined(consecuenciaIncumplimiento))
        {
            throw new ArgumentOutOfRangeException(
                nameof(consecuenciaIncumplimiento),
                "La consecuencia por incumplimiento no es válida.");
        }

        PromedioMinimoRenovacion = promedioMinimoRenovacion;
        MateriasMinimasAprobadas = materiasMinimasAprobadas;
        MaximoMateriasReprobadas = maximoMateriasReprobadas;
        HorasServicioPorPeriodo = horasServicioPorPeriodo;
        ConsecuenciaIncumplimiento = consecuenciaIncumplimiento;
    }

    public decimal PromedioMinimoRenovacion { get; }

    public int MateriasMinimasAprobadas { get; }

    public int MaximoMateriasReprobadas { get; }

    public int HorasServicioPorPeriodo { get; }

    public ConsecuenciaPermanencia ConsecuenciaIncumplimiento { get; }
}
