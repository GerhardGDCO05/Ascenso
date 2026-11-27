namespace Ascenso.Postulaciones.Application.ListaEspera.Comandos.PromoverCandidatoListaEspera;

public sealed record class PromoverCandidatoListaEsperaCommand(
    Guid ConvocatoriaId,
    Guid? EntradaListaEsperaId = null,
    string? MotivoPromocion = null);
