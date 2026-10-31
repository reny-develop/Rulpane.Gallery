using System.Net;

namespace Rulpane.Gallery;

/// <summary>
/// The submit page: a form that checks what can be checked in a browser and opens the issue with
/// every field filled in. The patterns are the ones tool/Admit/Submission.cs holds a submission
/// to, restated for a browser, and the query parameters are the ids of the fields in
/// .github/ISSUE_TEMPLATE/submit.yml: change one there and change it here. The reading itself is
/// tool/Admit's; this only saves a round trip for a typo.
/// </summary>
public static class Submit
{
    public static string Page(string repository) => $$"""
        <p class="crumb"><a href="../">Gallery</a> <span>/</span> Submit</p>
        <h1>Submit to the Gallery</h1>
        <p class="lead">A view, a theme or a paneset is read out of a public repository of yours or your organisation's, at one commit, and recorded once a machine has read it. Nobody approves it, and nobody reviews what it says.</p>
        <div class="submit">
        <div>
        <h2>What it is held to</h2>
        <ul>
          <li>It is named under your GitHub account, and the repository is yours — or, for an organisation's repository, it is named under the organisation, and you are a public member of it. A name is one kind of thing for good.</li>
          <li>The repository is under a licence that lets anybody keep and serve a copy: MIT, MIT-0, 0BSD, ISC, BSD-2-Clause, BSD-3-Clause, Apache-2.0, Unlicense, CC0-1.0 or CC-BY-4.0. The Gallery keeps one.</li>
          <li>Its words hold no address, e-mail address, domain name or markup, and no character that is not shown.</li>
          <li>It runs: a paneset as it is, a view in the sample you name — a paneset with everything but <code>v</code> — and a theme, if you name a sample, in one with everything but <code>t</code>. From the start, for up to 32 positions, each one as the view expects.</li>
        </ul>
        <p>The whole of it is the <a href="https://github.com/{{WebUtility.HtmlEncode(repository)}}/blob/main/doc/policy.md">policy</a>.</p>
        </div>
        <form id="submit" novalidate>
          <label>Kind
            <select id="kind"><option>View</option><option>Theme</option><option>Paneset</option></select></label>
          <label>Name <span class="muted">— your account or your organisation's, then a name for it, in lowercase</span>
            <input id="name" placeholder="octocat/kanban" autocomplete="off" required></label>
          <label>Repository
            <input id="repository" placeholder="octocat/rulpane-panes" autocomplete="off" required></label>
          <label>Commit <span class="muted">— what <code>git rev-parse HEAD</code> prints</span>
            <input id="commit" class="mono" placeholder="0123456789abcdef0123456789abcdef01234567" autocomplete="off" required></label>
          <label>Path <span class="muted">— of the document, from the top of the repository</span>
            <input id="path" placeholder="kanban.json" autocomplete="off" required></label>
          <label>Sample <span class="muted">— a paneset in the repository, without the part you are submitting: needed for a view, optional for a theme, none for a paneset</span>
            <input id="sample" placeholder="samples/kanban.json" autocomplete="off"></label>
          <ul id="problems" class="problems" hidden></ul>
          <button type="submit" class="button primary">Open the issue on GitHub</button>
        </form>
        </div>
        <script>
        const form = document.getElementById("submit");
        const problems = document.getElementById("problems");
        const value = id => document.getElementById(id).value.trim();
        const path = /^[A-Za-z0-9._-]+(\/[A-Za-z0-9._-]+)*\.json$/;
        const climbs = p => p.split("/").some(s => s === "." || s === "..");
        form.addEventListener("submit", event => {
          event.preventDefault();
          const wrong = [];
          const kind = value("kind"), name = value("name"), repository = value("repository"), sample = value("sample");
          if (!/^[a-z0-9](?:[a-z0-9-]{0,37}[a-z0-9])?\/[a-z0-9][a-z0-9-]{0,63}$/.test(name)) wrong.push("The name is the repository's account and a name, in lowercase: octocat/kanban.");
          if (!/^[A-Za-z0-9](?:[A-Za-z0-9-]{0,37}[A-Za-z0-9])?\/[A-Za-z0-9._-]{1,100}$/.test(repository)) wrong.push("The repository is owner/name.");
          else if (name.includes("/") && repository.split("/")[0].toLowerCase() !== name.split("/")[0]) wrong.push("The repository belongs to the account the name is under.");
          if (!/^[0-9a-f]{40}$/.test(value("commit"))) wrong.push("The commit is all 40 hexadecimal characters of one.");
          if (!path.test(value("path")) || climbs(value("path"))) wrong.push("The path is a .json file, from the top of the repository.");
          if (sample && (!path.test(sample) || climbs(sample))) wrong.push("The sample is a .json path, or left empty.");
          if (kind === "View" && !sample) wrong.push("A view is tried in a sample: name a paneset with everything but v.");
          if (kind === "Paneset" && sample) wrong.push("A paneset is tried as it is: leave the sample empty.");
          problems.replaceChildren(...wrong.map(w => Object.assign(document.createElement("li"), { textContent: w })));
          problems.hidden = wrong.length === 0;
          if (wrong.length > 0) return;
          const query = new URLSearchParams({ template: "submit.yml", title: `Submit ${name}` });
          for (const field of ["kind", "name", "repository", "commit", "path", "sample"]) {
            if (value(field)) query.set(field, value(field));
          }
          location.href = `https://github.com/{{WebUtility.HtmlEncode(repository)}}/issues/new?${query}`;
        });
        </script>

        """;
}
