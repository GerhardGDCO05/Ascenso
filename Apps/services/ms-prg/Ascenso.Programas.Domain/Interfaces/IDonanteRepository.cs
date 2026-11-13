using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Donantes.Entidades;
using Ascenso.Programas.Domain.Enums;

namespace Ascenso.Programas.Domain.Interfaces;

/// <summary>
/// Puerto de persistencia para el agregado <see cref="Donante"/> y su historial de aportes.
/// </summary>
public interface IDonanteRepository
{
    Task<Donante?> ObtenerPorIdAsync(Guid donanteId, CancellationToken cancellationToken = default);

    Task<bool> ExisteAsync(Guid donanteId, CancellationToken cancellationToken = default);

    Task<bool> ExisteIdentificacionFiscalAsync(string identificacionFiscal, CancellationToken cancellationToken = default);

    Task<bool> ExisteReferenciaBancariaAsync(string numeroReferenciaBancaria, CancellationToken cancellationToken = default);

    Task<bool> TieneFondosComprometidosEnProgramaVigenteAsync(Guid donanteId, CancellationToken cancellationToken = default);

    Task AgregarAsync(Donante donante, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Donante> Items, int TotalCount)> ConsultarPaginadoAsync(
        int pagina,
        int tamanoPagina,
        TipoDonante? tipoDonante,
        EstadoDonante? estadoDonante,
        string? busqueda,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AporteDonante>> ConsultarHistorialAportesAsync(
        Guid donanteId,
        string? periodoAcademicoId,
        CancellationToken cancellationToken = default);
}

