using Ascenso.Postulaciones.Domain.Rubricas.Agregados;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto segregado para consultar la <see cref="RubricaEvaluacion"/> vigente de un programa
/// y convocatoria durante el registro y envío de evaluaciones.
/// </summary>
public interface IConsultaRubricaParaEvaluacionRepository
{
    Task<RubricaEvaluacion?> ObtenerPorProgramaYConvocatoriaAsync(
        Guid programaBecaId,
        Guid convocatoriaId,
        CancellationToken cancellationToken = default);
}
