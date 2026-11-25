# ADR-006: Persistencia con PostgreSQL, SQL parametrizado y migraciones versionadas

| Campo | Valor |
|-------|-------|
| **Estado** | Aceptada, con revisión periódica (ver sección 8) |
| **Fecha** | 24 de noviembre de 2026 |
| **Autor** | [Tu nombre completo] |
| **Actividad** | Actividad 4: construcción, validación y defensa final |
| **Decisiones relacionadas** | ADR-001 (datos por módulo); ADR-003 (puertos y adaptadores); ADR-004 (reserva y compensación); plan de implementación D4-02 a D4-04, AJ-04 |

## 1. Contexto

El diseño de la Actividad 3 usaba adaptadores en memoria. Para que el vertical slice sea real hace falta persistencia que proteja dos cosas bajo concurrencia: que una entrega no tenga dos repartidores (INV01) y que una ruta no supere su capacidad (INV04). Además, el esquema debe poder reproducirse en cualquier ambiente y no debe cambiar a escondidas.

### Fuerzas que influyen en la decisión

| Fuerza | Origen |
|--------|--------|
| Dos asignaciones simultáneas no pueden superar el cupo de la ruta | INV04, QS01 |
| Los datos de cada módulo deben quedar separados | ADR-001 |
| El dominio y la aplicación no deben cambiar para conectar la base de datos | ADR-003 |
| El esquema debe ser reproducible y versionado | Enunciado de la Actividad 4 |
| Equipo pequeño: pocas piezas que operar | QS14 |

## 2. Decisión

1. **Base de datos relacional PostgreSQL 17**, ejecutada con contenedores mediante un archivo de composición.
2. **Un esquema por módulo** dentro de la misma base: `entregas`, `ruteo` y `transversal` (bandeja de salida e historial).
3. **Acceso a datos con SQL parametrizado** mediante el controlador Npgsql, sin ORM. Los valores nunca se concatenan dentro del SQL.
4. **Concurrencia en dos niveles:** versión optimista en la entrega, y bloqueo de la fila de la ruta (`FOR UPDATE`) al reservar un cupo.
5. **Migraciones SQL versionadas** con un ejecutor propio: cada script se aplica una sola vez, queda registrado con su suma de verificación SHA-256, un script ya aplicado que cambie hace fallar la ejecución, y un bloqueo consultivo evita que dos instancias migren a la vez.

## 3. Alternativas consideradas

| Alternativa | Resultado | Motivo |
|-------------|-----------|--------|
| **PostgreSQL con SQL parametrizado** | **Elegida** | Bloqueos de fila reales, control explícito del SQL y de la concurrencia, pocas dependencias |
| SQLite | Descartada | Más simple, pero sin bloqueos de fila ni concurrencia comparable a la de un entorno real |
| Almacenamiento de documentos | Descartada | Las invariantes de capacidad se expresan mejor con transacciones y restricciones relacionales |
| ORM completo | Descartada | Oculta el SQL y la concurrencia justo donde más importan y agrega una dependencia grande |
| Herramienta de migraciones externa | Descartada | Para un esquema pequeño, un ejecutor propio de unas 200 líneas mantiene el control sin otra herramienta |
| Una base de datos por módulo | Descartada | Multiplica la operación sin beneficio en un monolito modular (ADR-001) |

## 4. Justificación

- **Las invariantes se protegen donde ocurren.** El bloqueo de fila hace que dos reservas simultáneas de la última parada no puedan ganar ambas.
- **El dominio no se tocó.** Que conectar la base no exigiera cambiar `Delivery` ni `Route` confirma la separación de capas del ADR-003.
- **Los esquemas por módulo hacen visible la pertenencia de cada tabla** y facilitan extraer Ruteo en el futuro.

## 5. Consecuencias

| Lo que se gana | Lo que se cede |
|----------------|----------------|
| Concurrencia comprobable con pruebas reales | Más código SQL escrito a mano |
| Esquema reproducible y a prueba de cambios retroactivos | Un ejecutor de migraciones propio que mantener |
| Sin dependencias de un ORM | Cambiar de motor exige reescribir los adaptadores (no el dominio) |
| Esquemas separados por módulo | Una sola base de datos es un punto único de falla compartido |

## 6. Riesgos y mitigaciones

| Riesgo | Mitigación | Evidencia |
|--------|------------|-----------|
| Sobreasignación de la ruta | Bloqueo de fila y restricción en la base | `ReserveStop_ManyAtTheSameTime_NeverExceedsCapacity` |
| Doble asignación de la entrega | Versión optimista | `SimultaneousAssignments_OfSameDelivery_OnlyOneWins` |
| Migración alterada o aplicada dos veces | Suma de verificación y bloqueo consultivo | `Migrate_WhenAnAppliedScriptWasModified_Throws`, `Migrate_RunningTwiceAtTheSameTime_AppliesEachScriptOnce` |
| Inyección SQL | Solo parámetros | `Sql_IsNeverBuiltWithStringInterpolationInProductionCode` |

## 7. Cómo se verificó

Las pruebas de integración corren contra un PostgreSQL real, con una base temporal por clase de prueba. En integración continua usan un contenedor de servicio.

## 8. Condiciones de revisión

1. El volumen exige separar la base de Ruteo (condición de extracción del ADR-001).
2. Aparece la necesidad de consultas complejas donde un ORM o un generador de consultas ahorre trabajo real.
3. Las migraciones crecen hasta justificar una herramienta externa.

## 9. Referencias

- `src/Infrastructure/Migrations`, `src/Infrastructure/Persistence`, `src/Infrastructure/Routing`
- `docs/actividad-4/01-plan-de-implementacion.md` (modelo de datos)
