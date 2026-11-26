using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class EvaluacionCoevaluadoresPostgreSqlTests
{
    private readonly PostgreSqlFixture _fixture;

    public EvaluacionCoevaluadoresPostgreSqlTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private static (
        RubricaEvaluacion Rubrica,
        CriterioEvaluacion Criterio1,
        CriterioEvaluacion Criterio2) CrearRubricaPublicada(
        Guid programaBecaId,
        Guid convocatoriaId,
        Guid coordinadorId,
        DateTimeOffset fechaBase)
    {
        var rubrica = RubricaEvaluacion.Crear(
            programaBecaId,
            convocatoriaId,
            "Rúbrica Coevaluadores PostgreSQL 2026",
            fechaBase,
            coordinadorId);

        var criterio1 = rubrica.AgregarCriterio(
            "Mérito académico",
            "Promedio ponderado verificado",
            60.00m,
            0.00m,
            20.00m,
            fechaBase.AddMinutes(1),
            coordinadorId);

        var criterio2 = rubrica.AgregarCriterio(
            "Situación socioeconómica",
            "Vulnerabilidad económica familiar",
            40.00m,
            0.00m,
            10.00m,
            fechaBase.AddMinutes(2),
            coordinadorId);

        rubrica.Publicar(fechaBase.AddMinutes(3), coordinadorId);
        return (rubrica, criterio1, criterio2);
    }

    [Fact]
    public async Task Debe_RetornarAsignacionVigenteORetornarNullParaNoAsignadoOReasignadoPorConflicto_Cuando_SeConsultaEnPostgreSqlReal()
    {
        var fechaBase = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
        var programaBecaId = Guid.CreateVersion7();
        var convocatoriaId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();

        var evaluadorPendienteId = Guid.CreateVersion7();
        var evaluadorEnProgresoId = Guid.CreateVersion7();
        var evaluadorCompletadaId = Guid.CreateVersion7();
        var evaluadorReasignadoId = Guid.CreateVersion7();
        var evaluadorNoAsignadoId = Guid.CreateVersion7();

        var comite = ComiteEvaluador.Crear(
            programaBecaId,
            convocatoriaId,
            "Comité Coevaluadores Asignaciones PostgreSQL",
            evaluadorPendienteId,
            fechaBase,
            coordinadorId);
        var miembroPendiente = comite.Miembros.Single();
        var miembroEnProgreso = comite.AgregarMiembro(
            evaluadorEnProgresoId,
            TipoEvaluador.Externo,
            fechaBase.AddMinutes(1),
            coordinadorId);
        var miembroCompletada = comite.AgregarMiembro(
            evaluadorCompletadaId,
            TipoEvaluador.Interno,
            fechaBase.AddMinutes(2),
            coordinadorId);
        var miembroReasignado = comite.AgregarMiembro(
            evaluadorReasignadoId,
            TipoEvaluador.Externo,
            fechaBase.AddMinutes(3),
            coordinadorId);

        var asignacionPendiente = AsignacionEvaluacion.Crear(
            comite,
            miembroPendiente.Id,
            postulacionId,
            fechaBase.AddMinutes(10),
            fechaBase.AddDays(7),
            coordinadorId);

        var asignacionEnProgreso = AsignacionEvaluacion.Crear(
            comite,
            miembroEnProgreso.Id,
            postulacionId,
            fechaBase.AddMinutes(11),
            fechaBase.AddDays(7),
            coordinadorId);
        asignacionEnProgreso.IniciarEvaluacion(fechaBase.AddMinutes(20), evaluadorEnProgresoId);

        var asignacionCompletada = AsignacionEvaluacion.Crear(
            comite,
            miembroCompletada.Id,
            postulacionId,
            fechaBase.AddMinutes(12),
            fechaBase.AddDays(7),
            coordinadorId);
        asignacionCompletada.IniciarEvaluacion(fechaBase.AddMinutes(21), evaluadorCompletadaId);
        asignacionCompletada.CompletarEvaluacion(fechaBase.AddMinutes(25), evaluadorCompletadaId);

        var asignacionReasignada = AsignacionEvaluacion.Crear(
            comite,
            miembroReasignado.Id,
            postulacionId,
            fechaBase.AddMinutes(13),
            fechaBase.AddDays(7),
            coordinadorId);

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.ComitesEvaluadores.Add(comite);
            contextoSemilla.AsignacionesEvaluacion.AddRange(
                asignacionPendiente,
                asignacionEnProgreso,
                asignacionCompletada,
                asignacionReasignada);
            await contextoSemilla.GuardarCambiosAsync();

            await contextoSemilla.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE postulaciones.asignaciones_evaluacion
                SET estado = 'REASIGNADA_POR_CONFLICTO'
                WHERE id = {asignacionReasignada.Id}
                """);
        }

        await using var contextoConsulta = _fixture.CrearContexto();
        var repositorio = new ConsultaEvaluacionesCoevaluadoresRepository(contextoConsulta);

        var leidaPendiente = await repositorio.ObtenerAsignacionVigenteEvaluadorAsync(
            postulacionId,
            evaluadorPendienteId);
        var leidaEnProgreso = await repositorio.ObtenerAsignacionVigenteEvaluadorAsync(
            postulacionId,
            evaluadorEnProgresoId);
        var leidaCompletada = await repositorio.ObtenerAsignacionVigenteEvaluadorAsync(
            postulacionId,
            evaluadorCompletadaId);
        var leidaReasignada = await repositorio.ObtenerAsignacionVigenteEvaluadorAsync(
            postulacionId,
            evaluadorReasignadoId);
        var leidaNoAsignada = await repositorio.ObtenerAsignacionVigenteEvaluadorAsync(
            postulacionId,
            evaluadorNoAsignadoId);

        leidaPendiente.Should().NotBeNull();
        leidaPendiente!.Id.Should().Be(asignacionPendiente.Id);
        leidaPendiente.Estado.Should().Be(EstadoAsignacionEvaluacion.Pendiente);

        leidaEnProgreso.Should().NotBeNull();
        leidaEnProgreso!.Id.Should().Be(asignacionEnProgreso.Id);
        leidaEnProgreso.Estado.Should().Be(EstadoAsignacionEvaluacion.EnProgreso);

        leidaCompletada.Should().NotBeNull();
        leidaCompletada!.Id.Should().Be(asignacionCompletada.Id);
        leidaCompletada.Estado.Should().Be(EstadoAsignacionEvaluacion.Completada);

        leidaReasignada.Should().BeNull();
        leidaNoAsignada.Should().BeNull();
        contextoConsulta.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_ListarUnicamenteDefinitivasDeCoevaluadoresConCalificacionesYOrdenPorFechaEnvioEId_Cuando_SeConsultaEnPostgreSqlReal()
    {
        var fechaBase = new DateTimeOffset(2026, 11, 25, 11, 0, 0, TimeSpan.Zero);
        var programaBecaId = Guid.CreateVersion7();
        var convocatoriaId = Guid.CreateVersion7();
        var coordinadorId = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();

        var evaluadorSolicitanteId = Guid.CreateVersion7();
        var coevaluadorDefinitivoTardioId = Guid.CreateVersion7();
        var coevaluadorDefinitivoTempranoId = Guid.CreateVersion7();
        var coevaluadorBorradorId = Guid.CreateVersion7();
        var coevaluadorInvalidadoId = Guid.CreateVersion7();

        var (rubrica, criterio1, criterio2) = CrearRubricaPublicada(
            programaBecaId,
            convocatoriaId,
            coordinadorId,
            fechaBase);

        var comite = ComiteEvaluador.Crear(
            programaBecaId,
            convocatoriaId,
            "Comité Coevaluadores Evaluaciones PostgreSQL",
            evaluadorSolicitanteId,
            fechaBase.AddMinutes(5),
            coordinadorId);
        var miembroSolicitante = comite.Miembros.Single();
        var miembroTardio = comite.AgregarMiembro(
            coevaluadorDefinitivoTardioId,
            TipoEvaluador.Externo,
            fechaBase.AddMinutes(6),
            coordinadorId);
        var miembroTemprano = comite.AgregarMiembro(
            coevaluadorDefinitivoTempranoId,
            TipoEvaluador.Interno,
            fechaBase.AddMinutes(7),
            coordinadorId);
        var miembroBorrador = comite.AgregarMiembro(
            coevaluadorBorradorId,
            TipoEvaluador.Externo,
            fechaBase.AddMinutes(8),
            coordinadorId);
        var miembroInvalidado = comite.AgregarMiembro(
            coevaluadorInvalidadoId,
            TipoEvaluador.Interno,
            fechaBase.AddMinutes(9),
            coordinadorId);

        var fechaLimite = fechaBase.AddDays(10);
        var asignacionSolicitante = AsignacionEvaluacion.Crear(
            comite,
            miembroSolicitante.Id,
            postulacionId,
            fechaBase.AddMinutes(10),
            fechaLimite,
            coordinadorId);
        var asignacionTardio = AsignacionEvaluacion.Crear(
            comite,
            miembroTardio.Id,
            postulacionId,
            fechaBase.AddMinutes(11),
            fechaLimite,
            coordinadorId);
        var asignacionTemprano = AsignacionEvaluacion.Crear(
            comite,
            miembroTemprano.Id,
            postulacionId,
            fechaBase.AddMinutes(12),
            fechaLimite,
            coordinadorId);
        var asignacionBorrador = AsignacionEvaluacion.Crear(
            comite,
            miembroBorrador.Id,
            postulacionId,
            fechaBase.AddMinutes(13),
            fechaLimite,
            coordinadorId);
        var asignacionInvalidada = AsignacionEvaluacion.Crear(
            comite,
            miembroInvalidado.Id,
            postulacionId,
            fechaBase.AddMinutes(14),
            fechaLimite,
            coordinadorId);

        // 1. Propia definitiva del solicitante (debe ser excluida)
        var evaluacionPropia = Evaluacion.CrearBorrador(
            asignacionSolicitante,
            rubrica,
            [
                new DatosCalificacionCriterio(criterio1.Id, 19.00m, "Propio C1"),
                new DatosCalificacionCriterio(criterio2.Id, 9.00m, "Propio C2")
            ],
            "Evaluación propia",
            fechaBase.AddMinutes(20),
            evaluadorSolicitanteId);
        evaluacionPropia.EnviarDefinitiva(
            rubrica,
            fechaLimite,
            fechaBase.AddMinutes(25),
            evaluadorSolicitanteId);

        // 2. Coevaluador definitivo con fecha de envío posterior (creado antes, enviado después)
        var evaluacionCoevaluadorTardio = Evaluacion.CrearBorrador(
            asignacionTardio,
            rubrica,
            [
                new DatosCalificacionCriterio(criterio1.Id, 16.00m, "Tardío C1"),
                new DatosCalificacionCriterio(criterio2.Id, 8.00m, "Tardío C2")
            ],
            "Evaluación definitiva coevaluador tardío",
            fechaBase.AddMinutes(21),
            coevaluadorDefinitivoTardioId);
        var fechaEnvioTardio = fechaBase.AddMinutes(50);
        evaluacionCoevaluadorTardio.EnviarDefinitiva(
            rubrica,
            fechaLimite,
            fechaEnvioTardio,
            coevaluadorDefinitivoTardioId);

        // 3. Coevaluador definitivo con fecha de envío anterior (creado después, enviado antes)
        var evaluacionCoevaluadorTemprano = Evaluacion.CrearBorrador(
            asignacionTemprano,
            rubrica,
            [
                new DatosCalificacionCriterio(criterio1.Id, 18.00m, "Temprano C1"),
                new DatosCalificacionCriterio(criterio2.Id, 9.50m, "Temprano C2")
            ],
            "Evaluación definitiva coevaluador temprano",
            fechaBase.AddMinutes(22),
            coevaluadorDefinitivoTempranoId);
        var fechaEnvioTemprano = fechaBase.AddMinutes(30);
        evaluacionCoevaluadorTemprano.EnviarDefinitiva(
            rubrica,
            fechaLimite,
            fechaEnvioTemprano,
            coevaluadorDefinitivoTempranoId);

        // 4. Coevaluador en BORRADOR (debe ser excluida)
        var evaluacionBorrador = Evaluacion.CrearBorrador(
            asignacionBorrador,
            rubrica,
            [new DatosCalificacionCriterio(criterio1.Id, 14.00m, "Solo borrador")],
            "Borrador en curso",
            fechaBase.AddMinutes(23),
            coevaluadorBorradorId);

        // 5. Coevaluador INVALIDADA_POR_CONFLICTO (debe ser excluida)
        var evaluacionInvalidada = Evaluacion.CrearBorrador(
            asignacionInvalidada,
            rubrica,
            [
                new DatosCalificacionCriterio(criterio1.Id, 15.00m, "Invalidada C1"),
                new DatosCalificacionCriterio(criterio2.Id, 7.00m, "Invalidada C2")
            ],
            "Será invalidada por conflicto",
            fechaBase.AddMinutes(24),
            coevaluadorInvalidadoId);
        evaluacionInvalidada.EnviarDefinitiva(
            rubrica,
            fechaLimite,
            fechaBase.AddMinutes(35),
            coevaluadorInvalidadoId);

        await using (var contextoSemilla = _fixture.CrearContexto())
        {
            contextoSemilla.Rubricas.Add(rubrica);
            contextoSemilla.ComitesEvaluadores.Add(comite);
            contextoSemilla.AsignacionesEvaluacion.AddRange(
                asignacionSolicitante,
                asignacionTardio,
                asignacionTemprano,
                asignacionBorrador,
                asignacionInvalidada);
            contextoSemilla.Evaluaciones.AddRange(
                evaluacionPropia,
                evaluacionCoevaluadorTardio,
                evaluacionCoevaluadorTemprano,
                evaluacionBorrador,
                evaluacionInvalidada);
            await contextoSemilla.GuardarCambiosAsync();

            await contextoSemilla.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE postulaciones.evaluaciones
                SET estado = 'INVALIDADA_POR_CONFLICTO',
                    motivo_invalidacion = 'Conflicto de interés declarado con el postulante'
                WHERE id = {evaluacionInvalidada.Id}
                """);
        }

        await using var contextoConsulta = _fixture.CrearContexto();
        var repositorio = new ConsultaEvaluacionesCoevaluadoresRepository(contextoConsulta);

        var resultado = await repositorio.ListarDefinitivasDeCoevaluadoresAsync(
            postulacionId,
            evaluadorSolicitanteId);

        resultado.Should().HaveCount(2);
        resultado.Select(e => e.Id).Should().ContainInOrder(
            evaluacionCoevaluadorTemprano.Id,
            evaluacionCoevaluadorTardio.Id);

        resultado[0].Estado.Should().Be(EstadoEvaluacion.EnviadaDefinitiva);
        resultado[0].EvaluadorId.Should().Be(coevaluadorDefinitivoTempranoId);
        resultado[0].FechaEnvio.Should().Be(fechaEnvioTemprano);
        resultado[0].Calificaciones.Should().HaveCount(2);

        resultado[1].Estado.Should().Be(EstadoEvaluacion.EnviadaDefinitiva);
        resultado[1].EvaluadorId.Should().Be(coevaluadorDefinitivoTardioId);
        resultado[1].FechaEnvio.Should().Be(fechaEnvioTardio);
        resultado[1].Calificaciones.Should().HaveCount(2);

        resultado.Should().NotContain(e => e.Id == evaluacionPropia.Id);
        resultado.Should().NotContain(e => e.Id == evaluacionBorrador.Id);
        resultado.Should().NotContain(e => e.Id == evaluacionInvalidada.Id);
        contextoConsulta.ChangeTracker.Entries().Should().BeEmpty();
    }
}
