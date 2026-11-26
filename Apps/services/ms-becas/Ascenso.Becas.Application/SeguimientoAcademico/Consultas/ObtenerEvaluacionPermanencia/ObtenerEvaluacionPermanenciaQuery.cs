using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.SeguimientoAcademico.Dtos;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Consultas.ObtenerEvaluacionPermanencia;

public sealed record ObtenerEvaluacionPermanenciaQuery(Guid EvaluacionPermanenciaId)
    : IQuery<EvaluacionPermanenciaDto>;
