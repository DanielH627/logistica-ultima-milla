# ADR-009: Observabilidad sin datos sensibles

| Campo | Valor |
|-------|-------|
| **Estado** | Aceptada, con revisión periódica (ver sección 8) |
| **Fecha** | 24 de noviembre de 2026 |
| **Autor** | [Tu nombre completo] |
| **Actividad** | Actividad 4: construcción, validación y defensa final |
| **Decisiones relacionadas** | ADR-007 (bandeja de salida); ADR-008 (entrada HTTP); plan de implementación D4-08; riesgos R04 y R09 |

## 1. Contexto

El enunciado pide señales que permitan diagnosticar el flujo **sin exponer datos sensibles**. La plataforma maneja direcciones y datos de contacto, por lo que un registro descuidado es una fuga de datos (R09). A la vez, un flujo asíncrono con reintentos solo se puede operar si es observable.

## 2. Decisión

1. **Registros estructurados en JSON**, una línea por evento y con los ámbitos de la solicitud; formato legible en desarrollo.
2. **Identificador de correlación** (`X-Correlation-Id`) en cada solicitud: se acepta el recibido si es corto y de caracteres seguros, o se genera uno; se devuelve en la respuesta y aparece en todos los registros de esa solicitud.
3. **Una línea por solicitud** con método, **plantilla de ruta**, estado y duración. No se registran encabezados, cuerpos ni cadena de consulta.
4. **Métricas con `System.Diagnostics.Metrics`**, de baja cardinalidad (sin identificadores): solicitudes y duración HTTP, resultado de las asignaciones y resultado de la bandeja de salida (`sent`, `retry`, `dead`).
5. **Salud:** `/health/live` (el proceso responde, sin consultar nada externo) y `/health/ready` (base de datos y bandeja de salida; `Degraded` si hay mensajes `Dead` o pendientes antiguos; `Unhealthy` si la base no responde). Las respuestas solo traen estados.
6. **Endpoint operativo protegido** con la clave de API: conteos de la bandeja de salida.
7. **Qué se guarda de un error:** solo el tipo y el mensaje de la excepción (máximo 500 caracteres), nunca el contenido del mensaje.

## 3. Alternativas consideradas

| Alternativa | Resultado | Motivo |
|-------------|-----------|--------|
| **Registros JSON, métricas estándar y salud** | **Elegida** | Estándares de .NET, sin proveedor atado, verificable con pruebas |
| Registros de texto libre | Descartada | Difíciles de filtrar y más propensos a incluir datos sin querer |
| Paquete de telemetría completo con exportador | Aplazada | Requiere un destino (sistema de monitoreo) que este proyecto no despliega |
| Registrar cuerpos y encabezados para depurar | Descartada | Filtraría la clave de API y datos personales |

## 4. Justificación

- **Se puede responder qué pasó con una solicitud** siguiendo su identificador de correlación.
- **La plantilla de ruta evita la cardinalidad alta** en métricas y la exposición de identificadores en el mensaje del registro.
- **La salud degradada separa "sigue atendiendo" de "algo requiere atención"**, lo que evita sacar de servicio una aplicación que funciona.

## 5. Consecuencias

| Lo que se gana | Lo que se cede |
|----------------|----------------|
| Diagnóstico del flujo sin datos sensibles | Menos detalle al depurar (por diseño) |
| Métricas listas para exportar sin cambiar código | Aún no hay un sistema que las reciba ni alertas |
| Fallas visibles: `Dead`, latencia, tasa de errores | ASP.NET agrega la ruta real (con el UUID de la entrega) al ámbito del registro; se acepta porque es un identificador opaco |

## 6. Riesgos y mitigaciones

| Riesgo | Mitigación | Evidencia |
|--------|------------|-----------|
| Fuga de la clave, la contraseña o el solicitante en los registros | Registro solo de datos no sensibles | `Logs_NeverContainTheApiKeyThePasswordOrTheRequester` |
| URL real en el mensaje del registro | Plantilla de ruta | `RequestLog_UsesTheRouteTemplateInsteadOfTheRealUrl` |
| Datos personales en errores guardados | Solo tipo y mensaje | `Process_WhenItFails_LogsAndStoredErrorsContainNoPersonalData` |
| Inyección de texto por el identificador de correlación | Validación de caracteres y longitud | `Response_WithUnsafeCorrelationId_ReplacesIt` |
| Respuesta de salud que revele detalles | Solo estados | `HealthReady_WhenTheDatabaseIsDown_Returns503WithoutInternalDetails` |

## 7. Condiciones de revisión

1. Se despliega en un entorno con sistema de monitoreo: se exportan métricas y registros y se definen alertas.
2. Se requiere trazabilidad entre servicios (si se extrae Ruteo): se adopta propagación de contexto distribuido.
3. Una auditoría exige registrar quién consultó qué (R09).

## 8. Referencias

- `src/Api/Observability`, `src/Infrastructure/Outbox/OutboxMetrics.cs`
- `docs/actividad-4/03-observabilidad-y-seguridad.md`
