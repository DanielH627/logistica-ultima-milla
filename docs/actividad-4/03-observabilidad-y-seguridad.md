# Observabilidad y seguridad operativa

## 1. Propósito

Documenta el paso 6: cómo se puede saber qué está pasando en la plataforma (registros, métricas, salud) y qué se hizo para que esa información no exponga datos sensibles.

## 2. Registros estructurados

- En producción los registros salen en **JSON, una línea por evento** (`Observability:JsonLogs = true`). En desarrollo salen en formato legible.
- Cada solicitud recibe un **identificador de correlación** (`X-Correlation-Id`): se acepta el que llegue si es corto y de caracteres seguros (letras, números, `-`, `_`); si no, se genera uno. Se devuelve en la respuesta y aparece en todos los registros de esa solicitud.
- Cada solicitud genera una línea: método, **plantilla de ruta** (no la URL real), estado y duración. Nivel: 5xx `Error`, 4xx `Warning`, resto `Information`; las sondas de salud van en `Debug`.

| Se registra | No se registra nunca |
|-------------|----------------------|
| Método, plantilla de ruta, estado, duración | Cadena de consulta, encabezados, cuerpos |
| Identificadores de mensajes y tipo de mensaje | Encabezados (`X-Api-Key`, `X-Requested-By`) |
| Número de intento, tipo y mensaje de la excepción | Cuerpos de solicitud, contenido (`payload`) de los mensajes |
| Identificadores de entrega y repartidor (avisos) | Direcciones, nombres, contactos, cadena de conexión |

> **Nota.** El mensaje de nuestra línea de solicitud usa la plantilla de ruta. Pero ASP.NET Core agrega por sí mismo al ámbito del registro la ruta real (`RequestPath`), que incluye el UUID de la entrega. Se acepta porque es un identificador opaco, sin datos personales, y no incluye la cadena de consulta. Si en el futuro la ruta llevara datos personales, habría que desactivar los ámbitos de ASP.NET.

## 3. Métricas (System.Diagnostics.Metrics)

| Métrica | Tipo | Etiquetas | Para qué sirve |
|---------|------|-----------|----------------|
| `logistica.http.requests` | contador | `route`, `method`, `status_class` | Volumen y tasa de errores |
| `logistica.http.request.duration` | histograma (ms) | `route`, `method`, `status_class` | Latencia |
| `logistica.assignments` | contador | `result` (`success` o código de error) | Resultado de negocio: cuántas asignaciones se rechazan y por qué |
| `logistica.outbox.messages` | contador | `outcome` (`sent`, `retry`, `dead`) | Salud de la entrega de eventos |

Las etiquetas son de **baja cardinalidad**: nunca identificadores de entregas ni de repartidores. Las métricas se pueden observar con `dotnet-counters` o exportar a un sistema de monitoreo sin cambiar el código. En este proyecto **no se despliega un sistema de monitoreo**; queda como riesgo residual.

## 4. Salud

| Endpoint | Autenticación | Qué verifica | Respuestas |
|----------|---------------|--------------|------------|
| `GET /health/live` | No | Que el proceso responde (no consulta nada externo) | 200 |
| `GET /health/ready` | No | Base de datos (`SELECT 1`, límite de 2 s) y bandeja de salida | 200 `Healthy`, 200 `Degraded`, 503 `Unhealthy` |
| `GET /api/v1/operations/outbox` | Clave de API | Conteos de mensajes `pending`, `sent`, `dead` y antigüedad del pendiente más viejo | 200 / 401 |

- La respuesta de salud solo trae **estados**, nunca descripciones ni excepciones.
- **Degraded** (sigue atendiendo): hay mensajes `Dead` o el pendiente más antiguo supera 5 minutos. **Unhealthy**: la base no responde.
- Los endpoints de salud no llevan clave para que los pueda sondear un orquestador; no revelan nada más que el estado.

## 5. Seguridad aplicada en este paso

- Encabezados en todas las respuestas: `X-Content-Type-Options: nosniff`, `Cache-Control: no-store`, `Referrer-Policy: no-referrer`, `X-Frame-Options: DENY`.
- Los errores internos se responden con un identificador de traza y sin detalles (ya probado en el paso 4).
- Los endpoints operativos exigen la clave de API (comparación en tiempo constante).

## 6. Pruebas (14 nuevas)

| Qué se comprueba | Cantidad |
|------------------|----------|
| Salud: viva aunque la base caiga; lista responde 503 sin detalles internos; lista sana; degradada con mensaje `Dead` | 4 |
| Correlación: se genera, se respeta uno válido (y aparece en el ámbito del log), se reemplaza uno inseguro | 3 |
| Encabezados de seguridad | 1 |
| Endpoint operativo: 401 sin clave; conteo correcto tras una asignación | 2 |
| Métricas: solicitudes HTTP (con plantilla de ruta) y resultado `sent` de la bandeja | 2 |
| Registros: sin clave de API, contraseña ni solicitante; con plantilla de ruta en lugar de la URL real | 2 |

## 7. Riesgos residuales de este paso

- La clave de API es un secreto compartido: no identifica a cada persona y `X-Requested-By` no se puede verificar.
- No hay TLS propio: se asume que termina en un proxy o balanceador del despliegue.
- No hay límite de solicitudes por cliente (rate limiting).
- Métricas y registros no se envían a un sistema central; sin eso nadie recibe alertas automáticas.
- Los mensajes `Dead` solo se detectan por salud y consulta; no hay herramienta de reprocesamiento.
