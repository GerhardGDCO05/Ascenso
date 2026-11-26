using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Tests.Dobles;

public sealed class PeriodosAcademicosClientFalso : IPeriodosAcademicosClient
{
    public ResultadoPeriodoAcademico Resultado { get; set; } = null!;

    public Exception? Error { get; set; }

    public int Llamadas { get; private set; }

    public Task<ResultadoPeriodoAcademico> ObtenerActualAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Llamadas++;

        if (Error is not null)
        {
            throw Error;
        }

        return Task.FromResult(
            Resultado ?? throw new InvalidOperationException(
                "La prueba no configuró el periodo académico actual."));
    }
}
