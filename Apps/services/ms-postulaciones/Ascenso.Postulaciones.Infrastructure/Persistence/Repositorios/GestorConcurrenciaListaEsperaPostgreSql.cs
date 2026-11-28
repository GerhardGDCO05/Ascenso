using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Adaptador PostgreSQL de control de concurrencia atómico (C6) sobre la lista de espera
/// de una convocatoria mediante bloqueo transaccional <c>pg_advisory_xact_lock</c>
/// y lectura bloqueante <c>SELECT ... FOR UPDATE</c> en orden determinista.
/// </summary>
public sealed class GestorConcurrenciaListaEsperaPostgreSql : IGestorConcurrenciaListaEspera
{
    private readonly PostulacionesDbContext _contexto;

    public GestorConcurrenciaListaEsperaPostgreSql(PostulacionesDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public async Task<TResult> EjecutarConBloqueoConvocatoriaAsync<TResult>(
        Guid convocatoriaId,
        Func<IReadOnlyList<EntradaListaEspera>, CancellationToken, Task<TResult>> accionBloqueada,
        CancellationToken cancellationToken = default)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la convocatoria es obligatorio.",
                nameof(convocatoriaId));
        }

        ArgumentNullException.ThrowIfNull(accionBloqueada);

        var transaccionExistente = _contexto.Database.CurrentTransaction;
        IDbContextTransaction? transaccionPropia = null;

        if (transaccionExistente is null)
        {
            transaccionPropia = await _contexto.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var convocatoriaIdTexto = convocatoriaId.ToString("D");

            await _contexto.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({convocatoriaIdTexto}, 1002))",
                cancellationToken);

            var entradasBloqueadas = await ConsultarEntradasBloqueadas(convocatoriaId)
                .ToListAsync(cancellationToken);

            var resultado = await accionBloqueada(entradasBloqueadas, cancellationToken);

            if (transaccionPropia is not null)
            {
                await transaccionPropia.CommitAsync(cancellationToken);
            }

            return resultado;
        }
        finally
        {
            if (transaccionPropia is not null)
            {
                await transaccionPropia.DisposeAsync();
            }
        }
    }

    internal IQueryable<EntradaListaEspera> ConsultarEntradasBloqueadas(Guid convocatoriaId)
        => _contexto.EntradasListaEspera.FromSqlInterpolated(
            $"SELECT * FROM postulaciones.entradas_lista_espera WHERE convocatoria_id = {convocatoriaId} ORDER BY posicion_orden ASC FOR UPDATE");
}
