using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Modelo de lectura simulado para las consultas de becas.
/// </summary>
public sealed class ConsultasBecaFalsas : IConsultasBeca
{
    public OfertaBecaDto? Oferta { get; set; }

    public RecursoOfertaDto? RecursoOferta { get; set; }

    public RecursoBecaDto? RecursoBeca { get; set; }

    public RecursoCompromisoDto? RecursoCompromiso { get; set; }

    public BecaDto? Beca { get; set; }

    public IReadOnlyList<BecaActivaDto> Activas { get; set; } = [];

    public Guid? OfertaSolicitada { get; private set; }

    public Guid? RecursoOfertaSolicitado { get; private set; }

    public Guid? RecursoBecaSolicitado { get; private set; }

    public Guid? RecursoCompromisoSolicitado { get; private set; }

    public Guid? BecaSolicitada { get; private set; }

    public Guid? EstudianteSolicitado { get; private set; }

    public Guid? PeriodoSolicitado { get; private set; }

    public Task<OfertaBecaDto?> ObtenerOfertaAsync(
        Guid ofertaId,
        CancellationToken cancellationToken = default)
    {
        OfertaSolicitada = ofertaId;
        return Task.FromResult(Oferta);
    }

    public Task<RecursoOfertaDto?> ObtenerRecursoOfertaAsync(
        Guid ofertaId,
        CancellationToken cancellationToken = default)
    {
        RecursoOfertaSolicitado = ofertaId;
        return Task.FromResult(RecursoOferta);
    }

    public Task<RecursoBecaDto?> ObtenerRecursoBecaAsync(
        Guid becaId,
        CancellationToken cancellationToken = default)
    {
        RecursoBecaSolicitado = becaId;
        return Task.FromResult(RecursoBeca);
    }

    public Task<RecursoCompromisoDto?> ObtenerRecursoCompromisoAsync(
        Guid compromisoId,
        CancellationToken cancellationToken = default)
    {
        RecursoCompromisoSolicitado = compromisoId;
        return Task.FromResult(RecursoCompromiso);
    }

    public Task<BecaDto?> ObtenerBecaAsync(
        Guid becaId,
        CancellationToken cancellationToken = default)
    {
        BecaSolicitada = becaId;
        return Task.FromResult(Beca);
    }

    public Task<IReadOnlyList<BecaActivaDto>> ConsultarActivasAsync(
        Guid postulanteEstudianteId,
        Guid periodoAcademicoId,
        CancellationToken cancellationToken = default)
    {
        EstudianteSolicitado = postulanteEstudianteId;
        PeriodoSolicitado = periodoAcademicoId;
        return Task.FromResult(Activas);
    }
}
