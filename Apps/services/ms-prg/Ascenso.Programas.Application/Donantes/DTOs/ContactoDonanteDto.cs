namespace Ascenso.Programas.Application.Donantes.DTOs;

public sealed record class ContactoDonanteDto(
    string NombreRepresentante,
    string Cargo,
    string EmailContacto,
    string TelefonoContacto);

