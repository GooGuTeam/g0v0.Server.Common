// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

// ReSharper disable AccessToDisposedClosure
// ReSharper disable InconsistentNaming
// ReSharper disable LoopCanBePartlyConvertedToQuery
using System.Net;
using System.Security.Cryptography;
using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Extensions;
using g0v0.Server.Common.GeoIP;
using MaxMind.Db;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Responses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.GeoIP;

[TestFixture]
[Explicit("Requires GeoLite2-Country.mmdb and GeoLite2-ASN.mmdb in the Common repository geoip/ directory. Run with --filter FullyQualifiedName~GeoIPServiceTests.")]
public class GeoIPServiceTests
{
    private string _root = null!;
    private GeoIPConfig _config = null!;
    private TestClock _clock = null!;
    private DatabaseHandler _handler = null!;

    private string? _countryIso;
    private string? _countryName;
    private string? _ipv6CountryIso;
    private long? _asn;
    private string? _organization;

    private static string DatabaseDirectory => Environment.GetEnvironmentVariable("G0V0_GEOIP_TEST_DATABASE_DIRECTORY")
        ?? Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../geoip"));

    [OneTimeSetUp]
    public void LoadReferenceDatabases()
    {
        foreach (string edition in new[] { "Country", "ASN" })
        {
            string path = Path.Combine(DatabaseDirectory, $"GeoLite2-{edition}.mmdb");
            Assert.That(File.Exists(path), Is.True, $"Place the required MMDB at {path} before explicitly running GeoIP tests.");
        }

        using DatabaseReader country = new(Path.Combine(DatabaseDirectory, "GeoLite2-Country.mmdb"));
        using DatabaseReader asn = new(Path.Combine(DatabaseDirectory, "GeoLite2-ASN.mmdb"));
        CountryResponse countryResult = country.Country("81.2.69.142");
        AsnResponse asnResult = asn.Asn("8.8.8.8");
        _countryIso = countryResult.Country.IsoCode;
        _countryName = countryResult.Country.Names.GetValueOrDefault("en");
        _ipv6CountryIso = country.Country("2001:4860:4860::8888").Country.IsoCode;
        _asn = asnResult.AutonomousSystemNumber;
        _organization = asnResult.AutonomousSystemOrganization;
        Assert.Multiple(() =>
        {
            Assert.That(_countryIso, Is.Not.Null.And.Not.Empty);
            Assert.That(_countryName, Is.Not.Null.And.Not.Empty);
            Assert.That(_ipv6CountryIso, Is.Not.Null.And.Not.Empty);
            Assert.That(_asn, Is.GreaterThan(0));
            Assert.That(_organization, Is.Not.Null.And.Not.Empty);
        });
    }

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), $"geoip-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        _config = new GeoIPConfig { DatabaseBaseUrl = new Uri("https://geoip.test/") };
        _clock = new TestClock();
        _handler = new DatabaseHandler();
    }

    [TearDown]
    public void TearDown()
    {
        _handler.Dispose();
        Directory.Delete(_root, recursive: true);
    }

    [Test]
    public void MissingDatabasesReturnOnlyTheNormalizedIpWithoutNetworkAccess()
    {
        using GeoIPService service = CreateService();
        GeoIPLookupResult result = service.Lookup("::ffff:81.2.69.142");
        Assert.That(result, Is.EqualTo(new GeoIPLookupResult("81.2.69.142")));
        Assert.That(_handler.RequestCount, Is.Zero);
        Assert.Throws<FormatException>(() => service.Lookup("invalid-ip"));
    }

    [Test]
    public void LoadsCountryAndAsnDatabasesAndSupportsIpv6AndUnknownAddresses()
    {
        WriteLocalDatabases();
        using GeoIPService service = CreateService();
        Assert.Multiple(() =>
        {
            Assert.That(service.Lookup("81.2.69.142").CountryIso, Is.EqualTo(_countryIso));
            Assert.That(service.Lookup("81.2.69.142").CountryName, Is.EqualTo(_countryName));
            Assert.That(service.Lookup("8.8.8.8").Asn, Is.EqualTo(_asn));
            Assert.That(service.Lookup("8.8.8.8").Organization, Is.EqualTo(_organization));
            Assert.That(service.Lookup("2001:4860:4860::8888").CountryIso, Is.EqualTo(_ipv6CountryIso));
            Assert.That(service.Lookup(IPAddress.Parse("::ffff:81.2.69.142")).CountryIso, Is.EqualTo(_countryIso));
            Assert.That(service.Lookup("127.0.0.1"), Is.EqualTo(new GeoIPLookupResult("127.0.0.1")));
            Assert.That(_handler.RequestCount, Is.Zero);
        });
    }

    [Test]
    public void ACorruptLocalDatabaseDoesNotPreventUsingTheOtherEdition()
    {
        WriteLocalDatabases();
        File.WriteAllBytes(DatabasePath("Country"), [0, 1, 2]);
        using GeoIPService service = CreateService();
        Assert.That(service.Lookup("81.2.69.142").CountryIso, Is.Null);
        Assert.That(service.Lookup("8.8.8.8").Asn, Is.EqualTo(_asn));
    }

    [Test]
    public async Task DownloadsMissingDatabasesAndSkipsFreshOnes()
    {
        using GeoIPService service = CreateService();
        await service.UpdateAsync();
        Assert.Multiple(() =>
        {
            Assert.That(_handler.RequestCount, Is.EqualTo(4));
            Assert.That(service.Lookup("81.2.69.142").CountryIso, Is.EqualTo(_countryIso));
            Assert.That(service.Lookup("8.8.8.8").Asn, Is.EqualTo(_asn));
            Assert.That(File.ReadAllBytes(DatabasePath("Country")), Is.EqualTo(_handler.CountryData));
            Assert.That(File.ReadAllBytes(DatabasePath("ASN")), Is.EqualTo(_handler.AsnData));
        });

        await service.UpdateAsync();
        Assert.That(_handler.RequestCount, Is.EqualTo(4));
        Assert.That(Directory.EnumerateFiles(Path.GetDirectoryName(DatabasePath("Country"))!, "*.tmp"), Is.Empty);
    }

    [Test]
    public async Task UpdatesStaleDatabasesAndSupportsForcedUpdates()
    {
        WriteLocalDatabases();
        using GeoIPService service = CreateService();
        await service.UpdateAsync();
        Assert.That(_handler.RequestCount, Is.Zero);
        _clock.UtcNow += TimeSpan.FromDays(9);
        await service.UpdateAsync();
        Assert.That(_handler.RequestCount, Is.EqualTo(4));
        await service.UpdateAsync();
        Assert.That(_handler.RequestCount, Is.EqualTo(4));
        await service.UpdateAsync(force: true);
        Assert.That(_handler.RequestCount, Is.EqualTo(8));
    }

    [TestCase("Country")]
    [TestCase("ASN")]
    public void BadChecksumPreservesBothReadersAndExistingFiles(string edition)
    {
        WriteLocalDatabases();
        using GeoIPService service = CreateService();
        _handler.BadChecksumEdition = edition;
        Assert.ThrowsAsync<InvalidDataException>(() => service.UpdateAsync(force: true));
        AssertOldDatabasesRemainUsable(service);
    }

    [Test]
    public void VerifiedButCorruptDatabaseDoesNotReplaceAnExistingFile()
    {
        WriteLocalDatabases();
        using GeoIPService service = CreateService();
        byte[] original = _handler.CountryData;
        _handler.CountryData = [0, 1, 2];
        Assert.ThrowsAsync<InvalidDatabaseException>(() => service.UpdateAsync(force: true));
        Assert.That(File.ReadAllBytes(DatabasePath("Country")), Is.EqualTo(original));
        Assert.That(service.Lookup("81.2.69.142").CountryIso, Is.EqualTo(_countryIso));
        Assert.That(Directory.EnumerateFiles(Path.GetDirectoryName(DatabasePath("Country"))!, "*.tmp"), Is.Empty);
    }

    [Test]
    public void RejectsDatabaseFromTheWrongEditionEvenWhenChecksumIsValid()
    {
        WriteLocalDatabases();
        using GeoIPService service = CreateService();
        _handler.CountryData = _handler.AsnData;
        Assert.ThrowsAsync<InvalidDataException>(() => service.UpdateAsync(force: true));
        Assert.That(service.Lookup("81.2.69.142").CountryIso, Is.EqualTo(_countryIso));
    }

    [Test]
    public void FailedHttpRequestsLeaveExistingDatabasesAvailable()
    {
        WriteLocalDatabases();
        using GeoIPService service = CreateService();
        _handler.FailHttp = true;
        Assert.ThrowsAsync<HttpRequestException>(() => service.UpdateAsync(force: true));
        AssertOldDatabasesRemainUsable(service);
    }

    [Test]
    public void CancellationCleansTemporaryFilesAndPreservesReaders()
    {
        WriteLocalDatabases();
        using GeoIPService service = CreateService();
        using CancellationTokenSource cancellation = new();
        _handler.BeforeDownload = cancellation.Cancel;
        Assert.CatchAsync<OperationCanceledException>(() => service.UpdateAsync(force: true, cancellationToken: cancellation.Token));
        AssertOldDatabasesRemainUsable(service);
    }

    [Test]
    public async Task ConcurrentUpdateChecksDownloadEachEditionOnlyOnce()
    {
        using GeoIPService service = CreateService();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => service.UpdateAsync()));
        Assert.That(_handler.RequestCount, Is.EqualTo(4));
        Assert.That(service.Lookup("81.2.69.142").CountryIso, Is.EqualTo(_countryIso));
    }

    [Test]
    public async Task LookupsRemainValidDuringDatabaseReplacement()
    {
        WriteLocalDatabases();
        using GeoIPService service = CreateService();
        Task lookups = Task.Run(() => Parallel.For(0, 1000, _ =>
            Assert.That(service.Lookup("81.2.69.142").CountryIso, Is.EqualTo(_countryIso))));
        await Task.WhenAll(lookups, service.UpdateAsync(force: true));
    }

    [Test]
    public void DisposeIsIdempotentAndPreventsFurtherLookups()
    {
        GeoIPService service = CreateService();
        service.Dispose();
        Assert.DoesNotThrow(service.Dispose);
        Assert.Throws<ObjectDisposedException>(() => service.Lookup("8.8.8.8"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RegistrationUsesOneSingletonAndOptionalGeoIPConfiguration(bool configured)
    {
        if (configured)
        {
            Directory.CreateDirectory(Path.Combine(_root, "config"));
            File.WriteAllText(Path.Combine(_root, "config/geoip.json"), "{\"MaxAgeDays\":3}");
        }

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IPathProvider>(new TestPathProvider(_root));
        services.AddSingleton(new ConfigManager(_root));
        services.AddGeoIP();
        services.AddGeoIP();
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IGeoIPService>(), Is.SameAs(provider.GetRequiredService<GeoIPService>()));
            Assert.That(provider.GetServices<IHostedService>().OfType<GeoIPUpdateService>().Count(), Is.EqualTo(1));
            Assert.That(provider.GetRequiredService<GeoIPConfig>().MaxAgeDays, Is.EqualTo(configured ? 3 : 8));
        });
    }

    [Test]
    public async Task BackgroundInitializationFailureDoesNotFaultTheHostedService()
    {
        FailingGeoIPService fake = new();
        using GeoIPUpdateService worker = new(fake, _config, TimeProvider.System, NullLogger<GeoIPUpdateService>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await fake.Called.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);
        Assert.That(worker.ExecuteTask!.IsFaulted, Is.False);
    }

    private static byte[] ReadFixture(string name)
    {
        return File.ReadAllBytes(Path.Combine(DatabaseDirectory, name));
    }

    private GeoIPService CreateService()
    {
        return new GeoIPService(_config, new TestPathProvider(_root), new TestClientFactory(_handler), NullLogger<GeoIPService>.Instance, _clock);
    }

    private string DatabasePath(string edition) => Path.Combine(_root, _config.DatabaseDirectory, $"GeoLite2-{edition}.mmdb");

    private void WriteLocalDatabases()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath("Country"))!);
        File.WriteAllBytes(DatabasePath("Country"), _handler.CountryData);
        File.WriteAllBytes(DatabasePath("ASN"), _handler.AsnData);
        File.SetLastWriteTimeUtc(DatabasePath("Country"), _clock.UtcNow.UtcDateTime);
        File.SetLastWriteTimeUtc(DatabasePath("ASN"), _clock.UtcNow.UtcDateTime);
    }

    private void AssertOldDatabasesRemainUsable(GeoIPService service)
    {
        Assert.Multiple(() =>
        {
            Assert.That(service.Lookup("81.2.69.142").CountryIso, Is.EqualTo(_countryIso));
            Assert.That(service.Lookup("8.8.8.8").Asn, Is.EqualTo(_asn));
            Assert.That(File.ReadAllBytes(DatabasePath("Country")), Is.EqualTo(_handler.CountryData));
            Assert.That(File.ReadAllBytes(DatabasePath("ASN")), Is.EqualTo(_handler.AsnData));
            Assert.That(Directory.EnumerateFiles(Path.GetDirectoryName(DatabasePath("Country"))!, "*.tmp"), Is.Empty);
        });
    }

    private sealed class TestPathProvider(string root) : IPathProvider
    {
        public string GetBasePath() => root;
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class TestClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class DatabaseHandler : HttpMessageHandler
    {
        private int _requestCount;

        public byte[] CountryData { get; set; } = ReadFixture("GeoLite2-Country.mmdb");
        public byte[] AsnData { get; } = ReadFixture("GeoLite2-ASN.mmdb");
        public string? BadChecksumEdition { get; set; }
        public bool FailHttp { get; set; }
        public Action? BeforeDownload { get; set; }
        public int RequestCount => _requestCount;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requestCount);
            await Task.Yield();
            if (FailHttp)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            bool country = request.RequestUri!.AbsolutePath.Contains("Country", StringComparison.Ordinal);
            string edition = country ? "Country" : "ASN";
            byte[] data = country ? CountryData : AsnData;
            if (request.RequestUri.AbsolutePath.EndsWith(".sha256sum", StringComparison.Ordinal))
            {
                string hash = string.Equals(BadChecksumEdition, edition, StringComparison.Ordinal)
                    ? new string('0', 64)
                    : Convert.ToHexString(SHA256.HashData(data));
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"{hash}  GeoLite2-{edition}.mmdb\n") };
            }

            BeforeDownload?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(data) };
        }
    }

    private sealed class FailingGeoIPService : IGeoIPService
    {
        public TaskCompletionSource Called { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public GeoIPLookupResult Lookup(string ip) => throw new NotSupportedException();
        public GeoIPLookupResult Lookup(IPAddress ip) => throw new NotSupportedException();

        public Task UpdateAsync(bool force = false, CancellationToken cancellationToken = default)
        {
            Called.TrySetResult();
            return Task.FromException(new HttpRequestException("Simulated network failure."));
        }
    }
}