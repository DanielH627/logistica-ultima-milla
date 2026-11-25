# Riesgos, resultados y próximos pasos

## 1. Propósito

Este documento cierra la implementación de la Actividad 4. Relaciona cada riesgo operativo y de seguridad importante con una **mitigación** y una **evidencia verificable**, registra los **resultados** de las verificaciones, declara los **riesgos residuales** (lo que sigue abierto) y propone los **próximos pasos**.

Parte de los riesgos del diagnóstico (`docs/actividad-1/05-riesgos.md`, R01 a R15) y agrega los que aparecen al implementar. Los identificadores `RI` significan "riesgo de implementación".

> La evidencia citada son nombres de pruebas automatizadas del repositorio. Las pruebas se ejecutaron en el equipo del autor; el resultado de cada ejecución se registra en la sección 4.

## 2. Pre-mortem

Ejercicio: *dentro de tres meses el sistema falló de forma grave. ¿Por qué?* Cada causa se tradujo a un riesgo y la mayoría a una prueba.

| Causa imaginada del fallo | Riesgo |
|---------------------------|--------|
| Dos coordinadores asignaron la misma entrega y el cliente recibió dos repartidores | RI01 |
| Un repartidor recibió más paradas de las que su ruta soporta | RI02 |
| Se asignó la entrega pero nadie avisó al repartidor y nadie lo notó | RI03, RI04, RI06 |
| El servicio de avisos se cayó y se perdieron los avisos | RI04 |
| Alguien cambió una migración ya aplicada y los ambientes quedaron distintos | RI09 |
| Un registro de errores filtró una dirección o la clave de API | RI11 |
| Un endpoint nuevo quedó sin clave de API | RI10 |
| El código del dominio empezó a depender de la base de datos | RI13 |

## 3. Registro de riesgos de implementación

Escala: probabilidad (P) e impacto (I) de 1 a 3; nivel = P × I (igual que en la Actividad 1). Las estimaciones son cualitativas, sin datos reales de operación.

| ID | Riesgo | Relacionado con | P | I | Nivel | Mitigación | Evidencia de verificación | Estado |
|----|--------|-----------------|---|---|-------|------------|----------------------------|--------|
| RI01 | Doble asignación concurrente de una misma entrega | R03, R05 | 3 | 3 | Crítico (9) | Control de concurrencia optimista (versión de la entrega); el perdedor recibe `DELIVERY_ALREADY_ASSIGNED` | `SimultaneousAssignments_OfSameDelivery_OnlyOneWinsAndOnlyOneEventIsQueued`; `Post_SimultaneousRequests_ForTheSameDelivery_OnlyOneSucceeds`; `Save_WithStaleVersion_ThrowsConcurrencyException` | Mitigado |
| RI02 | Sobreasignación de la ruta por encima de su capacidad (INV04) | R02, R03 | 3 | 3 | Crítico (9) | Reserva de cupo con bloqueo de la fila de la ruta (`FOR UPDATE`) y restricción en la base | `ReserveStop_ManyAtTheSameTime_NeverExceedsCapacity`; `SimultaneousAssignments_ForLastSlot_OnlyOneWins`; `Post_SaturatedRoute_Returns409AndLeavesTheDeliveryPending` | Mitigado |
| RI03 | Evento perdido, o evento registrado de una operación que falló | R04 | 2 | 3 | Alto (6) | Bandeja de salida transaccional: el evento se guarda en la misma transacción que la entrega (AJ-01) | `Save_AssignedDelivery_QueuesExactlyOnePendingMessage`; `Save_WithStaleVersion_QueuesNoMessageForTheFailedOperation` | Mitigado |
| RI04 | Falla del sistema de avisos | R08 | 3 | 2 | Alto (6) | Entrega asíncrona con reintentos de espera exponencial y estado `Dead` al agotarlos; la asignación no se deshace (ADR-005) | `Process_FailingNotification_RetriesWithExponentialBackoffAndEventuallySucceeds`; `Process_AlwaysFailing_BecomesDeadAfterMaxAttempts`; `Process_FailedNotification_DoesNotUndoTheAssignment`; `Post_WhenNotificationsAlwaysFail_StillReturnsSuccessAndLeavesTheAssignmentInPlace` | Mitigado |
| RI05 | Mensaje procesado más de una vez (reintento o procesadores simultáneos) | R04 | 2 | 2 | Medio (4) | Manejadores idempotentes (historial con `UNIQUE(message_id)`); `SKIP LOCKED` evita el doble reclamo | `HistoryHandler_ProcessingTheSameMessageTwice_LeavesASingleRecord`; `Process_TwoProcessorsAtTheSameTime_NeverHandleTheSameMessageTwice` | Mitigado parcialmente (el aviso puede repetirse) |
| RI06 | Mensajes `Dead` que nadie atiende | R04, R05 | 2 | 2 | Medio (4) | Salud "degradada" si hay mensajes `Dead` o pendientes antiguos; endpoint operativo con conteos; métrica `logistica.outbox.messages` | `HealthReady_WithADeadMessage_IsDegradedButStillServing`; `OperationsOutbox_AfterAnAssignment_ReportsOnePendingMessage` | Parcial: se detecta, no se reprocesa ni alerta |
| RI07 | Ruteo no responde al verificar disponibilidad | R08, R14 | 2 | 2 | Medio (4) | Política POL01: no se asigna a ciegas; responde 503 `AVAILABILITY_UNVERIFIABLE` | `Execute_WhenAvailabilityCannotBeVerified_DoesNotAssign` | Mitigado |
| RI08 | Cupo reservado que queda huérfano si falla el guardado | R02 | 2 | 2 | Medio (4) | Compensación: se libera el cupo si el guardado falla o hay conflicto (ADR-004) | `Execute_WhenSaveConflicts_ReleasesStopAndSavesNoEvent`; `ReleaseStop_FreesTheSlot` | Parcial: si el proceso muere entre reservar y guardar, el cupo queda ocupado (ver sección 5) |
| RI09 | Esquema de datos inconsistente entre ambientes o migración alterada | R14 | 2 | 3 | Alto (6) | Migraciones versionadas con suma de verificación SHA-256 y bloqueo consultivo | `Migrate_WhenAnAppliedScriptWasModified_Throws`; `Migrate_RunningTwiceAtTheSameTime_AppliesEachScriptOnce`; `Migrate_OnEmptyDatabase_AppliesScripts_AndSecondRunAppliesNothing` | Mitigado |
| RI10 | Acceso no autorizado a la API | R09 | 2 | 3 | Alto (6) | Clave de API obligatoria con comparación en tiempo constante; respuesta que no distingue falta de error | `Post_WithoutApiKey_Returns401AndChangesNothing`; `Post_WithWrongApiKey_Returns401`; `OperationsEndpoint_WithoutApiKey_Returns401`; `EveryEndpointOutsideHealth_RejectsRequestsWithoutAnApiKey` | Mitigado parcialmente (secreto compartido) |
| RI11 | Fuga de datos personales o secretos en registros, mensajes o errores | R09 | 2 | 3 | Alto (6) | Mensajes solo con identificadores; registros sin encabezados, cuerpos ni URL real en nuestra línea; errores sin detalles internos | `Payload_NeverContainsAddressesOrOtherPersonalData`; `Process_WhenItFails_LogsAndStoredErrorsContainNoPersonalData`; `Logs_NeverContainTheApiKeyThePasswordOrTheRequester`; `RequestLog_UsesTheRouteTemplateInsteadOfTheRealUrl`; `Post_WhenTheDatabaseIsDown_Returns500WithoutLeakingInternalDetails`; `HealthReady_WhenTheDatabaseIsDown_Returns503WithoutInternalDetails` | Mitigado parcialmente (ver residual) |
| RI12 | Inyección SQL | R09 | 1 | 3 | Medio (3) | Todo el acceso a datos con parámetros tipados | `Sql_IsNeverBuiltWithStringInterpolationInProductionCode` | Mitigado (regla textual) |
| RI13 | Erosión de la arquitectura (dependencias hacia afuera, contextos acoplados) | R06, R13 | 3 | 2 | Alto (6) | Pruebas de arquitectura (funciones de aptitud) ejecutadas en cada cambio | `Domain_ReferencesOnlyTheBaseLibrary`; `Application_ReferencesOnlyTheDomainAndTheBaseLibrary`; `Infrastructure_DoesNotReferenceTheApiOrAspNetCore`; `Entregas_DoesNotDependOnRuteo`; `Ruteo_DoesNotDependOnEntregas`; `UseCases_DependOnlyOnInterfaces` | Mitigado |
| RI14 | Dependencias con vulnerabilidades conocidas | R09 | 2 | 2 | Medio (4) | Auditoría de NuGet con errores en CI; búsqueda de paquetes vulnerables; Dependabot | Flujo `.github/workflows/ci.yml` (paso "Buscar paquetes con vulnerabilidades conocidas") | Mitigado parcialmente (versiones flotantes) |
| RI15 | Credenciales de desarrollo expuestas | R09 | 2 | 2 | Medio (4) | Credenciales solo locales; ninguna en la configuración base ni en el código | `BaseConfiguration_DoesNotContainSecrets`; `ProductionCode_DoesNotContainCredentialsLiterals` | Mitigado en producción; las de desarrollo siguen versionadas |
| RI16 | Saturación o abuso de la API (volumen alto de solicitudes) | R06 | 2 | 2 | Medio (4) | Ninguna implementada | Sin evidencia | **Abierto** |
| RI17 | Pérdida de datos o indisponibilidad de la base | R14 | 2 | 3 | Alto (6) | Ninguna implementada en el slice (copias de seguridad y alta disponibilidad son de despliegue) | Sin evidencia; salud "lista" solo informa | **Abierto** |

## 4. Resultados de las verificaciones

| Paso | Qué se verificó | Resultado registrado |
|------|-----------------|----------------------|
| 2 | Persistencia, migraciones y reserva con PostgreSQL | 61 pruebas correctas |
| 4 | Entrada HTTP, errores y base caída | 78 pruebas correctas |
| 5 | Procesador de la bandeja de salida, reintentos y mensajes `Dead` | 90 pruebas esperadas |
| 6 | Salud, correlación, métricas, registros sin datos sensibles | 104 pruebas correctas |
| 7 | Pruebas de arquitectura y de seguridad automatizadas | **122 pruebas correctas** (ejecución local del autor, 24 de noviembre de 2026) |

Criterios de la actividad frente a la evidencia:

| Criterio | Evidencia |
|----------|-----------|
| Vertical slice ejecutable | `POST /api/v1/deliveries/{deliveryId}/assignment` recorre API, caso de uso, dominio, PostgreSQL y bandeja de salida (`FullFlow_AssignsDeliveryPersistsTheStopAndQueuesTheEvent`, `Post_ValidRequest_AssignsTheDeliveryAndQueuesTheEvent`) |
| Pruebas unitarias de invariantes | `DeliveryTests`, `RouteTests`, `ValueObjectTests` (17 pruebas) y `AssignCourierUseCaseTests` (10) |
| Pruebas de integración con error o límite | Concurrencia, ruta saturada, repartidor inactivo, entrega inexistente, base caída, aviso que siempre falla |
| Migraciones reproducibles | `MigrationTests` (3) |
| Observabilidad sin datos sensibles | `ObservabilityTests`, `OutboxProcessorTests` (sección RI11) |
| Comprobación automatizada de arquitectura | 18 pruebas de arquitectura y seguridad, ejecutadas en CI |

**No se midió** cobertura de código, rendimiento ni capacidad. No se ejecutó análisis estático de seguridad (SAST), pruebas dinámicas (DAST) ni pruebas de penetración: se aplicó una revisión manual y reglas automáticas, pero eso no los sustituye. Las "pruebas de caos" se limitaron a fallas simuladas y controladas (base caída, aviso que siempre falla), con hipótesis explícitas en cada prueba.

## 5. Riesgos residuales

Lo que sigue abierto después de la implementación, y por qué se acepta por ahora:

1. **Cupo huérfano (RI08).** Si el proceso se cae justo entre reservar el cupo y guardar la entrega, el cupo queda ocupado hasta intervención manual. El ajuste AJ-06 (caducidad de reservas) está previsto, no implementado. Se acepta porque la ventana es muy corta y el efecto es conservador: se asigna de menos, no de más.
2. **Mensajes `Dead` sin reprocesamiento ni alertas (RI06).** Se detectan por salud y por consulta, pero alguien debe actuar manualmente.
3. **Avisos repetidos (RI05).** La entrega es "al menos una vez": un reintento puede duplicar un aviso. Se acepta porque el aviso es informativo.
4. **Identidad débil (RI10).** La clave de API es un secreto compartido sin rotación, no identifica a cada persona, y `X-Requested-By` no se puede verificar. Solo es adecuada para el alcance de esta actividad.
5. **Ruta real en el ámbito de los registros (RI11).** ASP.NET agrega la ruta con el UUID de la entrega al ámbito del registro. Es un identificador opaco, sin datos personales.
6. **Sin límite de solicitudes, copias de seguridad ni alta disponibilidad (RI16, RI17).** Pertenecen al despliegue y no se abordan en esta actividad.
7. **Versiones de paquetes flotantes.** Varios paquetes usan `*`; cada restauración puede traer versiones distintas.
8. **Reglas de arquitectura basadas en texto.** Detectan lo común, no todo caso posible; se complementan con revisión de código.
9. **Datos de la Actividad 1 sin validar.** Las probabilidades e impactos siguen siendo estimaciones sin datos reales (supuesto S13).

## 6. Próximos pasos

En orden sugerido de valor frente a costo:

1. Implementar la caducidad de reservas (AJ-06) y una herramienta para reprocesar mensajes `Dead`.
2. Exportar métricas y registros a un sistema central (por ejemplo con OpenTelemetry) y definir alertas sobre `Dead`, latencia y tasa de errores.
3. Reemplazar la clave de API compartida por autenticación por usuario con roles (R11 de la Actividad 1).
4. Agregar límite de solicitudes y análisis SAST (por ejemplo CodeQL) al flujo de CI.
5. Fijar versiones de paquetes y activar archivos de bloqueo.
6. Hacer pruebas de carga sobre la reserva de cupos con muchas solicitudes simultáneas.
7. Implementar el segundo flujo del diseño (reasignación de rutas) reutilizando la bandeja de salida.

## 7. Trazabilidad con el diseño

| Diseño (Actividad 3) | Implementación | Riesgo asociado |
|----------------------|----------------|-----------------|
| INV01 a INV07 | `Delivery`, `Route`, objetos de valor | RI01, RI02 |
| ADR-003 (capas) | Estructura de proyectos y pruebas de arquitectura | RI13 |
| ADR-004 (coordinación Entregas–Ruteo) | Reserva y compensación | RI02, RI08 |
| ADR-005 (rechazos, eventos y avisos) | Bandeja de salida, reintentos, `Dead` | RI03 a RI06 |
| Contratos C1 a C5 | Puertos y adaptadores | RI07 |
