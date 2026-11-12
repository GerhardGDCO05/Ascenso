using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence.Proyecciones;

/// <summary>
/// Aplica los eventos de configuración de <c>MS-PRG</c> a las proyecciones
/// locales con upsert idempotente. Un evento anterior al último aplicado se
/// ignora para evitar retrocesos; repetir el mismo evento no modifica la fila.
/// </summary>
public sealed class ActualizadorProyeccionesRef
{
    private readonly BecasDbContext _contexto;

    public ActualizadorProyeccionesRef(BecasDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        _contexto = contexto;
    }

    public Task<int> ActualizarProgramaAsync(
        ProgramaBecaRef programa,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(programa);

        return _contexto.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO becas.programa_beca_ref (
                programa_beca_id,
                codigo,
                nombre,
                politica_cobertura_modalidad,
                politica_cobertura_porcentaje,
                politica_cobertura_monto_fijo_periodo,
                politica_cobertura_moneda,
                condiciones_permanencia_promedio_minimo_renovacion,
                condiciones_permanencia_materias_minimas_aprobadas,
                condiciones_permanencia_maximo_materias_reprobadas,
                condiciones_permanencia_horas_servicio_por_periodo,
                condiciones_permanencia_consecuencia_incumplimiento,
                permite_acumulacion,
                requiere_compromiso_firmado,
                duracion_periodos,
                estado,
                ultimo_evento_id,
                occurred_at,
                fecha_ultima_actualizacion)
            VALUES (
                {programa.ProgramaBecaId},
                {programa.Codigo},
                {programa.Nombre},
                {programa.PoliticaCobertura.Modalidad.ACodigo()},
                {programa.PoliticaCobertura.PorcentajeCobertura},
                {programa.PoliticaCobertura.MontoFijoPeriodo},
                {programa.PoliticaCobertura.Moneda},
                {programa.CondicionesPermanencia.PromedioMinimoRenovacion},
                {programa.CondicionesPermanencia.MateriasMinimasAprobadas},
                {programa.CondicionesPermanencia.MaximoMateriasReprobadas},
                {programa.CondicionesPermanencia.HorasServicioPorPeriodo},
                {programa.CondicionesPermanencia.ConsecuenciaIncumplimiento.ACodigo()},
                {programa.PermiteAcumulacion},
                {programa.RequiereCompromisoFirmado},
                {programa.DuracionPeriodos},
                {programa.Estado},
                {programa.UltimoEventoId},
                {programa.OccurredAt},
                NOW())
            ON CONFLICT (programa_beca_id) DO UPDATE SET
                codigo = EXCLUDED.codigo,
                nombre = EXCLUDED.nombre,
                politica_cobertura_modalidad = EXCLUDED.politica_cobertura_modalidad,
                politica_cobertura_porcentaje = EXCLUDED.politica_cobertura_porcentaje,
                politica_cobertura_monto_fijo_periodo = EXCLUDED.politica_cobertura_monto_fijo_periodo,
                politica_cobertura_moneda = EXCLUDED.politica_cobertura_moneda,
                condiciones_permanencia_promedio_minimo_renovacion = EXCLUDED.condiciones_permanencia_promedio_minimo_renovacion,
                condiciones_permanencia_materias_minimas_aprobadas = EXCLUDED.condiciones_permanencia_materias_minimas_aprobadas,
                condiciones_permanencia_maximo_materias_reprobadas = EXCLUDED.condiciones_permanencia_maximo_materias_reprobadas,
                condiciones_permanencia_horas_servicio_por_periodo = EXCLUDED.condiciones_permanencia_horas_servicio_por_periodo,
                condiciones_permanencia_consecuencia_incumplimiento = EXCLUDED.condiciones_permanencia_consecuencia_incumplimiento,
                permite_acumulacion = EXCLUDED.permite_acumulacion,
                requiere_compromiso_firmado = EXCLUDED.requiere_compromiso_firmado,
                duracion_periodos = EXCLUDED.duracion_periodos,
                estado = EXCLUDED.estado,
                ultimo_evento_id = EXCLUDED.ultimo_evento_id,
                occurred_at = EXCLUDED.occurred_at,
                fecha_ultima_actualizacion = NOW()
            WHERE EXCLUDED.ultimo_evento_id <> becas.programa_beca_ref.ultimo_evento_id
              AND EXCLUDED.occurred_at >= becas.programa_beca_ref.occurred_at
            """, cancellationToken);
    }

    public Task<int> ActualizarCostoMatriculaAsync(
        CostoMatriculaRef costo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(costo);

        return _contexto.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO becas.costo_matricula_ref (
                costo_matricula_referencia_id,
                carrera_id,
                periodo_academico_id,
                monto_referencia_matricula,
                moneda,
                numero_version,
                fecha_vigencia_desde,
                fecha_vigencia_hasta,
                estado,
                ultimo_evento_id,
                occurred_at,
                fecha_ultima_actualizacion)
            VALUES (
                {costo.CostoMatriculaReferenciaId},
                {costo.CarreraId},
                {costo.PeriodoAcademicoId},
                {costo.MontoReferenciaMatricula},
                {costo.Moneda},
                {costo.NumeroVersion},
                {costo.FechaVigenciaDesde},
                CAST({costo.FechaVigenciaHasta} AS date),
                {costo.Estado},
                {costo.UltimoEventoId},
                {costo.OccurredAt},
                NOW())
            ON CONFLICT (costo_matricula_referencia_id) DO UPDATE SET
                carrera_id = EXCLUDED.carrera_id,
                periodo_academico_id = EXCLUDED.periodo_academico_id,
                monto_referencia_matricula = EXCLUDED.monto_referencia_matricula,
                moneda = EXCLUDED.moneda,
                numero_version = EXCLUDED.numero_version,
                fecha_vigencia_desde = EXCLUDED.fecha_vigencia_desde,
                fecha_vigencia_hasta = EXCLUDED.fecha_vigencia_hasta,
                estado = EXCLUDED.estado,
                ultimo_evento_id = EXCLUDED.ultimo_evento_id,
                occurred_at = EXCLUDED.occurred_at,
                fecha_ultima_actualizacion = NOW()
            WHERE EXCLUDED.ultimo_evento_id <> becas.costo_matricula_ref.ultimo_evento_id
              AND EXCLUDED.occurred_at >= becas.costo_matricula_ref.occurred_at
            """, cancellationToken);
    }

    public Task<int> ActualizarConvenioAsync(
        ConvenioRef convenio,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(convenio);

        return _contexto.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO becas.convenio_ref (
                universidad_aliada_id,
                convenio_id,
                estado,
                fecha_fin,
                ultimo_evento_id,
                occurred_at,
                fecha_ultima_actualizacion)
            VALUES (
                {convenio.UniversidadAliadaId},
                {convenio.ConvenioId},
                {convenio.Estado},
                {convenio.FechaFin},
                {convenio.UltimoEventoId},
                {convenio.OccurredAt},
                NOW())
            ON CONFLICT (universidad_aliada_id) DO UPDATE SET
                convenio_id = EXCLUDED.convenio_id,
                estado = EXCLUDED.estado,
                fecha_fin = EXCLUDED.fecha_fin,
                ultimo_evento_id = EXCLUDED.ultimo_evento_id,
                occurred_at = EXCLUDED.occurred_at,
                fecha_ultima_actualizacion = NOW()
            WHERE EXCLUDED.ultimo_evento_id <> becas.convenio_ref.ultimo_evento_id
              AND EXCLUDED.occurred_at >= becas.convenio_ref.occurred_at
            """, cancellationToken);
    }
}
