namespace Ascenso.Becas.Domain.Desembolsos.Datos;

public sealed record CuotaProgramada(int NumeroCuota, DateOnly FechaProgramada, decimal Monto);
