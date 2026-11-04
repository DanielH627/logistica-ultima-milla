# Alcance

## 1. Objetivo del sistema

Brindar a la empresa una **vista única y confiable del estado real de cada pedido**, desde que se registra hasta que se entrega o se resuelve una incidencia, de modo que almacén, rutas y soporte trabajen con la misma información.

El alcance se limita a la **Última milla**: la gestión del pedido desde que está confirmado hasta que llega al cliente.

 Criterio para decidir que entra: una función pertenece a la plataforma si es necesaria para **coordinar, rastrear o dar trazabilidad**  a un pedido en su camino hasta el cliente. Si solo produce o consume datos de otro dominio (cobros,catálogo, contabilidad ), pertenece a otro sistema.

 ## 2. Dentro del alcance

 | # | Responsabilidad | Problema del diagnóstico que atiende |
|---|-----------------|--------------------------------------|
| R1 | Recibir y consolidar los pedidos de todos los canales con un **identificador único** | Formatos distintos por canal; pedidos duplicados |
| R2 | **Detectar posibles duplicados** antes de confirmar un pedido | Duplicación de pedidos |
| R3 | **Validar y reservar inventario** consultando la disponibilidad consolidada por ubicación | Se vende lo que no se tiene |
| R4 | Mantener un **estado común del pedido** con un ciclo de vida compartido por todas las áreas | Cada área ve solo su fragmento |
| R5 | **Asignar entregas** a rutas y repartidores según disponibilidad y ventanas de entrega | Coordinación manual y rutas mal planificadas |
| R6 | **Rastrear entregas**: estado, ubicación y evidencia de entrega | Reporte de entrega no centralizado ni en tiempo real |
| R7 | **Gestionar incidencias** (entrega fallida, producto faltante, reclamo) desde el registro hasta el cierre | Soporte debe preguntar manualmente a otras áreas |
| R8 | **Solicitar notificaciones** al cliente ante cambios relevantes del pedido | Incertidumbre del cliente |
| R9 | Registrar el **historial de eventos** de cada pedido (quién, qué, cuándo) | Pérdida de trazabilidad |
| R10 | Ofrecer **indicadores operativos básicos** (cumplimiento, fallidas, tiempos, incidencias) | Imposibilidad de medir el desempeño |
| R11 | Controlar el **acceso por rol** a la información y a las acciones | Riesgo de seguridad y errores operativos |
| R12 | **Intercambiar información** con los sistemas externos definidos | Información dispersa |
 
## 3. Fuera del alcance
 
Delimitar lo que **no** se hace es tan importante como lo que sí se hace. Cada exclusión tiene una razón.
 
| Excluido | Por qué queda fuera | Quién lo cubre |
|----------|---------------------|----------------|
| Procesar cobros y reembolsos | Es una responsabilidad financiera especializada y regulada. La plataforma solo necesita saber **el estado** del pago | Sistema de pagos |
| Catálogo de productos, precios y promociones | Son datos maestros de la empresa; duplicarlos crearía nuevas inconsistencias | ERP/CRM |
| Venta y experiencia de compra (vitrina, carrito, checkout) | La plataforma **recibe** pedidos, no los vende | Canales de venta |
| Operación interna del almacén (ubicación en bodega, picking detallado) | El interés está en saber si el pedido está **preparado**, no en cómo se prepara | Procesos propios del almacén |
| Compras, reposición y gestión de proveedores | Pertenecen a la cadena de suministro, no a la entrega | ERP / área de abastecimiento |
| Transporte entre almacenes y primera milla | El enfoque es la última milla | Otros procesos logísticos |
| Cálculo base de mapas, rutas y distancias | Se consume como servicio; la plataforma decide **la asignación**, no construye el mapa | Servicio de geolocalización |
| Envío físico de mensajes (correo, SMS, mensajería) | La plataforma decide **qué** avisar y a quién, no cómo se transporta el mensaje | Sistema de notificaciones |
| Contabilidad, nómina y gestión de personal de repartidores | No afectan la coordinación del pedido | Áreas administrativas |
| Mantenimiento de flota y vehículos | Es una gestión aparte de la entrega | Áreas de flota / terceros |
| Emisión de facturas electrónicas *(supuesto)* | Es un proceso fiscal propio | Sistema de facturación / ERP |
| Logística inversa completa *(supuesto)* | En esta fase las devoluciones se registran como incidencias, sin gestionar el ciclo completo | A definir en fases posteriores |
| Elección de tecnologías, arquitectura interna y diseño de la solución | Corresponde a las siguientes actividades | Actividades posteriores |
 
## 4. Fronteras con sistemas externos
 
Para cada sistema, qué le toca a la plataforma y qué le toca al sistema externo.
 
| Sistema externo | La plataforma es responsable de | El sistema externo es responsable de |
|-----------------|---------------------------------|--------------------------------------|
| Sistema de pagos | Conocer el estado del pago y no despachar pedidos sin pago confirmado | Cobrar, reembolsar y conciliar |
| Servicio de geolocalización | Decidir qué entrega va con qué repartidor | Validar direcciones, calcular distancias y ubicar en tiempo real |
| Sistema de notificaciones | Definir el evento, el destinatario y el contenido a comunicar | Entregar el mensaje y reportar su estado |
| ERP o CRM interno | Registrar movimientos y estados de entrega; mantener la trazabilidad del pedido | Datos maestros: productos, clientes, precios, existencias oficiales *(por confirmar)* |
| Canales de venta *(supuesto)* | Consolidar pedidos y devolver estados y disponibilidad | Captar la venta y la experiencia del comprador |
| Facturación electrónica *(supuesto)* | Informar la entrega confirmada | Emitir y validar el documento fiscal |
 
## 5. Restricciones
 
| Tipo | Restricción |
|------|-------------|
| De negocio | La plataforma debe resolver la falta de trazabilidad antes de pensar en crecer; la solución debe ser coherente con la operación actual |
| De integración | Existe un ERP/CRM interno con el que la plataforma debe convivir; no se puede asumir que se reemplaza |
| De datos | La información del cliente (nombre, dirección, contacto) es personal y requiere protección según la normativa local aplicable *(por confirmar)* |
| De operación | Los repartidores trabajan en campo; la información que reporten depende de su conectividad *(por confirmar)* |
| De transición | Durante la adopción, los procesos actuales seguirán funcionando en paralelo |
| De la actividad | No se eligen tecnologías ni se diseña la solución final en esta etapa |
| Sin información | No se conocen plazos, presupuesto ni volumen real de pedidos; se registran como incertidumbres |
 
## 6. Decisiones de alcance por confirmar
 
1. **Fuente de la verdad del inventario:** ¿el ERP/CRM o la plataforma? Define si la plataforma **reserva** stock o solo lo **consulta**.
2. **Transportistas externos:** ¿se incluyen en esta fase o solo repartidores propios?
3. **Devoluciones:** ¿se gestionan como incidencias o como proceso completo?
4. **Facturación:** ¿la plataforma solo informa la entrega o participa en el documento fiscal?
5. **Autoservicio del cliente:** ¿el cliente consulta su pedido directamente o solo recibe avisos?
6. **Canales:** ¿cuáles se integran en la primera fase?

## 7. Resumen

La plataforma ** coordina, rastrea y da trazabilidad** a los pedidos de última mill. **No** cobra, no vende, no administra catálogo ni inventario maestro, no opera la bodega ni envía mensajes. Esos limites mantienen el sistema enfocado en el problema diagnosticado y evitan el crecimiento descontrolado de responsabilidades.