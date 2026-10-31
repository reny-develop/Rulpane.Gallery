namespace Rulpane.Gallery;

/// <summary>
/// The little the pages do in a browser. Each page is whole without it: the filter is shown only
/// once it runs, and a link copied by hand is the same link.
/// </summary>
public static class Script
{
    /// <summary>Every button with a <c>data-copy</c> puts that text on the clipboard, and says so.</summary>
    public const string Copy = """
        document.addEventListener("click", event => {
          const button = event.target.closest("[data-copy]");
          if (!button || !navigator.clipboard) return;
          navigator.clipboard.writeText(button.dataset.copy).then(() => {
            const label = button.querySelector("span") ?? button;
            label.textContent = "Copied";
            setTimeout(() => { label.textContent = "Copy"; }, 1500);
          });
        });
        """;

    /// <summary>
    /// The index's filter: words, a kind and a rule set, held in the address as <c>?q=</c>,
    /// <c>?kind=</c> and <c>?rules=</c> so that a filtered list is a link too.
    /// </summary>
    public const string Filter = """
        (() => {
          const filters = document.getElementById("filters");
          const cards = [...document.querySelectorAll("#cards > .card")];
          const nothing = document.getElementById("nothing");
          const q = document.getElementById("q");
          const params = new URLSearchParams(location.search);
          const state = { q: params.get("q") ?? "", kind: params.get("kind") ?? "", rules: params.get("rules") ?? "" };
          const press = (selector, attribute, value) => {
            for (const b of filters.querySelectorAll(selector)) b.setAttribute("aria-pressed", String(b.dataset[attribute] === value));
          };
          const apply = () => {
            const words = state.q.toLowerCase().split(/\s+/).filter(Boolean);
            let shown = 0;
            for (const card of cards) {
              const show = (!state.kind || card.dataset.kind === state.kind)
                && (!state.rules || card.dataset.rules.split(" ").includes(state.rules))
                && words.every(w => card.dataset.text.includes(w));
              card.hidden = !show;
              shown += show;
            }
            nothing.hidden = shown > 0;
            press("[data-kind]", "kind", state.kind);
            press("[data-rules]", "rules", state.rules);
            const query = new URLSearchParams(Object.entries(state).filter(([, v]) => v));
            history.replaceState(null, "", query.size ? `?${query}` : location.pathname);
          };
          filters.addEventListener("click", event => {
            const button = event.target.closest("button");
            if (!button) return;
            if ("kind" in button.dataset) state.kind = button.dataset.kind;
            if ("rules" in button.dataset) state.rules = button.dataset.rules;
            apply();
          });
          q.value = state.q;
          q.addEventListener("input", () => { state.q = q.value.trim(); apply(); });
          filters.hidden = false;
          apply();
        })();
        """;
}
