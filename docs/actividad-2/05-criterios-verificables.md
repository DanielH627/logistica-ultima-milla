# Criterios verificables

## 1. Introducción

Una decisión arquitectónica solo es útil si se puede comprobar. Este documento define **cómo se verifica que la estructura elegida en `adr/ADR-001-estructura-de-la-plataforma.md` responde a las necesidades del sistema**: qué se mide, cuál es el umbral de aceptación, cómo se comprueba y en qué momento.

Los criterios se agrupan en cuatro bloques:

| Bloque | Pregunta que responde | Prefijo |
|--------|-----------------------|---------|
| **Estructura** | ¿Se respetan las reglas de la decisión? | CE |
| **Calidad** | ¿Se cumplen los escenarios de calidad? | QS (de `03-escenarios-de-calidad.md`) |
| **Negocio** | ¿Se resuelve el problema diagnosticado? | CN |
| **Revisión** | ¿Sigue siendo válida la decisión con el tiempo? | CR |


## 2. Momentos de verificación

La plataforma todavía no existe, así que no todos los criterios pueden comprobarse hoy.

| Momento | Qué se puede comprobar |
|---------|------------------------|
| **Diseño** | Que los dominios, los límites y las interfaces estén definidos y documentados |
| **Construcción** | Que el código respete los límites entre módulos |
| **Pruebas** | Que se cumplan las medidas de los escenarios de calidad |
| **Operación** | Que los indicadores de negocio mejoren y que las condiciones de revisión no se activen |

## 3. Criterios de estructura (CE)

Verifican las siete reglas de la sección 2 del ADR.

| ID | Criterio | Umbral de aceptación | Cómo se verifica | Momento | Estado hoy |
|----|----------|----------------------|------------------|---------|------------|
| CE01 | Cada dominio tiene su propio módulo, con modelo, reglas y datos | 5 de 5 módulos definidos (Pedidos, Inventario, Ruteo, Entregas, Incidencias) con su glosario | Revisar que cada módulo tenga responsabilidades y términos documentados | Diseño | Cumplido en el análisis (`04-comparacion-de-alternativas.md`) |
| CE02 | Las dependencias entre módulos pasan solo por interfaces públicas | 0 accesos directos a los datos o reglas internas de otro módulo | Revisión de dependencias entre módulos en cada cambio | Construcción | Pendiente de construcción |
| CE03 | Reservar inventario y cambiar el estado del pedido ocurren como una sola operación | 0 estados intermedios visibles entre la reserva y el cambio de estado | Prueba de reservas simultáneas (QS01) | Pruebas | Pendiente de construcción |
| CE04 | Las integraciones externas pasan por adaptadores con contratos versionados | 100 % de las integraciones (ERP/CRM, pagos, geolocalización, notificaciones y canales) con adaptador y versión de contrato | Inventario de integraciones y de sus contratos | Diseño y construcción | Definido en el ADR; la implementación queda pendiente |
| CE05 | Ruteo está aislado y puede extraerse | Interfaz de Ruteo definida; ningún otro módulo usa sus detalles internos; el cálculo de rutas no bloquea a los demás (QS07) | Revisión de dependencias y prueba de carga del cálculo de rutas | Construcción y pruebas | Pendiente de construcción |
| CE06 | Existe un único historial de eventos por pedido | 100 % de los cambios de estado registrados con quién, qué y cuándo | Contraste entre cambios de estado y registros (QS04) | Pruebas | Pendiente de construcción |
| CE07 | No hay servicios independientes sin que se cumplan las condiciones de revisión | 0 servicios separados sin una condición de la sección 6 activada y registrada | Revisión de cada propuesta de separación frente a CR01 a CR05 | Operación | Aplicable desde el inicio de la construcción |

## 4. Criterios de calidad (QS)

Cada escenario de `03-escenarios-de-calidad.md` se **aprueba cuando se cumple su medida**. Aquí se indica cómo comprobarlo.

| Escenario | Qué se comprueba | Método de verificación | Evidencia esperada |
|-----------|------------------|------------------------|--------------------|
| QS01 | 0 sobreventas con reservas simultáneas; respuesta en 3 segundos o menos | Prueba con solicitudes concurrentes sobre la misma unidad de stock | Registro con resultados de las pruebas |
| QS02 | Cambio de stock visible en 60 segundos o menos (95 %) | Cambiar existencias y medir cuándo se refleja | Tiempos medidos |
| QS03 | Al menos 95 % de duplicados exactos detectados; 0 despachos duplicados | Enviar pedidos repetidos por distintos canales | Conteo de detectados frente a enviados |
| QS04 | Historial completo en 5 segundos o menos; 100 % de cambios registrados | Consultar el historial de pedidos de 30 días atrás | Comparación entre cambios y registros |
| QS05 | Consulta de estado en menos de 2 segundos (95 %) con 20 usuarios | Prueba de carga con 20 consultas simultáneas | Distribución de tiempos |
| QS06 | Confirmación en 3 segundos o menos (95 %) con 1.000 pedidos por hora; 0 pedidos perdidos | Prueba de carga que simule el pico | Tiempos y conteo de pedidos |
| QS07 | Propuesta de asignación en 30 segundos o menos para 200 entregas; consultas de otros dominios en menos de 2 segundos | Ejecutar la asignación mientras se miden las demás consultas | Tiempos de ambos |
| QS08 | Funciones críticas disponibles al menos 99,5 % del horario operativo; recuperación en 30 minutos o menos | Simular la falla de un componente no crítico y medir | Tiempo de recuperación y disponibilidad |
| QS09 | 0 pedidos rechazados con geolocalización caída; validación en 15 minutos o menos tras la recuperación | Simular la caída del servicio externo | Pedidos registrados y tiempo de validación |
| QS10 | Sincronización en 2 minutos o menos; 0 reportes perdidos tras 20 minutos sin señal | Simular un repartidor sin conexión | Reportes enviados frente a recibidos |
| QS11 | 100 % de accesos indebidos bloqueados y registrados | Intentar acceder con un rol sin permiso | Intentos bloqueados y su registro |
| QS12 | 0 caídas con un cambio de versión del ERP/CRM; dos versiones del contrato coexisten | Simular una nueva versión del contrato | Integración operando con ambas versiones |
| QS13 | Un solo dominio modificado; 3 días hábiles o menos | Aplicar un cambio de criterio de asignación | Lista de módulos tocados y tiempo empleado |
| QS14 | Despliegue en 30 minutos o menos; reversión en 15 minutos o menos | Ensayo de despliegue y de reversión con el equipo | Tiempos medidos |
| QS15 | Alerta en 5 minutos o menos tras 4 horas sin cambio de estado | Dejar un pedido sin avanzar y medir | Hora de la alerta |

## 5. Criterios de negocio (CN)

Comprueban que la decisión **resuelve el problema** de la Actividad 1, no solo que cumple medidas técnicas. Se miden una vez en operación, comparando con una **línea base** tomada antes de adoptar la plataforma.

| ID | Problema del negocio | Indicador | Meta (supuesto) | Cómo se mide |
|----|----------------------|-----------|-----------------|--------------|
| CN01 | Pedidos duplicados (R01) | Pedidos duplicados despachados al mes | 0 | Conciliación mensual de pedidos con mismo cliente, producto y dirección |
| CN02 | Inconsistencias de inventario (R02) | Cancelaciones por falta de stock | Reducción de al menos 80 % frente a la línea base | Conteo mensual de cancelaciones con ese motivo |
| CN03 | Pérdida de trazabilidad (R04) | Pedidos activos sin actualización de estado en más de 4 horas y sin alerta | 0 | Revisión de pedidos activos |
| CN04 | Mala coordinación entre áreas (R05) | Consultas de soporte resueltas sin contactar a otras áreas | Al menos 90 % | Muestra de casos cerrados |
| CN05 | Entregas retrasadas (R03) | Entregas dentro de la ventana prometida | Al menos 90 % | Comparación entre ventana prometida y hora real |
| CN06 | Entregas fallidas por dirección (R11) | Entregas fallidas cuyo motivo es la dirección | Reducción de al menos 50 % frente a la línea base | Conteo mensual por motivo de fallo |

**Importante:** la empresa no conoce hoy sus cifras base. Medirlas es un paso previo, y sin ellas las metas de reducción no se pueden evaluar. Queda registrado como pendiente.

## 6. Criterios de revisión de la decisión (CR)

Corresponden a las condiciones de la sección 9 del ADR. Si alguna se cumple, la decisión se revisa y el primer paso previsto es **extraer Ruteo** como servicio independiente.

| ID | Criterio | Condición que activa la revisión | Frecuencia de control |
|----|----------|----------------------------------|-----------------------|
| CR01 | Carga | Pico sostenido por encima de 3.000 pedidos por hora, o QS07 incumplido en más del 10 % de las ejecuciones | Mensual |
| CR02 | Latencia | Consultas de otros dominios por encima de 2 segundos en más del 5 % de los casos durante el cálculo de rutas | Mensual |
| CR03 | Autonomía | Ruteo cambia y se despliega con una frecuencia mucho mayor que el resto, o lo mantiene un equipo distinto | Trimestral |
| CR04 | Costo operativo | El equipo ya puede monitorear, desplegar y atender más de un servicio | Trimestral |
| CR05 | Supuestos de base | Cambian el tamaño del equipo, el volumen esperado o la fuente de la verdad del inventario | Cada vez que cambie |

## 7. Verificación de esta entrega

Comprobación de los entregables de la Actividad 2 frente a lo que pide el docente.

| Entregable | Dónde está |
|------------|------------|
| Catálogo de requisitos funcionales y no funcionales | `01-catalogo-de-requisitos.md` |
| Matriz de priorización | `02-matriz-de-priorizacion.md` |
| Escenarios de calidad con métricas | `03-escenarios-de-calidad.md` |
| Comparación de al menos dos alternativas | `04-comparacion-de-alternativas.md` |
| Decisión argumentada con trade-offs (ADR) | `adr/ADR-001-estructura-de-la-plataforma.md` |
| Criterios verificables | Este documento |
| README con problema, requisitos prioritarios y decisión | `README.md` de la raíz |
| Historial de commits | Repositorio de GitHub |
| Video de sustentación | Enlace en el README |

## 8. Qué falta confirmar

1. La línea base actual de duplicados, cancelaciones, entregas fuera de ventana y entregas fallidas.
2. Las metas realistas de cada indicador de negocio, acordadas con quien opera el negocio.
3. Quién es responsable de cada control periódico (CR01 a CR05).
4. Cómo se medirán la carga y la latencia una vez la plataforma opere.
