// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Authentication;
using g0v0.Server.Common.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Authentication;

[TestFixture]
public class ConfigureJwtBearerOptionsTests
{
    private string _tempDir = null!;
    private string _configDir = null!;
    private ConfigurationManager _manager = null!;
    private ILogger<DatabaseJwtTokenHandler> _logger = null!;
    private IServiceProvider _serviceProvider = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _configDir = Path.Combine(_tempDir, "config");
        Directory.CreateDirectory(_configDir);
        _logger = NullLogger<DatabaseJwtTokenHandler>.Instance;
        _serviceProvider = new EmptyServiceProvider();
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
    public void Configure_WithJwtSettings_ShouldSetTokenValidationParameters()
    {
        WriteGeneralConfig(new
        {
            JwtSecretKey = "super_secret_key_1234567890123456",
            JwtAudience = "my_audience",
            JwtIssuer = "my_issuer",
        });

        _manager = new ConfigurationManager(_tempDir);

        JwtBearerOptions options = new();
        ConfigureJwtBearerOptions configurer = new(_manager, _serviceProvider, _logger);

        configurer.Configure(options);

        Assert.That(options.TokenValidationParameters, Is.Not.Null);
        Assert.That(options.TokenValidationParameters.ValidateIssuerSigningKey, Is.True);
        Assert.That(options.TokenValidationParameters.ValidateAudience, Is.True);
        Assert.That(options.TokenValidationParameters.ValidAudience, Is.EqualTo("my_audience"));
        Assert.That(options.TokenValidationParameters.ValidateIssuer, Is.True);
        Assert.That(options.TokenValidationParameters.ValidIssuer, Is.EqualTo("my_issuer"));
        Assert.That(options.TokenValidationParameters.ValidateLifetime, Is.True);
        Assert.That(options.TokenValidationParameters.NameClaimType, Is.EqualTo(OAuthClaimTypes.Subject));
    }

    [Test]
    public void Configure_WithEmptyAudienceAndIssuer_ShouldDisableValidation()
    {
        WriteGeneralConfig(new
        {
            JwtSecretKey = "super_secret_key_1234567890123456",
            JwtAudience = string.Empty,
            JwtIssuer = string.Empty,
        });

        _manager = new ConfigurationManager(_tempDir);

        JwtBearerOptions options = new();
        ConfigureJwtBearerOptions configurer = new(_manager, _serviceProvider, _logger);

        configurer.Configure(options);

        Assert.That(options.TokenValidationParameters.ValidateAudience, Is.False);
        Assert.That(options.TokenValidationParameters.ValidateIssuer, Is.False);
    }

    [Test]
    public void Configure_WithNullAudience_ShouldDisableAudienceValidation()
    {
        WriteGeneralConfig(new
        {
            JwtSecretKey = "super_secret_key_1234567890123456",
            JwtAudience = (string?)null,
            JwtIssuer = "my_issuer",
        });

        _manager = new ConfigurationManager(_tempDir);

        JwtBearerOptions options = new();
        ConfigureJwtBearerOptions configurer = new(_manager, _serviceProvider, _logger);

        configurer.Configure(options);

        Assert.That(options.TokenValidationParameters.ValidateAudience, Is.False);
        Assert.That(options.TokenValidationParameters.ValidateIssuer, Is.True);
    }

    [Test]
    public void Configure_ShouldReplaceDefaultTokenHandlersWithDatabaseTokenHandler()
    {
        WriteGeneralConfig(new
        {
            JwtSecretKey = "super_secret_key_1234567890123456",
        });

        _manager = new ConfigurationManager(_tempDir);

        JwtBearerOptions options = new();
        ConfigureJwtBearerOptions configurer = new(_manager, _serviceProvider, _logger);

        configurer.Configure(options);

        Assert.That(options.TokenHandlers, Has.Exactly(1).Items);
        Assert.That(options.TokenHandlers[0], Is.InstanceOf<DatabaseJwtTokenHandler>());
    }

    [Test]
    public void Configure_ShouldSetSaveTokenToTrue()
    {
        WriteGeneralConfig(new
        {
            JwtSecretKey = "super_secret_key_1234567890123456",
        });

        _manager = new ConfigurationManager(_tempDir);

        JwtBearerOptions options = new();
        ConfigureJwtBearerOptions configurer = new(_manager, _serviceProvider, _logger);

        configurer.Configure(options);

        Assert.That(options.SaveToken, Is.True);
    }

    [Test]
    public void Configure_NamedOptionsWithMatchingScheme_ShouldConfigure()
    {
        WriteGeneralConfig(new
        {
            JwtSecretKey = "super_secret_key_1234567890123456",
        });

        _manager = new ConfigurationManager(_tempDir);

        JwtBearerOptions options = new();
        ConfigureJwtBearerOptions configurer = new(_manager, _serviceProvider, _logger);

        configurer.Configure(JwtBearerDefaults.AuthenticationScheme, options);

        Assert.That(options.TokenValidationParameters, Is.Not.Null);
    }

    [Test]
    public void Configure_NamedOptionsWithNonMatchingScheme_ShouldNotConfigure()
    {
        WriteGeneralConfig(new
        {
            JwtSecretKey = "super_secret_key_1234567890123456",
        });

        _manager = new ConfigurationManager(_tempDir);

        JwtBearerOptions options = new();
        ConfigureJwtBearerOptions configurer = new(_manager, _serviceProvider, _logger);

        configurer.Configure("OtherScheme", options);

        // The options should not have our custom NameClaimType set.
        Assert.That(options.TokenValidationParameters.NameClaimType, Is.Not.EqualTo(OAuthClaimTypes.Subject));
    }

    private void WriteGeneralConfig(object content)
    {
        File.WriteAllText(
            Path.Combine(_configDir, "general.json"),
            JsonConvert.SerializeObject(content));
    }

    /// <summary>
    /// Minimal service provider that returns no services.
    /// </summary>
    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}