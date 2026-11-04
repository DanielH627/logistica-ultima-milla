# Riesgos

## 1. Introducción

Un riesgo es un punto donde el sistema o el proceso puede fallar y causar pérdidas. Este registro identifica los riesgos principales de la plataforma de logística de última milla, los relaciona con los actores y flujos del diagrama de contexto y propone una **respuesta inicial**.

Se parte de los seis riesgos del caso y se agregan otros que surgen del análisis del problema, los actores y el alcance.


## 2. Escala de valoración

| Valor | Probabilidad | Impacto |
|-------|--------------|---------|
| 1 (Baja) | Poco probable que ocurra | Molestia menor, se corrige sin costo relevante |
| 2 (Media) | Puede ocurrir de forma ocasional | Afecta pedidos o clientes de forma visible, con costo moderado |
| 3 (Alta) | Ocurre con frecuencia o es casi seguro | Afecta la operación, el dinero o la reputación de forma seria |

**Nivel de riesgo = probabilidad × impacto**

| Resultado | Nivel |
|-----------|-------|
| 1 a 2 | Bajo |
| 3 a 4 | Medio |
| 6 | Alto |
| 9 | Crítico |

## 3. Registro de riesgos

| ID | Riesgo | Origen | Actor o flujo afectado | P | I | Nivel |
|----|--------|--------|------------------------|---|---|-------|
| R01 | Duplicación de pedidos | Caso | Canales de venta, cliente, agente telefónico (F1) | 3 | 2 | Alto (6) |
| R02 | Inconsistencias de inventario | Caso | Almacén, tienda, ERP/CRM (F2, F9) | 3 | 3 | Crítico (9) |
| R03 | Entregas fallidas o retrasadas | Caso | Repartidor, cliente (F5, F6) | 3 | 3 | Crítico (9) |
| R04 | Pérdida de trazabilidad | Caso | Todos los actores, historial del pedido | 2 | 3 | Alto (6) |
| R05 | Mala coordinación entre almacén, rutas y soporte | Caso | Almacén, coordinador de despacho, soporte (F5, F8) | 3 | 2 | Alto (6) |
| R06 | Crecimiento operativo sin control | Caso | Operación completa, gerente | 2 | 3 | Alto (6) |
| R07 | Fuente de la verdad ambigua entre el ERP/CRM y la plataforma | Análisis | ERP/CRM, almacén (F9) | 3 | 3 | Crítico (9) |
| R08 | Caída o lentitud de sistemas externos (pagos, geolocalización, notificaciones) | Análisis | Sistemas externos (F3, F4, F7) | 2 | 2 | Medio (4) |
| R09 | Fuga o uso indebido de datos personales | Análisis | Cliente, soporte, plataforma | 2 | 3 | Alto (6) |
| R10 | Información de campo que no llega por falta de conectividad | Análisis | Repartidor (F6) | 3 | 2 | Alto (6) |
| R11 | Direcciones erróneas o incompletas | Análisis | Cliente, geolocalización (F4) | 3 | 2 | Alto (6) |
| R12 | Resistencia al cambio y convivencia con procesos antiguos | Análisis | Tienda, almacén, soporte | 2 | 2 | Medio (4) |
| R13 | Crecimiento del alcance del proyecto | Análisis | Equipo del proyecto | 2 | 2 | Medio (4) |
| R14 | Punto único de falla por centralización | Análisis | Todos los actores | 2 | 3 | Alto (6) |
| R15 | Mala experiencia de uso para operadores y repartidores | Caso (riesgo general) | Tienda, almacén, repartidor | 2 | 2 | Medio (4) |

## 4. Detalle y respuesta inicial

La respuesta inicial es una **orientación de diseño**, no una solución técnica. Se concretará en actividades posteriores.

| ID | Causa | Consecuencia | Respuesta inicial | Señal de alerta |
|----|-------|--------------|-------------------|-----------------|
| R01 | No hay identificador común entre canales; el cliente repite el pedido por impaciencia | Consumo falso de inventario y despachos duplicados | Un identificador único por pedido y una regla para detectar posibles duplicados antes de confirmar | Aumento de pedidos con mismo cliente, producto y dirección en poco tiempo |
| R02 | Cada ubicación actualiza el stock por separado y con distinta frecuencia | Ventas de productos que no existen, cancelaciones | Reservar el stock al confirmar el pedido y definir una única fuente de consulta de disponibilidad | Cancelaciones por falta de stock; diferencias en conteos |
| R03 | Despacho sin información completa; direcciones erróneas; capacidad mal estimada | Reenvíos, costos extra y clientes insatisfechos | Validar dirección y pago antes de despachar; monitorear entregas en curso y alertar retrasos | Porcentaje de entregas fuera de la ventana prometida |
| R04 | Cada área registra su fragmento y no hay historial común | No se puede responder qué pasó con un pedido ni medir el desempeño | Registrar cada cambio de estado con quién, qué y cuándo | Casos de soporte sin historial; conciliaciones manuales |
| R05 | Coordinación por contacto manual entre áreas | Reprocesos, esperas y soporte sin respuestas | Un estado de pedido compartido y alertas visibles para todas las áreas | Tiempo promedio de respuesta a incidencias |
| R06 | Procesos que no escalan; más pedidos implican más caos | Pérdida de control en picos de demanda | Definir indicadores y capacidad esperada desde el inicio; revisar el diseño ante crecimiento | Aumento de incidencias proporcional al volumen |
| R07 | El caso no define si el inventario y el estado del pedido se gestionan en el ERP/CRM o en la plataforma | Datos contradictorios entre sistemas y desconfianza | Decidir y documentar qué sistema es la fuente de la verdad para cada dato | Discrepancias entre ambos sistemas |
| R08 | Dependencia de terceros fuera del control de la empresa | Pedidos detenidos o sin aviso al cliente | Definir un comportamiento alterno cuando un sistema externo no responde | Errores o demoras repetidas en las integraciones |
| R09 | La plataforma concentra nombres, direcciones y contactos | Sanciones, pérdida de confianza y daño a la marca | Acceso por rol, mínimo de datos necesario y registro de quién consulta qué | Accesos inusuales; solicitudes de datos de los clientes |
| R10 | Los repartidores trabajan en campo con conexión variable | Estados desactualizados y ubicación inexacta | Permitir reportar estados con retraso y sincronizar después | Entregas sin reporte durante largos periodos |
| R11 | Direcciones libres, mal escritas o sin referencias | Entregas fallidas y rutas ineficientes | Validar y normalizar la dirección al registrar el pedido | Alto porcentaje de entregas fallidas por dirección |
| R12 | El personal está acostumbrado a trabajar con su propia información | Registros en paralelo, datos desactualizados | Involucrar a los usuarios desde el diseño; planear una transición por etapas | Uso de hojas o canales paralelos |
| R13 | Se intenta resolver todo el proceso logístico desde el inicio | Retrasos y pérdida de foco | Mantener el alcance definido y evaluar cada solicitud adicional contra `03-alcance.md` | Funciones nuevas que no atienden un problema del diagnóstico |
| R14 | Todo el proceso depende de un sistema único | Si falla, se detiene la operación | Definir qué debe seguir funcionando ante fallas y cómo recuperarse | Dependencia total sin alternativa manual |
| R15 | Interfaces pensadas sin considerar el trabajo real | Los usuarios evitan el sistema o cometen errores | Probar con usuarios reales; mantener flujos simples para cada rol | Baja adopción; errores de captura |

## 5. Mapa de calor

| Impacto \ Probabilidad | 1 (Baja) | 2 (Media) | 3 (Alta) |
|------------------------|----------|-----------|----------|
| **3 (Alto)** | | R04, R06, R09, R14 | **R02, R03, R07** |
| **2 (Medio)** | | R08, R12, R13, R15 | R01, R05, R10, R11 |
| **1 (Bajo)** | | | |

## 6. Prioridades

Los tres riesgos **críticos** son los que más deben guiar el diseño posterior:

1. **R02, inconsistencias de inventario**: es la causa raíz del síntoma más visible, vender lo que no se tiene.
2. **R03, entregas fallidas o retrasadas**: es lo que el cliente percibe y lo que más cuesta.
3. **R07, fuente de la verdad ambigua**: no es un problema de operación sino de definición. Si no se resuelve, afecta a R02 y R04.

Los riesgos **altos** se atienden en el diseño. Los **medios** se monitorean.

## 7. Relación con otros documentos

- **Actores y flujos:** cada riesgo señala el actor o flujo afectado (`02-actores-y-necesidades.md` y `04-diagrama-de-contexto.md`).
- **Alcance:** R13 y R14 se controlan manteniendo los límites de `03-alcance.md`.
- **Supuestos y decisiones:** R07 y la valoración de probabilidad e impacto se registran como incertidumbres en `06-supuestos-y-decisiones.md`.

## 8. Qué falta confirmar

1. Datos reales de la operación: porcentaje de entregas fallidas, pedidos duplicados y cancelaciones por stock.
2. Normativa aplicable a datos personales y sus exigencias.
3. Conectividad real de los repartidores en las zonas de entrega.
4. Existencia de procesos alternos si la plataforma no estuviera disponible.
5. Volumen y picos de demanda esperados.
