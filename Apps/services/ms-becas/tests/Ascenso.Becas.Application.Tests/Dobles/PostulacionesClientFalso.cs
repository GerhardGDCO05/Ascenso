using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Cliente de postulaciones simulado. Permite devolver un resultado o lanzar
/// un error de transporte.
/// </summary>
public sealed class PostulacionesClientFalso : IPostulacionesClient
{
    public ResultadoAdjudicabilidad? Resultado { get; set; }

    public Exception? Error { get; set; }

    public Guid? PostulacionSolicitada { get; private set; }

    public int Llamadas { get; private set; }

    public Task<ResultadoAdjudicabilidad> ObtenerAdjudicabilidadAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Llamadas++;
        PostulacionSolicitada = postulacionId;

        if (Error is not null)
        {
            throw Error;
        }

        return Task.FromResult(
            Resultado ?? throw new InvalidOperationException("La prueba no configuró el resultado de adjudicabilidad."));
    }
}
