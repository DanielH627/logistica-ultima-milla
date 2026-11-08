# Plataforma de gestión logística de última milla

Proyecto de arquitectura de software sobre una plataforma de logística de última milla para una empresa de retail digital. El trabajo avanza por actividades: primero se **comprende el problema**, luego se **decide la estructura** de la solución.

- **Autor:** [Tu nombre completo]
- **Curso / institución:** [Nombre del curso e institución]

| Actividad | Qué resuelve | Documentos | Video |
|-----------|--------------|------------|-------|
| **1. Diagnóstico y contexto arquitectónico** | Entender el problema, los actores, el alcance y los riesgos | [`docs/actividad-1`](docs/actividad-1) | [Pendiente de publicar] |
| **2. Requisitos y decisión estructural** | Traducir el diagnóstico en requisitos y decidir cómo se organiza la plataforma | [`docs/actividad-2`](docs/actividad-2) | [Pendiente de publicar] |

> En ninguna actividad se eligen tecnologías, lenguajes ni proveedores: se decide la **estructura**, no las herramientas.

---

## Resumen del problema

La empresa recibe pedidos por varios canales, maneja inventarios distribuidos y coordina entregas con distintas rutas y repartidores, pero **no tiene una vista única del estado real de cada pedido**. La información está dispersa entre los canales de venta, los inventarios, las rutas y el área de soporte.

Esto genera pedidos duplicados, inventario inconsistente, entregas retrasadas o fallidas y pérdida de trazabilidad, y limita la capacidad de la empresa para crecer con control.

---

## Actividad 1: Diagnóstico y contexto arquitectónico

### Objetivos

1. Dar una **vista única y confiable** del estado de cada pedido.
2. Evitar la **duplicación** de pedidos y las **inconsistencias de inventario**.
3. Reducir las **entregas fallidas o retrasadas**.
4. Garantizar la **trazabilidad** de cada pedido.
5. Permitir que la operación **crezca sin perder el control**.

### Actores

| Tipo | Quiénes |
|------|---------|
| **Personas** | Cliente, operador de comercio o tienda, administrador de almacén, repartidor, soporte al cliente, coordinador de despacho |
| **Sistemas externos** | Sistema de pagos, servicio de geolocalización, sistema de notificaciones, ERP o CRM interno, canales de venta |

Se identificaron 18 actores en total; el detalle está en [Actores y necesidades](docs/actividad-1/02-actores-y-necesidades.md).

### Alcance

**Dentro:** consolidar pedidos de todos los canales, validar y reservar inventario, mantener un estado común del pedido, asignar entregas, rastrear envíos, gestionar incidencias, solicitar notificaciones, registrar el historial de eventos y ofrecer indicadores básicos.

**Fuera:** cobrar pagos, administrar catálogo y precios, vender en los canales, operar la bodega, calcular mapas, enviar mensajes, emitir facturas y gestionar la logística inversa completa.

### Diagrama de contexto

![Diagrama de contexto de la plataforma](docs/actividad-1/diagramas/contexto.png)

### Riesgos críticos

| Riesgo | Por qué importa |
|--------|-----------------|
| **Inconsistencias de inventario** | Se vende lo que no se tiene |
| **Entregas fallidas o retrasadas** | Es lo que el cliente percibe y lo que más cuesta |
| **Fuente de la verdad ambigua entre el ERP/CRM y la plataforma** | Es una indefinición que afecta al inventario y a la trazabilidad |

Se registraron 15 riesgos en total. Ver [Riesgos](docs/actividad-1/05-riesgos.md).

---

## Actividad 2: Requisitos y decisión estructural

### Requisitos prioritarios

Se definieron **20 requisitos funcionales**, **13 atributos de calidad** y **7 restricciones**, y se priorizaron según su impacto en el negocio y en la arquitectura. De los 33 requisitos, 21 son *Must*.

Los cinco que mejor resumen lo que el negocio necesita:

| # | Requisito prioritario | Por qué |
|---|-----------------------|---------|
| 1 | **Estado común del pedido** con historial de eventos | Es la respuesta directa a la falta de una vista única |
| 2 | **Inventario consistente** con reserva al confirmar el pedido | Evita vender lo que no existe |
| 3 | **Asignación de entregas** a rutas y repartidores | Es el núcleo de la coordinación y el dominio con más carga |
| 4 | **Integración con el ERP/CRM** con contratos estables | Es la integración más delicada del sistema |
| 5 | **Seguridad por rol** de los datos personales | Hay información sensible y roles diferenciados |

Los atributos de calidad que más condicionan la estructura son la **consistencia de datos**, la **disponibilidad**, la **trazabilidad**, la **evolución**, la **interoperabilidad**, la **tolerancia a mala conectividad** y la **operabilidad con un equipo pequeño**.

### Escenarios de calidad destacados

Se redactaron 15 escenarios con medidas concretas. Las cifras son supuestos de trabajo, a validar con el negocio.

| Escenario | Condición | Medida |
|-----------|-----------|--------|
| **QS01** Reserva concurrente | Dos pedidos piden a la vez la última unidad de un producto | 0 sobreventas |
| **QS07** Asignación de rutas | Asignar 200 entregas pendientes | Propuesta en 30 segundos o menos, sin afectar al resto |
| **QS10** Conectividad | Un repartidor pierde señal durante 20 minutos | Sincronización en 2 minutos, 0 reportes perdidos |
| **QS14** Operación | Un equipo de 2 o 3 personas publica una versión | Despliegue en 30 minutos o menos, reversión en 15 |

### Alternativas comparadas

![Alternativas estructurales comparadas](docs/actividad-2/diagramas/alternativas.png)

| Alternativa | Puntaje ponderado | Lo mejor | Lo peor |
|-------------|-------------------|----------|---------|
| **A. Monolito modular con límites claros** | **3,75** | Consistencia, costo y rapidez | Escala como una sola unidad |
| B. Arquitectura distribuida por dominios | 2,90 | Autonomía y aislamiento de fallas | Complejidad y costo operativo |

Si el negocio creciera con rapidez y con varios equipos, ambas empatan en 3,45. La decisión depende de los supuestos de equipo pequeño y volumen moderado.

### Decisión arquitectónica

Se adopta un **monolito modular con límites de dominio explícitos**: una sola unidad desplegable, organizada en módulos por dominio (**Pedidos, Inventario, Ruteo, Entregas e Incidencias**), que se comunican por interfaces y se conectan a los sistemas externos mediante adaptadores con contratos versionados.

**Por qué:**

- Protege lo más sensible del caso: una reserva de inventario y el cambio de estado se hacen en una sola operación.
- Facilita la trazabilidad y la seguridad, porque el historial y los permisos viven en un solo lugar.
- Cumple 12 de los 15 escenarios de calidad, frente a 9 de la alternativa B.
- Un equipo pequeño puede operarla, y llega antes a resolver el problema.

**Qué se cede:** independencia de despliegue, escalado separado por dominio y aislamiento fuerte de fallas.

**Camino de evolución:** si la carga, la latencia, la autonomía o el costo operativo lo justifican, **Ruteo** puede extraerse como servicio independiente sin rehacer el resto. Las condiciones están definidas en el ADR.

Ver [ADR-001](docs/actividad-2/adr/ADR-001-estructura-de-la-plataforma.md). La fuente de la verdad del inventario sigue abierta y se tratará en un ADR posterior.

### Criterios verificables

Cuatro bloques de comprobación: **estructura** (siete reglas de la decisión), **calidad** (los 15 escenarios), **negocio** (seis indicadores sobre el problema original) y **revisión** (cinco condiciones que reabren la decisión). Ver [Criterios verificables](docs/actividad-2/05-criterios-verificables.md).

---

## Documentación

### Actividad 1

| # | Documento | Qué responde |
|---|-----------|--------------|
| 1 | [Análisis del problema](docs/actividad-1/01-analisis-del-problema.md) | ¿Qué pasa hoy, por qué importa y qué consecuencias tiene? |
| 2 | [Actores y necesidades](docs/actividad-1/02-actores-y-necesidades.md) | ¿Quién interactúa con el sistema y qué necesita? |
| 3 | [Alcance](docs/actividad-1/03-alcance.md) | ¿Qué hace el sistema y qué queda fuera? |
| 4 | [Diagrama de contexto](docs/actividad-1/04-diagrama-de-contexto.md) | ¿Cómo se relaciona con su entorno? |
| 5 | [Riesgos](docs/actividad-1/05-riesgos.md) | ¿Dónde puede fallar o generar pérdidas? |
| 6 | [Supuestos y decisiones](docs/actividad-1/06-supuestos-y-decisiones.md) | ¿Qué se asume, qué se decide y qué falta confirmar? |

### Actividad 2

| # | Documento | Qué responde |
|---|-----------|--------------|
| 1 | [Catálogo de requisitos](docs/actividad-2/01-catalogo-de-requisitos.md) | ¿Qué debe hacer el sistema y con qué cualidades? |
| 2 | [Matriz de priorización](docs/actividad-2/02-matriz-de-priorizacion.md) | ¿Qué importa más y por qué? |
| 3 | [Escenarios de calidad](docs/actividad-2/03-escenarios-de-calidad.md) | ¿Cómo se comprueba cada cualidad? |
| 4 | [Comparación de alternativas](docs/actividad-2/04-comparacion-de-alternativas.md) | ¿Qué estructuras son posibles y cuál conviene? |
| 5 | [Criterios verificables](docs/actividad-2/05-criterios-verificables.md) | ¿Cómo se sabe si la decisión funciona? |
| ADR | [ADR-001](docs/actividad-2/adr/ADR-001-estructura-de-la-plataforma.md) | ¿Qué se decidió, por qué y a qué costo? |

---

## Estructura del repositorio

```
logistica-ultima-milla/
├── README.md
└── docs/
    ├── actividad-1/
    │   ├── 01-analisis-del-problema.md
    │   ├── 02-actores-y-necesidades.md
    │   ├── 03-alcance.md
    │   ├── 04-diagrama-de-contexto.md
    │   ├── 05-riesgos.md
    │   ├── 06-supuestos-y-decisiones.md
    │   └── diagramas/
    │       └── contexto.png
    └── actividad-2/
        ├── 01-catalogo-de-requisitos.md
        ├── 02-matriz-de-priorizacion.md
        ├── 03-escenarios-de-calidad.md
        ├── 04-comparacion-de-alternativas.md
        ├── 05-criterios-verificables.md
        ├── adr/
        │   └── ADR-001-estructura-de-la-plataforma.md
        └── diagramas/
            └── alternativas.png
```

---

## Estado

### Actividad 1
- [x] Problema definido con claridad
- [x] Usuarios y actores identificados
- [x] Alcance delimitado
- [x] Diagrama de contexto
- [x] Riesgos principales listados
- [x] Supuestos explícitos
- [x] Decisiones iniciales justificadas
- [ ] Video de sustentación entregado

### Actividad 2
- [x] Requisitos funcionales claros
- [x] Requisitos no funcionales definidos
- [x] Escenarios de calidad con métricas
- [x] Al menos dos alternativas comparadas
- [x] Decisión argumentada con trade-offs
- [x] Registro de decisión arquitectónica (ADR)
- [x] Criterios verificables
- [ ] Video de sustentación entregado
