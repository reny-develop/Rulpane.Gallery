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
//   <folder>/rules/index.html                   every rule set, and how much opens it
//   <folder>/rules/<id>/index.html              one rule set: the views that fit it, its panesets, the samples tried with it
//   <folder>/submit/index.html                  the form a submission is made from
//   <folder>/llms.txt                           the same as text, for an AI: every name's newest release, where its
//                                               document is and where each one it names is, and every rule set
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

foreach (var name in names)
{
    Entry[] releases = [.. name.OrderBy(e => e.Version)];
    Entry? latest = releases.LastOrDefault(e => e.Withheld is null);
    string folder = Path.Combine(output, "release", name.Key);
    Directory.CreateDirectory(folder);
    File.WriteAllText(Path.Combine(folder, "index.html"), Page(name.Key, "../../../", NamePage(name.Key, releases, latest)));

    string? text = latest is null ? null : Read(latest, Ledger.DocumentFile);
    string? title = text is null ? null : Parse(text)?["title"]?.GetValue<string>();
    string? picture = latest is null ? null : Picture(latest);
    rows.Append($"""
        <li class="card">
          <a class="shot" href="release/{name.Key}/" tabindex="-1">{(picture is null ? $"<span class=\"none\">{H(releases[0].Kind)}</span>" : $"<img src=\"release/{picture}\" alt=\"\" width=\"640\" height=\"400\" loading=\"lazy\">")}</a>
          <div class="about">
            <a class="mono name" href="release/{name.Key}/">{H(name.Key)}</a>
            <p><span class="chip">{H(releases[0].Kind)}</span>{(latest is null ? "<span class=\"chip danger\">withheld</span>" : "")}{H(title ?? "")}</p>
            <p>{(text is null ? "" : What(releases[0].Kind, text))}</p>
            <p class="muted mono">{(latest is null ? "" : $"@{latest.Version} · ")}{H((latest ?? releases[^1]).License)}</p>
          </div>
        </li>

        """);
}

File.WriteAllText(Path.Combine(output, "index.html"), Page("Rulpane Gallery", "", $"""
    <h1>Rulpane Gallery</h1>
    <p class="lead">Views, themes and panesets for <a href="{rulpane}">Rulpane</a> that anybody can publish. A link names one as <span class="mono">&lt;owner&gt;/&lt;name&gt;@&lt;version&gt;</span>: a view in <span class="mono">v=</span>, a theme in <span class="mono">t=</span>, a paneset — rules, view, theme and where to start, as one document — in <span class="mono">p=</span>.</p>
    <p>A release is recorded without anybody approving it, once a machine has read it: its words, its shape, and a run on the rules its author tried it with. <strong>Nobody reviews what it says.</strong> Rule sets are published to the <a href="https://github.com/reny-develop/Rulealize.Registry">Rulealize Registry</a>, and named from here. <a href="submit/">Submit one</a>.</p>
    <p>Have rules, and want a screen for them? <a href="rules/">Look them up by rule set</a>: the views that fit each one as it is, and the panesets made for it.</p>
    {(names.Count == 0 ? "<p class=\"muted\">Nothing has been submitted yet.</p>" : $"""
    <ul class="cards">
    {rows}</ul>
    """)}
    """));

WriteRules();
WriteText();

Directory.CreateDirectory(Path.Combine(output, "submit"));
File.WriteAllText(Path.Combine(output, "submit", "index.html"), Page("Submit to the Gallery", "../", Submit.Page(repository)));
File.WriteAllText(Path.Combine(output, "style.css"), Style.Css);
File.WriteAllText(Path.Combine(output, ".nojekyll"), "");

Console.Error.WriteLine($"{names.Count} name(s), {entries.Count} release(s), {ruleSets.Count} rule set(s) → {output}");
return 0;

void WriteRules()
{
    var rows = new StringBuilder();
    foreach (RuleSet ruleSet in ruleSets)
    {
        string folder = Path.Combine(output, "rules", ruleSet.Id);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "index.html"), Page(ruleSet.Id, "../../", RulesPage(ruleSet)));
        rows.Append($"""
            <tr>
              <td><a class="mono" href="{H(ruleSet.Id)}/">{H(ruleSet.Id)}</a></td>
              <td>{H(ruleSet.Description ?? "")}</td>
              <td class="mono">{ruleSet.Fits.Count}</td>
              <td class="mono">{ruleSet.Panesets.Count}</td>
              <td class="mono">{ruleSet.Samples.Count}</td>
            </tr>

            """);
    }

    Directory.CreateDirectory(Path.Combine(output, "rules"));
    File.WriteAllText(Path.Combine(output, "rules", "index.html"), Page("By rule set — Rulpane Gallery", "../", $"""
        <p class="crumb"><a href="../">Gallery</a></p>
        <h1>By rule set</h1>
        <p class="lead">Every rule set Rulpane bundles or the Registry publishes, under the id it declares, and what here opens it.</p>
        <p>A view <strong>fits</strong> a rule set when Rulpane reads it against those rules with no <span class="mono">b</span> — everything it reads is a projection, a field or an input they have, by that name — and they then play from their start, every position as the view expects. A view that reads other names may still show them through a paneset's <span class="mono">b</span>: the samples tried with a rule set are where to find one.</p>
        <table>
          <thead><tr><th>Rule set</th><th>What it is</th><th>Views that fit</th><th>Panesets</th><th>Samples</th></tr></thead>
          <tbody>
        {rows}  </tbody>
        </table>
        """));
}

// The Gallery as one page of text, for an AI asked to make a Rulpane screen: every name's newest
// release, where its document is, and where each document it names is, so that a whole working
// one is copied and changed rather than written from nothing. What a paneset, a view or a theme
// is, is Rulpane's to say, and an operation the Registry's: this page links both and says neither.
void WriteText()
{
    Entry[] offered = [.. names.Select(n => n.Where(e => e.Withheld is null).MaxBy(e => e.Version)).OfType<Entry>()];
    var text = new StringBuilder($"""
        # Rulpane Gallery

        > Views, themes and panesets for Rulpane that anybody can publish, each under a short name, and the rule sets they open. This is the Gallery as text: every name's newest release, its document, and the documents it names, so that one close to what is asked for can be copied whole and changed.

        A release is recorded without anybody approving it, once a machine has read it: its words, its shape, and a run on the rules its author tried it with. Nobody reviews what it says; its author wrote every word of it. Built from the ledger at {builtAt}.

        ## How a link names a release

        As `<owner>/<name>@<version>`: a view in `v=`, a theme in `t=`, a paneset — rules, view, theme and where to start, as one document — in `p=`. The version counts that name's releases from 1, and a link never moves to a newer one. A paneset names a view or a theme the same way, in `v` or `t`.

            {rulpane}#p=<owner>/<name>@<version>

        How a paneset, a view and a theme are written, and how a link carries one, is Rulpane's to say: <{rulpane}llms.txt>. Rule sets are published to the Rulealize Registry, never here, and the operations they use are its to say: <https://reny-develop.github.io/Rulealize.Registry/llms.txt>.

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
            string? title = Parse(document)?["title"]?.GetValue<string>()
                ?? (paneset?.View is string view && Released(view) is Entry named ? Parse(Read(named, Ledger.DocumentFile)!)?["title"]?.GetValue<string>() : null);
            text.Append($"### {release.Reference}{(title is null ? "" : $" — {title}")}\n\n");
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

    text.Append("""
        ## By rule set

        Every rule set Rulpane bundles or the Registry publishes, under the id it declares, and what here opens it. A view fits a rule set when Rulpane reads it against those rules with no `b` and they then play from their start, every position as the view expects.


        """);
    foreach (RuleSet ruleSet in ruleSets)
    {
        text.Append($"### {ruleSet.Id}\n\n{(ruleSet.Description is null ? "" : $"{ruleSet.Description}\n\n")}");
        text.Append($"- r names it as {string.Join(", ", ruleSet.Names.Select(n => $"`{n}`"))}\n");
        text.Append($"- document: {Named(ruleSet.Tried ?? ruleSet.Names[^1], Source.Rules)}\n");
        List("views that fit it as they are", ruleSet.Fits.Select(v => $"{v.Reference}, opened by {rulpane}{Fragment.Build(("r", ruleSet.Tried), ("v", v.Reference))}"));
        List("panesets for it", ruleSet.Panesets.Select(p => p.Release.Reference));
        List("views and themes tried with it in their samples", ruleSet.Samples.Select(s => s.Bound ? $"{s.Release.Reference}, with `b`" : s.Release.Reference));
        text.Append('\n');
    }

    File.WriteAllText(Path.Combine(output, "llms.txt"), text.ToString().ReplaceLineEndings("\n"));

    void List(string heading, IEnumerable<string> items)
    {
        string[] all = [.. items];
        text.Append(all.Length switch
        {
            0 => "",
            1 => $"- {heading}: {all[0]}\n",
            _ => $"- {heading}:\n{string.Concat(all.Select(i => $"  - {i}\n"))}",
        });
    }
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

string RulesPage(RuleSet ruleSet)
{
    var page = new StringBuilder($"""
        <p class="crumb"><a href="../../">Gallery</a> / <a href="../">By rule set</a></p>
        <h1 class="mono">{H(ruleSet.Id)}</h1>
        {(ruleSet.Description is null ? "" : $"<p class=\"lead\">{H(ruleSet.Description)}</p>")}
        <p>A link names it in <span class="mono">r</span> as {string.Join(", ", ruleSet.Names.Select(n => $"<span class=\"mono\">{H(n)}</span>{(n.Contains('@') ? "" : " (bundled with Rulpane)")}"))}.{(ruleSet.Tried is null ? " There is no copy of it here, so no view was tried against it." : $" Views were tried against <span class=\"mono\">{H(ruleSet.Tried)}</span>.")}</p>

        <h2>Views that fit it as they are</h2>

        """);

    if (ruleSet.Fits.Count == 0)
    {
        page.Append("<p class=\"muted\">None yet.</p>\n");
    }
    else
    {
        page.Append("<table>\n<thead><tr><th>View</th><th>Title</th><th>What it reads</th><th></th></tr></thead>\n<tbody>\n");
        foreach (Entry view in ruleSet.Fits)
        {
            string text = Read(view, Ledger.DocumentFile)!;
            string open = rulpane + Fragment.Build(("r", ruleSet.Tried), ("v", view.Reference));
            page.Append($"""
                <tr>
                  <td><a class="mono" href="../../release/{view.Id}/">{H(view.Reference)}</a></td>
                  <td>{H(Parse(text)?["title"]?.GetValue<string>() ?? "")}</td>
                  <td>{What(view.Kind, text)}</td>
                  <td><a href="{H(open)}">Open it</a></td>
                </tr>

                """);
        }

        page.Append("</tbody>\n</table>\n");
    }

    page.Append("<h2>Panesets for it</h2>\n");
    if (ruleSet.Panesets.Count == 0)
    {
        page.Append("<p class=\"muted\">None yet.</p>\n");
    }
    else
    {
        page.Append("<table>\n<thead><tr><th>Paneset</th><th>Title</th><th>Its rules</th><th></th></tr></thead>\n<tbody>\n");
        foreach (var (paneset, rules) in ruleSet.Panesets)
        {
            page.Append($"""
                <tr>
                  <td><a class="mono" href="../../release/{paneset.Id}/">{H(paneset.Reference)}</a></td>
                  <td>{H(Parse(Read(paneset, Ledger.DocumentFile)!)?["title"]?.GetValue<string>() ?? "")}</td>
                  <td class="mono">{H(rules)}</td>
                  <td><a href="{H(Open(paneset)!)}">Open it</a></td>
                </tr>

                """);
        }

        page.Append("</tbody>\n</table>\n");
    }

    page.Append("<h2>Tried with it in their samples</h2>\n");
    if (ruleSet.Samples.Count == 0)
    {
        page.Append("<p class=\"muted\">None yet.</p>\n");
    }
    else
    {
        page.Append("<p>Views and themes whose authors tried them with these rules. A sample marked <span class=\"chip mono\">b</span> matches the rules' names to the view's, and the link that opens it carries that <span class=\"mono\">b</span>.</p>\n");
        page.Append("<table>\n<thead><tr><th>Release</th><th>Kind</th><th>Its sample</th><th></th></tr></thead>\n<tbody>\n");
        foreach (var (release, bound) in ruleSet.Samples)
        {
            string? open = Open(release);
            page.Append($"""
                <tr>
                  <td><a class="mono" href="../../release/{release.Id}/">{H(release.Reference)}</a></td>
                  <td><span class="chip">{H(release.Kind)}</span></td>
                  <td><span class="mono">{H(release.Sample!)}</span>{(bound ? " <span class=\"chip mono\">b</span>" : "")}</td>
                  <td>{(open is null ? "" : $"<a href=\"{H(open)}\">Open it</a>")}</td>
                </tr>

                """);
        }

        page.Append("</tbody>\n</table>\n");
    }

    return page.ToString();
}

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

    if (latest is not null && Picture(latest) is string picture && Open(latest) is string opened)
    {
        page.Append($"""
            <figure class="shot"><a href="{H(opened)}"><img src="../../../release/{picture}" alt="The screen {H(latest.Reference)} opens{(latest.Sample is null ? "" : " in its sample")}" width="640" height="400"></a>
            <figcaption>{H(latest.Reference)}{(latest.Sample is null ? "" : $" in its sample, {H(latest.Sample)}")}, as Rulpane opens it</figcaption></figure>

            """);
    }

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
                : $"The rule sets that have every name it reads, which it opens with no <span class=\"mono\">b</span>: {string.Join(", ", fits.Select(r => $"<a class=\"mono\" href=\"../../../rules/{H(r.Id)}/\">{H(r.Id)}</a>"))}.")}</p>

            """);
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
    string[] all = Parts(kind, text);
    return all.Length == 0 ? "—" : string.Join(" ", all.Select(a => $"<span class=\"chip mono\">{H(a)}</span>"));
}

// The same, as words: each of them is a chip on a page and a word in llms.txt.
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
    <header class="bar"><a class="brand" href="{root}">Rulpane Gallery</a><span class="spacer"></span><a href="{root}rules/">By rule set</a><a href="{root}submit/">Submit</a><a href="{H(rulpane)}">Rulpane</a></header>
    <main>
    {body}
    </main>
    <footer>Built from the <a href="https://github.com/{H(repository)}/blob/main/ledger/submitted.json">ledger</a> at {builtAt}. Each release is its author's, under the licence beside it; the ledger is CC0-1.0.</footer>
    </body>
    </html>

    """;

static string H(string text) => WebUtility.HtmlEncode(text);
