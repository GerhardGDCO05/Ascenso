namespace Ascenso.Becas.Domain.Comun;

/// <summary>
/// Generador de identificadores UUIDv7 basado en el soporte nativo de .NET 10.
/// </summary>
public static class UuidV7
{
    public static Guid Nuevo()
        => Guid.CreateVersion7();

    public static Guid Nuevo(DateTimeOffset marcaTiempo)
        => Guid.CreateVersion7(marcaTiempo.ToUniversalTime());

    public static bool EsVersion7(Guid valor)
        => valor.Version == 7;
}
