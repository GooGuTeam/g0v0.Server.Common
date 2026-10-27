// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Configuration;

[TestFixture]
public class GeneralConfigTests
{
    [Test]
    public void GetCorsAllowUrls_IncludesConfiguredUrlsAndBothTrailingSlashForms()
    {
        GeneralConfig config = new()
        {
            FrontendUrl = "https://frontend.example/",
            CORSAllowUrls = ["https://allowed.example", "https://other.example/", " "]
        };

        string[] urls = config.GetCorsAllowUrls();

        Assert.That(
            urls,
            Is.EqualTo(
            [
                "https://allowed.example",
                "https://allowed.example/",
                "https://other.example",
                "https://other.example/",
                "https://frontend.example",
                "https://frontend.example/"
            ]));
    }
}