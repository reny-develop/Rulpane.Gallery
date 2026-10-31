using System.Net;
using System.Reflection;
using Rulpane.Engine;

namespace Rulpane.Gallery;

/// <summary>
/// What Rulpane's code reads, with this repository's own releases served from disk: the bundled
/// <c>rulesets/</c>, <c>views/</c>, <c>themes/</c> and <c>panesets/</c> from the Rulpane checkout,
/// and the Gallery's <c>release/</c> from this repository's own ledger, as the Gallery's site
/// serves it. The Registry and nuget.org are read where they are, as the viewer reads them. So a
/// sample naming <c>approval</c>, <c>Rulealize.RuleSet.Request@1.1.0</c> or
/// <c>octocat/kanban@1</c> is read from where the viewer would read it, with a release not
/// published yet already there.
/// </summary>
public sealed class Origin(string viewer, IReadOnlyList<Entry> ledger) : DelegatingHandler(new HttpClientHandler())
{
    private static readonly Uri Here = new("http://gallery.invalid/");

    public static HttpClient Client(IReadOnlyList<Entry> ledger)
    {
        Addresses.Gallery = Here;
        return new(new Origin(Path.Combine(RulpaneRoot, "src", "Rulpane.Viewer", "wwwroot"), ledger))
        {
            BaseAddress = new Uri("http://rulpane.invalid/"),
        };
    }

    /// <summary>The names of the documents Rulpane bundles in <paramref name="folder"/>, such as <c>rulesets</c>: what a link names them by.</summary>
    public static IEnumerable<string> Bundled(string folder) =>
        Directory.EnumerateFiles(Path.Combine(RulpaneRoot, "src", "Rulpane.Viewer", "wwwroot", folder), "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f)!)
            .Order(StringComparer.Ordinal);

    /// <summary>Where the Rulpane checkout keeps what the viewer serves: its bundled documents, and the fonts the Gallery's pages share with it.</summary>
    public static string Served => Path.Combine(RulpaneRoot, "src", "Rulpane.Viewer", "wwwroot");

    /// <summary>The Rulpane checkout this tool was built against, whose bundled documents a sample may name.</summary>
    private static string RulpaneRoot =>
        typeof(Origin).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "RulpaneRoot").Value!;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Uri address = request.RequestUri!;
        string[] path = Uri.UnescapeDataString(address.AbsolutePath.TrimStart('/')).Split('/');
        string? text;
        if (address.Host == Here.Host)
        {
            text = path is ["release", var owner, var name, var file] && file.EndsWith(".json", StringComparison.Ordinal)
                ? Released($"{owner}/{name}", file[..^5])
                : null;
        }
        else if (address.Host == "rulpane.invalid")
        {
            text = path is [("rulesets" or "views" or "themes" or "panesets") and var folder, var name]
                ? Read(Path.Combine(viewer, folder, name))
                : null;
        }
        else
        {
            return base.SendAsync(request, cancellationToken);
        }

        return Task.FromResult(text is not null
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(text) }
            : new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
    }

    private static string? Read(string file) => File.Exists(file) ? File.ReadAllText(file) : null;

    // A withheld release is not there, as the Gallery's site does not serve it.
    private string? Released(string id, string version) =>
        ledger.FirstOrDefault(e => e.Id == id && e.Version.ToString() == version && e.Withheld is null) is Entry entry
            ? Ledger.Served(entry, File.ReadAllText(Path.Combine(entry.Folder, Ledger.DocumentFile))).ToJsonString()
            : null;
}
