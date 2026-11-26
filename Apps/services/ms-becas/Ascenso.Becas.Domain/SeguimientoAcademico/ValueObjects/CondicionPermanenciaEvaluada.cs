using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Domain.SeguimientoAcademico.ValueObjects;

public sealed record class CondicionPermanenciaEvaluada : ValueObject
{
    private CondicionPermanenciaEvaluada()
    {
        ValorRequerido = string.Empty;
        ValorObtenido = string.Empty;
    }

    public CondicionPermanenciaEvaluada(TipoCondicionPermanencia condicion,
        string valorRequerido, string valorObtenido, bool cumple)
    {
        if (!Enum.IsDefined(condicion) || string.IsNullOrWhiteSpace(valorRequerido)
            || string.IsNullOrWhiteSpace(valorObtenido))
            throw new ArgumentException("La condición y sus valores son obligatorios.");
        Condicion = condicion;
        ValorRequerido = valorRequerido;
        ValorObtenido = valorObtenido;
        Cumple = cumple;
    }

    public TipoCondicionPermanencia Condicion { get; }
    public string ValorRequerido { get; }
    public string ValorObtenido { get; }
    public bool Cumple { get; }
}
