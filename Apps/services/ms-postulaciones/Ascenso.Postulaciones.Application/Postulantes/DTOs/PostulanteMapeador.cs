using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;

namespace Ascenso.Postulaciones.Application.Postulantes.DTOs;

internal static class PostulanteMapeador
{
    public static PostulanteDto ADto(this Postulante postulante)
        => new(
            postulante.Id,
            postulante.PersonaId,
            postulante.UsuarioId,
            postulante.NumeroDocumentoIdentidad,
            postulante.Nombres,
            postulante.Apellidos,
            postulante.NombreCompleto,
            new DatosContactoDto(
                postulante.DatosContacto.CorreoElectronico,
                postulante.DatosContacto.Telefono),
            postulante.UniversidadAliadaId,
            postulante.CarreraId,
            postulante.PeriodoActualId,
            postulante.CondicionAcademica.ACodigo(),
            postulante.EstadoPerfil.ACodigo(),
            postulante.AceptaTratamientoDatos,
            postulante.FechaRegistro);

    public static bool IntentarConvertirCondicionAcademica(
        string? valor,
        out CondicionAcademicaIngreso condicionAcademica)
    {
        condicionAcademica = default;

        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        try
        {
            condicionAcademica = CodigoEnum.DesdeCodigo<CondicionAcademicaIngreso>(valor);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
