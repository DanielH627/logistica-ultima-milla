using LogisticaUltimaMilla.Infrastructure.Outbox;

namespace LogisticaUltimaMilla.Api.Startup;

/// <summary>Ejecuta el procesador de la bandeja de salida de forma periódica mientras la aplicación vive.</summary>
public sealed class OutboxBackgroundService : BackgroundService
{
    private readonly OutboxProcessor _processor;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxBackgroundService> _logger;

    public OutboxBackgroundService(OutboxProcessor processor, OutboxOptions options, ILogger<OutboxBackgroundService> logger)
    {
        _processor = processor;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Procesador de la bandeja de salida desactivado por configuración");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            var handled = 0;

            try
            {
                handled = await _processor.ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Una falla (por ejemplo, la base no responde) no detiene el servicio: se reintenta en el siguiente ciclo.
                _logger.LogError("El procesador de la bandeja de salida falló: {ErrorType}", exception.GetType().Name);
            }

            // Si había trabajo se sigue de inmediato; si no, se espera el intervalo.
            if (handled == 0)
            {
                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
