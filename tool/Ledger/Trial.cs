using System.Text.Json;
using System.Text.Json.Nodes;
using Rulealize;
using Rulpane.Engine;

namespace Rulpane.Gallery;

/// <summary>
/// A paneset run the way the viewer would run it: the rules compiled with Rulpane's own runtime,
/// the view and the theme read by Rulpane's own readers, and every position along one line of
/// play held to what the view expects. A release that would stop the screen on its own sample is
/// refused here instead.
/// </summary>
public static class Trial
{
    /// <summary>How many positions are visited: enough to leave the start, and few enough to be quick.</summary>
    public const int Positions = 32;

    private static readonly JsonDocumentOptions Lenient = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly RuleRuntime Runtime = CuratedRuntime.Create();

    /// <summary>What a trial saw: the rule set it ran, and how many positions it checked.</summary>
    public sealed record Seen(string RuleSet, int Positions, bool ReachedEnd);

    /// <summary>Rules as Rulpane compiles them: the document a view is read against, and what it compiled to.</summary>
    public sealed record Compiled(string Document, RuleContext Context);

    public static async Task<Seen> RunAsync(HttpClient http, Paneset paneset)
    {
        Compiled rules = await CompileAsync(http, paneset.Rules ?? throw new Refusal("The paneset names no rules: `r` is where they go."));

        View? view = null;
        if (paneset.View is string layout)
        {
            Source viewSource = Attempt("The view `v` names cannot be found", () => Source.View(layout));
            string text = await AttemptAsync("The view cannot be read", () => viewSource.ReadAsync(http));
            view = Attempt("The view does not fit the rules", () => View.Parse(text, rules.Context, rules.Document, paneset.Bind));
        }
        else if (paneset.Bind.Count > 0)
        {
            throw new Refusal("`b` binds what a view reads, and the paneset names no view.");
        }

        if (paneset.Theme is string look)
        {
            Source themeSource = Attempt("The theme `t` names cannot be found", () => Source.Theme(look));
            string text = await AttemptAsync("The theme cannot be read", () => themeSource.ReadAsync(http));
            Attempt("The theme is refused", () => Theme.Parse(text));
        }

        return Play(rules.Context, view, paneset.State);
    }

    /// <summary>The rules <paramref name="rules"/> names, as <c>r</c> names them, read and compiled with what they hold.</summary>
    public static async Task<Compiled> CompileAsync(HttpClient http, string rules)
    {
        Source source = Attempt("The rules `r` names cannot be found", () => Source.Rules(rules));
        string document = await AttemptAsync($"The rules{(source.IsPacked ? "" : $" `{rules}`")} cannot be read", () => source.ReadAsync(http));
        IReadOnlyList<Document> held = await AttemptAsync("What the rules hold cannot be read", () => source.HoldsAsync(http, document));
        return new Compiled(document, Attempt("The rules do not compile", () => Registry.Compile(Runtime, document, held)));
    }

    /// <summary>
    /// Plays one line of the rules from <paramref name="state"/>, the state's <c>data</c> packed as
    /// <c>s</c> carries it, or their start, holding every position to what <paramref name="view"/> expects.
    /// </summary>
    public static Seen Play(RuleContext context, View? view, string? state)
    {
        Session session = Attempt("The state `s` is not one the rules can start from", () => Session.Open(context, state));
        var played = new List<string>();
        for (int position = 1; ; position++)
        {
            var projections = context.Projections.ToDictionary(name => name, session.Project);
            try
            {
                view?.Check(name => projections.GetValueOrDefault(name), session.Data);
            }
            catch (FormatException e)
            {
                throw new Refusal($"At position {position}{(played.Count == 0 ? ", the start" : $", after {string.Join(", ", played)}")}, the screen would stop:\n\n```\n{e.Message}\n```");
            }

            if (session.Terminal.IsTerminal)
            {
                return new Seen(context.RuleSet, position, true);
            }

            // The first move with nothing to type, and the first of its outcomes: the same line of
            // play on every run, so a release that passed once passes again on the same Rulpane.
            ValidInput? move = position < Positions ? session.ValidInputs().FirstOrDefault(m => m.IsComplete) : null;
            if (move is null)
            {
                return new Seen(context.RuleSet, position, false);
            }

            Outcome outcome = context.GetOutcomes(move.ToInputDocument(context.RuleSet), session.State, Session.OutcomeLimit)[0];
            played.Add(Names.None.Describe(move));
            session = Session.Open(context, Session.PackData(outcome.Result.State));
        }
    }

    public static string Minify(string json) =>
        (JsonNode.Parse(json, documentOptions: Lenient) ?? throw new Refusal("A document is `null`.")).ToJsonString();

    private static async Task<T> AttemptAsync<T>(string what, Func<Task<T>> run)
    {
        try
        {
            return await run();
        }
        catch (Exception e) when (e is not Refusal)
        {
            throw new Refusal($"{what}:\n\n```\n{e.Message}\n```");
        }
    }

    public static T Attempt<T>(string what, Func<T> run)
    {
        try
        {
            return run();
        }
        catch (Exception e) when (e is not Refusal)
        {
            throw new Refusal($"{what}:\n\n```\n{e.Message}\n```");
        }
    }
}

/// <summary>Why a submission cannot be recorded, in words its author can act on.</summary>
public sealed class Refusal(string message) : Exception(message);
