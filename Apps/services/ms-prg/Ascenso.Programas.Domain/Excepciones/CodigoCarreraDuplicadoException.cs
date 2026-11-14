namespace Ascenso.Programas.Domain.Excepciones;

public sealed class CodigoCarreraDuplicadoException : DomainException
{
    public const string CodigoError = "CODIGO_CARRERA_DUPLICADO";

    public CodigoCarreraDuplicadoException(Guid universidadId, string codigoCarrera)
        : base(
            CodigoError,
            409,
            $"La universidad aliada '{universidadId}' ya tiene registrada una carrera elegible con código '{codigoCarrera}'.")
    {
        UniversidadId = universidadId;
        CodigoCarrera = codigoCarrera;
    }

    public Guid UniversidadId { get; }

    public string CodigoCarrera { get; }
}
