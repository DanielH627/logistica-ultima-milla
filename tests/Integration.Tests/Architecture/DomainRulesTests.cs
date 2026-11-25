using System.Reflection;
using System.Runtime.CompilerServices;
using LogisticaUltimaMilla.Application.Entregas;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Architecture;

/// <summary>Reglas de diseño del dominio y de la aplicación (ADR-001, ADR-003, documento 05).</summary>
public class DomainRulesTests
{
    private static readonly Assembly Domain = typeof(Delivery).Assembly;
    private static readonly Assembly Application = typeof(AssignCourierUseCase).Assembly;

    private static bool IsRecord(Type type) => type.GetMethod("<Clone>$") is not null;

    [Fact]
    public void Entregas_DoesNotDependOnRuteo()
    {
        var offending = SourceTree.CodeIn("src", "Domain", "Entregas")
            .Where(source => source.Code.Contains("Domain.Ruteo", StringComparison.Ordinal))
            .Select(source => SourceTree.RelativeName(source.File))
            .ToList();

        Assert.True(offending.Count == 0,
            $"Entregas no puede conocer a Ruteo (se comunican por puertos): {string.Join(", ", offending)}.");
    }

    [Fact]
    public void Ruteo_DoesNotDependOnEntregas()
    {
        var offending = SourceTree.CodeIn("src", "Domain", "Ruteo")
            .Where(source => source.Code.Contains("Domain.Entregas", StringComparison.Ordinal))
            .Select(source => SourceTree.RelativeName(source.File))
            .ToList();

        Assert.True(offending.Count == 0,
            $"Ruteo no puede conocer a Entregas: {string.Join(", ", offending)}.");
    }

    [Fact]
    public void DomainClasses_HaveNoPublicSetters()
    {
        // El estado solo cambia mediante métodos que protegen las invariantes, nunca asignando propiedades.
        var offending = Domain.GetTypes()
            .Where(type => type.IsClass && type.IsPublic && !typeof(Exception).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(property => property.SetMethod is { IsPublic: true }
                && !property.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit)))
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToList();

        Assert.True(offending.Count == 0,
            $"Propiedades del dominio con asignador público: {string.Join(", ", offending)}.");
    }

    [Fact]
    public void DomainEvents_AreSealedRecords()
    {
        var events = Domain.GetTypes()
            .Where(type => typeof(IDomainEvent).IsAssignableFrom(type) && type.IsClass)
            .ToList();

        Assert.NotEmpty(events);

        var offending = events.Where(type => !type.IsSealed || !IsRecord(type)).Select(type => type.Name).ToList();
        Assert.True(offending.Count == 0,
            $"Los eventos de dominio deben ser registros sellados (inmutables): {string.Join(", ", offending)}.");
    }

    [Fact]
    public void DomainExceptions_DeriveFromDomainException()
    {
        var offending = Domain.GetTypes()
            .Where(type => type.IsClass && typeof(Exception).IsAssignableFrom(type) && type != typeof(DomainException))
            .Where(type => !typeof(DomainException).IsAssignableFrom(type))
            .Select(type => type.Name)
            .ToList();

        Assert.True(offending.Count == 0,
            $"Las excepciones del dominio deben derivar de DomainException: {string.Join(", ", offending)}.");
    }

    [Fact]
    public void Domain_DoesNotUseFrameworkOrDataAccessNamespaces()
    {
        string[] forbidden = ["using Npgsql", "using Microsoft.", "using System.Text.Json", "using System.Data", "using System.Net"];

        var offending = SourceTree.CodeIn("src", "Domain")
            .Where(source => forbidden.Any(text => source.Code.Contains(text, StringComparison.Ordinal)))
            .Select(source => SourceTree.RelativeName(source.File))
            .ToList();

        Assert.True(offending.Count == 0,
            $"El dominio no usa bases de datos, serialización ni frameworks: {string.Join(", ", offending)}.");
    }

    [Fact]
    public void DomainAndApplication_NeverReadTheSystemClockDirectly()
    {
        // La hora entra por el puerto IClock; así el comportamiento es repetible en las pruebas.
        string[] forbidden = ["DateTime.Now", "DateTime.UtcNow", "DateTimeOffset.Now", "DateTimeOffset.UtcNow", "TimeProvider.System"];

        var offending = SourceTree.CodeIn("src", "Domain")
            .Concat(SourceTree.CodeIn("src", "Application"))
            .Where(source => forbidden.Any(text => source.Code.Contains(text, StringComparison.Ordinal)))
            .Select(source => SourceTree.RelativeName(source.File))
            .ToList();

        Assert.True(offending.Count == 0,
            $"Use IClock en lugar del reloj del sistema: {string.Join(", ", offending)}.");
    }

    [Fact]
    public void UseCases_DependOnlyOnInterfaces()
    {
        var useCases = Application.GetTypes()
            .Where(type => type.IsClass && type.IsPublic && type.Name.EndsWith("UseCase", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(useCases);

        var offending = useCases
            .SelectMany(type => type.GetConstructors().SelectMany(constructor => constructor.GetParameters()
                .Where(parameter => !parameter.ParameterType.IsInterface)
                .Select(parameter => $"{type.Name}({parameter.ParameterType.Name})")))
            .ToList();

        Assert.True(offending.Count == 0,
            $"Los casos de uso reciben puertos (interfaces), no implementaciones: {string.Join(", ", offending)}.");
    }
}
