// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using g0v0.Server.Common.Storage;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Storage;

[TestFixture]
public class LocalStorageServiceTests
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
    public void Constructor_WithRelativePathAndPathProvider_ShouldResolveUnderBasePath()
    {
        var settings = new LocalStorageSettings { LocalStoragePath = "data/storage" };

        var service = new LocalStorageService(settings, new TestPathProvider(_tempDir));

        Assert.That(service.StoragePath, Is.EqualTo(Path.GetFullPath(Path.Combine(_tempDir, "data/storage"))));
        Assert.That(Directory.Exists(service.StoragePath), Is.True);
    }

    [Test]
    public async Task WriteReadExistsDelete_ShouldRoundTripContentAndCleanEmptyDirectories()
    {
        var service = new LocalStorageService(Path.Combine(_tempDir, "storage"));
        byte[] content = { 1, 2, 3 };

        await service.WriteFileAsync("/avatars/user.bin", content);

        Assert.That(await service.IsExistsAsync("avatars/user.bin").ConfigureAwait(false), Is.True);
        Assert.That(await service.ReadFileAsync("avatars/user.bin").ConfigureAwait(false), Is.EqualTo(content));

        await service.DeleteFileAsync("avatars/user.bin").ConfigureAwait(false);

        Assert.That(await service.IsExistsAsync("avatars/user.bin").ConfigureAwait(false), Is.False);
        Assert.That(Directory.Exists(Path.Combine(service.StoragePath, "avatars")), Is.False);
    }

    [Test]
    public void ReadFileAsync_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        var service = new LocalStorageService(Path.Combine(_tempDir, "storage"));

        Assert.ThrowsAsync<FileNotFoundException>(async () => await service.ReadFileAsync("missing.bin").ConfigureAwait(false));
    }

    [Test]
    public void WriteFileAsync_WithPathEscape_ShouldThrowArgumentException()
    {
        var service = new LocalStorageService(Path.Combine(_tempDir, "storage"));

        Assert.ThrowsAsync<ArgumentException>(
            async () => await service.WriteFileAsync("../outside.bin", new byte[] { 1 }).ConfigureAwait(false));
        Assert.That(File.Exists(Path.Combine(_tempDir, "outside.bin")), Is.False);
    }

    [Test]
    public async Task GetFileUrlAsync_ShouldReturnLocalFileRoute()
    {
        var service = new LocalStorageService(Path.Combine(_tempDir, "storage"));

        string url = await service.GetFileUrlAsync("/dir/file.txt").ConfigureAwait(false);

        Assert.That(url, Is.EqualTo("file/dir/file.txt"));
    }

    [Test]
    public void GetFileNameByUrl_ShouldExtractFileRoutePath()
    {
        var service = new LocalStorageService(Path.Combine(_tempDir, "storage"));

        Assert.That(service.GetFileNameByUrl("file/dir/file.txt"), Is.EqualTo("dir/file.txt"));
        Assert.That(service.GetFileNameByUrl("http://localhost/file/dir/file.txt"), Is.EqualTo("dir/file.txt"));
        Assert.That(service.GetFileNameByUrl("http://localhost/assets/dir/file.txt"), Is.Null);
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
