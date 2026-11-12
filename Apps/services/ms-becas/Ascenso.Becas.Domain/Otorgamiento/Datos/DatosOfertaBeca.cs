using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Domain.Otorgamiento.Datos;

/// <summary>
/// Datos de negocio capturados al emitir la oferta de una beca.
/// </summary>
public sealed record class DatosOfertaBeca(
    Guid PostulacionId,
    Guid ReservaPresupuestoId,
    decimal MontoOfertaPorPeriodo,
    decimal MontoTotalEstimadoDuracion,
    int DuracionPeriodos,
    CondicionesPermanencia CondicionesSnapshot,
    string UrlCartaPdfFirebase,
    string HashCartaSha256,
    string ReferenciaJobExpiracionHangfireId,
    Guid? AutorizacionOtpEmisionId = null);
