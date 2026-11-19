using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>
/// Activa los jobs dentro de un alcance de dependencias de la aplicación, de
/// modo que puedan usar repositorios, el despachador y el contexto scoped.
/// </summary>
public sealed class ActivadorHangfire : JobActivator
{
    private readonly IServiceScopeFactory _alcances;

    public ActivadorHangfire(IServiceScopeFactory alcances)
    {
        _alcances = alcances ?? throw new ArgumentNullException(nameof(alcances));
    }

    public override JobActivatorScope BeginScope(JobActivatorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new AlcanceHangfire(_alcances.CreateScope());
    }

    private sealed class AlcanceHangfire : JobActivatorScope
    {
        private readonly IServiceScope _alcance;

        public AlcanceHangfire(IServiceScope alcance)
        {
            _alcance = alcance ?? throw new ArgumentNullException(nameof(alcance));
        }

        public override object Resolve(Type type)
            => _alcance.ServiceProvider.GetRequiredService(type);

        public override void DisposeScope()
            => _alcance.Dispose();
    }
}
