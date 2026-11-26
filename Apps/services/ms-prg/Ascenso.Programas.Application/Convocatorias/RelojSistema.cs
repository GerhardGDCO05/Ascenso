using Ascenso.Programas.Application.Convocatorias.Abstracciones;

namespace Ascenso.Programas.Application.Convocatorias;

/// <summary>
/// Implementación por defecto del reloj de convocatorias.
/// </summary>
public sealed class RelojSistema : IRelojConvocatorias
{
    public DateTimeOffset Ahora => DateTimeOffset.UtcNow;
}
