namespace Rulpane.Gallery;

/// <summary>
/// The gallery's one stylesheet, and nothing loaded from elsewhere. Its tokens, fonts, bar and
/// buttons are Rulpane's, as src/Rulpane.Viewer/wwwroot/css/app.css has them, in light and dark:
/// change one there and change it here. What only the Gallery has — cards, filters, the kinds'
/// colours — is built from those tokens.
/// </summary>
public static class Style
{
    public const string Css = """
        @font-face { font-family: "Inter"; src: url("fonts/inter-latin.woff2") format("woff2"); font-weight: 100 900; font-display: swap; }
        @font-face { font-family: "JetBrains Mono"; src: url("fonts/jetbrains-mono-latin.woff2") format("woff2"); font-weight: 100 800; font-display: swap; }
        :root {
          --color-bg: #f2f3ef; --color-surface: #ffffff; --color-sunken: #e9ebe5; --color-text: #14161a; --color-muted: #5e636b;
          --color-line: #daddd5; --color-line-strong: #b9bdb3;
          --color-accent: #3b3fd8; --color-accent-hover: #2f32b8; --color-accent-text: #ffffff; --color-accent-soft: #e7e8fc;
          --color-mark: #d7f36b; --color-mark-text: #1e2600; --color-ok: #16794a; --color-ok-soft: #dff2e7;
          --color-danger: #c1352a; --color-danger-soft: #fae5e2;
          --color-code: #14161a; --color-code-text: #ecedef;
          --radius-sm: 4px; --radius-md: 8px; --radius-lg: 12px; --radius-pill: 999px;
          --font-ui: "Inter", system-ui, -apple-system, "Hiragino Sans", "Noto Sans JP", "Yu Gothic UI", sans-serif;
          --font-mono: "JetBrains Mono", ui-monospace, "SFMono-Regular", Consolas, monospace;
          color-scheme: light;
        }
        @media (prefers-color-scheme: dark) {
          :root {
            --color-bg: #111214; --color-surface: #1a1b1f; --color-sunken: #232429; --color-text: #ecedef; --color-muted: #9a9ea6;
            --color-line: #2d2f35; --color-line-strong: #43464e;
            --color-accent: #8f92ff; --color-accent-hover: #a9abff; --color-accent-text: #0d0e1f; --color-accent-soft: #25264a;
            --color-mark: #c6e85a; --color-mark-text: #1a2100; --color-ok: #55c98f; --color-ok-soft: #163325;
            --color-danger: #ff8174; --color-danger-soft: #3d1c19;
            --color-code: #0b0c0e;
            color-scheme: dark;
          }
        }
        * { box-sizing: border-box; }
        [hidden] { display: none !important; }
        body { margin: 0; background: var(--color-bg); color: var(--color-text); font-family: var(--font-ui); font-size: 15px; line-height: 1.5; }
        a { color: var(--color-accent); text-decoration: none; }
        a:hover { text-decoration: underline; }
        .mono, code, pre { font-family: var(--font-mono); font-size: 13px; }
        .muted { color: var(--color-muted); }
        .icon { flex: none; display: block; }

        .bar { display: flex; align-items: center; gap: 10px; height: 56px; padding: 0 28px; background: var(--color-surface); border-bottom: 1px solid var(--color-line); position: sticky; top: 0; z-index: 3; }
        .brand { display: flex; align-items: center; gap: 10px; color: var(--color-text); font-weight: 700; font-size: 16px; }
        .brand:hover { text-decoration: none; }
        .spacer { flex: 1; }
        .bar nav { display: flex; gap: 2px; }

        button, .button { display: inline-flex; align-items: center; justify-content: center; gap: 6px; font: 500 14px/1.3 var(--font-ui); padding: 8px 14px; border-radius: var(--radius-md); border: 1px solid var(--color-line-strong); background: var(--color-surface); color: var(--color-text); cursor: pointer; white-space: nowrap; }
        button:hover, .button:hover { border-color: var(--color-muted); text-decoration: none; }
        button:focus-visible, .button:focus-visible, input:focus-visible, select:focus-visible { outline: 2px solid var(--color-accent); outline-offset: 2px; }
        .primary { background: var(--color-accent); border-color: var(--color-accent); color: var(--color-accent-text); }
        .primary:hover { background: var(--color-accent-hover); border-color: var(--color-accent-hover); }
        .ghost { background: transparent; border-color: transparent; }
        .ghost:hover { background: var(--color-sunken); border-color: transparent; }
        .small { padding: 5px 10px; font-size: 13px; }
        .button.wide { width: 100%; padding: 11px 14px; font-size: 15px; font-weight: 600; }

        main { max-width: 1280px; margin: 0 auto; padding: 32px 40px 64px; }
        footer { max-width: 1280px; margin: 0 auto; padding: 16px 40px 32px; color: var(--color-muted); font-size: 13px; border-top: 1px solid var(--color-line); }
        h1 { font-size: 32px; line-height: 1.2; font-weight: 700; margin: 8px 0; letter-spacing: -0.01em; overflow-wrap: anywhere; }
        h2 { font-size: 20px; margin: 40px 0 10px; }
        .lead { font-size: 17px; color: var(--color-muted); margin: 0 0 16px; max-width: 760px; }
        .crumb { margin: 0 0 16px; font-size: 13px; color: var(--color-muted); }
        .crumb span { margin: 0 4px; color: var(--color-line-strong); }
        .actions { display: flex; flex-wrap: wrap; gap: 8px; margin: 0; }
        .notice { display: flex; gap: 10px; align-items: flex-start; padding: 10px 14px; border-radius: var(--radius-md); background: var(--color-sunken); color: var(--color-muted); font-size: 14px; margin: 0 0 16px; }
        .notice .icon { margin-top: 2px; }
        .notice strong { color: var(--color-text); }
        .notice.danger { background: var(--color-danger-soft); color: var(--color-danger); }

        .kind, .chip { display: inline-block; padding: 1px 8px; border-radius: var(--radius-pill); font-size: 12px; font-weight: 600; line-height: 1.6; }
        .kind.paneset { background: var(--color-accent-soft); color: var(--color-accent); }
        .kind.view { background: var(--color-ok-soft); color: var(--color-ok); }
        .kind.theme { background: var(--color-mark); color: var(--color-mark-text); }
        .kind.withheld { background: var(--color-danger-soft); color: var(--color-danger); }
        .chip { background: var(--color-sunken); border: 1px solid var(--color-line); font-weight: 400; margin: 0 4px 4px 0; }

        .hero { display: flex; gap: 40px; align-items: flex-end; justify-content: space-between; margin: 16px 0 20px; }
        .hero .lead { margin: 0; }

        .filters { display: grid; gap: 14px; padding: 16px; margin: 0 0 28px; border-radius: var(--radius-lg); background: var(--color-surface); border: 1px solid var(--color-line); }
        .filters .row { display: flex; gap: 16px; align-items: center; flex-wrap: wrap; }
        .search { flex: 1 1 280px; display: flex; align-items: center; gap: 8px; padding: 0 12px; border-radius: var(--radius-md); border: 1px solid var(--color-line-strong); background: var(--color-bg); color: var(--color-muted); }
        .search:focus-within { outline: 2px solid var(--color-accent); outline-offset: 2px; }
        .search input { flex: 1; min-width: 0; font: inherit; padding: 9px 0; border: 0; background: none; color: var(--color-text); outline: none; }
        .segment { display: flex; gap: 2px; padding: 3px; border-radius: 10px; background: var(--color-sunken); }
        .segment button { border: 0; background: none; color: var(--color-muted); padding: 6px 12px; border-radius: 7px; }
        .segment button[aria-pressed="true"] { background: var(--color-surface); color: var(--color-text); font-weight: 600; box-shadow: 0 1px 2px rgb(20 22 26 / 0.12); }
        .segment .count { font-family: var(--font-mono); font-size: 12px; color: var(--color-muted); }
        .pills { gap: 8px !important; }
        .pills .label { font-size: 13px; font-weight: 600; color: var(--color-muted); margin-right: 4px; }
        .pill { padding: 4px 12px; font-size: 13px; border-radius: var(--radius-pill); border-color: var(--color-line); }
        .pill[aria-pressed="true"] { background: var(--color-text); border-color: var(--color-text); color: var(--color-surface); }

        .cards { list-style: none; padding: 0; margin: 0; display: grid; gap: 24px; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); }
        .card { background: var(--color-surface); border: 1px solid var(--color-line); border-radius: var(--radius-lg); overflow: hidden; display: flex; flex-direction: column; }
        .card .shot { display: block; aspect-ratio: 16 / 10; background: var(--color-sunken); border-bottom: 1px solid var(--color-line); }
        .card .shot img { display: block; width: 100%; height: 100%; object-fit: cover; object-position: top; }
        .card .none, .top .none { display: flex; height: 100%; align-items: center; justify-content: center; color: var(--color-muted); font-size: 13px; }
        .card .about { padding: 14px 16px 16px; display: flex; flex-direction: column; gap: 2px; flex: 1; }
        .card .title { font-size: 17px; font-weight: 600; color: var(--color-text); overflow-wrap: anywhere; }
        .card .about > .mono { font-size: 12px; overflow-wrap: anywhere; }
        .card .meta { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; margin: 8px 0 0; font-size: 13px; }
        .card .actions { margin-top: auto; padding-top: 14px; }

        .top { display: grid; grid-template-columns: minmax(0, 1fr) 400px; gap: 40px; align-items: start; }
        .top .shot { margin: 0; }
        .top .shot img, .top .shot .none { display: block; width: 100%; height: auto; aspect-ratio: 16 / 10; border: 1px solid var(--color-line); border-radius: var(--radius-lg); background: var(--color-surface); }
        .top .shot figcaption { color: var(--color-muted); font-size: 13px; margin-top: 8px; }
        .side { display: grid; gap: 16px; }
        .side .heading h1 { margin: 6px 0 4px; }
        .side .heading p { margin: 0; font-size: 14px; }
        .side .notice { margin: 0; font-size: 13px; }
        .link { display: grid; gap: 6px; }
        .label { font-size: 13px; font-weight: 600; color: var(--color-muted); }
        .field { display: flex; border: 1px solid var(--color-line-strong); border-radius: var(--radius-md); background: var(--color-surface); overflow: hidden; }
        .field input { flex: 1; min-width: 0; border: 0; padding: 9px 12px; background: none; color: var(--color-text); font: 13px var(--font-mono); text-overflow: ellipsis; }
        .field button { border: 0; border-left: 1px solid var(--color-line-strong); border-radius: 0; background: var(--color-sunken); font-size: 13px; font-weight: 600; }
        .facts { margin: 0; border: 1px solid var(--color-line); border-radius: var(--radius-lg); background: var(--color-surface); }
        .facts div { display: grid; grid-template-columns: 92px minmax(0, 1fr); gap: 12px; padding: 10px 14px; border-bottom: 1px solid var(--color-line); font-size: 13px; }
        .facts div:last-child { border-bottom: 0; }
        .facts dt { color: var(--color-muted); }
        .facts dd { margin: 0; overflow-wrap: anywhere; }

        .code { position: relative; }
        .code pre { margin: 0; background: var(--color-code); color: var(--color-code-text); border: 0; border-radius: 10px; padding: 16px 92px 16px 16px; overflow-x: auto; white-space: pre-wrap; overflow-wrap: anywhere; line-height: 1.6; }
        .code button { position: absolute; top: 10px; right: 10px; padding: 4px 10px; font-size: 12px; color: #9a9ea6; background: rgb(255 255 255 / 0.08); border: 0; border-radius: 6px; }
        .code button:hover { color: #ffffff; }
        pre { background: var(--color-sunken); border: 1px solid var(--color-line); border-radius: var(--radius-md); padding: 12px; overflow-x: auto; white-space: pre-wrap; overflow-wrap: anywhere; }

        .table { overflow-x: auto; border: 1px solid var(--color-line); border-radius: var(--radius-lg); background: var(--color-surface); }
        table { width: 100%; border-collapse: collapse; }
        th, td { text-align: left; padding: 10px 16px; border-bottom: 1px solid var(--color-line); vertical-align: middle; }
        tbody tr:last-child td { border-bottom: 0; }
        th { font-size: 12px; color: var(--color-muted); font-weight: 600; background: var(--color-sunken); }
        td.end { text-align: right; white-space: nowrap; }
        tr.why td { color: var(--color-muted); font-size: 13px; padding-top: 0; }
        tr:has(+ tr.why) td { border-bottom: 0; }

        details { margin-top: 16px; padding: 12px 16px; border: 1px solid var(--color-line); border-radius: 10px; background: var(--color-surface); }
        summary { cursor: pointer; font-weight: 500; }
        details pre { margin: 12px 0 0; }

        .submit { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); gap: 40px; align-items: start; }
        .submit h2 { margin-top: 0; }
        .submit ul { padding-left: 20px; margin: 0 0 16px; display: grid; gap: 8px; }
        form { display: grid; gap: 14px; padding: 24px; border: 1px solid var(--color-line); border-radius: var(--radius-lg); background: var(--color-surface); }
        form label { display: grid; gap: 4px; font-weight: 600; }
        form label .muted { font-weight: 400; font-size: 13px; }
        form input, form select { font: inherit; padding: 9px 12px; border-radius: var(--radius-md); border: 1px solid var(--color-line-strong); background: var(--color-bg); color: var(--color-text); width: 100%; }
        .problems { color: var(--color-danger); margin: 0; }

        @media (max-width: 900px) {
          .top, .submit { grid-template-columns: 1fr; }
          .hero { flex-direction: column; align-items: flex-start; gap: 16px; }
        }
        @media (max-width: 640px) {
          .bar { padding: 0 12px 0 16px; }
          .bar nav a:first-child, .bar nav a:nth-child(3) { display: none; }
          main { padding: 24px 16px 48px; }
          footer { padding: 16px; }
          h1 { font-size: 26px; }
          .lead { font-size: 15px; }
          .segment { width: 100%; }
          .segment button { flex: 1; padding: 6px 4px; }
          .segment .count { display: none; }
          .cards { grid-template-columns: 1fr; }
        }
        """;
}
