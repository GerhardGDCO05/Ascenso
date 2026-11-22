using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class TraductorExcepcionesPersistenciaTests
{
    private static DbUpdateException CrearDbUpdateUnicidad(string constraintName)
    {
        var pgException = new PostgresException(
            messageText: "duplicate key value violates unique constraint",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.UniqueViolation,
            constraintName: constraintName);

        return new DbUpdateException("Error al guardar cambios.", pgException);
    }

    [Fact]
    public void Debe_TraducirAIdentidadDuplicada_Cuando_FallaIndiceDeDocumento()
    {
        var excepcion = CrearDbUpdateUnicidad(TraductorExcepcionesPersistencia.IndiceDocumentoUnico);

        var traducida = TraductorExcepcionesPersistencia.TraducirSiAplica(excepcion);

        traducida.Should().BeOfType<IdentidadDuplicadaException>();
    }

    [Fact]
    public void Debe_TraducirAPerfilYaRegistrado_Cuando_FallaIndiceDeUsuario()
    {
        var excepcion = CrearDbUpdateUnicidad(TraductorExcepcionesPersistencia.IndiceUsuarioUnico);

        var traducida = TraductorExcepcionesPersistencia.TraducirSiAplica(excepcion);

        traducida.Should().BeOfType<PerfilYaRegistradoException>();
    }

    [Fact]
    public void Debe_TraducirARubricaDuplicada_Cuando_FallaIndiceDeRubricaPorProgramaYConvocatoria()
    {
        var excepcion = CrearDbUpdateUnicidad(TraductorExcepcionesPersistencia.IndiceRubricaProgramaConvocatoriaUnico);

        var traducida = TraductorExcepcionesPersistencia.TraducirSiAplica(excepcion);

        var duplicada = traducida.Should().BeOfType<RubricaDuplicadaException>().Subject;
        duplicada.Codigo.Should().Be(RubricaDuplicadaException.CodigoError);
        duplicada.EstadoHttp.Should().Be(409);
    }

    [Fact]
    public void Debe_TraducirAConflictoConCodigoRestriccionUnicidad_Cuando_FallaIndiceDeOrdenDeCriterio()
    {
        var excepcion = CrearDbUpdateUnicidad(TraductorExcepcionesPersistencia.IndiceCriterioRubricaOrdenUnico);

        var traducida = TraductorExcepcionesPersistencia.TraducirSiAplica(excepcion);

        var conflicto = traducida.Should().BeOfType<ExcepcionConflictoConcurrencia>().Subject;
        conflicto.Codigo.Should().Be(ExcepcionConflictoConcurrencia.CodigoRestriccionUnicidad);
        conflicto.EstadoHttp.Should().Be(409);
    }

    [Fact]
    public void Debe_TraducirAConflictoConCodigoRestriccionUnicidad_Cuando_FallaOtraRestriccionUnica()
    {
        var excepcion = CrearDbUpdateUnicidad("ux_otra_restriccion");

        var traducida = TraductorExcepcionesPersistencia.TraducirSiAplica(excepcion);

        var conflicto = traducida.Should().BeOfType<ExcepcionConflictoConcurrencia>().Subject;
        conflicto.Codigo.Should().Be(ExcepcionConflictoConcurrencia.CodigoRestriccionUnicidad);
        conflicto.EstadoHttp.Should().Be(409);
    }

    [Fact]
    public void Debe_RetornarNull_Cuando_LaExcepcionNoEsUnicidadPostgres()
    {
        var excepcion = new DbUpdateException("Error genérico", new InvalidOperationException("otro"));

        var traducida = TraductorExcepcionesPersistencia.TraducirSiAplica(excepcion);

        traducida.Should().BeNull();
    }
}
