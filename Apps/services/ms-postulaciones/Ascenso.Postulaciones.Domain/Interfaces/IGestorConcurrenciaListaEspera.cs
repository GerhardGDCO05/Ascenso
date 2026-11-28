using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de control de concurrencia atómico (C6) que serializa la lectura y mutación
/// de la lista de espera de una convocatoria específica.
/// </summary>
public interface IGestorConcurrenciaListaEspera
{
    Task<TResult> EjecutarConBloqueoConvocatoriaAsync<TResult>(
        Guid convocatoriaId,
        Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<TResult>> accionBloqueada,
        CancellationToken cancellationToken = default);
}
