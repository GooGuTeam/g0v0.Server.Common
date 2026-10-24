// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace g0v0.Server.Common.GeoIP;

/// <summary>Checks GeoIP databases at startup and periodically without failing the host on update errors.</summary>
/// <param name="geoIP">The shared lookup service.</param>
/// <param name="config">The update configuration.</param>
/// <param name="timeProvider">The scheduling clock.</param>
/// <param name="logger">The update logger.</param>
public sealed partial class GeoIPUpdateService(IGeoIPService geoIP, GeoIPConfig config, TimeProvider timeProvider, ILogger<GeoIPUpdateService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await geoIP.UpdateAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logUpdateFailure(logger, exception);
            }

            await Task.Delay(config.UpdateInterval, timeProvider, stoppingToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "GeoIP database update failed; existing databases remain available.")]
    private static partial void logUpdateFailure(ILogger logger, Exception exception);
}