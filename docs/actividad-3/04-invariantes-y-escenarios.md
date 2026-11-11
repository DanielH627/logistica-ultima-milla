# Invariantes y escenarios de aceptación

## 1. Introducción

Una invariante es una regla que debe cumplirse en todo momento, sin importar quién utilice el sistema ni por qué canal lo haga. Debido a esto, su protección no puede depender de la interfaz ni de la base de datos, sino del propio modelo de dominio. Por su parte, un escenario de aceptación describe una situación concreta mediante el formato Dado-Cuando-Entonces, con el fin de comprobar que la regla efectivamente se cumple.

En el caso de Mercadeo Compartido, estas reglas buscan evitar las asignaciones duplicadas y las rutas sobrecargadas, situaciones que hoy se traducen en entregas retrasadas o fallidas y en reprocesos entre el almacén, el despacho y soporte.

Este documento formaliza las reglas candidatas de `01-flujo-de-negocio.md` sobre el modelo de `03-modelo-de-dominio.md` y las convierte en escenarios verificables, utilizando el vocabulario de `02-lenguaje-ubicuo.md`.

## 2. Tipos de regla

No toda regla es una invariante de dominio. Distinguirlas permite ubicar cada una en la capa que le corresponde y evitar que la lógica del negocio se disperse.

| Tipo | Qué es | Quién la protege |
|------|--------|------------------|
| **Invariante de dominio** | Se cumple siempre dentro del modelo, aunque cambie la interfaz o la base de datos | Entidades y objetos de valor |
| **Precondición** | Algo que debe existir antes de ejecutar el caso de uso | Capa de aplicación |
| **Política de aplicación** | Decisión sobre cómo se comporta el flujo ante situaciones externas | Capa de aplicación |

## 3. Invariantes y reglas

| ID | Regla | Tipo | La protege | Si se viola |
|----|-------|------|------------|-------------|
| **INV01** | Una entrega no puede tener más de un repartidor activo | Invariante de dominio | `Delivery` | Se rechaza la asignación |
| **INV02** | Una entrega solo puede asignarse si está *Pendiente de asignación* | Invariante de dominio | `Delivery` | Se rechaza e informa el estado actual |
| **INV03** | El estado de la entrega y su repartidor son coherentes: una entrega *Pendiente de asignación* no tiene repartidor, y una entrega *Asignada* o *En camino* siempre lo tiene. Las entregas *Entregada*, *Fallida* y *Cancelada* conservan el último repartidor como registro histórico, y este se retira cuando la entrega vuelve a *Pendiente de asignación* | Invariante de dominio | `Delivery` | La entrega no llega a ese estado |
| **INV04** | Una ruta no puede superar su capacidad, sin excepción alguna, incluso para entregas urgentes | Invariante de dominio | `Route` | Se rechaza por ruta saturada |
| **INV05** | No se asigna una entrega a un repartidor inactivo | Regla de disponibilidad | Ruteo (`Courier`) y su consulta | Se rechaza e informa el motivo |
| **INV06** | Toda asignación deja un registro con quién, qué y cuándo | Invariante de dominio y de aplicación | `Delivery` genera el evento y la aplicación lo registra | La asignación no se considera completa |
| **INV07** | Los objetos de valor no pueden existir en un estado inválido | Invariante de dominio | Cada objeto de valor | No se puede crear el valor |
| **PRE01** | La entrega debe existir | Precondición | Aplicación | Se rechaza por entrega no encontrada |
| **POL01** | No se asigna una entrega sin poder verificar la disponibilidad del repartidor | Política de aplicación | Aplicación | Se rechaza e informa que no fue posible verificar |

## 4. Dónde vive cada regla

| Capa | Qué protege | Reglas |
|------|-------------|--------|
| **Dominio** | Lo que siempre debe ser verdad dentro del modelo | INV01, INV02, INV03, INV04, INV07 |
| **Aplicación** | Coordinar el caso de uso, consultar los puertos y decidir ante fallas | PRE01, POL01 y parte de INV05 e INV06 |
| **Infraestructura** | No decide reglas de negocio; ofrece datos y ejecuta efectos | Ninguna |

La infraestructura únicamente apoya el cumplimiento de las reglas, por ejemplo garantizando que dos escrituras simultáneas no se sobrescriban entre sí mediante un control de versión. Las reglas no se duplican en la base de datos ni en la interfaz.

## 5. Mensajes de rechazo

Cada rechazo tiene un código estable, pensado para el contrato de la aplicación, y un mensaje dirigido al coordinador. El mensaje explica lo ocurrido e indica qué puede hacer a continuación, ya que un rechazo que solo informa el error obliga al coordinador a buscar la causa por su cuenta.

| Código | Mensaje al coordinador | Regla |
|--------|------------------------|-------|
| `DELIVERY_ALREADY_ASSIGNED` | La entrega ya tiene asignado al repartidor {nombre}. Para cambiarlo debe solicitar una reasignación. | INV01 |
| `DELIVERY_NOT_PENDING` | La entrega se encuentra en estado {estado}. Solo se pueden asignar entregas pendientes de asignación. | INV02 |
| `ROUTE_SATURATED` | La ruta de {nombre} ya alcanzó su capacidad de {n} paradas. Seleccione otro repartidor. | INV04 |
| `COURIER_NOT_ACTIVE` | El repartidor {nombre} no se encuentra activo. Seleccione otro repartidor. | INV05 |
| `DELIVERY_NOT_FOUND` | No se encontró la entrega {código}. Verifique el código e inténtelo de nuevo. | PRE01 |
| `AVAILABILITY_UNVERIFIABLE` | No fue posible verificar la disponibilidad del repartidor en este momento. La entrega continúa pendiente; intente nuevamente en unos minutos. | POL01 |
| `INVALID_DELIVERY_WINDOW` | El inicio de la ventana de entrega debe ser anterior a su fin. | INV07 |
| `INVALID_ROUTE_CAPACITY` | La capacidad de la ruta debe ser mayor que cero. | INV07 |
| `ADDRESS_REQUIRED` | La dirección de entrega es obligatoria. | INV07 |

En los escenarios se usa una versión resumida del motivo. El texto completo y el código quedan definidos en esta tabla.

## 6. Escenarios de aceptación

Los escenarios siguen tres convenciones: utilizan datos concretos, comprueban un solo comportamiento cada uno y emplean lenguaje de negocio, sin mencionar bases de datos, pantallas ni servicios técnicos.

### 6.1 Asignación exitosa

**EA01. Asignar un repartidor a una entrega pendiente** (INV03, INV06)

```gherkin
Escenario: Asignar un repartidor disponible a una entrega pendiente
  Dado una entrega "ENT-3021" en estado "Pendiente de asignación" con dirección "Carrera 70 # 44-18"
  Y un repartidor activo "Andrés" con una ruta de capacidad 10 y 6 paradas
  Cuando la coordinadora "Sandra" asigna a "Andrés" la entrega "ENT-3021"
  Entonces la entrega "ENT-3021" queda "Asignada" a "Andrés"
  Y se registra el evento "Entrega asignada" con la entrega, el repartidor, la coordinadora y la hora
  Y se solicita un aviso para "Andrés"
```

### 6.2 Reglas de la entrega

**EA02. Entrega que ya tiene repartidor** (INV01)

```gherkin
Escenario: Rechazar la asignación de una entrega que ya tiene repartidor
  Dado una entrega "ENT-3022" en estado "Asignada" a "Andrés"
  Cuando la coordinadora "Sandra" asigna a "Luisa" la entrega "ENT-3022"
  Entonces se rechaza con el motivo "la entrega ya tiene un repartidor activo"
  Y la entrega "ENT-3022" sigue asignada a "Andrés"
  Y no se registra ningún evento
```

**EA03. Asignar otra vez al mismo repartidor** (INV01)

```gherkin
Escenario: Rechazar la asignación repetida al mismo repartidor
  Dado una entrega "ENT-3023" en estado "Asignada" a "Andrés"
  Cuando la coordinadora "Sandra" asigna a "Andrés" la entrega "ENT-3023"
  Entonces se rechaza con el motivo "la entrega ya tiene un repartidor activo"
  Y no se registra ningún evento
```

**EA04. Entrega que no está pendiente** (INV02)

```gherkin
Esquema del escenario: Rechazar la asignación de una entrega que no está pendiente
  Dado una entrega "ENT-3024" en estado "<estado>"
  Cuando la coordinadora "Sandra" asigna a "Andrés" la entrega "ENT-3024"
  Entonces se rechaza con el motivo "la entrega no está pendiente de asignación"
  Y el estado de la entrega sigue siendo "<estado>"

  Ejemplos:
    | estado     |
    | En camino  |
    | Entregada  |
    | Fallida    |
    | Cancelada  |
```

**EA05. Estado y repartidor coherentes** (INV03)

```gherkin
Escenario: Una entrega pendiente no tiene repartidor
  Dado una entrega "ENT-3025" recién creada
  Entonces la entrega "ENT-3025" está "Pendiente de asignación"
  Y la entrega "ENT-3025" no tiene repartidor
```

### 6.3 Precondiciones y políticas

**EA06. Entrega inexistente** (PRE01)

```gherkin
Escenario: Rechazar la asignación de una entrega que no existe
  Dado que no existe la entrega "ENT-9999"
  Cuando la coordinadora "Sandra" asigna a "Andrés" la entrega "ENT-9999"
  Entonces se rechaza con el motivo "entrega no encontrada"
  Y no se guarda ningún cambio
```

**EA07. Falla la verificación de disponibilidad** (POL01)

```gherkin
Escenario: No asignar si no se puede verificar la disponibilidad
  Dado una entrega "ENT-3026" en estado "Pendiente de asignación"
  Y que el servicio que informa la disponibilidad de los repartidores no responde
  Cuando la coordinadora "Sandra" asigna a "Andrés" la entrega "ENT-3026"
  Entonces se rechaza con el motivo "no se pudo verificar la disponibilidad"
  Y la entrega "ENT-3026" sigue "Pendiente de asignación"
  Y no se registra ningún evento
```

### 6.4 Disponibilidad y capacidad

**EA08. Repartidor inactivo** (INV05)

```gherkin
Escenario: Rechazar la asignación a un repartidor inactivo
  Dado una entrega "ENT-3027" en estado "Pendiente de asignación"
  Y un repartidor "Paola" que se encuentra inactiva
  Cuando la coordinadora "Sandra" asigna a "Paola" la entrega "ENT-3027"
  Entonces se rechaza con el motivo "el repartidor no está activo"
  Y la entrega "ENT-3027" sigue "Pendiente de asignación"
```

**EA09. Ruta saturada** (INV04)

```gherkin
Escenario: Rechazar la asignación cuando la ruta está saturada
  Dado una entrega "ENT-3028" en estado "Pendiente de asignación"
  Y un repartidor activo "Andrés" con una ruta de capacidad 10 y 10 paradas
  Cuando la coordinadora "Sandra" asigna a "Andrés" la entrega "ENT-3028"
  Entonces se rechaza con el motivo "la ruta está saturada"
  Y la entrega "ENT-3028" sigue "Pendiente de asignación"
```

**EA10. Último cupo de la ruta** (INV04, caso límite)

```gherkin
Escenario: Aceptar la asignación que ocupa el último cupo
  Dado una entrega "ENT-3029" en estado "Pendiente de asignación"
  Y un repartidor activo "Andrés" con una ruta de capacidad 10 y 9 paradas
  Cuando la coordinadora "Sandra" asigna a "Andrés" la entrega "ENT-3029"
  Entonces la entrega "ENT-3029" queda "Asignada" a "Andrés"
  Y la ruta de "Andrés" queda con 10 paradas
  Y la ruta de "Andrés" está saturada
```

**EA16. La urgencia no exime de la capacidad** (INV04)

```gherkin
Escenario: Rechazar una entrega urgente cuando la ruta está saturada
  Dado una entrega "ENT-3030" con prioridad "Urgente" en estado "Pendiente de asignación"
  Y un repartidor activo "Camilo" con una ruta de capacidad 10 y 10 paradas
  Cuando la coordinadora "Sandra" asigna a "Camilo" la entrega "ENT-3030"
  Entonces se rechaza con el motivo "la ruta está saturada"
  Y la entrega "ENT-3030" sigue "Pendiente de asignación"
  Y la ruta de "Camilo" conserva sus 10 paradas
```

Este escenario es una decisión de negocio deliberada: la urgencia puede cambiar el orden en que el coordinador atiende las entregas, pero no puede sobrepasar la capacidad de una ruta. De lo contrario, el intento de cumplir una entrega urgente generaría retrasos en las demás entregas de la misma ruta.

### 6.5 Concurrencia

**EA11. Dos coordinadores asignan la misma entrega** (INV01)

```gherkin
Escenario: Solo una de dos asignaciones simultáneas de la misma entrega prevalece
  Dado una entrega "ENT-3031" en estado "Pendiente de asignación"
  Cuando la coordinadora "Sandra" y el coordinador "Julián" asignan al mismo tiempo la entrega "ENT-3031", ella a "Andrés" y él a "Luisa"
  Entonces solo una de las asignaciones se completa
  Y la otra se rechaza con el motivo "la entrega ya tiene un repartidor activo"
  Y la entrega "ENT-3031" tiene exactamente un repartidor
```

**EA12. Dos entregas compiten por el último cupo** (INV04)

```gherkin
Escenario: Solo una de dos asignaciones simultáneas ocupa el último cupo
  Dado una ruta de "Andrés" con capacidad 10 y 9 paradas
  Y dos entregas "ENT-3032" y "ENT-3033" en estado "Pendiente de asignación"
  Cuando dos coordinadores asignan al mismo tiempo cada entrega a "Andrés"
  Entonces solo una de las entregas queda asignada a "Andrés"
  Y la otra se rechaza con el motivo "la ruta está saturada"
  Y la ruta de "Andrés" no supera las 10 paradas
```

> EA12 depende de la decisión pendiente sobre cómo se coordinan `Delivery` y `Route` (ver `03-modelo-de-dominio.md`, sección 9). Dicha decisión se resuelve en el paso 6 y se justifica en el ADR.

### 6.6 Objetos de valor

**EA13. Ventana de entrega inválida** (INV07)

```gherkin
Escenario: No crear una ventana de entrega con el inicio después del fin
  Cuando se intenta crear una ventana de entrega que empieza a las 16:00 y termina a las 14:00
  Entonces se rechaza con el motivo "el inicio debe ser anterior al fin"
```

**EA14. Capacidad de ruta inválida** (INV07)

```gherkin
Esquema del escenario: No crear una capacidad de ruta menor que uno
  Cuando se intenta crear una capacidad de ruta de <capacidad> paradas
  Entonces se rechaza con el motivo "la capacidad debe ser mayor que cero"

  Ejemplos:
    | capacidad |
    | 0         |
    | -5        |
```

**EA15. Dirección vacía** (INV07)

```gherkin
Escenario: No crear una dirección de entrega vacía
  Cuando se intenta crear una dirección de entrega sin texto
  Entonces se rechaza con el motivo "la dirección es obligatoria"
```

## 7. Cobertura: reglas y escenarios

| Regla | Escenarios |
|-------|------------|
| INV01 | EA02, EA03, EA11 |
| INV02 | EA04 |
| INV03 | EA01, EA05 |
| INV04 | EA09, EA10, EA12, EA16 |
| INV05 | EA08 |
| INV06 | EA01 |
| INV07 | EA13, EA14, EA15 |
| PRE01 | EA06 |
| POL01 | EA07 |

Todas las reglas cuentan con al menos un escenario. Las más críticas, INV01 e INV04, tienen varios, incluidos casos límite, de concurrencia y de prioridad.

## 8. Cómo se convertirán en pruebas

Cada escenario se comprobará en el nivel más bajo posible, lo que determina qué se prueba y con qué herramientas.

| Nivel de prueba | Qué comprueba | Escenarios |
|-----------------|---------------|------------|
| **Pruebas unitarias del dominio** | Entidades y objetos de valor, sin dependencias externas | EA02, EA03, EA04, EA05, EA10, EA13, EA14, EA15 |
| **Pruebas del caso de uso con dobles de los puertos** | Coordinación, consultas y políticas, sin infraestructura real | EA01, EA06, EA07, EA08, EA09, EA16 |
| **Pruebas de integración del flujo** | Concurrencia y comportamiento con almacenamiento real | EA11, EA12 (Actividad 4) |

Este reparto permite probar el negocio sin base de datos ni servicios reales, que es precisamente el propósito de separar el dominio de la infraestructura.

## 9. Relación con los escenarios de calidad

Los escenarios de aceptación comprueban reglas de negocio, mientras que los escenarios de calidad de la Actividad 2 comprueban cualidades medibles. En consecuencia, no se reemplazan entre sí.

| Escenario de aceptación | Escenario de calidad relacionado |
|-------------------------|----------------------------------|
| EA01 | QS13 (cambiar una regla de ruteo afecta un solo dominio) |
| EA07 | QS09 (la falla de un tercero no detiene la operación) |
| EA11, EA12 | QS01 (concurrencia sin sobreventa, por analogía con la reserva de inventario) |
| EA01, EA09 y EA16 | QS07 (asignación bajo carga) |

## 10. Decisiones pendientes

1. **Asignación repetida al mismo repartidor:** en este diseño se trata como un rechazo (EA03) por simplicidad. La alternativa sería hacerla idempotente.
2. **Coordinación entre `Delivery` y `Route`:** definir cómo se evita superar la capacidad bajo concurrencia (EA12). Se resuelve en el paso 6.
3. **Falla en el registro del evento:** qué ocurre si la asignación se guarda pero el evento no se registra. Se define en el paso 6 y en el ADR.
4. **Posición de las entregas urgentes en la ruta:** una mejora posible sería ubicarlas como primera parada. Quedaría como una regla adicional de Ruteo, fuera del alcance de esta actividad.
5. **Número de coordinadores simultáneos:** determina cuánta atención merece el riesgo de asignaciones concurrentes.

## 11. Qué falta confirmar

1. Con el negocio, que los mensajes de rechazo correspondan a las razones que realmente importan al coordinador.
2. La capacidad real de las rutas y la forma en que se mide.
3. Si existen reglas adicionales que no se hayan considerado, como zonas de cobertura o restricciones por tipo de producto.
