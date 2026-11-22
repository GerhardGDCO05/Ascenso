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
    fecha_ultima_actualizacion
) VALUES (
    '11111111-1111-4111-8111-111111111111',
    'PROG-LOCAL-001',
    'Programa local de prueba',
    'TOTAL',
    100,
    0,
    'USD',
    13,
    4,
    1,
    40,
    'LLAMADO_ATENCION',
    true,
    false,
    2,
    'ACTIVO',
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
)
ON CONFLICT (programa_beca_id) DO UPDATE SET
    codigo = EXCLUDED.codigo,
    nombre = EXCLUDED.nombre,
    politica_cobertura_modalidad = EXCLUDED.politica_cobertura_modalidad,
    politica_cobertura_porcentaje = EXCLUDED.politica_cobertura_porcentaje,
    politica_cobertura_monto_fijo_periodo = EXCLUDED.politica_cobertura_monto_fijo_periodo,
    politica_cobertura_moneda = EXCLUDED.politica_cobertura_moneda,
    condiciones_permanencia_promedio_minimo_renovacion =
        EXCLUDED.condiciones_permanencia_promedio_minimo_renovacion,
    condiciones_permanencia_materias_minimas_aprobadas =
        EXCLUDED.condiciones_permanencia_materias_minimas_aprobadas,
    condiciones_permanencia_maximo_materias_reprobadas =
        EXCLUDED.condiciones_permanencia_maximo_materias_reprobadas,
    condiciones_permanencia_horas_servicio_por_periodo =
        EXCLUDED.condiciones_permanencia_horas_servicio_por_periodo,
    condiciones_permanencia_consecuencia_incumplimiento =
        EXCLUDED.condiciones_permanencia_consecuencia_incumplimiento,
    permite_acumulacion = EXCLUDED.permite_acumulacion,
    requiere_compromiso_firmado = EXCLUDED.requiere_compromiso_firmado,
    duracion_periodos = EXCLUDED.duracion_periodos,
    estado = EXCLUDED.estado,
    ultimo_evento_id = EXCLUDED.ultimo_evento_id,
    occurred_at = EXCLUDED.occurred_at,
    fecha_ultima_actualizacion = EXCLUDED.fecha_ultima_actualizacion;

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
    fecha_ultima_actualizacion
) VALUES (
    '99999999-9999-4999-8999-999999999999',
    '66666666-6666-4666-8666-666666666666',
    '44444444-4444-4444-8444-444444444444',
    1500,
    'USD',
    1,
    CURRENT_DATE - 30,
    NULL,
    'VIGENTE',
    'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
)
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
    fecha_ultima_actualizacion = EXCLUDED.fecha_ultima_actualizacion;

INSERT INTO becas.convenio_ref (
    universidad_aliada_id,
    convenio_id,
    estado,
    fecha_fin,
    ultimo_evento_id,
    occurred_at,
    fecha_ultima_actualizacion
) VALUES (
    '55555555-5555-4555-8555-555555555555',
    'dddddddd-dddd-4ddd-8ddd-dddddddddddd',
    'VIGENTE',
    CURRENT_DATE + 365,
    'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
)
ON CONFLICT (universidad_aliada_id) DO UPDATE SET
    convenio_id = EXCLUDED.convenio_id,
    estado = EXCLUDED.estado,
    fecha_fin = EXCLUDED.fecha_fin,
    ultimo_evento_id = EXCLUDED.ultimo_evento_id,
    occurred_at = EXCLUDED.occurred_at,
    fecha_ultima_actualizacion = EXCLUDED.fecha_ultima_actualizacion;
