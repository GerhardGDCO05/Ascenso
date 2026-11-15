using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Repositorio de compromisos en memoria para los casos de uso de aplicación.
/// </summary>
public sealed class CompromisoBecarioRepositoryFalso : ICompromisoBecarioRepository
{
    public List<CompromisoBecario> Compromisos { get; } = [];

    public Task<CompromisoBecario?> ObtenerPorIdAsync(
        Guid compromisoId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Compromisos.SingleOrDefault(
            compromiso => compromiso.Id == compromisoId));

    public Task<CompromisoBecario?> ObtenerPorBecaAsync(
        Guid becaId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Compromisos.SingleOrDefault(
            compromiso => compromiso.BecaId == becaId));

    public Task AgregarAsync(
        CompromisoBecario compromiso,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compromiso);
        Compromisos.Add(compromiso);
        return Task.CompletedTask;
    }

    public Task<bool> ExisteAsync(
        Guid compromisoId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Compromisos.Exists(
            compromiso => compromiso.Id == compromisoId));

    public Task<bool> ExisteAprobadoParaBecaAsync(
        Guid becaId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Compromisos.Exists(compromiso =>
            compromiso.BecaId == becaId
            && compromiso.Estado == EstadoCompromiso.Aprobado));
}
