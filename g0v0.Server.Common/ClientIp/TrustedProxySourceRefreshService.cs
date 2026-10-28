// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Net;
using g0v0.Server.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace g0v0.Server.Common.ClientIp;

/// <summary>
/// Loads the CDN edge ranges published through <see cref="VerificationConfig.TrustedProxySources"/> at
/// startup and refreshes them on a fixed interval.
/// </summary>
/// <remarks>
/// A failed or empty refresh keeps the previous snapshot for that URL, so a publisher outage can
/// never widen trust (or silently drop the CDN edges the deployment depends on). Catch-all ranges
/// are rejected: a publisher listing <c>0.0.0.0/0</c> would otherwise let every client forge its own
/// address.
/// </remarks>
public sealed partial class TrustedProxySourceRefreshService : BackgroundService
{
    private readonly string[] _invalidSources;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeSpan _interval;
    private readonly ILogger<TrustedProxySourceRefreshService> _logger;
    private readonly Uri[] _sources;
    private readonly TrustedProxyRangeStore _store;
    private readonly TimeSpan _timeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrustedProxySourceRefreshService"/> class.
    /// </summary>
    /// <param name="config">The client IP configuration.</param>
    /// <param name="store">The shared trusted range store.</param>
    /// <param name="httpClientFactory">The factory used to fetch the published ranges.</param>
    /// <param name="logger">The refresh logger.</param>
    public TrustedProxySourceRefreshService(
        VerificationConfig config,
        TrustedProxyRangeStore store,
        IHttpClientFactory httpClientFactory,
        ILogger<TrustedProxySourceRefreshService> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _store = store;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(Math.Max(1, config.TrustedProxyRefreshMinutes));
        _timeout = TimeSpan.FromSeconds(Math.Max(1, config.TrustedProxySourceTimeoutSeconds));

        List<string> invalid = [];
        List<Uri> sources = [];
        foreach (string value in config.TrustedProxySources)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && uri.Scheme is "http" or "https")
            {
                sources.Add(uri);
            }
            else
            {
                invalid.Add(value);
            }
        }

        _sources = [.. sources];
        _invalidSources = [.. invalid];
    }

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (string invalid in _invalidSources)
        {
            Log.InvalidSource(_logger, invalid);
        }

        await RefreshAsync(cancellationToken).ConfigureAwait(false);
        LogConfiguration();
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches every configured source once. Exposed so the host's startup path and tests share the
    /// exact refresh logic.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when every source has been attempted.</returns>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (_sources.Length == 0)
        {
            return;
        }

        using HttpClient client = _httpClientFactory.CreateClient(nameof(TrustedProxySourceRefreshService));
        client.Timeout = _timeout;
        Task<SourceResult>[] pending = new Task<SourceResult>[_sources.Length];
        for (int i = 0; i < _sources.Length; i++)
        {
            pending[i] = FetchAsync(client, _sources[i], cancellationToken);
        }

        SourceResult[] results = await Task.WhenAll(pending).ConfigureAwait(false);

        _store.RetainSources(_sources.Select(source => source.AbsoluteUri));
        foreach (SourceResult result in results)
        {
            if (result.Networks is not { Length: > 0 } networks)
            {
                continue;
            }

            _store.SetSource(result.Source.AbsoluteUri, networks);
            Log.SourceLoaded(_logger, result.Source.AbsoluteUri, networks.Length);
        }

        Log.RangesLoaded(_logger, _store.FetchedNetworkCount, _sources.Length);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_sources.Length == 0)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                await RefreshAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                Log.RefreshFailure(_logger, exception);
            }
        }
    }

    private async Task<SourceResult> FetchAsync(HttpClient client, Uri source, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await client.GetAsync(source, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            IPNetwork[] parsed = ProxyRangeParser.ParsePayload(payload);
            IPNetwork[] networks = parsed.Where(network => network.PrefixLength > 0).ToArray();
            if (networks.Length < parsed.Length)
            {
                Log.CatchAllIgnored(_logger, source.AbsoluteUri);
            }

            if (networks.Length > 0)
            {
                return new SourceResult(Source: source, Networks: networks);
            }

            Log.SourceEmpty(_logger, source.AbsoluteUri);
            return new SourceResult(Source: source, Networks: null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or IOException)
        {
            Log.SourceFailure(_logger, source.AbsoluteUri, exception);
            return new SourceResult(Source: source, Networks: null);
        }
    }

    private void LogConfiguration()
    {
        IReadOnlyList<IPNetwork> staticNetworks = _store.StaticNetworks;
        if (staticNetworks.Count == 0 && _store.FetchedNetworkCount == 0)
        {
            Log.NoTrustedProxies(_logger);
        }

        if (_store.TrustsEveryAddress)
        {
            Log.TrustsEveryAddress(_logger);
        }

        if (staticNetworks.Count > 0)
        {
            Log.StaticRanges(_logger, string.Join(", ", staticNetworks));
        }

        if (_sources.Length > 0)
        {
            Log.Sources(_logger, string.Join(", ", _sources.Select(source => source.AbsoluteUri)), _store.FetchedNetworkCount, (int)_interval.TotalMinutes);
        }
    }

    private readonly record struct SourceResult(Uri Source, IPNetwork[]? Networks);

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Ignoring trusted proxy source that is not an absolute HTTP or HTTPS URL: {Value}")]
        internal static partial void InvalidSource(ILogger logger, string value);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Loaded {Count} trusted proxy range(s) from {Source}.")]
        internal static partial void SourceLoaded(ILogger logger, string source, int count);

        [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Trusted proxy source {Source} returned no ranges; keeping the previous snapshot.")]
        internal static partial void SourceEmpty(ILogger logger, string source);

        [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Trusted proxy source {Source} failed; keeping the previous snapshot.")]
        internal static partial void SourceFailure(ILogger logger, string source, Exception exception);

        [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Ignoring a catch-all range published by {Source}; trusting every address would let clients forge their identity.")]
        internal static partial void CatchAllIgnored(ILogger logger, string source);

        [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Trusted proxy ranges: {Ranges}")]
        internal static partial void StaticRanges(ILogger logger, string ranges);

        [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Trusted proxy sources: {Sources} ({Fetched} fetched range(s), refresh every {Minutes} min).")]
        internal static partial void Sources(ILogger logger, string sources, int fetched, int minutes);

        [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Refreshed {Count} trusted proxy range(s) for {Requested} source(s).")]
        internal static partial void RangesLoaded(ILogger logger, int count, int requested);

        [LoggerMessage(EventId = 9, Level = LogLevel.Warning, Message = "TRUSTED PROXIES is empty: forwarding headers are ignored, so every client behind a proxy shares one identity and trusted devices cannot be told apart.")]
        internal static partial void NoTrustedProxies(ILogger logger);

        [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "The trusted proxy ranges include a catch-all, so clients can forge their own identity.")]
        internal static partial void TrustsEveryAddress(ILogger logger);

        [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "Scheduled trusted proxy refresh failed.")]
        internal static partial void RefreshFailure(ILogger logger, Exception exception);
    }
}