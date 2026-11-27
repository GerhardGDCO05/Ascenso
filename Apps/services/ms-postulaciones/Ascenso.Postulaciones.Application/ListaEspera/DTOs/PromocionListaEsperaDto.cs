using Ascenso.Postulaciones.Application.Consolidacion.DTOs;

namespace Ascenso.Postulaciones.Application.ListaEspera.DTOs;

public sealed record class PromocionListaEsperaDto(
    EntradaListaEsperaDto Entrada,
    DisponibilidadRankingDto DisponibilidadVerificada,
    bool HabilitadaParaAdjudicacion);
