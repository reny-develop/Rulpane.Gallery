// Renders the Gallery as pages: where views, themes and panesets are found and submitted.
//
//   dotnet run --project tool/Site -- <folder> [--shots <folder>] [--rulpane <url>] [--repository <owner/name>]
//   dotnet run --project tool/Site -- --shots-to-take <file>
//   dotnet run --project tool/Site -- --releases-to <folder>
//
// It reads ledger/submitted.json and the copies under release/, so the pages are a view of
// those files rather than a second account of them, and the rule sets they open: those Rulpane
// bundles and those the Registry publishes, read where Rulpane reads them. Run from the top of
// the repository. It writes
//
//   <folder>/index.html                         every name, what it is, its newest release
//   <folder>/release/<owner>/<name>/index.html  one name: how to use it, every release, the document and its licence
//   <folder>/release/<owner>/<name>/<n>.json    one release that is not withheld, as Rulpane reads it when a link names it
//   <folder>/submit/index.html                  the form a submission is made from
//   <folder>/fonts/                             Rulpane's own fonts, copied from its checkout, so the pages look as it does
//   <folder>/llms.txt                           the same as text, for an AI: every name's newest release, where its
//                                               document is and where each one it names is
//
// --rulpane is where the viewer is, for the links that open a release; --repository is this
// repository on GitHub, where a submission is opened as an issue.
//
// --shots-to-take writes, instead, the pictures the pages would show: one line for the newest
// release of each name that a link opens, the picture's path and the fragment of that link,
// tab-separated, which is what tool/Shot/shoot.mjs takes. --shots is the folder it wrote them to;
// a release with a picture there is shown with it, and one without is shown without.
//
// --releases-to writes, instead, only the <n>.json of every release, under <folder>/release/: a
// Rulpane served with them beside it, and told so, opens what this ledger holds before it is
// published, which is how the pictures are taken.

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

if (positional.Count != (options.ContainsKey("shots-to-take") || options.ContainsKey("releases-to") ? 0 : 1))
{
    Console.Error.WriteLine("usage: Site <folder> [--shots <folder>] [--rulpane <url>] [--repository <owner/name>]");
    Console.Error.WriteLine("       Site --shots-to-take <file>");
    Console.Error.WriteLine("       Site --releases-to <folder>");
    return 2;
}

string output = positional.FirstOrDefault() ?? "";
string rulpane = options.GetValueOrDefault("rulpane", "https://reny-develop.github.io/Rulpane/").TrimEnd('/') + "/";
string repository = options.GetValueOrDefault("repository", "reny-develop/Rulpane.Gallery");
string builtAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

// The kinds, in the order the index shows them and its filter offers them.
string[] Shown = ["paneset", "view", "theme"];

var lenient = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
var relaxed = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
string? shots = options.GetValueOrDefault("shots");

if (options.TryGetValue("shots-to-take", out string? list))
{
    Entry[] newest = [.. Ledger.Read(Ledger.File).Where(e => e.Withheld is null).GroupBy(e => e.Id).Select(g => g.MaxBy(e => e.Version)!)];
    string[] lines = [.. newest.Select(e => (Release: e, Link: Open(e))).Where(s => s.Link is not null).Select(s => $"{Shot(s.Release)}\t{s.Link![rulpane.Length..]}\n")];
    File.WriteAllText(list, string.Concat(lines));
    Console.Error.WriteLine($"{lines.Length} picture(s) to take → {list}");
    return 0;
}

if (options.TryGetValue("releases-to", out string? beside))
{
    Console.Error.WriteLine($"{Serve(beside)} release(s) → {beside}");
    return 0;
}

if (Directory.Exists(output))
{
    Directory.Delete(output, recursive: true);
}

Directory.CreateDirectory(output);
Serve(output);

List<Entry> entries = Ledger.Read(Ledger.File);
List<RuleSet> ruleSets;
using (HttpClient origin = Origin.Client(entries))
{
    ruleSets = await RuleSet.FindAsync(origin, entries);
}

var names = entries.GroupBy(e => e.Id).OrderBy(g => g.Key, StringComparer.Ordinal).ToList();
var rows = new StringBuilder();

// Panesets first, which open as they are, then views, then themes, each by name.
foreach (var name in names.OrderBy(n => Array.IndexOf(Shown, n.First().Kind)).ThenBy(n => n.Key, StringComparer.Ordinal))
{
    Entry[] releases = [.. name.OrderBy(e => e.Version)];
    Entry? latest = releases.LastOrDefault(e => e.Withheld is null);
    string folder = Path.Combine(output, "release", name.Key);
    Directory.CreateDirectory(folder);
    File.WriteAllText(Path.Combine(folder, "index.html"), Page(name.Key, "../../../", NamePage(name.Key, releases, latest)));
    rows.Append(Card(latest ?? releases[^1], withheld: latest is null));
}

RuleSet[] filtered = [.. ruleSets.Where(r => r.Fits.Count + r.Panesets.Count + r.Samples.Count > 0)];
File.WriteAllText(Path.Combine(output, "index.html"), Page("Rulpane Gallery", "", $"""
    <section class="hero">
      <div>
        <h1>Screens for Rulpane, ready to open</h1>
        <p class="lead">Panesets, views and themes for <a href="{rulpane}">Rulpane</a> that anybody can publish. Open one as it is, copy its link, or take one close to what you want and change it.</p>
      </div>
      <p class="actions"><a class="button" href="{Addresses.Registry}">Find rules in the Registry{Icon.Line("out")}</a><a class="button primary" href="submit/">Submit yours</a></p>
    </section>
    <p class="notice">{Icon.Line("info", 16)}<span>Each release is recorded once a machine has read it and run it on the rules its author tried it with. <strong>Nobody reviews what it says</strong> — its author wrote every word. Rule sets are published to the <a href="{Addresses.Registry}">Rulealize Registry</a>, and named from here.</span></p>
    {(names.Count == 0 ? "<p class=\"muted\">Nothing has been submitted yet.</p>" : $"""
    <div class="filters" id="filters" hidden>
      <div class="row">
        <label class="search">{Icon.Line("search")}<input type="search" id="q" placeholder="Search by name or rules" aria-label="Search by name or rules" autocomplete="off"></label>
        <div class="segment" role="group" aria-label="Kind">
          <button type="button" data-kind="" aria-pressed="true">All <span class="count">{names.Count}</span></button>
          {string.Concat(Shown.Select(k => $"<button type=\"button\" data-kind=\"{k}\" aria-pressed=\"false\">{Plural(k)} <span class=\"count\">{names.Count(n => n.First().Kind == k)}</span></button>"))}
        </div>
      </div>
      <div class="row pills" role="group" aria-label="Rules">
        <span class="label">Rules</span>
        <button type="button" class="pill" data-rules="" aria-pressed="true">Any</button>
        {string.Concat(filtered.Select(r => $"<button type=\"button\" class=\"pill\" data-rules=\"{H(r.Id)}\" aria-pressed=\"false\">{H(r.Id)}</button>"))}
      </div>
    </div>
    <ul class="cards" id="cards">
    {rows}</ul>
    <p class="muted" id="nothing" hidden>Nothing here matches. Look for rules in the <a href="{Addresses.Registry}">Registry</a>, or <a href="submit/">submit a screen for them</a>.</p>
    <script>
    {Script.Filter}
    </script>
    """)}
    """));

WriteText();

Directory.CreateDirectory(Path.Combine(output, "submit"));
File.WriteAllText(Path.Combine(output, "submit", "index.html"), Page("Submit to the Gallery", "../", Submit.Page(repository)));
File.WriteAllText(Path.Combine(output, "style.css"), Style.Css);
File.WriteAllText(Path.Combine(output, "favicon.svg"), Icon.Svg);
File.WriteAllText(Path.Combine(output, ".nojekyll"), "");
Directory.CreateDirectory(Path.Combine(output, "fonts"));
foreach (string font in Directory.EnumerateFiles(Path.Combine(Origin.Served, "fonts")))
{
    File.Copy(font, Path.Combine(output, "fonts", Path.GetFileName(font)));
}

Console.Error.WriteLine($"{names.Count} name(s), {entries.Count} release(s), {ruleSets.Count} rule set(s) → {output}");
return 0;

// The Gallery as one page of text, for an AI asked to make a Rulpane screen: every name's newest
// release, where its document is, and where each document it names is, so that a whole working
// one is copied and changed rather than written from nothing. What a paneset, a view or a theme
// is, is Rulpane's to say, and an operation the Registry's: this page links both and says neither.
void WriteText()
{
    Entry[] offered = [.. names.Select(n => n.Where(e => e.Withheld is null).MaxBy(e => e.Version)).OfType<Entry>()];
    var text = new StringBuilder($"""
        # Rulpane Gallery

        > Views, themes and panesets for Rulpane that anybody can publish, each under a short name. This is the Gallery as text: every name's newest release, its document, and the documents it names, so that one close to what is asked for can be copied whole and changed.

        A release is recorded without anybody approving it, once a machine has read it: its words, its shape, and a run on the rules its author tried it with. Nobody reviews what it says; its author wrote every word of it. Built from the ledger at {builtAt}.

        ## How a link names a release

        As `<owner>/<name>@<version>`: a view in `v=`, a theme in `t=`, a paneset — rules, view, theme and where to start, as one document — in `p=`. The version counts that name's releases from 1, and a link never moves to a newer one. A paneset names a view or a theme the same way, in `v` or `t`.

            {rulpane}#p=<owner>/<name>@<version>

        How a paneset, a view and a theme are written, and how a link carries one, is Rulpane's to say: <{rulpane}llms.txt>. Rule sets are published to the Rulealize Registry, never here, and are looked for there, as are the operations they use: <https://reny-develop.github.io/Rulealize.Registry/llms.txt>. A screen for a rule set is found below by its id: a paneset names its rules in r, a view says which rule sets it fits as it is, and a view or a theme names the rules of the sample it was tried in.

        Each document below is the copy the Gallery keeps, as its author committed it, byte for byte. A withheld release is left out: Rulpane opens none.


        """);

    foreach (string kind in (string[])["paneset", "view", "theme"])
    {
        text.Append($"## {(kind == "paneset" ? "Panesets: rules, view and where to start, each opening as it is" : kind == "view" ? "Views" : "Themes")}\n\n");
        Entry[] these = [.. offered.Where(e => e.Kind == kind)];
        if (these.Length == 0)
        {
            text.Append("None yet.\n\n");
        }

        foreach (Entry release in these)
        {
            string document = Read(release, Ledger.DocumentFile)!;
            Paneset? paneset = kind == "paneset" ? Paneset.Parse(document) : null;
            text.Append($"### {release.Reference}\n\n");
            text.Append($"- {kind}, under {release.License}, from https://github.com/{release.Repository}\n");
            text.Append($"- document: {Raw(release, Ledger.DocumentFile)}\n");
            if (paneset is not null)
            {
                text.Append($"- r, the rules: {Named(paneset.Rules!, Source.Rules)}\n");
                text.Append(paneset.View is string v ? $"- v, the view: {Named(v, Source.View)}\n" : "");
                text.Append(paneset.Theme is string t ? $"- t, the theme: {Named(t, Source.Theme)}\n" : "");
                text.Append(paneset.State is null ? "" : "- s, where it starts: in the document\n");
                text.Append(paneset.Bind.Count == 0 ? "" : "- b, the rules' names for what the view reads: in the document\n");
            }
            else
            {
                string parts = string.Join(", ", Parts(kind, document));
                text.Append($"- {(kind == "view" ? "reads" : "looks")}: {(parts.Length == 0 ? "—" : parts)}\n");
            }

            if (kind == "view")
            {
                RuleSet[] fits = [.. ruleSets.Where(r => r.Fits.Contains(release))];
                text.Append(fits.Length == 0
                    ? "- fits as it is: no rule set here; rules that keep what it reads under other names are matched to it by a paneset's `b`\n"
                    : $"- fits as it is, with no `b`: {string.Join(", ", fits.Select(r => $"`{r.Tried}`"))}\n");
            }

            if (paneset is null && Read(release, Ledger.SampleFile) is string sample)
            {
                text.Append($"- its sample, the paneset it was tried in: {Raw(release, Ledger.SampleFile)}, whose r is {Named(Paneset.Parse(sample).Rules!, Source.Rules)}\n");
            }

            text.Append(Open(release) is string open ? $"- opens{(paneset is null ? " in its sample" : "")}: {open}\n\n" : "\n");
        }
    }

    File.WriteAllText(Path.Combine(output, "llms.txt"), text.ToString().ReplaceLineEndings("\n"));
}

// Each release that is not withheld, as Rulpane reads it from this site when a link names it. A
// withheld one is not served, so it stops opening as soon as it is withheld.
int Serve(string folder)
{
    Entry[] served = [.. Ledger.Read(Ledger.File).Where(e => e.Withheld is null)];
    foreach (Entry release in served)
    {
        string file = Path.Combine(folder, "release", release.Id, $"{release.Version}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, Ledger.Served(release, File.ReadAllText(Path.Combine(release.Folder, Ledger.DocumentFile))).ToJsonString());
    }

    return served.Length;
}

// Where a file of a release's copy is served as it is: this repository, on GitHub.
string Raw(Entry release, string file) => $"https://raw.githubusercontent.com/{repository}/main/{release.Folder}/{file}";

// The release a link's value names, when it is recorded here and not withheld.
Entry? Released(string value) => entries.FirstOrDefault(e => e.Reference == value && e.Withheld is null);

// What r, v or t names, and where its document is: bundled with Rulpane; published to the
// Registry, whose page for it says what it offers and which package holds it; recorded here;
// or written in place.
string Named(string value, Func<string, Source> read)
{
    Source source = read(value);
    return source switch
    {
        { IsPacked: true } => "written in place, in the document",
        { IsBundled: true } => $"`{value}`, bundled with Rulpane: {rulpane}{source.Folder}/{value}.json",
        { IsRegistered: true } => $"`{value}`, published to the Registry: {new Uri(Addresses.Registry, $"ruleset/{source.Reference.Id}.html")}",
        _ => Released(value) is Entry release ? $"`{value}`, recorded here: {Raw(release, Ledger.DocumentFile)}" : $"`{value}`, withheld or not recorded here",
    };
}

string NamePage(string id, Entry[] releases, Entry? latest)
{
    Entry shown = latest ?? releases[^1];
    string text = Read(shown, Ledger.DocumentFile)!;
    string? open = latest is null ? null : Open(latest);
    string? picture = latest is null ? null : Picture(latest);
    var page = new StringBuilder($"""
        <p class="crumb"><a href="../../../">Gallery</a> <span>/</span> <a href="../../../?kind={shown.Kind}">{Plural(shown.Kind)}</a> <span>/</span> {H(id)}</p>
        <div class="top">
          <figure class="shot">{(picture is null
              ? "<div class=\"none\">No picture of its screen</div>"
              : $"<a href=\"{H(open!)}\"><img src=\"../../../release/{picture}\" alt=\"The screen {H(latest!.Reference)} opens{(latest.Sample is null ? "" : " in its sample")}\" width=\"640\" height=\"400\"></a>\n<figcaption>{H(latest.Reference)}{(latest.Sample is null ? "" : $" in its sample, <span class=\"mono\">{H(latest.Sample)}</span>")}, as Rulpane opens it</figcaption>")}</figure>
          <aside class="side">
            <div class="heading">{Kind(shown.Kind)}{(latest is null ? " <span class=\"kind withheld\">withheld</span>" : "")}
              <h1>{H(id)}</h1>
              <p class="mono muted">@{shown.Version}</p>
            </div>
            {(open is null ? "" : $"""
            <a class="button primary wide" href="{H(open)}">Open in Rulpane{Icon.Line("arrow")}</a>
            <div class="link"><span class="label">Link</span><div class="field"><input readonly value="{H(open)}" aria-label="Link"><button type="button" data-copy="{H(open)}">{Icon.Line("copy")}<span>Copy</span></button></div></div>
            """)}
            <dl class="facts">
              {Facts(shown)}
              <div><dt>Author</dt><dd><a href="https://github.com/{shown.Owner}">{H(shown.Owner)}</a></dd></div>
              <div><dt>Read from</dt><dd><a class="mono" href="https://github.com/{shown.Repository}/blob/{shown.Commit}/{shown.Path}">{H(shown.Repository)} @ {shown.Commit[..7]}</a></dd></div>
              <div><dt>Licence</dt><dd class="mono">{H(shown.License)}</dd></div>
            </dl>
            <p class="notice">Its author wrote every word of it; nobody reviewed it.</p>
          </aside>
        </div>

        """);

    if (latest is null)
    {
        page.Append("<p class=\"notice danger\">Every release of it is withheld, and Rulpane opens none of them.</p>\n");
    }
    else
    {
        page.Append(Use(latest, text));
    }

    if (shown.Kind == "view" && latest is not null)
    {
        RuleSet[] fits = [.. ruleSets.Where(r => r.Fits.Contains(latest))];
        page.Append($"""
            <h2>Fits as it is</h2>
            <p>{(fits.Length == 0
                ? "No rule set here has every name it reads. Rules that keep them under other names are matched to it by a paneset's <span class=\"mono\">b</span>."
                : $"The rule sets that have every name it reads, which it opens with no <span class=\"mono\">b</span>: {string.Join(", ", fits.Select(r => Rules(r.Tried ?? r.Names[^1])))}.")}</p>

            """);
    }

    page.Append("<h2>Releases</h2>\n<div class=\"table\"><table>\n<thead><tr><th>Release</th><th>Read from</th><th></th></tr></thead>\n<tbody>\n");
    foreach (Entry release in releases.Reverse())
    {
        string? opens = release.Withheld is null ? Open(release) : null;
        page.Append($"""
            <tr>
              <td class="mono">{H(release.Reference)}</td>
              <td><a class="mono" href="https://github.com/{release.Repository}/blob/{release.Commit}/{release.Path}">{H(release.Path)} @ {release.Commit[..7]}</a></td>
              <td class="end">{(release.Withheld is not null ? "<span class=\"kind withheld\">withheld</span>" : opens is null ? "" : $"<a class=\"button small\" href=\"{H(opens)}\">Open</a>")}</td>
            </tr>
            {(release.Withheld is null ? "" : $"<tr class=\"why\"><td colspan=\"3\">{H(release.Withheld)}</td></tr>")}

            """);
    }

    page.Append("</tbody>\n</table></div>\n");
    page.Append($"""
        <h2>The document</h2>
        <p class="muted">As its author committed it.</p>
        {Code(Indent(text))}
        {(Read(shown, Ledger.SampleFile) is string sample ? $"<h2>Its sample</h2>\n<p>The paneset <span class=\"mono\">{H(shown.Sample!)}</span>, which it was tried in.</p>\n{Code(Indent(sample))}" : "")}
        <details>
          <summary>Licence ({H(shown.License)})</summary>
          <pre>{H(Read(shown, Ledger.LicenseFile) ?? "")}</pre>
        </details>

        """);
    return page.ToString();
}

// What a release is made of, beside its picture: a paneset's rules, view and theme, and the rules
// a view or a theme was tried with, the rules linked to the Registry's page for them and the rest
// to its page here, when they have one.
string Facts(Entry release)
{
    if (release.Kind != "paneset")
    {
        return Read(release, Ledger.SampleFile) is string sample
            ? $"<div><dt>Tried with</dt><dd>{Rules(Paneset.Parse(sample).Rules)}</dd></div>"
            : "";
    }

    Paneset paneset = Paneset.Parse(Read(release, Ledger.DocumentFile)!);
    return $"<div><dt>Rules</dt><dd>{Rules(paneset.Rules)}</dd></div><div><dt>View</dt><dd>{Part(paneset.View)}</dd></div><div><dt>Theme</dt><dd>{(paneset.Theme is null ? "Rulpane's own" : Part(paneset.Theme))}</dd></div>";

    string Part(string? value) => value switch
    {
        null => "written in place",
        _ when Released(value) is Entry named => $"<a class=\"mono\" href=\"../../../release/{named.Id}/\">{H(value)}</a>",
        _ => $"<span class=\"mono\">{H(value)}</span>",
    };
}

// A rule set, by the name r gives it: linked to its page in the Registry when it is published there.
string Rules(string? value) => value is null ? "written in place" : Source.Rules(value) switch
{
    { IsPacked: true } => "written in place",
    { IsRegistered: true } source => $"<a class=\"mono\" href=\"{H(new Uri(Addresses.Registry, $"ruleset/{source.Reference.Id}.html").ToString())}\">{H(value)}</a>",
    _ => $"<span class=\"mono\">{H(value)}</span>",
};

// How a link takes the newest release, by what it is.
string Use(Entry latest, string text) => latest.Kind switch
{
    "paneset" => $"""
        <h2>Use it</h2>
        <p>A link names it in <span class="mono">p=</span>, and may put any part of its own over it — here, another theme:</p>
        {Code($"{rulpane}#p={latest.Reference}&t=paper")}

        """,
    "theme" => $"""
        <h2>Use it</h2>
        <p>A link or a paneset names it in <span class="mono">t</span>, with any rules and view:</p>
        {Code($"{rulpane}#r=<rules>&v=<view>&t={latest.Reference}")}

        """,
    _ => $"""
        <h2>Reads</h2>
        {Reads(text)}
        <h2>Use it</h2>
        <p>A link or a paneset names it in <span class="mono">v</span>. Rules that keep what it reads under other names are matched to it by <span class="mono">b</span>:</p>
        {Code($$"""{ "$schema": "rulpane/paneset/v1", "r": "<rules>", "v": "{{latest.Reference}}", "b": { … } }""")}

        """,
};

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
        : $"<div class=\"table\"><table>\n<thead><tr><th></th><th>Name</th><th>Type, or the parameters it uses</th></tr></thead>\n<tbody>\n{rows}</tbody>\n</table></div>";
}

// What a release is made of, as words: a view's expects, a theme's look, a paneset's parts. Each is a
// word in llms.txt, and a theme's are the line under its title on its card.
string[] Parts(string kind, string text)
{
    JsonNode? root = Parse(text);
    return kind switch
    {
        "view" when root?["expects"] is JsonNode expects => [
            .. (expects["projections"] as JsonObject ?? []).Select(p => $"{p.Key}: {p.Value}"),
            .. (expects["inputs"] as JsonObject ?? []).Select(i => $"{i.Key}({string.Join(", ", i.Value?.AsArray().Select(a => a?.ToString()) ?? [])})")],
        "view" => [root?["ruleSet"]?.ToString() is string ruleSet ? $"for {ruleSet}" : "reads its own names"],
        "theme" => [.. new[] { "radius", "font", "density" }.Select(k => root?[k]?.ToString()).OfType<string>()],
        _ => [.. new[] { "r", "v", "t" }.Where(k => root?[k] is not null).Select(k => root![k] is JsonValue v ? $"{k}={v}" : $"{k}: written in place")],
    };
}

// Where a release's picture goes, under release/ in the pages and under the folder of --shots.
static string Shot(Entry release) => $"{release.Id}/{release.Version}.webp";

// The picture of a release, copied into the pages, when tool/Shot took one: its path under release/.
string? Picture(Entry release)
{
    string? taken = shots is null ? null : Path.Combine(shots, Shot(release));
    if (taken is null || !File.Exists(taken))
    {
        return null;
    }

    string copy = Path.Combine(output, "release", Shot(release));
    Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
    File.Copy(taken, copy, overwrite: true);
    return Shot(release);
}

string? Read(Entry release, string file)
{
    string path = Path.Combine(release.Folder, file);
    return File.Exists(path) ? File.ReadAllText(path) : null;
}

JsonNode? Parse(string text) => JsonNode.Parse(text, documentOptions: lenient);

string Indent(string text) => Parse(text)?.ToJsonString(relaxed) ?? text;

// The rule sets a release opens: those a view fits, a paneset names, or its sample was tried with.
RuleSet[] RulesFor(Entry release) =>
    [.. ruleSets.Where(r => r.Fits.Contains(release) || r.Panesets.Any(p => p.Release == release) || r.Samples.Any(s => s.Release == release))];

// One release as a card: its picture, its name, what it opens, and its page. Only a paneset opens
// from the card: it is a screen as it is, where a view or a theme is one only in its sample, which
// its page says. The index filters the cards by the data- attributes.
string Card(Entry release, bool withheld = false)
{
    string? open = release.Kind == "paneset" && !withheld ? Open(release) : null;
    string? picture = withheld ? null : Picture(release);
    RuleSet[] rules = RulesFor(release);
    string text = Read(release, Ledger.DocumentFile)!;
    string about = release.Kind == "theme" ? string.Join(" · ", Parts("theme", text)) : string.Join(", ", rules.Select(r => r.Id));
    string page = $"release/{release.Id}/";
    return $"""
        <li class="card" data-kind="{(withheld ? "withheld" : release.Kind)}" data-rules="{H(string.Join(" ", rules.Select(r => r.Id)))}" data-text="{H($"{release.Id} {about}".ToLowerInvariant())}">
          <a class="shot" href="{page}" tabindex="-1" aria-hidden="true">{(picture is null ? "<span class=\"none\">No picture</span>" : $"<img src=\"release/{picture}\" alt=\"\" width=\"640\" height=\"400\" loading=\"lazy\">")}</a>
          <div class="about">
            <a class="title" href="{page}">{H(release.Id)}</a>
            <span class="mono muted">@{release.Version}</span>
            <p class="meta">{Kind(release.Kind)}{(withheld ? "<span class=\"kind withheld\">withheld</span>" : "")}{(about.Length == 0 ? "" : $"<span>{H(about)}</span><span class=\"muted\">·</span>")}<span class="muted">{H(release.License)}</span></p>
            <p class="actions">{(open is null ? "" : $"<a class=\"button small primary\" href=\"{H(open)}\">Open</a>")}<a class="button small" href="{page}">Details</a></p>
          </div>
        </li>

        """;
}

// A block of code with a button that copies it.
static string Code(string text) =>
    $"<div class=\"code\"><button type=\"button\" data-copy=\"{H(text)}\">{Icon.Line("copy", 12)}<span>Copy</span></button><pre>{H(text)}</pre></div>";

static string Kind(string kind) => $"<span class=\"kind {kind}\">{kind}</span>";

static string Plural(string kind) => $"{char.ToUpperInvariant(kind[0])}{kind[1..]}s";

string Page(string title, string root, string body) => $"""
    <!doctype html>
    <html lang="en">
    <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>{H(title)}</title>
    <link rel="icon" href="{root}favicon.svg" type="image/svg+xml">
    <link rel="stylesheet" href="{root}style.css">
    </head>
    <body>
    <header class="bar"><a class="brand" href="{root}">{Icon.Mark}Rulpane Gallery</a><span class="spacer"></span><nav><a class="button ghost small" href="{root}">Browse</a><a class="button ghost small" href="{root}submit/">Submit</a><a class="button ghost small" href="{Addresses.Registry}">Rule sets{Icon.Line("out")}</a><a class="button ghost small" href="{H(rulpane)}">Rulpane{Icon.Line("out")}</a></nav></header>
    <main>
    {body}
    </main>
    <footer>Built from the <a href="https://github.com/{H(repository)}/blob/main/ledger/submitted.json">ledger</a> at {builtAt}. Each release is its author's, under the licence beside it; the ledger is CC0-1.0. <a href="{root}llms.txt">As text, for an AI</a>.</footer>
    <script>
    {Script.Copy}
    </script>
    </body>
    </html>

    """;

static string H(string text) => WebUtility.HtmlEncode(text);
