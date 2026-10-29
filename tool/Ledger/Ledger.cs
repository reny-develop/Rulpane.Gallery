using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Rulpane.Gallery;

/// <summary>
/// One line of the ledger: one release of one name. <see cref="Id"/> is <c>&lt;owner&gt;/&lt;name&gt;</c>,
/// the owner being the GitHub account that submitted it and owns <see cref="Repository"/>;
/// <see cref="Version"/> counts that name's releases from 1. A name is one <see cref="Kind"/> —
/// <c>view</c>, <c>theme</c> or <c>paneset</c> — for good. <see cref="Sample"/> is the path, in the
/// repository, of the paneset a view or a theme was tried in; a paneset is its own.
/// </summary>
public sealed record Entry(
    string Id,
    int Version,
    string Kind,
    string Repository,
    string Commit,
    string Path,
    string License,
    string Sha256,
    string? Sample,
    string? Withheld = null)
{
    /// <summary>What a link names it by: <c>octocat/kanban@1</c>.</summary>
    public string Reference => $"{Id}@{Version}";

    /// <summary>Where its copy is kept, relative to the repository.</summary>
    public string Folder => $"release/{Id}/{Version}";

    public string Owner => Id[..Id.IndexOf('/')];
}

/// <summary>
/// <c>ledger/submitted.json</c>: one line per release, in the order recorded. A person reviews
/// this file, so each entry is kept to one line; the releases themselves are beside it, under
/// <c>release/</c>.
/// </summary>
public static class Ledger
{
    public const string File = "ledger/submitted.json";

    public static readonly string[] Kinds = ["view", "theme", "paneset"];

    // The copy of one release, file by file, under its folder.
    public const string DocumentFile = "document.json";
    public const string LicenseFile = "LICENSE";
    public const string SampleFile = "sample.json";

    private static readonly JsonSerializerOptions Relaxed = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static List<Entry> Read(string path)
    {
        JsonNode root = JsonNode.Parse(System.IO.File.ReadAllText(path))
            ?? throw new InvalidDataException($"{path} is not a JSON document.");
        return [.. (root["releases"]?.AsArray() ?? []).Select(n => FromJson(n!))];
    }

    public static Entry FromJson(JsonNode n) => new(
        n["id"]!.GetValue<string>(),
        n["version"]!.GetValue<int>(),
        n["kind"]!.GetValue<string>(),
        n["repository"]!.GetValue<string>(),
        n["commit"]!.GetValue<string>(),
        n["path"]!.GetValue<string>(),
        n["license"]!.GetValue<string>(),
        n["sha256"]!.GetValue<string>(),
        n["sample"]?.GetValue<string>(),
        n["withheld"]?.GetValue<string>());

    public static void Write(string path, IEnumerable<Entry> entries)
    {
        var text = new StringBuilder("{\n  \"releases\": [\n");
        string[] lines = [.. entries.Select(e => "    " + Inline(ToJson(e)))];
        text.Append(string.Join(",\n", lines));
        text.Append(lines.Length > 0 ? "\n  ]\n}\n" : "  ]\n}\n");
        System.IO.File.WriteAllText(path, text.ToString());
    }

    public static JsonObject ToJson(Entry e)
    {
        var line = new JsonObject
        {
            ["id"] = e.Id,
            ["version"] = e.Version,
            ["kind"] = e.Kind,
            ["repository"] = e.Repository,
            ["commit"] = e.Commit,
            ["path"] = e.Path,
            ["license"] = e.License,
            ["sha256"] = e.Sha256,
        };
        if (e.Sample is not null)
        {
            line["sample"] = e.Sample;
        }

        if (e.Withheld is not null)
        {
            line["withheld"] = e.Withheld;
        }

        return line;
    }

    // { "a": 1, "b": "c" }: the shape a person reads a line of the ledger in.
    private static string Inline(JsonObject o) =>
        "{ " + string.Join(", ", o.Select(p => $"{JsonSerializer.Serialize(p.Key, Relaxed)}: {p.Value!.ToJsonString(Relaxed)}")) + " }";

    public static string Sha256(string path) =>
        Convert.ToHexStringLower(SHA256.HashData(System.IO.File.ReadAllBytes(path)));

    /// <summary>
    /// A release as the Gallery's site serves it at <c>release/&lt;owner&gt;/&lt;name&gt;/&lt;version&gt;.json</c>,
    /// where Rulpane reads it: the document as its author committed it, and where it was read from.
    /// </summary>
    public static JsonObject Served(Entry e, string text) => new()
    {
        ["id"] = e.Id,
        ["version"] = e.Version,
        ["kind"] = e.Kind,
        ["repository"] = e.Repository,
        ["commit"] = e.Commit,
        ["path"] = e.Path,
        ["license"] = e.License,
        ["text"] = text,
    };
}
