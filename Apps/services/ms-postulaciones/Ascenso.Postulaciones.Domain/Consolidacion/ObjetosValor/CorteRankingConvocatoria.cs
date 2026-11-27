using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;

/// <summary>
/// Objeto de valor inmutable que resume el cálculo completo del ranking y trazado de línea de corte de una convocatoria.
/// </summary>
public sealed record class CorteRankingConvocatoria : ValueObject
{
    public CorteRankingConvocatoria(
        Guid convocatoriaId,
        DisponibilidadProgramaRanking disponibilidad,
        IReadOnlyList<ElementoRankingEvaluado> elementos,
        CasoLimiteRankingDetectado? casoLimiteActivo = null,
        bool tieneDecisionCasoLimiteInvalidaPorDisponibilidad = false)
    {
        ArgumentNullException.ThrowIfNull(disponibilidad);
        ArgumentNullException.ThrowIfNull(elementos);

        if (convocatoriaId == Guid.Empty)
        {
            throw new RankingInvalidoException("El identificador de la convocatoria es obligatorio.");
        }

        if (disponibilidad.ConvocatoriaId != convocatoriaId)
        {
            throw new RankingInvalidoException(
                "La disponibilidad financiera y de cupos no corresponde a la convocatoria indicada.");
        }

        var copiaElementos = new ElementoRankingEvaluado[elementos.Count];
        var postulacionesVistas = new HashSet<Guid>(elementos.Count);
        var empatadas = new List<Guid>();
        var conDiscrepanciaEnCorte = new List<Guid>();

        var totalDentroDeCupo = 0;
        var totalListaDeEspera = 0;
        var montoTotalDentroDeCupo = 0.00m;
        decimal? puntajeEmpate = null;
        var tieneCasoLimiteResuelto = false;

        for (var i = 0; i < elementos.Count; i++)
        {
            var elemento = elementos[i]
                ?? throw new RankingInvalidoException("La lista de elementos del ranking no puede contener nulos.");

            if (elemento.PosicionRanking != i + 1)
            {
                throw new RankingInvalidoException(
                    "Las posiciones de ranking deben ser estrictamente secuenciales iniciando en 1.");
            }

            if (!postulacionesVistas.Add(elemento.PostulacionId))
            {
                throw new RankingInvalidoException(
                    "No se permiten postulaciones duplicadas dentro del mismo corte de ranking.");
            }

            copiaElementos[i] = elemento;

            if (elemento.Clasificacion == ClasificacionRanking.DentroDeCupo)
            {
                totalDentroDeCupo++;
                montoTotalDentroDeCupo += elemento.MontoEstimadoBeca;
            }
            else
            {
                totalListaDeEspera++;
            }

            if (elemento.EmpatadoEnLineaDeCorte)
            {
                if (puntajeEmpate.HasValue && puntajeEmpate.Value != elemento.PuntajeConsolidado)
                {
                    throw new RankingInvalidoException(
                        "Todos los postulantes empatados en la línea de corte deben compartir el mismo puntaje consolidado.");
                }

                puntajeEmpate = elemento.PuntajeConsolidado;
                empatadas.Add(elemento.PostulacionId);
            }

            if (elemento.AfectaAdvertenciaDiscrepanciaEnCorte)
            {
                conDiscrepanciaEnCorte.Add(elemento.PostulacionId);
            }

            if (elemento.CasoLimiteResuelto)
            {
                tieneCasoLimiteResuelto = true;
            }
        }

        if (casoLimiteActivo is not null)
        {
            for (var i = 0; i < casoLimiteActivo.PostulacionesAfectadas.Count; i++)
            {
                if (!postulacionesVistas.Contains(casoLimiteActivo.PostulacionesAfectadas[i]))
                {
                    throw new RankingInvalidoException(
                        "Todas las postulaciones del caso límite activo deben pertenecer a los elementos del corte.");
                }
            }

            if (casoLimiteActivo.Tipo is TipoCasoLimite.EmpatePorCupo or TipoCasoLimite.EmpatePorPresupuesto)
            {
                if (empatadas.Count == 0
                    || puntajeEmpate != casoLimiteActivo.PuntajeAfectado
                    || !empatadas.SequenceEqual(casoLimiteActivo.PostulacionesAfectadas))
                {
                    throw new RankingInvalidoException(
                        "El caso límite activo por empate debe coincidir con las postulaciones empatadas en la línea de corte.");
                }
            }
            else if (empatadas.Count > 0)
            {
                throw new RankingInvalidoException(
                    "No puede existir un caso límite presupuestario sin empate cuando hay postulaciones empatadas en la línea de corte.");
            }
        }

        ConvocatoriaId = convocatoriaId;
        Disponibilidad = disponibilidad;
        Elementos = Array.AsReadOnly(copiaElementos);
        TotalPostulantes = copiaElementos.Length;
        TotalDentroDeCupo = totalDentroDeCupo;
        TotalListaDeEspera = totalListaDeEspera;
        MontoTotalDentroDeCupo = decimal.Round(montoTotalDentroDeCupo, 2, MidpointRounding.AwayFromZero);
        TieneEmpateEnLineaDeCorte = empatadas.Count > 0;
        PuntajeEmpateEnLineaDeCorte = puntajeEmpate;
        PostulacionesEmpatadasEnCorte = Array.AsReadOnly(empatadas.ToArray());
        TieneAdvertenciaDiscrepanciasEnCorte = conDiscrepanciaEnCorte.Count > 0;
        CantidadDiscrepanciasSinRevisarEnCorte = conDiscrepanciaEnCorte.Count;
        PostulacionesConDiscrepanciaSinRevisarEnCorte = Array.AsReadOnly(conDiscrepanciaEnCorte.ToArray());
        CasoLimiteActivo = casoLimiteActivo;
        TieneCasoLimiteActivo = casoLimiteActivo is not null;
        TieneCasoLimiteResuelto = tieneCasoLimiteResuelto;
        TieneDecisionCasoLimiteInvalidaPorDisponibilidad = tieneDecisionCasoLimiteInvalidaPorDisponibilidad;
    }

    public Guid ConvocatoriaId { get; }

    public DisponibilidadProgramaRanking Disponibilidad { get; }

    public IReadOnlyList<ElementoRankingEvaluado> Elementos { get; }

    public int TotalPostulantes { get; }

    public int TotalDentroDeCupo { get; }

    public int TotalListaDeEspera { get; }

    public decimal MontoTotalDentroDeCupo { get; }

    public bool TieneEmpateEnLineaDeCorte { get; }

    public decimal? PuntajeEmpateEnLineaDeCorte { get; }

    public IReadOnlyList<Guid> PostulacionesEmpatadasEnCorte { get; }

    public bool TieneAdvertenciaDiscrepanciasEnCorte { get; }

    public int CantidadDiscrepanciasSinRevisarEnCorte { get; }

    public IReadOnlyList<Guid> PostulacionesConDiscrepanciaSinRevisarEnCorte { get; }

    public CasoLimiteRankingDetectado? CasoLimiteActivo { get; }

    public bool TieneCasoLimiteActivo { get; }

    public bool TieneCasoLimiteResuelto { get; }

    public bool TieneDecisionCasoLimiteInvalidaPorDisponibilidad { get; }

    public bool Equals(CorteRankingConvocatoria? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }

        return ConvocatoriaId == other.ConvocatoriaId
            && Disponibilidad.Equals(other.Disponibilidad)
            && Elementos.SequenceEqual(other.Elementos)
            && EqualityComparer<CasoLimiteRankingDetectado?>.Default.Equals(CasoLimiteActivo, other.CasoLimiteActivo)
            && TieneDecisionCasoLimiteInvalidaPorDisponibilidad == other.TieneDecisionCasoLimiteInvalidaPorDisponibilidad;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ConvocatoriaId);
        hash.Add(Disponibilidad);
        for (var i = 0; i < Elementos.Count; i++)
        {
            hash.Add(Elementos[i]);
        }

        hash.Add(CasoLimiteActivo);
        hash.Add(TieneDecisionCasoLimiteInvalidaPorDisponibilidad);

        return hash.ToHashCode();
    }
}
