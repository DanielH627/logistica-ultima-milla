# Flujo de negocio: asignar un repartidor a una entrega

## 1. Introducción

La Actividad 3 pide diseñar el modelo de dominio y los límites de la solución a partir de **un flujo de negocio acotado**. Este documento elige ese flujo, describe sus actores, su propósito y su resultado esperado, y fija qué queda dentro y fuera del diseño.

Todo parte de lo ya decidido: la plataforma es un **monolito modular** con módulos por dominio (Pedidos, Inventario, Ruteo, Entregas e Incidencias) y límites explícitos (`docs/actividad-2/adr/ADR-001-estructura-de-la-plataforma.md`).

## 2. Flujo elegido

**Asignar un repartidor a una entrega** (caso de uso `AssignCourier`).

### Por qué este flujo

| Razón | Detalle |
|-------|---------|
| **Es el núcleo de la coordinación** | Corresponde a RF09 (asignar pedidos a rutas y repartidores) y ataca los riesgos R03 (entregas retrasadas) y R05 (mala coordinación) |
| **Tiene reglas de negocio claras** | Una entrega no puede tener dos repartidores activos, y un repartidor con la ruta saturada no puede recibir más |
| **Cruza dos contextos** | Entregas (dueño de la entrega) y Ruteo (disponibilidad y capacidad), lo que permite practicar límites y contratos |
| **Obliga a separar negocio de infraestructura** | Para decidir necesita datos externos (disponibilidad, estimaciones de ruta) que deben entrar por puertos |
| **Es acotado** | Se puede modelar completo sin diseñar toda la plataforma |
| **Está ligado al diseño ya decidido** | Aplica las reglas 2 y 5 del ADR-001: dependencias por interfaces y Ruteo aislado |

## 3. Propósito y resultado esperado

**Propósito:** que cada entrega confirmada quede asignada a un repartidor concreto que pueda cumplirla, sin conflictos ni duplicidades.

**Resultado esperado:**

- La entrega queda con **un único repartidor activo** y pasa al estado *Asignada*.
- El cambio queda **registrado en el historial** de eventos del pedido (quién, qué y cuándo).
- El repartidor y, si corresponde, el cliente pueden ser **informados** a través del sistema de notificaciones.
- Si la asignación no es posible, la entrega **no cambia** y el coordinador recibe el **motivo**.

## 4. Actores

| Actor | Rol en el flujo | Qué necesita |
|-------|-----------------|--------------|
| **Coordinador de despacho** | Inicia la asignación | Saber si pudo asignar y, si no, por qué |
| **Plataforma** (módulos Entregas y Ruteo) | Valida y registra la asignación | Reglas claras y datos de disponibilidad confiables |
| **Repartidor** | Recibe la entrega asignada | Que la entrega aparezca en su lista y ruta |
| **Servicio de geolocalización** (sistema externo) | Informa estimaciones de tiempo y distancia | Direcciones y puntos de la ruta |
| **Sistema de notificaciones** (sistema externo) | Avisa de la asignación | Evento, destinatario y contenido |
| **Cliente** (indirecto) | Recibe el aviso de que su pedido ya tiene repartidor | Información sobre su entrega |

El servicio de geolocalización y el sistema de notificaciones **no se implementan** en esta actividad: se representan mediante puertos (ver el paso de casos de uso, puertos y adaptadores).

## 5. Precondiciones

Para poder asignar, deben cumplirse antes:

1. El pedido está **confirmado**, con el pago verificado y el inventario reservado (dominio Pedidos e Inventario).
2. Existe una **entrega** creada para ese pedido, en estado *Pendiente de asignación*.
3. Existe al menos un **repartidor activo** en el sistema.

## 6. Flujo principal

| Paso | Acción | Quién |
|------|--------|-------|
| 1 | El coordinador elige una entrega pendiente y un repartidor candidato | Coordinador |
| 2 | La plataforma carga la entrega | Plataforma |
| 3 | Verifica que la entrega exista y que no tenga repartidor activo | Plataforma |
| 4 | Verifica que la entrega esté en un estado que permita asignar | Plataforma |
| 5 | Consulta la disponibilidad del repartidor y la capacidad de su ruta, a través de un puerto | Plataforma |
| 6 | La entrega registra la asignación, protegiendo sus reglas | Plataforma (dominio) |
| 7 | Se guarda el resultado | Plataforma |
| 8 | Se registra el evento "entrega asignada" y se solicita el aviso al repartidor | Plataforma |
| 9 | Se informa al coordinador del resultado | Plataforma |

## 7. Flujos alternos y excepciones

| ID | Situación | Comportamiento esperado |
|----|-----------|-------------------------|
| A1 | La entrega no existe | Se rechaza con el motivo "entrega no encontrada"; no cambia nada |
| A2 | La entrega ya tiene un repartidor activo | Se rechaza; para cambiarlo se usa la reasignación, que es otro flujo |
| A3 | La entrega no está pendiente (cancelada, entregada o fallida) | Se rechaza con el estado actual |
| A4 | El repartidor no existe o no está activo | Se rechaza con el motivo |
| A5 | La ruta del repartidor está saturada | Se rechaza; el coordinador elige otro repartidor |
| A6 | El servicio que informa la disponibilidad no responde | **No se asigna a ciegas**: la entrega sigue pendiente, se informa que no se pudo verificar y se puede reintentar |
| A7 | Dos coordinadores intentan asignar la misma entrega a la vez | Solo una asignación prevalece; la otra recibe el rechazo A2 |

A6 y A7 son decisiones de diseño que se justifican en el ADR de esta actividad.

## 8. Postcondiciones

| Resultado | Qué queda |
|-----------|-----------|
| **Éxito** | Entrega con un repartidor activo, estado *Asignada*, evento registrado y aviso solicitado |
| **Rechazo** | Entrega sin cambios, motivo informado al coordinador |

## 9. Reglas de negocio candidatas

Se formalizan como invariantes en el paso siguiente del modelo.

1. Una entrega **no puede tener más de un repartidor activo**.
2. Una entrega **solo puede asignarse** si está pendiente de asignación.
3. **No se puede asignar** a un repartidor inactivo.
4. **No se puede asignar** a un repartidor cuya ruta ya está saturada.
5. Una entrega **no puede estar en dos estados contradictorios** a la vez.
6. Toda asignación queda **registrada** con quién, qué y cuándo.

## 10. Dentro y fuera de este diseño

| Dentro | Fuera |
|--------|-------|
| Modelo de la entrega, el repartidor y la ruta | Cálculo de la ruta óptima |
| Reglas e invariantes de la asignación | Reasignación y cancelación (otros flujos) |
| Caso de uso `AssignCourier` con sus puertos | Asignación automática por algoritmo |
| Contratos entre Entregas y Ruteo | Implementación real de la persistencia |
| Escenarios de aceptación | Integración real con geolocalización y notificaciones |
| Qué se publica al registrar la asignación | Pantallas o interfaz del coordinador |

La implementación completa del flujo y de sus integraciones corresponde a la Actividad 4.

## 11. Relación con las actividades anteriores

| Elemento | Referencia |
|----------|------------|
| Requisitos | RF09 (asignar), RF16 (historial), RF15 (notificar), RNF07 (evolución) |
| Riesgos | R03 (entregas retrasadas), R05 (mala coordinación) |
| Escenarios de calidad | QS07 (asignación bajo carga), QS13 (cambio de regla de ruteo) |
| Reglas del ADR-001 | Regla 2 (dependencias por interfaces), regla 4 (adaptadores) y regla 5 (Ruteo aislado) |
| Criterios verificables | CE02 y CE05 |

## 12. Supuestos nuevos

| ID | Supuesto | Si resulta falso |
|----|----------|------------------|
| A3-S01 | La asignación la hace un coordinador de despacho; no hay asignación automática en esta etapa | Cambia el actor y se agrega la lógica de selección |
| A3-S02 | La capacidad de una ruta se expresa como un máximo de paradas | Habría que modelar otras medidas, como tiempo o peso |
| A3-S03 | Un repartidor tiene como máximo una ruta activa a la vez | Cambia el concepto de ruta saturada |
| A3-S04 | Ante la falla del servicio de disponibilidad no se asigna (comportamiento seguro) | Se podría asignar de forma provisional y validar después |
| A3-S05 | La entrega se crea cuando el pedido está confirmado, con pago e inventario resueltos | Cambia la precondición y el contexto que crea la entrega |

## 13. Por confirmar

1. ¿Qué significa exactamente que una ruta esté **saturada**: número de paradas, tiempo o carga?
2. ¿Qué significa que un repartidor esté **disponible** para el contexto de Entregas y para el de Ruteo? (puede ser distinto)
3. ¿Quién crea la entrega: el módulo Pedidos al confirmar o el de Entregas por su cuenta?
4. ¿Debe avisarse al cliente cuando se asigna el repartidor, o solo al cambiar a "en camino"?
5. ¿Cuántos coordinadores operan a la vez, para dimensionar el riesgo de asignaciones simultáneas?
