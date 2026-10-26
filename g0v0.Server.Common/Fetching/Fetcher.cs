// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Caching;
using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using g0v0.Server.Common.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using Beatmap = g0v0.Server.Common.Database.Models.Beatmap;

namespace g0v0.Server.Common.Fetching;

/// <summary>
/// Fetches osu! data from the osu! API and other sites.
/// </summary>
/// <param name="httpService">The shared HTTP service.</param>
/// <param name="configManager">The configuration manager.</param>
/// <param name="cache">The cache adapter.</param>
/// <param name="beatmapSetRepository">The beatmap set repository.</param>
/// <param name="beatmapRepository">The beatmap repository, used to reuse a difficulty that is already stored.</param>
/// <param name="logger">The logger.</param>
public partial class Fetcher(
    IHttpService httpService,
    ConfigManager configManager,
    IStringCache cache,
    IBeatmapSetRepository beatmapSetRepository,
    IBeatmapRepository beatmapRepository,
    ILogger<Fetcher> logger) : IFetcher, IBeatmapSearchSource, IDisposable
{
    private const string OAuthEndpoint = "https://osu.ppy.sh/oauth/token";
    private const string BeatmapLookupEndpoint = "https://osu.ppy.sh/api/v2/beatmaps/lookup";

    private static readonly string[] RawMirrorEndpoints =
    [
        "https://osu.direct/api/osu/{0}",
        "https://catboy.best/osu/{0}",
    ];

    private readonly GeneralConfig _config = configManager.Get<GeneralConfig>();
    private readonly TimeSpan _rawCacheExpiry = TimeSpan.FromHours(configManager.Get<GeneralConfig>().FetcherBeatmapRawCacheExpireHours);
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private readonly Dictionary<string, string?> _tokenCache = new(StringComparer.Ordinal);
    private bool _disposed;

    private string? _accessToken;
    private long _accessTokenExpiryUnixSeconds;

    /// <inheritdoc/>
    public async Task<Beatmap> FetchBeatmapAsync(int beatmapId, CancellationToken cancellationToken = default)
    {
        return beatmapId <= 0
            ? throw new ArgumentOutOfRangeException(paramName: nameof(beatmapId), message: "Beatmap ID must be greater than zero.")
            : await FetchBeatmapCoreAsync(beatmapId: beatmapId, checksum: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Beatmap> FetchBeatmapAsync(string checksum, CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(checksum)
            ? throw new ArgumentException("Checksum must not be blank.", nameof(checksum))
            : await FetchBeatmapCoreAsync(beatmapId: null, checksum: checksum, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Beatmap?> FetchBeatmapByFilenameAsync(string filename, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return null;
        }

        // A difficulty that is already stored is answered without an API round trip; the stable
        // client asks for every local map on login, so this path is the hot one.
        IReadOnlyList<Beatmap> stored = await beatmapRepository.GetByFilenameAsync(filename).ConfigureAwait(false);
        if (stored.Count > 0)
        {
            return stored[0];
        }

        // The osu! API matches the filename exactly as the client stores it, ".osu" included.
        string name = filename.Trim();
        Beatmap? beatmap = await TryFetchByFilenameAsync(name, cancellationToken).ConfigureAwait(false);
        if (beatmap is null && !name.EndsWith(".osu", StringComparison.OrdinalIgnoreCase))
        {
            // Tolerate callers that pass the bare difficulty name.
            beatmap = await TryFetchByFilenameAsync(name + ".osu", cancellationToken).ConfigureAwait(false);
        }

        return beatmap is null ? null : await persistAsync(beatmap, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<BeatmapSet> FetchBeatmapSetAsync(int beatmapSetId, CancellationToken cancellationToken = default)
    {
        if (beatmapSetId <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName: nameof(beatmapSetId), message: "Beatmap set ID must be greater than zero.");
        }

        OsuBeatmapSet setDto = await GetJsonAsync<OsuBeatmapSet>(url: BeatmapSetEndpoint(beatmapSetId), cancellationToken).ConfigureAwait(false);
        return await PersistSetAsync(setDto, lookup: null).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string> FetchBeatmapRawAsync(int beatmapId, CancellationToken cancellationToken = default)
    {
        if (beatmapId <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName: nameof(beatmapId), message: "Beatmap ID must be greater than zero.");
        }

        string cacheKey = $"beatmap:{beatmapId}:raw";
        string? cached = await cache.GetAsync(cacheKey).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(cached))
        {
            await cache.RefreshAsync(cacheKey, _rawCacheExpiry).ConfigureAwait(false);
            return cached;
        }

        List<string> sources = [RawBeatmapEndpoint(beatmapId)];
        sources.AddRange(RawMirrorEndpoints.Select(endpoint => string.Format(endpoint, beatmapId)));

        foreach (string source in sources)
        {
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Get, source);
                using HttpResponseMessage response = await httpService.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Raw beatmap {BeatmapId} source {Source} returned HTTP {Status}.", beatmapId, source, (int)response.StatusCode);
                    continue;
                }

                string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(content))
                {
                    logger.LogWarning("Raw beatmap {BeatmapId} source {Source} returned an empty body.", beatmapId, source);
                    continue;
                }

                await cache.SetAsync(cacheKey, content, _rawCacheExpiry).ConfigureAwait(false);
                return content;
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Raw beatmap {BeatmapId} source {Source} failed.", beatmapId, source);
            }
        }

        throw new FetcherException($"Failed to fetch raw beatmap {beatmapId} from all sources.");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private static string BeatmapSetEndpoint(int beatmapSetId) => $"https://osu.ppy.sh/api/v2/beatmapsets/{beatmapSetId}";

    private static string RawBeatmapEndpoint(int beatmapId) => $"https://osu.ppy.sh/osu/{beatmapId}";

    private static BeatmapSet MapSet(OsuBeatmapSet dto)
    {
        return new BeatmapSet
        {
            Id = dto.Id,
            Status = ParseStatus(dto.Status),
            Artist = dto.Artist ?? string.Empty,
            ArtistUnicode = dto.ArtistUnicode ?? string.Empty,
            Title = dto.Title ?? string.Empty,
            TitleUnicode = dto.TitleUnicode ?? string.Empty,
            Creator = dto.Creator ?? string.Empty,
            CreatorId = dto.CreatorId,
            PreviewUrl = dto.PreviewUrl ?? string.Empty,
            Source = dto.Source ?? string.Empty,
            Tags = dto.Tags ?? string.Empty,
            Bpm = dto.Bpm,
            HasExplicitContent = dto.HasExplicitContent,
            Spotlight = dto.Spotlight,
            HasVideo = dto.HasVideo,
            HasStoryboard = dto.HasStoryboard,
            FeatureArtistTrackId = dto.TrackId,
            SubmittedDate = dto.SubmittedDate ?? DateTimeOffset.MinValue,
            RankedDate = dto.RankedDate,
            LastUpdatedDate = dto.LastUpdated,
            Covers = BeatmapCovers.ToGameCovers(dto.Covers),
            Genre = dto.Genre ?? default,
            Language = dto.Language ?? default,
            CurrentNominations = dto.CurrentNominations,
            Description = dto.Description == null ? null : new BeatmapDescription
            {
                Bbcode = dto.Description.Bbcode,
                Description = dto.Description.Description,
            },
        };
    }

    private static Beatmap MapBeatmap(OsuBeatmap dto, int beatmapSetId, OsuBeatmapSetCovers? setCovers)
    {
        return new Beatmap
        {
            // The API publishes covers per set; every difficulty inherits them.
            Covers = BeatmapCovers.FromBeatmapSet(setCovers),
            Id = dto.Id,
            BeatmapSetId = beatmapSetId,
            Url = dto.Url ?? string.Empty,
            Checksum = dto.Checksum,
            Version = dto.Version ?? string.Empty,
            Mode = dto.ModeInt,
            TotalLength = dto.TotalLength,
            HitLength = dto.HitLength,
            DifficultyRating = dto.DifficultyRating,
            ApproachRate = dto.ApproachRate,
            CircleSize = dto.CircleSize,
            HpDrainRate = dto.DrainRate,
            OverallDifficulty = dto.OverallDifficulty,
            Bpm = dto.Bpm,
            CirclesCount = dto.CircleCount,
            SlidersCount = dto.SliderCount,
            SpinnersCount = dto.SpinnerCount,
            MaxCombo = dto.MaxCombo,
            MapperId = dto.UserId,
            Status = ParseStatus(dto.Status),
            DeletedAt = dto.DeletedAt,
            LastUpdate = dto.LastUpdated,
        };
    }

    private static BeatmapOnlineStatus ParseStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status) || !Enum.TryParse(status, ignoreCase: true, out BeatmapOnlineStatus parsed)
            ? throw new FetcherException($"Unsupported beatmap status '{status ?? "unknown"}'.")
            : parsed;
    }

    private async Task<Beatmap?> TryFetchByFilenameAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            return await FetchBeatmapByQueryAsync($"filename={Uri.EscapeDataString(name)}", expectedId: null, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpServiceException exception) when (exception.StatusCode == 404)
        {
            return null;
        }
    }

    /// <summary>
    /// Re-reads a fetched beatmap from the database, inserting it when the fetch did not persist it.
    /// </summary>
    /// <param name="beatmap">The freshly fetched beatmap.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stored beatmap.</returns>
    /// <remarks>
    /// The lookup endpoint persists the whole beatmap set, so the insert only runs when the set
    /// on the API side did not include the requested difficulty - without it the caller would get a
    /// detached entity whose ID every later foreign key rejects.
    /// </remarks>
    private async Task<Beatmap> persistAsync(Beatmap beatmap, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Beatmap? stored = await beatmapRepository.GetByIdAsync(beatmap.Id).ConfigureAwait(false);
        if (stored is not null)
        {
            return stored;
        }

        logger.LogWarning(
            "Fetched beatmap {BeatmapId} ({Version}) was not part of the persisted set; inserting it.",
            beatmap.Id,
            beatmap.Version);
        await beatmapRepository.CreateAsync(beatmap).ConfigureAwait(false);
        return beatmap;
    }

    private async Task<Beatmap> FetchBeatmapCoreAsync(int? beatmapId, string? checksum, CancellationToken cancellationToken)
    {
        string query = beatmapId.HasValue ? $"id={beatmapId}" : $"checksum={Uri.EscapeDataString(checksum!)}";
        return await FetchBeatmapByQueryAsync(query, beatmapId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Beatmap> FetchBeatmapByQueryAsync(string query, int? expectedId, CancellationToken cancellationToken)
    {
        OsuBeatmap lookup = await GetJsonAsync<OsuBeatmap>(url: $"{BeatmapLookupEndpoint}?{query}", cancellationToken).ConfigureAwait(false);

        if (lookup.BeatmapSetId <= 0)
        {
            throw new FetcherException($"Lookup for beatmap {expectedId ?? lookup.Id} did not include a valid beatmap set ID.");
        }

        OsuBeatmapSet setDto = await GetJsonAsync<OsuBeatmapSet>(url: BeatmapSetEndpoint(lookup.BeatmapSetId), cancellationToken).ConfigureAwait(false);
        BeatmapSet set = await PersistSetAsync(setDto, lookup).ConfigureAwait(false);

        Beatmap? result = set.Beatmaps.FirstOrDefault(b => b.Id == (expectedId ?? lookup.Id));
        return result ?? throw new FetcherException($"Fetched beatmap {expectedId ?? lookup.Id} was not present in beatmap set {lookup.BeatmapSetId}.");
    }

    private async Task<BeatmapSet> PersistSetAsync(OsuBeatmapSet setDto, OsuBeatmap? lookup)
    {
        BeatmapSet set = MapSet(setDto);

        Dictionary<int, OsuBeatmap> beatmaps = new();
        if (setDto.Beatmaps != null)
        {
            foreach (OsuBeatmap beatmapDto in setDto.Beatmaps)
            {
                beatmaps[beatmapDto.Id] = beatmapDto;
            }
        }

        if (setDto.Converts != null)
        {
            foreach (OsuBeatmap beatmapDto in setDto.Converts)
            {
                beatmaps[beatmapDto.Id] = beatmapDto;
            }
        }

        if (lookup != null)
        {
            beatmaps[lookup.Id] = lookup;
        }

        foreach (OsuBeatmap beatmapDto in beatmaps.Values)
        {
            set.Beatmaps.Add(MapBeatmap(beatmapDto, set.Id, setDto.Covers));
        }

        return await beatmapSetRepository.UpsertWithBeatmapsAsync(set).ConfigureAwait(false);
    }

    private async Task<T> GetJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        string token = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await httpService.GetJsonAsync<T>(url, bearerToken: token, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpServiceException exception) when (exception.StatusCode == 401)
        {
            await InvalidateTokenAsync().ConfigureAwait(false);
            token = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            return await httpService.GetJsonAsync<T>(url, bearerToken: token, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_accessToken) && _accessTokenExpiryUnixSeconds > DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60)
        {
            return _accessToken!;
        }

        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrEmpty(_accessToken) && _accessTokenExpiryUnixSeconds > DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60)
            {
                return _accessToken!;
            }

            string accessTokenKey = $"fetcher:access_token:{_config.FetcherClientId}";
            string expireAtKey = $"fetcher:expire_at:{_config.FetcherClientId}";
            string refreshTokenKey = $"fetcher:refresh_token:{_config.FetcherClientId}";

            string? cachedToken = _tokenCache.TryGetValue(accessTokenKey, out string? value) ? value : await cache.GetAsync(accessTokenKey).ConfigureAwait(false);
            string? cachedExpiry = _tokenCache.TryGetValue(expireAtKey, out string? expiryValue) ? expiryValue : await cache.GetAsync(expireAtKey).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(cachedToken) && !string.IsNullOrEmpty(cachedExpiry)
                && long.TryParse(cachedExpiry, out long parsedExpiry)
                && parsedExpiry > DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60)
            {
                _accessToken = cachedToken;
                _accessTokenExpiryUnixSeconds = parsedExpiry;
                _tokenCache[accessTokenKey] = cachedToken;
                _tokenCache[expireAtKey] = cachedExpiry;
                return _accessToken!;
            }

            string? cachedRefreshToken = _tokenCache.TryGetValue(refreshTokenKey, out string? refreshValue) ? refreshValue : await cache.GetAsync(refreshTokenKey).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(cachedRefreshToken))
            {
                try
                {
                    return await RefreshAccessTokenAsync(cachedRefreshToken, cancellationToken).ConfigureAwait(false);
                }
                catch (FetcherException)
                {
                    await cache.DeleteAsync(refreshTokenKey).ConfigureAwait(false);
                    _tokenCache.Remove(refreshTokenKey);
                }
            }

            if (string.IsNullOrEmpty(cachedToken) && string.IsNullOrEmpty(cachedExpiry))
            {
                return await GrantAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            }

            await cache.DeleteAsync(accessTokenKey).ConfigureAwait(false);
            await cache.DeleteAsync(expireAtKey).ConfigureAwait(false);

            return await GrantAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<string> GrantAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_config.FetcherClientId <= 0 || string.IsNullOrEmpty(_config.FetcherClientSecret))
        {
            throw new FetcherException("Fetcher OAuth credentials are not configured.");
        }

        Dictionary<string, string> form = new(StringComparer.Ordinal)
        {
            ["client_id"] = _config.FetcherClientId.ToString(),
            ["client_secret"] = _config.FetcherClientSecret,
            ["grant_type"] = "client_credentials",
            ["scope"] = "public",
        };

        string body = await httpService.PostFormAsync(OAuthEndpoint, form, cancellationToken).ConfigureAwait(false);
        OsuTokenResponse? token = JsonConvert.DeserializeObject<OsuTokenResponse>(body);
        if (token == null || string.IsNullOrEmpty(token.AccessToken))
        {
            throw new FetcherException("The osu! API returned an invalid access token payload.");
        }

        await CacheTokenAsync(token).ConfigureAwait(false);
        return _accessToken!;
    }

    private async Task<string> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (_config.FetcherClientId <= 0 || string.IsNullOrEmpty(_config.FetcherClientSecret))
        {
            throw new FetcherException("Fetcher OAuth credentials are not configured.");
        }

        Dictionary<string, string> form = new(StringComparer.Ordinal)
        {
            ["client_id"] = _config.FetcherClientId.ToString(),
            ["client_secret"] = _config.FetcherClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        };

        string body = await httpService.PostFormAsync(OAuthEndpoint, form, cancellationToken).ConfigureAwait(false);
        OsuTokenResponse? token = JsonConvert.DeserializeObject<OsuTokenResponse>(body);
        if (token == null || string.IsNullOrEmpty(token.AccessToken))
        {
            throw new FetcherException("The osu! API returned an invalid refresh token payload.");
        }

        await CacheTokenAsync(token).ConfigureAwait(false);
        return _accessToken!;
    }

    private async Task CacheTokenAsync(OsuTokenResponse token)
    {
        long tokenExpiryUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Math.Max(1, token.ExpiresIn);
        string accessTokenKey = $"fetcher:access_token:{_config.FetcherClientId}";
        string expireAtKey = $"fetcher:expire_at:{_config.FetcherClientId}";
        string refreshTokenKey = $"fetcher:refresh_token:{_config.FetcherClientId}";
        TimeSpan ttl = TimeSpan.FromSeconds(Math.Max(1, token.ExpiresIn));

        await cache.SetAsync(accessTokenKey, token.AccessToken!, ttl).ConfigureAwait(false);
        await cache.SetAsync(expireAtKey, tokenExpiryUnixSeconds.ToString(), ttl).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(token.RefreshToken))
        {
            await cache.SetAsync(refreshTokenKey, token.RefreshToken, ttl.Add(TimeSpan.FromDays(30))).ConfigureAwait(false);
            _tokenCache[refreshTokenKey] = token.RefreshToken;
        }

        _accessToken = token.AccessToken;
        _accessTokenExpiryUnixSeconds = tokenExpiryUnixSeconds;
        _tokenCache[accessTokenKey] = token.AccessToken;
        _tokenCache[expireAtKey] = tokenExpiryUnixSeconds.ToString();
    }

    private async Task InvalidateTokenAsync()
    {
        string accessTokenKey = $"fetcher:access_token:{_config.FetcherClientId}";
        string expireAtKey = $"fetcher:expire_at:{_config.FetcherClientId}";
        string refreshTokenKey = $"fetcher:refresh_token:{_config.FetcherClientId}";

        _accessToken = null;
        _accessTokenExpiryUnixSeconds = 0;
        _tokenCache.Remove(accessTokenKey);
        _tokenCache.Remove(expireAtKey);
        _tokenCache.Remove(refreshTokenKey);

        await cache.DeleteAsync(accessTokenKey).ConfigureAwait(false);
        await cache.DeleteAsync(expireAtKey).ConfigureAwait(false);
        await cache.DeleteAsync(refreshTokenKey).ConfigureAwait(false);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _tokenLock.Dispose();
        }

        _disposed = true;
    }

    private sealed class OsuTokenResponse
    {
        [JsonProperty("access_token")]
        public string? AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }
}