namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

/// <summary>
/// Configuración completa necesaria para emitir una oferta. Si falta cualquiera
/// de sus componentes la emisión debe rechazarse con
/// <c>CONFIGURACION_PROGRAMA_NO_DISPONIBLE</c>.
/// </summary>
public sealed record ProyeccionOtorgamiento(
    ProgramaOtorgamientoProyeccion Programa,
    CostoMatriculaProyeccion CostoMatricula,
    ConvenioProyeccion Convenio);
