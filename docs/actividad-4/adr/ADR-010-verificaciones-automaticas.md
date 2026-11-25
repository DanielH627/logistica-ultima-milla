# ADR-010: Verificaciones automáticas: pruebas de arquitectura e integración continua

| Campo | Valor |
|-------|-------|
| **Estado** | Aceptada, con revisión periódica (ver sección 8) |
| **Fecha** | 24 de noviembre de 2026 |
| **Autor** | [Tu nombre completo] |
| **Actividad** | Actividad 4: construcción, validación y defensa final |
| **Decisiones relacionadas** | ADR-001, ADR-003 (reglas que se verifican); ADR-008 (clave de API); plan de implementación D4-09 a D4-11 |

## 1. Contexto

Las decisiones de arquitectura se pierden si solo viven en documentos: con el tiempo alguien agrega una dependencia indebida, un endpoint sin protección o SQL armado con cadenas. El enunciado pide una comprobación automatizada de calidad o arquitectura. Además, el equipo trabaja sobre un único tronco con cambios pequeños y frecuentes, por lo que cada cambio debe verificarse solo.

## 2. Decisión

1. **Pruebas de arquitectura (funciones de aptitud)** dentro del proyecto de pruebas de integración, sin paquetes adicionales: usan reflexión sobre los ensamblados, lectura de los `.csproj` y búsqueda en el código fuente. Cubren la dirección de las dependencias, la independencia entre Entregas y Ruteo, la pureza del dominio, la inmutabilidad de los eventos, el uso del puerto `IClock`, la prohibición de SQL interpolado y de credenciales en el código, y que **todo endpoint fuera de `/health` exija la clave de API**.
2. **Integración continua** en cada `push` y cada solicitud de cambios a `main`: restaura, compila con **advertencias como errores**, busca paquetes con vulnerabilidades conocidas (incluidas las transitivas) y ejecuta todas las pruebas contra un PostgreSQL real.
3. **Auditoría de NuGet** activa, con las alertas de vulnerabilidad convertidas en errores en integración continua.
4. **Dependabot** semanal para paquetes y acciones.
5. **Un único tronco de trabajo** con cambios pequeños, coherente con la decisión del proyecto de no usar ramas.

## 3. Alternativas consideradas

| Alternativa | Resultado | Motivo |
|-------------|-----------|--------|
| **Pruebas propias sin paquetes adicionales** | **Elegida** | Sin dependencias nuevas; las reglas quedan legibles en C# |
| Biblioteca especializada de pruebas de arquitectura | Descartada por ahora | Más expresiva, pero agrega una dependencia para unas pocas reglas |
| Revisión manual de las dependencias | Descartada | No es repetible ni rápida |
| Verificación local sin integración continua | Descartada | No deja evidencia reproducible |
| Análisis estático de seguridad (SAST) y pruebas dinámicas (DAST) | Aplazada | Complementan, no sustituyen; se listan como próximo paso |

## 4. Justificación

- **Una regla que se verifica no se olvida:** un endpoint nuevo sin clave rompe la compilación.
- **Las pruebas de control evitan reglas vacías:** una prueba comprueba que el código fuente se encuentra y no está vacío.
- **Las advertencias como errores** impiden que la deuda se acumule en silencio; la variable `CI` evita que bloqueen el trabajo local.

## 5. Consecuencias

| Lo que se gana | Lo que se cede |
|----------------|----------------|
| Las decisiones se hacen cumplir en cada cambio | Mantener las reglas cuando el código cambie de forma legítima |
| Evidencia reproducible de calidad | Las reglas basadas en texto no detectan todo caso posible |
| Detección temprana de dependencias vulnerables | Versiones flotantes de varios paquetes hacen la restauración menos predecible |

## 6. Riesgos y mitigaciones

| Riesgo | Mitigación | Evidencia |
|--------|------------|-----------|
| Erosión de las capas | Reglas de dependencia | `Domain_ReferencesOnlyTheBaseLibrary`, `Application_ReferencesOnlyTheDomainAndTheBaseLibrary`, `Infrastructure_DoesNotReferenceTheApiOrAspNetCore` |
| Contextos acoplados | Regla entre Entregas y Ruteo | `Entregas_DoesNotDependOnRuteo`, `Ruteo_DoesNotDependOnEntregas` |
| Endpoint sin protección | Recorrido automático de endpoints | `EveryEndpointOutsideHealth_RejectsRequestsWithoutAnApiKey` |
| Reglas que pasan sin revisar nada | Prueba de control | `SourceTree_IsFoundAndNotEmpty` |
| Paquetes vulnerables | Auditoría y búsqueda en CI | Flujo `.github/workflows/ci.yml` |

## 7. Condiciones de revisión

1. Las reglas basadas en texto producen falsos positivos o negativos repetidos: se evalúa una biblioteca especializada.
2. Se incorporan más equipos o ramas: se agregan protecciones de rama y revisiones obligatorias.
3. Se quiere medir cobertura, formato o complejidad: se agregan al flujo de CI.

## 8. Referencias

- `tests/Integration.Tests/Architecture`, `.github/workflows/ci.yml`, `Directory.Build.props`
- `docs/actividad-4/04-verificaciones-automaticas.md`
