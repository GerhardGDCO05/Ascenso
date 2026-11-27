using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.Transiciones;

namespace Ascenso.Becas.Application.Otorgamiento.Servicios;

/// <summary>Exige la liberación financiera antes del cierre local de una oferta.</summary>
internal static class LiberacionReservaOferta
{
    public static async Task ConfirmarAsync(Beca beca, Guid ofertaId,
        EstadoOferta destino, IProgramasClient programas,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(beca);
        ArgumentNullException.ThrowIfNull(programas);
        if (beca.Oferta is null || beca.Oferta.Id != ofertaId)
            throw new ConflictoOperacionException(
                "La oferta no pertenece a la beca que se va a cerrar.");
        if (destino is not (EstadoOferta.RechazadaPorPostulante
            or EstadoOferta.AnuladaPorCoordinacion or EstadoOferta.Expirada))
            throw new ArgumentOutOfRangeException(nameof(destino));

        TransicionesBeca.Tabla.Validar(beca.Estado, EstadoBeca.Revocada);
        TransicionesOfertaBeca.Tabla.Validar(beca.Oferta.Estado, destino);
        try
        {
            await programas.LiberarReservaAsync(beca.ProgramaBecaId,
                beca.Oferta.ReservaPresupuestoId, ofertaId, cancellationToken);
        }
        catch (Exception excepcion) when (!cancellationToken.IsCancellationRequested
            && excepcion is HttpRequestException or TimeoutException or TaskCanceledException)
        {
            throw new DependenciaNoDisponibleException("MS-PRG", excepcion);
        }
    }
}
