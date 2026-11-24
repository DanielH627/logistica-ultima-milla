namespace LogisticaUltimaMilla.Api.Security;

/// <summary>
/// Configuración de seguridad. La clave de API nunca se escribe en el código ni en el repositorio:
/// en un entorno real se entrega por variable de entorno (Security__ApiKey).
/// </summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public string ApiKey { get; set; } = string.Empty;
}
