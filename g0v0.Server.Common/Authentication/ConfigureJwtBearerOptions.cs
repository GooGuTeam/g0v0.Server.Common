// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Text;
using g0v0.Server.Common.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace g0v0.Server.Common.Authentication;

/// <summary>
/// Configures <see cref="JwtBearerOptions"/> using DI-resolved services.
/// </summary>
public class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly ConfigurationManager _manager;
    private readonly ILogger<DatabaseJwtTokenHandler> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureJwtBearerOptions"/> class.
    /// </summary>
    /// <param name="manager">The configuration manager for general settings.</param>
    /// <param name="serviceProvider">The root service provider used to resolve scoped dependencies.</param>
    /// <param name="logger">The logger for token handler setup diagnostics.</param>
    public ConfigureJwtBearerOptions(
        ConfigurationManager manager,
        IServiceProvider serviceProvider,
        ILogger<DatabaseJwtTokenHandler> logger)
    {
        _manager = manager;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Configures named JWT bearer options.
    /// </summary>
    /// <param name="name">The authentication scheme name.</param>
    /// <param name="options">The options to configure.</param>
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        Configure(options);
    }

    /// <summary>
    /// Configures the default JWT bearer options.
    /// </summary>
    /// <param name="options">The options to configure.</param>
    public void Configure(JwtBearerOptions options)
    {
        GeneralConfiguration generalConfig = _manager.Get<GeneralConfiguration>();
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(generalConfig.JwtSecretKey));

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateAudience = !string.IsNullOrEmpty(generalConfig.JwtAudience),
            ValidAudience = generalConfig.JwtAudience,
            ValidateIssuer = !string.IsNullOrEmpty(generalConfig.JwtIssuer),
            ValidIssuer = generalConfig.JwtIssuer,
            ValidateLifetime = true,
            NameClaimType = OAuthClaimTypes.Subject,
        };

        options.TokenHandlers.Clear();
        options.TokenHandlers.Add(new DatabaseJwtTokenHandler(_serviceProvider, _logger));

        options.SaveToken = true;
    }
}