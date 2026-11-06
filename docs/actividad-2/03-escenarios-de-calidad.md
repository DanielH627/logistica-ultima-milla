# Escenarios de calidad

## 1. Introducción

Un escenario de calidad convierte un atributo como "rendimiento" o "disponibilidad" en una situación concreta con una **medida que se puede comprobar**. Así, "el sistema debe ser rápido" pasa a ser "una consulta de estado responde en menos de 2 segundos en el 95 % de los casos".

Los escenarios de este documento cubren los requisitos de mayor impacto arquitectónico de `02-matriz-de-priorizacion.md`. Más adelante sirven para comparar alternativas (`04-comparacion-de-alternativas.md`) y para definir los criterios verificables de la decisión.


## 2. Formato de cada escenario

| Elemento | Qué describe |
|----------|--------------|
| **Fuente y estímulo** | Quién o qué provoca la situación y qué ocurre |
| **Entorno** | En qué condiciones de operación ocurre |
| **Respuesta esperada** | Qué debe hacer el sistema |
| **Medida** | Cómo se comprueba, con un umbral numérico |

## 3. Parámetros de referencia (supuestos)

Las medidas parten de un volumen hipotético, coherente con el supuesto A2-S02 (volumen moderado con picos).

| Parámetro | Valor supuesto |
|-----------|----------------|
| Pedidos por día (promedio) | 3.000 |
| Pedidos por hora en pico promocional | 1.000 |
| Horario operativo | 6:00 a 22:00 (16 horas) |
| Repartidores activos | 120 |
| Almacenes y tiendas con stock | 12 |
| Agentes de soporte conectados | 20 |
| Retención del historial | 12 meses |

Si el negocio entrega cifras distintas, se ajustan las medidas y se revisa si cambia la decisión.

## 4. Datos y consistencia

| ID | Atributo | Fuente y estímulo | Entorno | Respuesta esperada | Medida | Requisitos |
|----|----------|-------------------|---------|--------------------|--------|------------|
| **QS01** | Consistencia | Dos pedidos de canales distintos piden al mismo tiempo la última unidad de un producto | Operación normal | Solo uno obtiene la reserva; el otro recibe aviso de que no hay disponibilidad | 0 sobreventas en el 100 % de los casos concurrentes; respuesta en 3 segundos o menos | RNF03, RF04 |
| **QS02** | Consistencia | Una tienda actualiza sus existencias | Operación normal | La disponibilidad consolidada refleja el cambio | En 60 segundos o menos para el 95 % de las actualizaciones | RNF03, RF05 |
| **QS03** | Consistencia | Un cliente envía el mismo pedido por dos canales en menos de 10 minutos | Operación normal | El sistema lo detecta y lo marca antes de confirmarlo | Al menos 95 % de los duplicados exactos detectados; 0 despachos duplicados | RF02 |
| **QS04** | Trazabilidad | Soporte consulta qué pasó con un pedido de hace 30 días | Operación normal | Se muestra el historial completo: quién, qué y cuándo | Respuesta en 5 segundos o menos; 100 % de los cambios de estado registrados | RNF05, RF16, RF13 |

## 5. Rendimiento, carga, disponibilidad y resiliencia

| ID | Atributo | Fuente y estímulo | Entorno | Respuesta esperada | Medida | Requisitos |
|----|----------|-------------------|---------|--------------------|--------|------------|
| **QS05** | Rendimiento | Un agente de soporte consulta el estado de un pedido | Hora pico, 20 agentes conectados | El estado se muestra de inmediato | Menos de 2 segundos en el 95 % de las consultas | RNF01, RF13 |
| **QS06** | Escalabilidad | Un pico promocional lleva a 1.000 pedidos por hora | Operación bajo pico | Se registran y confirman sin degradarse ni intervención manual | Confirmación en 3 segundos o menos (95 %); 0 pedidos perdidos | RNF06, RF01 |
| **QS07** | Rendimiento y carga | El coordinador pide asignar 200 entregas pendientes | Horario operativo | Se propone una asignación sin afectar al resto del sistema | Propuesta en 30 segundos o menos; las consultas de otros dominios siguen en menos de 2 segundos | RF09, RNF01, RNF06 |
| **QS08** | Disponibilidad | Falla un componente interno no crítico (por ejemplo, el aviso de notificaciones) | Horario operativo | Registro de pedidos, estados y asignación siguen funcionando | Funciones críticas disponibles al menos 99,5 % del horario operativo; recuperación en 30 minutos o menos; pérdida de datos de 5 minutos o menos | RNF02 |
| **QS09** | Resiliencia | El servicio de geolocalización no responde | Horario operativo | Los pedidos se registran con la dirección "pendiente de validación" y se reintenta | 0 pedidos rechazados por la falla; validación completa en 15 minutos o menos tras la recuperación | RNF09, RF08 |
| **QS10** | Conectividad | Un repartidor pierde señal durante 20 minutos y completa 8 entregas | Operación en campo | Los reportes se guardan en el dispositivo y se envían al recuperar la señal | Sincronización en 2 minutos o menos; 0 reportes perdidos; se conserva la hora real de cada evento | RNF10, RF11 |

## 6. Seguridad, integración, evolución y operación

| ID | Atributo | Fuente y estímulo | Entorno | Respuesta esperada | Medida | Requisitos |
|----|----------|-------------------|---------|--------------------|--------|------------|
| **QS11** | Seguridad | Un usuario con rol de repartidor intenta ver datos personales de pedidos que no son suyos | Operación normal | Se deniega el acceso y se registra el intento | 100 % de los intentos bloqueados; registro con usuario, hora y recurso; datos personales protegidos en tránsito y en reposo | RNF04, RF18 |
| **QS12** | Interoperabilidad | El ERP/CRM publica una nueva versión de su interfaz con un campo modificado | Transición de versión | La integración sigue funcionando con la versión anterior mientras se migra | 0 caídas por el cambio; dos versiones del contrato coexisten durante la transición | RNF08, RF19 |
| **QS13** | Evolución | El negocio cambia el criterio de prioridad en la asignación de rutas | Mantenimiento | El cambio se hace sin tocar los demás dominios | Un solo dominio modificado; 3 días hábiles o menos entre la solicitud y la puesta en producción | RNF07, RF09 |
| **QS14** | Operabilidad y costo | Un equipo de 2 o 3 personas publica una nueva versión | Horario no operativo | La versión se despliega y se puede revertir con poco esfuerzo | Despliegue en 30 minutos o menos; reversión en 15 minutos o menos; interrupción de 5 minutos o menos | RNF13 |
| **QS15** | Observabilidad | Un pedido confirmado no cambia de estado en 4 horas | Horario operativo | El sistema alerta al coordinador | Alerta visible en 5 minutos o menos tras superar el umbral | RNF12, RF07 |

## 7. Cobertura de los requisitos

| Requisito | Escenarios |
|-----------|------------|
| RNF01 Rendimiento | QS05, QS07 |
| RNF02 Disponibilidad | QS08 |
| RNF03 Consistencia | QS01, QS02 |
| RNF04 Seguridad | QS11 |
| RNF05 Trazabilidad | QS04 |
| RNF06 Escalabilidad | QS06, QS07 |
| RNF07 Evolución | QS13 |
| RNF08 Interoperabilidad | QS12 |
| RNF09 Resiliencia | QS09 |
| RNF10 Conectividad | QS10 |
| RNF12 Observabilidad | QS15 |
| RNF13 Operabilidad y costo | QS14 |
| RNF11 Usabilidad | Sin escenario: es prioridad *Should* y se valida con usuarios en etapas posteriores |

Los requisitos funcionales críticos (RF02, RF04, RF05, RF09, RF11, RF19) quedan cubiertos por los escenarios que los citan.

## 8. Escenarios que más deciden la estructura

Cinco criterios servirán para comparar las alternativas. Cada uno se apoya en escenarios concretos:

| Criterio de comparación | Escenarios que lo miden | Qué pregunta responde |
|-------------------------|-------------------------|-----------------------|
| **Consistencia** | QS01, QS02 | ¿La estructura permite reservar inventario sin sobreventa? |
| **Carga** | QS06, QS07 | ¿Resiste los picos? ¿El cálculo de rutas ahoga al resto? |
| **Latencia** | QS05, QS07 | ¿Las consultas siguen siendo rápidas bajo carga? |
| **Autonomía** | QS12, QS13 | ¿Un dominio puede cambiar o desplegarse sin arrastrar a los demás? |
| **Costo operativo** | QS08, QS14 | ¿Un equipo pequeño puede operarla y recuperarla? |

QS01 y QS07 son los más reveladores. El primero favorece estructuras donde el inventario se gestiona de forma centralizada, y el segundo muestra cuándo **Ruteo** podría necesitar separarse del resto.

## 9. Qué falta confirmar

1. Las cifras reales de pedidos, picos, repartidores y puntos de stock.
2. El nivel de disponibilidad que exige el negocio y el horario operativo.
3. El tiempo máximo de recuperación y de pérdida de datos aceptables.
4. El tiempo de retención del historial según la normativa.
5. Si el cliente consulta su pedido directamente, lo que exigiría un escenario de rendimiento orientado al cliente.
