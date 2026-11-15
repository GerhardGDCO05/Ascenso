using Ascenso.Postulaciones.Domain.Excepciones;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Postulaciones.Infrastructure.Persistence;

/// <summary>
/// Traduce violaciones de restricciones únicas de PostgreSQL a excepciones de dominio.
/// </summary>
internal static class TraductorExcepcionesPersistencia
{
    public const string IndiceDocumentoUnico = "ux_postulante_numero_documento_identidad";
    public const string IndiceUsuarioUnico = "ux_postulante_usuario_id";

    public static Exception? TraducirSiAplica(DbUpdateException excepcion)
    {
        ArgumentNullException.ThrowIfNull(excepcion);

        if (excepcion.InnerException is not PostgresException pg
            || !string.Equals(pg.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal))
        {
            return null;
        }

        if (string.Equals(pg.ConstraintName, IndiceDocumentoUnico, StringComparison.OrdinalIgnoreCase))
        {
            return new IdentidadDuplicadaException();
        }

        if (string.Equals(pg.ConstraintName, IndiceUsuarioUnico, StringComparison.OrdinalIgnoreCase))
        {
            return new PerfilYaRegistradoException();
        }

        return new ExcepcionConflictoConcurrencia(
            "Ya existe un registro con los mismos datos únicos.",
            ExcepcionConflictoConcurrencia.CodigoRestriccionUnicidad);
    }
}
