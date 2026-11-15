using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Programas.Agregados;
using Ascenso.Programas.Domain.Programas.Entidades;

namespace Ascenso.Programas.Domain.Interfaces;

/// <summary>
/// Puerto de persistencia para el agregado <see cref="ProgramaBeca"/>, sus asignaciones presupuestarias,
/// reservas atómicas concurrentes y libro mayor de movimientos.
/// </summary>
public interface IProgramaBecaRepository
{
    Task<ProgramaBeca?> ObtenerPorIdAsync(Guid programaId, CancellationToken cancellationToken = default);

    Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default);

    Task AgregarAsync(ProgramaBeca programa, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ProgramaBeca> Items, int TotalCount)> ConsultarPaginadoAsync(
        int pagina,
        int tamanoPagina,
        EstadoProgramaBeca? estado,
        string? periodoInicialId,
        ModalidadCobertura? modalidadCobertura,
        string? busqueda,
        CancellationToken cancellationToken = default);

    Task<(ProgramaBeca Programa, ReservaPresupuestaria Reserva, MovimientoPresupuestario Movimiento, bool YaExistia)> ReservarPresupuestoAtomicoAsync(
        Guid programaId,
        Guid referenciaExternaId,
        string periodoAcademicoId,
        decimal monto,
        string descripcion,
        Guid usuarioAutorId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<MovimientoPresupuestario> Items, int TotalCount)> ConsultarMovimientosPaginadosAsync(
        Guid programaId,
        int pagina,
        int tamanoPagina,
        TipoMovimientoPresupuestario? tipoMovimiento,
        string? periodoAcademicoId,
        Guid? referenciaExternaId,
        CancellationToken cancellationToken = default);
}
