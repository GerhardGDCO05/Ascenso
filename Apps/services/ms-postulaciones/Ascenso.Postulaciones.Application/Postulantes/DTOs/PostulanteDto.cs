namespace Ascenso.Postulaciones.Application.Postulantes.DTOs;

public sealed record class PostulanteDto(
    Guid Id,
    Guid? PersonaId,
    Guid UsuarioId,
    string NumeroDocumentoIdentidad,
    string Nombres,
    string Apellidos,
    string NombreCompleto,
    DatosContactoDto DatosContacto,
    Guid UniversidadAliadaId,
    Guid CarreraId,
    Guid PeriodoAcademicoId,
    string CondicionAcademica,
    string EstadoPerfil,
    bool AceptaTratamientoDatos,
    DateTimeOffset FechaRegistro);
