using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;

namespace Ascenso.Postulaciones.Domain.ListaEspera.Servicios;

/// <summary>
/// Servicio de dominio puro que construye la cola secuencial de lista de espera
/// a partir de los resultados consolidados confirmados de una convocatoria.
/// </summary>
public static class ServicioConstruccionListaEspera
{
    public static IReadOnlyList<EntradaListaEspera> ConstruirDesdeResultadosConfirmados(
        Guid convocatoriaId,
        Guid programaBecaId,
        IReadOnlyCollection<ResultadoConsolidadoRanking> resultadosConfirmados,
        DateTimeOffset fechaIngreso,
        Guid usuarioActorId)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ListaEsperaInvalidaException("El identificador de la convocatoria es obligatorio.");
        }

        if (programaBecaId == Guid.Empty)
        {
            throw new ListaEsperaInvalidaException("El identificador del programa de beca es obligatorio.");
        }

        ArgumentNullException.ThrowIfNull(resultadosConfirmados);

        var enListaEspera = new List<ResultadoConsolidadoRanking>();

        foreach (var resultado in resultadosConfirmados)
        {
            if (resultado is null)
            {
                throw new ListaEsperaInvalidaException("Los resultados consolidados no pueden contener elementos nulos.");
            }

            if (resultado.ConvocatoriaId != convocatoriaId)
            {
                throw new ListaEsperaInvalidaException(
                    "Todos los resultados consolidados deben pertenecer a la convocatoria indicada.");
            }

            if (resultado.EstadoConfirmacionRanking != EstadoRanking.Confirmado
                || !resultado.PosicionRanking.HasValue
                || !resultado.MontoEstimadoBeca.HasValue
                || !resultado.Clasificacion.HasValue)
            {
                throw new ListaEsperaInvalidaException(
                    "Solo pueden generar lista de espera los resultados en estado CONFIRMADO con posición, monto y clasificación.");
            }

            if (resultado.Clasificacion.Value == ClasificacionRanking.ListaDeEspera)
            {
                enListaEspera.Add(resultado);
            }
        }

        if (enListaEspera.Count == 0)
        {
            return [];
        }

        enListaEspera.Sort(static (izquierda, derecha) =>
        {
            var comparacionPosicion = izquierda.PosicionRanking!.Value.CompareTo(derecha.PosicionRanking!.Value);
            return comparacionPosicion != 0
                ? comparacionPosicion
                : izquierda.Id.CompareTo(derecha.Id);
        });

        var entradas = new List<EntradaListaEspera>(enListaEspera.Count);
        for (var indice = 0; indice < enListaEspera.Count; indice++)
        {
            var resultado = enListaEspera[indice];
            entradas.Add(EntradaListaEspera.Crear(
                convocatoriaId,
                programaBecaId,
                resultado.PostulacionId,
                posicionOrden: indice + 1,
                montoEstimadoBecaSnapshot: resultado.MontoEstimadoBeca!.Value,
                fechaIngreso: fechaIngreso,
                usuarioActorId: usuarioActorId));
        }

        return entradas;
    }
}
