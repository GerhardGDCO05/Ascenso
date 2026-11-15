namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando no existe el perfil de postulante solicitado.
/// </summary>
public sealed class PostulanteNoEncontradoException : ExcepcionEntidadNoEncontrada
{
    public new const string CodigoError = "POSTULANTE_NO_ENCONTRADO";

    public PostulanteNoEncontradoException(string? mensaje = null)
        : base(
            string.IsNullOrWhiteSpace(mensaje)
                ? "No se encontró el perfil de postulante solicitado."
                : mensaje,
            CodigoError)
    {
    }
}
