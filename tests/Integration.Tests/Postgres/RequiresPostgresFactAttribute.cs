using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Postgres;

/// <summary>
/// Marca una prueba que necesita PostgreSQL. Si no hay una base disponible (la variable de entorno
/// no está definida), la prueba se omite con un aviso en lugar de fallar.
/// </summary>
public sealed class RequiresPostgresFactAttribute : FactAttribute
{
    public RequiresPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(TestDatabase.ConnectionStringFromEnvironment()))
        {
            Skip = $"Defina {TestDatabase.EnvironmentVariable} para ejecutar las pruebas con PostgreSQL.";
        }
    }
}
