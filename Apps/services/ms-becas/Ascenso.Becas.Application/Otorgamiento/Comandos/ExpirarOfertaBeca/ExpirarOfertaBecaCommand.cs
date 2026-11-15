using Ascenso.Becas.Application.Despacho;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.ExpirarOfertaBeca;

/// <summary>
/// Expira una oferta vencida. Es reutilizable por el job de Hangfire y por la
/// reconciliación periódica; devuelve <c>true</c> solo si cambió el estado.
/// </summary>
public sealed record ExpirarOfertaBecaCommand(Guid OfertaId) : ICommand<bool>;
