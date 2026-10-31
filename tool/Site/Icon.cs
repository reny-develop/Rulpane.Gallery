namespace Rulpane.Gallery;

/// <summary>
/// The gallery's icons, each Rulpane's own so that the two read as one place: the tab icon, a copy
/// of src/Rulpane.Viewer/wwwroot/favicon.svg there; the mark in the bar, a copy of the one in
/// src/Rulpane.Viewer/App.razor; and the line icons, drawn as src/Rulpane.Viewer/Components/Icon.razor
/// draws them. Change one there and change it here.
/// </summary>
public static class Icon
{
    public const string Svg = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 96 96"><path d="M22 0 H74 A22 22 0 0 1 96 22 V74 A22 22 0 0 1 74 96 H22 A22 22 0 0 1 0 74 V22 A22 22 0 0 1 22 0 Z" fill="#3B3FD8"/><g fill="none" stroke-linecap="round" stroke-linejoin="round"><path d="M32.22 41.38 L22.04 51.56 A15.84 15.84 0 0 0 44.44 73.96 L54.62 63.78" stroke="#FFFFFF" stroke-width="7.2"/><path d="M41.38 32.22 L51.56 22.04 A15.84 15.84 0 0 1 73.96 44.44 L63.78 54.62" stroke="#FFFFFF" stroke-width="7.2"/><path d="M37.82 56.15 Q33.24 51.56 37.27 47.53 L39.98 44.82 Q42.4 42.4 38.5 38.5 Q42.4 42.4 44.82 39.98 L47.53 37.27 Q51.56 33.24 56.15 37.82" stroke="#D7F36B" stroke-width="6.48"/><path d="M39.85 58.18 Q44.44 62.76 48.47 58.73 L51.18 56.02 Q53.6 53.6 57.5 57.5 Q53.6 53.6 56.02 51.18 L58.73 48.47 Q62.76 44.44 58.18 39.85" stroke="#D7F36B" stroke-width="6.48"/></g></svg>

        """;

    /// <summary>Rulpane's mark, as its bar shows it: the tab icon at 22 pixels, in its own colours.</summary>
    public const string Mark = """<svg width="22" height="22" viewBox="0 0 96 96" aria-hidden="true"><rect width="96" height="96" rx="22" fill="#3B3FD8"/><g fill="none" stroke-linecap="round" stroke-linejoin="round"><path d="M32.22 41.38 L22.04 51.56 A15.84 15.84 0 0 0 44.44 73.96 L54.62 63.78" stroke="#FFFFFF" stroke-width="7.2"/><path d="M41.38 32.22 L51.56 22.04 A15.84 15.84 0 0 1 73.96 44.44 L63.78 54.62" stroke="#FFFFFF" stroke-width="7.2"/><path d="M37.82 56.15 Q33.24 51.56 37.27 47.53 L39.98 44.82 Q42.4 42.4 38.5 38.5 Q42.4 42.4 44.82 39.98 L47.53 37.27 Q51.56 33.24 56.15 37.82" stroke="#D7F36B" stroke-width="6.48"/><path d="M39.85 58.18 Q44.44 62.76 48.47 58.73 L51.18 56.02 Q53.6 53.6 57.5 57.5 Q53.6 53.6 56.02 51.18 L58.73 48.47 Q62.76 44.44 58.18 39.85" stroke="#D7F36B" stroke-width="6.48"/></g></svg>""";

    /// <summary>
    /// A line icon in the text's colour. <c>copy</c>, <c>arrow</c> and <c>info</c> are Rulpane's;
    /// <c>search</c> and <c>out</c> (a link that leaves the Gallery) are the Gallery's, drawn the same way.
    /// </summary>
    public static string Line(string name, int size = 14) => $"""<svg class="icon" width="{size}" height="{size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">{name switch
    {
        "copy" => """<rect x="9" y="9" width="12" height="12" rx="2"/><path d="M5 15H4a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v1"/>""",
        "arrow" => """<path d="M5 12h14"/><path d="M13 5l7 7-7 7"/>""",
        "info" => """<circle cx="12" cy="12" r="9"/><path d="M12 11v5"/><path d="M12 8h.01"/>""",
        "search" => """<circle cx="11" cy="11" r="7"/><path d="M20 20l-4-4"/>""",
        "out" => """<path d="M7 17L17 7"/><path d="M8 7h9v9"/>""",
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "There is no such icon."),
    }}</svg>""";
}
