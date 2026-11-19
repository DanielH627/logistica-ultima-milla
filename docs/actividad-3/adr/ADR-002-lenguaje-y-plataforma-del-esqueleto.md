# ADR-002: Lenguaje y plataforma del esqueleto de diseño

| Campo | Valor |
|-------|-------|
| **Estado** | Aceptada, con revisión periódica (ver sección 9) |
| **Fecha** | 16 de noviembre de 2026 |
| **Autor** | [Tu nombre completo] |
| **Actividad** | Actividad 3: diseño y dominio |
| **Decisiones relacionadas** | D11 de la Actividad 1 (no elegir tecnologías); ADR-001; ADR-003 |

## 1. Contexto

La Actividad 3 exige mostrar el diseño del dominio con código: entidades, casos de uso, puertos, adaptadores y pruebas. En la Actividad 1 se decidió no elegir tecnologías (D11), y en la Actividad 2 se decidió únicamente la estructura (ADR-001). Para demostrar que la separación entre dominio, aplicación e infraestructura es real, y no solo un dibujo, se necesita un lenguaje concreto.

El enunciado de la actividad presenta sus ejemplos en C#, y el curso trabaja sobre ese ecosistema.

### Fuerzas que influyen en la decisión

| Fuerza | Origen |
|--------|--------|
| Que el compilador verifique la dirección de las dependencias entre capas | ADR-003 |
| Que las reglas del negocio se puedan probar sin base de datos ni servicios | `04-invariantes-y-escenarios.md` |
| Alineación con el curso y con los ejemplos del enunciado | Actividad 3 |
| Soporte vigente de la plataforma durante el proyecto | Mantenibilidad (RNF07) |
| No comprometer las decisiones de la Actividad 4 (persistencia, frameworks, proveedores) | D11 |

## 2. Decisión

**Se adopta C# sobre .NET 10 (versión de soporte a largo plazo) para el esqueleto de diseño, con xUnit como marco de pruebas.**

La versión de la plataforma se define en un único lugar (`Directory.Build.props`), de modo que un cambio posterior no exige tocar cada proyecto.

### Alcance de la decisión

| Incluye | No incluye |
|---------|------------|
| Lenguaje y plataforma del esqueleto | Base de datos o almacenamiento |
| Marco de pruebas | Frameworks web o de interfaz |
| | Mensajería, notificaciones o proveedores externos |

Las decisiones sobre persistencia, interfaz e integraciones reales corresponden a la Actividad 4 y requerirán su propio registro.

## 3. Alternativas consideradas

| Alternativa | Resultado | Motivo |
|-------------|-----------|--------|
| **C# sobre .NET 10 (LTS)** | **Elegida** | Coincide con el curso y con el enunciado, y cuenta con soporte vigente |
| C# sobre .NET 8 o .NET 9 | Descartada | Su soporte terminó el 10 de noviembre de 2026, por lo que iniciar el proyecto sobre ellas implicaría una migración inmediata |
| Otro lenguaje (por ejemplo Java o TypeScript) | Descartada | No es el del curso y habría que traducir todos los ejemplos del enunciado |
| Pseudocódigo, sin lenguaje | Descartada | No permite comprobar que el código compila, que las pruebas pasan ni que las dependencias apuntan hacia adentro |

## 4. Justificación

### Razones técnicas

- **Verificación por compilador.** Con un proyecto por capa, una dependencia en sentido contrario no compila, lo que convierte una regla de diseño en una garantía.
- **Tipado estático.** Las interfaces que expresan los puertos y los tipos que representan los objetos de valor hacen explícitas las reglas del dominio.
- **Pruebas sencillas.** El marco de pruebas permite comprobar los escenarios de aceptación sin infraestructura.

### Razones del contexto

- **Coherencia con el curso.** Los ejemplos del enunciado (`AssignCourierUseCase`, `IDeliveryRepository`) se pueden aplicar directamente.
- **Soporte vigente.** .NET 10 es de soporte a largo plazo hasta noviembre de 2028, mientras que las versiones anteriores ya no reciben actualizaciones.

## 5. Consecuencias

| Lo que se gana | Lo que se cede |
|----------------|----------------|
| Una demostración verificable de la separación de capas | Quedar ligado, por ahora, al ecosistema de .NET |
| Pruebas ejecutables de cada escenario | Quien quiera ejecutar el proyecto debe instalar el SDK |
| Soporte vigente de la plataforma | Una decisión adicional de actualización cuando termine el soporte |

El código del dominio es pequeño y no depende de bibliotecas externas, por lo que, si fuera necesario cambiar de lenguaje, la parte que habría que reescribir es acotada.

## 6. Riesgos y mitigaciones

| Riesgo | Mitigación |
|--------|------------|
| **Paquetes de prueba con versión flotante**: una actualización podría alterar el resultado de las pruebas | Fijar las versiones una vez verificado que todo compila y pasa |
| **El SDK instalado no es el esperado** | La versión se cambia en un solo archivo |
| **Fin de soporte de la plataforma** | Revisión programada (sección 9) |

## 7. Cómo se verificará

| Qué se comprueba | Cómo |
|------------------|------|
| Que el proyecto compila | `dotnet build` sin errores |
| Que las reglas se cumplen | `dotnet test` con todas las pruebas superadas |
| Que la versión está centralizada | Un único valor en `Directory.Build.props` |

## 8. Decisiones pendientes relacionadas

- Base de datos, interfaz e integraciones reales: se deciden en la Actividad 4.
- Fijación de las versiones de los paquetes de prueba.

## 9. Condiciones de revisión

La decisión se revisa si ocurre alguna de estas condiciones:

1. Se acerca el fin del soporte de .NET 10 (noviembre de 2028).
2. El curso o el equipo cambian de lenguaje o de plataforma.
3. La Actividad 4 exige una plataforma distinta.

## 10. Referencias

- `docs/actividad-1/06-supuestos-y-decisiones.md` (D11)
- `docs/actividad-2/adr/ADR-001-estructura-de-la-plataforma.md`
- `docs/actividad-3/06-casos-de-uso-puertos-adaptadores.md`
