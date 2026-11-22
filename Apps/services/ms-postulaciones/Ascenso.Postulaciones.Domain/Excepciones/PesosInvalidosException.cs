using System.Globalization;

namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta publicar una rúbrica cuya suma de pesos no es exactamente 100.00.
/// </summary>
public sealed class PesosInvalidosException : ExcepcionReglaNegocio
{
    public const string CodigoError = "PESOS_INVALIDOS";
    private const decimal SumaObjetivo = 100.00m;

    public PesosInvalidosException(decimal sumaPesos)
        : this(sumaPesos, SumaObjetivo - sumaPesos)
    {
    }

    public PesosInvalidosException(decimal sumaPesos, decimal diferenciaPara100)
        : base(CodigoError, ConstruirMensaje(sumaPesos, diferenciaPara100), 422)
    {
        SumaPesos = sumaPesos;
        DiferenciaPara100 = diferenciaPara100;
    }

    public decimal SumaPesos { get; }

    public decimal DiferenciaPara100 { get; }

    private static string ConstruirMensaje(decimal sumaPesos, decimal diferenciaPara100)
    {
        if (diferenciaPara100 < 0m)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"La suma de pesos es {sumaPesos:0.00}; excede por {Math.Abs(diferenciaPara100):0.00} a 100.00.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"La suma de pesos es {sumaPesos:0.00}; faltan {diferenciaPara100:0.00} para 100.00.");
    }
}
