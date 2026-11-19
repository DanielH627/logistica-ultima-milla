# ADR-004: Coordinación entre Entregas y Ruteo mediante reserva previa con liberación compensatoria

| Campo | Valor |
|-------|-------|
| **Estado** | Aceptada, con revisión periódica (ver sección 9) |
| **Fecha** | 16 de noviembre de 2026 |
| **Autor** | [Tu nombre completo] |
| **Actividad** | Actividad 3: diseño y dominio |
| **Decisiones relacionadas** | ADR-001 (regla 3 y camino de evolución); ADR-003; contrato C3 |

## 1. Contexto

Asignar un repartidor a una entrega toca **dos agregados de dos contextos distintos**: la entrega, que pertenece a Entregas, y la ruta, que pertenece a Ruteo. Cada uno protege su propia regla: la entrega no puede tener dos repartidores (INV01) y la ruta no puede superar su capacidad (INV04).

Si solo se consulta la disponibilidad antes de asignar, dos asignaciones simultáneas pueden pasar la consulta al mismo tiempo y superar la capacidad de la ruta (escenario EA12). Del mismo modo, dos coordinadores podrían asignar la misma entrega a repartidores distintos (EA11). El documento `05-contextos-y-contratos.md` dejó abierta la forma de coordinar ambas partes (contrato C3).

### Fuerzas que influyen en la decisión

| Fuerza | Origen |
|--------|--------|
| La capacidad de una ruta nunca debe superarse, ni siquiera bajo concurrencia | INV04, EA12 |
| Una entrega no debe tener dos repartidores | INV01, EA11 |
| No dejar cambios a medias si algo falla | ADR-001, regla 3 |
| Mantener los módulos desacoplados en su almacenamiento | ADR-001, ADR-003 |
| Poder extraer Ruteo como servicio en el futuro | ADR-001, sección 9 |

## 2. Decisión

**Ruteo reserva el cupo de forma atómica antes de que se guarde la entrega; si el guardado falla, el cupo se libera.**

### Secuencia

1. Se carga la entrega y se consulta la disponibilidad del repartidor.
2. El dominio de `Delivery` ejecuta la asignación en memoria, protegiendo INV01 e INV02.
3. Se **reserva el cupo** en la ruta. `Route` protege su capacidad (INV04) de forma atómica.
4. Si no hay cupo, **no se guarda nada**.
5. Se guarda la entrega con control de versión. Si otra operación la modificó, se **libera el cupo** y se rechaza.
6. Solo después se registra el evento y se solicita el aviso.

### Reglas que forman parte de la decisión

1. La **consulta de disponibilidad es orientativa**: puede cambiar entre la consulta y la reserva, por lo que no sustituye a la reserva.
2. **Ruteo es el único que decide** si hay cupo.
3. **La liberación debe ser segura** si se ejecuta más de una vez.
4. Una falla de Ruteo se trata como "no se pudo verificar", y no se asigna a ciegas.

## 3. Alternativas consideradas

| Alternativa | Resultado | Motivo |
|-------------|-----------|--------|
| **Reserva previa con liberación compensatoria** | **Elegida** | Cada agregado protege su regla y no hay cambios a medias |
| Solo consultar la disponibilidad y asignar | Descartada | No evita superar la capacidad con asignaciones simultáneas |
| Evento posterior: Entregas asigna y Ruteo registra la parada después | Descartada | Si Ruteo no tiene cupo, habría que revertir una asignación ya confirmada y comunicada |
| Transacción única sobre ambos agregados | Descartada | Acopla el almacenamiento de dos módulos y limita su separación futura |

## 4. Justificación

### Razones técnicas

- **Cada regla vive donde corresponde.** La capacidad la protege `Route`, y el estado de la entrega lo protege `Delivery`.
- **Resuelve los escenarios de concurrencia.** EA11 y EA12 quedan cubiertos con pruebas, que comprueban además que no se queda ningún cupo reservado de más.
- **Prepara la evolución.** Si Ruteo se extrae como servicio, la reserva y la liberación ya son operaciones explícitas del contrato, y no una transacción compartida.

### Razones de negocio

- **Evita el retraso que genera una ruta sobrecargada**, que es una de las causas de las entregas fallidas (R03).
- **No comunica una asignación que luego tenga que deshacerse**, lo que evita confundir al repartidor y al coordinador.

## 5. Consecuencias

| Lo que se gana | Lo que se cede |
|----------------|----------------|
| Capacidad protegida bajo concurrencia | Una operación adicional (liberar) que debe ser segura |
| Sin cambios a medias en el caso normal | Una ventana breve en la que hay un cupo reservado sin entrega guardada |
| Módulos desacoplados en su almacenamiento | La consistencia depende de que la liberación se ejecute |

## 6. Riesgos y mitigaciones

| Riesgo | Mitigación |
|--------|------------|
| **Un cupo queda reservado si el proceso se interrumpe entre la reserva y el guardado** | En la Actividad 4, reservas con vencimiento y una conciliación periódica entre paradas y entregas asignadas |
| **La liberación falla** | Que sea repetible sin efecto y que su falla se registre |
| **Mayor complejidad del caso de uso** | Mantenerlo corto y cubrirlo con pruebas de cada rama |
| **Latencia si Ruteo se separa como servicio** | Revisar el diseño con las condiciones de la sección 9 del ADR-001 |

## 7. Cómo se verificará

| Qué se comprueba | Cómo |
|------------------|------|
| Con una sola ruta y un cupo, solo una asignación prevalece | Prueba `SimultaneousAssignments_ForLastSlot_OnlyOneWins` (EA12) |
| Con dos coordinadores sobre la misma entrega, solo uno prevalece y no queda cupo de más | Prueba `SimultaneousAssignments_OfSameDelivery_OnlyOneWins` (EA11) |
| Si no hay cupo, no se guarda la entrega | Prueba `Execute_WhenReservationFails_DoesNotSave` |
| Si el guardado falla, se libera el cupo | Prueba `Execute_WhenSaveConflicts_ReleasesStop` |
| Criterios CE03 y CE05 de la Actividad 2 | `docs/actividad-2/05-criterios-verificables.md` |

## 8. Decisiones pendientes relacionadas

- Vencimiento de las reservas y conciliación (Actividad 4).
- Idempotencia de la liberación con persistencia real.

## 9. Condiciones de revisión

La decisión se revisa si ocurre alguna de estas condiciones:

1. Ruteo se extrae como servicio independiente: la reserva y la liberación pasarían a ser una secuencia de pasos con reintentos.
2. Aparecen cupos reservados que no se liberan con frecuencia.
3. Se adopta un almacenamiento único para ambos módulos que haga viable una transacción compartida.

## 10. Referencias

- `docs/actividad-3/04-invariantes-y-escenarios.md` (EA11 y EA12)
- `docs/actividad-3/05-contextos-y-contratos.md` (contrato C3)
- `docs/actividad-3/06-casos-de-uso-puertos-adaptadores.md` (sección 7)
- `docs/actividad-2/adr/ADR-001-estructura-de-la-plataforma.md`
