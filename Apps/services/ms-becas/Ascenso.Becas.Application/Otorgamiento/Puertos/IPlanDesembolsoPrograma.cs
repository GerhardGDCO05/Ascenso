using Ascenso.Becas.Domain.Desembolsos.Datos;
using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

/// <summary>Plan oficial de cuotas del periodo inicial de una beca.</summary>
public interface IPlanDesembolsoPrograma
{
    Task<PlanDesembolsoPrograma?> ObtenerAsync(Guid programaBecaId,
        Guid periodoAcademicoId, CancellationToken cancellationToken = default);
}

public sealed record PlanDesembolsoPrograma(Guid ProgramaBecaId,
    Guid PeriodoAcademicoId, FrecuenciaDesembolso Frecuencia,
    DestinatarioPago Destinatario, DateOnly FechaInicio,
    DateOnly FechaFin, IReadOnlyList<CuotaProgramada> Cuotas);
