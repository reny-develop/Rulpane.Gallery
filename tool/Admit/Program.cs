// Decides whether a view, a theme or a paneset can be recorded in the Gallery, and records it.
//
//   dotnet run --project tool/Admit -- read <issue body> --author <login> --out <folder>
//                                      [--checkout <folder> --license <spdx>] [--registry <folder>]
//   dotnet run --project tool/Admit -- record <folder>
//   dotnet run --project tool/Admit -- check [--registry <folder>]
//
// read     takes the submission an issue states, clones the repository at the commit it names
//          (or reads --checkout, which is how the tests run it), asks GitHub what licence the
//          repository is under at that commit (or takes --license), and holds the release to
//          everything doc/policy.md says it is held to — its words, its shape, and a run in its
//          sample with Rulpane's own code. It writes <folder>/answer.md either way and, when the
//          release passes, <folder>/submission.json and <folder>/files/: the line and the copy
//          `record` would write. Exit 0 is a release that passed, 1 one that did not.
//
// record   gives the release in <folder> its number, the next for that name, and writes it into
//          the ledger and under release/. It reads the ledger as it is now, so two submissions of
//          one name read at the same time are numbered one after the other. It prints what a link
//          names the release by: <owner>/<name>@<version>.
//
// check    holds every line of the ledger to the copy beside it, and runs every release that is
//          not withheld in its sample again, with the Rulpane it was built against — so a change
//          to Rulpane that would stop a published release is seen here first.
//
// --registry is a copy of the Registry's rule sets, as Rulpane's tool/Mirror writes it, for
// rules that are one of them or hold one. Run from the top of this repository.

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Rulpane.Engine;
using Rulpane.Gallery;

// A licence under which anybody may keep and serve a copy, which is what the Gallery does.
string[] licenses = ["MIT", "MIT-0", "0BSD", "ISC", "BSD-2-Clause", "BSD-3-Clause", "Apache-2.0", "Unlicense", "CC0-1.0", "CC-BY-4.0"];

var options = new Dictionary<string, string>(StringComparer.Ordinal);
var positional = new List<string>();
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

return positional switch
{
    ["read", string body] when options.ContainsKey("author") && options.ContainsKey("out") => await ReadAsync(body),
    ["record", string folder] => Record(folder),
    ["check"] => await CheckAsync(),
    _ => Usage(),
};

static int Usage()
{
    Console.Error.WriteLine("usage: Admit read <issue body> --author <login> --out <folder> [--checkout <folder> --license <spdx>] [--registry <folder>]");
    Console.Error.WriteLine("       Admit record <folder>");
    Console.Error.WriteLine("       Admit check [--registry <folder>]");
    return 2;
}

async Task<int> ReadAsync(string bodyFile)
{
    string output = options["out"];
    Directory.CreateDirectory(output);
    string answer = Path.Combine(output, "answer.md");
    try
    {
        Submission submission = Submission.Read(File.ReadAllText(bodyFile), options["author"]);
        List<Entry> ledger = File.Exists(Ledger.File) ? Ledger.Read(Ledger.File) : [];
        if (ledger.FirstOrDefault(e => e.Id == submission.Id) is { } named && named.Kind != submission.Kind)
        {
            throw new Refusal($"`{submission.Id}` is a {named.Kind} in the Gallery, and a name is one thing for good. Give the {submission.Kind} a name of its own.");
        }

        string checkout = options.GetValueOrDefault("checkout") ?? Clone(submission, Path.Combine(output, "repository"));
        (string license, string licenseFile) = options.TryGetValue("license", out string? stated)
            ? (stated, FindLicense(checkout))
            : await AskLicenseAsync(submission, checkout);
        if (!licenses.Contains(license))
        {
            throw new Refusal($"`{submission.Repository}` is under {(license == "NOASSERTION" ? "a licence GitHub does not recognise" : $"`{license}`")} at that commit. The Gallery keeps a copy of every release and serves it, so it takes one under a licence that lets anybody do that: {string.Join(", ", licenses.Select(l => $"`{l}`"))}.");
        }

        var files = new Dictionary<string, string>(StringComparer.Ordinal) { [Ledger.DocumentFile] = submission.Path };
        if (submission.Sample is string samplePath)
        {
            files[Ledger.SampleFile] = samplePath;
        }

        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        var problems = new List<string>();
        foreach (var (copy, path) in files)
        {
            string full = Path.Combine(checkout, path);
            if (!File.Exists(full))
            {
                problems.Add($"`{submission.Repository}` has no `{path}` at `{submission.Commit[..7]}`.");
                continue;
            }

            if (new FileInfo(full).Length > Wording.MaxBytes)
            {
                problems.Add($"`{path}` is {new FileInfo(full).Length / 1024} KiB, and a document here is at most {Wording.MaxBytes / 1024} KiB.");
                continue;
            }

            texts[copy] = File.ReadAllText(full);
            problems.AddRange(Wording.Problems(path, texts[copy]));
        }

        if (problems.Count > 0)
        {
            throw new Refusal(string.Join("\n\n", problems.Select(p => "- " + p)));
        }

        using HttpClient origin = Origin.Client(options.GetValueOrDefault("registry"), ledger);
        Trial.Seen? seen = await TryAsync(origin, submission.Kind, submission.Path, texts[Ledger.DocumentFile], submission.Sample, texts.GetValueOrDefault(Ledger.SampleFile));

        string sha256 = Ledger.Sha256(Path.Combine(checkout, submission.Path));
        if (ledger.LastOrDefault(e => e.Id == submission.Id && e.Withheld is null) is { } latest && latest.Sha256 == sha256)
        {
            throw new Refusal($"This is `{latest.Reference}`, to the byte. A new release is a document that changed.");
        }

        // What record writes: the line without its number, and the copy.
        var entry = new Entry(submission.Id, 0, submission.Kind, submission.Repository, submission.Commit, submission.Path, license, sha256, submission.Sample);
        string copied = Path.Combine(output, "files");
        if (Directory.Exists(copied))
        {
            Directory.Delete(copied, recursive: true);
        }

        Directory.CreateDirectory(copied);
        foreach (var (copy, path) in files)
        {
            File.Copy(Path.Combine(checkout, path), Path.Combine(copied, copy));
        }

        File.Copy(licenseFile, Path.Combine(copied, Ledger.LicenseFile));
        File.WriteAllText(Path.Combine(output, "submission.json"), Ledger.ToJson(entry).ToJsonString());

        File.WriteAllText(answer, $"""
            ### ✓ Read — the {submission.Kind} `{submission.Id}`

            Read out of `{submission.Repository}` at `{submission.Commit}`: `{submission.Path}`, under `{license}`.

            {Summary(submission.Kind, texts[Ledger.DocumentFile])}{(seen is null ? "- shown in no sample: its colours were checked on their own" : $"- tried {(submission.Sample is null ? "as it is" : $"in `{submission.Sample}`")} — rule set `{seen.RuleSet}`, {seen.Positions} position{(seen.Positions == 1 ? "" : "s")}{(seen.ReachedEnd ? ", to the end" : "")}, each as the view expects")}

            """);
        return 0;
    }
    catch (Refusal refusal)
    {
        File.WriteAllText(answer, $"""
            ### ✕ Not recorded

            {refusal.Message}

            Edit this issue to correct it — it is read again whenever the issue is saved. Nothing was recorded, and this issue stays open until it is put right or closed.

            """);
        return 1;
    }
}

int Record(string folder)
{
    Entry line = Ledger.FromJson(JsonNode.Parse(File.ReadAllText(Path.Combine(folder, "submission.json")))!);
    var entries = File.Exists(Ledger.File) ? Ledger.Read(Ledger.File) : [];
    if (entries.FirstOrDefault(e => e.Id == line.Id) is { } named && named.Kind != line.Kind)
    {
        Console.Error.WriteLine($"{line.Id} became a {named.Kind} while this submission was being read, and a name is one thing for good.");
        return 1;
    }

    if (entries.LastOrDefault(e => e.Id == line.Id && e.Withheld is null) is { } latest && latest.Sha256 == line.Sha256)
    {
        Console.Error.WriteLine($"This is {latest.Reference}, to the byte, recorded while this submission was being read.");
        return 1;
    }

    Entry entry = line with { Version = entries.Where(e => e.Id == line.Id).Select(e => e.Version).DefaultIfEmpty(0).Max() + 1 };
    string files = Path.Combine(folder, "files");
    Directory.CreateDirectory(entry.Folder);
    foreach (string file in Directory.EnumerateFiles(files))
    {
        File.Copy(file, Path.Combine(entry.Folder, Path.GetFileName(file)));
    }

    Directory.CreateDirectory(Path.GetDirectoryName(Ledger.File)!);
    Ledger.Write(Ledger.File, [.. entries, entry]);
    Console.WriteLine(entry.Reference);
    return 0;
}

async Task<int> CheckAsync()
{
    var entries = Ledger.Read(Ledger.File);
    int wrong = 0;
    void Wrong(string what)
    {
        Console.WriteLine($"✕ {what}");
        wrong++;
    }

    foreach (var group in entries.GroupBy(e => e.Id))
    {
        int[] versions = [.. group.Select(e => e.Version)];
        if (!versions.SequenceEqual(Enumerable.Range(1, versions.Length)))
        {
            Wrong($"{group.Key}: its releases are numbered {string.Join(", ", versions)}, and they are numbered from 1 in the order recorded.");
        }

        if (group.Select(e => e.Kind).Distinct().Count() > 1)
        {
            Wrong($"{group.Key}: its releases are {string.Join(" and ", group.Select(e => e.Kind).Distinct())}, and a name is one thing.");
        }
    }

    using HttpClient origin = Origin.Client(options.GetValueOrDefault("registry"), entries);
    foreach (Entry entry in entries)
    {
        string document = Path.Combine(entry.Folder, Ledger.DocumentFile);
        string[] missing = [.. new[] { Ledger.DocumentFile, Ledger.LicenseFile, entry.Sample is null ? null : Ledger.SampleFile }
            .OfType<string>().Where(f => !File.Exists(Path.Combine(entry.Folder, f)))];
        if (missing.Length > 0)
        {
            Wrong($"{entry.Reference}: {entry.Folder} has no {string.Join(", ", missing)}.");
            continue;
        }

        if (Ledger.Sha256(document) != entry.Sha256)
        {
            Wrong($"{entry.Reference}: {document} is not the document the ledger recorded.");
            continue;
        }

        if (entry.Withheld is not null)
        {
            Console.WriteLine($"- {entry.Reference}: withheld ({entry.Withheld})");
            continue;
        }

        try
        {
            string? sample = entry.Sample is null ? null : File.ReadAllText(Path.Combine(entry.Folder, Ledger.SampleFile));
            Trial.Seen? seen = await TryAsync(origin, entry.Kind, entry.Path, File.ReadAllText(document), entry.Sample, sample);
            Console.WriteLine($"✓ {entry.Kind} {entry.Reference}: {(seen is null ? "its colours" : $"{seen.Positions} position(s) of {seen.RuleSet}")}");
        }
        catch (Refusal refusal)
        {
            Wrong($"{entry.Reference}: {refusal.Message.ReplaceLineEndings(" ")}");
        }
    }

    Console.WriteLine($"{entries.Count} release(s), {wrong} wrong");
    return wrong == 0 ? 0 : 1;
}

// Reads the document as what it says it is, and runs it: a paneset as it is, a view or a theme
// in its sample, with itself in the part the sample leaves out. A theme with no sample is only
// read, which is where its colours are held to their contrast.
static async Task<Trial.Seen?> TryAsync(HttpClient origin, string kind, string path, string document, string? samplePath, string? sample)
{
    string packed = Fragment.Pack(Trial.Minify(document));
    switch (kind)
    {
        case "paneset":
            return await Trial.RunAsync(origin, Trial.Attempt($"`{path}` is not a paneset", () => Paneset.Parse(document)));
        case "theme":
            Trial.Attempt($"`{path}` is refused as a theme", () => Theme.Parse(document));
            return sample is null ? null : await Trial.RunAsync(origin, In(samplePath!, sample, "t", p => p.Theme) with { Theme = packed });
        default:
            if (JsonNode.Parse(Trial.Minify(document)) is not JsonObject view || view["$schema"]?.ToString() != View.Schema)
            {
                throw new Refusal($"`{path}` is not a view: a view is an object that says `\"$schema\": \"{View.Schema}\"`.");
            }

            return await Trial.RunAsync(origin, In(samplePath!, sample!, "v", p => p.View) with { View = packed });
    }
}

// The sample, which leaves out the part the release fills.
static Paneset In(string path, string sample, string key, Func<Paneset, string?> part)
{
    Paneset paneset = Trial.Attempt($"The sample `{path}` is not a paneset", () => Paneset.Parse(sample));
    return part(paneset) is null
        ? paneset
        : throw new Refusal($"The sample `{path}` has a `{key}` of its own. A sample leaves out `{key}`: that is where what you are submitting goes.");
}

// What the answer says the release is, beyond its kind: a line that ends in a newline, or nothing.
static string Summary(string kind, string document)
{
    if (kind != "view")
    {
        return "";
    }

    JsonNode? expects = JsonNode.Parse(Trial.Minify(document))?["expects"];
    if (expects is null)
    {
        return "- expects: not said; it reads what its bindings name\n";
    }

    var projections = (expects["projections"] as JsonObject ?? []).Select(p => $"`{p.Key}: {p.Value}`");
    var inputs = (expects["inputs"] as JsonObject ?? []).Select(i => $"`{i.Key}({string.Join(", ", i.Value?.AsArray().Select(a => a?.GetValue<string>()) ?? [])})`");
    string[] all = [.. projections, .. inputs];
    return $"- expects: {(all.Length == 0 ? "nothing" : string.Join(", ", all))}\n";
}

static string Clone(Submission submission, string into)
{
    if (Directory.Exists(into))
    {
        Directory.Delete(into, recursive: true);
    }

    if (Git(".", "clone", "--quiet", "--filter=blob:none", "--no-checkout", $"https://github.com/{submission.Repository}.git", into) != 0)
    {
        throw new Refusal($"`{submission.Repository}` could not be cloned. The Gallery reads a release out of a public repository.");
    }

    if (Git(into, "checkout", "--quiet", "--detach", submission.Commit) != 0)
    {
        throw new Refusal($"`{submission.Repository}` has no commit `{submission.Commit}`.");
    }

    return into;
}

static int Git(string directory, params string[] arguments)
{
    var start = new ProcessStartInfo("git") { WorkingDirectory = directory, RedirectStandardError = true };
    foreach (string argument in arguments)
    {
        start.ArgumentList.Add(argument);
    }

    using Process git = Process.Start(start)!;
    git.StandardError.ReadToEnd();
    git.WaitForExit();
    return git.ExitCode;
}

// The licence GitHub detects at that commit, and the file it detected it in.
static async Task<(string, string)> AskLicenseAsync(Submission submission, string checkout)
{
    using var http = new HttpClient();
    http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Rulpane.Gallery", "1"));
    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    if (Environment.GetEnvironmentVariable("GITHUB_TOKEN") is { Length: > 0 } token)
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    using HttpResponseMessage response = await http.GetAsync($"https://api.github.com/repos/{submission.Repository}/license?ref={submission.Commit}");
    if (response.StatusCode == HttpStatusCode.NotFound)
    {
        throw new Refusal($"GitHub finds no licence in `{submission.Repository}` at `{submission.Commit[..7]}`. Add a `LICENSE` file, and submit the commit that has it.");
    }

    response.EnsureSuccessStatusCode();
    JsonNode found = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    return (found["license"]?["spdx_id"]?.GetValue<string>() ?? "NOASSERTION", Path.Combine(checkout, found["path"]!.GetValue<string>()));
}

static string FindLicense(string checkout) =>
    new[] { "LICENSE", "LICENSE.md", "LICENSE.txt", "COPYING" }.Select(f => Path.Combine(checkout, f)).FirstOrDefault(File.Exists)
    ?? throw new Refusal("The repository has no `LICENSE` file at its top.");
