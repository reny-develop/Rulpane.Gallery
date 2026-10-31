# Rulpane.Gallery

Where views, themes and panesets for [Rulpane](https://github.com/reny-develop/Rulpane) are
published, found and submitted. Anybody may submit one; it is recorded once a machine has read
it, and **nobody reviews what it says**.

**Browse it: <https://reny-develop.github.io/Rulpane.Gallery/>** · **Submit one:
<https://reny-develop.github.io/Rulpane.Gallery/submit/>**

| Kind | What it is | A link names it in |
| --- | --- | --- |
| view | where each value goes on the screen, and which moves are offered | `v=octocat/kanban@1` |
| theme | colours, corners, typeface and spacing | `t=octocat/forest@1` |
| paneset | rules, view, theme and where to start, as one document | `p=octocat/othello@1` |

(`octocat/…` names show the form; they are not releases.) What each of those documents is, is Rulpane's to say, in [its readme](https://github.com/reny-develop/Rulpane#the-url).
Rule sets are published to the [Rulealize Registry](https://github.com/reny-develop/Rulealize.Registry)
and named from here, never submitted here.

## How a release is recorded

It is submitted from the [submit page](https://reny-develop.github.io/Rulpane.Gallery/submit/),
which opens an issue with every field filled in: what kind it is, its name, the public
repository and the commit to read it at, the file, and the sample it was tried in.
[`submit.yml`](.github/workflows/submit.yml) reads that issue with [`tool/Admit`](tool/Admit/),
and **records it without anybody reading it** once it passes. One that does not pass waits on
nobody either: the answer on the issue says why, and editing the issue reads it again.

What it is held to — whose it may be, which licences, which words, and that it runs — is in
[the policy](doc/policy.md). `tool/Admit` reads every document with Rulpane's own code, built
from a checkout of Rulpane, so a release that passes here is one Rulpane opens.

## The ledger

[`ledger/submitted.json`](ledger/submitted.json) holds one line per release: its name, the
number the Gallery gave it, its kind, the repository, the commit, the file, the licence, the
SHA-256 of the document, and the sample's path. Beside each line, `release/<owner>/<name>/<n>/`
holds the copy: `document.json` as its author committed it, to the byte, the repository's
`LICENSE` at that commit, and `sample.json`. **A release stays openable when its repository is
deleted or rewritten**, which is why the copy is in git rather than fetched.

The site serves each release that is not withheld as `release/<owner>/<name>/<n>.json` — the
document and where it was read from — and that is where Rulpane reads it, as a link naming it is
opened. A release is there to open as soon as the site is published after it is recorded.

A release that has to stop opening — one reported as misleading, say — gets
`"withheld": "<why>"` on its line. It stays in the ledger and on its page, marked; it is no
longer offered or served, and Rulpane stops opening it, and every paneset that names it, as soon
as the site is published again.

## The pages and the checks

[`tool/Site`](tool/Site/) renders the ledger and the copies: every name, which the index narrows by
kind, by rule set and by words; a page for each — how to use it, every release, the document, its
sample and its licence, and a link that opens it, with a button that copies it; and the submit page.
They look as Rulpane does: its colours, its mark and icons, and its fonts, copied from the checkout.
Rule sets are looked for in the Registry, which the pages link; here, each rule set — those Rulpane
bundles and those in the Registry, under the id each declares — is only what the index narrows by,
and a view's page says which ones it **fits as they are**: those Rulpane reads it against with no
`b` and plays from their start, every position as the view expects.

The same is written as text for an AI, at
[`llms.txt`](https://reny-develop.github.io/Rulpane.Gallery/llms.txt): every name's newest release
that is not withheld, the URL of its document, and the URL of each document it names — its rule
set, its view, its theme — so that a whole working one can be copied and changed. How those
documents are written is left to Rulpane's own `llms.txt`, which it links.

Each name is shown with a **picture of the screen its newest release opens** — a paneset as it
is, a view or a theme in its sample — taken by [`tool/Shot`](tool/Shot/shoot.mjs) in headless
Chrome, from a Rulpane served on the same machine with this ledger's releases beside it. A release
whose screen does not come up has no picture, and its page says nothing about one.

[`pages.yml`](.github/workflows/pages.yml) runs on every push and once a week: every case in
[`tool/Admit/test/`](tool/Admit/test/), then `tool/Admit check`, which holds every line to its
copy and runs every release in its sample again with that week's Rulpane, and only then takes
the pictures, renders the pages and publishes them. A change to Rulpane that would stop a published release is seen here
first, and publishes nothing.

```sh
bash tool/Admit/test/run.sh
dotnet run --project tool/Admit -- check
dotnet run --project tool/Site -- work/site
```

A sample whose rules are the Registry's, or hold one, is read from the Registry and nuget.org,
as Rulpane reads it, so these need the network.

The pictures take a Rulpane to open the releases in, with this ledger's releases beside it and an
`appsettings.json` telling it to read the Gallery there, served from anywhere — the shot list's
fragments are opened against the URL given — and Node 22 or later:

```sh
dotnet publish ../Rulpane/src/Rulpane.Viewer -c Release -o work/viewer
dotnet run --project tool/Site -- --releases-to work/viewer/wwwroot/gallery
echo '{ "Gallery": "gallery/" }' > work/viewer/wwwroot/appsettings.json
python3 -m http.server 5080 -d work/viewer/wwwroot &
dotnet run --project tool/Site -- --shots-to-take work/shots.tsv
node tool/Shot/shoot.mjs work/shots.tsv http://127.0.0.1:5080/ work/shots   # Chrome, or $CHROME
dotnet run --project tool/Site -- work/site --shots work/shots
```

The tools build against Rulpane: on a workstation the clone beside this one, in CI a checkout in
`rulpane/`.

## Setting it up

- **Pages**: Settings → Pages, Source "GitHub Actions"
- **main** accepts a push from `GITHUB_TOKEN`, which is how a release is recorded without a pull
  request. The label `submission` exists

## License

Apache-2.0 covers what this repository is made of — the tools under [`tool/`](tool/), the
workflows under [`.github/`](.github/), and the prose. The ledger is dedicated to the public
domain under [CC0-1.0](ledger/LICENSE-CC0). **Each release under `release/` is its author's**,
under the licence beside it.
