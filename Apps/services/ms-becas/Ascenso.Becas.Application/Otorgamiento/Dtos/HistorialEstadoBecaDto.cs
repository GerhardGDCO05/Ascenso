using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Application.Otorgamiento.Dtos;

/// <summary>
/// Fila inmutable del historial de estados del agregado de beca.
/// </summary>
public sealed record HistorialEstadoBecaDto(
    Guid HistorialId,
    string TipoEntidad,
    string? EstadoBecaAnterior,
    string? EstadoBecaNuevo,
    string? EstadoOfertaAnterior,
    string? EstadoOfertaNuevo,
    string? EstadoCompromisoAnterior,
    string? EstadoCompromisoNuevo,
    DateTimeOffset FechaCambio,
    Guid UsuarioAutorId,
    string Motivo,
    string? JustificacionDetallada,
    bool EstadoTerminalIrreversible)
{
    public static HistorialEstadoBecaDto Desde(
        Guid historialId,
        TipoEntidadHistorial tipoEntidad,
        EstadoBeca? estadoBecaAnterior,
        EstadoBeca? estadoBecaNuevo,
        EstadoOferta? estadoOfertaAnterior,
        EstadoOferta? estadoOfertaNuevo,
        EstadoCompromiso? estadoCompromisoAnterior,
        EstadoCompromiso? estadoCompromisoNuevo,
        DateTimeOffset fechaCambio,
        Guid usuarioAutorId,
        string motivo,
        string? justificacionDetallada,
        bool estadoTerminalIrreversible)
        => new(
            historialId,
            tipoEntidad.ACodigo(),
            estadoBecaAnterior?.ACodigo(),
            estadoBecaNuevo?.ACodigo(),
            estadoOfertaAnterior?.ACodigo(),
            estadoOfertaNuevo?.ACodigo(),
            estadoCompromisoAnterior?.ACodigo(),
            estadoCompromisoNuevo?.ACodigo(),
            fechaCambio,
            usuarioAutorId,
            motivo,
            justificacionDetallada,
            estadoTerminalIrreversible);
}
