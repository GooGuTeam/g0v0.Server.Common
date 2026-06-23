// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Storage;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Storage;

[TestFixture]
public class S3StorageServiceTests
{
    [Test]
    public async Task GetFileUrlAsync_WithPublicUrlBase_ShouldJoinBaseAndPathWithoutNetwork()
    {
        await using var service = new S3StorageService(
            "access",
            "secret",
            "bucket",
            "us-east-1",
            "https://cdn.example.com/assets/");

        string url = await service.GetFileUrlAsync("/dir/file.txt").ConfigureAwait(false);

        Assert.That(url, Is.EqualTo("https://cdn.example.com/assets/dir/file.txt"));
    }

    [Test]
    public async Task AWSS3GetFileNameByUrl_ShouldExtractS3UrlFormats()
    {
        await using var service = new S3StorageService(
            "access",
            "secret",
            "bucket",
            "us-east-1",
            "https://cdn.example.com/assets");

        Assert.That(
            service.GetFileNameByUrl("https://cdn.example.com/assets/dir/file.txt"),
            Is.EqualTo("assets/dir/file.txt"));
        Assert.That(
            service.GetFileNameByUrl("https://s3.amazonaws.com/bucket/dir/file.txt"),
            Is.EqualTo("dir/file.txt"));
        Assert.That(
            service.GetFileNameByUrl("https://bucket.s3.us-east-1.amazonaws.com/dir/file.txt"),
            Is.EqualTo("dir/file.txt"));
        Assert.That(service.GetFileNameByUrl("dir/file.txt"), Is.EqualTo("dir/file.txt"));
    }

    [Test]
    public async Task CloudflareR2GetFileNameByUrl_ShouldExtractR2UrlFormats()
    {
        await using var service = new CloudflareR2StorageService(
            "account",
            "access",
            "secret",
            "bucket",
            "https://cdn.example.com/public");

        Assert.That(service.EndpointUrl, Is.EqualTo("https://account.r2.cloudflarestorage.com"));
        Assert.That(
            service.GetFileNameByUrl("https://account.r2.cloudflarestorage.com/dir/file.txt"),
            Is.EqualTo("dir/file.txt"));
        Assert.That(
            service.GetFileNameByUrl("https://cdn.example.com/public/dir/file.txt"),
            Is.EqualTo("public/dir/file.txt"));
        Assert.That(service.GetFileNameByUrl(string.Empty), Is.Null);
    }
}