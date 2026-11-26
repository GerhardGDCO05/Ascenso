using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerMisAsignaciones;

public sealed record class ObtenerMisAsignacionesQuery(
    EstadoAsignacionEvaluacion? Estado = null,
    int Pagina = 1,
    int Tamano = ObtenerMisAsignacionesQuery.TamanoPorDefecto)
{
    public const int TamanoMinimo = 1;
    public const int TamanoMaximo = 100;
    public const int TamanoPorDefecto = 20;
}
