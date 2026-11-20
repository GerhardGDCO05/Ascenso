using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;

/// <summary>
/// Datos inmutables de un miembro del grupo familiar declarados por el
/// postulante.
/// </summary>
public sealed record class DatosMiembroFamiliar : ValueObject
{
    public const int EdadMinima = 0;
    public const int EdadMaxima = 120;

    private const int DecimalesMaximos = 2;

    public DatosMiembroFamiliar(
        Parentesco parentesco,
        int edad,
        Ocupacion ocupacion,
        decimal ingresoMensual)
    {
        if (!Enum.IsDefined(parentesco))
        {
            throw new CatalogoInvalidoException(nameof(parentesco));
        }

        if (edad is < EdadMinima or > EdadMaxima)
        {
            throw new MiembroFamiliarInvalidoException(
                $"La edad del miembro familiar debe estar entre {EdadMinima} y {EdadMaxima} años.");
        }

        if (!Enum.IsDefined(ocupacion))
        {
            throw new CatalogoInvalidoException(nameof(ocupacion));
        }

        if (ingresoMensual < 0m)
        {
            throw new MiembroFamiliarInvalidoException(
                "El ingreso mensual del miembro familiar no puede ser negativo.");
        }

        if (decimal.Round(ingresoMensual, DecimalesMaximos) != ingresoMensual)
        {
            throw new MiembroFamiliarInvalidoException(
                $"El ingreso mensual del miembro familiar admite como máximo {DecimalesMaximos} decimales.");
        }

        Parentesco = parentesco;
        Edad = edad;
        Ocupacion = ocupacion;
        IngresoMensual = ingresoMensual;
    }

    public Parentesco Parentesco { get; }

    public int Edad { get; }

    public Ocupacion Ocupacion { get; }

    public decimal IngresoMensual { get; }
}
