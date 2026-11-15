using Ascenso.Becas.Application.Correlacion;

namespace Ascenso.Becas.Api.Correlacion;

/// <summary>
/// Implementación HTTP de <see cref="ICorrelacionContexto"/>; fuera de una
/// petición cae al identificador de la traza o genera uno nuevo.
/// </summary>
public sealed class CorrelacionContexto : ICorrelacionContexto
{
    private readonly IHttpContextAccessor _accesorHttp;

    public CorrelacionContexto(IHttpContextAccessor accesorHttp)
    {
        _accesorHttp = accesorHttp ?? throw new ArgumentNullException(nameof(accesorHttp));
    }

    public string CorrelationId
    {
        get
        {
            var contexto = _accesorHttp.HttpContext;
            if (contexto is not null)
            {
                return CorrelacionHttp.Obtener(contexto);
            }

            return System.Diagnostics.Activity.Current?.TraceId.ToString()
                ?? Guid.NewGuid().ToString("N");
        }
    }
}
