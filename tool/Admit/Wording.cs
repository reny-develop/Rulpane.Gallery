using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Rulpane.Gallery;

/// <summary>
/// What the words of a submitted document may not hold. A release in the Gallery is named
/// by a link anybody can make, and its words are what the screen shows, so a release that
/// tells its reader where to go is the one kind this Gallery exists to keep out: an address
/// to visit, an address to write to, markup, or characters that change how the text around
/// them reads without showing themselves. A view has no block that shows a picture or a link,
/// so this is about text alone.
/// </summary>
public static partial class Wording
{
    /// <summary>A release is named by a link, never carried in one, but it is read on every screen that shows it.</summary>
    public const int MaxBytes = 32 * 1024;

    /// <summary>Every string in the document, its keys included, that says something this Gallery refuses; each as one line of markdown.</summary>
    public static List<string> Problems(string file, string json)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
        }
        catch (JsonException e)
        {
            return [$"`{file}` is not JSON: {e.Message}"];
        }

        var problems = new List<string>();
        foreach (var (at, text, shown) in Strings(root, "", false))
        {
            if (Why(text, shown, at.StartsWith("/expects/", StringComparison.Ordinal)) is string why)
            {
                problems.Add($"`{file}` at `{(at.Length == 0 ? "/" : at)}` holds {why}: `{Shown(text)}`");
            }
        }

        return problems;
    }

    // A domain is looked for only in words the screen shows, where a binding such as move.to never is.
    // Markup is not looked for in what a view expects: a type there is written list<string>, and
    // Rulpane refuses anything there that is not a type.
    private static string? Why(string text, bool shown, bool typed) =>
        text.Contains("://", StringComparison.Ordinal) || text.Contains("www.", StringComparison.OrdinalIgnoreCase) ? "an address"
        : Mail().IsMatch(text) ? "an e-mail address"
        : shown && Domain().IsMatch(text) ? "what reads as a domain name"
        : !typed && Markup().IsMatch(text) ? "markup"
        : text.Any(Invisible) ? "a character that is not shown (a direction mark, a zero-width space or a byte-order mark)"
        : null;

    // U+200B–U+200F, U+202A–U+202E, U+2060–U+2064, U+2066–U+2069 and U+FEFF.
    private static bool Invisible(char c) =>
        c is >= '​' and <= '‏' or >= '‪' and <= '‮' or >= '⁠' and <= '⁤' or >= '⁦' and <= '⁩' or '﻿';

    // Each string with whether the screen shows it as words: a title, a text, a label, or a name the view gives.
    private static IEnumerable<(string At, string Text, bool Shown)> Strings(JsonNode? node, string at, bool shown)
    {
        switch (node)
        {
            case JsonObject o:
                foreach (var (key, value) in o)
                {
                    string here = $"{at}/{key}";
                    yield return (here, key, false);
                    bool words = key is "title" or "text" or "label" || at.StartsWith("/names/", StringComparison.Ordinal);
                    foreach (var found in Strings(value, here, words))
                    {
                        yield return found;
                    }
                }

                break;
            case JsonArray a:
                for (int i = 0; i < a.Count; i++)
                {
                    foreach (var found in Strings(a[i], $"{at}/{i}", shown))
                    {
                        yield return found;
                    }
                }

                break;
            case JsonValue v when v.TryGetValue(out string? s):
                yield return (at, s, shown);
                break;
        }
    }

    // Shown in the answer as escapes, so what could not be seen can be.
    private static string Shown(string text) =>
        string.Concat(text.Take(80).Select(c => Invisible(c) ? $"\\u{(int)c:X4}" : c == '`' ? "'" : c.ToString())) + (text.Length > 80 ? "…" : "");

    // Ending in letters, so a version pinned with @ (approval@1.0.0) is not one.
    [GeneratedRegex(@"[\w.+-]+@[\w-]+(\.[\w-]+)*\.[A-Za-z]{2,}\b")]
    private static partial Regex Mail();

    // The suffixes an address is most often given under.
    [GeneratedRegex(@"(?i)\b[a-z0-9-]+(\.[a-z0-9-]+)*\.(com|net|org|info|biz|io|co|me|app|dev|page|site|online|shop|store|link|click|xyz|top|ly|gl|to|cc|tk|ml|ga|cf|gq|ru|cn|jp|us|uk|de|fr)\b")]
    private static partial Regex Domain();

    [GeneratedRegex(@"<\s*[A-Za-z!/?]")]
    private static partial Regex Markup();
}
