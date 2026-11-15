namespace Ascenso.Becas.Application.Excepciones;

/// <summary>
/// Se produce cuando no hay una configuración vigente de programa, costo de
/// matrícula o convenio para emitir una oferta.
/// </summary>
public sealed class ConfiguracionProgramaNoDisponibleException : AplicacionException
{
    public const string CodigoError = "CONFIGURACION_PROGRAMA_NO_DISPONIBLE";

    public ConfiguracionProgramaNoDisponibleException(Guid programaBecaId, string detalle)
        : base(CodigoError, 422, detalle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detalle);
        ProgramaBecaId = programaBecaId;
    }

    public Guid ProgramaBecaId { get; }
}
