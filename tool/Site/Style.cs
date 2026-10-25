namespace Rulpane.Gallery;

/// <summary>The gallery's one stylesheet: Rulpane's colours, in light and dark, and nothing loaded from elsewhere.</summary>
public static class Style
{
    public const string Css = """
        :root {
          --bg: #f7f7f5; --surface: #ffffff; --sunken: #efefec; --text: #1c1d1f; --muted: #5d6066;
          --line: #e2e2de; --accent: #3b5bdb; --accent-text: #ffffff; --danger: #c92a2a; --danger-soft: #fdecec;
          color-scheme: light dark;
        }
        @media (prefers-color-scheme: dark) {
          :root {
            --bg: #141517; --surface: #1c1d20; --sunken: #232529; --text: #ececec; --muted: #a2a6ad;
            --line: #2e3035; --accent: #8ea6ff; --accent-text: #10131c; --danger: #ff8f8f; --danger-soft: #3a1d1f;
          }
        }
        * { box-sizing: border-box; }
        body { margin: 0; background: var(--bg); color: var(--text); font: 15px/1.55 system-ui, -apple-system, "Segoe UI", sans-serif; }
        a { color: var(--accent); }
        .bar { display: flex; gap: 16px; align-items: center; padding: 12px 24px; background: var(--surface); border-bottom: 1px solid var(--line); }
        .bar a { text-decoration: none; }
        .brand { font-weight: 650; color: var(--text) !important; }
        .spacer { flex: 1; }
        main { max-width: 920px; margin: 0 auto; padding: 24px 16px 48px; }
        footer { max-width: 920px; margin: 0 auto; padding: 16px; color: var(--muted); font-size: 13px; border-top: 1px solid var(--line); }
        h1 { font-size: 28px; line-height: 1.2; margin: 8px 0 12px; overflow-wrap: anywhere; }
        h2 { font-size: 18px; margin: 32px 0 8px; }
        .lead { font-size: 17px; }
        .muted { color: var(--muted); }
        .crumb { margin: 0; font-size: 13px; }
        .mono, code, pre { font-family: ui-monospace, "JetBrains Mono", Consolas, monospace; font-size: 13px; }
        h1.mono { font-size: 24px; }
        pre { background: var(--sunken); border: 1px solid var(--line); border-radius: 8px; padding: 12px; overflow-x: auto; white-space: pre-wrap; overflow-wrap: anywhere; }
        table { width: 100%; border-collapse: collapse; background: var(--surface); border: 1px solid var(--line); border-radius: 8px; display: block; overflow-x: auto; }
        th, td { text-align: left; padding: 8px 12px; border-bottom: 1px solid var(--line); vertical-align: top; }
        th { font-size: 13px; color: var(--muted); font-weight: 600; }
        .chip { display: inline-block; padding: 1px 8px; margin: 0 4px 4px 0; border-radius: 999px; background: var(--sunken); border: 1px solid var(--line); font-size: 12px; }
        .chip.danger, .notice.danger { background: var(--danger-soft); color: var(--danger); border-color: transparent; }
        .notice { padding: 12px; border-radius: 8px; }
        .button { display: inline-block; padding: 8px 14px; border-radius: 8px; background: var(--accent); color: var(--accent-text); border: 0; font: inherit; font-weight: 600; text-decoration: none; cursor: pointer; }
        form { display: grid; gap: 14px; margin-top: 24px; }
        label { display: grid; gap: 4px; font-weight: 600; }
        label .muted { font-weight: 400; }
        input, select { font: inherit; padding: 8px 10px; border-radius: 8px; border: 1px solid var(--line); background: var(--surface); color: var(--text); width: 100%; }
        .problems { color: var(--danger); margin: 0; }
        details { margin-top: 16px; }
        """;
}
