// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Security.Cryptography;
using g0v0.Server.Common.Configuration;
using MaxMind.Db;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Responses;
using Microsoft.Extensions.Logging;

namespace g0v0.Server.Common.GeoIP;

/// <summary>
/// Manages reusable in-memory Country and ASN database readers.
/// </summary>
public sealed partial class GeoIPService : IGeoIPService, IDisposable
{
    private static readonly string[] Editions = ["Country", "ASN"];
    private readonly GeoIPConfig _config;
    private readonly string _directory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeoIPService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Lock _readersLock = new();
    private readonly Lock _disposeLock = new();
    private readonly SemaphoreSlim _updateLock = new(1, 1);
    private readonly Dictionary<string, DatabaseReader> _readers = new(StringComparer.Ordinal);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeoIPService"/> class and opens existing local databases.
    /// </summary>
    /// <param name="config">The database configuration.</param>
    /// <param name="pathProvider">The server base path provider.</param>
    /// <param name="httpClientFactory">The factory used for update HTTP requests.</param>
    /// <param name="logger">The service logger.</param>
    /// <param name="timeProvider">The clock used for database freshness checks.</param>
    public GeoIPService(GeoIPConfig config, IPathProvider pathProvider, IHttpClientFactory httpClientFactory, ILogger<GeoIPService> logger, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.DatabaseDirectory);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(config.MaxAgeDays);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(config.RequestTimeoutSeconds);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(config.UpdateInterval, TimeSpan.Zero);
        if (!config.DatabaseBaseUrl.IsAbsoluteUri || config.DatabaseBaseUrl.Scheme is not ("https" or "http"))
        {
            throw new ArgumentException("The GeoIP database source must be an absolute HTTP or HTTPS URL.", nameof(config));
        }

        _config = config;
        _directory = Path.GetFullPath(config.DatabaseDirectory, pathProvider.GetBasePath());
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _timeProvider = timeProvider;
        foreach (string edition in Editions)
        {
            string? path = latestFile(edition);
            if (path == null)
            {
                continue;
            }

            try
            {
                _readers.Add(edition, openDatabase(path, edition));
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or InvalidDatabaseException or UnauthorizedAccessException)
            {
                logLoadFailure(_logger, path, exception);
            }
        }
    }

    /// <inheritdoc />
    public GeoIPLookupResult Lookup(string ip)
    {
        ArgumentNullException.ThrowIfNull(ip);
        return IPAddress.TryParse(ip, out IPAddress? address)
            ? Lookup(address)
            : throw new FormatException("The GeoIP lookup address is invalid.");
    }

    /// <inheritdoc />
    public GeoIPLookupResult Lookup(IPAddress ip)
    {
        ArgumentNullException.ThrowIfNull(ip);
        IPAddress address = ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip;
        lock (_readersLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            CountryResponse? country = null;
            AsnResponse? asn = null;
            if (_readers.TryGetValue("Country", out DatabaseReader? countryReader))
            {
                countryReader.TryCountry(address, out country);
            }

            if (_readers.TryGetValue("ASN", out DatabaseReader? asnReader))
            {
                asnReader.TryAsn(address, out asn);
            }

            return new GeoIPLookupResult(address.ToString())
            {
                CountryIso = country?.Country.IsoCode,
                CountryName = country?.Country.Name,
                Asn = asn?.AutonomousSystemNumber,
                Organization = asn?.AutonomousSystemOrganization
            };
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        await _updateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        List<(string Edition, string Path, DatabaseReader Reader)> pending = [];
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            using HttpClient client = _httpClientFactory.CreateClient(nameof(GeoIPService));
            client.Timeout = TimeSpan.FromSeconds(_config.RequestTimeoutSeconds);
            foreach (string edition in Editions)
            {
                string? path = latestFile(edition);
                bool loaded;
                lock (_readersLock)
                {
                    loaded = _readers.ContainsKey(edition);
                }

                if (!force && loaded && path != null
                    && _timeProvider.GetUtcNow().UtcDateTime - File.GetLastWriteTimeUtc(path) < TimeSpan.FromDays(_config.MaxAgeDays))
                {
                    continue;
                }

                (string temporaryPath, DatabaseReader reader) = await downloadAsync(client, edition, cancellationToken).ConfigureAwait(false);
                pending.Add((edition, temporaryPath, reader));
            }

            cancellationToken.ThrowIfCancellationRequested();
            foreach ((_, string path, _) in pending)
            {
                File.SetLastWriteTimeUtc(path, _timeProvider.GetUtcNow().UtcDateTime);
            }

            lock (_readersLock)
            {
                while (pending.Count > 0)
                {
                    (string edition, string path, DatabaseReader reader) = pending[0];
                    File.Move(path, Path.Combine(_directory, filename(edition)), overwrite: true);
                    if (_readers.Remove(edition, out DatabaseReader? previous))
                    {
                        previous.Dispose();
                    }

                    _readers.Add(edition, reader);
                    pending.RemoveAt(0);
                    logUpdated(_logger, edition);
                }
            }
        }
        finally
        {
            try
            {
                foreach ((_, string path, DatabaseReader reader) in pending)
                {
                    reader.Dispose();
                    File.Delete(path);
                }
            }
            finally
            {
                _updateLock.Release();
            }
        }
    }

    /// <summary>
    /// Closes the database readers after any running update completes.
    /// </summary>
    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_disposed)
            {
                return;
            }

            _updateLock.Wait();
            try
            {
                lock (_readersLock)
                {
                    _disposed = true;
                    foreach (DatabaseReader reader in _readers.Values)
                    {
                        reader.Dispose();
                    }

                    _readers.Clear();
                }
            }
            finally
            {
                _updateLock.Release();
                _updateLock.Dispose();
            }
        }
    }

    private static string filename(string edition) => $"GeoLite2-{edition}.mmdb";

    private static DatabaseReader openDatabase(string path, string edition)
    {
        DatabaseReader reader = new(path, FileAccessMode.Memory);
        if (reader.Metadata.DatabaseType.EndsWith($"-{edition}", StringComparison.Ordinal))
        {
            return reader;
        }

        reader.Dispose();
        throw new InvalidDataException($"The database does not contain {edition} records.");
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Could not open local GeoIP database {Path}.")]
    private static partial void logLoadFailure(ILogger logger, string path, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Updated the GeoIP {Edition} database.")]
    private static partial void logUpdated(ILogger logger, string edition);

    private string? latestFile(string edition)
    {
        return Directory.Exists(_directory)
            ? Directory.EnumerateFiles(_directory, $"GeoLite2-{edition}*.mmdb")
                .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
            : null;
    }

    private async Task<(string Path, DatabaseReader Reader)> downloadAsync(HttpClient client, string edition, CancellationToken cancellationToken)
    {
        string name = filename(edition);
        Uri source = new(_config.DatabaseBaseUrl.AbsoluteUri.TrimEnd('/') + "/");
        string checksum = await client.GetStringAsync(new Uri(source, name + ".sha256sum"), cancellationToken).ConfigureAwait(false);
        string? expectedHash = checksum.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (expectedHash?.Length != 64 || !expectedHash.All(Uri.IsHexDigit))
        {
            throw new InvalidDataException($"Invalid SHA256 checksum for {name}.");
        }

        Directory.CreateDirectory(_directory);
        string temporaryPath = Path.Combine(_directory, $"{name}.{Guid.NewGuid():N}.tmp");
        DatabaseReader? reader = null;
        try
        {
            using IncrementalHash digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using HttpResponseMessage response = await client.GetAsync(new Uri(source, name), HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using (Stream input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (FileStream output = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
            {
                byte[] buffer = new byte[81920];
                int count;
                while ((count = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    digest.AppendData(buffer, 0, count);
                    await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
                }
            }

            if (!string.Equals(Convert.ToHexString(digest.GetHashAndReset()), expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"SHA256 checksum mismatch for {name}.");
            }

            reader = openDatabase(temporaryPath, edition);
            return (temporaryPath, reader);
        }
        catch
        {
            reader?.Dispose();
            File.Delete(temporaryPath);
            throw;
        }
    }
}