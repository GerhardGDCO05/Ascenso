using Ascenso.Becas.Domain.Desembolsos.Datos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Application.Renovacion.Puertos;

/// <summary>Datos oficiales del nuevo periodo necesarios para activar la sucesora.</summary>
public interface IPlanNuevoPeriodoPrograma
{
    Task<PlanNuevoPeriodo?> ObtenerAsync(Guid programaBecaId,
        Guid periodoSolicitadoId, CancellationToken cancellationToken = default);
}

public sealed record PlanNuevoPeriodo(
    Guid ProgramaBecaId, Guid PeriodoAcademicoId,
    Guid CostoMatriculaReferenciaId, decimal MontoReferenciaMatricula,
    PoliticaCobertura Cobertura,
    CondicionesPermanencia Condiciones, bool RequiereCompromisoFirmado,
    bool RequiereNuevaFirma, string? UrlPlantillaCompromiso,
    FrecuenciaDesembolso Frecuencia, DestinatarioPago Destinatario,
    DateOnly FechaInicio, DateOnly FechaFin,
    IReadOnlyList<CuotaProgramada> Cuotas);
