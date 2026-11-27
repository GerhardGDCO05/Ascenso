using Ascenso.Postulaciones.Application.Asignaciones.Comandos.AsignarEvaluadoresPostulacion;
using Ascenso.Postulaciones.Application.Asignaciones.Comandos.AutorizarExcepcionEvaluadorUnico;
using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerAsignacionesPostulacion;
using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerCargaEvaluadoresComite;
using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerExcepcionEvaluadorUnico;
using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerMisAsignaciones;
using Ascenso.Postulaciones.Application.Comites.Comandos.AgregarMiembroComite;
using Ascenso.Postulaciones.Application.Comites.Comandos.CrearComiteEvaluador;
using Ascenso.Postulaciones.Application.Comites.Comandos.DesvincularMiembroComite;
using Ascenso.Postulaciones.Application.Comites.Consultas.ObtenerComiteEvaluador;
using Ascenso.Postulaciones.Application.Comites.Consultas.ObtenerComitePorPrograma;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConfirmarRankingConvocatoria;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConsolidarPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ResolverCasoLimiteRanking;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.RevisarDiscrepanciaConsolidacion;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ConsultarAdjudicabilidadPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ListarResultadosConsolidadosConvocatoria;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerRankingConvocatoria;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerResultadoConsolidadoPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.Interfaces;
using Ascenso.Postulaciones.Application.Consolidacion.Servicios;
using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.EnviarEvaluacionDefinitiva;
using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.GuardarBorradorEvaluacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerEvaluacionesCoevaluadoresPostulacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerEvaluacionesPostulacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerMiEvaluacionPorAsignacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerMisEvaluaciones;
using Ascenso.Postulaciones.Application.Evaluaciones.Interfaces;
using Ascenso.Postulaciones.Application.Evaluaciones.Servicios;
using Ascenso.Postulaciones.Application.Postulantes.Comandos.ActualizarPerfilSocioeconomico;
using Ascenso.Postulaciones.Application.Postulantes.Comandos.RegistrarPostulante;
using Ascenso.Postulaciones.Application.Postulantes.Consultas.ObtenerMiPerfil;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.AgregarCriterio;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.BloquearRubrica;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.CrearRubrica;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.EditarCriterio;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.EliminarCriterio;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.PublicarRubrica;
using Ascenso.Postulaciones.Application.Rubricas.Consultas.ObtenerRubrica;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Postulaciones.Application;

public static class DependencyInjection
{
    public static IServiceCollection AgregarAplicacionPostulaciones(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<RegistrarPostulanteCommandHandler>();
        services.AddScoped<ActualizarPerfilSocioeconomicoCommandHandler>();
        services.AddScoped<ObtenerMiPerfilQueryHandler>();
        services.AddScoped<CrearRubricaCommandHandler>();
        services.AddScoped<AgregarCriterioCommandHandler>();
        services.AddScoped<EditarCriterioCommandHandler>();
        services.AddScoped<EliminarCriterioCommandHandler>();
        services.AddScoped<PublicarRubricaCommandHandler>();
        services.AddScoped<BloquearRubricaCommandHandler>();
        services.AddScoped<ObtenerRubricaQueryHandler>();
        services.AddScoped<CrearComiteEvaluadorCommandHandler>();
        services.AddScoped<AgregarMiembroComiteCommandHandler>();
        services.AddScoped<DesvincularMiembroComiteCommandHandler>();
        services.AddScoped<ObtenerComiteEvaluadorQueryHandler>();
        services.AddScoped<ObtenerComitePorProgramaQueryHandler>();
        services.AddScoped<AsignarEvaluadoresPostulacionCommandHandler>();
        services.AddScoped<ObtenerAsignacionesPostulacionQueryHandler>();
        services.AddScoped<ObtenerCargaEvaluadoresComiteQueryHandler>();
        services.AddScoped<ObtenerMisAsignacionesQueryHandler>();
        services.AddScoped<AutorizarExcepcionEvaluadorUnicoCommandHandler>();
        services.AddScoped<ObtenerExcepcionEvaluadorUnicoQueryHandler>();
        services.AddScoped<IBloqueadorRubricaEvaluacion, BloqueadorRubricaEvaluacionAdapter>();
        services.AddScoped<GuardarBorradorEvaluacionCommandHandler>();
        services.AddScoped<EnviarEvaluacionDefinitivaCommandHandler>();
        services.AddScoped<ObtenerMiEvaluacionPorAsignacionQueryHandler>();
        services.AddScoped<ObtenerMisEvaluacionesQueryHandler>();
        services.AddScoped<ObtenerEvaluacionesPostulacionQueryHandler>();
        services.AddScoped<ObtenerEvaluacionesCoevaluadoresPostulacionQueryHandler>();
        services.AddScoped<IConsolidadorPostulacionEvaluacion, ConsolidadorPostulacionEvaluacionAdapter>();
        services.AddScoped<ConsolidarPostulacionCommandHandler>();
        services.AddScoped<RevisarDiscrepanciaConsolidacionCommandHandler>();
        services.AddScoped<ObtenerResultadoConsolidadoPostulacionQueryHandler>();
        services.AddScoped<ListarResultadosConsolidadosConvocatoriaQueryHandler>();
        services.AddScoped<ObtenerRankingConvocatoriaQueryHandler>();
        services.AddScoped<ConfirmarRankingConvocatoriaCommandHandler>();
        services.AddScoped<ResolverCasoLimiteRankingCommandHandler>();
        services.AddScoped<ConsultarAdjudicabilidadPostulacionQueryHandler>();

        return services;
    }
}
