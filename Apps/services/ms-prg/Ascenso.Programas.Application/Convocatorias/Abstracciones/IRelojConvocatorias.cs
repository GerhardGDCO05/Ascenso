namespace Ascenso.Programas.Application.Convocatorias.Abstracciones;

/// <summary>
/// Reloj de aplicación para que las reglas temporales sean deterministas en pruebas.
/// </summary>
public interface IRelojConvocatorias
{
    DateTimeOffset Ahora { get; }
}
