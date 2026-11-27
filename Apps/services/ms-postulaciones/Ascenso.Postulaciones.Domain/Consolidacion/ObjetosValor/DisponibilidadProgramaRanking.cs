using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;

/// <summary>
/// Objeto de valor inmutable que encapsula la disponibilidad efectiva de cupos y presupuesto
/// de una convocatoria y su programa de beca descontando el monto comprometido en renovaciones pendientes.
/// </summary>
public sealed record class DisponibilidadProgramaRanking : ValueObject
{
    public const int LongitudMaximaMoneda = 10;

    public DisponibilidadProgramaRanking(
        Guid convocatoriaId,
        Guid programaBecaId,
        int cuposAsignadosConvocatoria,
        int cuposDisponiblesPrograma,
        decimal presupuestoDisponibleBruto,
        decimal presupuestoComprometidoRenovaciones,
        string moneda)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new RankingInvalidoException("El identificador de la convocatoria es obligatorio.");
        }

        if (programaBecaId == Guid.Empty)
        {
            throw new RankingInvalidoException("El identificador del programa de beca es obligatorio.");
        }

        if (cuposAsignadosConvocatoria < 0)
        {
            throw new RankingInvalidoException("Los cupos asignados a la convocatoria no pueden ser negativos.");
        }

        if (cuposDisponiblesPrograma < 0)
        {
            throw new RankingInvalidoException("Los cupos disponibles del programa no pueden ser negativos.");
        }

        ValidarMontoNoNegativo(presupuestoDisponibleBruto, "El presupuesto disponible bruto");
        ValidarMontoNoNegativo(presupuestoComprometidoRenovaciones, "El presupuesto comprometido por renovaciones");

        if (string.IsNullOrWhiteSpace(moneda))
        {
            throw new RankingInvalidoException("La moneda del presupuesto es obligatoria.");
        }

        var monedaNormalizada = moneda.Trim().ToUpperInvariant();
        if (monedaNormalizada.Length > LongitudMaximaMoneda)
        {
            throw new RankingInvalidoException(
                $"La moneda del presupuesto no puede superar los {LongitudMaximaMoneda} caracteres.");
        }

        ConvocatoriaId = convocatoriaId;
        ProgramaBecaId = programaBecaId;
        CuposAsignadosConvocatoria = cuposAsignadosConvocatoria;
        CuposDisponiblesPrograma = cuposDisponiblesPrograma;
        CuposDisponiblesEfectivos = Math.Min(cuposAsignadosConvocatoria, cuposDisponiblesPrograma);
        PresupuestoDisponibleBruto = presupuestoDisponibleBruto;
        PresupuestoComprometidoRenovaciones = presupuestoComprometidoRenovaciones;
        PresupuestoDisponibleEfectivo = Math.Max(
            0.00m,
            decimal.Round(
                presupuestoDisponibleBruto - presupuestoComprometidoRenovaciones,
                2,
                MidpointRounding.AwayFromZero));
        Moneda = monedaNormalizada;
    }

    public Guid ConvocatoriaId { get; }

    public Guid ProgramaBecaId { get; }

    public int CuposAsignadosConvocatoria { get; }

    public int CuposDisponiblesPrograma { get; }

    public int CuposDisponiblesEfectivos { get; }

    public decimal PresupuestoDisponibleBruto { get; }

    public decimal PresupuestoComprometidoRenovaciones { get; }

    public decimal PresupuestoDisponibleEfectivo { get; }

    public string Moneda { get; }

    private static void ValidarMontoNoNegativo(decimal monto, string nombreCampo)
    {
        if (monto < 0.00m)
        {
            throw new RankingInvalidoException($"{nombreCampo} no puede ser negativo.");
        }

        if (decimal.Round(monto, 2) != monto)
        {
            throw new RankingInvalidoException($"{nombreCampo} no puede tener más de dos decimales.");
        }
    }
}
