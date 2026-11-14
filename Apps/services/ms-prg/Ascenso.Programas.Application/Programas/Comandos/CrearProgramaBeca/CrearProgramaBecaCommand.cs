using Ascenso.Programas.Application.Programas.DTOs;

namespace Ascenso.Programas.Application.Programas.Comandos.CrearProgramaBeca;

public sealed record CrearProgramaBecaCommand(
    string Codigo,
    string Nombre,
    string Descripcion,
    PoliticaCoberturaDto PoliticaCobertura,
    int CupoTotal,
    int DuracionPeriodos,
    string PeriodoInicialId,
    DateOnly VigenciaDesde,
    DateOnly VigenciaHasta,
    bool PermiteAcumulacion,
    bool RequiereCompromisoFirmado,
    string? TerminosCompromisoEspecial,
    Guid UsuarioCreacionId);
