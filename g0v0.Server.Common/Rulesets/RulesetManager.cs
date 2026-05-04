// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using g0v0.Server.Common.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;

namespace g0v0.Server.Common.Rulesets;

/// <summary>
/// Manages the loading, registration, and retrieval of osu! rulesets.
/// Supports both official (built-in) rulesets and custom rulesets loaded from disk.
/// Rulesets are indexed by their short name and legacy ID for efficient lookup.
/// </summary>
public class RulesetManager
{
    private const string RulesetLibraryPrefix = "osu.Game.Rulesets";
    private const string RulesetPath = "rulesets";

    private readonly Dictionary<string, Ruleset> _rulesets = new(StringComparer.Ordinal);
    private readonly Dictionary<int, Ruleset> _rulesetsById = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="RulesetManager"/> class.
    /// </summary>
    /// <param name="rulesetPath">The file system path to the directory containing custom ruleset DLL files.</param>
    /// <param name="includeOfficial">Whether to load official osu! rulesets (Osu, Taiko, Catch, Mania). Defaults to <c>true</c>.</param>
    public RulesetManager(string rulesetPath, bool includeOfficial = true)
    {
        if (includeOfficial)
        {
            LoadOfficialRulesets();
        }

        LoadFromDisk(rulesetPath);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RulesetManager"/> class using an <see cref="IPathProvider"/>
    /// to resolve the rulesets directory path.
    /// </summary>
    /// <param name="pathProvider">The path provider used to resolve the base path. The rulesets directory is expected at <c>{basePath}/rulesets</c>.</param>
    /// <param name="includeOfficial">Whether to load official osu! rulesets (Osu, Taiko, Catch, Mania). Defaults to <c>true</c>.</param>
    public RulesetManager(IPathProvider pathProvider, bool includeOfficial = true)
        : this(
            Path.Combine(pathProvider.GetBasePath(), RulesetPath), includeOfficial)
    {
    }

    /// <summary>
    /// Retrieves a ruleset by its legacy numeric ID.
    /// </summary>
    /// <param name="rulesetId">The legacy ID of the ruleset to retrieve.</param>
    /// <returns>The <see cref="Ruleset"/> instance matching the given ID.</returns>
    /// <exception cref="ArgumentException">Thrown when no ruleset is registered with the specified <paramref name="rulesetId"/>.</exception>
    public Ruleset GetRuleset(int rulesetId)
    {
        return _rulesetsById.TryGetValue(rulesetId, out Ruleset? ruleset)
            ? ruleset
            : throw new ArgumentException("Invalid ruleset ID provided.", paramName: nameof(rulesetId));
    }

    /// <summary>
    /// Retrieves a ruleset by its short name (e.g., "osu", "taiko", "fruits", "mania").
    /// </summary>
    /// <param name="shortName">The short name of the ruleset to retrieve.</param>
    /// <returns>The <see cref="Ruleset"/> instance matching the given short name.</returns>
    /// <exception cref="ArgumentException">Thrown when no ruleset is registered with the specified <paramref name="shortName"/>.</exception>
    public Ruleset GetRuleset(string shortName)
    {
        return _rulesets.TryGetValue(shortName, out Ruleset? ruleset)
            ? ruleset
            : throw new ArgumentException("Invalid ruleset name provided.", paramName: nameof(shortName));
    }

    /// <summary>
    /// Returns all registered rulesets, ensuring no duplicates.
    /// </summary>
    /// <returns>A collection of unique <see cref="Ruleset"/> instances.</returns>
    public IEnumerable<Ruleset> GetAllRulesets()
    {
        return _rulesets.Values.Distinct();
    }

    /// <summary>
    /// Registers a ruleset by its short name and, if it implements <see cref="ILegacyRuleset"/>, by its legacy ID.
    /// Duplicate entries are silently skipped with a warning written to <see cref="Console.Error"/>.
    /// </summary>
    /// <param name="ruleset">The ruleset instance to register.</param>
    private void AddRuleset(Ruleset ruleset)
    {
        if (!_rulesets.TryAdd(ruleset.ShortName, ruleset))
        {
            Console.Error.WriteLine($"Ruleset with short name {ruleset.ShortName} already exists, skipping.");
            return;
        }

        if (ruleset is not ILegacyRuleset legacyRuleset)
        {
            return;
        }

        if (!_rulesetsById.TryAdd(legacyRuleset.LegacyID, ruleset))
        {
            Console.Error.WriteLine($"Ruleset with ID {legacyRuleset.LegacyID} already exists, skipping.");
        }
    }

    /// <summary>
    /// Loads the four official osu! rulesets (Osu, Taiko, Catch, Mania) and registers them.
    /// Also sets up the "catch" alias for the "fruits" ruleset.
    /// </summary>
    private void LoadOfficialRulesets()
    {
        foreach (Ruleset ruleset in
                 (List<Ruleset>)[new OsuRuleset(), new TaikoRuleset(), new CatchRuleset(), new ManiaRuleset()])
        {
            AddRuleset(ruleset);
        }

        _rulesets["catch"] = _rulesets["fruits"];
    }

    /// <summary>
    /// Scans the specified directory for custom ruleset DLLs matching the pattern <c>osu.Game.Rulesets.*.dll</c>,
    /// loads each one, and registers any concrete <see cref="Ruleset"/> subclass found within.
    /// </summary>
    /// <param name="rulesetPath">The directory path to scan for ruleset DLLs.</param>
    private void LoadFromDisk(string rulesetPath)
    {
        if (!Directory.Exists(rulesetPath))
        {
            return;
        }

        string[] rulesets = Directory.GetFiles(rulesetPath, $"{RulesetLibraryPrefix}.*.dll");

        foreach (string ruleset in rulesets.Where(f => !f.Contains(@"Tests")))
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(ruleset);
                Type? rulesetType = assembly.GetTypes()
                    .FirstOrDefault(t => t.IsSubclassOf(typeof(Ruleset)) && !t.IsAbstract);

                if (rulesetType == null)
                {
                    continue;
                }

                Ruleset instance = (Ruleset)Activator.CreateInstance(rulesetType)!;
                Console.Error.WriteLine($"Loading ruleset {ruleset}");
                AddRuleset(instance);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load ruleset from {ruleset}: {ex}");
            }
        }
    }
}