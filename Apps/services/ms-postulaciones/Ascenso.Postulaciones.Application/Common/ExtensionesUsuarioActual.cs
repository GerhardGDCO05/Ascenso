using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Application.Common;

internal static class ExtensionesUsuarioActual
{
    public static Guid ObtenerUsuarioIdRequerido(this IUsuarioActual usuarioActual)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);

        if (!usuarioActual.EstaAutenticado || usuarioActual.UsuarioId == Guid.Empty)
        {
            throw new NoAutenticadoException();
        }

        return usuarioActual.UsuarioId;
    }
}
