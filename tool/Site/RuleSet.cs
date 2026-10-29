using System.Text.Json;
using System.Text.Json.Nodes;
using Rulpane.Engine;

namespace Rulpane.Gallery;

/// <summary>
/// One rule set, under the id it declares, and what in the Gallery opens it. <see cref="Names"/>
/// is every way <c>r</c> names it: bundled with Rulpane, or a version of it in the Registry.
/// <see cref="Tried"/> is the newest of those, which every view was read against; it is
/// <see langword="null"/> when none could be read here. A paneset that carries its rules in
/// itself is under none of these: what it declares is its author's word, not a rule set's name.
/// </summary>
public sealed record RuleSet(string Id, string? Description, IReadOnlyList<string> Names, string? Tried)
{
    /// <summary>The views whose newest release fits it as it is: read with no <c>b</c>, and played from its start.</summary>
    public List<Entry> Fits { get; } = [];

    /// <summary>The panesets whose newest release names it, and the name its <c>r</c> gives.</summary>
    public List<(Entry Release, string Rules)> Panesets { get; } = [];

    /// <summary>The views and themes tried with it in their samples, and whether the sample's <c>b</c> matches it to the view.</summary>
    public List<(Entry Release, bool Bound)> Samples { get; } = [];

    /// <summary>
    /// Every rule set Rulpane bundles or the Registry publishes,
    /// and any other a release names, each with what opens it. A view fits a rule set when
    /// Rulpane's <see cref="View.Parse"/> reads it against the rules with no <c>b</c>, and the
    /// rules then play from their start, every position as the view expects — the trial a
    /// submission's sample is put through, with these rules instead.
    /// </summary>
    public static async Task<List<RuleSet>> FindAsync(HttpClient origin, IReadOnlyList<Entry> ledger)
    {
        // Each name r can give, with the version its document says and the id it declares.
        var named = new List<(string Id, string Name, string Version, string? Description, bool Registered)>();
        foreach (string name in Origin.Bundled("rulesets"))
        {
            JsonNode? document = JsonNode.Parse(await origin.GetStringAsync($"rulesets/{name}.json"), documentOptions: Lenient);
            if (document?["id"]?.GetValue<string>() is string id)
            {
                named.Add((id, name, document["version"]?.GetValue<string>() ?? "0.0.0", null, false));
            }
        }

        // The Registry's, as Rulpane reads them: the versions it has not withheld.
        var published = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonNode? summary in JsonNode.Parse(await origin.GetStringAsync(new Uri(Addresses.Registry, "index.json")))!["ruleSets"]!.AsArray())
        {
            if (await Registry.EntryAsync(origin, summary!["id"]!.GetValue<string>()) is Registered entry)
            {
                named.AddRange(entry.Versions.Select(version => (entry.Id, $"{entry.Id}@{version}", version, entry.Description, true)));
                published.UnionWith(entry.Versions.Select(version => $"{entry.Id}@{version}"));
            }
        }

        // The newest release of each name, which is the one a page offers.
        Entry[] latest = [.. ledger.Where(e => e.Withheld is null).GroupBy(e => e.Id).Select(g => g.MaxBy(e => e.Version)!)];

        // A Registry name it does not have (withheld, or never published) still gets a page, so a
        // paneset naming it is found; nothing could be tried against it.
        foreach (Entry release in latest)
        {
            if (RulesOf(release) is string rules && Source.Rules(rules).IsRegistered && !named.Any(n => n.Name == rules))
            {
                named.Add((rules[..rules.LastIndexOf('@')], rules, rules[(rules.LastIndexOf('@') + 1)..], null, true));
            }
        }

        var found = new List<RuleSet>();
        foreach (var group in named.GroupBy(n => n.Id, StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var newest = group.Where(n => !n.Registered || published.Contains(n.Name))
                .OrderBy(n => System.Version.TryParse(n.Version, out var v) ? v : new System.Version())
                .ThenBy(n => n.Registered)
                .LastOrDefault();
            var ruleSet = new RuleSet(
                group.FirstOrDefault(n => n.Registered).Id ?? group.Key,
                group.Select(n => n.Description).FirstOrDefault(d => d is not null),
                [.. group.Select(n => n.Name)],
                newest.Name);
            found.Add(ruleSet);

            if (ruleSet.Tried is null)
            {
                continue;
            }

            Trial.Compiled compiled;
            try
            {
                compiled = await Trial.CompileAsync(origin, ruleSet.Tried);
            }
            catch (Refusal refusal)
            {
                Console.Error.WriteLine($"{ruleSet.Tried} could not be compiled, and nothing is tried against it: {refusal.Message.ReplaceLineEndings(" ")}");
                continue;
            }

            foreach (Entry view in latest.Where(e => e.Kind == "view"))
            {
                try
                {
                    Trial.Play(compiled.Context, View.Parse(Read(view, Ledger.DocumentFile)!, compiled.Context, compiled.Document), null);
                    ruleSet.Fits.Add(view);
                }
                catch (Exception e) when (e is FormatException or Refusal or InvalidOperationException or JsonException)
                {
                    // It reads something these rules do not have, or would stop on them: it does not fit.
                }
            }
        }

        foreach (Entry release in latest)
        {
            if (RulesOf(release) is not string rules || Source.Rules(rules) is not { IsPacked: false } source)
            {
                continue;
            }

            string id = source.IsRegistered ? rules[..rules.LastIndexOf('@')] : named.FirstOrDefault(n => !n.Registered && n.Name == rules).Id;
            if (found.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase)) is not RuleSet ruleSet)
            {
                continue;
            }

            if (release.Kind == "paneset")
            {
                ruleSet.Panesets.Add((release, rules));
            }
            else
            {
                ruleSet.Samples.Add((release, Paneset.Parse(Read(release, Ledger.SampleFile)!).Bind.Count > 0));
            }
        }

        return found;
    }

    private static readonly JsonDocumentOptions Lenient = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    // What r says in a paneset, or in the sample a view or a theme was tried in.
    private static string? RulesOf(Entry release) =>
        (release.Kind == "paneset" ? Read(release, Ledger.DocumentFile) : Read(release, Ledger.SampleFile)) is string text
            ? Paneset.Parse(text).Rules
            : null;

    private static string? Read(Entry release, string file)
    {
        string path = Path.Combine(release.Folder, file);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
