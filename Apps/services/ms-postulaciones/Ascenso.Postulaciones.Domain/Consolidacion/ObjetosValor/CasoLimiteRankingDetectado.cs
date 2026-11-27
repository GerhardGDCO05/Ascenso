using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;

/// <summary>
/// Objeto de valor inmutable que describe un empate en frontera o un caso límite presupuestario activo detectado al calcular el corte de ranking.
/// </summary>
public sealed record class CasoLimiteRankingDetectado : ValueObject
{
    public const int LongitudMinimaJustificacion = 10;

    public const int LongitudMaximaJustificacion = 1000;

    public CasoLimiteRankingDetectado(
        TipoCasoLimite tipo,
        decimal? puntajeAfectado,
        int cuposDisponibles,
        decimal presupuestoDisponible,
        IEnumerable<Guid> postulacionesAfectadas)
    {
        ArgumentNullException.ThrowIfNull(postulacionesAfectadas);

        if (!Enum.IsDefined(tipo))
        {
            throw new CasoLimiteInvalidoException("El tipo de caso límite indicado no es válido.");
        }

        if (tipo is TipoCasoLimite.EmpatePorCupo or TipoCasoLimite.EmpatePorPresupuesto)
        {
            if (!puntajeAfectado.HasValue
                || puntajeAfectado.Value < ResultadoCalculoConsolidacion.PuntajeMinimo
                || puntajeAfectado.Value > ResultadoCalculoConsolidacion.PuntajeMaximo
                || decimal.Round(puntajeAfectado.Value, 2) != puntajeAfectado.Value)
            {
                throw new CasoLimiteInvalidoException(
                    "Un empate en la línea de corte requiere un puntaje afectado entre 0.00 y 100.00 con máximo dos decimales.");
            }
        }
        else if (puntajeAfectado.HasValue)
        {
            throw new CasoLimiteInvalidoException(
                "Un caso límite presupuestario sin empate no debe especificar un puntaje afectado.");
        }

        if (cuposDisponibles < 1)
        {
            throw new CasoLimiteInvalidoException(
                "Los cupos disponibles para resolver un caso límite deben ser al menos 1.");
        }

        if (presupuestoDisponible <= 0.00m
            || decimal.Round(presupuestoDisponible, 2) != presupuestoDisponible)
        {
            throw new CasoLimiteInvalidoException(
                "El presupuesto disponible para resolver un caso límite debe ser mayor a 0.00 con máximo dos decimales.");
        }

        var copiaPostulaciones = new List<Guid>();
        var vistas = new HashSet<Guid>();

        foreach (var postulacionId in postulacionesAfectadas)
        {
            if (postulacionId == Guid.Empty)
            {
                throw new CasoLimiteInvalidoException(
                    "Las postulaciones afectadas por el caso límite no pueden contener identificadores vacíos.");
            }

            if (!vistas.Add(postulacionId))
            {
                throw new CasoLimiteInvalidoException(
                    "Las postulaciones afectadas por el caso límite no pueden contener duplicados.");
            }

            copiaPostulaciones.Add(postulacionId);
        }

        var minimoPostulaciones = tipo == TipoCasoLimite.CasoLimitePresupuestario ? 1 : 2;
        if (copiaPostulaciones.Count < minimoPostulaciones)
        {
            throw new CasoLimiteInvalidoException(
                minimoPostulaciones == 2
                    ? "Un empate en la línea de corte debe involucrar al menos dos postulaciones."
                    : "Un caso límite presupuestario debe involucrar al menos una postulación elegible.");
        }

        Tipo = tipo;
        PuntajeAfectado = puntajeAfectado;
        CuposDisponibles = cuposDisponibles;
        PresupuestoDisponible = presupuestoDisponible;
        PostulacionesAfectadas = Array.AsReadOnly(copiaPostulaciones.ToArray());
    }

    public TipoCasoLimite Tipo { get; }

    public decimal? PuntajeAfectado { get; }

    public int CuposDisponibles { get; }

    public decimal PresupuestoDisponible { get; }

    public IReadOnlyList<Guid> PostulacionesAfectadas { get; }

    public bool Equals(CasoLimiteRankingDetectado? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }

        return Tipo == other.Tipo
            && PuntajeAfectado == other.PuntajeAfectado
            && CuposDisponibles == other.CuposDisponibles
            && PresupuestoDisponible == other.PresupuestoDisponible
            && PostulacionesAfectadas.SequenceEqual(other.PostulacionesAfectadas);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Tipo);
        hash.Add(PuntajeAfectado);
        hash.Add(CuposDisponibles);
        hash.Add(PresupuestoDisponible);
        for (var i = 0; i < PostulacionesAfectadas.Count; i++)
        {
            hash.Add(PostulacionesAfectadas[i]);
        }

        return hash.ToHashCode();
    }
}
