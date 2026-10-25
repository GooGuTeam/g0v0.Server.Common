// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using g0v0.Server.Common.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace g0v0.Server.Common.Screenshots;

/// <summary>Validates images and shares screenshot storage.</summary>
/// <param name="screenshots">The screenshot repository.</param>
/// <param name="storage">The shared storage.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class ScreenshotService(IScreenshotRepository screenshots, IStorageService storage, TimeProvider timeProvider)
{
    /// <summary>The maximum screenshot upload size, in bytes.</summary>
    public const int MaximumUploadSize = 4 * 1024 * 1024;

    private const long MaximumPixels = 33_177_600; // One 8K frame.

    /// <summary>Stores a valid JPEG or PNG as a JPEG using the lazer content-addressed key.</summary>
    /// <param name="userId">The uploading user ID.</param>
    /// <param name="content">The uploaded bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The share hash, or null for an invalid image.</returns>
    public async Task<string?> UploadAsync(int userId, byte[] content, CancellationToken cancellationToken = default)
    {
        if (content.Length is 0 or > MaximumUploadSize)
        {
            return null;
        }

        byte[]? jpeg = await NormalizeAsync(content, cancellationToken).ConfigureAwait(false);
        if (jpeg is null)
        {
            return null;
        }

        string hash = Convert.ToHexStringLower(SHA256.HashData(jpeg));
        string path = GetStoragePath(userId, hash);
        await storage.WriteFileAsync(path, jpeg, "image/jpeg", cancellationToken: cancellationToken).ConfigureAwait(false);
        string url = await storage.GetFileUrlAsync(path, cancellationToken).ConfigureAwait(false);
        DateTimeOffset now = timeProvider.GetUtcNow();
        Screenshot screenshot = new()
        {
            Sha256Hash = hash,
            UserId = userId,
            Url = url,
            Timestamp = now,
            LastAccess = now,
        };
        await screenshots.RecordUploadAsync(screenshot, cancellationToken).ConfigureAwait(false);
        return hash;
    }

    private static string GetStoragePath(int userId, string hash)
        => FormattableString.Invariant($"screenshots/{userId}_{hash}.jpg");

    private static async Task<byte[]?> NormalizeAsync(byte[] content, CancellationToken cancellationToken)
    {
        try
        {
            using MemoryStream input = new(content, writable: false);
            DecoderOptions options = new()
            {
                Configuration = new SixLabors.ImageSharp.Configuration(new JpegConfigurationModule(), new PngConfigurationModule()),
                SkipMetadata = true,
                MaxFrames = 1,
            };
            ImageInfo info = await Image.IdentifyAsync(options, input, cancellationToken).ConfigureAwait(false);
            IImageFormat? format = info.Metadata.DecodedImageFormat;
            if (format is not JpegFormat and not PngFormat || (long)info.Width * info.Height > MaximumPixels)
            {
                return null;
            }

            input.Position = 0;
            using Image image = await Image.LoadAsync(options, input, cancellationToken).ConfigureAwait(false);
            if (format is JpegFormat)
            {
                return content;
            }

            using MemoryStream output = new();
            await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 90, SkipMetadata = true }, cancellationToken).ConfigureAwait(false);
            return output.ToArray();
        }
        catch (UnknownImageFormatException)
        {
            return null;
        }
        catch (InvalidImageContentException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}