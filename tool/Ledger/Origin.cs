using System.Net;
using System.Reflection;
using Rulpane.Engine;

namespace Rulpane.Gallery;

/// <summary>
/// The viewer's origin, as Rulpane's code reads it, served from disk: the bundled
/// <c>rulesets/</c>, <c>views/</c>, <c>themes/</c> and <c>panesets/</c> from the Rulpane checkout,
/// <c>registry/</c> from the copy of the Registry Rulpane's <c>tool/Mirror</c> wrote, and
/// <c>gallery/</c> from this repository's own ledger, as <c>tool/Mirror --gallery</c> would copy
/// it. So a sample naming <c>approval</c>, <c>Rulealize.RuleSet.Request@1.1.0</c> or
/// <c>octocat/kanban@1</c> is read from where the viewer would read it.
/// </summary>
public sealed class Origin(string viewer, string? registry, IReadOnlyList<Entry> ledger) : HttpMessageHandler
{
    public static HttpClient Client(string? registry, IReadOnlyList<Entry> ledger) => new(new Origin(
        Path.Combine(RulpaneRoot, "src", "Rulpane.Viewer", "wwwroot"), registry, ledger))
    {
        BaseAddress = new Uri("http://rulpane.invalid/"),
    };

    /// <summary>The names of the documents Rulpane bundles in <paramref name="folder"/>, such as <c>rulesets</c>: what a link names them by.</summary>
    public static IEnumerable<string> Bundled(string folder) =>
        Directory.EnumerateFiles(Path.Combine(RulpaneRoot, "src", "Rulpane.Viewer", "wwwroot", folder), "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f)!)
            .Order(StringComparer.Ordinal);

    /// <summary>The Rulpane checkout this tool was built against, whose bundled documents a sample may name.</summary>
    private static string RulpaneRoot =>
        typeof(Origin).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "RulpaneRoot").Value!;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string path = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath.TrimStart('/'));
        string? text = path.Split('/') switch
        {
            ["registry", .. var rest] => registry is null ? null : Read(Path.Combine([registry, .. rest])),
            [("rulesets" or "views" or "themes" or "panesets") and var folder, var name] => Read(Path.Combine(viewer, folder, name)),
            [Engine.Gallery.Folder, var owner, var name, var file] when file.EndsWith(".json", StringComparison.Ordinal) => Released($"{owner}/{name}", file[..^5]),
            _ => null,
        };

        return Task.FromResult(text is not null
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(text) }
            : new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
    }

    private static string? Read(string file) => File.Exists(file) ? File.ReadAllText(file) : null;

    // A withheld release is not there, as it would not be beside the viewer.
    private string? Released(string id, string version) =>
        ledger.FirstOrDefault(e => e.Id == id && e.Version.ToString() == version && e.Withheld is null) is Entry entry
            ? Ledger.Mirrored(entry, File.ReadAllText(Path.Combine(entry.Folder, Ledger.DocumentFile))).ToJsonString()
            : null;
}
