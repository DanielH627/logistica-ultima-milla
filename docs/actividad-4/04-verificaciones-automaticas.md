# Verificaciones automáticas de arquitectura y calidad

## 1. Propósito

Las decisiones de arquitectura (ADR-001, ADR-003, ADR-004, ADR-005) se vuelven **reglas que la computadora comprueba en cada cambio**. Si alguien las viola, las pruebas fallan y el cambio no pasa la integración continua. Así la arquitectura no depende de que todos se acuerden de ella.

## 2. Pruebas de arquitectura (funciones de aptitud)

Están en `tests/Integration.Tests/Architecture/`. No necesitan base de datos, salvo la de endpoints, que usa una base inalcanzable a propósito.

| Regla | Decisión que protege | Cómo se comprueba |
|-------|----------------------|-------------------|
| El dominio solo usa la biblioteca base de .NET | ADR-003 | Referencias del ensamblado y del `.csproj` |
| La aplicación solo depende del dominio | ADR-003 | Referencias del ensamblado y del `.csproj` |
| La infraestructura no depende de la API ni de ASP.NET Core | ADR-003 | Referencias del ensamblado |
| La API no usa tipos del dominio | ADR-003 | Búsqueda en el código fuente |
| Entregas no conoce a Ruteo, ni Ruteo a Entregas | Contextos delimitados, ADR-004 | Búsqueda de espacios de nombres en el código |
| Las clases del dominio no tienen asignadores públicos | Invariantes INV01 a INV07 | Reflexión sobre las propiedades |
| Los eventos de dominio son registros sellados | Mensajes inmutables (ADR-005) | Reflexión |
| Las excepciones del dominio derivan de `DomainException` | Rechazos como resultados (ADR-005) | Reflexión |
| Dominio y aplicación no leen el reloj del sistema | Puerto `IClock` | Búsqueda en el código |
| El dominio no usa bases de datos, serialización ni frameworks | Dominio puro | Búsqueda de `using` |
| Los casos de uso reciben solo interfaces (puertos) | Arquitectura hexagonal | Reflexión sobre constructores |
| Nada de SQL por interpolación de cadenas | Seguridad (inyección SQL) | Expresión regular en `src` |
| Sin credenciales escritas en el código ni en la configuración base | Seguridad | Búsqueda en `src` y `appsettings.json` |
| **Todo endpoint fuera de `/health` rechaza solicitudes sin clave** | Seguridad | Se recorren los endpoints registrados y se invocan sin clave |
| El código fuente se encuentra y no está vacío | Evita reglas que pasan sin revisar nada | Conteo mínimo de archivos |

## 3. Integración continua (`.github/workflows/ci.yml`)

En cada `push` y cada solicitud de cambios hacia `main`:

1. Restaura paquetes.
2. Compila en `Release` con **advertencias tratadas como errores** (`CI=true` activa esa regla en `Directory.Build.props`).
3. Busca **paquetes con vulnerabilidades conocidas**, también en dependencias transitivas (`dotnet list package --vulnerable --include-transitive`), y falla si hay alguno. Además, la auditoría de NuGet convierte las alertas NU1901 a NU1904 en errores.
4. Ejecuta **todas las pruebas** contra un PostgreSQL 17 real en un contenedor de servicio, incluidas las de arquitectura.
5. Guarda los resultados (`.trx`) como artefacto.

`dependabot.yml` propone cada semana actualizaciones de paquetes y de acciones.

## 4. Cómo reproducirlo en local

```
set CI=true
dotnet build --no-incremental
```
Si compila así, la integración continua no debería fallar por advertencias.

## 5. Límites reconocidos

- Las reglas basadas en búsqueda de texto detectan lo común, no todo: un `using` con alias o una interpolación escrita en dos líneas podrían escaparse. Se complementan con revisión de código.
- Las versiones de varios paquetes están como `*` (flotantes): cada restauración puede traer versiones distintas. Queda como riesgo residual; se corrige fijando versiones o usando archivos de bloqueo.
- No se verifica formato de código ni cobertura de pruebas. Es una decisión consciente para no frenar el avance; se puede agregar después.
- La clave de API sigue siendo un secreto compartido sin rotación automática.
