using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Correlacion;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Otorgamiento;

/// <summary>
/// Construye el contexto de dominio a partir del contexto de ejecución y del
/// reloj del servicio.
/// </summary>
internal static class ContextoAplicacion
{
    public static ContextoOperacion Crear(
        IContextoOperacionActual contexto,
        IClock reloj,
        string? justificacion = null,
        bool exigirUsuario = true,
        Guid? autorizacionOtpId = null)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(reloj);

        var usuarioId = contexto.UsuarioId ?? (exigirUsuario ? null : IdentidadSistema.UsuarioId);

        if (usuarioId is null || usuarioId == Guid.Empty)
        {
            throw new OperacionNoPermitidaException(
                "La operación requiere un usuario autenticado con identificador válido.");
        }

        return new ContextoOperacion(
            usuarioId.Value,
            CorrelacionOperacion.ADomain(contexto.CorrelationId),
            reloj.UtcNow,
            justificacion,
            autorizacionOtpId ?? contexto.AutorizacionOtpId);
    }
}
