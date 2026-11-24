using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class MigracionExcepcionEvaluadorUnicoTests
{
    [Fact]
    public void Debe_GenerarElScriptDeLaMigracionExcepcionEvaluadorUnico_Cuando_SeMigraDesdeAsignacionesEvaluacion()
    {
        using var contexto = new PostulacionesDbContextFactory().CreateDbContext([]);
        var migrador = contexto.GetService<IMigrator>();

        var script = migrador.GenerateScript(
            fromMigration: nameof(AsignacionesEvaluacion),
            toMigration: nameof(ExcepcionEvaluadorUnico));

        script.Should().Contain("CREATE TABLE postulaciones.excepciones_evaluador_unico");
        script.Should().Contain("pk_excepciones_evaluador_unico");
        script.Should().Contain("id uuid NOT NULL");
        script.Should().Contain("postulacion_id uuid NOT NULL");
        script.Should().Contain("justificacion varchar(1000) NOT NULL");
        script.Should().Contain("usuario_autorizador_id uuid NOT NULL");
        script.Should().Contain("fecha_autorizacion timestamptz NOT NULL");
        script.Should().Contain("correlacion_id uuid NOT NULL");

        script.Should().Contain("ck_excepciones_justificacion_no_vacia");
        script.Should().Contain("char_length(btrim(justificacion)) >= 10");

        script.Should().Contain("ux_excepciones_evaluador_unico_postulacion");

        script.Should().Contain("fn_excepciones_evaluador_unico_inmutable");
        script.Should().Contain("trg_excepciones_evaluador_unico_inmutable");
        script.Should().Contain("BEFORE UPDATE OR DELETE ON postulaciones.excepciones_evaluador_unico");
    }
}
