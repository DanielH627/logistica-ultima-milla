# Matriz de priorización

## 1. Introducción

Esta matriz ordena los requisitos de `01-catalogo-de-requisitos.md` según su **importancia para el negocio** y su **peso en la arquitectura**. Sirve para decidir qué requisitos deben guiar la elección de la estructura y cuáles pueden esperar.

Cada requisito se valora en cuatro aspectos:

| Aspecto | Pregunta que responde |
|---------|-----------------------|
| **Impacto en el negocio** | ¿Qué tanto afecta al problema y a los riesgos de la Actividad 1 si no se cumple? |
| **Impacto en la arquitectura** | ¿Qué tanto condiciona la estructura del sistema? |
| **Complejidad** | ¿Qué tan difícil es de cumplir? |
| **Prioridad** | ¿Es indispensable, deseable o postergable? |

> Las valoraciones son **estimaciones cualitativas** hechas sin datos reales de la empresa. Se registran como supuesto (A2-S05) y deben validarse con el negocio.

## 2. Escalas

**Impacto y complejidad:** Alto (A), Medio (M), Bajo (B).

| Valor | Impacto en el negocio | Impacto en la arquitectura | Complejidad |
|-------|----------------------|----------------------------|-------------|
| **A** | Su falla reproduce el problema central o un riesgo crítico | Obliga a decidir cómo se separa, se conecta o se despliega el sistema | Requiere coordinar varios dominios, datos o sistemas externos |
| **M** | Afecta la operación de forma visible, pero con alternativas | Influye en un dominio concreto | Requiere lógica o integración moderada |
| **B** | Mejora la experiencia o el control, sin ser central | Se resuelve dentro de un módulo | Lógica simple o servicio externo directo |

**Prioridad (MoSCoW):**

| Valor | Significado |
|-------|-------------|
| **Must** | Sin esto el sistema no resuelve el problema diagnosticado |
| **Should** | Importante, pero la primera versión puede operar sin él temporalmente |
| **Could** | Deseable; se hace si hay margen |
| **Won't (por ahora)** | Reconocido y postergado de forma consciente |

**Regla guía:** un requisito es *Must* cuando su impacto en el negocio es alto y se relaciona con la definición del problema o con un riesgo alto o crítico. Se aplica con criterio: no es un cálculo automático.

## 3. Requisitos funcionales

| ID | Requisito | Dominio | Negocio | Arquitectura | Complejidad | Prioridad | Justificación |
|----|-----------|---------|---------|--------------|-------------|-----------|---------------|
| RF01 | Registrar y consolidar pedidos de todos los canales con identificador único | Pedidos | A | M | M | **Must** | Es la base de la vista única y ataca la duplicación (R01) |
| RF02 | Detectar posibles pedidos duplicados | Pedidos | A | M | M | **Must** | La duplicación es un riesgo alto y consume inventario y despachos |
| RF03 | Consultar disponibilidad de inventario consolidada por ubicación | Inventario | A | A | M | **Must** | Evita vender lo que no existe (R02, crítico) |
| RF04 | Reservar inventario al confirmar el pedido | Inventario | A | A | A | **Must** | Es lo que previene la sobreventa; depende de la fuente de la verdad (R07) |
| RF05 | Recibir y aplicar actualizaciones de stock | Inventario | A | A | M | **Must** | Sin stock actualizado, la reserva pierde sentido |
| RF06 | Consultar el estado del pago y no despachar sin pago confirmado | Pedidos | M | M | B | Should | Evita pérdidas, pero se apoya en un servicio externo |
| RF07 | Mantener un estado común del pedido con ciclo de vida compartido | Pedidos | A | A | M | **Must** | Es la respuesta directa a la falta de vista única |
| RF08 | Validar y normalizar la dirección de entrega | Ruteo | M | B | M | Should | Reduce entregas fallidas (R11) con un servicio externo |
| RF09 | Asignar pedidos a rutas y repartidores | Ruteo | A | A | A | **Must** | Es el núcleo de la coordinación y el dominio con más carga (R03, R05) |
| RF10 | Reasignar entregas ante imprevistos | Ruteo | M | M | M | Should | Mejora la reacción ante fallas una vez resuelta la asignación |
| RF11 | Registrar estado, ubicación y evidencia de entrega desde el campo | Entregas | A | A | M | **Must** | Da trazabilidad real y exige tolerar mala conectividad (R10) |
| RF12 | Gestionar entregas fallidas y reintentos | Entregas | M | M | M | Should | El estado "fallida" entra con RF11; los reintentos pueden madurar después |
| RF13 | Consultar el estado y el historial completo de un pedido | Pedidos | A | M | B | **Must** | Permite a soporte resolver sin llamar a otras áreas (R05) |
| RF14 | Registrar y gestionar incidencias hasta su cierre | Incidencias | A | M | M | **Must** | Cubre el flujo de reclamos descrito en el problema |
| RF15 | Solicitar notificaciones al cliente | Transversal | M | B | B | Should | Reduce incertidumbre; el envío lo hace un servicio externo |
| RF16 | Registrar el historial de eventos de cada pedido | Transversal | A | A | M | **Must** | Es el núcleo de la trazabilidad (R04) y de los indicadores |
| RF17 | Ofrecer indicadores operativos básicos | Transversal | M | B | M | Could | Útil para decidir, pero se alimenta del historial ya existente |
| RF18 | Controlar el acceso por rol | Transversal | A | A | M | **Must** | Protege datos personales (R09) y define quién hace qué |
| RF19 | Intercambiar movimientos y datos maestros con el ERP/CRM | Transversal | A | A | A | **Must** | Es la integración más delicada y condiciona la fuente de la verdad (R07) |
| RF20 | Cancelar o modificar un pedido antes de su despacho | Pedidos | M | B | M | Could | Requisito añadido como supuesto (A2-S04); no viene del caso |

## 4. Requisitos no funcionales

| ID | Atributo | Negocio | Arquitectura | Complejidad | Prioridad | Justificación |
|----|----------|---------|--------------|-------------|-----------|---------------|
| RNF01 | Rendimiento | M | M | M | Should | La consulta debe ser ágil, pero no es el eje del problema |
| RNF02 | Disponibilidad | A | A | A | **Must** | La operación de entrega no puede detenerse (R14) |
| RNF03 | Consistencia de datos | A | A | A | **Must** | Es la base para no sobrevender (R02, crítico) |
| RNF04 | Seguridad y privacidad | A | A | M | **Must** | Hay datos personales y roles diferenciados (R09) |
| RNF05 | Trazabilidad y auditabilidad | A | A | M | **Must** | Es el valor central de la plataforma (R04) |
| RNF06 | Escalabilidad | M | A | A | Should | El volumen es moderado según A2-S02; se vigila para picos |
| RNF07 | Mantenibilidad y facilidad de evolución | A | A | M | **Must** | La solución debe poder crecer sin reescribirse (R13) |
| RNF08 | Interoperabilidad con contratos versionados | A | A | M | **Must** | Hay varios sistemas externos y un ERP existente (R07, R08) |
| RNF09 | Resiliencia ante fallas de terceros | M | A | M | Should | Evita detener todo por una falla externa (R08) |
| RNF10 | Tolerancia a conectividad intermitente | A | A | M | **Must** | Los repartidores trabajan en campo (R10) |
| RNF11 | Usabilidad | M | B | B | Should | Favorece la adopción (R12, R15) |
| RNF12 | Observabilidad | M | M | M | Should | Permite detectar pedidos atascados e integraciones fallando |
| RNF13 | Operabilidad y costo | A | A | M | **Must** | Es decisivo al comparar estructuras con equipo y recursos limitados |

## 5. Resumen de la priorización

| Prioridad | Requisitos funcionales | Requisitos no funcionales | Total |
|-----------|------------------------|---------------------------|-------|
| Must | 13 | 8 | 21 |
| Should | 5 | 5 | 10 |
| Could | 2 | 0 | 2 |
| **Total** | **20** | **13** | **33** |

El 64 % de los requisitos son *Must*. Es una proporción alta, pero coherente con el caso: el problema nace de la falta de una vista única, y casi todo lo esencial (estado, inventario, trazabilidad, integración) es necesario para resolverlo. Lo que se posterga son indicadores, cancelaciones y funciones de apoyo.

## 6. Requisitos que más condicionan la arquitectura

Son los de **impacto alto en la arquitectura**. Estos requisitos, y no los demás, determinan qué estructura conviene, y serán los criterios de `04-comparacion-de-alternativas.md`.

| Requisito | Por qué condiciona la estructura |
|-----------|----------------------------------|
| RNF03 Consistencia + RF04 Reserva de inventario | Exigen decidir dónde viven los datos de stock y cómo se evita que dos operaciones reserven lo mismo |
| RNF05 Trazabilidad + RF16 Historial + RF07 Estado común | Piden una única fuente confiable de eventos por pedido |
| RF19 Integración con ERP/CRM + RNF08 Contratos | Obligan a definir fronteras e interfaces estables entre sistemas |
| RF09 Asignación de rutas | Es el dominio con más carga y el candidato natural a separarse si crece |
| RNF02 Disponibilidad + RNF09 Resiliencia | Definen cuánto debe aislarse una falla del resto del sistema |
| RNF10 Conectividad intermitente + RF11 Registro de entregas | Condicionan cómo se reciben y se concilian los reportes de campo |
| RNF07 Evolución | Exige límites claros entre dominios para cambiar sin reescribir |
| RNF13 Operabilidad y costo | Pone un límite realista a la complejidad de la estructura |

Los cuatro criterios que se usarán para comparar las alternativas se derivan de aquí: **carga, latencia, autonomía y costo operativo**, más la **consistencia de datos**, que es el requisito más sensible del caso.

## 7. Requisitos prioritarios para el resumen

Los cinco que mejor resumen lo que el negocio necesita, para el README y el video:

1. **Estado común del pedido** (RF07) con historial de eventos (RF16).
2. **Inventario consistente** con reserva al confirmar (RF03, RF04, RF05).
3. **Asignación de entregas** a rutas y repartidores (RF09).
4. **Integración con el ERP/CRM** con contratos estables (RF19, RNF08).
5. **Seguridad por rol** de los datos personales (RF18, RNF04).

## 8. Qué falta confirmar

1. La priorización con el negocio, porque hoy es una estimación.
2. Si RF20 (cancelar o modificar pedidos) es realmente un requisito.
3. Si el cliente consulta su pedido por su cuenta, lo que subiría la prioridad de RNF01 y RNF11.
4. El volumen real, que podría subir la prioridad de RNF06.
