namespace Ascenso.Programas.Application.Convocatorias.Comandos.CerrarConvocatoriasVencidas;

public sealed record CerrarConvocatoriasVencidasCommand(
    Guid UsuarioSistemaId)
{
    public static Guid UsuarioSistemaPorDefecto { get; } =
        Guid.Parse("01966000-0000-7000-8000-000000000001");
}
