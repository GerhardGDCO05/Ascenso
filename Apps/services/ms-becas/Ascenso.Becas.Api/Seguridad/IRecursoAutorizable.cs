namespace Ascenso.Becas.Api.Seguridad;

/// <summary>
/// Identificadores de negocio que las políticas de autorización basadas en
/// recurso necesitan evaluar. Los controladores deben pasar una instancia de
/// este contrato como recurso a <c>IAuthorizationService.AuthorizeAsync</c>.
/// </summary>
public interface IRecursoAutorizable
{
    Guid? ProgramaId { get; }

    Guid? PostulanteId { get; }
}

/// <summary>
/// Recurso mínimo para evaluar alcance de programa y propiedad del postulante.
/// </summary>
public sealed record RecursoAutorizable(
    Guid? ProgramaId = null,
    Guid? PostulanteId = null) : IRecursoAutorizable;
