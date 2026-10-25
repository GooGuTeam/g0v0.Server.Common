// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration.Attributes;

namespace g0v0.Server.Common.Configuration;

/// <summary>Configures beatmap archive mirrors using the lazer API's regional fallback policy.</summary>
[ConfigFile("beatmap_download")]
public class BeatmapDownloadConfig
{
    /// <summary>Gets or sets the full archive URL template outside China; {id} is the set ID.</summary>
    [Reloadable]
    public string FullUrl { get; set; } = "https://catboy.best/d/{id}";

    /// <summary>Gets or sets the video-free archive URL template outside China.</summary>
    [Reloadable]
    public string NoVideoUrl { get; set; } = "https://catboy.best/d/{id}n";

    /// <summary>Gets or sets the full archive URL template for Chinese IP addresses.</summary>
    [Reloadable]
    public string ChinaFullUrl { get; set; } = "https://dl.sayobot.cn/beatmaps/download/full/{id}";

    /// <summary>Gets or sets the video-free archive URL template for Chinese IP addresses.</summary>
    [Reloadable]
    public string ChinaNoVideoUrl { get; set; } = "https://dl.sayobot.cn/beatmaps/download/novideo/{id}";
}