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
    /// Gets MySQL database host.
    /// </summary>
    public string MySqlHost => "127.0.0.1";

    /// <summary>
    /// Gets MySQL database port.
    /// </summary>
    public string MySqlPort => "3306";

    /// <summary>
    /// Gets MySQL database.
    /// </summary>
    public string MySqlDatabase => "osu_api";

    /// <summary>
    /// Gets MySQL username to connect.
    /// </summary>
    public string MySqlUsername => "osu_api";

    /// <summary>
    /// Gets MySQL password to connect.
    /// </summary>
    public string MySqlPassword => "password";

    /// <summary>
    /// Gets MySQL connection string based on the provided configuration properties.
    /// </summary>
    [JsonIgnore]
    public string MySqlConnectionString =>
        $"Server={MySqlHost};Port={MySqlPort};Database={MySqlDatabase};Uid={MySqlUsername};Pwd={MySqlPassword};";

    #endregion
}