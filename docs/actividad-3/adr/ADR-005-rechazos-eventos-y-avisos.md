# ADR-005: Rechazos de negocio como resultados, evento registrado tras guardar y aviso sin esperar

| Campo | Valor |
|-------|-------|
| **Estado** | Aceptada; la regla del evento fue ajustada por el ADR-007 en la Actividad 4 (ver sección 8) |
| **Fecha** | 16 de noviembre de 2026 |
| **Autor** | [Tu nombre completo] |
| **Actividad** | Actividad 3: diseño y dominio |
| **Decisiones relacionadas** | ADR-003; ADR-004; contratos C4 y C5 |

## 1. Contexto

El caso de uso de asignación puede terminar de varias maneras: con éxito, con un rechazo previsto por el negocio (la entrega ya tiene repartidor, la ruta está saturada, etc.) o con una falla inesperada. Además, debe registrar el hecho ocurrido y avisar al repartidor. Hay que decidir **cómo se informan los rechazos** y **en qué orden y con qué garantías** se registran el evento y el aviso.

### Fuerzas que influyen en la decisión

| Fuerza | Origen |
|--------|--------|
| Quien invoque el caso de uso debe poder distinguir un rechazo previsto de una falla | Contrato de aplicación |
| El coordinador debe entender qué ocurrió y qué hacer | Escenarios de aceptación |
| Una falla de un tercero no debe detener la operación | QS08 y QS09 |
| Toda asignación debe quedar registrada: quién, qué y cuándo | INV06 |

## 2. Decisión

Se adoptan tres reglas:

1. **Los rechazos de negocio son resultados, no excepciones.** El caso de uso devuelve un resultado con un código estable y un mensaje legible. Las excepciones se reservan para fallas inesperadas.
2. **El evento se registra después de guardar la entrega.** Así solo se registra lo que efectivamente ocurrió.
3. **El aviso al repartidor no revierte la asignación.** Si la solicitud falla, la asignación se mantiene y el resultado indica que el aviso no se pudo solicitar.

### Códigos de rechazo

| Código | Situación |
|--------|-----------|
| `DELIVERY_NOT_FOUND` | La entrega no existe |
| `DELIVERY_ALREADY_ASSIGNED` | La entrega ya tiene repartidor, o fue asignada por otra operación al mismo tiempo |
| `DELIVERY_NOT_PENDING` | La entrega no está pendiente de asignación |
| `COURIER_NOT_ACTIVE` | El repartidor está inactivo |
| `ROUTE_SATURATED` | La ruta alcanzó su capacidad |
| `AVAILABILITY_UNVERIFIABLE` | No se pudo verificar la disponibilidad |

**Los códigos son estables:** quien los consume puede depender de ellos. Los mensajes de texto pueden cambiar.

## 3. Alternativas consideradas

### Informar los rechazos

| Alternativa | Resultado | Motivo |
|-------------|-----------|--------|
| **Resultado con código y mensaje** | **Elegida** | Es una situación prevista y el consumidor recibe información accionable |
| Excepciones para todo | Descartada | Mezcla situaciones normales del negocio con fallas inesperadas |
| Un tipo de resultado genérico de una biblioteca externa | Descartada | Agrega una dependencia sin necesidad en esta etapa |

### Registrar el evento

| Alternativa | Resultado | Motivo |
|-------------|-----------|--------|
| **Registrar después de guardar** | **Elegida por ahora** | Evita registrar hechos que no ocurrieron |
| Registrar antes de guardar | Descartada | Si el guardado falla, queda registrado un hecho falso |
| Guardar el evento junto con la entrega y publicarlo después (bandeja de salida) | Aplazada a la Actividad 4 | Es la solución más robusta, pero requiere persistencia real |

### Avisar al repartidor

| Alternativa | Resultado | Motivo |
|-------------|-----------|--------|
| **Solicitar sin revertir si falla** | **Elegida** | Una falla de un tercero no debe deshacer una asignación válida |
| Revertir la asignación si el aviso falla | Descartada | Convertiría la disponibilidad de un tercero en condición del negocio |

## 4. Justificación

### Razones técnicas

- **Claridad del contrato.** Quien invoca el caso de uso revisa un resultado en lugar de capturar errores de distinto tipo.
- **Coherencia entre capas.** El dominio lanza errores propios del negocio y la aplicación los traduce a códigos, de modo que los detalles del dominio no salen hacia el exterior.
- **Aislamiento de terceros.** La falla del sistema de notificaciones queda contenida.

### Razones de negocio

- **El coordinador recibe una explicación y una acción posible**, por ejemplo "seleccione otro repartidor", en lugar de un error genérico.
- **Una entrega asignada no se deshace por un problema de comunicación**, lo que evita retrabajos.

## 5. Consecuencias

| Lo que se gana | Lo que se cede |
|----------------|----------------|
| Contrato claro y estable | El consumidor debe revisar el resultado en lugar de capturar errores |
| Operación resistente a fallas de terceros | El repartidor podría enterarse tarde si el aviso falla |
| Solo se registran hechos ocurridos | Si falla el registro del evento, la entrega queda asignada sin evento |

## 6. Riesgos y mitigaciones

| Riesgo | Mitigación |
|--------|------------|
| **Evento perdido** si el registro falla tras guardar la entrega | Bandeja de salida en la Actividad 4: el evento se guarda junto con la entrega y se publica después |
| **Aviso no entregado** | Reintento o registro como pendiente, y el resultado indica `NoticeRequested` en falso |
| **Códigos que cambian y rompen a los consumidores** | Tratar los códigos como parte del contrato versionado |

## 7. Cómo se verificará

| Qué se comprueba | Cómo |
|------------------|------|
| Cada rechazo devuelve su código | Pruebas del caso de uso de los escenarios EA02, EA04, EA06, EA07, EA08 y EA09 |
| Un aviso fallido no revierte la asignación | Prueba `Execute_WhenNoticeFails_AssignmentStands` |
| El evento se registra solo si hubo éxito | Pruebas de los escenarios de rechazo, que verifican que no se registra ningún evento |

## 8. Decisiones pendientes relacionadas

- ~~Garantía de que el evento no se pierda (bandeja de salida, Actividad 4).~~ **Resuelta en la Actividad 4 por el [ADR-007](../../actividad-4/adr/ADR-007-bandeja-de-salida-reintentos-y-mensajes-muertos.md):** el evento se guarda en la misma transacción que la entrega y un procesador lo entrega después. Las reglas 1 (rechazos como resultados) y 3 (el aviso no revierte la asignación) se mantienen; la regla 2 ("registrar después de guardar") queda reemplazada por la bandeja de salida.
- ~~Política de reintento de los avisos.~~ **Resuelta por el ADR-007:** tres intentos con espera exponencial y estado `Dead`.

## 9. Condiciones de revisión

La decisión se revisa si ocurre alguna de estas condiciones:

1. Se pierden eventos con una frecuencia relevante.
2. Los consumidores necesitan más detalle del que ofrece un código y un mensaje.
3. Los avisos pasan a ser críticos para el negocio y su falla debe tratarse como un rechazo.

## 10. Referencias

- `docs/actividad-3/05-contextos-y-contratos.md` (contratos C4 y C5, sección 5)
- `docs/actividad-3/06-casos-de-uso-puertos-adaptadores.md`
- `docs/actividad-2/03-escenarios-de-calidad.md` (QS08 y QS09)
