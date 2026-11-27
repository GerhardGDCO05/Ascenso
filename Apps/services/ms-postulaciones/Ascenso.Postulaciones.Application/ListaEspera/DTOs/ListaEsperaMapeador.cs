using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;

namespace Ascenso.Postulaciones.Application.ListaEspera.DTOs;

public static class ListaEsperaMapeador
{
    public static EntradaListaEsperaDto ADto(this EntradaListaEspera entrada)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        return new EntradaListaEsperaDto(
            entrada.Id,
            entrada.ConvocatoriaId,
            entrada.ProgramaBecaId,
            entrada.PostulacionId,
            entrada.PosicionOrden,
            entrada.MontoEstimadoBecaSnapshot,
            entrada.Estado.ACodigo(),
            entrada.FechaIngreso,
            entrada.FechaElegibilidad,
            entrada.FechaPromocion,
            entrada.PromovidoPorUsuarioId,
            entrada.MotivoPromocion,
            entrada.Version);
    }

    public static ListaEsperaConvocatoriaDto AListaEsperaConvocatoriaDto(
        Guid convocatoriaId,
        IReadOnlyList<EntradaListaEspera> entradasConvocatoria,
        EstadoListaEspera? filtroEstado = null,
        int pagina = 1,
        int tamano = 20)
    {
        ArgumentNullException.ThrowIfNull(entradasConvocatoria);

        var ordenadas = entradasConvocatoria
            .OrderBy(e => e.PosicionOrden)
            .ThenBy(e => e.Id)
            .ToArray();

        var totalEnEspera = 0;
        var totalElegibles = 0;
        var totalPromovidas = 0;

        foreach (var entrada in ordenadas)
        {
            switch (entrada.Estado)
            {
                case EstadoListaEspera.EnEspera:
                    totalEnEspera++;
                    break;
                case EstadoListaEspera.ElegiblePorVacante:
                    totalElegibles++;
                    break;
                case EstadoListaEspera.Promovida:
                    totalPromovidas++;
                    break;
            }
        }

        var filtradas = filtroEstado.HasValue
            ? ordenadas.Where(e => e.Estado == filtroEstado.Value).ToArray()
            : ordenadas;

        var itemsPagina = filtradas
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(e => e.ADto())
            .ToArray();

        return new ListaEsperaConvocatoriaDto(
            convocatoriaId,
            TotalEnListaEspera: ordenadas.Length,
            TotalEnEspera: totalEnEspera,
            TotalElegiblesPorVacante: totalElegibles,
            TotalPromovidas: totalPromovidas,
            new PaginaDto<EntradaListaEsperaDto>(itemsPagina, pagina, tamano, filtradas.Length));
    }
}
