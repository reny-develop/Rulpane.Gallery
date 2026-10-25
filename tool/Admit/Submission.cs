using System.Text.RegularExpressions;

namespace Rulpane.Gallery;

/// <summary>
/// A submission as its issue states it: the form in <c>.github/ISSUE_TEMPLATE/submit.yml</c>,
/// whose body is one <c>### </c> heading per field and the answer under it. Nothing here is
/// believed beyond its shape; <see cref="Read"/> says what is wrong with the shape, all at once.
/// A name is under the account that owns the repository. When the account that opened the issue
/// is another, <see cref="Organization"/> is that owner, which has to be an organisation the
/// author is a public member of: a thing only GitHub can say, so it is asked after this.
/// </summary>
public sealed partial record Submission(string Kind, string Id, string Repository, string Commit, string Path, string? Sample, string? Organization)
{
    // The headings of the form. Change one and change the form, and the submit page that fills it.
    public const string KindHeading = "Kind";
    public const string NameHeading = "Name";
    public const string RepositoryHeading = "Repository";
    public const string CommitHeading = "Commit";
    public const string PathHeading = "Path";
    public const string SampleHeading = "Sample";

    public static Submission Read(string body, string author)
    {
        var answers = Answers(body);
        string Answer(string heading) => answers.GetValueOrDefault(heading, "");

        string kind = Answer(KindHeading).ToLowerInvariant();
        string id = Answer(NameHeading);
        string repository = Answer(RepositoryHeading);
        string commit = Answer(CommitHeading);
        string path = Answer(PathHeading);
        string? sample = Answer(SampleHeading) is { Length: > 0 } s ? s : null;

        var wrong = new List<string>();
        if (!Ledger.Kinds.Contains(kind))
        {
            wrong.Add($"The **kind** is View, Theme or Paneset. It says `{Answer(KindHeading)}`.");
        }

        if (!Name().IsMatch(id))
        {
            wrong.Add($"The **name** is your GitHub account, or the organisation whose repository it is, and a name of your choosing, in lowercase: `{author.ToLowerInvariant()}/kanban`. It says `{id}`.");
        }

        string? organization = null;
        if (!RepositoryName().IsMatch(repository))
        {
            wrong.Add($"The **repository** is `owner/name` on GitHub. It says `{repository}`.");
        }
        else if (Name().IsMatch(id))
        {
            string owner = id[..id.IndexOf('/')];
            string holder = repository[..repository.IndexOf('/')];
            bool authorHolds = string.Equals(holder, author, StringComparison.OrdinalIgnoreCase);
            if (!string.Equals(owner, holder, StringComparison.OrdinalIgnoreCase))
            {
                wrong.Add(owner == author.ToLowerInvariant()
                    ? $"The **repository** is one the account submitting owns: `{author}/…`. It says `{repository}`. A release from an organisation's repository is named under the organisation."
                    : authorHolds
                        ? $"A release is submitted by the account it is named under: this issue was opened by `{author}`, so the name is `{author.ToLowerInvariant()}/{id[(id.IndexOf('/') + 1)..]}`."
                        : $"A release is named under the account that owns its repository: `{holder.ToLowerInvariant()}/{id[(id.IndexOf('/') + 1)..]}`. It says `{id}`.");
            }
            else if (!authorHolds)
            {
                organization = owner;
            }
        }

        if (!CommitSha().IsMatch(commit))
        {
            wrong.Add($"The **commit** is the whole of one, 40 lowercase hexadecimal characters: what `git rev-parse HEAD` prints. It says `{commit}`.");
        }

        Check(path, PathHeading);
        if (sample is not null)
        {
            Check(sample, SampleHeading);
        }

        if (kind == "view" && sample is null)
        {
            wrong.Add("A view is tried in a **sample**: a paneset in the repository that says which rules to show it with — everything but `v`, which is the view itself.");
        }
        else if (kind == "paneset" && sample is not null)
        {
            wrong.Add("A paneset is tried as it is, so it takes no **sample**. Leave it empty.");
        }

        if (wrong.Count > 0)
        {
            throw new Refusal(string.Join("\n\n", wrong));
        }

        return new Submission(kind, id, repository, commit, path, sample, organization);

        void Check(string file, string heading)
        {
            if (file.Length == 0)
            {
                wrong.Add($"The **{heading.ToLowerInvariant()}** is empty.");
            }
            else if (!RepositoryPath().IsMatch(file) || file.Split('/').Any(p => p is "." or ".."))
            {
                wrong.Add($"The **{heading.ToLowerInvariant()}** is the path of a `.json` file from the top of the repository, with `/` between folders: `panes/kanban.json`. It says `{file}`.");
            }
        }
    }

    // One answer per heading. GitHub writes "_No response_" for a field left empty, and a person
    // may wrap a value in backticks; neither is part of the value.
    private static Dictionary<string, string> Answers(string body)
    {
        var answers = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        List<string>? current = null;
        foreach (string line in body.Replace("\r", "").Split('\n'))
        {
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                current = [];
                answers[line[4..].Trim()] = current;
            }
            else if (current is not null && line.Trim().Length > 0)
            {
                current.Add(line.Trim());
            }
        }

        return answers.ToDictionary(a => a.Key, a =>
        {
            string value = string.Join(" ", a.Value);
            value = value == "_No response_" ? "" : value;
            return value.Length >= 2 && value[0] == '`' && value[^1] == '`' ? value[1..^1].Trim() : value;
        });
    }

    // A GitHub account, as the Gallery records it, then the name, as Rulpane's Source reads one.
    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,37}[a-z0-9])?/[a-z0-9][a-z0-9-]{0,63}$")]
    private static partial Regex Name();

    [GeneratedRegex("^[A-Za-z0-9](?:[A-Za-z0-9-]{0,37}[A-Za-z0-9])?/[A-Za-z0-9._-]{1,100}$")]
    private static partial Regex RepositoryName();

    [GeneratedRegex("^[0-9a-f]{40}$")]
    private static partial Regex CommitSha();

    [GeneratedRegex(@"^[A-Za-z0-9._-]+(/[A-Za-z0-9._-]+)*\.json$")]
    private static partial Regex RepositoryPath();
}
