using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Renovacion.Dtos;

namespace Ascenso.Becas.Application.Renovacion.Consultas.ListarRenovacionesBeca;

public sealed record ListarRenovacionesBecaQuery(Guid BecaId, int Pagina,
    int Tamano) : IQuery<PaginaRenovacionesDto>;
