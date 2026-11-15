using Ascenso.Postulaciones.Application.Postulantes.DTOs;

namespace Ascenso.Postulaciones.Application.Postulantes.Comandos.RegistrarPostulante;

public sealed record class RegistrarPostulanteCommand(
    string NumeroDocumentoIdentidad,
    string Nombres,
    string Apellidos,
    DatosContactoDto DatosContacto,
    Guid UniversidadAliadaId,
    Guid CarreraId,
    Guid PeriodoAcademicoId,
    string CondicionAcademica,
    bool AceptaTratamientoDatos);
