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
    public void Debe_TraducirAComiteDuplicado_Cuando_FallaIndiceDeComitePorProgramaYConvocatoria()
    {
        var excepcion = CrearDbUpdateUnicidad(TraductorExcepcionesPersistencia.IndiceComiteProgramaConvocatoriaUnico);

        var traducida = TraductorExcepcionesPersistencia.TraducirSiAplica(excepcion);

        var duplicado = traducida.Should().BeOfType<ComiteDuplicadoException>().Subject;
        duplicado.Codigo.Should().Be(ComiteDuplicadoException.CodigoError);
        duplicado.EstadoHttp.Should().Be(409);
    }

    [Fact]
    public void Debe_TraducirAMiembroComiteDuplicado_Cuando_FallaIndiceDeMiembroActivo()
    {
        var excepcion = CrearDbUpdateUnicidad(TraductorExcepcionesPersistencia.IndiceMiembroComiteUsuarioActivoUnico);

        var traducida = TraductorExcepcionesPersistencia.TraducirSiAplica(excepcion);

        var duplicado = traducida.Should().BeOfType<MiembroComiteDuplicadoException>().Subject;
        duplicado.Codigo.Should().Be(MiembroComiteDuplicadoException.CodigoError);
        duplicado.EstadoHttp.Should().Be(409);
    }

    [Fact]
    public void Debe_TraducirAAsignacionDuplicada_Cuando_FallaIndiceDeAsignacionPostulacionEvaluadorVigente()
    {
        var excepcion = CrearDbUpdateUnicidad(TraductorExcepcionesPersistencia.IndiceAsignacionPostulacionEvaluadorVigenteUnico);

        var traducida = TraductorExcepcionesPersistencia.TraducirSiAplica(excepcion);

        var duplicada = traducida.Should().BeOfType<AsignacionDuplicadaException>().Subject;
        duplicada.Codigo.Should().Be(AsignacionDuplicadaException.CodigoError);
        duplicada.EstadoHttp.Should().Be(409);
    }

    [Fact]
    public void Debe_TraducirAExcepcionEvaluadorUnicoYaAutorizada_Cuando_FallaIndiceDeExcepcionPorPostulacion()
    {
        var excepcion = CrearDbUpdateUnicidad(TraductorExcepcionesPersistencia.IndiceExcepcionEvaluadorUnicoPostulacionUnico);

        var traducida = TraductorExcepcionesPersistencia.TraducirSiAplica(excepcion);

        var yaAutorizada = traducida.Should().BeOfType<ExcepcionEvaluadorUnicoYaAutorizadaException>().Subject;
        yaAutorizada.Codigo.Should().Be(ExcepcionEvaluadorUnicoYaAutorizadaException.CodigoError);
        yaAutorizada.EstadoHttp.Should().Be(409);
    }

    [Fact]
    public void Debe_TraducirAEvaluacionDuplicada_Cuando_FallaIndiceDeEvaluacionPorAsignacion()
    {
        var excepcion = CrearDbUpdateUnicidad(TraductorExcepcionesPersistencia.IndiceEvaluacionAsignacionUnico);

        var traducida = TraductorExcepcionesPersistencia.TraducirSiAplica(excepcion);

        var duplicada = traducida.Should().BeOfType<EvaluacionDuplicadaException>().Subject;
        duplicada.Codigo.Should().Be(EvaluacionDuplicadaException.CodigoError);
        duplicada.EstadoHttp.Should().Be(409);
    }

    [Fact]
    public void Debe_TraducirAResultadoConsolidadoDuplicado_Cuando_FallaIndiceDeResultadoConsolidadoPorPostulacion()
    {
        var excepcion = CrearDbUpdateUnicidad(TraductorExcepcionesPersistencia.IndiceResultadoConsolidadoPostulacionUnico);

        var traducida = TraductorExcepcionesPersistencia.TraducirSiAplica(excepcion);

        var duplicado = traducida.Should().BeOfType<ResultadoConsolidadoDuplicadoException>().Subject;
        duplicado.Codigo.Should().Be(ResultadoConsolidadoDuplicadoException.CodigoError);
        duplicado.EstadoHttp.Should().Be(409);
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
    public void Debe_TraducirAConflictoConCodigoRestriccionUnicidad_Cuando_FallaIndiceDePosicionDeRankingPorConvocatoria()
    {
        var excepcion = CrearDbUpdateUnicidad(TraductorExcepcionesPersistencia.IndiceResultadoConsolidadoConvocatoriaPosicionUnico);

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
