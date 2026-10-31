## 1.Enunciado del problema

La empresa Mercadeo Compartido no puede conocer el estado real de cada pedido porque la información está dispersa entre los canales de venta, los inventarios distribuidos, las rutas de entrega, y el área de soporte. Este desconocimiento está generando pedidos duplicados, inconsistencias en el inventario, las entregas están retrasadas o no se están realizando y se pierde la trazabilidad, afectando tanto la experiencia del cliente como la capacidad de la empresa para escalar su operación de manera controlada.

## 2.Situación Actual

En la actualidad un pedido en esta empresa atraviesa varias areas y cada una de ellas trabaja con su propia información:

1. **Entrada:**los lientes son atendidos por diferentes canales ya sea por el sitio web, app, marketplace o atención telefonica. Cada canal esta registrando los pedidos con su propio formato y su propio identificador.
2. **Inventario:**el stock esta ubicado en varios almacenes y tiendas. Cada punto actualiza sus existencias con distinta frecuencia, por lo que la disponibilidad que ve el cliente no siempre coincide con los datos reales.
3. **Despacho:**el almacen prepara el pedido y un coordinador define que repartidor hará la entrega y porque ruta, normalmente con la información sobre la carga y disponibilidad de cada repartidor.
4. **Entrega:**el repartidor entrega, sin embargo el reporte de entrega no se ve reflejado en tiempo real ni de manera centralizada.
5. **Incidencias:**al momento de un fallo el cliente contacta a soporte, y soporte debe preguntar a almacén o a logistica para determinar que falló.
   
## 3.¿Por qué es un problema?

El sintoma pricipal son los retrasos y errores en los pedidos. Las causas de fondo son:

- **No existe una vista única del pedido.**Cada área ve solo su fragmento del proceso, asi que nadie detecta a tiempo un pedido atrazado.
- **La falta de consistencia en la información del inventario.**Se vende lo que no se tiene.
-  **No hay un identificador universal entre canales.** debido a esto no se detectan facilmente los duplicados.
-  **La coordinación operativa entre almacenes, rutas y soporte se realiza manualmente**, haciendo la operación mas lenta y propensa a los errores.
-  **Falta Trazabilidad**. No hay un registro exacto y confiable de lo que sucede con cada pedido y cuando sucede alguna novedad.
  

  Análisis de "los cinco porqués":
  
  -Entregas tardías → ¿por qué? Porque los pedidos se estan despachando sin tener -clara la disponibilidad real en el inventario → ¿por qué? Porque el inventario -no permanece actualizado → ¿por qué? Porque cada departamento lo actualiza por -separado → ¿por qué? Porque no hay una consolidación centralizada de los -cambios -en el stock.


## 4. Consecuencias

### Para el cliente
- Incertidumbre acerca de las entregas.
- Pedidos cancelados o incompletos luego de haber sido confirmados debido a la falta de stock.
- Entregas tardías o fallidas.
- Alto tráfico de PQR en soporte.
- Perdida de confianza en la marca.

### Para la operación 
- Pedidos duplicados que generan un consumo falso en el inventario y uso ineficiente de los recursos de despacho.
- Mala planificación de las rutas porque la información es incierta.
- La conciliación de datos o empalmes se esta realizando de manera manual.
- Sobrecarga en las lineas de soporte.
- Repartidores con tiempos muertos o sobrecargados.

### Para el negocio
- Costos adicionales por reenvíos, devoluciones y compensaciones.
- Ventas perdidas por cancelaciones y clientes que no hacen recompra.
- Deterioro de la imagen de marca y baja valoración del servicio.
- Imposibilidad de usar indicadores confiables para medir el desempeño logístico.
- La falta de control hace que la operación sea dificil de escalar, a mayor flujo de pedidos es mayor el caos.


## 5.Variables relevantes
|Variable | Por qué importa|

|Canal de origen del pedido | Cada canal es propenso a generar duplicados o formatos distintos|
|Disponibilidad de Inventario por punto físico | Determina el mejor punto para hacer los despachos|
|Frecuencia de actualización del stock | Define cuanto puede divergir el
|inventario real del mostrado|
|Ventana de entrega proemtida | Condiciona la prioridad y la ruta|
|Capacidad y disponibilidad de repartidores | Limita cuantas entregas son viables|
|Zona Geográfica y distancias | Afecta tiempos y costos de entrega|
|Estado del pago | Un pedido sin pago confirmado no debería despacharse|
|Calidad de la dirección | Direcciones erradas causan entregas falidas|
|Volumen y picos de demanda | Afectan la capacidad operativa|
|Tiempo de respuesta ante incidencias | Determina el impacto de un problema en el cliente|


## 6.Oportunidades

Si la empresa logra contar con una visión clara y confiable de cada pedido, en consecuencia se reducirán los errores operativos, anticipará problemas antes de que causen un impacto negativo en el cliente, coordinará mejor sus almacenes, rutas y soporte, además, podrá tomar decisiones con datos reales. En ese orden de ideas se mejorará la experiencia del cliente, reducirá costos de reprocesos y sentará las bases para escalar la operación sin perder el control de la misma.


