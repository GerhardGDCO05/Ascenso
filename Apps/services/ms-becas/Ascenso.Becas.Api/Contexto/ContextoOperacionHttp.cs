using System.Security.Claims;
using Ascenso.Becas.Api.Correlacion;
using Ascenso.Becas.Api.Seguridad;
using Ascenso.Becas.Application.Comun;

namespace Ascenso.Becas.Api.Contexto;

/// <summary>
/// Implementación HTTP de <see cref="IContextoOperacionActual"/>. El usuario
/// se toma del claim <c>sub</c>; fuera de una petición autenticada el contexto
/// queda sin usuario para que los jobs usen la identidad del sistema.
/// </summary>
public sealed class ContextoOperacionHttp : IContextoOperacionActual
{
    public const string EncabezadoAutorizacionOtp = "X-Autorizacion-OTP-Id";

    private readonly IHttpContextAccessor _accesor;

    public ContextoOperacionHttp(IHttpContextAccessor accesor)
    {
        _accesor = accesor ?? throw new ArgumentNullException(nameof(accesor));
    }

    public Guid? UsuarioId
    {
        get
        {
            var usuario = _accesor.HttpContext?.User;

            if (usuario?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var valor = LectorClaims.PrimerValor(
                usuario,
                "sub",
                ClaimTypes.NameIdentifier);

            return Guid.TryParse(valor, out var identificador)
                && identificador != Guid.Empty
                ? identificador
                : null;
        }
    }

    public string CorrelationId
    {
        get
        {
            var contexto = _accesor.HttpContext;

            if (contexto is not null)
            {
                return CorrelacionHttp.Obtener(contexto);
            }

            return System.Diagnostics.Activity.Current?.TraceId.ToString()
                ?? Guid.NewGuid().ToString("N");
        }
    }

    public string? DireccionIp
        => _accesor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public Guid? AutorizacionOtpId
    {
        get
        {
            var valor = _accesor.HttpContext?.Request.Headers[EncabezadoAutorizacionOtp].ToString();
            return Guid.TryParse(valor, out var id) && id != Guid.Empty ? id : null;
        }
    }
}
