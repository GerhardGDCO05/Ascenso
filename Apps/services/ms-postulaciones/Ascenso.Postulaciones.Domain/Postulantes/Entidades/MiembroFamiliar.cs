using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;

namespace Ascenso.Postulaciones.Domain.Postulantes.Entidades;

/// <summary>
/// Miembro del grupo familiar que forma parte del agregado del postulante.
/// </summary>
public sealed class MiembroFamiliar : EntidadAuditable
{
    // Constructor requerido por los adaptadores de persistencia.
    private MiembroFamiliar()
    {
    }

    private MiembroFamiliar(
        Guid postulanteId,
        DatosMiembroFamiliar datos,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
        : base(UuidV7.Nuevo(), fechaCreacion, usuarioCreacionId)
    {
        PostulanteId = postulanteId;
        Parentesco = datos.Parentesco;
        Edad = datos.Edad;
        Ocupacion = datos.Ocupacion;
        IngresoMensual = datos.IngresoMensual;
    }

    public Guid PostulanteId { get; private set; }

    public Parentesco Parentesco { get; private set; }

    public int Edad { get; private set; }

    public Ocupacion Ocupacion { get; private set; }

    public decimal IngresoMensual { get; private set; }

    public static MiembroFamiliar Crear(
        Guid postulanteId,
        DatosMiembroFamiliar datos,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
    {
        if (postulanteId == Guid.Empty)
        {
            throw new MiembroFamiliarInvalidoException(
                "El miembro familiar debe estar asociado a un postulante.");
        }

        if (datos is null)
        {
            throw new MiembroFamiliarInvalidoException(
                "Los datos del miembro familiar son obligatorios.");
        }

        return new MiembroFamiliar(postulanteId, datos, fechaCreacion, usuarioCreacionId);
    }
}
