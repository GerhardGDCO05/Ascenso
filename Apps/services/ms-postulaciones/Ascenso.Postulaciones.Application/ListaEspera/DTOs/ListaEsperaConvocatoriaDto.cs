using Ascenso.Postulaciones.Application.Asignaciones.DTOs;

namespace Ascenso.Postulaciones.Application.ListaEspera.DTOs;

public sealed record class ListaEsperaConvocatoriaDto(
    Guid ConvocatoriaId,
    int TotalEnListaEspera,
    int TotalEnEspera,
    int TotalElegiblesPorVacante,
    int TotalPromovidas,
    PaginaDto<EntradaListaEsperaDto> Entradas);
