# Plan de implementación del vertical slice

## 1. Introducción

La Actividad 4 implementa y valida el flujo diseñado en la Actividad 3, **asignar un repartidor a una entrega**, conectando la entrada, la aplicación, el dominio y la persistencia sin romper los límites ya definidos. Este documento fija el alcance, las decisiones de implementación, el contrato de entrada, el modelo de datos y la estrategia de errores y de pruebas, antes de construir.

Parte de lo ya decidido: la estructura de monolito modular (ADR-001), las capas con dependencias hacia adentro (ADR-003), la coordinación entre Entregas y Ruteo mediante reserva y liberación (ADR-004) y el tratamiento de rechazos, eventos y avisos (ADR-005). La implementación debe **demostrar que el dominio es la pieza central y que la infraestructura solo lo acompaña**.

## 2. Alcance del vertical slice

Un vertical slice es una porción del sistema que recorre todas las capas. Este atraviesa: **entrada HTTP, caso de uso, dominio, persistencia y adaptadores de salida**.

| Dentro del alcance | Fuera del alcance |
|--------------------|-------------------|
| Un punto de entrada HTTP para asignar un repartidor a una entrega | Pantallas o interfaz de usuario |
| El caso de uso `AssignCourierUseCase` conectado a persistencia real | Reasignación, cancelación y los demás estados de la entrega |
| Persistencia de entregas, repartidores, rutas y paradas en PostgreSQL | Creación de entregas desde pedidos confirmados (se usan datos de demostración) |
| Migraciones reproducibles del esquema | Integración real con un proveedor de notificaciones o de geolocalización |
| Bandeja de salida con reintentos y mensajes fallidos para los avisos | Asignación automática por algoritmo |
| Pruebas unitarias, de integración y de arquitectura | Autenticación completa de usuarios |
| Observabilidad mínima y revisión de riesgos | Despliegue en un entorno productivo |

## 3. Ajustes previstos al diseño de la Actividad 3

Implementar casi siempre revela detalles que el diseño no tenía. Estos ajustes se registran aquí y se confirman al construir; cada uno quedará vinculado a una decisión.

| ID | Ajuste | Motivo |
|----|--------|--------|
| AJ-01 | El evento `DeliveryAssigned` se guarda en una **bandeja de salida** dentro de la misma transacción que la entrega, en lugar de publicarse después de guardar | Elimina el riesgo de evento perdido que el ADR-005 dejó para esta actividad |
| AJ-02 | El caso de uso deja de invocar directamente `IDomainEventPublisher` e `INotificationRequester`; el repositorio persiste los eventos y un procesador los entrega | Consecuencia de AJ-01 |
| AJ-03 | El campo `NoticeRequested` del resultado pasa a significar que el aviso **quedó encolado** | Con la bandeja de salida, el aviso ya no se envía dentro de la solicitud |
| AJ-04 | Ruteo se persiste en la **misma base de datos, en un esquema propio del módulo** | Respeta la separación de datos por módulo del ADR-001 sin multiplicar los almacenes |
| AJ-05 | Los datos de demostración se cargan con una **semilla en el entorno de desarrollo** | La creación de entregas desde pedidos queda fuera del slice |
| AJ-06 | Las reservas de cupo pueden llevar **vencimiento** | Mitigación prevista en el ADR-004 para un cupo reservado sin entrega guardada |

## 4. Componentes nuevos por capa

| Capa | Componentes nuevos | Cambios al código existente |
|------|--------------------|-----------------------------|
| **Entrada** (proyecto nuevo `Api`) | Punto de entrada HTTP, contratos de entrada y salida, validación, traducción de códigos de rechazo a respuestas HTTP, raíz de composición | Ninguno |
| **Aplicación** | Contrato de la bandeja de salida | Ajuste del caso de uso por AJ-01 a AJ-03 |
| **Dominio** | Ninguno | Ninguno: el dominio no debe cambiar para conectar la persistencia |
| **Infraestructura** | Repositorios con persistencia real, ejecutor de migraciones, bandeja de salida y su procesador, adaptador de avisos simulado con falla configurable, semilla de datos | Se conservan los adaptadores en memoria para las pruebas |
| **Pruebas** | Integración con persistencia real, integración del punto de entrada, pruebas de arquitectura | Las pruebas existentes se actualizan por los ajustes |

Que el dominio no cambie es la prueba de que el diseño de la Actividad 3 era correcto: si hubiera que tocarlo para guardar en una base de datos, la separación habría fallado.

## 5. Decisiones de implementación

Cada decisión se registrará en un ADR con sus alternativas y trade-offs.

| ID | Decisión | Alternativa descartada | Razón principal |
|----|----------|------------------------|-----------------|
| D4-01 | **Punto de entrada HTTP** (API mínima de ASP.NET Core) | Línea de comandos | Representa un punto de entrada real y permite probar el contrato completo |
| D4-02 | **Persistencia relacional con PostgreSQL**, ejecutada con contenedores | SQLite, almacenamiento en documentos | Es un motor realista para este tipo de plataforma, ofrece bloqueos de fila que permiten proteger la capacidad de la ruta bajo concurrencia (INV04) y se reproduce con un archivo de composición. El adaptador sigue siendo reemplazable sin tocar el dominio ni la aplicación |
| D4-03 | **Acceso a datos con SQL parametrizado** mediante el controlador del motor, sin ORM | ORM completo | Control explícito del SQL, de la concurrencia y de las migraciones, con menos dependencias |
| D4-04 | **Migraciones SQL versionadas** con un ejecutor propio y tabla de historial | Herramienta de migraciones externa | Mantiene el principio de migraciones reproducibles y versionadas sin agregar una herramienta para un esquema pequeño |
| D4-05 | **Estado durable con bandeja de salida**, sin event sourcing | Event sourcing | El estado actual es lo que consulta el negocio; el registro de eventos sirve de historial, no de fuente de verdad |
| D4-06 | **Procesamiento con productor y consumidor sobre la bandeja de salida**, con reintentos y mensajes fallidos | Envío directo sin reintentos | Una falla del sistema de notificaciones no debe perder el aviso ni deshacer la asignación |
| D4-07 | **Autenticación mínima por clave de API** | Sin autenticación, o autenticación completa de usuarios | Protege el punto de entrada con un costo bajo y deja documentada la limitación |
| D4-08 | **Observabilidad con registros estructurados, métricas y verificación de salud**, sin datos personales | Registros de texto libre | Permite diagnosticar el flujo sin exponer información sensible |
| D4-09 | **Pruebas de arquitectura automatizadas** como funciones de aptitud | Revisión manual de las dependencias | Las reglas de dependencia se verifican en cada ejecución de las pruebas |
| D4-10 | **Integración continua** que compila, prueba contra una base de datos real y revisa vulnerabilidades en cada cambio | Verificación manual | Evidencia reproducible de la calidad |
| D4-11 | **Un único tronco de trabajo**, con cambios pequeños y frecuentes | Ramas de larga vida | Coincide con la decisión de no usar ramas ya tomada en el proyecto |

## 6. Contrato de entrada (borrador)

| Elemento | Detalle |
|----------|---------|
| **Ruta** | `POST /api/v1/deliveries/{deliveryId}/assignment` |
| **Cuerpo** | Identificador del repartidor |
| **Encabezados** | Clave de API (obligatoria) y quién solicita la asignación |
| **Respuesta exitosa** | Entrega asignada, repartidor, momento de la asignación y si el aviso quedó encolado |
| **Respuesta de error** | Un cuerpo estándar con un código estable y un mensaje legible |
| **Versionado** | La versión va en la ruta |

La entrada solo valida la forma de los datos y los traduce al comando del caso de uso. **No contiene reglas de negocio.**

| Código de rechazo | Respuesta HTTP | Razón |
|-------------------|----------------|-------|
| (datos mal formados) | 400 | La solicitud no cumple el formato |
| (clave ausente o inválida) | 401 | No se puede identificar al solicitante |
| `DELIVERY_NOT_FOUND` | 404 | El recurso no existe |
| `DELIVERY_ALREADY_ASSIGNED` | 409 | Conflicto con el estado actual |
| `DELIVERY_NOT_PENDING` | 409 | Conflicto con el estado actual |
| `ROUTE_SATURATED` | 409 | Conflicto con la capacidad actual |
| `COURIER_NOT_ACTIVE` | 422 | La solicitud es válida, pero no procesable |
| `AVAILABILITY_UNVERIFIABLE` | 503 | Dependencia no disponible; se puede reintentar |

## 7. Modelo de datos (borrador)

Cada módulo tiene su propio **esquema** dentro de la base de datos, lo que deja visible a quién pertenece cada tabla y refuerza la separación de datos entre módulos.

| Tabla | Módulo | Contenido |
|-------|--------|-----------|
| `entregas.entrega` | Entregas | Entregas con su estado, repartidor asignado y número de versión |
| `ruteo.repartidor` | Ruteo | Repartidores y si están activos |
| `ruteo.ruta` | Ruteo | Rutas con su capacidad |
| `ruteo.parada` | Ruteo | Paradas reservadas por ruta, con vencimiento opcional |
| `transversal.bandeja_salida` | Transversal | Mensajes pendientes, enviados o fallidos, con intentos y último error |
| `transversal.historial_eventos` | Transversal | Historial de eventos de cada pedido |
| `public.historial_migraciones` | Infraestructura | Migraciones aplicadas y su verificación |

Ninguna tabla guarda nombres, direcciones o datos de contacto de los clientes en los mensajes de la bandeja de salida: solo identificadores.

## 8. Manejo de errores y fallos

El enunciado pide decidir, ante cada fallo, si se reintenta, se registra, se devuelve un mensaje claro o se falla de forma controlada.

| Situación | Reacción | Dónde |
|-----------|----------|-------|
| Entrega inexistente, ya asignada, no pendiente, repartidor inactivo o ruta saturada | **Mensaje claro**: rechazo con código estable, sin registro de error | Caso de uso y entrada |
| Datos mal formados | **Mensaje claro** con el detalle de la validación | Entrada |
| Dos asignaciones simultáneas | **Falla controlada**: una prevalece, la otra recibe un rechazo, y el cupo se libera | Caso de uso y repositorio |
| Falla al guardar | **Liberar el cupo y propagar** el error como falla inesperada | Caso de uso |
| El sistema de notificaciones no responde | **Reintentar** con espera creciente y, agotados los intentos, **dejar el mensaje como fallido** | Procesador de la bandeja de salida |
| Mensaje fallido tras los reintentos | **Registrar** y dejar visible para su revisión manual | Bandeja de salida |
| Falla del procesador a mitad de camino | Al reiniciar, **retoma** los mensajes pendientes | Procesador |
| Falla inesperada | **Registrar** con un identificador de correlación y responder con un mensaje genérico, sin detalles internos | Entrada |

## 9. Videos del curso: qué aplica y qué no

El enunciado pide seleccionar los videos de implementación según las decisiones tomadas, y justificar qué patrones o tecnologías aplican y cuáles no.

> Esta tabla se basa en los **títulos** de los videos, porque no he visto su contenido. Se ajustará cuando se revisen.

| Video | Tema | ¿Aplica? | Justificación |
|-------|------|----------|---------------|
| 3 | Monorepos con una herramienta específica | **No** | El proyecto ya es un único repositorio pequeño, y no se necesita una herramienta de construcción de monorepos |
| 4 | Desarrollo sobre un tronco con reglas en GitHub | **Sí, en parte** | Se aplica el flujo de un solo tronco (D4-11) y las comprobaciones automáticas en cada cambio (D4-10) |
| 14 | Migración gradual de arquitecturas | **No** | No hay un sistema anterior que migrar |
| 15 | Migraciones de base de datos | **Sí, el principio** | Se aplican migraciones versionadas y reproducibles (D4-04), con un ejecutor propio en lugar de la herramienta |
| 18 | Infraestructura como código | **Sí, en parte** | El entorno se reproduce con comandos versionados y con la integración continua; no hay servicios independientes que aprovisionar |
| 16 | Pasarela de API | **No** | Es un monolito con una sola entrada; una pasarela agregaría una pieza sin beneficio |
| 19 | Mensajes frente a eventos | **Sí** | El proyecto distingue comandos (reserva de cupo) de eventos (`DeliveryAssigned`) |
| 20 | Productor y consumidor | **Sí** | La bandeja de salida y su procesador (D4-06) |
| 21 | Cola de mensajes fallidos | **Sí** | Los mensajes que agotan sus reintentos quedan como fallidos (D4-06) |
| 22 | Consumidores comparados en tiempo real | **No** | El volumen supuesto no exige procesamiento en paralelo ni comparación de consumidores |
| 23 | Gestor de procesos | **No por ahora** | La coordinación de la asignación es corta y la orquesta el caso de uso; se reconsideraría si el flujo creciera |
| 24 | Estado durable frente a event sourcing | **Sí, como decisión** | Se elige estado durable con bandeja de salida (D4-05) |
| 25 | Máquinas de estados en el front-end | **No** | No hay interfaz; los estados de la entrega los controla el dominio |
| 8 | Agentes de IA que revisan el código | **Opcional** | Puede apoyar la revisión, pero no sustituye las pruebas ni el criterio propio |
| 11 y 12 | Pre-mortem y pruebas derivadas de riesgos | **Sí** | El registro de riesgos de esta actividad parte de un pre-mortem y cada riesgo importante tiene una prueba |
| 13 | Métricas para evaluar la arquitectura | **Sí** | Se definen métricas de dependencias y de calidad del código |
| 26 | Análisis de seguridad (SAST, DAST, pruebas de penetración) | **Sí, en parte** | Revisión de dependencias vulnerables y análisis estático del código; las pruebas de penetración quedan fuera de alcance |
| 27 | Funciones de aptitud | **Sí** | Las pruebas de arquitectura automatizadas (D4-09) |
| 28 | Observabilidad e ingeniería del caos | **Sí, en parte** | Registros y métricas (D4-08), y fallas inyectadas de forma controlada en las pruebas |
| 29 y 30 | Criterio y defensa final | **Sí** | Base de la sustentación |

## 10. Estrategia de pruebas

| Nivel | Qué comprueba | Cantidad prevista |
|-------|---------------|-------------------|
| **Unitarias del dominio** | Las invariantes, sin infraestructura | Las 22 existentes |
| **Del caso de uso con dobles** | La coordinación, con puertos simulados | Las 13 existentes, ajustadas por AJ-01 a AJ-03 |
| **De integración con persistencia real** | El flujo completo contra PostgreSQL, incluida la concurrencia. Se ejecutan cuando hay una base disponible (variable de entorno) y se omiten con un aviso cuando no la hay | Las 3 existentes, más las que cubran persistencia, migraciones y la bandeja de salida |
| **De integración del punto de entrada** | El contrato HTTP de extremo a extremo | Camino correcto y rechazos principales |
| **De arquitectura** | Las reglas de dependencia entre capas | Una por regla |

**Escenarios de error y de límite que se comprobarán:**

1. Asignación simultánea de la misma entrega (EA11) con persistencia real.
2. Dos entregas que compiten por el último cupo (EA12) con persistencia real.
3. Falla del sistema de notificaciones: el aviso se reintenta y, agotados los intentos, queda como fallido sin deshacer la asignación.
4. Reinicio del procesador con mensajes pendientes.
5. Clave de API ausente o inválida.

## 11. Observabilidad y seguridad (adelanto)

| Aspecto | Qué se prevé |
|---------|--------------|
| **Registros** | Estructurados, con el identificador de la entrega, el del repartidor y un identificador de correlación; sin direcciones, nombres ni datos de contacto |
| **Métricas** | Asignaciones por resultado, mensajes pendientes en la bandeja, mensajes fallidos y duración de la asignación |
| **Salud** | Un punto de verificación que confirma la conexión a la base de datos y el estado de las migraciones |
| **Seguridad** | Clave de API, consultas parametrizadas, validación de entrada, secretos fuera del repositorio y revisión de dependencias vulnerables |

El detalle, los riesgos y la evidencia de cada mitigación se documentarán en el registro de riesgos de esta actividad.

## 12. Plan de trabajo

| Paso | Contenido | Resultado verificable |
|------|-----------|-----------------------|
| 1 | Este plan y sus decisiones | Documento revisado |
| 2 | Persistencia con PostgreSQL: esquema, migraciones y repositorios | Pruebas de integración de los repositorios |
| 3 | Bandeja de salida y ajuste del caso de uso | Pruebas del flujo con eventos persistidos |
| 4 | Entrada HTTP, validación y composición | Pruebas del contrato HTTP |
| 5 | Procesador, reintentos y mensajes fallidos | Pruebas de fallas del sistema de notificaciones |
| 6 | Observabilidad y seguridad | Registros, métricas y revisión de riesgos |
| 7 | Pruebas de arquitectura e integración continua | Comprobaciones automáticas en cada cambio |
| 8 | Registro de riesgos, resultados y riesgos residuales | Documento de resultados |
| 9 | Expediente actualizado: README, `Architecture.md`, C4 y ADR | Trazabilidad completa |
| 10 | Video de demostración y defensa final | Guion y grabación |

Cada paso termina con una compilación y una ejecución de las pruebas, y con su propio commit.

## 13. Trazabilidad entre el diseño y la implementación

Esta tabla se completa a medida que se construye.

| Elemento del diseño | Documento | Implementación | Estado |
|---------------------|-----------|----------------|--------|
| `Delivery` y sus invariantes | `03-modelo-de-dominio.md` | `src/Domain/Entregas` | Hecho |
| Caso de uso `AssignCourier` | `06-casos-de-uso-puertos-adaptadores.md` | `src/Application/Entregas` | Hecho, por ajustar (AJ-01 a AJ-03) |
| Repositorio de entregas | Puerto `IDeliveryRepository` | Persistencia real | Pendiente |
| Consulta y reserva en Ruteo | Contratos C2 y C3 | Persistencia real | Pendiente |
| Evento `DeliveryAssigned` | Contrato C4 | Bandeja de salida | Pendiente |
| Solicitud de aviso | Contrato C5 | Procesador y adaptador | Pendiente |
| Entrada del coordinador | Contrato de aplicación | Punto de entrada HTTP | Pendiente |
| Escenarios EA01 a EA15 | `04-invariantes-y-escenarios.md` | Pruebas | Parcial: 38 pruebas existentes |

## 14. Por confirmar

1. Si el equipo dispone de Docker. Si no, se puede usar un PostgreSQL instalado localmente con la misma cadena de conexión.
2. Si el docente espera una herramienta de migraciones específica.
3. Si la solución debe poder ejecutarse con contenedores.
4. El tiempo de espera y el número de reintentos de los avisos (supuestos: tres intentos con espera creciente).
5. Si las reservas de cupo deben vencer y tras cuánto tiempo (AJ-06).
