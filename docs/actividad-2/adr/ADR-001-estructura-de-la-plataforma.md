# ADR-001: Estructura inicial de la plataforma de logística de última milla

| Campo | Valor |
|-------|-------|
| **Estado** | Aceptada, con revisión periódica (ver sección 9) |
| **Fecha** | 5 de noviembre de 2026 |
| **Autor** | [Tu nombre completo] |
| **Actividad** | Actividad 2: requisitos y decisión estructural |
| **Reemplaza a** | Ninguna |
| **Decisiones relacionadas** | D01, D06, D07 y D12 de la Actividad 1; ADR-002 pendiente (fuente de la verdad) |

## 1. Contexto

La empresa de retail digital recibe pedidos por varios canales, maneja inventarios distribuidos y coordina entregas con distintas rutas y repartidores. No existe una vista única del estado real de cada pedido, lo que produce duplicados, inconsistencias de inventario, entregas fallidas y pérdida de trazabilidad (`docs/actividad-1`).

La Actividad 1 propuso una plataforma centralizada que coordina, rastrea y da trazabilidad a los pedidos de última milla, y que convive con el ERP/CRM existente. Falta decidir **cómo se organiza esa plataforma por dentro**: una sola unidad con módulos o un conjunto de servicios independientes. Esta decisión condiciona el diseño, la construcción y la operación de las siguientes unidades.

### Fuerzas que influyen en la decisión

| Fuerza | Origen |
|--------|--------|
| Evitar sobreventas incluso con operaciones simultáneas (consistencia) | RNF03, RF04, QS01, riesgo R02 |
| Una única fuente confiable de eventos por pedido (trazabilidad) | RNF05, RF07, RF16, QS04, riesgo R04 |
| Integración estable con el ERP/CRM y sistemas externos | RF19, RNF08, QS12, riesgos R07 y R08 |
| Ruteo es el dominio con más carga | RF09, QS07 |
| La operación de entrega no puede detenerse | RNF02, QS08, riesgo R14 |
| Poder cambiar un dominio sin arrastrar a los demás | RNF07, QS13, riesgo R13 |
| Un equipo pequeño debe poder operar la plataforma | RNF13, QS14, supuestos A2-S01 a A2-S03 |
| Volumen moderado con picos y crecimiento gradual | Supuestos A2-S02 y A2-S06 |

## 2. Decisión

**Se adopta un monolito modular con límites de dominio explícitos como estructura inicial de la plataforma.**

La plataforma se construye como **una sola unidad desplegable**, organizada en módulos por dominio: **Pedidos, Inventario, Ruteo, Entregas e Incidencias**, más las capacidades transversales (historial de eventos, control de acceso, indicadores e integración).

### Reglas que forman parte de la decisión

1. **Un módulo por dominio.** Cada módulo tiene su propio modelo, sus reglas y sus datos. Una misma palabra puede significar cosas distintas en cada uno (por ejemplo, "disponible").
2. **Dependencias solo por interfaces públicas.** Ningún módulo accede directamente a los datos o a las reglas internas de otro.
3. **Operaciones consistentes.** Reservar inventario y cambiar el estado de un pedido ocurren como una sola operación, sin estados intermedios visibles.
4. **Integraciones por adaptadores.** El ERP/CRM, pagos, geolocalización, notificaciones y canales de venta se conectan mediante adaptadores con contratos versionados.
5. **Ruteo se construye aislado.** Su interfaz se define de modo que pueda ejecutarse de forma diferida y, si hace falta, extraerse como servicio independiente.
6. **Un único historial de eventos por pedido**, con quién, qué y cuándo.
7. **No se introducen servicios independientes** mientras no se cumplan las condiciones de revisión de la sección 9.

> Esta decisión es **estructural**. No elige lenguajes, productos ni proveedores, que se tratarán en actividades posteriores.

## 3. Alternativas consideradas

El análisis completo está en `04-comparacion-de-alternativas.md`.

| Alternativa | Puntaje ponderado | Resultado |
|-------------|-------------------|-----------|
| **A. Monolito modular con límites claros** | 3,75 | **Elegida** |
| B. Arquitectura distribuida por dominios | 2,90 | Descartada por ahora; queda como camino de evolución |
| Arquitectura por capas, cliente-servidor y enfoque basado en eventos | No aplica | No son alternativas excluyentes; se pueden usar dentro de A o junto a ella |
| Microservicios muy finos desde el inicio | No aplica | Versión extrema de B, sin evidencia que la justifique |

## 4. Justificación

### Razones técnicas

- **Protege lo más sensible del caso:** la consistencia del inventario. En la Opción A una reserva y un cambio de estado se hacen en una sola operación, lo que atiende el riesgo crítico R02 y el escenario QS01. En la Opción B exigiría coordinar servicios y manejar estados intermedios.
- **Facilita la trazabilidad y la seguridad:** el historial y los permisos viven en un solo lugar (QS04 y QS11).
- **Cumple mejor los escenarios de calidad:** cumple 12 de 15, frente a 9 de la Opción B.
- **Conserva la puerta abierta a evolucionar:** los límites de dominio permiten extraer Ruteo más adelante sin rehacer el resto.

### Razones de negocio

- **Resuelve el problema diagnosticado a menor costo.** El problema nace de la falta de una vista única, y una unidad bien organizada la resuelve sin la carga operativa de un sistema distribuido.
- **Llega antes a operar.** La Opción A es más rápida de construir, y el problema actual genera pérdidas todos los días.
- **Coincide con la capacidad del equipo.** Con un equipo pequeño (A2-S01) y sin experiencia operando muchos servicios (A2-S03), el riesgo de la Opción B está en la operación (QS14).
- **Evita sobrediseñar.** No hay evidencia de que el crecimiento exija separar servicios.

### Resultado de la evaluación

| Perfil | Puntaje A | Puntaje B |
|--------|-----------|-----------|
| Equipo pequeño, volumen moderado (base) | 3,75 | 2,90 |
| Crecimiento rápido con varios equipos | 3,45 | 3,45 |

La decisión es sólida en el perfil base y **sensible a un cambio de supuestos**. Por eso incluye condiciones de revisión.

## 5. Consecuencias

### Positivas

- Construcción y puesta en marcha más rápidas.
- Operación sencilla: un despliegue, un monitoreo.
- Consistencia de datos y trazabilidad más simples de garantizar.
- Menor costo inicial y de mantenimiento.
- Límites de dominio que ordenan el diseño y facilitan una evolución posterior.

### Negativas y trade-offs

| Lo que se gana | Lo que se cede |
|----------------|----------------|
| Consistencia simple y operación sencilla | Independencia de despliegue: cualquier cambio redespliega toda la unidad |
| Menor costo y menor complejidad | Escalado independiente: la plataforma escala como una sola pieza |
| Rapidez de implementación | Aislamiento fuerte de fallas: una falla grave puede afectar a toda la plataforma |
| Historial y permisos en un solo lugar | Autonomía de equipos separados por dominio |

## 6. Riesgos de la decisión y mitigaciones

| Riesgo | Mitigación |
|--------|------------|
| **Acoplamiento creciente entre módulos**: el monolito modular se degrada en una base de código enredada | Reglas de dependencia explícitas y verificables; revisión de que ningún módulo acceda a datos de otro |
| **Ruteo bajo carga** afecta a los demás dominios (QS07) | Ejecución diferida o aislada; interfaz preparada para extraerse |
| **Punto único de falla** (R14) | Aislar las fallas de módulos no críticos; recuperación definida (QS08) |
| **Los supuestos resultan falsos** (equipo, volumen) | Condiciones de revisión de la sección 9 y validación temprana con el negocio |
| **Límites mal definidos desde el inicio** | Los dominios y el glosario por contexto se acuerdan con el negocio antes de construir |

## 7. Supuestos de los que depende

| Supuesto | Si resulta falso |
|----------|------------------|
| A2-S01: equipo pequeño | Una estructura más distribuida sería viable |
| A2-S02 y A2-S06: volumen moderado con picos y crecimiento gradual | Habría que priorizar la escalabilidad desde el inicio |
| A2-S03: sin capacidad de operar muchos servicios | El costo operativo de la Opción B bajaría |
| **Hipótesis provisional sobre la fuente de la verdad** (catálogo, sección 6): la plataforma es la fuente del estado del pedido y de las reservas; el ERP/CRM, de los datos maestros y las existencias oficiales | Se revisa el ADR; podría requerir ADR-002 y cambios en los módulos Inventario e integración |

## 8. Cómo se verificará

Los criterios completos están en `05-criterios-verificables.md`. Los principales:

| Qué se comprueba | Cómo |
|------------------|------|
| Los módulos no se acceden entre sí por fuera de sus interfaces | Revisión de dependencias entre módulos |
| No hay sobreventas con reservas simultáneas | Prueba del escenario QS01 |
| Ruteo no degrada a los demás dominios | Prueba del escenario QS07 |
| Un cambio de regla de ruteo toca un solo dominio | Revisión del cambio frente a QS13 |
| Un equipo de 2 o 3 personas puede desplegar y revertir | Prueba del escenario QS14 |

## 9. Condiciones de revisión

La decisión se revisa si ocurre alguna de estas condiciones. Todas las cifras son supuestos que se ajustan con datos reales.

| Criterio | Condición que activa la revisión |
|----------|----------------------------------|
| **Carga** | El pico sostenido supera 3.000 pedidos por hora, o QS07 se incumple en más del 10 % de las ejecuciones |
| **Latencia** | Las consultas de otros dominios superan 2 segundos en más del 5 % de los casos durante el cálculo de rutas |
| **Autonomía** | Ruteo cambia y se despliega con una frecuencia mucho mayor que el resto, o lo mantiene un equipo distinto |
| **Costo operativo** | El equipo ya puede monitorear, desplegar y atender más de un servicio |
| **Supuestos de base** | Cambian el tamaño del equipo, el volumen esperado o la fuente de la verdad del inventario |

**Primer paso previsto si se cumple alguna:** extraer **Ruteo** como servicio independiente, manteniendo el resto como monolito modular.

## 10. Decisiones pendientes relacionadas

- **ADR-002 (pendiente):** fuente de la verdad del inventario y del estado del pedido (decisión D12 de la Actividad 1). Se redactará cuando haya información del negocio.
- **Elección de tecnologías:** se decide en actividades posteriores, una vez fijada la estructura.

## 11. Referencias

- `docs/actividad-1/05-riesgos.md`
- `docs/actividad-1/06-supuestos-y-decisiones.md`
- `docs/actividad-2/01-catalogo-de-requisitos.md`
- `docs/actividad-2/02-matriz-de-priorizacion.md`
- `docs/actividad-2/03-escenarios-de-calidad.md`
- `docs/actividad-2/04-comparacion-de-alternativas.md`
