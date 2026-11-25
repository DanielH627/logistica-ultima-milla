# ADR-008: Entrada HTTP con clave de API y traducción de rechazos

| Campo | Valor |
|-------|-------|
| **Estado** | Aceptada, con revisión periódica (ver sección 8) |
| **Fecha** | 24 de noviembre de 2026 |
| **Autor** | [Tu nombre completo] |
| **Actividad** | Actividad 4: construcción, validación y defensa final |
| **Decisiones relacionadas** | ADR-003 (capas); ADR-005 (códigos de rechazo); plan de implementación D4-01 y D4-07 |

## 1. Contexto

El flujo necesita un punto de entrada real. El enunciado indica que puede ser una API REST, una línea de comandos, un servicio interno o un evento. Esa entrada debe respetar los límites: no contener reglas del negocio y no conocer el dominio. Además, protege una operación que cambia datos, por lo que necesita al menos una autenticación mínima.

## 2. Decisión

1. **API HTTP mínima de ASP.NET Core** con un endpoint: `POST /api/v1/deliveries/{deliveryId}/assignment`, con cuerpo `{ "courierId": ... }` y encabezado `X-Requested-By`.
2. **La entrada solo valida la forma de los datos**, los traduce al comando del caso de uso y convierte el resultado en HTTP. No usa tipos del dominio (regla verificada por una prueba de arquitectura).
3. **Traducción estable de rechazos** (códigos del ADR-005):

| Resultado | HTTP |
|-----------|------|
| Éxito | 200 |
| Datos mal formados | 400 |
| Sin clave de API o clave incorrecta | 401 |
| Entrega inexistente | 404 |
| Ya asignada, no pendiente o ruta saturada | 409 |
| Repartidor inactivo | 422 |
| Disponibilidad no verificable | 503 |
| Falla inesperada | 500 con identificador de traza y sin detalles internos |

4. **Autenticación mínima por clave de API** en el encabezado `X-Api-Key`, comparada en tiempo constante; la respuesta no distingue entre clave ausente e incorrecta. Sin clave configurada, la aplicación no arranca.
5. **Versionado en la ruta** (`/api/v1`) y errores con el formato estándar ProblemDetails, más un campo `code` estable.
6. **Raíz de composición única** (`Program.cs`): es el único lugar que decide qué adaptador implementa cada puerto.

## 3. Alternativas consideradas

| Alternativa | Resultado | Motivo |
|-------------|-----------|--------|
| **API HTTP mínima con clave de API** | **Elegida** | Entrada realista, contrato probable de punta a punta, costo bajo |
| Línea de comandos | Descartada | Más simple, pero no representa una entrada de sistema real ni permite probar el contrato HTTP |
| Autenticación completa de usuarios (tokens, roles) | Aplazada | Excede el alcance del slice; ver riesgos residuales |
| Sin autenticación | Descartada | Dejaría sin protección una operación que cambia datos |
| Controladores MVC | Descartada | Más estructura de la necesaria para un solo endpoint |

## 4. Justificación

- **Las capas se respetan:** la entrada habla con comandos y resultados de la aplicación.
- **Los consumidores dependen de códigos estables**, no de textos.
- **La clave en tiempo constante** evita revelar información por la duración de la comparación.

## 5. Consecuencias

| Lo que se gana | Lo que se cede |
|----------------|----------------|
| Contrato HTTP completo y probado | La clave de API es un secreto compartido |
| Entrada delgada y sin reglas de negocio | No identifica a cada persona: `X-Requested-By` no se puede verificar |
| Respuestas de error que no filtran detalles | Sin rotación automática de la clave ni límite de solicitudes |

## 6. Riesgos y mitigaciones

| Riesgo | Mitigación | Evidencia |
|--------|------------|-----------|
| Acceso no autorizado | Clave obligatoria | `Post_WithoutApiKey_Returns401AndChangesNothing`, `Post_WithWrongApiKey_Returns401` |
| Un endpoint nuevo sin protección | Prueba que recorre todos los endpoints | `EveryEndpointOutsideHealth_RejectsRequestsWithoutAnApiKey` |
| Filtración de detalles internos | Respuesta genérica con traza | `Post_WhenTheDatabaseIsDown_Returns500WithoutLeakingInternalDetails` |
| Datos mal formados | Validación en la entrada | `Post_EmptyCourierId_Returns400WithTheInvalidFields`, `Post_MissingRequestedBy_Returns400` |

## 7. Condiciones de revisión

1. Hay más de un consumidor o más de un rol: se reemplaza la clave por autenticación por usuario con roles (R11 de la Actividad 1).
2. Se expone la API fuera de la red de confianza: se agregan TLS propio, límite de solicitudes y una pasarela de entrada.
3. Aparecen otros flujos que justifiquen más endpoints o un estilo distinto.

## 8. Referencias

- `src/Api`
- `docs/actividad-4/01-plan-de-implementacion.md` (contrato de entrada)
