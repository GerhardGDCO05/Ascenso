## Tasks

- [x] 9.1 Configurar Hangfire con PostgreSQL, esquema `hangfire`, cola `critical` y dashboard protegido; verificar que el dashboard responde `401/403` sin autorización y `200` con rol administrativo.
- [x] 9.2 Implementar `ExpirarOfertaBecaJob` y programarlo al emitir la oferta, guardando el id en `referenciaJobExpiracionHangfireId`; verificar con una prueba de integración que una oferta pendiente expira y una aceptada no.
- [x] 9.3 Implementar `ReconciliarOfertasPendientesJob` recurrente cada 5 minutos como red de seguridad; verificar que detecta ofertas vencidas sin job o con job perdido.
- [x] 9.4 Implementar los jobs de limpieza de idempotencia y Outbox con retención configurable; verificar que eliminan solo registros vencidos.
- [x] 9.5 Verificar la persistencia de jobs tras reiniciar el contenedor de la API; comprobar que la oferta sigue expirando después del reinicio.
- [x] Verificar build con `--warnaserror`, `dotnet format --verify-no-changes`, suite completa, migraciones sin cambios pendientes y `openspec validate --all --strict`.
- [x] Corregir la concurrencia de las filas nuevas de `HistorialEstadoBeca` con `ValueGeneratedNever()` y verificar la expiración idempotente.
- [x] Corregir la configuración de `DashboardApiFactory` para usar la base Testcontainers en CI (`UseSetting`) y verificar los 3 tests del dashboard.
- [x] Registrar este histórico de sesión sin mover ni archivar el cambio activo `tech-bootstrap-int4-msbeca`.
