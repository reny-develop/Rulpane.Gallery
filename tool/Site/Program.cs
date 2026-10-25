// Renders the Gallery as pages: where views, themes and panesets are found and submitted.
//
//   dotnet run --project tool/Site -- <folder> [--rulpane <url>] [--repository <owner/name>]
//
// It reads ledger/submitted.json and the copies under release/, and nothing else, so the pages
// are a view of those files rather than a second account of them. Rulpane copies releases out
// of this repository, never out of these pages. Run from the top of the repository. It writes
//
//   <folder>/index.html                         every name, what it is, its newest release
//   <folder>/release/<owner>/<name>/index.html  one name: how to use it, every release, the document and its licence
//   <folder>/submit/index.html                  the form a submission is made from
//
// --rulpane is where the viewer is, for the links that open a release; --repository is this
// repository on GitHub, where a submission is opened as an issue.

using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Rulpane.Engine;
using Rulpane.Gallery;

var positional = new List<string>();
var options = new Dictionary<string, string>(StringComparer.Ordinal);
for (int i = 0; i < args.Length; i++)
{
    if (args[i].StartsWith("--", StringComparison.Ordinal) && i + 1 < args.Length)
    {
        options[args[i][2..]] = args[++i];
    }
    else
    {
        positional.Add(args[i]);
    }
}

if (positional.Count != 1)
{
    Console.Error.WriteLine("usage: Site <folder> [--rulpane <url>] [--repository <owner/name>]");
    return 2;
}

string output = positional[0];
string rulpane = options.GetValueOrDefault("rulpane", "https://reny-develop.github.io/Rulpane/").TrimEnd('/') + "/";
string repository = options.GetValueOrDefault("repository", "reny-develop/Rulpane.Gallery");
string builtAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

var lenient = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
var relaxed = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

if (Directory.Exists(output))
{
    Directory.Delete(output, recursive: true);
}

Directory.CreateDirectory(output);

List<Entry> entries = Ledger.Read(Ledger.File);
var names = entries.GroupBy(e => e.Id).OrderBy(g => g.Key, StringComparer.Ordinal).ToList();
var rows = new StringBuilder();

foreach (var name in names)
{
    Entry[] releases = [.. name.OrderBy(e => e.Version)];
    Entry? latest = releases.LastOrDefault(e => e.Withheld is null);
    string folder = Path.Combine(output, "release", name.Key);
    Directory.CreateDirectory(folder);
    File.WriteAllText(Path.Combine(folder, "index.html"), Page(name.Key, "../../../", NamePage(name.Key, releases, latest)));

    string? text = latest is null ? null : Read(latest, Ledger.DocumentFile);
    rows.Append($"""
        <tr>
          <td><a class="mono" href="release/{name.Key}/">{H(name.Key)}</a>{(latest is null ? " <span class=\"chip danger\">withheld</span>" : "")}</td>
          <td><span class="chip">{H(releases[0].Kind)}</span></td>
          <td>{H(text is null ? "" : Parse(text)?["title"]?.GetValue<string>() ?? "")}</td>
          <td>{(text is null ? "—" : What(releases[0].Kind, text))}</td>
          <td class="mono">{(latest is null ? "—" : latest.Version.ToString())}</td>
          <td class="mono">{H((latest ?? releases[^1]).License)}</td>
        </tr>

        """);
}

File.WriteAllText(Path.Combine(output, "index.html"), Page("Rulpane Gallery", "", $"""
    <h1>Rulpane Gallery</h1>
    <p class="lead">Views, themes and panesets for <a href="{rulpane}">Rulpane</a> that anybody can publish. A link names one as <span class="mono">&lt;owner&gt;/&lt;name&gt;@&lt;version&gt;</span>: a view in <span class="mono">v=</span>, a theme in <span class="mono">t=</span>, a paneset — rules, view, theme and where to start, as one document — in <span class="mono">p=</span>.</p>
    <p>A release is recorded without anybody approving it, once a machine has read it: its words, its shape, and a run on the rules its author tried it with. <strong>Nobody reviews what it says.</strong> Rule sets are published to the <a href="https://github.com/reny-develop/Rulealize.Registry">Rulealize Registry</a>, and named from here. <a href="submit/">Submit one</a>.</p>
    {(names.Count == 0 ? "<p class=\"muted\">Nothing has been submitted yet.</p>" : $"""
    <table>
      <thead><tr><th>Name</th><th>Kind</th><th>Title</th><th>What it is</th><th>Release</th><th>Licence</th></tr></thead>
      <tbody>
    {rows}  </tbody>
    </table>
    """)}
    """));

Directory.CreateDirectory(Path.Combine(output, "submit"));
File.WriteAllText(Path.Combine(output, "submit", "index.html"), Page("Submit to the Gallery", "../", Submit.Page(repository)));
File.WriteAllText(Path.Combine(output, "style.css"), Style.Css);
File.WriteAllText(Path.Combine(output, ".nojekyll"), "");

Console.Error.WriteLine($"{names.Count} name(s), {entries.Count} release(s) → {output}");
return 0;

string NamePage(string id, Entry[] releases, Entry? latest)
{
    Entry shown = latest ?? releases[^1];
    string text = Read(shown, Ledger.DocumentFile)!;
    string? title = Parse(text)?["title"]?.GetValue<string>();
    var page = new StringBuilder($"""
        <p class="crumb"><a href="../../../">Gallery</a></p>
        <h1 class="mono">{H(id)}</h1>
        <p><span class="chip">{H(shown.Kind)}</span>{(title is null ? "" : $" {H(title)}")}</p>
        <p>Published by the GitHub account <a href="https://github.com/{shown.Owner}">{H(shown.Owner)}</a> from <a href="https://github.com/{shown.Repository}">{H(shown.Repository)}</a>, under <span class="mono">{H(shown.License)}</span>. Its author wrote every word of it; nobody reviewed it.</p>

        """);

    if (latest is null)
    {
        page.Append("<p class=\"notice danger\">Every release of it is withheld, and Rulpane opens none of them.</p>\n");
    }
    else
    {
        page.Append(Use(latest, text));
    }

    page.Append("<h2>Releases</h2>\n<table>\n<thead><tr><th>Release</th><th>Read from</th><th></th></tr></thead>\n<tbody>\n");
    foreach (Entry release in releases.Reverse())
    {
        string? open = release.Withheld is null ? Open(release) : null;
        page.Append($"""
            <tr>
              <td class="mono">{H(release.Reference)}</td>
              <td><a class="mono" href="https://github.com/{release.Repository}/blob/{release.Commit}/{release.Path}">{H(release.Path)} @ {release.Commit[..7]}</a></td>
              <td>{(release.Withheld is not null ? $"<span class=\"chip danger\">withheld</span> {H(release.Withheld)}" : open is null ? "" : $"<a href=\"{H(open)}\">Open it</a>")}</td>
            </tr>

            """);
    }

    page.Append("</tbody>\n</table>\n");
    page.Append($"""
        <h2>The document</h2>
        <pre>{H(Indent(text))}</pre>
        {(Read(shown, Ledger.SampleFile) is string sample ? $"<h2>Its sample</h2>\n<p>The paneset <span class=\"mono\">{H(shown.Sample!)}</span>, which it was tried in.</p>\n<pre>{H(Indent(sample))}</pre>" : "")}
        <details>
          <summary>Licence ({H(shown.License)})</summary>
          <pre>{H(Read(shown, Ledger.LicenseFile) ?? "")}</pre>
        </details>

        """);
    return page.ToString();
}

// How a link takes the newest release, by what it is.
string Use(Entry latest, string text)
{
    string? open = Open(latest);
    string button = open is null ? "" : $"<p><a class=\"button\" href=\"{H(open)}\">Open it</a>{(latest.Sample is null ? "" : $" <span class=\"muted\">— in its sample, {H(latest.Sample)}</span>")}</p>";
    return latest.Kind switch
    {
        "paneset" => $"""
            <h2>Use it</h2>
            <p>A link names it in <span class="mono">p=</span>, and may put any part of its own over it — here, another theme:</p>
            <pre>{H(rulpane)}#p={H(latest.Reference)}&amp;t=paper</pre>
            {button}

            """,
        "theme" => $"""
            <h2>Use it</h2>
            <p>A link or a paneset names it in <span class="mono">t</span>, with any rules and view:</p>
            <pre>{H(rulpane)}#r=&lt;rules&gt;&amp;v=&lt;view&gt;&amp;t={H(latest.Reference)}</pre>
            {button}

            """,
        _ => $"""
            <h2>Reads</h2>
            {Reads(text)}
            <h2>Use it</h2>
            <p>A link or a paneset names it in <span class="mono">v</span>. Rules that keep what it reads under other names are matched to it by <span class="mono">b</span>:</p>
            <pre>{H($$"""{ "$schema": "rulpane/paneset/v1", "r": "<rules>", "v": "{{latest.Reference}}", "b": { … } }""")}</pre>
            {button}

            """,
    };
}

// The release opened in Rulpane: a paneset by its name, and a view or a theme by name in the
// sample it was tried in, the sample's other parts given as a link gives them.
string? Open(Entry release)
{
    if (release.Kind == "paneset")
    {
        return rulpane + Fragment.Build(("p", release.Reference));
    }

    if (Read(release, Ledger.SampleFile) is not string sample)
    {
        return null;
    }

    Paneset paneset = Paneset.Parse(sample);
    return rulpane + Fragment.Build(
        ("r", paneset.Rules),
        ("v", release.Kind == "view" ? release.Reference : paneset.View),
        ("t", release.Kind == "theme" ? release.Reference : paneset.Theme),
        ("b", paneset.Bind.Count == 0 ? null : Paneset.PackBind(paneset.Bind)),
        ("s", paneset.State));
}

// What a view reads: what its expects says, or that it says nothing and reads its bindings' names.
string Reads(string text)
{
    JsonNode? expects = Parse(text)?["expects"];
    if (expects is null)
    {
        return "<p>It does not say: it reads the names its bindings give, so it fits rules that have those names.</p>";
    }

    var rows = new StringBuilder();
    foreach (var (name, type) in expects["projections"] as JsonObject ?? [])
    {
        rows.Append($"<tr><td>projection</td><td class=\"mono\">{H(name)}</td><td class=\"mono\">{H(type?.ToString() ?? "")}</td></tr>\n");
    }

    foreach (var (name, parameters) in expects["inputs"] as JsonObject ?? [])
    {
        rows.Append($"<tr><td>input</td><td class=\"mono\">{H(name)}</td><td class=\"mono\">{H(string.Join(", ", parameters?.AsArray().Select(p => p?.ToString()) ?? []))}</td></tr>\n");
    }

    return rows.Length == 0
        ? "<p>Nothing: it reads no projection and names no input.</p>"
        : $"<table>\n<thead><tr><th></th><th>Name</th><th>Type, or the parameters it uses</th></tr></thead>\n<tbody>\n{rows}</tbody>\n</table>";
}

// One line on what it is: what a view expects, a theme's look, a paneset's parts.
string What(string kind, string text)
{
    JsonNode? root = Parse(text);
    string[] all = kind switch
    {
        "view" when root?["expects"] is JsonNode expects => [
            .. (expects["projections"] as JsonObject ?? []).Select(p => $"{p.Key}: {p.Value}"),
            .. (expects["inputs"] as JsonObject ?? []).Select(i => $"{i.Key}({string.Join(", ", i.Value?.AsArray().Select(a => a?.ToString()) ?? [])})")],
        "view" => [root?["ruleSet"]?.ToString() is string ruleSet ? $"for {ruleSet}" : "reads its own names"],
        "theme" => [.. new[] { "radius", "font", "density" }.Select(k => root?[k]?.ToString()).OfType<string>()],
        _ => [.. new[] { "r", "v", "t" }.Where(k => root?[k] is not null).Select(k => root![k] is JsonValue v ? $"{k}={v}" : $"{k}: written in place")],
    };
    return all.Length == 0 ? "—" : string.Join(" ", all.Select(a => $"<span class=\"chip mono\">{H(a)}</span>"));
}

string? Read(Entry release, string file)
{
    string path = Path.Combine(release.Folder, file);
    return File.Exists(path) ? File.ReadAllText(path) : null;
}

JsonNode? Parse(string text) => JsonNode.Parse(text, documentOptions: lenient);

string Indent(string text) => Parse(text)?.ToJsonString(relaxed) ?? text;

string Page(string title, string root, string body) => $"""
    <!doctype html>
    <html lang="en">
    <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>{H(title)}</title>
    <link rel="stylesheet" href="{root}style.css">
    </head>
    <body>
    <header class="bar"><a class="brand" href="{root}">Rulpane Gallery</a><span class="spacer"></span><a href="{root}submit/">Submit</a><a href="{H(rulpane)}">Rulpane</a></header>
    <main>
    {body}
    </main>
    <footer>Built from the <a href="https://github.com/{H(repository)}/blob/main/ledger/submitted.json">ledger</a> at {builtAt}. Each release is its author's, under the licence beside it; the ledger is CC0-1.0.</footer>
    </body>
    </html>

    """;

static string H(string text) => WebUtility.HtmlEncode(text);
