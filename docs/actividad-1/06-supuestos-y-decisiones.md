# Supuestos y decisiones iniciales

## 1. Introducción

Este documento reúne lo que se **asume**, lo que ya se **decide**, lo que **aún no se sabe** y lo que **habrá que confirmar** antes de diseñar la solución. Cierra el diagnóstico de la Actividad 1 y deja explícito sobre qué bases se avanza.

El caso de estudio no entrega todos los datos de la empresa. Por eso, parte del análisis descansa en supuestos razonables que aquí se hacen visibles para poder validarlos o corregirlos.

| Concepto | Significado |
|----------|-------------|
| **Supuesto** | Algo que se da por cierto sin haberlo verificado |
| **Decisión inicial** | Una elección ya tomada, con su justificación |
| **Incertidumbre** | Algo que se sabe que falta y condiciona el diseño |
| **Por confirmar** | Pregunta concreta, con alguien que puede responderla |

## 2. Supuestos

| ID | Supuesto | Documento | Si resulta falso |
|----|----------|-----------|------------------|
| S01 | La empresa vende por sitio web, aplicación móvil, marketplace y atención telefónica | 01, 02 | Cambian los canales que se integran y los flujos de entrada |
| S02 | El inventario está repartido en varios almacenes y tiendas, y cada punto lo actualiza con distinta frecuencia | 01 | El riesgo de inconsistencia (R02) sería menor o diferente |
| S03 | Hoy una persona o área coordina qué repartidor lleva cada pedido y por qué ruta | 01, 02 | Habría que definir quién asigna, y el actor "coordinador de despacho" cambiaría |
| S04 | Los repartidores son del personal propio de la empresa | 02, 03 | Si hay terceros, se agrega el actor "operador logístico tercero" y sus reglas |
| S05 | Existen actores no mencionados en el caso: coordinador de despacho, gerente o analista, administrador de la plataforma, agente telefónico, operador logístico tercero, receptor del pedido, facturación y proveedores | 02 | Se eliminan o se ajustan en el documento de actores y en el diagrama |
| S06 | El ERP/CRM interno permite intercambiar información con otros sistemas | 02, 04 | La integración sería más costosa y habría que replantear el alcance |
| S07 | La plataforma no cobra; solo conoce el estado del pago que le informa un sistema externo | 03, 04 | Si debe cobrar, el alcance crece y aparecen exigencias financieras y de seguridad |
| S08 | Los repartidores cuentan con un medio para reportar desde campo, aunque con conectividad variable | 05 | Cambia cómo se diseña el reporte de estados (R10) |
| S09 | Los datos de los clientes están protegidos por la normativa local de datos personales | 03, 05 | Cambian las exigencias de seguridad y privacidad (R09) |
| S10 | La facturación electrónica queda fuera de la plataforma | 03 | Habría que incluir un flujo con el sistema de facturación |
| S11 | Las devoluciones se registran como incidencias, sin gestionar el ciclo completo de logística inversa | 03 | El alcance crece y se agrega un proceso nuevo |
| S12 | La operación es principalmente urbana o regional, con picos de demanda en fechas promocionales | 01, 05 | Cambian la planificación de rutas y la capacidad esperada |
| S13 | Las probabilidades e impactos de los riesgos son estimaciones sin datos reales | 05 | Se recalcula la priorización |
| S14 | El cliente recibe avisos por notificación; no se asume que consulte el estado por su cuenta | 02, 04 | Si debe consultarlo, se agrega esa necesidad al alcance |
| S15 | El nombre de la empresa del caso, *Mercadeo Compartido*, se usa como contexto ilustrativo | 01 | Ninguno; es solo referencia narrativa |

## 3. Decisiones iniciales

| ID | Decisión | Justificación | Alternativa descartada | Estado |
|----|----------|---------------|------------------------|--------|
| D01 | Proponer **una plataforma centralizada** que registre pedidos, valide inventario, asigne rutas, rastree entregas y gestione incidencias | La causa raíz del problema es la falta de una vista única; centralizar la información ataca esa causa | Mantener cada área con su sistema y conciliar a mano, que perpetúa los errores y la duplicación | Tomada (viene del caso) |
| D02 | Limitar el alcance a la **última milla**, desde el pedido confirmado hasta la entrega | Es donde se concentra el problema descrito y evita crecer sin foco | Abarcar toda la cadena de suministro, que excede el diagnóstico | Tomada |
| D03 | La plataforma **coordina, rastrea y da trazabilidad**; no cobra, no vende ni administra el catálogo | Respeta responsabilidades que ya tienen otros sistemas y evita duplicar datos maestros | Que la plataforma asuma pagos y catálogo, lo que ampliaría el alcance y el riesgo | Tomada |
| D04 | Usar **un identificador único por pedido** y un **estado común** compartido por todas las áreas | Atiende directamente la duplicación y la pérdida de trazabilidad (R01, R04) | Dejar un identificador y estados distintos por canal | Tomada |
| D05 | Registrar cada **cambio de estado** con quién, qué y cuándo | La trazabilidad es el núcleo de la propuesta y soporta los indicadores | Registrar solo el estado final del pedido | Tomada |
| D06 | El **ERP/CRM** se trata como sistema externo con el que se convive; no se reemplaza | Es un sistema interno existente que guarda datos maestros de la empresa | Reemplazarlo o absorber sus funciones | Tomada |
| D07 | **Delegar** pagos, geolocalización y notificaciones en sistemas externos | Son servicios especializados; la plataforma decide qué se necesita, no cómo se presta | Construir esas capacidades dentro de la plataforma | Tomada |
| D08 | Representar el diagrama de contexto con **una caja central**, **canales de venta agrupados** y **una flecha bidireccional por interlocutor** | Mantiene el diagrama legible y fiel a la definición de contexto | Dibujar los 18 actores y 22 flechas, lo que genera un diagrama ilegible | Tomada |
| D09 | **Priorizar los riesgos críticos** (inventario, entregas y fuente de la verdad) como guía para el diseño | Son los de mayor combinación de probabilidad e impacto | Tratar todos los riesgos con la misma prioridad | Tomada |
| D10 | Adoptar la plataforma por **etapas**, con convivencia temporal con los procesos actuales | Reduce la resistencia al cambio y el riesgo de interrumpir la operación | Reemplazo total en una sola fecha | Provisional |
| D11 | **No elegir tecnologías, estilos de arquitectura ni estructura interna** en esta actividad | La arquitectura comienza con el contexto; elegir antes de entender condiciona mal el diseño | Definir la solución técnica desde el inicio | Tomada |
| D12 | Decidir **más adelante** quién es la fuente de la verdad del inventario y del estado del pedido | No hay información suficiente para decidir hoy; hacerlo sin datos genera riesgo (R07) | Elegir ahora sin consultar al negocio | Aplazada |

## 4. Lo que aún no se sabe

1. **Cuál es la fuente de la verdad** del inventario y del estado del pedido: el ERP/CRM o la plataforma.
2. **Cuántos pedidos** se manejan por día, cuáles son los picos y qué crecimiento se espera.
3. **Cuántos almacenes y tiendas** hay, y con qué frecuencia actualiza cada uno su stock.
4. **Qué porcentaje de entregas falla hoy** y cuáles son las causas más frecuentes.
5. **Cómo se asignan hoy las rutas**: de forma manual, por área o por una persona.
6. **Qué canales de venta existen realmente** y cuáles deben integrarse primero.
7. **Cómo reportan hoy los repartidores** y con qué conectividad cuentan.
8. **Qué exige la normativa** sobre datos personales y trazabilidad.
9. **Plazos, presupuesto y recursos** disponibles para el proyecto.

## 5. Por confirmar

| # | Pregunta | Quién puede responder | Afecta a |
|---|----------|----------------------|----------|
| 1 | ¿El ERP/CRM o la plataforma es la fuente de la verdad del inventario? | Área de tecnología y responsable de inventario | Alcance, D06, D12, R02, R07 |
| 2 | ¿Se usan transportistas externos? | Operaciones y logística | Actores, alcance, S04 |
| 3 | ¿Cómo se gestionan hoy las devoluciones? | Operaciones y soporte | Alcance, S11 |
| 4 | ¿La facturación electrónica depende de la entrega? | Finanzas | Alcance, S10 |
| 5 | ¿El cliente consulta el estado por su cuenta o solo recibe avisos? | Negocio y experiencia de cliente | Actores, S14 |
| 6 | ¿Qué canales se integran en la primera fase? | Negocio y comercial | Alcance, S01 |
| 7 | ¿Cuántos pedidos y entregas fallidas hay al mes? | Operaciones | Riesgos, S13 |
| 8 | ¿Qué normativa de datos personales aplica y qué exige? | Legal y cumplimiento | R09, S09 |
| 9 | ¿Qué debe seguir funcionando si la plataforma no está disponible? | Operaciones y tecnología | R14 |

## 6. Relación con los riesgos

| Decisión | Riesgos que atiende |
|----------|---------------------|
| D01 y D04 | R01 (duplicación), R04 (trazabilidad), R05 (coordinación) |
| D03 y D02 | R06 (crecimiento sin control), R13 (crecimiento del alcance) |
| D05 | R04 (trazabilidad) |
| D06 y D12 | R02 (inventario), R07 (fuente de la verdad) |
| D07 | R08 (sistemas externos) |
| D10 | R12 (resistencia al cambio) |
| D09 | Orienta el tratamiento de R02, R03 y R07 |

## 7. Conclusión

El diagnóstico se apoya en el caso, en un conjunto de supuestos explícitos y en decisiones iniciales justificadas. Lo más importante que queda abierto es **quién es la fuente de la verdad del inventario**, porque condiciona el alcance, el diseño y el riesgo más crítico del proyecto. Las siguientes actividades deben partir de resolver esa pregunta con el negocio antes de elegir una arquitectura.
