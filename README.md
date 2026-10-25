# Rulpane.Gallery

Where views, themes and panesets for [Rulpane](https://github.com/reny-develop/Rulpane) are
published, found and submitted. Anybody may submit one; it is recorded once a machine has read
it, and **nobody reviews what it says**.

> **Not open yet.** It is built and tested, and waits on GitHub: neither these pages nor
> Rulpane is published.

| Kind | What it is | A link names it in |
| --- | --- | --- |
| view | where each value goes on the screen, and which moves are offered | `v=octocat/kanban@1` |
| theme | colours, corners, typeface and spacing | `t=octocat/forest@1` |
| paneset | rules, view, theme and where to start, as one document | `p=octocat/othello@1` |

What each of those documents is, is Rulpane's to say, in [its readme](https://github.com/reny-develop/Rulpane#the-url).
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

Rulpane copies the releases out of a checkout of this repository on every deploy — its
`tool/Mirror --gallery` — holding each copy to its line's SHA-256 and leaving out what is
withheld. The pages are not where Rulpane reads anything from, so their being down or behind
changes what can be found, never what a link opens.

A release that has to stop opening — one reported as misleading, say — gets
`"withheld": "<why>"` on its line. It stays in the ledger and on its page, marked; it is no
longer offered, and Rulpane stops opening it, and every paneset that names it, at its next deploy.

## The pages and the checks

[`tool/Site`](tool/Site/) renders the ledger and the copies, and nothing else: every name, a page
for each — how to use it, every release, the document, its sample and its licence, and a link
that opens it — and the submit page.

[`pages.yml`](.github/workflows/pages.yml) runs on every push and once a week: every case in
[`tool/Admit/test/`](tool/Admit/test/), then `tool/Admit check`, which holds every line to its
copy and runs every release in its sample again with that week's Rulpane, and only then renders
and publishes the pages. A change to Rulpane that would stop a published release is seen here
first, and publishes nothing.

```sh
bash tool/Admit/test/run.sh
dotnet run --project ../Rulpane/tool/Mirror -- work/registry   # for samples that use the Registry
dotnet run --project tool/Admit -- check --registry work/registry
dotnet run --project tool/Site -- work/site
```

The tools build against Rulpane: on a workstation the clone beside this one, in CI a checkout in
`rulpane/`.

## Setting it up

- **Pages**: Settings → Pages, Source "GitHub Actions"
- **Rulpane**: while it is private, a secret `RULPANE_READ_TOKEN` holding a token that can read
  `reny-develop/Rulpane`; once it is public, nothing
- **main** accepts a push from `GITHUB_TOKEN`, which is how a release is recorded without a pull
  request. The label `submission` exists

## License

Apache-2.0 covers what this repository is made of — the tools under [`tool/`](tool/), the
workflows under [`.github/`](.github/), and the prose. The ledger is dedicated to the public
domain under [CC0-1.0](ledger/LICENSE-CC0). **Each release under `release/` is its author's**,
under the licence beside it.
