using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerMisEvaluaciones;

public sealed record class ObtenerMisEvaluacionesQuery(
    EstadoEvaluacion? Estado = null,
    int Pagina = 1,
    int Tamano = ObtenerMisEvaluacionesQuery.TamanoPorDefecto)
{
    public const int TamanoMinimo = 1;
    public const int TamanoMaximo = 100;
    public const int TamanoPorDefecto = 20;
}
