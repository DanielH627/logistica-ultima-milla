# ADR-003: Tres capas con inversión de dependencias, puertos y adaptadores

| Campo | Valor |
|-------|-------|
| **Estado** | Aceptada, con revisión periódica (ver sección 9) |
| **Fecha** | 16 de noviembre de 2026 |
| **Autor** | [Tu nombre completo] |
| **Actividad** | Actividad 3: diseño y dominio |
| **Decisiones relacionadas** | ADR-001 (reglas 2 y 4); ADR-002; ADR-004 |

## 1. Contexto

El ADR-001 decidió que la plataforma es un monolito modular, con módulos por dominio que se comunican por interfaces y se conectan a los sistemas externos mediante adaptadores. Falta decidir **cómo se organiza el código dentro de ese monolito** para que la lógica del negocio pueda cambiar sin depender de una base de datos, un servicio externo o un framework concreto.

### Fuerzas que influyen en la decisión

| Fuerza | Origen |
|--------|--------|
| Proteger las reglas e invariantes del negocio de los detalles técnicos | Actividad 3 |
| Poder probar el negocio sin infraestructura | Escenarios de aceptación |
| Poder reemplazar la persistencia y las integraciones sin tocar el negocio | RNF07 y RNF08 |
| Mantener una estructura que un equipo pequeño pueda sostener | RNF13 |

## 2. Decisión

**Se organiza el código en tres capas, con dependencias dirigidas hacia adentro y comunicación con el exterior mediante puertos y adaptadores.**

| Capa | Contiene | Depende de |
|------|----------|------------|
| **Domain** | Entidades, objetos de valor, invariantes, eventos y errores del negocio | Nada |
| **Application** | Casos de uso, contratos de entrada y salida, y puertos | Domain |
| **Infrastructure** | Adaptadores que implementan los puertos | Application |

### Reglas que forman parte de la decisión

1. **El dominio no tiene dependencias**: ni de proyectos ni de paquetes externos.
2. **La aplicación define los puertos** y expresa lo que necesita en lenguaje del negocio, sin mencionar tecnologías.
3. **La infraestructura implementa los puertos** y es la única capa que conoce tecnologías concretas.
4. **Los casos de uso reciben sus dependencias por constructor** y solo conocen interfaces.
5. **Las reglas del negocio viven en el dominio**, no en el caso de uso ni en la infraestructura.
6. **Los módulos** (Entregas, Ruteo, etc.) son agrupaciones dentro de cada capa, no proyectos separados.
7. **Los adaptadores de esta actividad son en memoria**, y los reales se construyen en la Actividad 4.

## 3. Alternativas consideradas

| Alternativa | Resultado | Motivo |
|-------------|-----------|--------|
| **Tres capas con inversión de dependencias** | **Elegida** | Protege el dominio y permite probarlo sin infraestructura |
| Capas tradicionales (presentación, negocio, datos) | Descartada | El negocio depende de la capa de datos, de modo que cambiar la persistencia obliga a tocar el negocio |
| Un proyecto por cada módulo y capa | Descartada por ahora | Garantizaría los límites entre módulos con el compilador, pero multiplica los proyectos y exige más mantenimiento para un equipo pequeño |
| Un único proyecto sin capas | Descartada | No hay garantía alguna de que el negocio se mantenga separado de los detalles técnicos |

## 4. Justificación

### Razones técnicas

- **El compilador refuerza la regla.** Con un proyecto por capa, una dependencia en sentido contrario no compila.
- **El negocio se prueba solo.** Las pruebas del dominio y de la aplicación no usan base de datos ni servicios externos.
- **La infraestructura es reemplazable.** Cambiar un adaptador en memoria por uno real no modifica el dominio ni el caso de uso.

### Razones de negocio

- **Las reglas pueden cambiar sin riesgo para la persistencia**, y a la inversa, lo que atiende el riesgo R13 (crecimiento del alcance) y el requisito de evolución (RNF07).
- **Menor costo de cambio** cuando se defina la tecnología de almacenamiento en la Actividad 4.

## 5. Consecuencias

| Lo que se gana | Lo que se cede |
|----------------|----------------|
| Negocio protegido y probable de forma aislada | Más indirección: interfaces y adaptadores que de otro modo no existirían |
| Infraestructura intercambiable | Más archivos y proyectos que mantener |
| Dependencias verificadas por el compilador | Traducir datos entre capas, por ejemplo con contratos de aplicación con tipos simples |

## 6. Riesgos y mitigaciones

| Riesgo | Mitigación |
|--------|------------|
| **Los límites entre módulos no los verifica el compilador**, porque los módulos son agrupaciones dentro de cada capa | Revisión de dependencias (criterio CE02) y, en la Actividad 4, pruebas de arquitectura que comprueben que ningún módulo usa el interior de otro |
| **Exceso de interfaces** sin beneficio real | Crear un puerto solo donde exista una frontera real con otro contexto o con infraestructura |
| **Lógica de negocio que se filtra al caso de uso** | Revisar que el caso de uso solo coordine y que las reglas estén en el dominio |

## 7. Cómo se verificará

| Qué se comprueba | Cómo |
|------------------|------|
| Que el dominio no depende de nada | El proyecto de dominio no tiene referencias a otros proyectos ni paquetes |
| Que la aplicación depende solo del dominio | Sus referencias de proyecto |
| Que la infraestructura depende solo de la aplicación | Sus referencias de proyecto |
| Que el negocio se prueba sin infraestructura | Las pruebas del dominio y de la aplicación se ejecutan sin base de datos ni servicios |
| Que los módulos respetan sus límites | Criterio CE02 de `docs/actividad-2/05-criterios-verificables.md` |

## 8. Decisiones pendientes relacionadas

- Si los límites entre módulos deben reforzarse con proyectos separados o con pruebas de arquitectura (Actividad 4).
- El adaptador real de persistencia.

## 9. Condiciones de revisión

La decisión se revisa si ocurre alguna de estas condiciones:

1. Se detectan dependencias indebidas entre módulos con frecuencia.
2. Las interfaces se vuelven una carga sin aportar beneficio.
3. El equipo crece o se separa por módulos, caso en el que convendría un proyecto por módulo.

## 10. Referencias

- `docs/actividad-2/adr/ADR-001-estructura-de-la-plataforma.md`
- `docs/actividad-3/05-contextos-y-contratos.md`
- `docs/actividad-3/06-casos-de-uso-puertos-adaptadores.md`
