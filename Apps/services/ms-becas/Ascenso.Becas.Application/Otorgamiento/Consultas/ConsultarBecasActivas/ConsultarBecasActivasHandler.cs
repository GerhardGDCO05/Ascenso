using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;

namespace Ascenso.Becas.Application.Otorgamiento.Consultas.ConsultarBecasActivas;

/// <summary>
/// Devuelve solo las becas activas del estudiante en el periodo, sin datos
/// personales ni bancarios.
/// </summary>
public sealed class ConsultarBecasActivasHandler
    : IQueryHandler<ConsultarBecasActivasQuery, IReadOnlyList<BecaActivaDto>>
{
    private readonly IConsultasBeca _consultas;

    public ConsultarBecasActivasHandler(IConsultasBeca consultas)
    {
        _consultas = consultas ?? throw new ArgumentNullException(nameof(consultas));
    }

    public Task<IReadOnlyList<BecaActivaDto>> HandleAsync(
        ConsultarBecasActivasQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _consultas.ConsultarActivasAsync(
            query.PostulanteEstudianteId,
            query.PeriodoAcademicoId,
            cancellationToken);
    }
}
