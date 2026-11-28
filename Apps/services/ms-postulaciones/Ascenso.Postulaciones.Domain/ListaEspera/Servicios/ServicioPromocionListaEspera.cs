using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;

namespace Ascenso.Postulaciones.Domain.ListaEspera.Servicios;

/// <summary>
/// Servicio de dominio puro que valida el orden FIFO de la lista de espera
/// y la disponibilidad efectiva de cupos y presupuesto para promover candidatos.
/// </summary>
public static class ServicioPromocionListaEspera
{
    public static EntradaListaEspera SeleccionarSiguienteParaVacante(
        IReadOnlyList<EntradaListaEspera> entradasOrdenadas)
    {
        ArgumentNullException.ThrowIfNull(entradasOrdenadas);

        var siguiente = entradasOrdenadas
            .OrderBy(e => e.PosicionOrden)
            .ThenBy(e => e.Id)
            .FirstOrDefault(e => e.Estado == EstadoListaEspera.EnEspera);

        return siguiente ?? throw new ListaEsperaSinCandidatosException(
            "No existen candidatos en estado EN_ESPERA disponibles para marcar como elegibles por vacante.");
    }

    public static EntradaListaEspera ResolverCandidatoAPromover(
        IReadOnlyList<EntradaListaEspera> entradasOrdenadas,
        Guid? entradaListaEsperaId)
    {
        ArgumentNullException.ThrowIfNull(entradasOrdenadas);

        var ordenadas = entradasOrdenadas
            .OrderBy(e => e.PosicionOrden)
            .ThenBy(e => e.Id)
            .ToArray();

        var pendientes = ordenadas
            .Where(e => e.EstaPendiente)
            .ToArray();

        if (entradaListaEsperaId.HasValue)
        {
            var objetivo = ordenadas.FirstOrDefault(e => e.Id == entradaListaEsperaId.Value)
                ?? throw new EntradaListaEsperaNoEncontradaException();

            if (objetivo.EstaPromovida)
            {
                throw new EntradaListaEsperaYaPromovidaException();
            }

            if (pendientes.Length == 0 || pendientes[0].Id != objetivo.Id)
            {
                throw new OrdenListaEsperaVioladoException();
            }

            return objetivo;
        }

        if (pendientes.Length == 0)
        {
            throw new ListaEsperaSinCandidatosException();
        }

        return pendientes[0];
    }

    public static void ValidarDisponibilidadParaPromocion(
        DisponibilidadProgramaRanking disponibilidad,
        decimal montoEstimadoBeca)
    {
        ArgumentNullException.ThrowIfNull(disponibilidad);

        if (montoEstimadoBeca <= 0.00m || decimal.Round(montoEstimadoBeca, 2) != montoEstimadoBeca)
        {
            throw new ListaEsperaInvalidaException(
                "El monto estimado de beca para la promoción debe ser mayor a 0.00 con máximo dos decimales.");
        }

        if (disponibilidad.CuposDisponiblesEfectivos < 1)
        {
            throw new SinCupoDisponiblePromocionException();
        }

        if (disponibilidad.PresupuestoDisponibleEfectivo < montoEstimadoBeca)
        {
            throw new PresupuestoInsuficientePromocionException();
        }
    }
}
