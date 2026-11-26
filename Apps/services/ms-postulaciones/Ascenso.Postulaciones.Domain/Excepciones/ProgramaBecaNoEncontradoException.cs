namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando el programa de beca referenciado por la convocatoria no existe en MS-PRG.
/// </summary>
public sealed class ProgramaBecaNoEncontradoException : ExcepcionEntidadNoEncontrada
{
    public new const string CodigoError = "PROGRAMA_NO_ENCONTRADO";

    public ProgramaBecaNoEncontradoException(string? mensaje = null)
        : base(
            string.IsNullOrWhiteSpace(mensaje)
                ? "No se encontró el programa de beca asociado a la convocatoria."
                : mensaje,
            CodigoError)
    {
    }
}
