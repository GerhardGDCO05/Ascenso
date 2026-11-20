using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Domain.Postulantes.Entidades;

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
            postulante.EstadoCivil is { } estadoCivil ? estadoCivil.ACodigo() : null,
            postulante.PromedioActual,
            postulante.Trabaja,
            postulante.Ocupacion is { } ocupacion ? ocupacion.ACodigo() : null,
            postulante.IngresoMensualPostulante,
            postulante.NumeroIntegrantesFamilia,
            postulante.IngresoFamiliarMensualTotal,
            postulante.MiembrosFamiliares.Select(miembro => miembro.ADto()).ToArray(),
            postulante.EstadoPerfil.ACodigo(),
            postulante.AceptaTratamientoDatos,
            postulante.FechaRegistro);

    public static MiembroFamiliarDto ADto(this MiembroFamiliar miembro)
        => new(
            miembro.Id,
            miembro.Parentesco.ACodigo(),
            miembro.Edad,
            miembro.Ocupacion.ACodigo(),
            miembro.IngresoMensual);

    public static bool IntentarConvertirCondicionAcademica(
        string? valor,
        out CondicionAcademicaIngreso condicionAcademica)
        => IntentarConvertir(valor, out condicionAcademica);

    public static bool IntentarConvertirEstadoCivil(
        string? valor,
        out EstadoCivil estadoCivil)
        => IntentarConvertir(valor, out estadoCivil);

    public static bool IntentarConvertirParentesco(
        string? valor,
        out Parentesco parentesco)
        => IntentarConvertir(valor, out parentesco);

    public static bool IntentarConvertirOcupacion(
        string? valor,
        out Ocupacion ocupacion)
        => IntentarConvertir(valor, out ocupacion);

    private static bool IntentarConvertir<TEnum>(
        string? valor,
        out TEnum valorConvertido)
        where TEnum : struct, Enum
    {
        valorConvertido = default;

        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        try
        {
            valorConvertido = CodigoEnum.DesdeCodigo<TEnum>(valor);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
