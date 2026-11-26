using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Ascenso.Postulaciones.Infrastructure.Adapters;

/// <summary>
/// Adaptador que resuelve los parámetros de consolidación desde <see cref="ConsolidacionOptions"/>.
/// </summary>
public sealed class ProveedorParametrosConsolidacionConfigurado : IProveedorParametrosConsolidacion
{
    private readonly IOptions<ConsolidacionOptions> _opciones;

    public ProveedorParametrosConsolidacionConfigurado(IOptions<ConsolidacionOptions> opciones)
    {
        _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
    }

    public Task<ParametrosConsolidacion> ObtenerParametrosAsync(
        Guid convocatoriaId,
        Guid rubricaEvaluacionId,
        CancellationToken cancellationToken = default)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la convocatoria es obligatorio.",
                nameof(convocatoriaId));
        }

        if (rubricaEvaluacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la rúbrica de evaluación es obligatorio.",
                nameof(rubricaEvaluacionId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var valor = _opciones.Value ?? new ConsolidacionOptions();
        var parametros = new ParametrosConsolidacion(
            valor.EvaluadoresMinimosRequeridos,
            valor.UmbralDiscrepanciaPuntos);

        return Task.FromResult(parametros);
    }
}
