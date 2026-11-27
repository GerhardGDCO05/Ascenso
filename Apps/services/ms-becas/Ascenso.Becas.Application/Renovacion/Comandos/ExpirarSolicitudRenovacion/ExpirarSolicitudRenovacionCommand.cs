using Ascenso.Becas.Application.Despacho;

namespace Ascenso.Becas.Application.Renovacion.Comandos.ExpirarSolicitudRenovacion;

public sealed record ExpirarSolicitudRenovacionCommand(Guid SolicitudId) : ICommand<bool>;
