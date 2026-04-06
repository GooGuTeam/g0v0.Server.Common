// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration.Attributes;
using Newtonsoft.Json;

namespace g0v0.Server.Common.Configuration;

/// <summary>
/// Represents the general configuration settings for the application, including global settings for any server instance.
/// </summary>
[ConfigurationFile("general")]
public class GeneralConfiguration
{
    #region Database

    /// <summary>
    /// Gets or sets a value indicating whether it gets whether using g0v0 v1 database (MySQL + Old schemas).
    /// </summary>
    public bool UseLegacyDatabase { get; set; } = true;

    /// <summary>
    /// Gets or sets MySQL database host.
    /// </summary>
    public string MySqlHost { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets MySQL database port.
    /// </summary>
    public string MySqlPort { get; set; } = "3306";

    /// <summary>
    /// Gets or sets MySQL database.
    /// </summary>
    public string MySqlDatabase { get; set; } = "osu_api";

    /// <summary>
    /// Gets or sets MySQL username to connect.
    /// </summary>
    public string MySqlUsername { get; set; } = "osu_api";

    /// <summary>
    /// Gets or sets MySQL password to connect.
    /// </summary>
    public string MySqlPassword { get; set; } = "password";

    /// <summary>
    /// Gets MySQL connection string based on the provided configuration properties.
    /// </summary>
    [JsonIgnore]
    public string MySqlConnectionString =>
        $"Server={MySqlHost};Port={MySqlPort};Database={MySqlDatabase};Uid={MySqlUsername};Pwd={MySqlPassword};";

    /// <summary>
    /// Gets or sets the new database (v2) connection string.
    /// </summary>
    public string PostgresqlConnectionString { get; set; } =
        "Host=localhost;Port=5432;Username=g0v0;Password=password;Database=g0v0";

    #endregion

    #region JWT

    /// <summary>
    /// Gets or sets the JWT secret key used for signing and verifying tokens.
    /// </summary>
    public string JwtSecretKey { get; set; } = "your_jwt_secret_here";

    /// <summary>
    /// Gets or sets the JWT signing algorithm.
    /// </summary>
    public string JwtAlgorithm { get; set; } = "HS256";

    /// <summary>
    /// Gets or sets the JWT audience claim value.
    /// </summary>
    public string JwtAudience { get; set; } = "5";

    /// <summary>
    /// Gets or sets the JWT issuer claim value.
    /// </summary>
    public string? JwtIssuer { get; set; }

    #endregion

    #region OAuth

    /// <summary>
    /// Gets or sets the osu! client ID (lazer client).
    /// </summary>
    public int OsuClientId { get; set; } = 5;

    /// <summary>
    /// Gets or sets the osu! client secret.
    /// </summary>
    public string OsuClientSecret { get; set; } = "FGc9GAtyHzeQDshWP5Ah7dega8hJACAJpQtw6OXk";

    /// <summary>
    /// Gets or sets the osu! web app client ID.
    /// </summary>
    public int OsuWebClientId { get; set; } = 6;

    /// <summary>
    /// Gets or sets the osu! web app client secret.
    /// </summary>
    public string OsuWebClientSecret { get; set; } = "your_osu_web_client_secret_here";

    #endregion
}