using System.Text;
using Microsoft.Extensions.Logging;

namespace LogisticaUltimaMilla.Integration.Tests.Api;

public sealed record CapturedLog(string Category, LogLevel Level, string Message, string Scopes);

/// <summary>
/// Proveedor de logs para pruebas: guarda cada registro con su mensaje, su excepción completa y sus ámbitos.
/// Si algo sensible llegara a cualquiera de esos tres lugares, las pruebas lo detectan.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly List<CapturedLog> _entries = new();
    private readonly object _gate = new();
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

    public IReadOnlyList<CapturedLog> Entries
    {
        get { lock (_gate) { return _entries.ToList(); } }
    }

    /// <summary>Todo el texto registrado en una sola cadena, para buscar valores que no deben aparecer.</summary>
    public string AllText() =>
        string.Join('\n', Entries.Select(e => $"{e.Category} {e.Message} {e.Scopes}"));

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    public void Dispose()
    {
    }

    private void Add(string category, LogLevel level, string message)
    {
        var scopes = new StringBuilder();
        _scopes.ForEachScope((scope, builder) =>
        {
            if (scope is IEnumerable<KeyValuePair<string, object>> pairs)
            {
                foreach (var pair in pairs)
                {
                    builder.Append(pair.Key).Append('=').Append(pair.Value).Append(' ');
                }
            }
            else
            {
                builder.Append(scope).Append(' ');
            }
        }, scopes);

        lock (_gate)
        {
            _entries.Add(new CapturedLog(category, level, message, scopes.ToString()));
        }
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly CapturingLoggerProvider _provider;
        private readonly string _category;

        public CapturingLogger(CapturingLoggerProvider provider, string category)
        {
            _provider = provider;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            _provider._scopes.Push(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (exception is not null)
            {
                message += "\n" + exception;
            }

            _provider.Add(_category, logLevel, message);
        }
    }
}
