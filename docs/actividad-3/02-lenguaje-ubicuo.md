# Lenguaje ubicuo

## 1. Introducción

El lenguaje ubicuo es el vocabulario común que comparten el negocio y el equipo técnico. Cada término tiene **un significado preciso dentro de un contexto**, y ese mismo término aparece sin cambios en las conversaciones, los documentos, los escenarios de aceptación y el código.

Este glosario cubre el flujo **asignar un repartidor a una entrega** (`01-flujo-de-negocio.md`). Extiende el glosario de `docs/actividad-2/04-comparacion-de-alternativas.md`, que ya mostraba que una palabra como "disponible" cambia de significado entre dominios.

> Los tipos indicados (entidad, objeto de valor, etc.) son **provisionales**. Se confirman en `03-modelo-de-dominio.md`.

## 2. Reglas del lenguaje

| Regla | Detalle |
|-------|---------|
| **Un término, un significado por contexto** | Si el significado cambia entre contextos, se registra de forma explícita (sección 5) |
| **Español en los documentos, inglés en el código** | Cada término en español tiene un nombre fijo en código, que figura en el glosario |
| **El mismo término en todas partes** | Escenarios, pruebas, diagramas y código usan los nombres del glosario, sin sinónimos |
| **Sin abreviaturas** | Se escribe `DeliveryAddress`, no `DelAddr` |
| **Eventos en pasado** | "Entrega asignada", no "asignar entrega" |
| **Acciones como verbos de negocio** | `AssignCourier`, no `SetCourierId` |

## 3. Glosario por contexto

### 3.1 Contexto Entregas (dueño de la entrega)

| Término | Nombre en código | Definición | Tipo provisional |
|---------|------------------|------------|------------------|
| **Entrega** | `Delivery` | Compromiso de llevar un pedido confirmado a una dirección, dentro de una ventana, con un repartidor asignado. Tiene identidad propia | Entidad, raíz del agregado |
| **Identificador de entrega** | `DeliveryId` | Valor único que distingue una entrega de otra | Objeto de valor |
| **Dirección de entrega** | `DeliveryAddress` | Lugar donde se entrega, validado y normalizado | Objeto de valor |
| **Ventana de entrega** | `DeliveryWindow` | Intervalo de fecha y hora prometido al cliente | Objeto de valor |
| **Prioridad de entrega** | `DeliveryPriority` | Grado de urgencia de la entrega (por ejemplo, normal o urgente) | Objeto de valor |
| **Estado de la entrega** | `DeliveryStatus` | Situación actual de la entrega en su ciclo de vida: pendiente de asignación, asignada, en camino, entregada, fallida o cancelada | Objeto de valor (enumeración) |
| **Pendiente de asignación** | `PendingAssignment` | Estado de una entrega confirmada que aún no tiene repartidor | Estado |
| **Asignada** | `Assigned` | Estado de una entrega que tiene un repartidor activo | Estado |
| **Asignar repartidor** | `AssignCourier` | Acción del negocio que da a una entrega un repartidor, si se cumplen las reglas | Comando / método de la entidad |
| **Repartidor asignado** | `CourierId` (en `Delivery`) | Referencia al repartidor que tiene la entrega. La entrega solo conoce su identificador, no sus datos | Referencia por identificador |
| **Entrega asignada** | `DeliveryAssigned` | Hecho ocurrido: a una entrega se le asignó un repartidor | Evento de dominio |

### 3.2 Contexto Ruteo (dueño de la disponibilidad y la capacidad)

| Término | Nombre en código | Definición | Tipo provisional |
|---------|------------------|------------|------------------|
| **Repartidor** | `Courier` | Persona que realiza entregas. En Ruteo se modela como un recurso con jornada y capacidad | Entidad |
| **Identificador de repartidor** | `CourierId` | Valor único que distingue a un repartidor | Objeto de valor |
| **Repartidor activo** | `ActiveCourier` | Repartidor con alta vigente, no suspendido ni retirado | Estado / condición |
| **Ruta** | `Route` | Conjunto ordenado de paradas que un repartidor cubre en una jornada | Entidad |
| **Parada** | `Stop` | Cada punto de entrega dentro de una ruta. Corresponde a una entrega | Objeto de valor |
| **Capacidad de ruta** | `RouteCapacity` | Máximo de paradas que una ruta admite (supuesto A3-S02) | Objeto de valor |
| **Ruta saturada** | `SaturatedRoute` | Ruta cuyo número de paradas ya alcanzó su capacidad | Condición calculada |
| **Disponibilidad del repartidor** | `CourierAvailability` | Respuesta de Ruteo sobre si un repartidor puede recibir otra entrega: activo, en jornada y con ruta no saturada | Objeto de valor |

### 3.3 Contextos Pedidos e Inventario (aguas arriba del flujo)

| Término | Nombre en código | Definición | Tipo provisional |
|---------|------------------|------------|------------------|
| **Pedido** | `Order` | Solicitud de compra de un cliente, con sus líneas y su estado | Entidad, raíz del agregado |
| **Pedido confirmado** | `ConfirmedOrder` | Pedido con pago verificado e inventario reservado, listo para despacharse | Estado |
| **Reserva de inventario** | `StockReservation` | Apartado de unidades de un producto para un pedido | Entidad |

Estos contextos **crean la necesidad** de la entrega, pero no participan en el flujo de asignación. Se mencionan para dejar claro de dónde viene la entrega.

### 3.4 Capacidades transversales

| Término | Nombre en código | Definición | Tipo provisional |
|---------|------------------|------------|------------------|
| **Historial de eventos** | `EventLog` | Registro cronológico de todo lo que ocurre con un pedido: quién, qué y cuándo | Servicio / registro |
| **Notificación** | `Notification` | Aviso que la plataforma pide enviar a una persona sobre un cambio | Concepto de aplicación |
| **Coordinador de despacho** | `DispatchCoordinator` | Persona que decide qué repartidor lleva cada entrega | Rol (actor) |

## 4. Resumen de tipos

| Tipo | Términos |
|------|----------|
| **Entidades** | `Delivery`, `Courier`, `Route`, `Order`, `StockReservation` |
| **Objetos de valor** | `DeliveryId`, `CourierId`, `DeliveryAddress`, `DeliveryWindow`, `DeliveryPriority`, `DeliveryStatus`, `Stop`, `RouteCapacity`, `CourierAvailability` |
| **Eventos de dominio** | `DeliveryAssigned` |
| **Acciones** | `AssignCourier` |
| **Roles** | `DispatchCoordinator` |

La distinción entre entidad y objeto de valor se justifica en `03-modelo-de-dominio.md`.

## 5. Términos que significan cosas distintas según el contexto

Las mismas palabras aparecen en varios contextos con significados que no se pueden mezclar. Esta es la razón de existir de los límites de contexto.

| Término | Pedidos | Inventario | Ruteo | Entregas |
|---------|---------|------------|-------|----------|
| **Disponible** | El producto puede ofrecerse y el pedido puede confirmarse | Existencias menos reservas y bloqueos de un punto | Repartidor activo, en jornada y con ruta no saturada | No aplica; Entregas no decide la disponibilidad, la consulta a Ruteo |
| **Asignado** | Tiene responsable de despacho | Unidades reservadas para ese pedido | Parada incluida en la ruta de un repartidor | La entrega tiene un repartidor activo (`Assigned`) |
| **Activo** | Pedido vigente, no cancelado ni cerrado | No aplica | Repartidor con alta vigente | Repartidor asignado que no ha sido reemplazado |
| **Cancelado** | Pedido anulado | Reserva liberada | Parada retirada de la ruta | Entrega anulada |
| **Estado** | Ciclo de vida del pedido | Situación del stock | Situación de la ruta | Ciclo de vida de la entrega (`DeliveryStatus`) |

**Regla:** cada contexto usa su propia definición. Cuando uno necesita información de otro, la pide por un contrato (ver `05-contextos-y-contratos.md`) en vez de adoptar su significado.

## 6. Términos ambiguos por confirmar

| Término | Ambigüedad | Decisión provisional | A quién confirmar |
|---------|------------|----------------------|-------------------|
| **Ruta saturada** | ¿Se mide en número de paradas, tiempo o carga? | Número de paradas (A3-S02) | Operaciones y logística |
| **Repartidor disponible** | ¿Incluye estar en jornada, tener vehículo y no estar en descanso? | Activo, en jornada y con ruta no saturada | Operaciones |
| **Repartidor activo** | ¿Qué lo vuelve inactivo: suspensión, vacaciones, baja? | Con alta vigente y sin suspensión | Recursos humanos / operaciones |
| **Entrega y pedido** | ¿Un pedido genera una sola entrega o puede dividirse en varias? | Una entrega por pedido | Negocio |
| **Quién crea la entrega** | ¿Pedidos al confirmar o Entregas por su cuenta? | Entregas la crea al recibir un pedido confirmado | Negocio y tecnología |
| **Asignada y en camino** | ¿Cuándo pasa de una a otra: al salir el repartidor o al iniciar la ruta? | En camino empieza cuando el repartidor inicia la ruta | Operaciones |
| **Ventana de entrega** | ¿Es un intervalo fijo o flexible? | Intervalo con inicio y fin | Negocio |
| **Urgente** | ¿Qué hace que una entrega sea urgente y quién lo decide? | Se define al crear la entrega | Negocio |

## 7. Relación con los documentos anteriores

| Documento | Cómo se relaciona |
|-----------|-------------------|
| `docs/actividad-1/02-actores-y-necesidades.md` | Aporta los actores: coordinador de despacho, repartidor, cliente |
| `docs/actividad-2/01-catalogo-de-requisitos.md` | Define los dominios (Pedidos, Inventario, Ruteo, Entregas) |
| `docs/actividad-2/04-comparacion-de-alternativas.md` | Contiene el glosario inicial de "disponible", "asignado", "entregado" y "cancelado" |
| `01-flujo-de-negocio.md` | Define el flujo del que salen los términos |

## 8. Qué falta confirmar

1. Las ocho ambigüedades de la sección 6.
2. Si el negocio usa nombres propios para algunos conceptos (por ejemplo, "ruta" o "carga"), para adoptarlos en lugar de los provisionales.
3. Si los estados de la entrega coinciden con los que el negocio reconoce hoy.
