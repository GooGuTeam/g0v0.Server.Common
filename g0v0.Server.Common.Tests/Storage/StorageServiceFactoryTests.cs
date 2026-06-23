// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Storage;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Storage;

[TestFixture]
public class StorageServiceFactoryTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Test]
    public void Create_WithLocalStorage_ShouldParseSnakeCaseOptionsAndUsePathProvider()
    {
        var configuration = new StorageConfiguration
        {
            Type = StorageConfiguration.StorageType.Local,
            Options = JObject.Parse("""{ "local_storage_path": "files" }"""),
        };

        StorageService service = StorageServiceFactory.Create(configuration, new TestPathProvider(_tempDir));

        var local = (LocalStorageService)service;
        Assert.That(local.StoragePath, Is.EqualTo(Path.GetFullPath(Path.Combine(_tempDir, "files"))));
    }

    [Test]
    public void Create_WithLocalStorageWithoutPathProvider_ShouldThrowInvalidOperationException()
    {
        var configuration = new StorageConfiguration
        {
            Type = StorageConfiguration.StorageType.Local,
            Options = JObject.Parse("""{ "local_storage_path": "files" }"""),
        };

        Assert.Throws<InvalidOperationException>(() => StorageServiceFactory.Create(configuration));
    }

    [Test]
    public async Task Create_WithS3Storage_ShouldParseSnakeCaseOptions()
    {
        var configuration = new StorageConfiguration
        {
            Type = StorageConfiguration.StorageType.S3,
            Options = JObject.Parse(
                """
                {
                  "s3_access_key_id": "access",
                  "s3_secret_access_key": "secret",
                  "s3_bucket_name": "bucket",
                  "s3_region_name": "us-east-1",
                  "s3_public_url_base": "https://cdn.example.com"
                }
                """),
        };

        await using StorageService service = StorageServiceFactory.Create(configuration);

        var s3 = (S3StorageService)service;
        Assert.That(s3.BucketName, Is.EqualTo("bucket"));
        Assert.That(s3.RegionName, Is.EqualTo("us-east-1"));
        Assert.That(s3.PublicUrlBase, Is.EqualTo("https://cdn.example.com"));
        Assert.That(s3.EndpointUrl, Is.Null);
    }

    [Test]
    public async Task Create_WithR2Storage_ShouldParseSnakeCaseOptions()
    {
        var configuration = new StorageConfiguration
        {
            Type = StorageConfiguration.StorageType.R2,
            Options = JObject.Parse(
                """
                {
                  "r2_account_id": "account",
                  "r2_access_key_id": "access",
                  "r2_secret_access_key": "secret",
                  "r2_bucket_name": "bucket",
                  "r2_public_url_base": "https://cdn.example.com"
                }
                """),
        };

        await using StorageService service = StorageServiceFactory.Create(configuration);

        var r2 = (CloudflareR2StorageService)service;
        Assert.That(r2.AccountId, Is.EqualTo("account"));
        Assert.That(r2.BucketName, Is.EqualTo("bucket"));
        Assert.That(r2.RegionName, Is.EqualTo("auto"));
        Assert.That(r2.EndpointUrl, Is.EqualTo("https://account.r2.cloudflarestorage.com"));
    }

    private sealed class TestPathProvider : IPathProvider
    {
        private readonly string basePath;

        public TestPathProvider(string basePath)
        {
            this.basePath = basePath;
        }

        public string GetBasePath() => basePath;
    }
}