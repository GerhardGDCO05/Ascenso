using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Dtos;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.CargarCompromisoBecario;

/// <summary>
/// Carga un compromiso firmado en PDF para una beca que lo exige.
/// </summary>
public sealed record CargarCompromisoBecarioCommand(
    Guid BecaId,
    string NombreArchivo,
    string TipoMime,
    byte[] Contenido)
    : ICommand<CompromisoCargadoDto>;
