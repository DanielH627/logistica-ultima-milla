## 1. Introducción
Entendiendo que un actor es cualquier persona, área o sistea que interactúa con nuestra plataforma de logistica de ultima milla sea porque la usa, le entrega información o depende de lo que la plataforma produce. a continuación se presentarán los tres grupos en los que se clasificarón los actores:

- Actores humanos: Estas son todas las personas que usan la plataforma o dependen de ella.
- Sistemas externos: Son todos los sistemas que intercambian información con la plataforma sin formar parte de ella.
- Actores adicionales (supuestos): Estos son actores que no se mensionan directamente en el caso, pero desde al análisis se hace necesario considerar.
  
  ## 2. Resumen de actores

  | # | Actor | Tipo | Origen | Prioridad |
|---|-------|------|--------|-----------|
| 1 | Cliente | Humano | Caso | Primario |
| 2 | Operador de comercio o tienda | Humano | Caso | Primario |
| 3 | Administrador de almacén | Humano | Caso | Primario |
| 4 | Repartidor | Humano | Caso | Primario |
| 5 | Soporte al cliente | Humano | Caso | Primario |
| 6 | Sistema de pagos | Sistema externo | Caso | Secundario |
| 7 | Servicio de geolocalización | Sistema externo | Caso | Secundario |
| 8 | Sistema de notificaciones | Sistema externo | Caso | Secundario |
| 9 | ERP o CRM interno | Sistema externo | Caso | Secundario (crítico) |
| 10 | Coordinador de despacho / logística | Humano | Supuesto | Primario |
| 11 | Gerente de operaciones / analista | Humano | Supuesto | Primario |
| 12 | Administrador de la plataforma (TI) | Humano | Supuesto | Secundario |
| 13 | Canales de venta (web, app, marketplace) | Sistema externo | Supuesto | Secundario (crítico) |
| 14 | Agente de atención telefónica | Humano | Supuesto | Primario |
| 15 | Operador logístico tercero (transportista externo) | Humano / organización | Supuesto | Secundario |
| 16 | Receptor del pedido (distinto del comprador) | Humano | Supuesto | Secundario |
| 17 | Sistema de facturación electrónica | Sistema externo | Supuesto | Secundario |
| 18 | Proveedor / abastecimiento | Humano / sistema | Supuesto | Secundario |

## 3. Actores humanos 

| Actor | Rol | Qué necesita de la plataforma | Qué aporta al sistema | Cómo interactúa |
|-------|-----|-------------------------------|-----------------------|-----------------|
| **Cliente** | Compra el producto y espera la entrega | Conocer el estado de su pedido y la hora estimada de llegada; recibir avisos ante cambios o retrasos; poder reportar un problema | Datos del pedido, dirección, ventana de entrega preferida, confirmación de recepción, reclamos | Consulta el estado del pedido; recibe notificaciones; confirma o califica la entrega; abre incidencias |
| **Operador de comercio o tienda** | Atiende ventas y despachos desde un punto físico | Ver los pedidos que debe preparar o despachar; conocer el stock real de su punto; informar faltantes | Registro de pedidos (ventas en tienda), actualización de existencias, confirmación de pedido listo | Registra pedidos; actualiza disponibilidad; marca pedidos como preparados; consulta su cola de trabajo |
| **Administrador de almacén** | Controla inventario y preparación de pedidos | Inventario consolidado y confiable; lista de pedidos por preparar con prioridad; alertas de faltantes o discrepancias | Movimientos de stock, ajustes de inventario, confirmación de salida de mercancía | Valida inventario; reserva y descuenta stock; confirma despacho; gestiona devoluciones y faltantes |
| **Repartidor** | Entrega físicamente el pedido | Lista de entregas del día; ruta clara; datos de contacto y dirección; forma de reportar novedades | Estado de cada entrega, ubicación, evidencia de entrega, motivo de fallo | Recibe asignaciones; reporta recogida, en camino, entregado o fallido; registra evidencia |
| **Soporte al cliente** | Atiende consultas, reclamos e incidencias | Ver el historial completo del pedido sin llamar a otras áreas; herramientas para resolver o escalar incidencias | Registro de incidencias, resoluciones, compensaciones y comunicaciones con el cliente | Consulta el estado; crea y gestiona incidencias; escala a almacén o ruta; cierra casos |

## 4. Sistemas externos 

| Sistema | Propósito | Qué recibe de la plataforma | Qué entrega a la plataforma |
|---------|-----------|-----------------------------|------------------------------|
| **Sistema de pagos** | Procesar, confirmar y conciliar cobros y reembolsos | Solicitud de cobro, monto, referencia del pedido, solicitud de reembolso | Confirmación, rechazo o reversión del pago; estado del cobro |
| **Servicio de geolocalización** | Ubicar direcciones, calcular distancias y rastrear repartidores | Direcciones, coordenadas, puntos de origen y destino | Coordenadas validadas, distancias y tiempos estimados, ubicación en tiempo real |
| **Sistema de notificaciones** | Enviar avisos al cliente y a otros actores por distintos medios | Mensaje, destinatario, tipo de evento (pedido confirmado, en camino, entregado, retraso) | Estado del envío (enviado, entregado, fallido) |
| **ERP o CRM interno** | Mantener datos maestros de productos, clientes, finanzas e inventario corporativo | Pedidos confirmados, movimientos de inventario, estados de entrega, incidencias | Catálogo de productos, datos de clientes, existencias oficiales, precios, reglas de negocio |
 
 **ERP/CRM:** el ERP/CRM es uno de los puntos más delicados debido a que este es interno a la empresa pero externo a la plataforma. Ahi queda abierta la pregunta de quién es la **fuente de la verdad** del inventario y del estado del pedido.


 ## 5. Actores adicionales

Estos actores no aparecen directamente en este caso, sin embargo, están icluidos porque el análisis del proceso indica que alguien debe cumplir con ciertas funciones. Cabe anotar que cada uno de estos actores debe ser confirmado por el negocio.

### 5.1 Humanos
 
| Actor | Por qué se incluye | Qué necesita | Qué aporta | Cómo interactúa |
|-------|--------------------|--------------|------------|-----------------|
| **Coordinador de despacho / logística** | El caso describe que alguien asigna repartidores y rutas, pero no lo nombra como actor | Pedidos listos, disponibilidad y carga de repartidores, mapa de rutas, alertas de retraso | Asignación de pedidos a rutas y repartidores, reasignaciones ante imprevistos | Asigna y reasigna rutas; monitorea entregas en curso; atiende excepciones |
| **Gerente de operaciones / analista** | La empresa necesita medir el desempeño logístico con datos confiables | Indicadores de cumplimiento, tiempos, entregas fallidas, costos, incidencias | Reglas operativas, metas, decisiones de mejora | Consulta reportes y paneles; define políticas y prioridades |
| **Administrador de la plataforma (TI)** | Alguien debe gestionar accesos, parámetros y salud de la plataforma | Control de usuarios y permisos, registros de auditoría, monitoreo | Configuración, gestión de roles, soporte técnico | Crea usuarios y perfiles; configura parámetros; revisa registros |
| **Agente de atención telefónica** | El caso menciona varios canales de entrada, y la atención telefónica suele ser uno | Registrar pedidos de forma ágil y verificar duplicados antes de crearlos | Pedidos tomados por teléfono | Registra pedidos; consulta estado; deriva incidencias a soporte |
| **Operador logístico tercero (transportista externo)** | Ante picos de demanda o zonas lejanas, una empresa de retail suele apoyarse en terceros | Pedidos asignados, ventanas de entrega, reglas de reporte de estado | Entregas realizadas y su estado, evidencia de entrega | Recibe asignaciones; reporta estados; cierra entregas |
| **Receptor del pedido** | Quien recibe no siempre es quien compró | Información de entrega y datos de contacto | Confirmación de recibo, firma o código | Recibe el pedido; confirma la entrega |
| **Proveedor / abastecimiento** | El inventario distribuido depende de reposiciones | Visibilidad de faltantes y reposiciones esperadas | Ingresos de mercancía | Su información llega vía ERP o almacén (interacción indirecta) |
 

### 5.2 Sistemas
 
| Sistema | Por qué se incluye | Qué recibe de la plataforma | Qué entrega a la plataforma |
|---------|--------------------|-----------------------------|------------------------------|
| **Canales de venta (sitio web, app móvil, marketplace)** | Son el origen de los pedidos y de los duplicados; el caso los menciona como "canales" sin tratarlos como actores | Disponibilidad de stock, estado del pedido, confirmaciones | Pedidos nuevos, cancelaciones, cambios de dirección |
| **Sistema de facturación electrónica** | La venta y la entrega pueden requerir documentos fiscales | Datos del pedido y de la entrega confirmada | Número y estado del documento fiscal |
 

## 6. Prioridad de los actores
 
- **Primarios:** usan directamente la plataforma o dependen de ella para su trabajo diario. Sin ellos el proceso no funciona (cliente, operador de tienda, almacén, repartidor, soporte, coordinador de despacho, agente telefónico, gerente).
- **Secundarios:** prestan un servicio a la plataforma o consumen información de ella. No la operan a diario (pagos, geolocalización, notificaciones, ERP/CRM, canales de venta, facturación, administrador TI, transportista externo, receptor, proveedor).
- **Críticos:** el ERP/CRM y los canales de venta son secundarios por rol, pero críticos por impacto: un fallo en ellos afecta directamente la consistencia de pedidos e inventario.
  

## 7. Flujos de información principales
 
| # | Flujo | De → A |
|---|-------|--------|
| F1 | Pedido nuevo | Canal de venta / Cliente / Agente telefónico → Plataforma |
| F2 | Consulta y reserva de inventario | Plataforma ↔ Almacén / ERP |
| F3 | Solicitud y confirmación de pago | Plataforma ↔ Sistema de pagos |
| F4 | Validación de dirección y cálculo de ruta | Plataforma ↔ Geolocalización |
| F5 | Asignación de entrega | Coordinador de despacho → Plataforma → Repartidor / Transportista |
| F6 | Reporte de estado y ubicación | Repartidor / Transportista → Plataforma |
| F7 | Avisos al cliente | Plataforma → Sistema de notificaciones → Cliente |
| F8 | Incidencias y reclamos | Cliente → Soporte → Plataforma (y de vuelta al cliente) |
| F9 | Sincronización de datos maestros y movimientos | Plataforma ↔ ERP/CRM |
| F10 | Indicadores de desempeño | Plataforma → Gerente / analista |
 
 
## 8. Dudas por confirmar
 
1. ¿Quién es la fuente de la verdad del inventario y del estado del pedido: el ERP/CRM o la plataforma?
2. ¿La empresa usa transportistas externos o solo repartidores propios?
3. ¿Cuáles son los canales de venta reales y cuáles se integran directamente?
4. ¿Quién asigna hoy las rutas: una persona específica o cada almacén?
5. ¿El cliente podrá consultar el estado por sí mismo o solo recibirá avisos?
6. ¿Se requiere facturación electrónica ligada a la entrega?


## 9. Conclusiones
 
- Los actores que concentran más complejidad son el **ERP/CRM**, los **canales de venta** y el **repartidor**: los dos primeros por consistencia de datos y el tercero por la información en tiempo real desde campo.
- El **soporte al cliente** es el principal beneficiario de la visibilidad centralizada.
- Los actores adicionales deben validarse antes de incluirlos en el alcance, y su inclusión se refleja en el documento de supuestos.