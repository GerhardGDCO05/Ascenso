using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Repositorio de becas en memoria para los casos de uso de aplicación.
/// </summary>
public sealed class BecaRepositoryFalso : IBecaRepository
{
    private readonly HashSet<Guid> _agregadasPendientes = [];

    public List<Beca> Becas { get; } = [];

    public void Confirmar() => _agregadasPendientes.Clear();

    public void Revertir()
    {
        Becas.RemoveAll(beca => _agregadasPendientes.Contains(beca.Id));
        _agregadasPendientes.Clear();
    }

    public List<Guid> EstudiantesBloqueados { get; } = [];

    public Exception? ErrorAgregar { get; set; }

    public Task<Beca?> ObtenerPorIdAsync(
        Guid becaId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Becas.SingleOrDefault(beca => beca.Id == becaId));

    public Task<Beca?> ObtenerPorOfertaAsync(
        Guid ofertaId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Becas.SingleOrDefault(
            beca => beca.Oferta is not null && beca.Oferta.Id == ofertaId));

    public Task AgregarAsync(Beca beca, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(beca);

        if (ErrorAgregar is not null)
        {
            throw ErrorAgregar;
        }

        Becas.Add(beca);
        _agregadasPendientes.Add(beca.Id);
        return Task.CompletedTask;
    }

    public Task<Beca?> ObtenerBecaParaActualizarAsync(
        Guid becaId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Becas.SingleOrDefault(beca => beca.Id == becaId));

    public Task AdquirirBloqueoEstudianteAsync(
        Guid estudianteId,
        CancellationToken cancellationToken = default)
    {
        EstudiantesBloqueados.Add(estudianteId);
        return Task.CompletedTask;
    }

    public Task<bool> ExisteAsync(Guid becaId, CancellationToken cancellationToken = default)
        => Task.FromResult(Becas.Exists(beca => beca.Id == becaId));

    public Task<bool> ExisteActivaConCoberturaTotalAsync(
        Guid estudianteId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Becas.Exists(beca =>
            beca.PostulanteEstudianteId == estudianteId
            && beca.Estado == EstadoBeca.Activa
            && beca.CoberturaAplicada.Modalidad == TipoCobertura.Total));

    public Task<bool> ExisteActivaEnPeriodoAsync(
        Guid estudianteId,
        Guid periodoAcademicoId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Becas.Exists(beca =>
            beca.PostulanteEstudianteId == estudianteId
            && beca.PeriodoVigenteActualId == periodoAcademicoId
            && beca.Estado == EstadoBeca.Activa));

    public Task<IReadOnlyList<Beca>> ObtenerActivasPorEstudianteAsync(
        Guid estudianteId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Beca>>(Becas
            .Where(beca => beca.PostulanteEstudianteId == estudianteId
                && beca.Estado == EstadoBeca.Activa)
            .ToArray());
}
