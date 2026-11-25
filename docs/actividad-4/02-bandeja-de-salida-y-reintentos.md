# Bandeja de salida, reintentos y mensajes muertos

## 1. Propósito

Documenta el paso 5 del plan de implementación: cómo los eventos llegan a sus consumidores sin perderse y sin deshacer una asignación ya válida (AJ-01, ADR-005).

## 2. Flujo

1. El caso de uso asigna el repartidor y guarda la entrega. En la **misma transacción** se escribe el mensaje `DeliveryAssigned.v1` en `transversal.bandeja_salida` con estado `Pending`.
2. El procesador (`OutboxProcessor`), ejecutado por un servicio en segundo plano, reclama un mensaje vencido con `FOR UPDATE SKIP LOCKED`.
3. Ejecuta los manejadores del tipo de mensaje: **historial** (`EventHistoryHandler`) y **aviso al repartidor** (`DeliveryAssignedNotificationHandler`).
4. Si todos funcionan: `Sent`. Si alguno falla: se reintenta con espera exponencial. Al agotar los intentos: `Dead`.

## 3. Reglas

| Regla | Valor por defecto | Dónde se configura |
|-------|-------------------|--------------------|
| Intentos totales antes de `Dead` | 3 | `Outbox:MaxAttempts` |
| Espera tras el primer fallo (se duplica) | 5 s, 10 s, 20 s… | `Outbox:BaseDelaySeconds` |
| Mensajes por ciclo | 20 | `Outbox:BatchSize` |
| Intervalo de consulta sin trabajo | 2 s | `Outbox:PollIntervalSeconds` |
| Simular fallas del sistema de avisos | `false` | `Notifications:SimulateFailure` |

## 4. Garantías y límites

- **Al menos una vez.** Un reintento puede repetir un mensaje. Por eso cada manejador es idempotente: el historial usa `UNIQUE(message_id)` con `ON CONFLICT DO NOTHING`. El aviso puede repetirse; se acepta porque es informativo.
- **Sin dobles procesamientos concurrentes.** `SKIP LOCKED` evita que dos procesadores tomen el mismo mensaje.
- **Un fallo de aviso no deshace la asignación.** La entrega queda `Assigned` aunque el mensaje termine en `Dead`.
- **Nada se descarta en silencio.** Un mensaje sin manejador también pasa a `Dead` con su motivo.
- **Sin datos personales.** El mensaje lleva solo identificadores y el solicitante; los registros y `last_error` guardan únicamente tipo y mensaje de la excepción, nunca el contenido.
- **Limitación.** Los manejadores se ejecutan con la fila bloqueada; es suficiente a esta escala. Con manejadores lentos habría que reclamar con una marca de tiempo.
- **Operación.** Los mensajes `Dead` requieren revisión manual: consulta `SELECT * FROM transversal.bandeja_salida WHERE status = 'Dead'`. Reprocesarlos es una decisión operativa pendiente (riesgo residual, paso 8).

## 5. Pruebas (12 nuevas)

| Prueba | Qué demuestra |
|--------|---------------|
| Mensaje pendiente → `Sent` | Entrega a historial y aviso |
| Sin mensajes | No hace nada |
| Mensaje ya enviado | No se reprocesa |
| Reintentos con espera exponencial | 5 s, luego 10 s, y éxito al tercer intento |
| Siempre falla | `Dead` tras 3 intentos con su motivo |
| Aviso fallido | La entrega sigue `Assigned` |
| Sin manejador | `Dead`, no se descarta |
| Historial idempotente | Una sola fila aunque se procese dos veces |
| Dos procesadores a la vez | 10 mensajes, 10 avisos, ninguno doble |
| Sin datos personales | Registros y errores no contienen dirección ni solicitante |
| Extremo a extremo (2) | HTTP → `Sent` con historial; HTTP responde bien aunque el aviso falle y el mensaje termine `Dead` |
