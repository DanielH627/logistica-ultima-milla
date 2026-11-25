using System.Text.RegularExpressions;

namespace LogisticaUltimaMilla.Integration.Tests.Architecture;

/// <summary>
/// Acceso al código fuente de la solución para las pruebas de arquitectura.
/// Busca la raíz subiendo desde la carpeta de las pruebas hasta encontrar "src" y "tests".
/// </summary>
internal static class SourceTree
{
    private static readonly Lazy<string> RootDirectory = new(FindRoot);

    public static string Root => RootDirectory.Value;

    public static string Path(params string[] parts) => System.IO.Path.Combine([Root, .. parts]);

    /// <summary>Archivos .cs de una carpeta, sin bin ni obj.</summary>
    public static IReadOnlyList<string> CSharpFiles(params string[] relativeDirectory) =>
        Directory.EnumerateFiles(Path(relativeDirectory), "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(file))
            .ToList();

    /// <summary>Contenido de un archivo sin las líneas de comentario (//, ///), para no confundir una mención con un uso.</summary>
    public static string CodeOf(string file) =>
        string.Join('\n', File.ReadLines(file).Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal)));

    public static IEnumerable<(string File, string Code)> CodeIn(params string[] relativeDirectory) =>
        CSharpFiles(relativeDirectory).Select(file => (file, CodeOf(file)));

    public static string RelativeName(string file) => System.IO.Path.GetRelativePath(Root, file);

    private static bool IsBuildOutput(string file)
    {
        var normalized = file.Replace('\\', '/');
        return normalized.Contains("/bin/") || normalized.Contains("/obj/");
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(System.IO.Path.Combine(directory.FullName, "src"))
                && Directory.Exists(System.IO.Path.Combine(directory.FullName, "tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("No se encontró la raíz de la solución (carpetas src y tests).");
    }

    /// <summary>Referencias de un .csproj: (ProjectReference, PackageReference).</summary>
    public static (IReadOnlyList<string> Projects, IReadOnlyList<string> Packages) ReferencesOf(string csprojRelativePath)
    {
        var xml = File.ReadAllText(Path(csprojRelativePath.Split('/')));

        var projects = Regex.Matches(xml, "<ProjectReference\\s+Include=\"([^\"]+)\"")
            .Select(match => match.Groups[1].Value)
            .ToList();
        var packages = Regex.Matches(xml, "<PackageReference\\s+Include=\"([^\"]+)\"")
            .Select(match => match.Groups[1].Value)
            .ToList();

        return (projects, packages);
    }
}
