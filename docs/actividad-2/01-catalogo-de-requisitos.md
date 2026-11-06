# Catálogo de requisitos

## 1. Introducción

Este catálogo traduce el diagnóstico de la Actividad 1 (problema, actores, alcance y riesgos) en **requisitos** que orientan la decisión arquitectónica. Todavía no se eligen tecnologías: se describe **qué debe hacer** el sistema y **qué cualidades debe tener**.

Cada requisito indica su origen en la Actividad 1, de modo que se pueda comprobar que ninguno aparece sin una necesidad detrás.

| Tipo | Pregunta que responde |
|------|-----------------------|
| **Requisito funcional (RF)** | ¿Qué debe hacer el sistema? |
| **Requisito no funcional (RNF)** | ¿Qué cualidades debe tener al hacerlo? |
| **Restricción (RC)** | ¿Qué límites no se pueden cambiar? |


## 2. Punto de partida: lo que hereda la Actividad 2

| Elemento de la Actividad 1 | Cómo condiciona los requisitos |
|----------------------------|--------------------------------|
| **Problema:** no hay vista única del estado real de cada pedido | Se necesita un estado común y un historial de eventos |
| **Alcance:** coordinar, rastrear y dar trazabilidad en la última milla | Los requisitos se limitan a ese perímetro |
| **Fuera de alcance:** pagos, catálogo, ventas, operación de bodega, mapas, envío de mensajes | Esas capacidades se consumen, no se construyen |
| **Riesgos críticos:** inventario (R02), entregas (R03) y fuente de la verdad (R07) | Condicionan los atributos de calidad más exigentes |
| **Decisión aplazada D12:** quién es la fuente de la verdad del inventario | Se trata como condicionante abierta (ver sección 6) |

### Dominios del sistema

Para ordenar los requisitos se agrupan en contextos con significado y reglas propias. Una misma palabra puede significar cosas distintas en cada uno (por ejemplo, "disponible" para inventario no es lo mismo que para pedidos o entregas).

| Dominio | Qué protege | Requisitos funcionales |
|---------|-------------|------------------------|
| **Pedidos** | Registro, estado y ciclo de vida del pedido | RF01, RF02, RF06, RF07, RF13, RF20 |
| **Inventario** | Disponibilidad y reservas de stock | RF03, RF04, RF05 |
| **Ruteo** | Asignación de pedidos a rutas y repartidores | RF08, RF09, RF10 |
| **Entregas** | Ejecución y evidencia de la entrega | RF11, RF12 |
| **Incidencias** | Reclamos, fallas y su resolución | RF14 |
| **Transversal** | Historial, indicadores, accesos, notificaciones e integración con el ERP/CRM | RF15, RF16, RF17, RF18, RF19 |

## 3. Requisitos funcionales

| ID | Requisito | Actor principal | Origen en la Actividad 1 |
|----|-----------|-----------------|--------------------------|
| RF01 | Registrar y consolidar los pedidos de todos los canales con un identificador único | Canales de venta, agente telefónico, operador de tienda | Alcance R1; Riesgo R01 |
| RF02 | Detectar posibles pedidos duplicados antes de confirmarlos | Plataforma | Alcance R2; Riesgo R01 |
| RF03 | Consultar la disponibilidad de inventario consolidada por ubicación | Almacén, operador de tienda | Alcance R3; Riesgo R02 |
| RF04 | Reservar inventario al confirmar el pedido | Plataforma, almacén | Alcance R3; Riesgos R02 y R07 |
| RF05 | Recibir y aplicar actualizaciones de stock desde almacenes y tiendas | Almacén, operador de tienda | Riesgo R02 |
| RF06 | Consultar el estado del pago y no despachar pedidos sin pago confirmado | Sistema de pagos | Alcance R12; variables del análisis |
| RF07 | Mantener un estado común del pedido con un ciclo de vida compartido por todas las áreas | Todos los actores | Alcance R4; Riesgos R04 y R05 |
| RF08 | Validar y normalizar la dirección de entrega | Cliente, geolocalización | Riesgo R11 |
| RF09 | Asignar pedidos a rutas y repartidores | Coordinador de despacho | Alcance R5; Riesgos R03 y R05 |
| RF10 | Reasignar entregas ante imprevistos | Coordinador de despacho | Alcance R5; Riesgo R03 |
| RF11 | Registrar estado, ubicación y evidencia de entrega desde el campo | Repartidor | Alcance R6; Riesgo R10 |
| RF12 | Gestionar entregas fallidas y reintentos | Repartidor, soporte | Riesgo R03 |
| RF13 | Consultar el estado y el historial completo de un pedido | Soporte (cliente, por confirmar) | Alcance R4 y R7; supuesto S14 |
| RF14 | Registrar y gestionar incidencias hasta su cierre | Soporte al cliente | Alcance R7 |
| RF15 | Solicitar notificaciones al cliente ante cambios relevantes del pedido | Sistema de notificaciones | Alcance R8 |
| RF16 | Registrar el historial de eventos de cada pedido (quién, qué, cuándo) | Todos los actores | Alcance R9; Riesgo R04 |
| RF17 | Ofrecer indicadores operativos básicos | Gerente de operaciones | Alcance R10 |
| RF18 | Controlar el acceso por rol a la información y a las acciones | Administrador de la plataforma | Alcance R11; Riesgo R09 |
| RF19 | Intercambiar con el ERP/CRM los movimientos y los datos maestros | ERP/CRM | Alcance R12; Riesgo R07 |
| RF20 | Cancelar o modificar un pedido antes de su despacho | Cliente, soporte | Requisito nuevo (supuesto, ver sección 7) |


## 4. Requisitos no funcionales (atributos de calidad)

| ID | Atributo | Descripción | Origen en la Actividad 1 |
|----|----------|-------------|--------------------------|
| RNF01 | Rendimiento | Soporte y operación deben consultar el estado de un pedido con rapidez | Riesgo R05; actor soporte |
| RNF02 | Disponibilidad | La operación de despacho y entrega no debe detenerse por una falla de la plataforma | Riesgo R14 |
| RNF03 | Consistencia de datos | El inventario no debe permitir vender lo que no existe | Riesgo R02 |
| RNF04 | Seguridad y privacidad | Los datos personales se protegen y el acceso depende del rol | Riesgo R09 |
| RNF05 | Trazabilidad y auditabilidad | Cada cambio de estado queda registrado y puede consultarse | Riesgo R04; decisión D05 |
| RNF06 | Escalabilidad | La plataforma absorbe picos de demanda sin perder control | Riesgo R06 |
| RNF07 | Mantenibilidad y facilidad de evolución | Las reglas de negocio pueden cambiar sin reescribir todo el sistema | Riesgo R13; decisión D10 |
| RNF08 | Interoperabilidad | La integración con el ERP/CRM y los sistemas externos se hace con contratos claros y versionados, de modo que un cambio en un lado no rompa al otro | Alcance R12; Riesgo R07 |
| RNF09 | Resiliencia ante terceros | Una falla de pagos, geolocalización o notificaciones no detiene toda la operación | Riesgo R08 |
| RNF10 | Tolerancia a conectividad intermitente | Los repartidores pueden reportar con retraso sin perder información | Riesgo R10 |
| RNF11 | Usabilidad | Los flujos de tienda, almacén y repartidor son simples y rápidos de aprender | Riesgo R15 |
| RNF12 | Observabilidad | Se pueden detectar pedidos atascados o integraciones fallando | Riesgos R03 y R04 |
| RNF13 | Operabilidad y costo | El sistema puede desplegarse, monitorearse y mantenerse con los recursos disponibles | Decisión D10; supuestos de equipo |

## 5. Restricciones

| ID | Restricción | Origen |
|----|-------------|--------|
| RC01 | La plataforma debe convivir con el ERP/CRM existente; no lo reemplaza | Decisión D06 |
| RC02 | Los datos personales se rigen por la normativa local aplicable *(por confirmar)* | Supuesto S09 |
| RC03 | La adopción es gradual, con convivencia temporal con los procesos actuales | Decisión D10 |
| RC04 | Pagos, geolocalización y notificaciones se consumen como servicios externos | Decisión D07 |
| RC05 | Los repartidores trabajan en campo con conectividad variable | Supuesto S08; Riesgo R10 |
| RC06 | Plazos, presupuesto y tamaño del equipo son desconocidos y deben asumirse | Actividad 1, sección "Lo que aún no se sabe" |
| RC07 | No se eligen tecnologías ni productos concretos en esta actividad | Decisión D11 |

## 6. Condicionante abierto: la fuente de la verdad

La decisión **D12** de la Actividad 1 dejó abierta la pregunta de si el ERP/CRM o la plataforma es la fuente de la verdad del inventario y del estado del pedido.

Esa pregunta afecta directamente a RF03, RF04, RF05 y RF19, y a los atributos RNF03 y RNF08. En esta actividad se trabaja con la siguiente **hipótesis provisional**, que deberá confirmarse con el negocio:

> La plataforma es la fuente de la verdad del **estado del pedido y de las reservas de inventario**; el ERP/CRM mantiene los **datos maestros y las existencias oficiales**, y ambos se sincronizan.

Si la hipótesis resulta falsa, se revisan los requisitos mencionados.

## 7. Supuestos nuevos de esta actividad

| ID | Supuesto | Si resulta falso |
|----|----------|------------------|
| A2-S01 | El equipo de desarrollo y operación es pequeño | Una estructura más distribuida sería viable |
| A2-S02 | El volumen de pedidos es moderado, con picos en fechas promocionales y crecimiento gradual | Habría que priorizar la escalabilidad horizontal desde el inicio |
| A2-S03 | No existe hoy una capacidad de operar muchos servicios independientes | El costo operativo de una estructura distribuida sería menor |
| A2-S04 | RF20 (cancelar o modificar pedidos) es necesario aunque el caso no lo menciona | Se elimina el requisito |
| A2-S05 | Los requisitos se priorizan con criterio de negocio, sin datos reales de la empresa | Se recalcula la priorización |

## 8. Lo que falta confirmar

1. ¿Quién es la fuente de la verdad del inventario? (D12)
2. ¿El cliente consulta su pedido directamente o solo recibe avisos? (afecta RF13)
3. ¿Cuántos pedidos por hora y por día se esperan, y cuál es el pico?
4. ¿Cuál es el tamaño del equipo y el presupuesto?
5. ¿Qué normativa de datos personales aplica?
