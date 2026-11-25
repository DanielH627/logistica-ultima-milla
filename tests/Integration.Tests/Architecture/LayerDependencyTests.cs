using System.Reflection;
using LogisticaUltimaMilla.Application.Entregas;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Infrastructure.Persistence;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Architecture;

/// <summary>
/// Funciones de aptitud (fitness functions): reglas de arquitectura que se verifican en cada ejecución.
/// Si alguien viola la dirección de las dependencias (ADR-003), la compilación de CI falla.
/// Regla: Domain ← Application ← Infrastructure ← Api. Las flechas apuntan hacia adentro.
/// </summary>
public class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(Delivery).Assembly;
    private static readonly Assembly Application = typeof(AssignCourierUseCase).Assembly;
    private static readonly Assembly Infrastructure = typeof(PostgresDeliveryRepository).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    private static IEnumerable<string> ReferencedNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(name => name.Name ?? "");

    private static bool IsBaseLibrary(string name) =>
        name.StartsWith("System", StringComparison.Ordinal)
        || name is "netstandard" or "mscorlib";

    [Fact]
    public void SourceTree_IsFoundAndNotEmpty()
    {
        // Control: evita que las reglas basadas en el código fuente "pasen" porque no encontraron nada.
        Assert.True(SourceTree.CSharpFiles("src", "Domain").Count >= 5);
        Assert.True(SourceTree.CSharpFiles("src", "Application").Count >= 3);
        Assert.True(SourceTree.CSharpFiles("src", "Infrastructure").Count >= 5);
        Assert.True(SourceTree.CSharpFiles("src", "Api").Count >= 3);
    }

    [Fact]
    public void Domain_ReferencesOnlyTheBaseLibrary()
    {
        var offending = ReferencedNames(Domain).Where(name => !IsBaseLibrary(name)).ToList();

        Assert.True(offending.Count == 0,
            $"El dominio no puede depender de: {string.Join(", ", offending)}.");
    }

    [Fact]
    public void Application_ReferencesOnlyTheDomainAndTheBaseLibrary()
    {
        var offending = ReferencedNames(Application)
            .Where(name => !IsBaseLibrary(name) && name != Domain.GetName().Name)
            .ToList();

        Assert.True(offending.Count == 0,
            $"La aplicación solo puede depender del dominio. Dependencias no permitidas: {string.Join(", ", offending)}.");
    }

    [Fact]
    public void Infrastructure_DoesNotReferenceTheApiOrAspNetCore()
    {
        var offending = ReferencedNames(Infrastructure)
            .Where(name => name == Api.GetName().Name || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
            .ToList();

        Assert.True(offending.Count == 0,
            $"La infraestructura no puede depender de la entrada HTTP: {string.Join(", ", offending)}.");
    }

    [Fact]
    public void ProjectFiles_DeclareOnlyTheAllowedReferences()
    {
        var domain = SourceTree.ReferencesOf("src/Domain/LogisticaUltimaMilla.Domain.csproj");
        Assert.Empty(domain.Projects);
        Assert.Empty(domain.Packages);

        var application = SourceTree.ReferencesOf("src/Application/LogisticaUltimaMilla.Application.csproj");
        Assert.Single(application.Projects, reference => reference.Contains("Domain", StringComparison.Ordinal));
        Assert.Empty(application.Packages);

        var infrastructure = SourceTree.ReferencesOf("src/Infrastructure/LogisticaUltimaMilla.Infrastructure.csproj");
        Assert.DoesNotContain(infrastructure.Projects, reference => reference.Contains("Api", StringComparison.Ordinal));
    }

    [Fact]
    public void Api_DoesNotUseDomainTypesDirectly()
    {
        // La entrada HTTP habla con los comandos y resultados de la aplicación, no con el dominio.
        var offending = SourceTree.CodeIn("src", "Api")
            .Where(source => source.Code.Contains("LogisticaUltimaMilla.Domain", StringComparison.Ordinal))
            .Select(source => SourceTree.RelativeName(source.File))
            .ToList();

        Assert.True(offending.Count == 0,
            $"La API no debe usar el dominio directamente: {string.Join(", ", offending)}.");
    }
}
