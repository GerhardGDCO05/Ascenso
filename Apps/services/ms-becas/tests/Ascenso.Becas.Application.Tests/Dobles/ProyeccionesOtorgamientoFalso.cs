using Ascenso.Becas.Application.Otorgamiento.Puertos;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Proyecciones de otorgamiento simuladas.
/// </summary>
public sealed class ProyeccionesOtorgamientoFalso : IProyeccionesOtorgamiento
{
    public ProyeccionOtorgamiento? Proyeccion { get; set; }

    public int Llamadas { get; private set; }

    public Guid? ProgramaSolicitado { get; private set; }

    public Guid? CarreraSolicitada { get; private set; }

    public Guid? PeriodoSolicitado { get; private set; }

    public Guid? UniversidadSolicitada { get; private set; }

    public Task<ProyeccionOtorgamiento?> ObtenerAsync(
        Guid programaBecaId,
        Guid carreraId,
        Guid periodoAcademicoId,
        Guid universidadAliadaId,
        DateOnly fecha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Llamadas++;
        ProgramaSolicitado = programaBecaId;
        CarreraSolicitada = carreraId;
        PeriodoSolicitado = periodoAcademicoId;
        UniversidadSolicitada = universidadAliadaId;

        return Task.FromResult(Proyeccion);
    }
}
